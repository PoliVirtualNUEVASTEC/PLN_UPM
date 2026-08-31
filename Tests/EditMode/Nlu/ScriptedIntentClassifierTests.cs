using NpcAi.Core;
using NpcAi.Core.Tests;
using NpcAi.Nlu.Fakes;
using NUnit.Framework;

namespace NpcAi.Nlu.Tests
{
    public class ScriptedIntentClassifierTests : IntentClassifierContract
    {
        protected override IIntentClassifier CreateSubject() => new ScriptedIntentClassifier();

        // --- Pruebas propias del doble, ademas del contrato ---

        [Test]
        public void Reconoce_una_solicitud_respetuosa()
        {
            var r = new ScriptedIntentClassifier().Classify("Por favor, necesito ayuda");
            Assert.AreEqual(Intent.SolicitudRespetuosa, r.Intent);
        }

        [Test]
        public void Reconoce_una_solicitud_agresiva()
        {
            var r = new ScriptedIntentClassifier().Classify("Hagalo rapido");
            Assert.AreEqual(Intent.SolicitudAgresiva, r.Intent);
            Assert.AreEqual(Tone.Agresivo, r.Tone);
        }
    }
}
