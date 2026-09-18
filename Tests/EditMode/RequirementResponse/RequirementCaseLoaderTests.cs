using System.Linq;
using NpcAi.Core;
using NUnit.Framework;

namespace NpcAi.RequirementResponse.Tests
{
    /// <summary>
    /// Prueba unitaria de <see cref="RequirementCaseLoader"/> con JSON embebido en memoria,
    /// sin depender de <c>Data/Requirements/</c> (esa cobertura, sobre los .json reales, es
    /// <c>RequirementCasesDataTests</c>, Fase 4 de este cambio). M14/M15 no tienen un
    /// <c>ClinicalCaseLoaderTests</c> dedicado, pero M16 si necesita uno propio porque AD1
    /// (mapeo string-a-Receptivity por switch) y AD2 (descartar un requerimiento por nombre
    /// invalido sin abortar el caso) son comportamiento nuevo que M14 no tiene.
    /// </summary>
    public class RequirementCaseLoaderTests
    {
        // 6 requerimientos (por encima del minimo de 4 que exige RequirementCaseLoader; bajado
        // de 6 a 4 el 2026-09-18 porque el dominio real de colegio en Data/Requirements/ solo
        // llega a 4 sin inventar contenido). Cubre los 3 nombres de receptividadMinima (AD1) y
        // deja "emotionTag"/"animationCue" ausentes en "req-1" para probar los defaults de AD8;
        // "req-2" si los declara, para comparar contra el default.
        private const string JsonDeCasoValido = @"{
            ""id"": ""caso-prueba"",
            ""cliente"": {
                ""empresa"": ""Empresa de prueba"",
                ""rol"": ""Gerente"",
                ""proyecto"": ""Sistema de prueba"",
                ""contexto"": ""Contexto de prueba.""
            },
            ""requerimientos"": [
                { ""id"": ""req-1"", ""receptividadMinima"": ""NoReceptivo"",
                  ""ejemplosDePregunta"": [""pregunta uno"", ""otra forma""],
                  ""respuesta"": ""Respuesta uno."" },
                { ""id"": ""req-2"", ""receptividadMinima"": ""NoReceptivo"",
                  ""ejemplosDePregunta"": [""pregunta dos""],
                  ""respuesta"": ""Respuesta dos."",
                  ""emotionTag"": ""neutral"", ""animationCue"": ""asentir"" },
                { ""id"": ""req-3"", ""receptividadMinima"": ""Neutral"",
                  ""ejemplosDePregunta"": [""pregunta tres""],
                  ""respuesta"": ""Respuesta tres."" },
                { ""id"": ""req-4"", ""receptividadMinima"": ""Neutral"",
                  ""ejemplosDePregunta"": [""pregunta cuatro""],
                  ""respuesta"": ""Respuesta cuatro."" },
                { ""id"": ""req-5"", ""receptividadMinima"": ""Receptivo"",
                  ""ejemplosDePregunta"": [""pregunta cinco""],
                  ""respuesta"": ""Respuesta cinco."" },
                { ""id"": ""req-6"", ""receptividadMinima"": ""Receptivo"",
                  ""ejemplosDePregunta"": [""pregunta seis""],
                  ""respuesta"": ""Respuesta seis."" }
            ]
        }";

        [Test]
        public void Un_json_valido_carga_los_6_requerimientos_con_su_receptividad()
        {
            var cargo = RequirementCaseLoader.TryParse(JsonDeCasoValido, out var caso);

            Assert.IsTrue(cargo, "Un JSON con 6 requerimientos validos DEBE cargar");
            Assert.AreEqual("caso-prueba", caso.Id);
            Assert.AreEqual(6, caso.Requerimientos.Count);
            Assert.AreEqual(Receptivity.NoReceptivo, caso.Requerimientos[0].ReceptividadMinima);
            Assert.AreEqual(Receptivity.Neutral, caso.Requerimientos[2].ReceptividadMinima);
            Assert.AreEqual(Receptivity.Receptivo, caso.Requerimientos[4].ReceptividadMinima);
        }

        [Test]
        public void Json_basura_no_lanza_y_devuelve_false()
        {
            var resultado = true;
            RequirementCase caso = null;
            Assert.DoesNotThrow(() => resultado = RequirementCaseLoader.TryParse("esto no es json {{{", out caso));

            Assert.IsFalse(resultado);
            Assert.IsNull(caso);
        }

        [Test]
        public void Json_nulo_vacio_o_solo_espacios_devuelve_false_sin_lanzar()
        {
            Assert.IsFalse(RequirementCaseLoader.TryParse(null, out _));
            Assert.IsFalse(RequirementCaseLoader.TryParse(string.Empty, out _));
            Assert.IsFalse(RequirementCaseLoader.TryParse("   ", out _));
        }

