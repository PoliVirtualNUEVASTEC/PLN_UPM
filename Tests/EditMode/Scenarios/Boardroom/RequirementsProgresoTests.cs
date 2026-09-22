using NpcAi.Core;
using NUnit.Framework;

namespace NpcAi.Scenarios.Boardroom.Tests
{
    /// <summary>
    /// Aritmetica del progreso de <see cref="RequirementsScenarioObjective"/>: tabla de sanidad
    /// de 7 filas (design.md, "Aritmetica del progreso"; pesos por defecto: PesoDeTrato=0.2,
    /// PesoDeCobertura=0.75, PasosDeTrato=5, que aplanan a
    /// <c>Progress01 = 0.2*t + 0.6*c + 0.2*k</c> con caso asignado), cada via variando sola,
    /// renormalizacion sin caso (fila 7), clamp a [0,1] bajo maltrato/mejora excesiva,
    /// reversibilidad de <c>IsComplete</c> y la penalizacion exacta de 0.04 (0.2 * 1/5) que deja
    /// un <c>Worsened</c> sin recuperar.
    /// </summary>
    public class RequirementsProgresoTests
    {
        // Caso fijo de 4 requerimientos para toda la tabla de sanidad.
        private const string JsonCasoDeCuatro = @"{
            ""id"": ""caso-progreso"",
            ""requerimientos"": [
                { ""id"": ""a"" }, { ""id"": ""b"" }, { ""id"": ""c"" }, { ""id"": ""d"" }
            ]
        }";

        private static string CargarCasoDeCuatro(RequirementCaseId id) =>
            id.Value == "caso-progreso" ? JsonCasoDeCuatro : null;

        private static RequirementsScenarioObjective NuevoConCaso()
        {
            var o = new RequirementsScenarioObjective(new BoardroomObjectiveSettings(), CargarCasoDeCuatro);
            o.AssignCase(new RequirementCaseId("caso-progreso"));
            return o;
        }

        private static readonly RequirementId[] Ids =
        {
            new RequirementId("a"), new RequirementId("b"), new RequirementId("c"), new RequirementId("d"),
        };

        private static Core.RequirementResponse Revelado(int i) =>
            new Core.RequirementResponse(RequirementOutcome.Revelado, default, Ids[i]);

        private static ReceptivityChange Mejora() =>
            new ReceptivityChange(Receptivity.Neutral, Receptivity.Receptivo, 2, "GESTO_EMPATICO");

        private static ReceptivityChange Empeora() =>
            new ReceptivityChange(Receptivity.Neutral, Receptivity.NoReceptivo, -2, "AGRESION_DIRECTA");

        // --- Tabla de sanidad, filas 1-6: caso asignado, trato arranca sembrado lleno (t=1) ---

        [Test]
        public void Fila1_recien_asignado_solo_trato_sembrado()
        {
            var o = NuevoConCaso();

            // t=1 (5/5), c=0, k=0 => 0.2*1 + 0.6*0 + 0.2*0 = 0.2
            Assert.AreEqual(0.2f, o.Progress01, 1e-4f);
        }

        [Test]
        public void Fila2_cobertura_parcial_sola_sube_el_progreso()
        {
            var o = NuevoConCaso();
            o.RegisterDisclosure(Revelado(0));
            o.RegisterDisclosure(Revelado(1)); // 2 de 4

            // t=1, c=0.5, k=0 => 0.2 + 0.6*0.5 + 0 = 0.5
            Assert.AreEqual(0.5f, o.Progress01, 1e-4f);
        }

        [Test]
        public void Fila2b_cierre_temprano_honesto_con_cobertura_parcial()
        {
            var o = NuevoConCaso();
            o.RegisterDisclosure(Revelado(0));
            o.RegisterDisclosure(Revelado(1)); // 2 de 4
            o.PresentSummary(new[] { Ids[0], Ids[1] }); // cierre EXACTO con lo revelado, no con el catalogo

            // t=1, c=0.5, k=1 => 0.2 + 0.6*0.5 + 0.2 = 0.7 -- cierre temprano honesto, Progress01 < 1
            Assert.AreEqual(0.7f, o.Progress01, 1e-4f);
            Assert.IsFalse(o.IsComplete, "cerrar antes de cubrir el catalogo no completa el objetivo");
        }

        [Test]
        public void Fila3_cobertura_completa_sin_cierre()
        {
            var o = NuevoConCaso();
            for (var i = 0; i < 4; i++) o.RegisterDisclosure(Revelado(i));

            // t=1, c=1, k=0 => 0.2 + 0.6 + 0 = 0.8
            Assert.AreEqual(0.8f, o.Progress01, 1e-4f);
        }

