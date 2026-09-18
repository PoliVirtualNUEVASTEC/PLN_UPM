using System;
using System.Collections.Generic;
using NUnit.Framework;
using NpcAi.Core;

namespace NpcAi.Harness.Tests
{
    /// <summary>
    /// Fase 2 (design.md, "Orden TDD"): constructor + validacion (AD3, Architecture Decisions),
    /// Requirement "Secuencia de inicio de sesion" y Requirement "Seleccion reproducible por
    /// semilla". Primero el arranque y la seleccion: son la precondicion de todo lo demas.
    /// </summary>
    public sealed class SessionDirectorSesionTests
    {
        private static readonly ClinicalCaseId[] Catalogo =
        {
            new ClinicalCaseId("caso-01"),
            new ClinicalCaseId("caso-02"),
            new ClinicalCaseId("caso-03"),
        };

        private static readonly PersonalityId[] Personalidades =
        {
            new PersonalityId("grosero"),
            new PersonalityId("amable"),
        };

        private static SessionDirector Construir(
            IIntentClassifier m2 = null,
            IReceptivityEngine m4 = null,
            IDialogueGenerator m6 = null,
            IScenarioObjective m9 = null,
            IClinicalResponder m15 = null,
            Action<ClinicalCaseId> asignarCaso = null,
            Action<string> declararTriaje = null,
            IReadOnlyList<ClinicalCaseId> casos = null,
            IReadOnlyList<PersonalityId> personalidades = null,
            int semilla = 1234)
        {
            return new SessionDirector(
                m2 ?? new EspiaClasificador(),
                m4 ?? new EspiaReceptividad(),
                m6 ?? new EspiaDialogo(),
                m9 ?? new EspiaObjetivo(),
                m15 ?? new EspiaClinico(),
                asignarCaso ?? (_ => { }),
                declararTriaje ?? (_ => { }),
                casos ?? Catalogo,
                personalidades ?? Personalidades,
                semilla);
        }

        // --- AD3: validacion en construccion. Los metodos nunca lanzan; solo el constructor. ---

        [Test]
        public void Constructor_lanza_ArgumentNullException_con_m2_nulo()
        {
            Assert.Throws<ArgumentNullException>(() => new SessionDirector(
                null, new EspiaReceptividad(), new EspiaDialogo(), new EspiaObjetivo(), new EspiaClinico(),
                _ => { }, _ => { }, Catalogo, Personalidades, 1));
        }

        [Test]
        public void Constructor_lanza_ArgumentNullException_con_m4_nulo()
        {
            Assert.Throws<ArgumentNullException>(() => new SessionDirector(
                new EspiaClasificador(), null, new EspiaDialogo(), new EspiaObjetivo(), new EspiaClinico(),
                _ => { }, _ => { }, Catalogo, Personalidades, 1));
        }

        [Test]
        public void Constructor_lanza_ArgumentNullException_con_m6_nulo()
        {
            Assert.Throws<ArgumentNullException>(() => new SessionDirector(
                new EspiaClasificador(), new EspiaReceptividad(), null, new EspiaObjetivo(), new EspiaClinico(),
                _ => { }, _ => { }, Catalogo, Personalidades, 1));
        }

        [Test]
        public void Constructor_lanza_ArgumentNullException_con_m9_nulo()
        {
            Assert.Throws<ArgumentNullException>(() => new SessionDirector(
                new EspiaClasificador(), new EspiaReceptividad(), new EspiaDialogo(), null, new EspiaClinico(),
                _ => { }, _ => { }, Catalogo, Personalidades, 1));
        }

        [Test]
        public void Constructor_lanza_ArgumentNullException_con_m15_nulo()
        {
            Assert.Throws<ArgumentNullException>(() => new SessionDirector(
                new EspiaClasificador(), new EspiaReceptividad(), new EspiaDialogo(), new EspiaObjetivo(), null,
                _ => { }, _ => { }, Catalogo, Personalidades, 1));
        }

        [Test]
        public void Constructor_lanza_ArgumentNullException_con_delegado_asignarCaso_nulo()
        {
            Assert.Throws<ArgumentNullException>(() => new SessionDirector(
                new EspiaClasificador(), new EspiaReceptividad(), new EspiaDialogo(), new EspiaObjetivo(), new EspiaClinico(),
                null, _ => { }, Catalogo, Personalidades, 1));
        }

        [Test]
        public void Constructor_lanza_ArgumentNullException_con_delegado_declararTriaje_nulo()
        {
            Assert.Throws<ArgumentNullException>(() => new SessionDirector(
                new EspiaClasificador(), new EspiaReceptividad(), new EspiaDialogo(), new EspiaObjetivo(), new EspiaClinico(),
                _ => { }, null, Catalogo, Personalidades, 1));
        }

