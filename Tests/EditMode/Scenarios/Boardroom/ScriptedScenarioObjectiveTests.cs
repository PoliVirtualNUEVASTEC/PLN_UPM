using NpcAi.Core;
using NpcAi.Core.Tests;
using NpcAi.Scenarios.Boardroom.Fakes;

namespace NpcAi.Scenarios.Boardroom.Tests
{
    public class ScriptedScenarioObjectiveTests : ScenarioObjectiveContract
    {
        protected override IScenarioObjective CreateSubject() => new ScriptedScenarioObjective();
    }
}
