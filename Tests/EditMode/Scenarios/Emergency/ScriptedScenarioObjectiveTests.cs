using NpcAi.Core;
using NpcAi.Core.Tests;
using NpcAi.Scenarios.Emergency.Fakes;

namespace NpcAi.Scenarios.Emergency.Tests
{
    public class ScriptedScenarioObjectiveTests : ScenarioObjectiveContract
    {
        protected override IScenarioObjective CreateSubject() => new ScriptedScenarioObjective();
    }
}
