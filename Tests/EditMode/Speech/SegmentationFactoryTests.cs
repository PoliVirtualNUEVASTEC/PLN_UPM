using System;
using NpcAi.Speech.Segmentation;
using NUnit.Framework;

namespace NpcAi.Speech.Tests
{
    /// <summary>
    /// Criterio de exito 3 ("cambiar de estrategia no debe requerir recompilar", tasks.md
    /// 2.7): <see cref="SegmentationFactory.Crear"/> selecciona la implementacion por el
    /// valor de <see cref="TriggerStrategy"/> que recibe en tiempo de ejecucion, no por una
    /// rama que haya que tocar en codigo para agregar un escenario nuevo.
    /// </summary>
    public class SegmentationFactoryTests
    {
        [TestCase(TriggerStrategy.PulsarParaHablar, typeof(PushToTalkStrategy))]
        [TestCase(TriggerStrategy.ActividadDeVoz, typeof(VoiceActivityStrategy))]
        public void Crear_selecciona_la_estrategia_segun_el_dato_sin_recompilar(TriggerStrategy estrategia, Type tipoEsperado)
        {
            var strategy = SegmentationFactory.Crear(
                estrategia,
                umbralDeEnergia: 0.02f,
                msMinimosDeVoz: 200,
                msDeSilencioParaCortar: 700,
                maxSegundosPorFrase: 15f);

            Assert.IsInstanceOf(tipoEsperado, strategy);
        }

        [Test]
        public void Crear_lanza_para_un_valor_de_estrategia_desconocido()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                SegmentationFactory.Crear((TriggerStrategy)99, 0.02f, 200, 700, 15f));
        }
    }
}