        [Test]
        public void Los_3_nombres_de_receptividadMinima_mapean_al_enum_correcto()
        {
            RequirementCaseLoader.TryParse(JsonDeCasoValido, out var caso);

            var porId = caso.Requerimientos.ToDictionary(r => r.Id);
            Assert.AreEqual(Receptivity.NoReceptivo, porId["req-1"].ReceptividadMinima);
            Assert.AreEqual(Receptivity.Neutral, porId["req-3"].ReceptividadMinima);
            Assert.AreEqual(Receptivity.Receptivo, porId["req-5"].ReceptividadMinima);
        }

        [Test]
        public void Un_nombre_de_receptividadMinima_desconocido_descarta_solo_ese_requerimiento()
        {
            // 5 requerimientos: 4 validos + 1 con receptividadMinima desconocida ("Urgente").
            // Tras descartar el invalido quedan exactamente 4 (el minimo), asi que el caso SI
            // carga, pero sin el requerimiento descartado (AD2).
            const string json = @"{
                ""id"": ""caso-prueba-2"",
                ""cliente"": { ""empresa"": ""E"", ""rol"": ""R"", ""proyecto"": ""P"", ""contexto"": ""C"" },
                ""requerimientos"": [
                    { ""id"": ""req-1"", ""receptividadMinima"": ""NoReceptivo"", ""ejemplosDePregunta"": [""a""], ""respuesta"": ""r1"" },
                    { ""id"": ""req-2"", ""receptividadMinima"": ""NoReceptivo"", ""ejemplosDePregunta"": [""b""], ""respuesta"": ""r2"" },
                    { ""id"": ""req-3"", ""receptividadMinima"": ""Neutral"", ""ejemplosDePregunta"": [""c""], ""respuesta"": ""r3"" },
                    { ""id"": ""req-4"", ""receptividadMinima"": ""Receptivo"", ""ejemplosDePregunta"": [""d""], ""respuesta"": ""r4"" },
                    { ""id"": ""req-invalido"", ""receptividadMinima"": ""Urgente"", ""ejemplosDePregunta"": [""g""], ""respuesta"": ""r7"" }
                ]
            }";

            var cargo = RequirementCaseLoader.TryParse(json, out var caso);

            Assert.IsTrue(cargo, "4 requerimientos validos alcanzan el minimo tras descartar el invalido");
            Assert.AreEqual(4, caso.Requerimientos.Count);
            Assert.IsFalse(caso.Requerimientos.Any(r => r.Id == "req-invalido"),
                "El requerimiento con receptividadMinima desconocida DEBE descartarse (AD2)");
        }

        [Test]
        public void Descartar_deja_menos_del_minimo_y_TryParse_devuelve_false()
        {
            // 5 requerimientos, 2 con receptividadMinima invalida (una desconocida, una
            // vacia): quedan 3 validos, por debajo del minimo de 4, asi que el caso completo
            // NO carga.
            const string json = @"{
                ""id"": ""caso-prueba-3"",
                ""cliente"": { ""empresa"": ""E"", ""rol"": ""R"", ""proyecto"": ""P"", ""contexto"": ""C"" },
                ""requerimientos"": [
                    { ""id"": ""req-1"", ""receptividadMinima"": ""NoReceptivo"", ""ejemplosDePregunta"": [""a""], ""respuesta"": ""r1"" },
                    { ""id"": ""req-2"", ""receptividadMinima"": ""Neutral"", ""ejemplosDePregunta"": [""c""], ""respuesta"": ""r3"" },
                    { ""id"": ""req-3"", ""receptividadMinima"": ""Receptivo"", ""ejemplosDePregunta"": [""d""], ""respuesta"": ""r4"" },
                    { ""id"": ""req-invalido-1"", ""receptividadMinima"": ""Urgente"", ""ejemplosDePregunta"": [""e""], ""respuesta"": ""r5"" },
                    { ""id"": ""req-invalido-2"", ""receptividadMinima"": """", ""ejemplosDePregunta"": [""f""], ""respuesta"": ""r6"" }
                ]
            }";

            var cargo = RequirementCaseLoader.TryParse(json, out var caso);

            Assert.IsFalse(cargo, "3 requerimientos validos estan bajo el minimo de 4");
            Assert.IsNull(caso);
        }

        [Test]
        public void EmotionTag_y_AnimationCue_ausentes_toman_los_defaults_neutral_e_idle()
        {
            RequirementCaseLoader.TryParse(JsonDeCasoValido, out var caso);

            var sinTags = caso.Requerimientos.First(r => r.Id == "req-1");
            Assert.AreEqual("neutral", sinTags.EmotionTag, "Sin emotionTag, el default es \"neutral\" (AD8)");
            Assert.AreEqual("idle", sinTags.AnimationCue, "Sin animationCue, el default es \"idle\" (AD8)");

            var conTags = caso.Requerimientos.First(r => r.Id == "req-2");
            Assert.AreEqual("neutral", conTags.EmotionTag);
            Assert.AreEqual("asentir", conTags.AnimationCue);
        }
    }
}
