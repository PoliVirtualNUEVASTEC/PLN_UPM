using NpcAi.Core;
using NpcAi.Core.Tests;
using NpcAi.Dialogue.Fakes;

namespace NpcAi.Dialogue.Tests
{
    public class ScriptedDialogueGeneratorTests : DialogueGeneratorContract
    {
        protected override IDialogueGenerator CreateSubject() => new ScriptedDialogueGenerator();
    }
}
