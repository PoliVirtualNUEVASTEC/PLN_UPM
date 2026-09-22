using NpcAi.Core;
using NpcAi.Scenarios.Boardroom.Fakes;
using NUnit.Framework;

namespace NpcAi.Scenarios.Boardroom.Tests
{
    /// <summary>
    /// Paridad de <c>Reset()</c> entre <see cref="RequirementsScenarioObjective"/> (real) y
    /// <see cref="ScriptedScenarioObjective"/> (doble): ambos DEBEN volver al estado recien
    /// construido, ser idempotentes, y dejar <c>AssignCase</c> funcionando otra vez despues
    /// (spec.md, "Reset() real, idempotente y en paridad real/doble").
    /// </summary>
    public class ResetParityTests
    {
        private const string JsonCaso = @"{
            ""id"": ""caso-reset"",
            ""requerimientos"": [ { ""id"": ""uno"" }, { ""id"": ""dos"" } ]
        }";

        private static string CargarJsonDeCaso(RequirementCaseId id) =>
            id.Value == "caso-reset" ? JsonCaso : null;

        private static RequirementsScenarioObjective NuevoReal() =>
            new RequirementsScenarioObjective(new BoardroomObjectiveSettings(), CargarJsonDeCaso);

        private static ReceptivityChange Mejora() =>
            new ReceptivityChange(Receptivity.Neutral, Receptivity.Receptivo, 2, "GESTO_EMPATICO");

        [Test]
        public void Reset_del_real_vuelve_al_estado_recien_construido_con_progreso_parcial_previo()
        {
            var o = NuevoReal();
            o.AssignCase(new RequirementCaseId("caso-reset"));
            o.RegisterDisclosure(new Core.RequirementResponse(
                RequirementOutcome.Revelado, default, new RequirementId("uno")));
            o.PresentSummary(new[] { new RequirementId("uno") });
            Assert.Greater(o.Progress01, 0f, "precondicion: hay progreso antes del Reset");

            o.Reset();

            Assert.IsFalse(o.HasCase);
            Assert.AreEqual(0, o.RequirementCount);
            Assert.AreEqual(0, o.RequirementsDisclosed);
            Assert.IsFalse(o.SummaryIsFaithful);
            Assert.AreEqual(0f, o.Progress01);
            Assert.IsFalse(o.IsComplete);
        }

        [Test]
        public void Reset_del_doble_vuelve_al_estado_recien_construido_con_progreso_parcial_previo()
        {
            var o = new ScriptedScenarioObjective();
            o.AssignCase(new RequirementCaseId("caso-juntas-01"));
            o.RegisterDisclosure(new Core.RequirementResponse(
                RequirementOutcome.Revelado, default, new RequirementId("equipos")));
            o.PresentSummary(new[] { new RequirementId("equipos") });
            Assert.Greater(o.Progress01, 0f, "precondicion: hay progreso antes del Reset");

            o.Reset();

            Assert.IsFalse(o.HasCase);
            Assert.AreEqual(0, o.RequirementCount);
            Assert.AreEqual(0, o.RequirementsDisclosed);
            Assert.IsFalse(o.SummaryIsFaithful);
            Assert.AreEqual(0f, o.Progress01);
            Assert.IsFalse(o.IsComplete);
        }

        [Test]
        public void Reset_es_idempotente_en_el_real()
        {
            var o = NuevoReal();
            o.AssignCase(new RequirementCaseId("caso-reset"));
            o.Notify(Mejora());

            o.Reset();
            o.Reset();

            Assert.IsFalse(o.HasCase);
            Assert.AreEqual(0f, o.Progress01);
        }

        [Test]
        public void Reset_es_idempotente_en_el_doble()
        {
            var o = new ScriptedScenarioObjective();
            o.AssignCase(new RequirementCaseId("caso-juntas-01"));
            o.Notify(Mejora());

            o.Reset();
            o.Reset();

            Assert.IsFalse(o.HasCase);
            Assert.AreEqual(0f, o.Progress01);
        }

        [Test]
        public void AssignCase_vuelve_a_funcionar_despues_de_Reset_en_el_real()
        {
            var o = NuevoReal();
            o.AssignCase(new RequirementCaseId("caso-reset"));
            o.Reset();

            o.AssignCase(new RequirementCaseId("caso-reset"));

            Assert.IsTrue(o.HasCase);
            Assert.AreEqual(2, o.RequirementCount);
        }

        [Test]
        public void AssignCase_vuelve_a_funcionar_despues_de_Reset_en_el_doble()
        {
            var o = new ScriptedScenarioObjective();
            o.AssignCase(new RequirementCaseId("caso-juntas-01"));
            o.Reset();

            o.AssignCase(new RequirementCaseId("caso-juntas-01"));

            Assert.IsTrue(o.HasCase);
            Assert.AreEqual(6, o.RequirementCount);
        }

        [Test]
        public void Real_y_doble_convergen_al_mismo_estado_observable_tras_Reset()
        {
            var real = NuevoReal();
            real.AssignCase(new RequirementCaseId("caso-reset"));
            real.Notify(Mejora());
            real.Reset();

            var doble = new ScriptedScenarioObjective();
            doble.AssignCase(new RequirementCaseId("caso-juntas-01"));
            doble.Notify(Mejora());
            doble.Reset();

            Assert.AreEqual(real.HasCase, doble.HasCase);
            Assert.AreEqual(real.Progress01, doble.Progress01);
            Assert.AreEqual(real.IsComplete, doble.IsComplete);
        }
    }
}
