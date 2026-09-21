using NpcAi.Core;
using NpcAi.Core.Tests;
using NUnit.Framework;

namespace NpcAi.RequirementResponse.Tests
{
    /// <summary>
    /// <see cref="RequirementResponder"/> (real) contra el contrato de
    /// <see cref="IRequirementResponder"/>. El fixture embebido es fiel al dominio real de
    /// <c>Data/Requirements/caso-juntas-01.json</c> (torneo de futbol, 6 requerimientos, 2 por
    /// nivel de receptividad): NO declara un requerimiento de "presupuesto" porque el catalogo
    /// real solo transcribe lo que narra <c>Data/Corpus/juntas.json</c>, sin inventar contenido
    /// (decision de Jefferson, 2026-09-18, #66).
    /// <para>
    /// <see cref="RequirementResponderContract"/> usa la frase fija
    /// "cual es el presupuesto del proyecto", que en este fixture NUNCA empareja (no hay
    /// requerimiento de presupuesto), asi que <c>Respond</c> siempre devuelve
    /// <c>RequirementOutcome.NoAplica</c> para esa frase. Verificado linea por linea contra
    /// <c>RequirementResponderContract.cs</c> (M0, congelada, solo lectura): de las 10 pruebas
    /// heredadas, SOLO 4 dependen de un <c>Assume.That(...Outcome...)</c> que exige
    /// <c>Revelado</c> o <c>AunNoRevelado</c> con esa frase, y por eso quedan <c>Assume</c>-omitidas
    /// (inconclusas, NUNCA rojas): <c>Cuando_revela_el_texto_no_es_vacio_y_los_tags_no_son_nulos</c>,
    /// <c>Cuando_aun_no_revela_responde_con_un_desvio_no_vacio</c>,
    /// <c>El_RequirementId_esta_poblado_si_y_solo_si_el_turno_aplica</c> y
    /// <c>Baja_receptividad_y_alta_receptividad_no_dan_el_mismo_texto</c>. Las otras 6 SI corren
    /// en verde, incluidas 2 que <c>tasks.md</c> (redactado antes de escribir esta clase) listo
    /// por error entre las omitidas: <c>Es_determinista_en_Outcome_texto_y_RequirementId_para_la_misma_entrada</c>
    /// y <c>AssignCase_es_idempotente_con_el_mismo_par</c> NO tienen ningun <c>Assume</c> sobre
    /// <c>Outcome</c>, solo comparan consistencia entre llamadas — y esa consistencia se cumple
    /// igual de bien cuando las 3 llamadas dan <c>NoAplica</c>. Correccion documentada aqui y en
    /// <c>apply-progress.md</c>, no oculta; gap conocido contra la spec
    /// <c>respondedor-requerimientos-m16</c>, para <c>sdd-verify</c>.
    /// </para>
    /// </summary>
    public class RequirementResponderTests : RequirementResponderContract
    {
        // Fiel al dominio real de Data/Requirements/caso-juntas-01.json (torneo de futbol),
        // sin "presupuesto" (decision #66). 6 requerimientos: 2 NoReceptivo, 2 Neutral,
        // 2 Receptivo.
        private const string CasoJuntas01 = @"{
            ""id"": ""caso-juntas-01"",
            ""cliente"": {
                ""empresa"": ""Liga municipal de futbol"",
                ""rol"": ""Coordinador del torneo"",
                ""proyecto"": ""Sistema de gestion del torneo"",
                ""contexto"": ""Hoy llevan todo en cuadernos y hojas de calculo sueltas.""
            },
            ""requerimientos"": [
                { ""id"": ""equipos"", ""receptividadMinima"": ""NoReceptivo"",
                  ""ejemplosDePregunta"": [""como identifican a cada equipo"", ""que datos manejan del equipo""],
                  ""respuesta"": ""Cada equipo se registra con su nombre y el ano de fundacion."" },
                { ""id"": ""jugadores"", ""receptividadMinima"": ""NoReceptivo"",
                  ""ejemplosDePregunta"": [""que datos manejan de cada jugador"", ""puede un jugador jugar en dos equipos""],
                  ""respuesta"": ""De cada jugador guardamos documento, nombre y posicion, y solo puede pertenecer a un equipo durante el torneo."" },
                { ""id"": ""partidos"", ""receptividadMinima"": ""Neutral"",
                  ""ejemplosDePregunta"": [""como arman un partido"", ""quien dirige cada partido""],
                  ""respuesta"": ""Un partido enfrenta a dos equipos, uno local y uno visitante, y siempre lo dirige un grupo de arbitros."" },
                { ""id"": ""estadio"", ""receptividadMinima"": ""Neutral"",
                  ""ejemplosDePregunta"": [""que informacion registran del estadio"", ""un estadio tiene varios partidos""],
                  ""respuesta"": ""Cada estadio queda identificado por nombre, ciudad y capacidad, y puede tener varios partidos."" },
                { ""id"": ""arbitros"", ""receptividadMinima"": ""Receptivo"",
                  ""ejemplosDePregunta"": [""como asignan los arbitros"", ""puede un arbitro pitar varios partidos""],
                  ""respuesta"": ""Cada arbitro tiene codigo de licencia y nombre, y puede pitar varios partidos del torneo."" },
                { ""id"": ""estadisticas"", ""receptividadMinima"": ""Receptivo"",
                  ""ejemplosDePregunta"": [""como llevan el rendimiento"", ""como registran goles y tarjetas""],
                  ""respuesta"": ""Registramos los goles y las tarjetas, amarillas y rojas, por jugador y por partido."" }
            ]
        }";

