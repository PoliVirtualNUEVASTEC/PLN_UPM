using System;
using NpcAi.Core;
using NUnit.Framework;

namespace NpcAi.Scenarios.Boardroom.Tests
{
    /// <summary>
    /// Superficie aditiva de <see cref="RequirementsScenarioObjective"/>
    /// (<c>AssignCase</c>/<c>RegisterDisclosure</c>/<c>PresentSummary</c>) y los casos borde
    /// del constructor rico: <c>cargarJson</c> nulo/que lanza/JSON basura/checklist vacio
    /// (AD4), reasignacion de caso (resiembra el trato, AD1), filtrado y idempotencia de
    /// <c>RegisterDisclosure</c> (AD5), y la fidelidad recalculada de <c>PresentSummary</c>
    /// (AD8/AD9). Espejo estructural de <c>TriageSuperficieAditivaTests</c> (M9).
    /// </summary>
    public class RequirementsSuperficieAditivaTests
    {
        private const string JsonCasoUno = @"{
            ""id"": ""caso-1"",
            ""requerimientos"": [
                { ""id"": ""presupuesto"" },
                { ""id"": ""plazos"" },
                { ""id"": ""alcance"" }
            ]
        }";

        private const string JsonCasoDos = @"{
            ""id"": ""caso-2"",
            ""requerimientos"": [
                { ""id"": ""equipo"" },
                { ""id"": ""riesgos"" }
            ]
        }";

        private const string JsonSinRequerimientos = @"{ ""id"": ""caso-vacio"", ""requerimientos"": [] }";

        private static string CargarJsonDePrueba(RequirementCaseId id)
        {
            if (id.Value == "caso-1") return JsonCasoUno;
            if (id.Value == "caso-2") return JsonCasoDos;
            if (id.Value == "caso-vacio") return JsonSinRequerimientos;
            return null;
        }

        private static RequirementsScenarioObjective NuevoConCasoUno()
        {
            var o = new RequirementsScenarioObjective(new BoardroomObjectiveSettings(), CargarJsonDePrueba);
            o.AssignCase(new RequirementCaseId("caso-1"));
            return o;
        }

        private static ReceptivityChange Empeora() =>
            new ReceptivityChange(Receptivity.Neutral, Receptivity.NoReceptivo, -2, "AGRESION_DIRECTA");

        private static Core.RequirementResponse Revelado(RequirementId id) =>
            new Core.RequirementResponse(RequirementOutcome.Revelado, default, id);

        private static Core.RequirementResponse AunNoRevelado(RequirementId id) =>
            new Core.RequirementResponse(RequirementOutcome.AunNoRevelado, default, id);

        // --- AssignCase: carga fallida nunca lanza ---

        [Test]
        public void AssignCase_con_id_desconocido_deja_HasCase_falso_sin_lanzar()
        {
            var o = new RequirementsScenarioObjective(new BoardroomObjectiveSettings(), CargarJsonDePrueba);

            Assert.DoesNotThrow(() => o.AssignCase(new RequirementCaseId("caso-inexistente")));

            Assert.IsFalse(o.HasCase);
        }

        [Test]
        public void AssignCase_con_cargador_null_deja_HasCase_falso_sin_lanzar()
        {
            var o = new RequirementsScenarioObjective(new BoardroomObjectiveSettings(), null);

            Assert.DoesNotThrow(() => o.AssignCase(new RequirementCaseId("caso-1")));

            Assert.IsFalse(o.HasCase);
        }

        [Test]
        public void AssignCase_con_funcion_que_lanza_deja_HasCase_falso_sin_propagar_la_excepcion()
        {
            string CargarQueLanza(RequirementCaseId id) => throw new InvalidOperationException("boom");
            var o = new RequirementsScenarioObjective(new BoardroomObjectiveSettings(), CargarQueLanza);

            Assert.DoesNotThrow(() => o.AssignCase(new RequirementCaseId("caso-1")));

            Assert.IsFalse(o.HasCase);
        }

        [Test]
        public void AssignCase_con_json_basura_deja_HasCase_falso_sin_lanzar()
        {
            string CargarBasura(RequirementCaseId id) => "esto no es json";
            var o = new RequirementsScenarioObjective(new BoardroomObjectiveSettings(), CargarBasura);

            Assert.DoesNotThrow(() => o.AssignCase(new RequirementCaseId("caso-1")));

            Assert.IsFalse(o.HasCase);
        }

        [Test]
        public void AssignCase_con_RequirementCaseId_None_deja_HasCase_falso()
        {
            var o = new RequirementsScenarioObjective(new BoardroomObjectiveSettings(), CargarJsonDePrueba);

            Assert.DoesNotThrow(() => o.AssignCase(RequirementCaseId.None));

            Assert.IsFalse(o.HasCase);
        }

        [Test]
        public void AssignCase_con_checklist_vacio_deja_HasCase_falso_AD4()
        {
            var o = new RequirementsScenarioObjective(new BoardroomObjectiveSettings(), CargarJsonDePrueba);

            Assert.DoesNotThrow(() => o.AssignCase(new RequirementCaseId("caso-vacio")));

            Assert.IsFalse(o.HasCase);
        }

        // --- AssignCase: exito puebla el checklist y siembra el trato ---

        [Test]
        public void AssignCase_exitoso_puebla_HasCase_y_RequirementCount()
        {
            var o = NuevoConCasoUno();

            Assert.IsTrue(o.HasCase);
            Assert.AreEqual(3, o.RequirementCount);
        }

        [Test]
        public void Reasignar_caso_descarta_progreso_previo_y_resiembra_el_trato_AD1()
        {
            var o = NuevoConCasoUno();
            o.RegisterDisclosure(Revelado(new RequirementId("presupuesto")));
            o.PresentSummary(new[] { new RequirementId("presupuesto") });
            o.Notify(Empeora()); // baja el trato de 5 a 4
            Assert.AreEqual(1, o.RequirementsDisclosed, "precondicion");

            o.AssignCase(new RequirementCaseId("caso-2"));

            Assert.IsTrue(o.HasCase);
            Assert.AreEqual(2, o.RequirementCount); // caso-2 tiene 2 requerimientos
            Assert.AreEqual(0, o.RequirementsDisclosed);
            Assert.IsFalse(o.SummaryIsFaithful);
            // solo el trato resembrado lleno cuenta: 0.2*1 + 0.8*(0.75*0 + 0.25*0) = 0.2
            Assert.AreEqual(0.2f, o.Progress01, 1e-4f);
        }

        // --- RegisterDisclosure: filtra, es idempotente, nunca lanza ---

        [Test]
        public void RegisterDisclosure_acredita_Outcome_Revelado_del_caso_asignado()
        {
            var o = NuevoConCasoUno();

            o.RegisterDisclosure(Revelado(new RequirementId("presupuesto")));

            Assert.AreEqual(1, o.RequirementsDisclosed);
        }

        [Test]
        public void RegisterDisclosure_con_Outcome_distinto_de_Revelado_no_acredita()
        {
            var o = NuevoConCasoUno();

            o.RegisterDisclosure(AunNoRevelado(new RequirementId("presupuesto")));
            o.RegisterDisclosure(Core.RequirementResponse.NoAplica);

            Assert.AreEqual(0, o.RequirementsDisclosed);
        }

        [Test]
        public void RegisterDisclosure_con_id_ajeno_al_caso_asignado_no_acredita()
        {
            var o = NuevoConCasoUno();

            o.RegisterDisclosure(Revelado(new RequirementId("id-inexistente")));

            Assert.AreEqual(0, o.RequirementsDisclosed);
        }

        [Test]
        public void RegisterDisclosure_es_idempotente_por_id()
        {
            var o = NuevoConCasoUno();

            o.RegisterDisclosure(Revelado(new RequirementId("presupuesto")));
            o.RegisterDisclosure(Revelado(new RequirementId("presupuesto")));

            Assert.AreEqual(1, o.RequirementsDisclosed);
        }

        [Test]
        public void RegisterDisclosure_antes_de_AssignCase_no_tiene_efecto()
        {
            var o = new RequirementsScenarioObjective(new BoardroomObjectiveSettings(), CargarJsonDePrueba);

            Assert.DoesNotThrow(() => o.RegisterDisclosure(Revelado(new RequirementId("presupuesto"))));

            Assert.AreEqual(0, o.RequirementsDisclosed);
            Assert.IsFalse(o.HasCase);
        }

        [Test]
        public void RegisterDisclosure_con_default_nunca_lanza_y_no_acredita()
        {
            var o = NuevoConCasoUno();

            Assert.DoesNotThrow(() => o.RegisterDisclosure(default));

            Assert.AreEqual(0, o.RequirementsDisclosed);
        }

        // --- PresentSummary: coincidencia exacta acredita, nunca lanza ---

        [Test]
        public void PresentSummary_con_coincidencia_exacta_acredita_aun_con_cobertura_parcial()
        {
            var o = NuevoConCasoUno(); // 3 requerimientos: presupuesto, plazos, alcance
            o.RegisterDisclosure(Revelado(new RequirementId("presupuesto")));

            o.PresentSummary(new[] { new RequirementId("presupuesto") });

            Assert.IsTrue(o.SummaryIsFaithful);
            // c = 1/3, k = 1, t = 1 (trato sembrado lleno) => 0.2*1 + 0.8*(0.75*(1/3) + 0.25*1)
            var esperado = 0.2f + 0.8f * (0.75f * (1f / 3f) + 0.25f);
            Assert.AreEqual(esperado, o.Progress01, 1e-4f);
        }

        [Test]
        public void PresentSummary_al_que_le_falta_un_id_revelado_no_acredita()
        {
            var o = NuevoConCasoUno();
            o.RegisterDisclosure(Revelado(new RequirementId("presupuesto")));
            o.RegisterDisclosure(Revelado(new RequirementId("plazos")));

            o.PresentSummary(new[] { new RequirementId("presupuesto") }); // falta "plazos"

            Assert.IsFalse(o.SummaryIsFaithful);
        }

        [Test]
        public void PresentSummary_con_un_id_que_el_cliente_nunca_revelo_no_acredita()
        {
            var o = NuevoConCasoUno();
            o.RegisterDisclosure(Revelado(new RequirementId("presupuesto")));

            // "plazos" nunca se revelo: sobra en el resumen presentado
            o.PresentSummary(new[] { new RequirementId("presupuesto"), new RequirementId("plazos") });

            Assert.IsFalse(o.SummaryIsFaithful);
        }

        [Test]
        public void PresentSummary_es_sobrescribible_gana_la_ultima_llamada()
        {
            var o = NuevoConCasoUno();
            o.RegisterDisclosure(Revelado(new RequirementId("presupuesto")));
            o.PresentSummary(new[] { new RequirementId("presupuesto"), new RequirementId("plazos") });
            Assert.IsFalse(o.SummaryIsFaithful, "precondicion: sobra plazos");

            o.PresentSummary(new[] { new RequirementId("presupuesto") }); // ahora exacto

            Assert.IsTrue(o.SummaryIsFaithful);
        }

        [Test]
        public void PresentSummary_antes_de_AssignCase_no_tiene_efecto()
        {
            var o = new RequirementsScenarioObjective(new BoardroomObjectiveSettings(), CargarJsonDePrueba);

            Assert.DoesNotThrow(() => o.PresentSummary(new[] { new RequirementId("presupuesto") }));

            Assert.IsFalse(o.SummaryIsFaithful);
            Assert.IsFalse(o.HasCase);
        }

        [Test]
        public void PresentSummary_con_null_nunca_lanza_y_no_acredita()
        {
            var o = NuevoConCasoUno();

            Assert.DoesNotThrow(() => o.PresentSummary(null));

            Assert.IsFalse(o.SummaryIsFaithful);
        }

        [Test]
        public void PresentSummary_con_coleccion_vacia_nunca_lanza_y_no_acredita_sin_revelaciones_AD9()
        {
            var o = NuevoConCasoUno(); // sin ninguna revelacion

            Assert.DoesNotThrow(() => o.PresentSummary(Array.Empty<RequirementId>()));

            Assert.IsFalse(o.SummaryIsFaithful,
                "un resumen vacio sin ninguna revelacion no debe acreditar el cierre (AD9)");
        }

        [Test]
        public void Revelar_despues_de_cerrar_vuelve_el_cierre_infiel_hasta_volver_a_presentar_AD8()
        {
            var o = NuevoConCasoUno();
            o.RegisterDisclosure(Revelado(new RequirementId("presupuesto")));
            o.PresentSummary(new[] { new RequirementId("presupuesto") });
            Assert.IsTrue(o.SummaryIsFaithful, "precondicion");

            o.RegisterDisclosure(Revelado(new RequirementId("plazos")));

            Assert.IsFalse(o.SummaryIsFaithful,
                "revelar un requerimiento nuevo despues de cerrar debe volver el cierre infiel");

            o.PresentSummary(new[] { new RequirementId("presupuesto"), new RequirementId("plazos") });

            Assert.IsTrue(o.SummaryIsFaithful, "volver a presentar el conjunto exacto debe recuperar la fidelidad");
        }
    }
}
