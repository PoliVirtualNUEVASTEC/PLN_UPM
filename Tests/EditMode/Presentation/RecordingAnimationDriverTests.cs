using NpcAi.Presentation.Fakes;
using NUnit.Framework;

namespace NpcAi.Presentation.Tests
{
    public class RecordingAnimationDriverTests
    {
        [Test]
        public void Registra_cada_llamada_en_orden()
        {
            var d = new RecordingAnimationDriver();

            d.Aplicar("molesto", "cruzar_brazos");
            d.Aplicar("neutral", "idle");

            Assert.AreEqual(2, d.Aplicadas.Count);
            Assert.AreEqual("molesto", d.Aplicadas[0].emotionTag);
            Assert.AreEqual("cruzar_brazos", d.Aplicadas[0].animationCue);
            Assert.AreEqual("neutral", d.Aplicadas[1].emotionTag);
            Assert.AreEqual("idle", d.Aplicadas[1].animationCue);
        }

        [Test]
        public void Tolera_tags_nulos_sin_lanzar()
        {
            var d = new RecordingAnimationDriver();

            Assert.DoesNotThrow(() => d.Aplicar(null, null));
            Assert.AreEqual(1, d.Aplicadas.Count);
        }
    }
}
