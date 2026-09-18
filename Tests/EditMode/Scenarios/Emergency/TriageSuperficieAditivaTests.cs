using System;
using NpcAi.Core;
using NUnit.Framework;

namespace NpcAi.Scenarios.Emergency.Tests
{
    /// <summary>
    /// Superficie aditiva de <see cref="TriageScenarioObjective"/>
    /// (<c>AssignCase</c>/<c>RegisterRedFlag</c>/<c>DeclareTriage</c>/<c>Reset</c>) y los
    /// casos borde del constructor rico: <c>cargarJson</c> nulo/que lanza/sin bloque
    /// <c>clave</c>, <c>ClinicalCaseId.None</c>, reasignacion de caso y la idempotencia de
    /// <c>Reset()</c> (AD8).
    /// </summary>
    public class TriageSuperficieAditivaTests
    {
        private const string JsonCasoUno = @"{
            ""clave"": {
                ""triajeEsperado"": ""II"",
                ""banderasRojas"": [""b0"", ""b1"", ""b2"", ""b3"", ""b4""],
                ""cierreEsperado"": ""cierre""
            }
        }";

        private const string JsonCasoDos = @"{
            ""clave"": {
                ""triajeEsperado"": ""III"",
                ""banderasRojas"": [""x0"", ""x1""],
                ""cierreEsperado"": ""cierre dos""
            }
        }";

        private const string JsonSinClave = @"{ ""id"": ""caso-sin-clave"" }";

        private static string CargarJsonDePrueba(ClinicalCaseId id)
        {
            if (id.Value == "caso-01") return JsonCasoUno;
            if (id.Value == "caso-02") return JsonCasoDos;
            if (id.Value == "caso-sin-clave") return JsonSinClave;
            return null;
        }

        private static TriageScenarioObjective NuevoConCasoUno()
        {
            var o = new TriageScenarioObjective(new TriageObjectiveSettings(), CargarJsonDePrueba);
            o.AssignCase(new ClinicalCaseId("caso-01"));
            return o;
        }

        // --- RegisterRedFlag ---

        [Test]
        public void RegisterRedFlag_es_idempotente_por_indice()
        {
            var o = NuevoConCasoUno();

            o.RegisterRedFlag(2);
            o.RegisterRedFlag(2);

            Assert.AreEqual(1, o.RedFlagsFound);
        }

        [Test]
        public void RegisterRedFlag_con_indice_fuera_de_rango_no_lanza_y_no_cuenta()
        {
            var o = NuevoConCasoUno();

            Assert.DoesNotThrow(() => o.RegisterRedFlag(99));
            Assert.DoesNotThrow(() => o.RegisterRedFlag(-1));
            Assert.AreEqual(0, o.RedFlagsFound);
        }

        [Test]
        public void RegisterRedFlag_antes_de_AssignCase_no_tiene_efecto()
        {
            var o = new TriageScenarioObjective(new TriageObjectiveSettings(), CargarJsonDePrueba);

            Assert.DoesNotThrow(() => o.RegisterRedFlag(0));

            Assert.AreEqual(0, o.RedFlagsFound);
            Assert.IsFalse(o.HasKey);
        }

        [Test]
        public void RedFlagCount_refleja_el_total_de_banderas_de_la_clave_cargada()
        {
            var o = NuevoConCasoUno();

            Assert.AreEqual(5, o.RedFlagCount);
        }

        [Test]
        public void RedFlagCount_es_cero_sin_clave()
        {
            var o = new TriageScenarioObjective(new TriageObjectiveSettings(), CargarJsonDePrueba);

            Assert.AreEqual(0, o.RedFlagCount);
        }

        // --- DeclareTriage ---

        [Test]
        public void DeclareTriage_correcto_contribuye_el_peso_completo_de_esa_mitad()
        {
            var o = NuevoConCasoUno();

            o.DeclareTriage("II");

            // clinico = 0.7*0 + 0.3*1 = 0.3 (peso completo del componente de triaje);
            // total = 0.3*0 + 0.7*0.3 = 0.21
            Assert.AreEqual(0.7f * 0.3f, o.Progress01, 1e-4f);
        }

        [TestCase("ii")]
        [TestCase(" II ")]
        public void DeclareTriage_normaliza_espacios_y_mayusculas(string categoriaCruda)
        {
            var o = NuevoConCasoUno();

            o.DeclareTriage(categoriaCruda);

            Assert.AreEqual(0.7f * 0.3f, o.Progress01, 1e-4f);
        }

        [Test]
        public void DeclareTriage_sobrescribe_de_correcto_a_incorrecto()
        {
            var o = NuevoConCasoUno();
            o.DeclareTriage("II");
            var progresoCorrecto = o.Progress01;

            o.DeclareTriage("V");

            Assert.Less(o.Progress01, progresoCorrecto);
        }

        [Test]
        public void DeclareTriage_antes_de_AssignCase_no_tiene_efecto()
        {
            var o = new TriageScenarioObjective(new TriageObjectiveSettings(), CargarJsonDePrueba);

            Assert.DoesNotThrow(() => o.DeclareTriage("II"));

            Assert.AreEqual(0f, o.Progress01);
        }

        // --- Carga fallida de clave (Requirement: Carga del bloque clave) ---

        [Test]
        public void CargarJson_nulo_deja_HasKey_falso_sin_lanzar()
        {
            var o = new TriageScenarioObjective(new TriageObjectiveSettings(), null);

            Assert.DoesNotThrow(() => o.AssignCase(new ClinicalCaseId("caso-01")));

            Assert.IsFalse(o.HasKey);
        }

        [Test]
        public void CargarJson_que_lanza_deja_HasKey_falso_sin_propagar_la_excepcion()
        {
            string CargarQueLanza(ClinicalCaseId id) => throw new InvalidOperationException("boom");
            var o = new TriageScenarioObjective(new TriageObjectiveSettings(), CargarQueLanza);

            Assert.DoesNotThrow(() => o.AssignCase(new ClinicalCaseId("caso-01")));

            Assert.IsFalse(o.HasKey);
        }

        [Test]
        public void Caso_sin_bloque_clave_deja_HasKey_falso_sin_lanzar()
        {
            var o = new TriageScenarioObjective(new TriageObjectiveSettings(), CargarJsonDePrueba);

            Assert.DoesNotThrow(() => o.AssignCase(new ClinicalCaseId("caso-sin-clave")));

            Assert.IsFalse(o.HasKey);
        }

        [Test]
        public void AssignCase_con_ClinicalCaseId_None_deja_HasKey_falso()
        {
            var o = new TriageScenarioObjective(new TriageObjectiveSettings(), CargarJsonDePrueba);

            Assert.DoesNotThrow(() => o.AssignCase(ClinicalCaseId.None));

            Assert.IsFalse(o.HasKey);
        }

        // --- Reasignacion y Reset (AD8) ---

        [Test]
        public void Reasignar_caso_descarta_banderas_y_triaje_previos()
        {
            var o = NuevoConCasoUno();
            o.RegisterRedFlag(0);
            o.RegisterRedFlag(1);
            o.DeclareTriage("II");
            Assert.AreEqual(2, o.RedFlagsFound, "precondicion");

            o.AssignCase(new ClinicalCaseId("caso-02"));

            Assert.AreEqual(0, o.RedFlagsFound);
            Assert.AreEqual(2, o.RedFlagCount); // caso-02 tiene 2 banderas (x0, x1)
            Assert.AreEqual(0f, o.Progress01); // ni banderas ni triaje sobreviven a la reasignacion
        }

        [Test]
        public void Reset_vuelve_al_estado_recien_construido()
        {
            var o = NuevoConCasoUno();
            o.RegisterRedFlag(0);
            o.DeclareTriage("II");
            for (var i = 0; i < 3; i++)
                o.Notify(new ReceptivityChange(Receptivity.Neutral, Receptivity.Receptivo, 2, "GESTO"));

            o.Reset();

            Assert.IsFalse(o.HasKey);
            Assert.AreEqual(0, o.RedFlagsFound);
            Assert.AreEqual(0, o.RedFlagCount);
            Assert.AreEqual(string.Empty, o.ExpectedClosure);
            Assert.AreEqual(0f, o.Progress01);
        }

        [Test]
        public void Reset_es_idempotente()
        {
            var o = NuevoConCasoUno();

            o.Reset();
            Assert.DoesNotThrow(() => o.Reset());

            Assert.IsFalse(o.HasKey);
            Assert.AreEqual(0f, o.Progress01);
        }
    }
}
