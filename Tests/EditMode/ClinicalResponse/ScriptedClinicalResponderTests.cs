using NpcAi.ClinicalResponse.Fakes;
using NpcAi.Core;
using NpcAi.Core.Tests;
using NUnit.Framework;

namespace NpcAi.ClinicalResponse.Tests
{
    /// <summary>
    /// <see cref="ScriptedClinicalResponder"/> contra el contrato de
    /// <see cref="IClinicalResponder"/>. <c>CreateSubject</c> no necesita <c>AssignCase</c>
    /// aqui: la base (<see cref="ClinicalResponderContract"/>) lo hace ella misma con
    /// <c>caso-01</c>, id que el doble ya reconoce.
    /// </summary>
    public class ScriptedClinicalResponderTests : ClinicalResponderContract
    {
        protected override IClinicalResponder CreateSubject() => new ScriptedClinicalResponder();

        [Test]
        public void Con_un_id_desconocido_distinto_de_no_existe_tampoco_queda_listo()
        {
            var s = CreateSubject();

            s.AssignCase(new ClinicalCaseId("caso-99"), new PersonalityId("empatico"));

            Assert.IsFalse(s.IsReady);
        }

        [Test]
        public void Un_saludo_no_es_un_turno_clinico()
        {
            var s = CreateSubject();
            s.AssignCase(new ClinicalCaseId("caso-01"), new PersonalityId("empatico"));

            var respuesta = s.Respond(new Utterance("Buenos dias, ¿como esta?", 1f, 1f), default);

            Assert.IsFalse(respuesta.Handled);
        }
    }
}