        [Test]
        public void Fila4_cobertura_y_cierre_completos_con_trato_lleno_es_progreso_total()
        {
            var o = NuevoConCaso();
            for (var i = 0; i < 4; i++) o.RegisterDisclosure(Revelado(i));
            o.PresentSummary(Ids);

            // t=1, c=1, k=1 => 0.2 + 0.6 + 0.2 = 1.0
            Assert.AreEqual(1f, o.Progress01, 1e-4f);
            Assert.IsTrue(o.IsComplete);
        }

        [Test]
        public void Fila5_un_Worsened_no_recuperado_penaliza_exactamente_004_y_revierte_IsComplete()
        {
            var o = NuevoConCaso();
            for (var i = 0; i < 4; i++) o.RegisterDisclosure(Revelado(i));
            o.PresentSummary(Ids);
            Assert.IsTrue(o.IsComplete, "precondicion: fila 4 completa");

            o.Notify(Empeora()); // trato baja de 5 a 4: t = 0.8

            // t=0.8, c=1, k=1 => 0.2*0.8 + 0.6 + 0.2 = 0.96 -- 0.04 menos que la fila 4
            Assert.AreEqual(0.96f, o.Progress01, 1e-4f);
            Assert.AreEqual(0.04f, 1f - o.Progress01, 1e-4f,
                "un Worsened no recuperado debe penalizar exactamente 0.04 (0.2 * 1/5)");
            Assert.IsFalse(o.IsComplete, "IsComplete debe revertirse: no es pegajosa (G11)");
        }

        [Test]
        public void Fila6_trato_agotado_por_completo_con_levantamiento_completo()
        {
            var o = NuevoConCaso();
            for (var i = 0; i < 4; i++) o.RegisterDisclosure(Revelado(i));
            o.PresentSummary(Ids);
            for (var i = 0; i < 5; i++) o.Notify(Empeora()); // vacia el credito de trato

            // t=0, c=1, k=1 => 0 + 0.6 + 0.2 = 0.8
            Assert.AreEqual(0.8f, o.Progress01, 1e-4f);
            Assert.IsFalse(o.IsComplete);
        }

        // --- Fila 7: sin caso, renormalizacion (el trato es el 100% del progreso, sin escalar) ---

        [Test]
        public void Fila7_sin_caso_el_trato_se_renormaliza_al_100_por_ciento()
        {
            var o = new RequirementsScenarioObjective(new BoardroomObjectiveSettings(), CargarCasoDeCuatro);
            for (var i = 0; i < 3; i++) o.Notify(Mejora()); // t=3/5=0.6, sin caso

            Assert.IsFalse(o.HasCase, "precondicion");
            // sin caso: Progress01 = clamp01(t) = 0.6, no se escala por PesoDeTrato
            Assert.AreEqual(0.6f, o.Progress01, 1e-4f);
        }

        // --- Clamp a [0,1]: ningun Notify en exceso saca el progreso del rango, con o sin caso ---

        [Test]
        public void El_progreso_nunca_sale_de_0_1_bajo_maltrato_o_mejora_excesiva_con_caso()
        {
            var o = NuevoConCaso();
            for (var i = 0; i < 4; i++) o.RegisterDisclosure(Revelado(i));
            o.PresentSummary(Ids);

            for (var i = 0; i < 20; i++)
            {
                o.Notify(i % 2 == 0 ? Empeora() : Mejora());
                Assert.GreaterOrEqual(o.Progress01, 0f);
                Assert.LessOrEqual(o.Progress01, 1f);
            }
        }

        [Test]
        public void El_trato_satura_en_PasosDeTrato_y_nunca_baja_del_piso_cero()
        {
            var o = NuevoConCaso(); // trato ya sembrado lleno (AD1)

            for (var i = 0; i < 10; i++) o.Notify(Mejora()); // ya esta al tope, no debe subir mas
            for (var i = 0; i < 4; i++) o.RegisterDisclosure(Revelado(i));
            o.PresentSummary(Ids);
            Assert.AreEqual(1f, o.Progress01, 1e-4f, "el trato saturado sigue siendo 1 paso pleno");

            for (var i = 0; i < 10; i++) o.Notify(Empeora()); // no debe bajar del piso 0

            Assert.AreEqual(0.8f, o.Progress01, 1e-4f, "el trato agotado no baja del piso 0");
        }
    }
}
