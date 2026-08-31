using NpcAi.Core;
using NpcAi.Core.Tests;
using NpcAi.Receptivity.Fakes;
using NUnit.Framework;

namespace NpcAi.Receptivity.Tests
{
    public class ScriptedReceptivityEngineTests : ReceptivityEngineContract
    {
        protected override IReceptivityEngine CreateSubject() => new ScriptedReceptivityEngine();

        // --- Tabla de transiciones del doble ---

        [Test]
        public void Dos_gestos_empaticos_llevan_a_receptivo()
        {
            var e = new ScriptedReceptivityEngine();
            e.Reset(new PersonalityId("empatico"));

            e.Evaluate(new IntentResult(Intent.Empatia, Tone.Empatico, 1f, 0f), PhysicalAction.Ninguna);
            var cambio = e.Evaluate(new IntentResult(Intent.Empatia, Tone.Empatico, 1f, 0f), PhysicalAction.Ninguna);

            Assert.AreEqual(Core.Receptivity.Receptivo, cambio.To);
            Assert.AreEqual(Core.Receptivity.Receptivo, e.Current);
        }

        [Test]
        public void Una_agresion_directa_deja_la_razon_correcta()
        {
            var e = new ScriptedReceptivityEngine();
            e.Reset(new PersonalityId("grosero"));

            var cambio = e.Evaluate(new IntentResult(Intent.SolicitudAgresiva, Tone.Agresivo, 1f, 0f),
                                    PhysicalAction.Ninguna);

            Assert.AreEqual("AGRESION_DIRECTA", cambio.ReasonCode);
        }
    }
}
