using NpcAi.Core;
using NpcAi.Speech;
using NUnit.Framework;

namespace NpcAi.Speech.Tests
{
    /// <summary>
    /// Guarda de emision tardia (Decision 5 de design.md) y clamps de <see cref="Utterance"/>
    /// (G14) sobre <see cref="OfflineSpeechToText"/>. Estas pruebas no son parte de
    /// <c>SpeechToTextContract</c> porque ejercitan costuras internas especificas de la
    /// implementacion real (contador de generacion, acotado de confianza/duracion) que el
    /// doble scripted no necesita reproducir.
    /// </summary>
    public class OfflineSpeechToTextGuardTests
    {
        [Test]
        public void Resultado_de_generacion_anterior_no_se_emite_en_la_vigente()
        {
            var subject = new OfflineSpeechToText();
            var recibidas = 0;
            subject.OnUtterance += _ => recibidas++;

            subject.StartListening();
            var generacionVieja = subject.GeneracionActual;
            subject.StopListening();
            subject.StartListening();

            var emitido = subject.ProcesarResultadoDePrueba("resultado tardio", 0.9f, 1f, generacionVieja);

            Assert.IsFalse(emitido);
            Assert.AreEqual(0, recibidas);
            Assert.IsTrue(subject.IsListening);
        }

        [Test]
        public void Resultado_que_resuelve_despues_de_StopListening_no_se_emite()
        {
            var subject = new OfflineSpeechToText();
            var recibidas = 0;
            subject.OnUtterance += _ => recibidas++;

            subject.StartListening();
            subject.StopListening();
            var generacionActual = subject.GeneracionActual;

            var emitido = subject.ProcesarResultadoDePrueba("resultado tardio", 0.9f, 1f, generacionActual);

            Assert.IsFalse(emitido);
            Assert.AreEqual(0, recibidas);
        }

        [TestCase(1.7f, 1f)]
        [TestCase(-0.2f, 0f)]
        [TestCase(0.5f, 0.5f)]
        public void Confidence_se_acota_a_0_1_antes_de_emitir(float confianzaCruda, float confianzaEsperada)
        {
            var subject = new OfflineSpeechToText();
            var recibida = default(Utterance);
            subject.OnUtterance += u => recibida = u;

            subject.StartListening();
            subject.ProcesarResultadoDePrueba("texto", confianzaCruda, 1f, subject.GeneracionActual);

            Assert.AreEqual(confianzaEsperada, recibida.Confidence);
        }

        [Test]
        public void DurationSeconds_negativo_se_acota_a_cero_antes_de_emitir()
        {
            var subject = new OfflineSpeechToText();
            var recibida = default(Utterance);
            subject.OnUtterance += u => recibida = u;

            subject.StartListening();
            subject.ProcesarResultadoDePrueba("texto", 1f, -3f, subject.GeneracionActual);

            Assert.AreEqual(0f, recibida.DurationSeconds);
        }

        [Test]
        public void Confianza_baja_igual_se_emite_sin_filtrar()
        {
            var subject = new OfflineSpeechToText();
            var recibidas = 0;
            subject.OnUtterance += _ => recibidas++;

            subject.StartListening();
            var emitido = subject.ProcesarResultadoDePrueba("hola", 0.05f, 1f, subject.GeneracionActual);

            Assert.IsTrue(emitido);
            Assert.AreEqual(1, recibidas);
        }
    }
}
