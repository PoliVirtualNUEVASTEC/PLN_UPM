using NUnit.Framework;
using NpcAi.Core;
using NpcAi.ClinicalResponse.Fakes;

namespace NpcAi.Harness.Tests
{
    /// <summary>
    /// Fase 3 (design.md, "Orden TDD"): Requirement "Centinelas para senales parciales",
    /// Requirement "Silencio de la accion fisica sola" y Requirement "Enrutado
    /// clinico/social". Una sesion ya iniciada es la precondicion de estos escenarios.
    /// </summary>
    public sealed class SessionDirectorTurnoTests
    {
        private static readonly ClinicalCaseId[] Catalogo = { new ClinicalCaseId("caso-01") };
        private static readonly PersonalityId[] Personalidades = { new PersonalityId("grosero") };

        // Valores distinguibles de default en cada campo: si el director enviara default, o
        // reconstruyera el valor en lugar de reenviar el que recibio, la comparacion fallaria.
        private static readonly ReceptivityChange CambioDeM4 =
            new ReceptivityChange(Receptivity.Receptivo, Receptivity.NoReceptivo, 7, "PRUEBA_M4");

        private static readonly IntentResult IntentDeM2 =
            new IntentResult(Intent.SolicitudRespetuosa, Tone.Respetuoso, 0.85f, 12.5f);

        private static SessionDirector Construir(
            IIntentClassifier m2 = null,
            IReceptivityEngine m4 = null,
            IDialogueGenerator m6 = null,
            IScenarioObjective m9 = null,
            IClinicalResponder m15 = null)
        {
            var director = new SessionDirector(
                m2 ?? new EspiaClasificador(),
                m4 ?? new EspiaReceptividad(),
                m6 ?? new EspiaDialogo(),
                m9 ?? new EspiaObjetivo(),
                m15 ?? new EspiaClinico(),
                _ => { },
                _ => { },
                Catalogo,
                Personalidades,
                1);

            director.IniciarSesion(Catalogo[0], Personalidades[0]);
            return director;
        }

        // --- Requirement: Centinelas para senales parciales ---

        [Test]
        public void SoloUtterance_evalua_con_PhysicalAction_Ninguna()
        {
            var m4 = new EspiaReceptividad();
            var director = Construir(m4: m4);

            director.ProcesarTurno(new Utterance("hola", 1f, 1f));

            Assert.AreEqual(1, m4.Evaluaciones.Count);
            Assert.AreEqual(PhysicalAction.Ninguna, m4.Evaluaciones[0].Action);
        }

        [Test]
        public void SoloPhysicalAction_evalua_con_IntentResult_Unknown()
        {
            var m4 = new EspiaReceptividad();
            var m2 = new EspiaClasificador();
            var director = Construir(m4: m4, m2: m2);

            director.ProcesarAccion(PhysicalAction.ContactoVisual);

            Assert.AreEqual(1, m4.Evaluaciones.Count);
            Assert.AreEqual(Intent.Desconocida, m4.Evaluaciones[0].Intent.Intent);
            Assert.AreEqual(0f, m4.Evaluaciones[0].Intent.Confidence);
            Assert.AreEqual(0, m2.Invocaciones);
        }

        // --- Requirement: Silencio de la accion fisica sola ---

        [Test]
        public void AccionFisica_mueve_receptividad()
        {
            var m4 = new EspiaReceptividad();
            var director = Construir(m4: m4);

            director.ProcesarAccion(PhysicalAction.Acercarse);

            Assert.AreEqual(Receptivity.Receptivo, m4.Current);
        }

        [Test]
        public void AccionFisica_no_produce_respuesta_hablada()
        {
            var m15 = new EspiaClinico();
            var m6 = new EspiaDialogo();
            var director = Construir(m15: m15, m6: m6);

            var respuesta = director.ProcesarAccion(PhysicalAction.Acercarse);

            Assert.IsNull(respuesta);
            Assert.AreEqual(0, m15.Invocaciones);
            Assert.AreEqual(0, m6.Invocaciones);
        }

        // --- Requirement: Enrutado clinico/social ---

        [Test]
        public void Turno_clinico_manejado_no_llama_Generate()
        {
            var m15 = new ScriptedClinicalResponder();
            var m6 = new EspiaDialogo();
            var director = Construir(m15: m15, m6: m6);

            var respuesta = director.ProcesarTurno(new Utterance("desde cuando le duele", 1f, 1f));

            Assert.AreEqual("Desde hace un par de dias, doctora.", respuesta.Text);
            Assert.AreEqual(0, m6.Invocaciones);
        }

        [Test]
        public void Turno_social_llama_Generate_con_receptividad_actual()
        {
            var m15 = new ScriptedClinicalResponder();
            var m4 = new EspiaReceptividad();
            var m6 = new EspiaDialogo();
            var director = Construir(m15: m15, m4: m4, m6: m6);

            var respuesta = director.ProcesarTurno(new Utterance("hola futbol", 1f, 1f));

            Assert.AreEqual(1, m6.Invocaciones);
            Assert.AreEqual(m4.Current, m6.Llamadas[0].Receptivity);
            Assert.AreEqual(Personalidades[0], m6.Llamadas[0].Personality);
            Assert.AreEqual(m6.Respuesta.Text, respuesta.Text);
        }

