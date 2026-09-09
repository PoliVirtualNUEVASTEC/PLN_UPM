namespace NpcAi.Core
{
    /// <summary>Lo que el usuario dijo, ya transcrito. Producido por M1.</summary>
    public readonly struct Utterance
    {
        public readonly string Text;
        public readonly float  Confidence;       // 0..1
        public readonly float  DurationSeconds;

        public Utterance(string text, float confidence, float durationSeconds)
        {
            Text            = text;
            Confidence      = confidence;
            DurationSeconds = durationSeconds;
        }

        public bool IsEmpty => string.IsNullOrWhiteSpace(Text);
    }

    /// <summary>Lo que el usuario quiso decir. Producido por M2.</summary>
    public readonly struct IntentResult
    {
        public readonly Intent Intent;
        public readonly Tone   Tone;
        public readonly float  Confidence;   // 0..1
        public readonly float  LatencyMs;    // >= 0

        public IntentResult(Intent intent, Tone tone, float confidence, float latencyMs)
        {
            Intent     = intent;
            Tone       = tone;
            Confidence = confidence;
            LatencyMs  = latencyMs;
        }

        /// <summary>Resultado seguro para entradas vacias o no entendidas.</summary>
        public static IntentResult Unknown(float latencyMs = 0f) =>
            new IntentResult(Intent.Desconocida, Tone.Neutral, 0f, latencyMs);
    }

    /// <summary>Una transicion del motor de receptividad. Producido por M4.</summary>
    public readonly struct ReceptivityChange
    {
        public readonly Receptivity From;
        public readonly Receptivity To;
        public readonly int         Score;       // puntaje interno tras la evaluacion
        public readonly string      ReasonCode;  // p. ej. "AGRESION_DIRECTA"

        public ReceptivityChange(Receptivity from, Receptivity to, int score, string reasonCode)
        {
            From       = from;
            To         = to;
            Score      = score;
            ReasonCode = reasonCode ?? string.Empty; // nunca null: ReasonCode es diagnostico, no bandera
        }

        public bool Changed  => From != To;
        public bool Improved => (int)To > (int)From;
        public bool Worsened => (int)To < (int)From;
    }

    /// <summary>Lo que el NPC responde. Producido por M6, consumido por M8.</summary>
    public readonly struct NpcReply
    {
        public readonly string Text;
        public readonly string EmotionTag;   // p. ej. "molesto", "aliviado"
        public readonly string AnimationCue; // p. ej. "cruzar_brazos"

        public NpcReply(string text, string emotionTag, string animationCue)
        {
            Text         = text         ?? string.Empty;
            EmotionTag   = emotionTag   ?? string.Empty;
            AnimationCue = animationCue ?? string.Empty;
        }

        public bool IsEmpty => string.IsNullOrWhiteSpace(Text);
    }

    /// <summary>
    /// Respuesta del respondedor clinico. Producido por M15, consumido por el enrutador (M11).
    /// <para>
    /// <c>Handled == false</c> significa "turno no clinico": el llamador enruta a
    /// <see cref="IDialogueGenerator"/> (M6) y <c>Reply</c> NO tiene garantias.
    /// <c>Handled == true</c> significa que <c>Reply</c> va tal cual a M8.
    /// </para>
    /// </summary>
    public readonly struct ClinicalResponse
    {
        public readonly bool     Handled;   // false => turno no clinico, enrutar a M6
        public readonly NpcReply Reply;     // valido solo si Handled

        public ClinicalResponse(bool handled, NpcReply reply)
        {
            Handled = handled;
            Reply   = reply;
        }

        /// <summary>Centinela de "este turno no es clinico": <c>Handled == false</c>.</summary>
        public static ClinicalResponse NoAplica => new ClinicalResponse(false, default);
    }
}
