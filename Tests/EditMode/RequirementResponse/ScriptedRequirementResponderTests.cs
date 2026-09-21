using NpcAi.Core;
using NpcAi.Core.Tests;
using NpcAi.RequirementResponse.Fakes;
using NUnit.Framework;

namespace NpcAi.RequirementResponse.Tests
{
    /// <summary>
    /// <see cref="ScriptedRequirementResponder"/> contra el contrato de
    /// <see cref="IRequirementResponder"/>: hereda las 10 pruebas de la base sin tocarla y las
    /// pasa TODAS sin <c>Assume</c>-omitir ninguna, porque el doble si puede usar el dato
    /// sintetico "presupuesto" (decision #66; el catalogo real no). Las pruebas propias fijan lo
    /// que la base no cubre: los 4 ids del catalogo, la independencia de <c>Intent</c> (AD5) y
    /// los 3 umbrales de la puerta.
    /// </summary>
    public class ScriptedRequirementResponderTests : RequirementResponderContract
    {
        private static readonly PersonalityId Personalidad = new PersonalityId("empatico");

        protected override IRequirementResponder CreateSubject() => new ScriptedRequirementResponder();

        private static IRequirementResponder SujetoListo()
        {
            var s = new ScriptedRequirementResponder();
            s.AssignCase(new RequirementCaseId("caso-juntas-01"), Personalidad);
            return s;
        }

        private static RequirementOutcome Outcome(IRequirementResponder s, string frase, Receptivity receptividad) =>
            s.Respond(new Utterance(frase, 1f, 1f), IntentResult.Unknown(), receptividad).Outcome;

        [Test]
        public void Reconoce_los_4_ids_del_catalogo_real()
        {
            foreach (var id in new[] { "caso-juntas-01", "caso-juntas-02", "caso-juntas-03", "caso-juntas-04" })
            {
                var s = CreateSubject();
                s.AssignCase(new RequirementCaseId(id), Personalidad);

                Assert.IsTrue(s.IsReady, $"El doble DEBE reconocer \"{id}\" como caso existente");
            }
        }

        [Test]
        public void Con_un_id_desconocido_distinto_de_no_existe_tampoco_queda_listo()
        {
            var s = CreateSubject();

            s.AssignCase(new RequirementCaseId("caso-juntas-99"), Personalidad);

            Assert.IsFalse(s.IsReady);
        }

        [Test]
        public void Un_saludo_no_es_un_turno_de_requerimientos()
        {
            var respuesta = SujetoListo().Respond(
                new Utterance("Buenos dias, ¿como esta?", 1f, 1f), default, Receptivity.Receptivo);

            Assert.AreEqual(RequirementOutcome.NoAplica, respuesta.Outcome);
            Assert.IsTrue(respuesta.RequirementId.IsNone);
        }

        [Test]
        public void El_Intent_no_cambia_el_requerimiento_emparejado()
        {
            var s = SujetoListo();
            var frase = new Utterance("cual es el presupuesto del proyecto", 1f, 1f);
            var presupuesto = new RequirementId("presupuesto");

            var aporta = s.Respond(frase, new IntentResult(Intent.AportaInformacion, Tone.Respetuoso, 0.9f, 0f), Receptivity.Receptivo);
            var fueraDeTema = s.Respond(frase, new IntentResult(Intent.PreguntaFueraDeTema, Tone.Agresivo, 0.9f, 0f), Receptivity.Receptivo);
            var desconocido = s.Respond(frase, IntentResult.Unknown(), Receptivity.Receptivo);

            Assert.AreEqual(presupuesto, aporta.RequirementId);
            Assert.AreEqual(presupuesto, fueraDeTema.RequirementId, "PreguntaFueraDeTema NO filtra (AD5)");
            Assert.AreEqual(presupuesto, desconocido.RequirementId);
            Assert.AreEqual(RequirementOutcome.Revelado, fueraDeTema.Outcome);
        }

        [Test]
        public void La_puerta_aplica_los_3_umbrales_de_la_tabla_embebida()
        {
            var s = SujetoListo();
            const string umbralNoReceptivo = "que datos manejan del equipo";        // equipos
            const string umbralNeutral     = "como arman un partido";              // partidos
            const string umbralReceptivo   = "cual es el presupuesto del proyecto"; // presupuesto

            Assert.AreEqual(RequirementOutcome.Revelado,     Outcome(s, umbralNoReceptivo, Receptivity.NoReceptivo));
            Assert.AreEqual(RequirementOutcome.AunNoRevelado, Outcome(s, umbralNeutral, Receptivity.NoReceptivo));
            Assert.AreEqual(RequirementOutcome.Revelado,     Outcome(s, umbralNeutral, Receptivity.Neutral));
            Assert.AreEqual(RequirementOutcome.AunNoRevelado, Outcome(s, umbralReceptivo, Receptivity.Neutral));
            Assert.AreEqual(RequirementOutcome.Revelado,     Outcome(s, umbralReceptivo, Receptivity.Receptivo));
        }
    }
}
