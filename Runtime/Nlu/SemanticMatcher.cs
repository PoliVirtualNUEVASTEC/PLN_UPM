using NpcAi.Core;

namespace NpcAi.Nlu
{
    /// <summary>
    /// Motor de coincidencia semántica y extracción de intenciones de usuario para el módulo NLU.
    /// </summary>
    public static class SemanticMatcher
    {
        private static readonly (string pattern, Intent intent, float baseConfidence)[] IntentRules =
        {
            // Solicitud agresiva / Imperativos fuertes
            ("hagalo ya", Intent.SolicitudAgresiva, 0.95f),
            ("ya mismo", Intent.SolicitudAgresiva, 0.90f),
            ("callese", Intent.SolicitudAgresiva, 0.95f),
            ("callate", Intent.SolicitudAgresiva, 0.95f),
            ("apurese", Intent.SolicitudAgresiva, 0.85f),
            ("muevase", Intent.SolicitudAgresiva, 0.85f),
            ("de una vez", Intent.SolicitudAgresiva, 0.85f),

            // Interrupción / Pausa
            ("un momento", Intent.Interrupcion, 0.90f),
            ("espere", Intent.Interrupcion, 0.85f),
            ("espera", Intent.Interrupcion, 0.85f),
            ("pare", Intent.Interrupcion, 0.85f),
            ("detengase", Intent.Interrupcion, 0.90f),
            ("alto", Intent.Interrupcion, 0.80f),
            ("silencio", Intent.Interrupcion, 0.85f),

            // Empatía / Contención
            ("entiendo", Intent.Empatia, 0.90f),
            ("comprendo", Intent.Empatia, 0.90f),
            ("lo siento", Intent.Empatia, 0.95f),
            ("tranquilo", Intent.Empatia, 0.90f),
            ("tranquila", Intent.Empatia, 0.90f),
            ("calma", Intent.Empatia, 0.85f),
            ("no se preocupe", Intent.Empatia, 0.90f),

            // Solicitud respetuosa / Consultas formales (prioridad sobre datos informativos)
            ("por favor", Intent.SolicitudRespetuosa, 0.90f),
            ("podria", Intent.SolicitudRespetuosa, 0.85f),
            ("podrias", Intent.SolicitudRespetuosa, 0.85f),
            ("ayudeme", Intent.SolicitudRespetuosa, 0.85f),
            ("ayudame", Intent.SolicitudRespetuosa, 0.85f),
            ("necesito", Intent.SolicitudRespetuosa, 0.80f),
            ("quisiera", Intent.SolicitudRespetuosa, 0.80f),
            ("solicito", Intent.SolicitudRespetuosa, 0.85f),
            ("disculpe", Intent.SolicitudRespetuosa, 0.85f),

            // Preguntas fuera de tema / irrelevantes al escenario
            ("futbol", Intent.PreguntaFueraDeTema, 0.95f),
            ("partido", Intent.PreguntaFueraDeTema, 0.90f),
            ("clima", Intent.PreguntaFueraDeTema, 0.90f),
            ("almuerzo", Intent.PreguntaFueraDeTema, 0.85f),
            ("pelicula", Intent.PreguntaFueraDeTema, 0.85f),

            // Aporta información clínica / técnica / datos (declarativo sin verbo modal de petición)
            ("el paciente", Intent.AportaInformacion, 0.90f),
            ("los datos", Intent.AportaInformacion, 0.85f),
            ("signos vitales", Intent.AportaInformacion, 0.95f),
            ("presion", Intent.AportaInformacion, 0.85f),
            ("saturacion", Intent.AportaInformacion, 0.85f),
            ("temperatura", Intent.AportaInformacion, 0.85f),
            ("monitor", Intent.AportaInformacion, 0.80f),
            ("fiebre", Intent.AportaInformacion, 0.85f),
            ("medicamento", Intent.AportaInformacion, 0.85f),
        };

        /// <summary>
        /// Clasifica el texto normalizado determinando la intención y nivel de confianza.
        /// </summary>
        public static (Intent intent, float confidence) ClassifyIntent(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return (Intent.Desconocida, 0f);

            var normalized = TextPreprocessor.Normalize(text);
            if (string.IsNullOrEmpty(normalized))
                return (Intent.Desconocida, 0f);

            foreach (var (pattern, intent, confidence) in IntentRules)
            {
                if (normalized.Contains(pattern))
                {
                    // Clamp de seguridad en [0, 1]
                    float safeConfidence = confidence < 0f ? 0f : (confidence > 1f ? 1f : confidence);
                    return (intent, safeConfidence);
                }
            }

            return (Intent.Desconocida, 0f);
        }
    }
}
