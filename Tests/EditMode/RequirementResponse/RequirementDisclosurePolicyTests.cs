using NpcAi.Core;
using NUnit.Framework;

namespace NpcAi.RequirementResponse.Tests
{
    /// <summary>
    /// La puerta de receptividad (AD4 de <c>design.md</c>): matriz completa 3 x 3,
    /// <c>receptividadMinima</c> del requerimiento x <c>Receptivity</c> actual. Seis
    /// combinaciones revelan, tres difieren y ninguna devuelve <c>NoAplica</c>: "no aplica" es
    /// ausencia de match o de caso, responsabilidad del adaptador y no de la puerta.
    /// </summary>
    public class RequirementDisclosurePolicyTests
    {
        private static readonly Receptivity[] Niveles =
            { Receptivity.NoReceptivo, Receptivity.Neutral, Receptivity.Receptivo };

        // Umbral NoReceptivo: la puerta esta abierta siempre, incluso con receptividad negativa
        // (escenario "Un requerimiento trivial se revela incluso con receptividad negativa").
        [TestCase(Receptivity.NoReceptivo, Receptivity.NoReceptivo, RequirementOutcome.Revelado)]
        [TestCase(Receptivity.NoReceptivo, Receptivity.Neutral,     RequirementOutcome.Revelado)]
        [TestCase(Receptivity.NoReceptivo, Receptivity.Receptivo,   RequirementOutcome.Revelado)]
        // Umbral Neutral: solo la receptividad negativa difiere. La igualdad revela (>=, no >).
        [TestCase(Receptivity.Neutral,     Receptivity.NoReceptivo, RequirementOutcome.AunNoRevelado)]
        [TestCase(Receptivity.Neutral,     Receptivity.Neutral,     RequirementOutcome.Revelado)]
        [TestCase(Receptivity.Neutral,     Receptivity.Receptivo,   RequirementOutcome.Revelado)]
        // Umbral Receptivo: solo la receptividad maxima revela (escenario "Bajo umbral desvia").
        [TestCase(Receptivity.Receptivo,   Receptivity.NoReceptivo, RequirementOutcome.AunNoRevelado)]
        [TestCase(Receptivity.Receptivo,   Receptivity.Neutral,     RequirementOutcome.AunNoRevelado)]
        [TestCase(Receptivity.Receptivo,   Receptivity.Receptivo,   RequirementOutcome.Revelado)]
        public void Decidir_compara_la_receptividad_actual_contra_el_umbral(
            Receptivity minima, Receptivity actual, RequirementOutcome esperado)
        {
            Assert.AreEqual(esperado, RequirementDisclosurePolicy.Decidir(actual, minima));
        }

        [Test]
        public void La_matriz_completa_da_6_Revelado_3_AunNoRevelado_y_ningun_NoAplica()
        {
            int reveladas = 0, diferidas = 0, noAplica = 0;

            foreach (var minima in Niveles)
            {
                foreach (var actual in Niveles)
                {
                    var resultado = RequirementDisclosurePolicy.Decidir(actual, minima);

                    if (resultado == RequirementOutcome.Revelado) reveladas++;
                    else if (resultado == RequirementOutcome.AunNoRevelado) diferidas++;
                    else noAplica++;
                }
            }

            Assert.AreEqual(6, reveladas);
            Assert.AreEqual(3, diferidas);
            Assert.AreEqual(0, noAplica, "La puerta NUNCA devuelve NoAplica (AD4)");
        }
    }
}
