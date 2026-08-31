using NpcAi.Core;
using NpcAi.Core.Tests;
using NpcAi.Speech.Fakes;

namespace NpcAi.Speech.Tests
{
    /// <summary>
    /// El doble de M1 pasa exactamente el mismo contrato que pasara la implementacion
    /// real. Cuando M1 exista, se agrega una clase gemela a esta que devuelva la
    /// implementacion real en CreateSubject, y nada mas.
    /// </summary>
    public class ScriptedSpeechToTextTests : SpeechToTextContract
    {
        protected override ISpeechToText CreateSubject() => new ScriptedSpeechToText();

        protected override bool EmitTestUtterance(ISpeechToText subject, Utterance utterance)
            => ((ScriptedSpeechToText)subject).Emit(utterance);
    }
}