        // --- Cierre de W3 (AD10): M9 recibe el cambio de M4, una vez por llamada, en ambas ramas ---

        [Test]
        public void Turno_clinico_manejado_notifica_a_M9_una_vez_con_el_cambio_de_M4()
        {
            var m4 = new EspiaReceptividad { CambioProgramado = CambioDeM4 };
            var m9 = new EspiaObjetivo();
            var m6 = new EspiaDialogo();
            var m15 = new EspiaClinico
            {
                Respuesta = new Core.ClinicalResponse(
                    true, new NpcReply("[espia-clinico] respuesta clinica", "neutral", "idle")),
            };
            var director = Construir(m4: m4, m9: m9, m6: m6, m15: m15);

            director.ProcesarTurno(new Utterance("desde cuando le duele", 1f, 1f));

            Assert.AreEqual(0, m6.Invocaciones); // rama clinica: la precondicion de esta prueba
            Assert.AreEqual(1, m9.Notificaciones.Count);
            Assert.AreEqual(CambioDeM4, m9.Notificaciones[0]);
        }

        [Test]
        public void Turno_social_notifica_a_M9_una_vez_con_el_cambio_de_M4()
        {
            var m4 = new EspiaReceptividad { CambioProgramado = CambioDeM4 };
            var m9 = new EspiaObjetivo();
            var m6 = new EspiaDialogo();
            var director = Construir(m4: m4, m9: m9, m6: m6); // M15 por defecto: NoAplica

            director.ProcesarTurno(new Utterance("hola futbol", 1f, 1f));

            Assert.AreEqual(1, m6.Invocaciones); // rama social: la precondicion de esta prueba
            Assert.AreEqual(1, m9.Notificaciones.Count);
            Assert.AreEqual(CambioDeM4, m9.Notificaciones[0]);
        }

        [Test]
        public void ProcesarAccion_notifica_a_M9_una_vez_con_el_cambio_de_M4_y_no_responde()
        {
            var m4 = new EspiaReceptividad { CambioProgramado = CambioDeM4 };
            var m9 = new EspiaObjetivo();
            var director = Construir(m4: m4, m9: m9);

            var respuesta = director.ProcesarAccion(PhysicalAction.Acercarse);

            Assert.IsNull(respuesta);
            Assert.AreEqual(1, m9.Notificaciones.Count);
            Assert.AreEqual(CambioDeM4, m9.Notificaciones[0]);
        }

        // --- Cierre de W4: el flujo Classify -> IntentResult ---

        [Test]
        public void Turno_clasifica_una_vez_con_el_texto_de_la_utterance()
        {
            var m2 = new EspiaClasificador { Resultado = IntentDeM2 };
            var director = Construir(m2: m2);

            director.ProcesarTurno(new Utterance("buenos dias doctora", 0.9f, 2f));

            Assert.AreEqual(1, m2.Invocaciones);
            Assert.AreEqual("buenos dias doctora", m2.UltimoTexto);
        }

        [Test]
        public void Turno_social_propaga_el_IntentResult_de_M2_a_M4_M15_y_M6()
        {
            var m2 = new EspiaClasificador { Resultado = IntentDeM2 };
            var m4 = new EspiaReceptividad();
            var m15 = new EspiaClinico(); // por defecto NoAplica: turno social
            var m6 = new EspiaDialogo();
            var director = Construir(m2: m2, m4: m4, m15: m15, m6: m6);

            director.ProcesarTurno(new Utterance("hola futbol", 1f, 1f));

            Assert.AreEqual(1, m4.Evaluaciones.Count);
            Assert.AreEqual(IntentDeM2, m4.Evaluaciones[0].Intent);
            Assert.AreEqual(1, m15.Invocaciones);
            Assert.AreEqual(IntentDeM2, m15.UltimoIntent);
            Assert.AreEqual(1, m6.Invocaciones);
            Assert.AreEqual(IntentDeM2, m6.Llamadas[0].Intent);
        }

        // --- Cierre de W5: M6 recibe la receptividad que M4 reporta, no una constante ---

        // Neutral (el valor por defecto) queda fuera a proposito: no discriminaria una constante.
        [TestCase(Receptivity.NoReceptivo)]
        [TestCase(Receptivity.Receptivo)]
        public void Turno_social_entrega_a_Generate_la_receptividad_que_M4_reporta_como_Current(
            Receptivity receptividad)
        {
            var m4 = new EspiaReceptividad();
            var m6 = new EspiaDialogo();
            var director = Construir(m4: m4, m6: m6);

            // Despues de Construir: IniciarSesion llama Reset, que devuelve Current a Neutral.
            m4.Current = receptividad;

            director.ProcesarTurno(new Utterance("hola futbol", 1f, 1f));

            Assert.AreEqual(1, m6.Invocaciones);
            Assert.AreEqual(receptividad, m6.Llamadas[0].Receptivity);
            Assert.AreEqual(Personalidades[0], director.PersonalidadActual);
            Assert.AreEqual(director.PersonalidadActual, m6.Llamadas[0].Personality);
        }
    }
}
