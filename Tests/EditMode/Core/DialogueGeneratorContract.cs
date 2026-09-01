using NpcAi.Core;
using NUnit.Framework;

namespace NpcAi.Core.Tests
{
    /// <summary>Contrato de <see cref="IDialogueGenerator"/>.</summary>
    public abstract class DialogueGeneratorContract
    {
        protected abstract IDialogueGenerator CreateSubject();

        private static readonly PersonalityId Personalidad = new PersonalityId("empatico");

        [Test]
        public void Nunca_devuelve_texto_vacio()
        {
            var g = CreateSubject();

            foreach (Core.Receptivity estado in System.Enum.GetValues(typeof(Core.Receptivity)))
            {
                var reply = g.Generate(Personalidad, estado, IntentResult.Unknown());
                Assert.IsFalse(reply.IsEmpty, $"estado: {estado}");
            }
        }

        [Test]
        public void Las_etiquetas_nunca_son_nulas()
        {
            var reply = CreateSubject().Generate(Personalidad, Core.Receptivity.Neutral, IntentResult.Unknown());

            Assert.IsNotNull(reply.EmotionTag);
            Assert.IsNotNull(reply.AnimationCue);
        }

        [Test]
        public void Funciona_sin_personalidad_asignada()
        {
            Assert.DoesNotThrow(() =>
            {
                var reply = CreateSubject().Generate(PersonalityId.None, Core.Receptivity.Neutral, IntentResult.Unknown());
                Assert.IsFalse(reply.IsEmpty);
            });
        }

        [Test]
        public void Un_NPC_no_receptivo_no_responde_igual_que_uno_receptivo()
        {
            var g = CreateSubject();
            var intent = IntentResult.Unknown();

            var receptivo   = g.Generate(Personalidad, Core.Receptivity.Receptivo,   intent);
            var noReceptivo = g.Generate(Personalidad, Core.Receptivity.NoReceptivo, intent);

            Assert.AreNotEqual(receptivo.Text, noReceptivo.Text);
        }

        // --- G8: Generate NO exige determinismo (asimetria frente a Classify) ---

        [Test]
        public void Generate_no_esta_obligado_a_ser_determinista()
        {
            var g = CreateSubject();
            var intent = IntentResult.Unknown();

            var primera = g.Generate(Personalidad, Core.Receptivity.Neutral, intent);
            var segunda = g.Generate(Personalidad, Core.Receptivity.Neutral, intent);

            // Dos llamadas con la misma entrada PUEDEN devolver texto igual o distinto: el
            // contrato NO exige igualdad (el generador real usa Markov). Lo unico exigible
            // es que ninguna salida sea vacia. NO se asertan (des)igualdad a proposito.
            Assert.IsFalse(primera.IsEmpty);
            Assert.IsFalse(segunda.IsEmpty);
        }
    }
}