        [Test]
        public void Constructor_lanza_ArgumentException_con_catalogo_de_casos_nulo()
        {
            Assert.Throws<ArgumentException>(() => new SessionDirector(
                new EspiaClasificador(), new EspiaReceptividad(), new EspiaDialogo(), new EspiaObjetivo(), new EspiaClinico(),
                _ => { }, _ => { }, null, Personalidades, 1));
        }

        [Test]
        public void Constructor_lanza_ArgumentException_con_catalogo_de_casos_vacio()
        {
            Assert.Throws<ArgumentException>(() => new SessionDirector(
                new EspiaClasificador(), new EspiaReceptividad(), new EspiaDialogo(), new EspiaObjetivo(), new EspiaClinico(),
                _ => { }, _ => { }, Array.Empty<ClinicalCaseId>(), Personalidades, 1));
        }

        [Test]
        public void Constructor_lanza_ArgumentException_con_catalogo_de_personalidades_nulo()
        {
            Assert.Throws<ArgumentException>(() => new SessionDirector(
                new EspiaClasificador(), new EspiaReceptividad(), new EspiaDialogo(), new EspiaObjetivo(), new EspiaClinico(),
                _ => { }, _ => { }, Catalogo, null, 1));
        }

        [Test]
        public void Constructor_lanza_ArgumentException_con_catalogo_de_personalidades_vacio()
        {
            Assert.Throws<ArgumentException>(() => new SessionDirector(
                new EspiaClasificador(), new EspiaReceptividad(), new EspiaDialogo(), new EspiaObjetivo(), new EspiaClinico(),
                _ => { }, _ => { }, Catalogo, Array.Empty<PersonalityId>(), 1));
        }

        // --- Requirement: Secuencia de inicio de sesion ---

        [Test]
        public void IniciarSesion_dispara_Reset_AssignCase_M9_AssignCase_M15_con_mismo_caso()
        {
            var m4 = new EspiaReceptividad();
            var objetivo = new EspiaObjetivo();
            var clinico = new EspiaClinico();

            var director = Construir(m4: m4, m9: objetivo, m15: clinico, asignarCaso: objetivo.AssignCase);

            var caso = new ClinicalCaseId("caso-02");
            var personalidad = new PersonalityId("amable");

            director.IniciarSesion(caso, personalidad);

            Assert.AreEqual(1, m4.Resets.Count);
            Assert.AreEqual(personalidad, m4.Resets[0]);
            Assert.AreEqual(1, objetivo.CasosAsignados);
            Assert.AreEqual(1, clinico.Asignaciones);
            Assert.AreEqual(caso, objetivo.UltimoCaso);
            Assert.AreEqual(objetivo.UltimoCaso, clinico.UltimoCaso);
            Assert.AreEqual(caso, director.CasoActual);
            Assert.AreEqual(personalidad, director.PersonalidadActual);
        }

        // --- Requirement: Seleccion reproducible por semilla ---

        [Test]
        public void MismaSemillaYCatalogo_elige_mismo_par()
        {
            var a = Construir(semilla: 1234);
            var b = Construir(semilla: 1234);

            for (var sesion = 0; sesion < 20; sesion++)
            {
                a.IniciarSesion();
                b.IniciarSesion();

                Assert.AreEqual(a.CasoActual, b.CasoActual, $"sesion {sesion}");
                Assert.AreEqual(a.PersonalidadActual, b.PersonalidadActual, $"sesion {sesion}");
            }
        }

        [Test]
        public void SesionNueva_no_repite_caso_anterior()
        {
            for (var semilla = 0; semilla < 20; semilla++)
            {
                var director = Construir(semilla: semilla);
                director.IniciarSesion(new ClinicalCaseId("caso-01"), new PersonalityId("grosero"));

                director.IniciarSesion();

                Assert.AreNotEqual(new ClinicalCaseId("caso-01"), director.CasoActual, $"semilla {semilla}");
            }
        }

        [Test]
        public void SesionNueva_con_catalogo_de_un_caso_no_lanza_y_repite()
        {
            var unSoloCaso = new[] { new ClinicalCaseId("caso-01") };
            var director = Construir(casos: unSoloCaso, semilla: 7);

            director.IniciarSesion(new ClinicalCaseId("caso-01"), new PersonalityId("grosero"));

            Assert.DoesNotThrow(() => director.IniciarSesion());
            Assert.AreEqual(new ClinicalCaseId("caso-01"), director.CasoActual);
        }

        // --- Prueba de diseno no nombrada por la spec (design.md, Testing Strategy) ---

        [Test]
        public void Progreso_refleja_Progress01_del_objetivo_sin_cachear()
        {
            var objetivo = new EspiaObjetivo { Progress01 = 0.25f };
            var director = Construir(m9: objetivo, asignarCaso: objetivo.AssignCase);

            Assert.AreEqual(0.25f, director.Progreso);

            objetivo.Progress01 = 0.75f;

            Assert.AreEqual(0.75f, director.Progreso);
        }
    }
}
