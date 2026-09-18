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
    }
}
