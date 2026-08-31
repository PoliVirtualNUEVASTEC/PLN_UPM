using NpcAi.Core;
using NpcAi.Core.Tests;
using NpcAi.VrInput.Fakes;

namespace NpcAi.VrInput.Tests
{
    public class ScriptedPhysicalActionSourceTests : PhysicalActionSourceContract
    {
        protected override IPhysicalActionSource CreateSubject() => new ScriptedPhysicalActionSource();

        protected override bool EmitTestAction(IPhysicalActionSource subject, PhysicalAction action)
            => ((ScriptedPhysicalActionSource)subject).Emit(action);
    }
}
