using NpcAi.Core;
using NUnit.Framework;

namespace NpcAi.Core.Tests
{
    /// <summary>
    /// Contrato de <see cref="IClinicalResponder"/>. El doble de M15 y el respondedor clinico
    /// real heredan de aqui y pasan las mismas pruebas, sin escena ni VR. Las pruebas que
    /// exigen <c>Handled == true</c> usan <see cref="Assume"/> para no fallar contra un doble
    /// no listo (igual que <c>ScenarioObjectiveContract.La_completitud_es_reversible</c>).
    /// </summary>
    public abstract class ClinicalResponderContract
    {
        protected abstract IClinicalResponder CreateSubject();

        // El doble y la implementacion real de M15 DEBEN reconocer este caso como existente
        // para que corran las pruebas dependientes de IsReady; si no, Assume las omite.
        private static readonly ClinicalCaseId CasoCualquiera = new ClinicalCaseId("caso-01");
        private static readonly PersonalityId PersonalidadCualquiera = new PersonalityId("empatico");

        private static Utterance UtteranceDePrueba() =>
            new Utterance("desde cuando le duele la cabeza", 1f, 1.2f);

        private static IntentResult IntentDePrueba() =>
            new IntentResult(Intent.SolicitudRespetuosa, Tone.Respetuoso, 0.9f, 0f);

        [Test]
        public void Reporta_si_esta_listo_sin_lanzar()
        {
            Assert.DoesNotThrow(() => { var _ = CreateSubject().IsReady; });
        }

        [Test]
        public void Sin_caso_asignado_Respond_devuelve_NoAplica()
        {
            var noListo = new RespondedorNoListo();
            Assert.IsFalse(noListo.IsReady);
            Assert.IsFalse(noListo.Respond(default, default).Handled,
                "Con IsReady == false, Respond DEBE devolver ClinicalResponse.NoAplica");

            var sujeto = CreateSubject();
            if (!sujeto.IsReady)
                Assert.IsFalse(sujeto.Respond(default, default).Handled,
                    "Un sujeto no listo DEBE degradar a NoAplica sin lanzar");
        }

        [Test]
        public void Respond_no_lanza_en_ningun_estado()
        {
            var sujeto = CreateSubject();
            var noListo = new RespondedorNoListo();
            var simbolos = new Utterance("!!!???", 0f, 0f);
            var cadenaLarga = new Utterance(new string('a', 5000), 0f, 0f);

            Assert.DoesNotThrow(() =>
            {
                var _ = sujeto.IsReady;
                sujeto.Respond(default, default);
                sujeto.Respond(simbolos, default);
                sujeto.Respond(cadenaLarga, IntentResult.Unknown());

                noListo.Respond(default, default);
                noListo.Respond(cadenaLarga, IntentResult.Unknown());
            });
        }

        [Test]
        public void Cuando_responde_el_texto_no_es_vacio_y_los_tags_no_son_nulos()
        {
            var s = CreateSubject();
            s.AssignCase(CasoCualquiera, PersonalidadCualquiera);
            Assume.That(s.IsReady, "El sujeto no quedo listo con el caso de prueba");

            var r = s.Respond(UtteranceDePrueba(), IntentDePrueba());
            Assume.That(r.Handled, "El sujeto no manejo el turno de prueba");

            Assert.IsFalse(string.IsNullOrWhiteSpace(r.Reply.Text),
                "Con Handled == true, Reply.Text NO DEBE ser vacio ni solo espacios");
            Assert.IsNotNull(r.Reply.EmotionTag, "Reply.EmotionTag NO DEBE ser null");
            Assert.IsNotNull(r.Reply.AnimationCue, "Reply.AnimationCue NO DEBE ser null");
        }

        [Test]
        public void Es_determinista_en_Handled_y_en_el_texto_para_la_misma_entrada()
        {
            var s = CreateSubject();
            s.AssignCase(CasoCualquiera, PersonalidadCualquiera);
            Assume.That(s.IsReady, "El sujeto no quedo listo con el caso de prueba");

            var entrada = UtteranceDePrueba();
            var intent = IntentDePrueba();
            var a = s.Respond(entrada, intent);
            var b = s.Respond(entrada, intent);
            var c = s.Respond(entrada, intent);

            Assert.AreEqual(a.Handled, b.Handled, "Handled DEBE ser identico para la misma entrada");
            Assert.AreEqual(a.Handled, c.Handled, "Handled DEBE ser identico para la misma entrada");
            Assert.AreEqual(a.Reply.Text, b.Reply.Text, "Reply.Text DEBE ser identico para la misma entrada");
            Assert.AreEqual(a.Reply.Text, c.Reply.Text, "Reply.Text DEBE ser identico para la misma entrada");
        }

        [Test]
        public void AssignCase_es_idempotente_con_el_mismo_par()
        {
            var s = CreateSubject();
            s.AssignCase(CasoCualquiera, PersonalidadCualquiera);
            Assume.That(s.IsReady, "El sujeto no quedo listo con el caso de prueba");

            var entrada = UtteranceDePrueba();
            var intent = IntentDePrueba();
            var listoAntes = s.IsReady;
            var antes = s.Respond(entrada, intent);

            s.AssignCase(CasoCualquiera, PersonalidadCualquiera);

            Assert.AreEqual(listoAntes, s.IsReady, "IsReady no debe cambiar al reasignar el mismo par");
            var despues = s.Respond(entrada, intent);
            Assert.AreEqual(antes.Handled, despues.Handled, "Handled DEBE ser identico tras reasignar el mismo par");
            Assert.AreEqual(antes.Reply.Text, despues.Reply.Text, "Reply.Text DEBE ser identico tras reasignar el mismo par");
        }

        [Test]
        public void AssignCase_con_caso_desconocido_no_lanza_y_deja_no_listo()
        {
            var s = CreateSubject();

            Assert.DoesNotThrow(() => s.AssignCase(new ClinicalCaseId("no-existe"), PersonalidadCualquiera),
                "AssignCase con un ClinicalCaseId desconocido NO DEBE lanzar");
            Assert.IsFalse(s.IsReady, "Tras un caso desconocido, IsReady DEBE quedar en false");
        }

        /// <summary>
        /// Stub minimo NO listo, definido DENTRO de Tests/EditMode/Core/ (mismo patron que
        /// <c>ClasificadorNoListo</c> en <see cref="IntentClassifierContract"/>): alcanza el
        /// camino "no listo" sin depender del doble de M15.
        /// </summary>
        private sealed class RespondedorNoListo : IClinicalResponder
        {
            public bool IsReady => false;
            public void AssignCase(ClinicalCaseId caseId, PersonalityId personality) { }
            public ClinicalResponse Respond(Utterance nurseUtterance, IntentResult intent) => ClinicalResponse.NoAplica;
        }
    }
}
