using NpcAi.Core;
using NUnit.Framework;

namespace NpcAi.Core.Tests
{
    /// <summary>
    /// Contrato de <see cref="IIntentClassifier"/>. El doble de M2 y el clasificador
    /// real (MiniLM en Sentis) heredan de aqui y pasan las mismas pruebas.
    /// </summary>
    public abstract class IntentClassifierContract
    {
        protected abstract IIntentClassifier CreateSubject();

        [Test]
        public void Reporta_si_esta_listo_sin_lanzar()
        {
            Assert.DoesNotThrow(() => { var _ = CreateSubject().IsReady; });
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void Texto_vacio_devuelve_Desconocida_y_no_lanza(string texto)
        {
            var r = CreateSubject().Classify(texto);

            Assert.AreEqual(Intent.Desconocida, r.Intent);
            Assert.AreEqual(0f, r.Confidence);
        }

        [Test]
        public void La_confianza_siempre_esta_entre_cero_y_uno()
        {
            var s = CreateSubject();

            foreach (var texto in new[] { "por favor ayudeme", "hagalo ya", "no entiendo nada", "" })
            {
                var r = s.Classify(texto);
                Assert.GreaterOrEqual(r.Confidence, 0f, $"texto: '{texto}'");
                Assert.LessOrEqual(r.Confidence, 1f, $"texto: '{texto}'");
            }
        }

        [Test]
        public void La_latencia_nunca_es_negativa()
        {
            Assert.GreaterOrEqual(CreateSubject().Classify("por favor").LatencyMs, 0f);
        }

        [Test]
        public void Es_determinista_para_la_misma_entrada()
        {
            var s = CreateSubject();

            var a = s.Classify("por favor ayudeme con el paciente");
            var b = s.Classify("por favor ayudeme con el paciente");

            Assert.AreEqual(a.Intent, b.Intent);
            Assert.AreEqual(a.Tone,   b.Tone);
        }

        [Test]
        public void Nunca_lanza_con_entradas_raras()
        {
            var s = CreateSubject();

            Assert.DoesNotThrow(() =>
            {
                s.Classify("!!!???");
                s.Classify(new string('a', 5000));
                s.Classify("123 456");
            });
        }

        // --- G9: Classify NO DEBE lanzar en ningun estado; no-listo DEBERIA devolver Unknown ---

        [Test]
        public void Classify_no_lanza_en_ningun_estado()
        {
            var listo = CreateSubject();
            var noListo = new ClasificadorNoListo();

            Assert.DoesNotThrow(() =>
            {
                var _ = listo.IsReady;
                listo.Classify(null);
                listo.Classify(string.Empty);
                listo.Classify("   ");
                listo.Classify("texto normal");

                var __ = noListo.IsReady;
                noListo.Classify(null);
                noListo.Classify("por favor");
            });
        }

        [Test]
        public void Un_clasificador_no_listo_devuelve_Unknown()
        {
            var noListo = new ClasificadorNoListo();

            var r = noListo.Classify("por favor");

            Assert.IsFalse(noListo.IsReady);
            Assert.AreEqual(Intent.Desconocida, r.Intent);
            Assert.AreEqual(Tone.Neutral,       r.Tone);
            Assert.AreEqual(0f,                 r.Confidence);
            Assert.AreEqual(0f,                 r.LatencyMs);
        }

        /// <summary>
        /// Stub minimo NO listo, definido DENTRO de Tests/EditMode/Core/ (decision D3):
        /// no es un doble de modulo. <c>ScriptedIntentClassifier</c> tiene
        /// <c>IsReady == true</c> por diseno, asi que el camino no-listo solo se alcanza
        /// con este stub local.
        /// </summary>
        private sealed class ClasificadorNoListo : IIntentClassifier
        {
            public bool IsReady => false;
            public IntentResult Classify(string text) => IntentResult.Unknown();
        }
    }
}
