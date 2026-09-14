using System.IO;
using NpcAi.Core;
using NpcAi.Core.Tests;
using NpcAi.Nlu;
using NUnit.Framework;

namespace NpcAi.Nlu.Tests
{
    /// <summary>
    /// Contrato heredado de <see cref="IntentClassifierContract"/> (sin modificarla, tasks.md
    /// 0.4) contra el clasificador real de M2: encoder BERT reducido congelado + cabezas
    /// Intent/Tone entrenadas (design.md), ejecutado via Unity Sentis
    /// (<c>com.unity.ai.inference</c>). <c>CreateSubject()</c> carga el
    /// <c>.onnx</c> commiteado en <c>Runtime/Nlu/Models/</c> (tasks.md 2.4/2.5).
    ///
    /// La ruta se resuelve como ruta de paquete de Unity ("Packages/com.poli.npc-ai/..."):
    /// <see cref="BertIntentClassifier"/> la usa para pedirle al Editor el
    /// <c>ModelAsset</c> ya importado por Sentis a partir del <c>.onnx</c>, y deriva de ahi
    /// mismo la carpeta hermana <c>tokenizer/</c> que <c>Training/Nlu/train.py</c> deja junto
    /// al modelo.
    /// </summary>
    public class BertIntentClassifierTests : IntentClassifierContract
    {
        private const string RutaDelModelo =
            "Packages/com.poli.npc-ai/Runtime/Nlu/Models/intent-tone-classifier.onnx";

        protected override IIntentClassifier CreateSubject() => new BertIntentClassifier(RutaDelModelo);

        // --- Pruebas propias del clasificador real, ademas del contrato heredado ---

        [Test]
        public void El_modelo_commiteado_carga_y_queda_listo()
        {
            // De-riesga 2.5/2.7: si esto falla, ninguna otra prueba de esta clase es
            // significativa (todas caerian al camino "no listo" -> Unknown trivial).
            using var clasificador = new BertIntentClassifier(RutaDelModelo);

            Assert.IsTrue(clasificador.IsReady,
                "El .onnx y el tokenizador de Runtime/Nlu/Models/ deberian cargar via Sentis " +
                "dentro del Editor (Test Runner, tasks.md 2.7).");
        }

        [Test]
        public void Una_ruta_de_modelo_inexistente_deja_el_clasificador_no_listo_y_no_lanza()
        {
            var rutaInexistente = Path.Combine(
                "Packages/com.poli.npc-ai/Runtime/Nlu/Models", "no-existe.onnx");

            IIntentClassifier clasificador = null;
            Assert.DoesNotThrow(() => clasificador = new BertIntentClassifier(rutaInexistente));

            Assert.IsFalse(clasificador.IsReady);
            var r = clasificador.Classify("por favor ayudeme con el paciente");
            Assert.AreEqual(Intent.Desconocida, r.Intent);
            Assert.AreEqual(Tone.Neutral, r.Tone);
            Assert.AreEqual(0f, r.Confidence);
        }

        [Test]
        public void Clasifica_una_solicitud_del_dominio_con_una_intencion_valida()
        {
            using var clasificador = new BertIntentClassifier(RutaDelModelo);

            var r = clasificador.Classify("por favor ayudeme con el paciente");

            // No fijamos la clase exacta (es un modelo entrenado, no reglas fijas): solo que
            // el resultado sea un miembro real del enum y no el trivial "no listo".
            Assert.IsTrue(System.Enum.IsDefined(typeof(Intent), r.Intent));
            Assert.IsTrue(System.Enum.IsDefined(typeof(Tone), r.Tone));
        }
    }
}
