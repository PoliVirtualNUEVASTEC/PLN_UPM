using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using NpcAi.Core;
using NpcAi.Presentation.Fakes;

namespace NpcAi.Harness.Tests
{
    /// <summary>
    /// Fase 4 (design.md, "Orden TDD" — las fronteras al final): Requirement "Fronteras que
    /// SessionDirector no cruza" y Requirement "SessionDirector es C# puro". Incluye
    /// <c>DeclararTriaje</c>, que el In Scope de la propuesta pide aunque ningun requisito
    /// formal de la spec lo nombre (design.md, Open Questions).
    /// </summary>
    public sealed class SessionDirectorFronterasTests
    {
        private static readonly ClinicalCaseId[] Catalogo = { new ClinicalCaseId("caso-01") };
        private static readonly PersonalityId[] Personalidades = { new PersonalityId("grosero") };

        private static SessionDirector Construir(
            Action<string> declararTriaje = null,
            IScenarioObjective m9 = null)
        {
            return new SessionDirector(
                new EspiaClasificador(),
                new EspiaReceptividad(),
                new EspiaDialogo(),
                m9 ?? new EspiaObjetivo(),
                new EspiaClinico(),
                _ => { },
                declararTriaje ?? (_ => { }),
                Catalogo,
                Personalidades,
                1);
        }

        // --- Requirement: Fronteras que SessionDirector no cruza ---

        [Test]
        public void ConstructorPublico_no_declara_ningun_parametro_INpcPresenter()
        {
            var ctor = typeof(SessionDirector)
                .GetConstructors(BindingFlags.Public | BindingFlags.Instance)
                .Single();

            var tieneParametroPresentador = ctor.GetParameters()
                .Any(p => typeof(INpcPresenter).IsAssignableFrom(p.ParameterType));

            Assert.IsFalse(tieneParametroPresentador);

            // RecordingNpcPresenter existe y se instancia, pero no hay costura donde
            // inyectarlo en SessionDirector: la aserion estructural de arriba es la que
            // puede fallar de verdad si algun dia se agrega ese parametro.
            var presentador = new RecordingNpcPresenter();
            Assert.IsFalse(presentador.HasPlayed);
        }

        [Test]
        public void SesionCompleta_nunca_incrementa_el_contador_de_bandera_roja_del_espia_local()
        {
            var objetivo = new EspiaObjetivo();
            var director = Construir(m9: objetivo);

            director.IniciarSesion(Catalogo[0], Personalidades[0]);
            director.ProcesarTurno(new Utterance("hola", 1f, 1f));
            director.ProcesarAccion(PhysicalAction.Acercarse);
            director.DeclararTriaje("infeccioso");

            Assert.AreEqual(0, objetivo.BanderasRegistradas);
        }

        // --- Requirement: SessionDirector es C# puro ---

        [Test]
        public void Construible_y_operativo_sin_escena_Unity()
        {
            SessionDirector director = null;

            Assert.DoesNotThrow(() => director = Construir());

            // Antes de cualquier IniciarSesion: el estado no inicializado que los
            // contratos de M0 ya cubren (AD9) — SessionDirector no agrega guardas propias.
            Assert.DoesNotThrow(() => director.ProcesarTurno(new Utterance("hola", 1f, 1f)));
            Assert.DoesNotThrow(() => director.ProcesarAccion(PhysicalAction.Ninguna));
        }

        // --- In Scope de la propuesta; sin requisito formal en la spec (design.md, Open Questions) ---

        [Test]
        public void DeclararTriaje_hace_pass_through_crudo_al_delegado()
        {
            string recibido = null;
            var director = Construir(declararTriaje: categoria => recibido = categoria);

            director.DeclararTriaje("Infeccioso ");

            Assert.AreEqual("Infeccioso ", recibido);
        }
    }
}
