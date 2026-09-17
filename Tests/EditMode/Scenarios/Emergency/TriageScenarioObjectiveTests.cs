using NpcAi.Core;
using NpcAi.Core.Tests;

namespace NpcAi.Scenarios.Emergency.Tests
{
    /// <summary>
    /// Conformidad de <see cref="TriageScenarioObjective"/> con
    /// <see cref="ScenarioObjectiveContract"/> (7 pruebas heredadas, sin modificar la clase
    /// base). <c>CreateSubject()</c> usa el constructor sin parametros: sin caso asignado,
    /// <c>HasKey</c> es siempre falso y la renormalizacion (requisito "Renormalizacion de
    /// pesos sin caso asignado") deja la receptividad sostenida como el 100% del progreso,
    /// que es lo unico que este contrato ejercita via <c>Notify(ReceptivityChange)</c>.
    /// </summary>
    public class TriageScenarioObjectiveTests : ScenarioObjectiveContract
    {
        protected override IScenarioObjective CreateSubject() => new TriageScenarioObjective();
    }
}
