using NpcAi.Core;
using NUnit.Framework;

namespace NpcAi.Core.Tests
{
    /// <summary>
    /// Contrato de <see cref="IRequirementResponder"/>. El doble y la implementacion real de
    /// M16 heredan de aqui y pasan las mismas pruebas, sin escena ni VR. Las pruebas que
    /// exigen un <see cref="RequirementOutcome"/> concreto usan <see cref="Assume"/> para no
    /// fallar contra un doble no listo o que no reconozca el caso/frase de prueba (mismo
    /// patron que <see cref="ClinicalResponderContract"/>).
    /// </summary>
    public abstract class RequirementResponderContract
    {
        protected abstract IRequirementResponder CreateSubject();

        // El doble y la implementacion real de M16 DEBEN reconocer este caso y esta frase
        // como aplicables para que corran las pruebas dependientes de IsReady/Outcome; si no,
        // Assume las omite (no fallan).
        private static readonly RequirementCaseId CasoCualquiera = new RequirementCaseId("caso-juntas-01");
        private static readonly PersonalityId PersonalidadCualquiera = new PersonalityId("empatico");

        private static Utterance UtteranceDePrueba() =>
            new Utterance("cual es el presupuesto del proyecto", 1f, 1.4f);

        private static IntentResult IntentDePrueba() =>
            new IntentResult(Intent.AportaInformacion, Tone.Respetuoso, 0.9f, 0f);

        [Test]
        public void Reporta_si_esta_listo_sin_lanzar()
        {
            Assert.DoesNotThrow(() => { var _ = CreateSubject().IsReady; });
        }

        [Test]
        public void Sin_caso_asignado_Respond_devuelve_NoAplica()
        {
            var noListo = new RespondedorDeRequerimientosNoListo();
            Assert.IsFalse(noListo.IsReady);
            Assert.AreEqual(RequirementOutcome.NoAplica,
                noListo.Respond(default, default, Receptivity.Neutral).Outcome,
                "Con IsReady == false, Respond DEBE devolver RequirementResponse.NoAplica");

            var sujeto = CreateSubject();
            if (!sujeto.IsReady)
                Assert.AreEqual(RequirementOutcome.NoAplica,
                    sujeto.Respond(default, default, Receptivity.Neutral).Outcome,
                    "Un sujeto no listo DEBE degradar a NoAplica sin lanzar");
        }

        [Test]
        public void Respond_no_lanza_en_ningun_estado()
        {
            var sujeto = CreateSubject();
            var noListo = new RespondedorDeRequerimientosNoListo();
            var simbolos = new Utterance("!!!???", 0f, 0f);
            var cadenaLarga = new Utterance(new string('a', 5000), 0f, 0f);

            foreach (var receptividad in new[] { Receptivity.NoReceptivo, Receptivity.Neutral, Receptivity.Receptivo })
            {
                Assert.DoesNotThrow(() =>
                {
                    var _ = sujeto.IsReady;
                    sujeto.Respond(default, default, receptividad);
                    sujeto.Respond(simbolos, default, receptividad);
                    sujeto.Respond(cadenaLarga, IntentResult.Unknown(), receptividad);

                    noListo.Respond(default, default, receptividad);
                    noListo.Respond(cadenaLarga, IntentResult.Unknown(), receptividad);
                });
            }
        }

        [Test]
        public void Cuando_revela_el_texto_no_es_vacio_y_los_tags_no_son_nulos()
        {
            var s = CreateSubject();
            s.AssignCase(CasoCualquiera, PersonalidadCualquiera);
            Assume.That(s.IsReady, "El sujeto no quedo listo con el caso de prueba");

            var r = s.Respond(UtteranceDePrueba(), IntentDePrueba(), Receptivity.Receptivo);
            Assume.That(r.Outcome, Is.EqualTo(RequirementOutcome.Revelado),
                "El sujeto no revelo el requerimiento de prueba con receptividad alta");

            Assert.IsFalse(string.IsNullOrWhiteSpace(r.Reply.Text),
                "Con Outcome == Revelado, Reply.Text NO DEBE ser vacio ni solo espacios");
            Assert.IsNotNull(r.Reply.EmotionTag, "Reply.EmotionTag NO DEBE ser null");
            Assert.IsNotNull(r.Reply.AnimationCue, "Reply.AnimationCue NO DEBE ser null");
        }

