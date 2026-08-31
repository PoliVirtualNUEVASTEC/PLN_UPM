using System.Collections.Generic;
using NpcAi.Core;

namespace NpcAi.Presentation.Fakes
{
    /// <summary>
    /// Doble determinista de M8. No sintetiza voz ni anima nada: registra lo que le
    /// pidieron reproducir, para que una prueba pueda afirmar sobre ello.
    /// </summary>
    public sealed class RecordingNpcPresenter : INpcPresenter
    {
        private readonly List<NpcReply> _played = new List<NpcReply>();

        public IReadOnlyList<NpcReply> Played => _played;
        public bool     HasPlayed => _played.Count > 0;
        public NpcReply Last      => _played.Count == 0 ? default : _played[_played.Count - 1];

        public void Play(NpcReply reply) => _played.Add(reply);

        public void Clear() => _played.Clear();
    }
}
