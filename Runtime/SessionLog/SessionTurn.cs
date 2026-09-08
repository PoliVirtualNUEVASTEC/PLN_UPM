namespace NpcAi.SessionLog
{
    /// <summary>Quien produjo un turno de la bitacora.</summary>
    public enum Hablante
    {
        Usuario = 0,
        Npc     = 1,
    }

    /// <summary>Un turno persistido de la conversacion. Producido por <see cref="SessionRecorder"/>.</summary>
    public readonly struct SessionTurn
    {
        public readonly int      Secuencia;
        public readonly Hablante Hablante;
        public readonly string   Texto;
        public readonly string   EmotionTag;   // vacio para turnos de Usuario
        public readonly string   AnimationCue; // vacio para turnos de Usuario

        public SessionTurn(int secuencia, Hablante hablante, string texto, string emotionTag, string animationCue)
        {
            Secuencia    = secuencia;
            Hablante     = hablante;
            Texto        = texto        ?? string.Empty;
            EmotionTag   = emotionTag   ?? string.Empty;
            AnimationCue = animationCue ?? string.Empty;
        }
    }
}
