using NpcAi.Core;
using NUnit.Framework;

namespace NpcAi.Core.Tests
{
    /// <summary>Contrato de <see cref="INpcPresenter"/>.</summary>
    public abstract class NpcPresenterContract
    {
        protected abstract INpcPresenter CreateSubject();

        [Test]
        public void Reproduce_una_respuesta_normal_sin_lanzar()
        {
            var p = CreateSubject();
            Assert.DoesNotThrow(() => p.Play(new NpcReply("Lo escucho.", "neutral", "idle")));
        }

        [Test]
        public void Sobrevive_a_una_respuesta_vacia()
        {
            var p = CreateSubject();
            Assert.DoesNotThrow(() => p.Play(default));
        }

        [Test]
        public void Sobrevive_a_reproducciones_encadenadas()
        {
            var p = CreateSubject();

            Assert.DoesNotThrow(() =>
            {
                for (var i = 0; i < 10; i++)
                    p.Play(new NpcReply($"linea {i}", "neutral", "idle"));
            });
        }
    }
}