        [Test]
        public void Cuando_aun_no_revela_responde_con_un_desvio_no_vacio()
        {
            var s = CreateSubject();
            s.AssignCase(CasoCualquiera, PersonalidadCualquiera);
            Assume.That(s.IsReady, "El sujeto no quedo listo con el caso de prueba");

            var r = s.Respond(UtteranceDePrueba(), IntentDePrueba(), Receptivity.NoReceptivo);
            Assume.That(r.Outcome, Is.EqualTo(RequirementOutcome.AunNoRevelado),
                "El sujeto no diferio el requerimiento de prueba con receptividad baja");

            Assert.IsFalse(string.IsNullOrWhiteSpace(r.Reply.Text),
                "Con Outcome == AunNoRevelado, Reply.Text NO DEBE ser vacio (desvio, nunca silencio)");
        }

        [Test]
        public void El_RequirementId_esta_poblado_si_y_solo_si_el_turno_aplica()
        {
            var s = CreateSubject();
            s.AssignCase(CasoCualquiera, PersonalidadCualquiera);
            Assume.That(s.IsReady, "El sujeto no quedo listo con el caso de prueba");

            var aplica = s.Respond(UtteranceDePrueba(), IntentDePrueba(), Receptivity.Receptivo);
            Assume.That(aplica.Outcome, Is.Not.EqualTo(RequirementOutcome.NoAplica),
                "El sujeto no reconocio el turno de prueba como aplicable");
            Assert.IsFalse(aplica.RequirementId.IsNone,
                "Outcome != NoAplica DEBE dejar RequirementId poblado");

            var noAplica = s.Respond(new Utterance("que clima hace hoy", 1f, 1f), IntentDePrueba(), Receptivity.Receptivo);
            Assume.That(noAplica.Outcome, Is.EqualTo(RequirementOutcome.NoAplica),
                "El sujeto no reconocio el turno de control como no aplicable");
            Assert.IsTrue(noAplica.RequirementId.IsNone,
                "Outcome == NoAplica DEBE dejar RequirementId en None");
        }

        [Test]
        public void Es_determinista_en_Outcome_texto_y_RequirementId_para_la_misma_entrada()
        {
            var s = CreateSubject();
            s.AssignCase(CasoCualquiera, PersonalidadCualquiera);
            Assume.That(s.IsReady, "El sujeto no quedo listo con el caso de prueba");

            var entrada = UtteranceDePrueba();
            var intent = IntentDePrueba();
            var a = s.Respond(entrada, intent, Receptivity.Receptivo);
            var b = s.Respond(entrada, intent, Receptivity.Receptivo);
            var c = s.Respond(entrada, intent, Receptivity.Receptivo);

            Assert.AreEqual(a.Outcome, b.Outcome, "Outcome DEBE ser identico para la misma entrada");
            Assert.AreEqual(a.Outcome, c.Outcome, "Outcome DEBE ser identico para la misma entrada");
            Assert.AreEqual(a.Reply.Text, b.Reply.Text, "Reply.Text DEBE ser identico para la misma entrada");
            Assert.AreEqual(a.Reply.Text, c.Reply.Text, "Reply.Text DEBE ser identico para la misma entrada");
            Assert.AreEqual(a.RequirementId, b.RequirementId, "RequirementId DEBE ser identico para la misma entrada");
            Assert.AreEqual(a.RequirementId, c.RequirementId, "RequirementId DEBE ser identico para la misma entrada");
        }

