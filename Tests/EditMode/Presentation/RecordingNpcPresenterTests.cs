using NpcAi.Core;
using NpcAi.Core.Tests;
using NpcAi.Presentation.Fakes;
using NUnit.Framework;

namespace NpcAi.Presentation.Tests
{
    public class RecordingNpcPresenterTests : NpcPresenterContract
    {
        protected override INpcPresenter CreateSubject() => new RecordingNpcPresenter();

        [Test]
        public void Registra_lo_que_reproduce()
        {
            var p = new RecordingNpcPresenter();
            p.Play(new NpcReply("Lo escucho.", "neutral", "idle"));

            Assert.IsTrue(p.HasPlayed);
            Assert.AreEqual("Lo escucho.", p.Last.Text);
        }
    }
}
