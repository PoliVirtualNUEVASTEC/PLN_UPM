using NpcAi.Core;
using NpcAi.Core.Tests;
using NpcAi.Speech;

namespace NpcAi.Speech.Tests
{
    /// <summary>
    /// Gemela del contrato para la implementacion real de M1 (PR1: solo el nucleo del
    /// adaptador, sin motor ni microfono). Debe pasar exactamente las mismas pruebas que
    /// <see cref="ScriptedSpeechToTextTests"/>: es el mecanismo que impide que la
    /// implementacion real mienta sobre <see cref="ISpeechToText"/>.
    /// </summary>
    public class OfflineSpeechToTextTests : SpeechToTextContract
    {
        protected override ISpeechToText CreateSubject() => new OfflineSpeechToText();

        protected override bool EmitTestUtterance(ISpeechToText subject, Utterance utterance)
            => ((OfflineSpeechToText)subject).EmitirParaPrueba(utterance);
    }
}
