using NpcAi.Core;
using NpcAi.Core.Tests;

namespace NpcAi.Scenarios.Boardroom.Tests
{
    /// <summary>
    /// Conformidad de <see cref="RequirementsScenarioObjective"/> con
    /// <see cref="ScenarioObjectiveContract"/> (7 pruebas heredadas, sin modificar la clase
    /// base). <c>CreateSubject()</c> usa el constructor sin parametros: sin caso asignado,
    /// <c>HasCase</c> es siempre falso y la renormalizacion (design.md, "Aritmetica del
    /// progreso") deja el trato sostenido como el 100% del progreso, que es lo unico que
    /// este contrato ejercita via <c>Notify(ReceptivityChange)</c>.
    /// </summary>
    public class RequirementsScenarioObjectiveTests : ScenarioObjectiveContract
    {
        protected override IScenarioObjective CreateSubject() => new RequirementsScenarioObjective();
    }
}
