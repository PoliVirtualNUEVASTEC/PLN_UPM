using NpcAi.SessionLog.Fakes;

namespace NpcAi.SessionLog.Tests
{
    public class InMemorySessionStoreTests : SessionStoreContract
    {
        protected override ISessionStore CreateSubject() => new InMemorySessionStore();
    }
}