        // Espejo de Data/Requirements/matices.json, embebido para que esta prueba unitaria no
        // dependa de IO (RequirementCasesDataTests, Fase 4, valida el archivo real).
        private const string MaticesJson = @"{
            ""matices"": [
                { ""personalidad"": ""grosero"", ""prefijoRevelado"": ""Ya se lo dije, "",
                  ""desvios"": [""Eso no se lo voy a explicar ahora.""] },
                { ""personalidad"": ""empatico"", ""prefijoRevelado"": ""Claro, con gusto. "",
                  ""desvios"": [""Prefiero que primero nos conozcamos un poco mas.""] },
                { ""personalidad"": ""histerico"", ""prefijoRevelado"": ""Uf, si! "",
                  ""desvios"": [""No, no, eso todavia no!""] },
                { ""personalidad"": ""introvertido"", ""prefijoRevelado"": """",
                  ""desvios"": [""Preferiria no hablar de eso todavia.""] }
            ]
        }";

        private const string RespuestaEstadisticas =
            "Registramos los goles y las tarjetas, amarillas y rojas, por jugador y por partido.";

        private static string CargarCaso(RequirementCaseId id) =>
            id.Value == "caso-juntas-01" ? CasoJuntas01 : null;

        protected override IRequirementResponder CreateSubject() =>
            new RequirementResponder(CargarCaso, MaticesJson);

        private static RequirementResponder SujetoListo(PersonalityId personalidad)
        {
            var s = new RequirementResponder(CargarCaso, MaticesJson);
            s.AssignCase(new RequirementCaseId("caso-juntas-01"), personalidad);
            return s;
        }

        [Test]
        public void La_respuesta_del_caso_aparece_intacta_cuando_Revelado()
        {
            var s = SujetoListo(new PersonalityId("empatico"));

            var r = s.Respond(
                new Utterance("como llevan el rendimiento", 1f, 1f), IntentResult.Unknown(), Receptivity.Receptivo);

            Assert.AreEqual(RequirementOutcome.Revelado, r.Outcome);
            StringAssert.Contains(RespuestaEstadisticas, r.Reply.Text,
                "Con Outcome == Revelado, Reply.Text DEBE contener la respuesta del requerimiento intacta");
        }

        [Test]
        public void La_respuesta_del_caso_no_aparece_cuando_AunNoRevelado()
        {
            var s = SujetoListo(new PersonalityId("empatico"));

            var r = s.Respond(
                new Utterance("como llevan el rendimiento", 1f, 1f), IntentResult.Unknown(), Receptivity.NoReceptivo);

            Assert.AreEqual(RequirementOutcome.AunNoRevelado, r.Outcome);
            Assert.IsFalse(r.Reply.Text.Contains(RespuestaEstadisticas),
                "Con Outcome == AunNoRevelado, Reply.Text NO DEBE contener la respuesta real (el desvio no filtra el hecho)");
            Assert.IsFalse(string.IsNullOrWhiteSpace(r.Reply.Text),
                "El desvio nunca es silencio");
        }

        [Test]
        public void El_matiz_de_personalidad_cambia_el_prefijo_pero_nunca_el_dato()
        {
            var grosero = SujetoListo(new PersonalityId("grosero"));
            var empatico = SujetoListo(new PersonalityId("empatico"));
            var pregunta = new Utterance("como llevan el rendimiento", 1f, 1f);

            var textoGrosero = grosero.Respond(pregunta, IntentResult.Unknown(), Receptivity.Receptivo).Reply.Text;
            var textoEmpatico = empatico.Respond(pregunta, IntentResult.Unknown(), Receptivity.Receptivo).Reply.Text;

            Assert.AreNotEqual(textoGrosero, textoEmpatico,
                "Personalidades distintas DEBEN producir prefijos distintos");
            StringAssert.Contains(RespuestaEstadisticas, textoGrosero,
                "El dato (la respuesta) no cambia con la personalidad");
            StringAssert.Contains(RespuestaEstadisticas, textoEmpatico,
                "El dato (la respuesta) no cambia con la personalidad");
            StringAssert.StartsWith("Ya se lo dije, ", textoGrosero);
            StringAssert.StartsWith("Claro, con gusto. ", textoEmpatico);
        }
    }
}
