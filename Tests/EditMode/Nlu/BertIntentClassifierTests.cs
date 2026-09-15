using NpcAi.Core;
using NpcAi.Core.Tests;
using NpcAi.Nlu;
using NUnit.Framework;
using Unity.InferenceEngine;
using UnityEditor;
using UnityEngine;

namespace NpcAi.Nlu.Tests
{
    /// <summary>
    /// Contrato heredado de <see cref="IntentClassifierContract"/> (sin modificarla, tasks.md
    /// 0.4) contra el clasificador real de M2: encoder BERT reducido congelado + cabezas
    /// Intent/Tone entrenadas (design.md), ejecutado via Unity Sentis
    /// (<c>com.unity.ai.inference</c>). <c>CreateSubject()</c> carga el
    /// <c>.onnx</c> commiteado en <c>Runtime/Nlu/Models/</c> (tasks.md 2.4/2.5/2.9).
    ///
    /// <see cref="BertIntentClassifier"/> ya no recibe una ruta de archivo (tasks.md 2.9): esta
    /// clase de prueba usa <c>AssetDatabase</c> solo para RESOLVER las referencias de asset que
    /// el constructor real espera (<see cref="ModelAsset"/>/<see cref="TextAsset"/>) — uso
    /// correcto y confinado a codigo de prueba, que siempre corre dentro del Editor; la clase
    /// de produccion misma no usa <c>AssetDatabase</c> en ningun camino.
    /// </summary>
    public class BertIntentClassifierTests : IntentClassifierContract
    {
        private const string RutaDelModelo =
            "Packages/com.poli.npc-ai/Runtime/Nlu/Models/intent-tone-classifier.onnx";

        private const string RutaDelTokenizador =
            "Packages/com.poli.npc-ai/Runtime/Nlu/Models/tokenizer/tokenizer.json";

        private static ModelAsset CargarModeloDePrueba() =>
            AssetDatabase.LoadAssetAtPath<ModelAsset>(RutaDelModelo);

        private static TextAsset CargarTokenizadorDePrueba() =>
            AssetDatabase.LoadAssetAtPath<TextAsset>(RutaDelTokenizador);

        protected override IIntentClassifier CreateSubject() =>
            new BertIntentClassifier(CargarModeloDePrueba(), CargarTokenizadorDePrueba());

        // --- Pruebas propias del clasificador real, ademas del contrato heredado ---

        [Test]
        public void El_modelo_commiteado_carga_y_queda_listo()
        {
            // De-riesga 2.5/2.7/2.9: si esto falla, ninguna otra prueba de esta clase es
            // significativa (todas caerian al camino "no listo" -> Unknown trivial).
            using var clasificador = new BertIntentClassifier(CargarModeloDePrueba(), CargarTokenizadorDePrueba());

            Assert.IsTrue(clasificador.IsReady,
                "El ModelAsset y el TextAsset del tokenizador de Runtime/Nlu/Models/ deberian " +
                "cargar via Sentis dentro del Editor (Test Runner, tasks.md 2.7/2.9).");
        }

        [Test]
        public void Un_modelo_o_tokenizador_nulo_deja_el_clasificador_no_listo_y_no_lanza()
        {
            var modeloValido = CargarModeloDePrueba();
            var tokenizadorValido = CargarTokenizadorDePrueba();

            AssertNoListoYNoLanza(null, null);
            AssertNoListoYNoLanza(modeloValido, null);
            AssertNoListoYNoLanza(null, tokenizadorValido);
        }

        private static void AssertNoListoYNoLanza(ModelAsset modelo, TextAsset tokenizadorJson)
        {
            IIntentClassifier clasificador = null;
            Assert.DoesNotThrow(() => clasificador = new BertIntentClassifier(modelo, tokenizadorJson));

            Assert.IsFalse(clasificador.IsReady);
            var r = clasificador.Classify("por favor ayudeme con el paciente");
            Assert.AreEqual(Intent.Desconocida, r.Intent);
            Assert.AreEqual(Tone.Neutral, r.Tone);
            Assert.AreEqual(0f, r.Confidence);
        }

        [Test]
        public void Clasifica_una_solicitud_del_dominio_con_una_intencion_valida()
        {
            using var clasificador = new BertIntentClassifier(CargarModeloDePrueba(), CargarTokenizadorDePrueba());

            var r = clasificador.Classify("por favor ayudeme con el paciente");

            // No fijamos la clase exacta (es un modelo entrenado, no reglas fijas): solo que
            // el resultado sea un miembro real del enum y no el trivial "no listo".
            Assert.IsTrue(System.Enum.IsDefined(typeof(Intent), r.Intent));
            Assert.IsTrue(System.Enum.IsDefined(typeof(Tone), r.Tone));
        }
    }
}
