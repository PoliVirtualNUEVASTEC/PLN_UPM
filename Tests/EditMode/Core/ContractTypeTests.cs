using NpcAi.Core;
using NUnit.Framework;

namespace NpcAi.Core.Tests
{
    /// <summary>
    /// Invariantes de los tipos del contrato. Si una de estas falla, el contrato cambio
    /// sin que nadie lo notara y hay que revisar Docs/CONTRACT-CHANGELOG.md.
    /// </summary>
    public class ContractTypeTests
    {
        [Test]
        public void Version_del_contrato_es_uno()
        {
            Assert.AreEqual(1, Contract.Version);
        }

        [Test]
        public void Los_enums_tienen_el_valor_seguro_en_cero()
        {
            Assert.AreEqual(0, (int)Intent.Desconocida);
            Assert.AreEqual(0, (int)Tone.Neutral);
            Assert.AreEqual(0, (int)PhysicalAction.Ninguna);
            Assert.AreEqual(0, (int)Core.Receptivity.Neutral);
        }

        [Test]
        public void La_receptividad_esta_ordenada()
        {
            Assert.Less((int)Core.Receptivity.NoReceptivo, (int)Core.Receptivity.Neutral);
            Assert.Less((int)Core.Receptivity.Neutral,     (int)Core.Receptivity.Receptivo);
        }

        [Test]
        public void PersonalityId_normaliza_y_compara_por_valor()
        {
            var a = new PersonalityId("Grosero");
            var b = new PersonalityId("  grosero ");

            Assert.AreEqual(a, b);
            Assert.AreEqual("grosero", a.Value);
            Assert.IsTrue(PersonalityId.None.IsNone);
            Assert.IsFalse(a.IsNone);
        }

        [Test]
        public void IntentResult_Unknown_es_seguro()
        {
            var r = IntentResult.Unknown();

            Assert.AreEqual(Intent.Desconocida, r.Intent);
            Assert.AreEqual(Tone.Neutral,       r.Tone);
            Assert.AreEqual(0f,                 r.Confidence);
        }

        [Test]
        public void ReceptivityChange_reporta_direccion()
        {
            var mejora = new ReceptivityChange(Core.Receptivity.Neutral, Core.Receptivity.Receptivo, 2, "X");
            var empeora = new ReceptivityChange(Core.Receptivity.Neutral, Core.Receptivity.NoReceptivo, -2, "Y");
            var igual = new ReceptivityChange(Core.Receptivity.Neutral, Core.Receptivity.Neutral, 0, "Z");

            Assert.IsTrue(mejora.Improved);
            Assert.IsTrue(empeora.Worsened);
            Assert.IsFalse(igual.Changed);
        }
    }
}
