using NpcAi.Speech.Segmentation;
using NUnit.Framework;

namespace NpcAi.Speech.Tests
{
    /// <summary>
    /// Requisito "Estrategia de disparo configurable por escenario", escenario
    /// PulsarParaHablar: la ventana ES la frase, asi que <see cref="PushToTalkStrategy"/>
    /// nunca corta por silencio y solo respeta el tope duro (tasks.md 2.3).
    /// </summary>
    public class PushToTalkStrategyTests
    {
        [Test]
        public void Continua_mientras_no_llegue_al_maximo()
        {
            var strategy = new PushToTalkStrategy(maxSegundosPorFrase: 15f);
            var muestras = new float[160];

            for (var segundo = 0; segundo < 15; segundo++)
            {
                var decision = strategy.Evaluar(muestras, muestras.Length, segundosEnFrase: segundo);
                Assert.AreEqual(SegmentDecision.Continuar, decision);
            }
        }

        [Test]
        public void Cierra_la_frase_al_alcanzar_el_maximo()
        {
            var strategy = new PushToTalkStrategy(maxSegundosPorFrase: 15f);
            var muestras = new float[160];

            var decision = strategy.Evaluar(muestras, muestras.Length, segundosEnFrase: 15d);

            Assert.AreEqual(SegmentDecision.CerrarFrase, decision);
        }

        [Test]
        public void Nunca_descarta_muestras()
        {
            var strategy = new PushToTalkStrategy(maxSegundosPorFrase: 15f);
            var muestras = new float[160];

            for (var segundo = 0d; segundo < 20d; segundo += 0.5d)
            {
                var decision = strategy.Evaluar(muestras, muestras.Length, segundosEnFrase: segundo);
                Assert.AreNotEqual(SegmentDecision.Descartar, decision);
            }
        }
    }
}
