using System.Collections.Generic;

namespace NpcAi.Presentation.Fakes
{
    /// <summary>
    /// Doble determinista de la costura de animación. No toca ningún <c>Animator</c>:
    /// registra en orden cada par <c>(emotionTag, animationCue)</c> que le pidieron aplicar,
    /// para que una prueba pueda afirmar sin escena.
    /// </summary>
    internal sealed class RecordingAnimationDriver : IAnimationDriver
    {
        private readonly List<(string emotionTag, string animationCue)> _aplicadas =
            new List<(string emotionTag, string animationCue)>();

        public IReadOnlyList<(string emotionTag, string animationCue)> Aplicadas => _aplicadas;

        public void Aplicar(string emotionTag, string animationCue) =>
            _aplicadas.Add((emotionTag, animationCue));
    }
}
