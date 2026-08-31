using NpcAi.Core;

namespace NpcAi.Nlu.Fakes
{
    /// <summary>
    /// Doble determinista de M2. Tabla de palabras clave, cero modelos, cero Sentis.
    /// Existe para que el carril de comportamiento (M4, M6, M8, M11) no espere a que
    /// el clasificador real exista. Misma entrada -> siempre la misma salida.
    /// </summary>
    public sealed class ScriptedIntentClassifier : IIntentClassifier
    {
        private static readonly (string keyword, Intent intent, Tone tone)[] Table =
        {
            ("por favor",   Intent.SolicitudRespetuosa, Tone.Respetuoso),
            ("disculpe",    Intent.SolicitudRespetuosa, Tone.Respetuoso),
            ("necesito",    Intent.SolicitudRespetuosa, Tone.Neutral),
            ("ya",          Intent.SolicitudAgresiva,   Tone.Agresivo),
            ("rapido",      Intent.SolicitudAgresiva,   Tone.Agresivo),
            ("callese",     Intent.SolicitudAgresiva,   Tone.Agresivo),
            ("entiendo",    Intent.Empatia,             Tone.Empatico),
            ("tranquilo",   Intent.Empatia,             Tone.Empatico),
            ("lo siento",   Intent.Empatia,             Tone.Empatico),
            ("el paciente", Intent.AportaInformacion,   Tone.Neutral),
            ("los datos",   Intent.AportaInformacion,   Tone.Neutral),
            ("futbol",      Intent.PreguntaFueraDeTema, Tone.Neutral),
            ("espere",      Intent.Interrupcion,        Tone.Ansioso),
        };

        /// <summary>El doble no carga modelos, asi que esta listo siempre.</summary>
        public bool IsReady => true;

        public IntentResult Classify(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return IntentResult.Unknown();

            var normalized = Normalize(text);

            foreach (var (keyword, intent, tone) in Table)
                if (normalized.Contains(keyword))
                    return new IntentResult(intent, tone, 0.9f, 0f);

            return IntentResult.Unknown();
        }

        /// <summary>Minusculas y sin tildes, para que "por favor" y "Por Favor" sean lo mismo.</summary>
        private static string Normalize(string text)
        {
            var sb = new System.Text.StringBuilder(text.Length);
            foreach (var c in text.ToLowerInvariant())
            {
                switch (c)
                {
                    case '\u00e1': sb.Append('a'); break;
                    case '\u00e9': sb.Append('e'); break;
                    case '\u00ed': sb.Append('i'); break;
                    case '\u00f3': sb.Append('o'); break;
                    case '\u00fa': sb.Append('u'); break;
                    case '\u00f1': sb.Append('n'); break;
                    default:  sb.Append(c); break;
                }
            }
            return sb.ToString();
        }
    }
}
