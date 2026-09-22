using NUnit.Framework;
using NpcAi.Core;

namespace NpcAi.Scenarios.Boardroom.Tests
{
    /// <summary>
    /// tasks.md 2.3: fija <see cref="RequirementChecklistLoader.TryParse"/> contra JSON
    /// embebido en memoria (nunca <c>Data/Requirements/</c>). Espejo de
    /// <c>TriageKeyLoaderTests</c> (M9): caso feliz, entradas invalidas, ids vacios, dedupe
    /// con orden, y los bordes propios de M10 (AD10, AD12).
    /// </summary>
    public class RequirementChecklistLoaderTests
    {
        private const string JsonCasoFeliz = @"{
            ""id"": ""caso-juntas-01"",
            ""cliente"": { ""empresa"": ""Liga municipal de futbol"" },
            ""requerimientos"": [
                { ""id"": ""equipos"", ""receptividadMinima"": ""NoReceptivo"", ""respuesta"": ""x"" },
                { ""id"": ""jugadores"", ""receptividadMinima"": ""Neutral"", ""respuesta"": ""y"" }
            ]
        }";

        [Test]
        public void TryParse_con_json_bien_formado_devuelve_true_y_puebla_id_y_requerimientos()
        {
            var resultado = RequirementChecklistLoader.TryParse(JsonCasoFeliz, out var checklist);

            Assert.IsTrue(resultado);
            Assert.AreEqual("caso-juntas-01", checklist.Id);
            Assert.AreEqual(2, checklist.Count);
            Assert.AreEqual(new RequirementId("equipos"), checklist.Requerimientos[0]);
            Assert.AreEqual(new RequirementId("jugadores"), checklist.Requerimientos[1]);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        [TestCase("esto no es json")]
        [TestCase("{}")]
        [TestCase("[]")]
        public void TryParse_con_entrada_invalida_no_lanza_y_devuelve_false(string jsonInvalido)
        {
            RequirementChecklist checklist = null;
            bool resultado = true;

            Assert.DoesNotThrow(() => resultado = RequirementChecklistLoader.TryParse(jsonInvalido, out checklist));
            Assert.IsFalse(resultado);
            Assert.IsNull(checklist);
        }

        [TestCase("")]
        [TestCase("   ")]
        public void TryParse_con_id_de_caso_vacio_devuelve_false(string idInvalido)
        {
            var json = @"{ ""id"": """ + idInvalido + @""", ""requerimientos"": [ { ""id"": ""equipos"" } ] }";

            var resultado = RequirementChecklistLoader.TryParse(json, out var checklist);

            Assert.IsFalse(resultado);
            Assert.IsNull(checklist);
        }

        [TestCase(@"{ ""id"": ""caso-juntas-01"" }")]
        [TestCase(@"{ ""id"": ""caso-juntas-01"", ""requerimientos"": [] }")]
        public void TryParse_con_requerimientos_ausente_o_vacio_devuelve_false(string json)
        {
            var resultado = RequirementChecklistLoader.TryParse(json, out var checklist);

            Assert.IsFalse(resultado);
            Assert.IsNull(checklist);
        }

        [Test]
        public void TryParse_descarta_ids_de_requerimiento_vacios_y_conserva_los_validos()
        {
            const string json = @"{
                ""id"": ""caso-juntas-01"",
                ""requerimientos"": [
                    { ""id"": """" },
                    { ""id"": ""equipos"" },
                    { ""id"": ""   "" }
                ]
            }";

            var resultado = RequirementChecklistLoader.TryParse(json, out var checklist);

            Assert.IsTrue(resultado);
            Assert.AreEqual(1, checklist.Count);
            Assert.AreEqual(new RequirementId("equipos"), checklist.Requerimientos[0]);
        }

        [Test]
        public void TryParse_colapsa_duplicados_conservando_el_orden_del_archivo()
        {
            const string json = @"{
                ""id"": ""caso-juntas-01"",
                ""requerimientos"": [
                    { ""id"": ""equipos"" },
                    { ""id"": ""jugadores"" },
                    { ""id"": ""EQUIPOS"" },
                    { ""id"": ""partidos"" },
                    { ""id"": ""  jugadores  "" }
                ]
            }";

            var resultado = RequirementChecklistLoader.TryParse(json, out var checklist);

            Assert.IsTrue(resultado);
            Assert.AreEqual(3, checklist.Count);
            Assert.AreEqual(new RequirementId("equipos"), checklist.Requerimientos[0]);
            Assert.AreEqual(new RequirementId("jugadores"), checklist.Requerimientos[1]);
            Assert.AreEqual(new RequirementId("partidos"), checklist.Requerimientos[2]);
        }

        [Test]
        public void TryParse_ignora_campos_extra_del_esquema_de_m16_sin_romper()
        {
            const string json = @"{
                ""id"": ""caso-juntas-01"",
                ""cliente"": { ""empresa"": ""x"", ""rol"": ""y"", ""proyecto"": ""z"", ""contexto"": ""w"" },
                ""requerimientos"": [
                    {
                        ""id"": ""equipos"",
                        ""receptividadMinima"": ""NoReceptivo"",
                        ""ejemplosDePregunta"": [""a"", ""b""],
                        ""respuesta"": ""respuesta completa que M10 no debe mapear"",
                        ""emotionTag"": ""neutral"",
                        ""animationCue"": ""asentir""
                    }
                ]
            }";

            var resultado = RequirementChecklistLoader.TryParse(json, out var checklist);

            Assert.IsTrue(resultado);
            Assert.AreEqual("caso-juntas-01", checklist.Id);
            Assert.AreEqual(1, checklist.Count);
            Assert.AreEqual(new RequirementId("equipos"), checklist.Requerimientos[0]);
        }

        [Test]
        public void TryParse_acepta_un_caso_de_un_solo_requerimiento_AD12()
        {
            // AD12: RequirementChecklistLoader NO exige el minimo de 4 requerimientos que si
            // exige RequirementCaseLoader — ese minimo es invariante de M16, no de M10.
            const string json = @"{
                ""id"": ""caso-juntas-99"",
                ""requerimientos"": [ { ""id"": ""unico"" } ]
            }";

            var resultado = RequirementChecklistLoader.TryParse(json, out var checklist);

            Assert.IsTrue(resultado);
            Assert.AreEqual(1, checklist.Count);
            Assert.IsTrue(checklist.Contains(new RequirementId("unico")));
        }
    }
}
