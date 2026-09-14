using System;
using System.Diagnostics;
using System.IO;
using NpcAi.Core;
using Unity.InferenceEngine;
using Unity.InferenceEngine.Tokenization;
using Unity.InferenceEngine.Tokenization.Parsers.HuggingFace;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace NpcAi.Nlu
{
    /// <summary>
    /// Implementacion real de <see cref="IIntentClassifier"/> (M2, design.md): un encoder BERT
    /// reducido congelado + dos cabezas de clasificacion entrenadas (Intent, Tone) sobre el
    /// mismo embedding compartido (AD1/AD2), exportadas juntas a un unico grafo ONNX por
    /// <c>Training/Nlu/train.py</c> y ejecutadas on-device via Unity Sentis
    /// (<c>com.unity.ai.inference</c>, AD3). El pooling de tokens ya queda dentro del propio
    /// grafo ONNX (mean pooling enmascarado, ver <c>train.py</c>): este adaptador solo
    /// tokeniza, alimenta <c>input_ids</c>/<c>attention_mask</c> y lee <c>intent_probs</c>/
    /// <c>tone_probs</c>.
    ///
    /// <para>
    /// <b>Orden de clases</b>: el indice de cada salida ONNX coincide 1:1, por construccion,
    /// con el orden de declaracion de <see cref="Intent"/> y <see cref="Tone"/> en
    /// <c>Runtime/Core/Enums.cs</c> (<c>train.py</c>, tuplas <c>INTENT_CLASSES</c>/
    /// <c>TONE_CLASSES</c>, deja constancia explicita del mismo orden). El design de este
    /// cambio proponia leer esa correspondencia desde los <c>metadata_props</c> que
    /// <c>train.py</c> graba en el <c>.onnx</c> en vez de asumir un orden fijo; leyendo el
    /// paquete <c>com.unity.ai.inference</c> 2.6.1 instalado se confirmo que Sentis NO expone
    /// esos <c>metadata_props</c> en su API publica de runtime (<c>Editor/ONNX/
    /// ONNXModelConverter.cs</c> los emite unicamente via un evento Editor-only,
    /// <c>MetadataLoaded</c>, inalcanzable desde <see cref="Model"/>/<see cref="Worker"/> una
    /// vez cargado el modelo). Por eso se usa el indice directo — una desviacion documentada
    /// de esa nota de "de-riesgo" de <c>design.md</c>, con el mismo efecto practico: nunca se
    /// hardcodea un mapeo independiente del enum real, porque el enum YA es ese mapeo.
    /// </para>
    ///
    /// <para>
    /// <b>Carga del modelo</b>: Sentis no ofrece un conversor ONNX-&gt;<see cref="Model"/> en
    /// tiempo de ejecucion fuera del Editor (el unico importador vive en
    /// <c>Editor/ONNX/ONNXModelConverter.cs</c>, Editor-only). Dentro del Editor — el
    /// contexto real de <c>Tests/EditMode</c> — el <c>.onnx</c> commiteado ya fue importado
    /// automaticamente por Unity a un <see cref="ModelAsset"/> nativo, y esta clase lo carga
    /// via <c>AssetDatabase</c>. Fuera del Editor (build de jugador, wiring real de M11) esta
    /// via de carga por ruta de archivo queda pendiente — ver Deviations en
    /// <c>apply-progress.md</c> de este cambio: la integracion real necesitara exponer el
    /// <see cref="ModelAsset"/> ya importado (referencia serializada o Resources), no una
    /// ruta de archivo cruda.
    /// </para>
    /// </summary>
    public sealed class BertIntentClassifier : IIntentClassifier, IDisposable
    {
        private const string NombreEntradaIds = "input_ids";
        private const string NombreEntradaMascara = "attention_mask";
        private const string NombreSalidaIntent = "intent_probs";
        private const string NombreSalidaTone = "tone_probs";
        private const string CarpetaTokenizador = "tokenizer";
        private const string ArchivoTokenizador = "tokenizer.json";

        private static readonly int CantidadDeIntenciones = Enum.GetValues(typeof(Intent)).Length;
        private static readonly int CantidadDeTonos = Enum.GetValues(typeof(Tone)).Length;

        private readonly Worker _worker;
        private readonly ITokenizer _tokenizador;
        private bool _disposed;

        /// <param name="modelPath">
        /// Ruta al <c>.onnx</c> entrenado (por ejemplo,
        /// <c>"Packages/com.poli.npc-ai/Runtime/Nlu/Models/intent-tone-classifier.onnx"</c>).
        /// El tokenizador se busca en la carpeta hermana <c>tokenizer/tokenizer.json</c>, la
        /// misma convencion que deja <c>Training/Nlu/train.py</c> al exportar. Nunca lanza: si
        /// el modelo o el tokenizador no cargan, <see cref="IsReady"/> queda en <c>false</c>.
        /// </param>
        public BertIntentClassifier(string modelPath)
        {
            try
            {
                var modelo = CargarModelo(modelPath);
                var tokenizador = CargarTokenizador(modelPath);

                if (modelo != null && tokenizador != null)
                {
                    _worker = new Worker(modelo, BackendType.CPU); // CPU, no GPU: de-riesga el
                    // determinismo de Es_determinista_para_la_misma_entrada (tasks.md 2.6) —
                    // el orden de reduccion en GPUCompute no esta garantizado entre corridas.
                    _tokenizador = tokenizador;
                }
            }
            catch (Exception)
            {
                // Cualquier falla de carga deja el clasificador no listo, nunca lanza (mismo
                // contrato que exige Ports.cs para IsReady/Classify).
                _worker?.Dispose();
                _worker = null;
                _tokenizador = null;
            }
        }

        /// <summary>False hasta que el modelo y el tokenizador terminen de cargar sin error.</summary>
        public bool IsReady => !_disposed && _worker != null && _tokenizador != null;

        public IntentResult Classify(string text)
        {
            var cronometro = Stopwatch.StartNew();

            if (string.IsNullOrWhiteSpace(text) || !IsReady)
                return IntentResult.Unknown(LatenciaMs(cronometro));

            try
            {
                var codificacion = _tokenizador.Encode(text);
                var ids = codificacion.GetIds();
                var mascara = codificacion.GetAttentionMask();

                using var tensorIds = new Tensor<int>(new TensorShape(1, ids.Count), AArregloDeInts(ids));
                using var tensorMascara = new Tensor<int>(new TensorShape(1, mascara.Count), AArregloDeInts(mascara));

                _worker.SetInput(NombreEntradaIds, tensorIds);
                _worker.SetInput(NombreEntradaMascara, tensorMascara);
                _worker.Schedule();

                // PeekOutput no transfiere propiedad: el worker sigue siendo dueno del tensor,
                // por eso NO se dispone aca (ver Samples~/Run a model/ModelExecution.cs).
                var salidaIntent = _worker.PeekOutput(NombreSalidaIntent) as Tensor<float>;
                var salidaTone = _worker.PeekOutput(NombreSalidaTone) as Tensor<float>;

                if (salidaIntent == null || salidaTone == null)
                    return IntentResult.Unknown(LatenciaMs(cronometro));

                var probsIntent = salidaIntent.DownloadToArray(); // bloqueante, copia propia
                var probsTone = salidaTone.DownloadToArray();

                var (indiceIntent, confianzaIntent) = MaximoIndice(probsIntent);
                var (indiceTone, _) = MaximoIndice(probsTone);

                return new IntentResult(
                    AIntent(indiceIntent),
                    ATone(indiceTone),
                    Acotar(confianzaIntent),
                    LatenciaMs(cronometro));
            }
            catch (Exception)
            {
                // Nunca lanza (Ports.cs, Nunca_lanza_con_entradas_raras / Classify_no_lanza_en_ningun_estado).
                return IntentResult.Unknown(LatenciaMs(cronometro));
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _worker?.Dispose();
        }

        private static float LatenciaMs(Stopwatch cronometro) => (float)cronometro.Elapsed.TotalMilliseconds;

        private static int[] AArregloDeInts(System.Collections.Generic.IReadOnlyList<int> valores)
        {
            var arreglo = new int[valores.Count];
            for (var i = 0; i < valores.Count; i++)
                arreglo[i] = valores[i];
            return arreglo;
        }

        private static (int indice, float valor) MaximoIndice(float[] valores)
        {
            var mejorIndice = 0;
            var mejorValor = float.NegativeInfinity;
            for (var i = 0; i < valores.Length; i++)
            {
                if (valores[i] > mejorValor)
                {
                    mejorValor = valores[i];
                    mejorIndice = i;
                }
            }
            return (mejorIndice, mejorValor);
        }

        /// <summary>Acota a [0,1] y descarta NaN/Infinito (Acotamiento de confianza y latencia).</summary>
        private static float Acotar(float valor) =>
            float.IsFinite(valor) ? Math.Clamp(valor, 0f, 1f) : 0f;

        private static Intent AIntent(int indice) =>
            indice >= 0 && indice < CantidadDeIntenciones ? (Intent)indice : Intent.Desconocida;

        private static Tone ATone(int indice) =>
            indice >= 0 && indice < CantidadDeTonos ? (Tone)indice : Tone.Neutral;

        private static Model CargarModelo(string modelPath)
        {
            if (string.IsNullOrWhiteSpace(modelPath))
                return null;

#if UNITY_EDITOR
            // Camino real de Tests/EditMode: el .onnx commiteado ya fue importado por Unity a
            // un ModelAsset nativo (Editor/ONNX/ONNXModelConverter, Editor-only en el paquete
            // instalado) en el momento en que se agrego al proyecto.
            var modelAsset = AssetDatabase.LoadAssetAtPath<ModelAsset>(modelPath);
            if (modelAsset != null)
                return ModelLoader.Load(modelAsset);
#endif
            // Fuera del Editor, ModelLoader.Load(string) solo lee el formato binario propio
            // de Sentis (.sentis), no ONNX crudo. Se deja como via de respaldo para cuando la
            // integracion real (M11) provea un .sentis pre-convertido en esa ruta.
            return File.Exists(modelPath) ? ModelLoader.Load(modelPath) : null;
        }

        private static ITokenizer CargarTokenizador(string modelPath)
        {
            if (string.IsNullOrWhiteSpace(modelPath))
                return null;

            var carpetaDelModelo = Path.GetDirectoryName(modelPath) ?? string.Empty;
            var rutaTokenizador = Path.Combine(carpetaDelModelo, CarpetaTokenizador, ArchivoTokenizador);

            if (!File.Exists(rutaTokenizador))
                return null;

            var json = File.ReadAllText(rutaTokenizador, System.Text.Encoding.UTF8);
            return HuggingFaceParser.GetDefault().Parse(json);
        }
    }
}
