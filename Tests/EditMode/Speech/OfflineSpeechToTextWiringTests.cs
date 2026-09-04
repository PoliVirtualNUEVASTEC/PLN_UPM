using System.Collections.Generic;
using NpcAi.Core;
using NpcAi.Speech;
using NpcAi.Speech.Config;
using NpcAi.Speech.Segmentation;
using NpcAi.Speech.Threading;
using NUnit.Framework;

namespace NpcAi.Speech.Tests
{
    /// <summary>
    /// tasks.md 3.9/3.10: cableado real de <see cref="OfflineSpeechToText"/> — segmentacion
    /// -> motor -> bomba -> emision (Decision 3/4 de design.md) — con
    /// <see cref="FakeRecognitionEngine"/> en lugar de Vosk. RED: escrito antes del cableado
    /// de la tarea 3.8; no compila hasta que <c>OfflineSpeechToText</c> tenga el constructor
    /// cableado y <c>AlimentarBloqueDeAudio</c>.
    /// </summary>
    public class OfflineSpeechToTextWiringTests
    {
        [Test]
        public void PulsarParaHablar_emite_exactamente_un_Utterance_por_ventana()
        {
            var motor = new FakeRecognitionEngine { ResultadoAlFinalizar = new RecognitionResult("hola mundo", 0.9f, 16000) };
            var config = ConfigDePrueba(TriggerStrategy.PulsarParaHablar);
            var subject = new OfflineSpeechToText(config, motor);

            var recibidas = new List<Utterance>();
            subject.OnUtterance += u => recibidas.Add(u);

            subject.StartListening();
            subject.AlimentarBloqueDeAudio(new float[160], 160);
            subject.StopListening();

            Assert.AreEqual(1, recibidas.Count);
            Assert.AreEqual("hola mundo", recibidas[0].Text);
            Assert.AreEqual(1, motor.VecesFinalizado);
        }

        [Test]
        public void PulsarParaHablar_no_corta_por_silencio_solo_al_llegar_a_StopListening()
        {
            var motor = new FakeRecognitionEngine { ResultadoAlFinalizar = new RecognitionResult("frase", 0.7f, 0) };
            var config = ConfigDePrueba(TriggerStrategy.PulsarParaHablar);
            var subject = new OfflineSpeechToText(config, motor);
            var recibidas = 0;
            subject.OnUtterance += _ => recibidas++;

            subject.StartListening();
            var silencio = new float[160]; // todo ceros: PulsarParaHablar nunca corta por esto
            for (var i = 0; i < 5; i++) subject.AlimentarBloqueDeAudio(silencio, silencio.Length);

            Assert.AreEqual(0, recibidas); // aun no llega StopListening
            Assert.AreEqual(0, motor.VecesFinalizado);

            subject.StopListening();
            Assert.AreEqual(1, recibidas);
        }

        [Test]
        public void ActividadDeVoz_emite_un_Utterance_por_corte_de_silencio_via_QueuedMainThreadPump()
        {
            var motor = new FakeRecognitionEngine { ResultadoAlFinalizar = new RecognitionResult("frase con voz", 0.8f, 4800) };
            var config = ConfigDePrueba(TriggerStrategy.ActividadDeVoz);
            var bomba = new QueuedMainThreadPump();
            var subject = new OfflineSpeechToText(config, motor, bomba);

            var recibidas = new List<Utterance>();
            subject.OnUtterance += u => recibidas.Add(u);

            subject.StartListening();

            var bloqueDeVoz = ConstruirBloque(0.2f, 4800);      // 300 ms de voz a 16 kHz
            var bloqueDeSilencio = ConstruirBloque(0f, 12800);  // 800 ms de silencio a 16 kHz

            subject.AlimentarBloqueDeAudio(bloqueDeVoz, bloqueDeVoz.Length);
            subject.AlimentarBloqueDeAudio(bloqueDeSilencio, bloqueDeSilencio.Length);

            Assert.AreEqual(0, recibidas.Count); // encolado en la bomba, aun no drenado
            bomba.Drenar();

            Assert.AreEqual(1, recibidas.Count);
            Assert.AreEqual("frase con voz", recibidas[0].Text);
        }

        private static SpeechSettings ConfigDePrueba(TriggerStrategy estrategia)
        {
            return new SpeechSettings
            {
                Estrategia = estrategia,
                TasaDeMuestreo = 16000,
                UmbralDeEnergia = 0.02f,
                MsMinimosDeVoz = 200,
                MsDeSilencioParaCortar = 700,
                MaxSegundosPorFrase = 15f
            };
        }

        private static float[] ConstruirBloque(float amplitud, int cantidad)
        {
            var bloque = new float[cantidad];
            for (var i = 0; i < cantidad; i++) bloque[i] = amplitud;
            return bloque;
        }
    }
}
