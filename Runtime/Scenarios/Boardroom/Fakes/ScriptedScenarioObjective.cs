using NpcAi.Core;

namespace NpcAi.Scenarios.Boardroom.Fakes
{
    /// <summary>
    /// Doble determinista de M10 — Sala de Juntas (levantamiento de requerimientos).
    /// Avanza un paso fijo por cada mejora de receptividad y retrocede por cada
    /// empeoramiento. El objetivo real define condiciones propias del escenario.
    /// </summary>
    public sealed class ScriptedScenarioObjective : IScenarioObjective
    {
        private const int PasosParaCompletar = 4;

        private int _pasos;

        public float Progress01 => (float)_pasos / PasosParaCompletar;

        public bool IsComplete => _pasos >= PasosParaCompletar;

        public void Notify(ReceptivityChange change)
        {
            if (change.Improved) _pasos++;
            else if (change.Worsened) _pasos--;

            if (_pasos < 0) _pasos = 0;
            if (_pasos > PasosParaCompletar) _pasos = PasosParaCompletar;
        }

        public void Reset() => _pasos = 0;
    }
}
