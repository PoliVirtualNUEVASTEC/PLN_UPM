using NpcAi.Core;

namespace NpcAi.RequirementResponse
{
    /// <summary>
    /// La puerta de receptividad: decide solo entre <see cref="RequirementOutcome.Revelado"/> y
    /// <see cref="RequirementOutcome.AunNoRevelado"/> (AD4 de <c>design.md</c>). Pieza propia de
    /// M16 que M15 no tiene: M15 responde o no responde, M16 responde, desvia o no aplica.
    /// </summary>
    public static class RequirementDisclosurePolicy
    {
        /// <summary>
        /// Aprovecha que <see cref="Receptivity"/> esta numerado -1/0/1 a proposito (ver el
        /// comentario del enum en <c>Runtime/Core/Enums.cs</c>): el orden es comparable, asi que
        /// la puerta es una comparacion aritmetica y no una tabla. Un umbral
        /// <c>NoReceptivo</c> revela con los tres valores. NUNCA devuelve
        /// <see cref="RequirementOutcome.NoAplica"/>: eso es ausencia de match o de caso, y lo
        /// decide el adaptador.
        /// </summary>
        public static RequirementOutcome Decidir(Receptivity actual, Receptivity minima) =>
            (int)actual >= (int)minima
                ? RequirementOutcome.Revelado
                : RequirementOutcome.AunNoRevelado;
    }
}
