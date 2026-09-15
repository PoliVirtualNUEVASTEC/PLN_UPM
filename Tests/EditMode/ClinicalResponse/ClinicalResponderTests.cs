using NpcAi.Core;
using NpcAi.Core.Tests;
using NUnit.Framework;

namespace NpcAi.ClinicalResponse.Tests
{
    /// <summary>
    /// <see cref="ClinicalResponder"/> (implementacion real) contra el contrato de
    /// <see cref="IClinicalResponder"/>, mas las garantias propias de M15: el dato sobrevive
    /// intacto (AD5), un turno social no se maneja, y el matiz de personalidad es determinista.
    /// </summary>
    public class ClinicalResponderTests : ClinicalResponderContract
    {
        // JSON de prueba embebido (no toca Data/Cases/): 8 hechos, el minimo que exige
        // ClinicalCaseLoader. "inicio_sintoma" empareja con la UtteranceDePrueba fija que usa
        // ClinicalResponderContract ("desde cuando le duele la cabeza").
        private const string JsonDeCasoUno = @"{
            ""id"": ""caso-01"",
            ""paciente"": {
                ""edad"": 38,
                ""acompanamiento"": ""sola"",
                ""motivoConsulta"": ""Dolor de cabeza"",
                ""sintomas"": [""dolor de cabeza""],
                ""antecedentes"": [],
                ""alergias"": [""Tramadol""],
                ""medicacionActual"": [],
                ""signosVitales"": {
                    ""fcLpm"": 102, ""taMmHg"": ""163/99"", ""frRpm"": 23,
                    ""satO2Pct"": 94, ""glasgow"": ""15/15"", ""temperaturaC"": null
                }
            },
            ""hechos"": [
                { ""campo"": ""inicio_sintoma"", ""ejemplosDePregunta"": [""desde cuando"", ""hace cuanto""], ""respuesta"": ""Hace dos meses."" },
                { ""campo"": ""alergias"", ""ejemplosDePregunta"": [""es alergica"", ""alergica a algo""], ""respuesta"": ""Soy alergica al Tramadol."" },
                { ""campo"": ""dolor"", ""ejemplosDePregunta"": [""cuanto le duele"", ""escala de dolor""], ""respuesta"": ""El dolor esta en un 8 de 10."" },
                { ""campo"": ""mareo"", ""ejemplosDePregunta"": [""siente mareo"", ""se ha sentido mareada""], ""respuesta"": ""Si, me siento mareada."" },
                { ""campo"": ""habla"", ""ejemplosDePregunta"": [""dificultad para hablar"", ""se le traba la lengua""], ""respuesta"": ""Se me dificulta hablar."" },
                { ""campo"": ""antecedentes"", ""ejemplosDePregunta"": [""tiene antecedentes"", ""sufre de alguna enfermedad""], ""respuesta"": ""No tengo antecedentes."" },
                { ""campo"": ""acompanamiento"", ""ejemplosDePregunta"": [""viene sola"", ""quien la acompana""], ""respuesta"": ""Vengo sola."" },
                { ""campo"": ""medicacion"", ""ejemplosDePregunta"": [""toma algun medicamento"", ""que medicamentos toma""], ""respuesta"": ""No tomo ningun medicamento."" }
            ],
            ""clave"": { ""triajeEsperado"": ""II"", ""tiempoAtencion"": ""< 30 min"", ""banderasRojas"": [], ""cierreEsperado"": ""..."" }
        }";

        // Solo "caso-01" existe para este cargador de prueba: replica, a proposito, que un id
        // desconocido (p. ej. "no-existe", que usa ClinicalResponderContract) debe fallar a
        // cargar y dejar IsReady en false, tal como pasaria con la implementacion real contra
        // Data/Cases/ si el archivo no existe.
        private static string CargarJsonDePrueba(ClinicalCaseId id) =>
            id.Value == "caso-01" ? JsonDeCasoUno : null;

        protected override IClinicalResponder CreateSubject() =>
            new ClinicalResponder(CargarJsonDePrueba);

        private static readonly PersonalityId[] Personalidades =
        {
            new PersonalityId("grosero"),
            new PersonalityId("histerico"),
            new PersonalityId("introvertido"),
            new PersonalityId("empatico"),
        };

        [Test]
        public void La_respuesta_del_caso_siempre_aparece_intacta_en_el_texto()
        {
            foreach (var personalidad in Personalidades)
            {
                var s = CreateSubject();
                s.AssignCase(new ClinicalCaseId("caso-01"), personalidad);

                var r = s.Respond(new Utterance("¿es alergica a algo?", 1f, 1f), default);

                Assert.IsTrue(r.Handled, personalidad.Value);
                StringAssert.Contains("Soy alergica al Tramadol.", r.Reply.Text, personalidad.Value);
            }
        }

        [Test]
        public void Un_saludo_no_es_un_turno_clinico()
        {
            var s = CreateSubject();
            s.AssignCase(new ClinicalCaseId("caso-01"), new PersonalityId("empatico"));

            var r = s.Respond(new Utterance("Buenos dias, ¿como esta?", 1f, 1f), default);

            Assert.IsFalse(r.Handled);
        }

        [Test]
        public void PersonalityId_None_no_agrega_matiz()
        {
            var s = CreateSubject();
            s.AssignCase(new ClinicalCaseId("caso-01"), PersonalityId.None);

            var r = s.Respond(new Utterance("¿es alergica a algo?", 1f, 1f), default);

            Assert.AreEqual("Soy alergica al Tramadol.", r.Reply.Text);
        }

        [Test]
        public void Introvertido_tampoco_agrega_matiz()
        {
            var s = CreateSubject();
            s.AssignCase(new ClinicalCaseId("caso-01"), new PersonalityId("introvertido"));

            var r = s.Respond(new Utterance("¿es alergica a algo?", 1f, 1f), default);

            Assert.AreEqual("Soy alergica al Tramadol.", r.Reply.Text);
        }

        [Test]
        public void Mil_llamadas_con_la_misma_entrada_no_varian_para_ninguna_personalidad()
        {
            var entrada = new Utterance("desde cuando le duele la cabeza", 1f, 1.2f);
            var intent = new IntentResult(Intent.SolicitudRespetuosa, Tone.Respetuoso, 0.9f, 0f);

            foreach (var personalidad in Personalidades)
            {
                var s = CreateSubject();
                s.AssignCase(new ClinicalCaseId("caso-01"), personalidad);

                var referencia = s.Respond(entrada, intent);

                for (var i = 0; i < 1000; i++)
                {
                    var r = s.Respond(entrada, intent);
                    Assert.AreEqual(referencia.Handled, r.Handled, personalidad.Value + " iteracion " + i);
                    Assert.AreEqual(referencia.Reply.Text, r.Reply.Text, personalidad.Value + " iteracion " + i);
                }
            }
        }
    }
}
