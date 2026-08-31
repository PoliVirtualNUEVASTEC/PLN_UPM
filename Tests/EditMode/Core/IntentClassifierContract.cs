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
    }
}