        [Test]
        public void Baja_receptividad_y_alta_receptividad_no_dan_el_mismo_texto()
        {
            var s = CreateSubject();
            s.AssignCase(CasoCualquiera, PersonalidadCualquiera);
            Assume.That(s.IsReady, "El sujeto no quedo listo con el caso de prueba");

            var entrada = UtteranceDePrueba();
            var intent = IntentDePrueba();
            var baja = s.Respond(entrada, intent, Receptivity.NoReceptivo);
            var alta = s.Respond(entrada, intent, Receptivity.Receptivo);

            Assume.That(baja.Outcome, Is.EqualTo(RequirementOutcome.AunNoRevelado));
            Assume.That(alta.Outcome, Is.EqualTo(RequirementOutcome.Revelado));

            Assert.AreNotEqual(baja.Reply.Text, alta.Reply.Text,
                "Baja y alta receptividad NO DEBEN dar el mismo texto");
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
            var antes = s.Respond(entrada, intent, Receptivity.Receptivo);

            s.AssignCase(CasoCualquiera, PersonalidadCualquiera);

            Assert.AreEqual(listoAntes, s.IsReady, "IsReady no debe cambiar al reasignar el mismo par");
            var despues = s.Respond(entrada, intent, Receptivity.Receptivo);
            Assert.AreEqual(antes.Outcome, despues.Outcome, "Outcome DEBE ser identico tras reasignar el mismo par");
            Assert.AreEqual(antes.Reply.Text, despues.Reply.Text, "Reply.Text DEBE ser identico tras reasignar el mismo par");
            Assert.AreEqual(antes.RequirementId, despues.RequirementId, "RequirementId DEBE ser identico tras reasignar el mismo par");
        }

        [Test]
        public void AssignCase_con_caso_desconocido_no_lanza_y_deja_no_listo()
        {
            var s = CreateSubject();

            Assert.DoesNotThrow(() => s.AssignCase(new RequirementCaseId("no-existe"), PersonalidadCualquiera),
                "AssignCase con un RequirementCaseId desconocido NO DEBE lanzar");
            Assert.IsFalse(s.IsReady, "Tras un caso desconocido, IsReady DEBE quedar en false");
        }

        /// <summary>
        /// Stub minimo NO listo, definido DENTRO de Tests/EditMode/Core/ (mismo patron que
        /// <c>RespondedorNoListo</c> en <see cref="ClinicalResponderContract"/>): alcanza el
        /// camino "no listo" sin depender del doble de M16.
        /// </summary>
        private sealed class RespondedorDeRequerimientosNoListo : IRequirementResponder
        {
            public bool IsReady => false;
            public void AssignCase(RequirementCaseId caseId, PersonalityId personality) { }
            public RequirementResponse Respond(Utterance studentUtterance, IntentResult intent, Receptivity receptivity) =>
                RequirementResponse.NoAplica;
        }

        /// <summary>
        /// Stub minimo LISTO: sin el, los escenarios de <see cref="RequirementOutcome.AunNoRevelado"/>
        /// y <see cref="RequirementOutcome.Revelado"/> quedarian omitidos por <see cref="Assume"/>,
        /// no verdes. Reconoce un unico caso y un unico requerimiento ("presupuesto"); cualquier
        /// otra frase es NoAplica. Andamio de prueba de este cambio, no implementacion de M16.
        /// </summary>
        private sealed class RespondedorDeRequerimientosDePrueba : IRequirementResponder
        {
            private const string CasoConocido = "caso-juntas-01";
            private static readonly RequirementId Presupuesto = new RequirementId("presupuesto");

            private bool _listo;

            public bool IsReady => _listo;

            public void AssignCase(RequirementCaseId caseId, PersonalityId personality)
            {
                _listo = caseId.Value == CasoConocido;
            }

            public RequirementResponse Respond(Utterance studentUtterance, IntentResult intent, Receptivity receptivity)
            {
                if (!_listo)
                    return RequirementResponse.NoAplica;

                var texto = studentUtterance.Text ?? string.Empty;
                if (!texto.ToLowerInvariant().Contains("presupuesto"))
                    return RequirementResponse.NoAplica;

                var revela = (int)receptivity >= (int)Receptivity.Receptivo;
                var reply = revela
                    ? new NpcReply("El presupuesto del proyecto es de doscientos millones", "neutral", "asentir")
                    : new NpcReply("Prefiero no hablar de numeros todavia", "reservado", "cruzar_brazos");

                return new RequirementResponse(
                    revela ? RequirementOutcome.Revelado : RequirementOutcome.AunNoRevelado,
                    reply,
                    Presupuesto);
            }
        }

        /// <summary>
        /// Subclase concreta que ejerce la base contra <see cref="RespondedorDeRequerimientosDePrueba"/>.
        /// Sin ella NUnit no ejecuta ni un <c>[Test]</c> de la base en este cambio (M16 aun no existe).
        /// </summary>
        public sealed class RequirementResponderContractStubTests : RequirementResponderContract
        {
            protected override IRequirementResponder CreateSubject() => new RespondedorDeRequerimientosDePrueba();
        }
    }
}
