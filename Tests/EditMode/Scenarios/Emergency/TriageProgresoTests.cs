using NpcAi.Core;
using NUnit.Framework;

namespace NpcAi.Scenarios.Emergency.Tests
{
    /// <summary>
    /// La mezcla de <see cref="TriageScenarioObjective"/> con caso asignado: los techos
    /// exactos 0.3 (solo receptividad) / 0.7 (solo clinica) / 1.0 (ambas) que fija la tabla
    /// "Aritmetica" de <c>design.md</c>, la racha de receptividad sostenida, el credito
    /// proporcional de banderas rojas, y la reversibilidad de <c>IsComplete</c> por sus dos
    /// vias independientes.
    /// </summary>
    public class TriageProgresoTests
    {
        // JSON embebido en memoria (nunca Data/Cases/): 4 banderas rojas, para que el credito
        // proporcional de 2/4 sea exactamente la mitad del peso maximo de esa mitad.
        private const string JsonCasoDePrueba = @"{
            ""clave"": {
                ""triajeEsperado"": ""II"",
                ""banderasRojas"": [""b0"", ""b1"", ""b2"", ""b3""],
                ""cierreEsperado"": ""cierre de prueba""
            }
        }";

        private static string CargarJsonDePrueba(ClinicalCaseId id) =>
            id.Value == "caso-01" ? JsonCasoDePrueba : null;

        private static TriageScenarioObjective NuevoConCaso()
        {
            var o = new TriageScenarioObjective(new TriageObjectiveSettings(), CargarJsonDePrueba);
            o.AssignCase(new ClinicalCaseId("caso-01"));
            return o;
        }

        private static ReceptivityChange Mejora() =>
            new ReceptivityChange(Receptivity.Neutral, Receptivity.Receptivo, 2, "GESTO_EMPATICO");

        private static ReceptivityChange Empeora() =>
            new ReceptivityChange(Receptivity.Neutral, Receptivity.NoReceptivo, -2, "AGRESION_DIRECTA");

        [Test]
        public void Receptividad_sostenida_sola_no_completa_el_progreso()
        {
            var o = NuevoConCaso();

            for (var i = 0; i < 10; i++) o.Notify(Mejora());

            Assert.AreEqual(0.3f, o.Progress01, 1e-4f);
            Assert.IsFalse(o.IsComplete);
        }

        [Test]
        public void Correccion_clinica_sola_no_completa_el_progreso()
        {
            var o = NuevoConCaso();

            o.RegisterRedFlag(0);
            o.RegisterRedFlag(1);
            o.RegisterRedFlag(2);
            o.RegisterRedFlag(3);
            o.DeclareTriage("II");

            Assert.AreEqual(0.7f, o.Progress01, 1e-4f);
            Assert.IsFalse(o.IsComplete);
        }

        [Test]
        public void Ambas_mitades_completan_el_progreso()
        {
            var o = NuevoConCaso();

            for (var i = 0; i < 5; i++) o.Notify(Mejora());
            o.RegisterRedFlag(0);
            o.RegisterRedFlag(1);
            o.RegisterRedFlag(2);
            o.RegisterRedFlag(3);
            o.DeclareTriage("II");

            Assert.AreEqual(1f, o.Progress01, 1e-4f);
            Assert.IsTrue(o.IsComplete);
        }

        [Test]
        public void El_credito_por_bandera_roja_es_proporcional_a_la_fraccion_encontrada()
        {
            var o = NuevoConCaso();

            o.RegisterRedFlag(0);
            o.RegisterRedFlag(1);

            // clinico = 0.7*(2/4) + 0.3*0 = 0.35; total = 0.3*0 + 0.7*0.35 = 0.245
            Assert.AreEqual(0.245f, o.Progress01, 1e-4f);
        }

        [Test]
        public void La_racha_crece_monotonamente_hasta_su_techo_con_caso_asignado()
        {
            var o = NuevoConCaso();
            var anterior = 0f;

            for (var i = 0; i < 5; i++)
            {
                o.Notify(Mejora());
                Assert.Greater(o.Progress01, anterior);
                anterior = o.Progress01;
            }

            Assert.AreEqual(0.3f, o.Progress01, 1e-4f);
        }

        [Test]
        public void Un_Worsened_reinicia_la_racha_completa_no_descuenta_un_paso()
        {
            var o = NuevoConCaso();
            for (var i = 0; i < 3; i++) o.Notify(Mejora());
            Assert.Greater(o.Progress01, 0f);

            o.Notify(Empeora());

            Assert.AreEqual(0f, o.Progress01);
        }

        [Test]
        public void Notify_con_el_valor_por_defecto_no_altera_la_racha()
        {
            var o = NuevoConCaso();
            for (var i = 0; i < 3; i++) o.Notify(Mejora());
            var antes = o.Progress01;

            o.Notify(default);

            Assert.AreEqual(antes, o.Progress01);
        }

        [Test]
        public void Notify_con_From_igual_a_To_no_altera_la_racha()
        {
            var o = NuevoConCaso();
            for (var i = 0; i < 3; i++) o.Notify(Mejora());
            var antes = o.Progress01;

            o.Notify(new ReceptivityChange(Receptivity.Neutral, Receptivity.Neutral, 0, "SIN_CAMBIO"));

            Assert.AreEqual(antes, o.Progress01);
        }

        [Test]
        public void IsComplete_se_revierte_por_Worsened_tras_haber_completado()
        {
            var o = NuevoConCaso();
            for (var i = 0; i < 5; i++) o.Notify(Mejora());
            o.RegisterRedFlag(0);
            o.RegisterRedFlag(1);
            o.RegisterRedFlag(2);
            o.RegisterRedFlag(3);
            o.DeclareTriage("II");
            Assert.IsTrue(o.IsComplete, "precondicion: el objetivo debe completar antes de revertir");

            o.Notify(Empeora());

            Assert.Less(o.Progress01, 1f);
            Assert.IsFalse(o.IsComplete);
        }

        [Test]
        public void IsComplete_se_revierte_por_triaje_incorrecto_tras_haber_acertado()
        {
            var o = NuevoConCaso();
            for (var i = 0; i < 5; i++) o.Notify(Mejora());
            o.RegisterRedFlag(0);
            o.RegisterRedFlag(1);
            o.RegisterRedFlag(2);
            o.RegisterRedFlag(3);
            o.DeclareTriage("II");
            Assert.IsTrue(o.IsComplete, "precondicion: el objetivo debe completar antes de revertir");

            o.DeclareTriage("III");

            Assert.Less(o.Progress01, 1f);
            Assert.IsFalse(o.IsComplete);
        }
    }
}
