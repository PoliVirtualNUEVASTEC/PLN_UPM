using NpcAi.Core;

namespace NpcAi.Nlu
{
    /// <summary>
    /// Analizador ortogonal de tono emocional del usuario basado en marcadores lingüísticos.
    /// </summary>
    public static class ToneAnalyzer
    {
        private static readonly (string pattern, Tone tone)[] TonePatterns =
        {
            // Agresivo (prioridad alta ante insultos o imperativos agresivos)
            ("callese", Tone.Agresivo),
            ("callate", Tone.Agresivo),
            ("incompetente", Tone.Agresivo),
            ("estupido", Tone.Agresivo),
            ("idiota", Tone.Agresivo),
            ("inutil", Tone.Agresivo),
            ("apurese", Tone.Agresivo),
            ("apurate", Tone.Agresivo),
            ("muevase", Tone.Agresivo),
            ("hagalo ya", Tone.Agresivo),

            // Ansioso (urgencia extrema / angustia)
            ("auxilio", Tone.Ansioso),
            ("socorro", Tone.Ansioso),
            ("urgente", Tone.Ansioso),
            ("emergencia", Tone.Ansioso),
            ("dios mio", Tone.Ansioso),
            ("por dios", Tone.Ansioso),
            ("se muere", Tone.Ansioso),

            // Empático (contención y calma)
            ("lo siento", Tone.Empatico),
            ("entiendo", Tone.Empatico),
            ("comprendo", Tone.Empatico),
            ("tranquilo", Tone.Empatico),
            ("tranquila", Tone.Empatico),
            ("calma", Tone.Empatico),
            ("no se preocupe", Tone.Empatico),
            ("no te preocupes", Tone.Empatico),

            // Respetuoso (cortesía y deferencia)
            ("por favor", Tone.Respetuoso),
            ("muchas gracias", Tone.Respetuoso),
            ("gracias", Tone.Respetuoso),
            ("con permiso", Tone.Respetuoso),
            ("disculpe", Tone.Respetuoso),
            ("disculpa", Tone.Respetuoso),
            ("con gusto", Tone.Respetuoso),
            ("buenos dias", Tone.Respetuoso),
            ("buenas tardes", Tone.Respetuoso),
            ("senor", Tone.Respetuoso),
            ("senora", Tone.Respetuoso),
            ("doctor", Tone.Respetuoso),
            ("doctora", Tone.Respetuoso),
        };

        /// <summary>
        /// Analiza el texto y determina el tono predominante.
        /// </summary>
        public static Tone AnalyzeTone(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return Tone.Neutral;

            var normalized = TextPreprocessor.Normalize(text);
            if (string.IsNullOrEmpty(normalized))
                return Tone.Neutral;

            foreach (var (pattern, tone) in TonePatterns)
            {
                if (normalized.Contains(pattern))
                    return tone;
            }

            return Tone.Neutral;
        }
    }
}
