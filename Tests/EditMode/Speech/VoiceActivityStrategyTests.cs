using NpcAi.Speech.Segmentation;
using NUnit.Framework;

namespace NpcAi.Speech.Tests
{
    /// <summary>
    /// Requisito "Estrategia de disparo configurable por escenario", escenario
    /// ActividadDeVoz (tasks.md 2.5): voz seguida de silencio cierra una sola vez; solo
    /// silencio nunca cierra; <c>MaxSegundosPorFrase</c> fuerza el cierre aunque haya voz.
    /// </summary>
    public class VoiceActivityStrategyTests
    {
        private const double DuracionDelBloqueSegundos = 0.02d; // 20 ms, simulando Update()

        [Test]
        public void Voz_seguida_de_silencio_cierra_la_frase_una_sola_vez()
        {
            var strategy = new VoiceActivityStrategy(
                umbralDeEnergia: 0.02f,
                msMinimosDeVoz: 200,
                msDeSilencioParaCortar: 700,
                maxSegundosPorFrase: 15f);
            strategy.AbrirVentana();

            var bloqueDeVoz      = LlenarConAmplitud(0.2f, 32);
            var bloqueDeSilencio = LlenarConAmplitud(0f, 32);
            var segundosEnFrase  = 0d;
            var cierres          = 0;

            // 300 ms de voz (15 bloques de 20 ms)
            for (var i = 0; i < 15; i++)
            {
                segundosEnFrase += DuracionDelBloqueSegundos;
                if (strategy.Evaluar(bloqueDeVoz, bloqueDeVoz.Length, segundosEnFrase) == SegmentDecision.CerrarFrase)
                    cierres++;
            }

            // 800 ms de silencio (40 bloques de 20 ms)
            for (var i = 0; i < 40; i++)
            {
                segundosEnFrase += DuracionDelBloqueSegundos;
                if (strategy.Evaluar(bloqueDeSilencio, bloqueDeSilencio.Length, segundosEnFrase) == SegmentDecision.CerrarFrase)
                    cierres++;
            }

            Assert.AreEqual(1, cierres);
        }

        [Test]
        public void Solo_silencio_nunca_cierra_la_frase()
        {
            var strategy = new VoiceActivityStrategy(
                umbralDeEnergia: 0.02f,
                msMinimosDeVoz: 200,
                msDeSilencioParaCortar: 700,
                maxSegundosPorFrase: 15f);
            strategy.AbrirVentana();

            var bloqueDeSilencio = LlenarConAmplitud(0f, 32);
            var segundosEnFrase  = 0d;
            var huboCierre       = false;

            for (var i = 0; i < 100; i++) // 2 s continuos de silencio puro
            {
                segundosEnFrase += DuracionDelBloqueSegundos;
                if (strategy.Evaluar(bloqueDeSilencio, bloqueDeSilencio.Length, segundosEnFrase) == SegmentDecision.CerrarFrase)
                    huboCierre = true;
            }

            Assert.IsFalse(huboCierre);
        }

        [Test]
        public void MaxSegundosPorFrase_fuerza_el_cierre_aunque_siga_habiendo_voz()
        {
            var strategy = new VoiceActivityStrategy(
                umbralDeEnergia: 0.02f,
                msMinimosDeVoz: 200,
                msDeSilencioParaCortar: 700,
                maxSegundosPorFrase: 1f);
            strategy.AbrirVentana();

            var bloqueDeVoz     = LlenarConAmplitud(0.2f, 32);
            var segundosEnFrase = 0d;
            var ultimaDecision  = SegmentDecision.Continuar;

            for (var i = 0; i < 60; i++) // 1.2 s continuos de voz, sin silencio
            {
                segundosEnFrase += DuracionDelBloqueSegundos;
                ultimaDecision = strategy.Evaluar(bloqueDeVoz, bloqueDeVoz.Length, segundosEnFrase);
                if (ultimaDecision == SegmentDecision.CerrarFrase) break;
            }

            Assert.AreEqual(SegmentDecision.CerrarFrase, ultimaDecision);
        }

        private static float[] LlenarConAmplitud(float amplitud, int cantidad)
        {
            var muestras = new float[cantidad];
            for (var i = 0; i < cantidad; i++) muestras[i] = amplitud;
            return muestras;
        }
    }
}
