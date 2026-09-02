using NpcAi.Core;
using NpcAi.Core.Tests;
using NUnit.Framework;

namespace NpcAi.Nlu.Tests
{
    /// <summary>
    /// Pruebas contractuales y de dominio para la implementación real de M2 (<see cref="NluIntentClassifier"/>).
    /// Hereda la suite completa de <see cref="IntentClassifierContract"/>.
    /// </summary>
    [TestFixture]
    public class NluIntentClassifierTests : IntentClassifierContract
    {
        protected override IIntentClassifier CreateSubject() => new NluIntentClassifier();

        [Test]
        public void Clasificador_instanciado_por_defecto_esta_listo()
        {
            var classifier = new NluIntentClassifier();
            Assert.IsTrue(classifier.IsReady);
        }

        [Test]
        public void Clasificador_no_listo_devuelve_Unknown_y_confianza_cero()
        {
            var classifier = new NluIntentClassifier(isReady: false);
            Assert.IsFalse(classifier.IsReady);

            var resultado = classifier.Classify("Por favor, necesito los signos vitales");
            Assert.AreEqual(Intent.Desconocida, resultado.Intent);
            Assert.AreEqual(Tone.Neutral, resultado.Tone);
            Assert.AreEqual(0f, resultado.Confidence);
            Assert.GreaterOrEqual(resultado.LatencyMs, 0f);
        }

        [Test]
        public void Mide_latencia_en_milisegundos_positiva()
        {
            var classifier = new NluIntentClassifier();
            var resultado = classifier.Classify("Por favor ayudeme con el paciente de urgencias");

            Assert.GreaterOrEqual(resultado.LatencyMs, 0f);
            Assert.AreEqual(Intent.SolicitudRespetuosa, resultado.Intent);
        }

        [Test]
        public void Clasifica_solicitud_con_tono_respetuoso()
        {
            var classifier = new NluIntentClassifier();
            var resultado = classifier.Classify("Doctor, por favor podría revisar al paciente");

            Assert.AreEqual(Intent.SolicitudRespetuosa, resultado.Intent);
            Assert.AreEqual(Tone.Respetuoso, resultado.Tone);
            Assert.Greater(resultado.Confidence, 0.7f);
        }

        [Test]
        public void Clasifica_solicitud_agresiva_con_tono_agresivo()
        {
            var classifier = new NluIntentClassifier();
            var resultado = classifier.Classify("¡Cállese y hágalo ya de una vez!");

            Assert.AreEqual(Intent.SolicitudAgresiva, resultado.Intent);
            Assert.AreEqual(Tone.Agresivo, resultado.Tone);
        }

        [Test]
        public void Clasifica_empatia_con_tono_empatico()
        {
            var classifier = new NluIntentClassifier();
            var resultado = classifier.Classify("Tranquilo, entiendo perfectamente la situación");

            Assert.AreEqual(Intent.Empatia, resultado.Intent);
            Assert.AreEqual(Tone.Empatico, resultado.Tone);
        }

        [Test]
        public void Clasifica_informacion_clinica_con_tono_neutral()
        {
            var classifier = new NluIntentClassifier();
            var resultado = classifier.Classify("El paciente presenta saturacion en 92 y presion alta");

            Assert.AreEqual(Intent.AportaInformacion, resultado.Intent);
            Assert.AreEqual(Tone.Neutral, resultado.Tone);
        }

        [Test]
        public void Clasifica_pregunta_fuera_de_tema()
        {
            var classifier = new NluIntentClassifier();
            var resultado = classifier.Classify("¿Viste el partido de fútbol anoche?");

            Assert.AreEqual(Intent.PreguntaFueraDeTema, resultado.Intent);
        }

        [Test]
        public void Clasifica_interrupcion()
        {
            var classifier = new NluIntentClassifier();
            var resultado = classifier.Classify("Un momento, espere por favor");

            Assert.AreEqual(Intent.Interrupcion, resultado.Intent);
        }
    }
}
