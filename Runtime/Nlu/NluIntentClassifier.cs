using System.Diagnostics;
using NpcAi.Core;

namespace NpcAi.Nlu
{
    /// <summary>
    /// Implementación principal del clasificador de lenguaje natural (M2).
    /// Transforma texto libre en intenciones estructuradas y tonos emocionales,
    /// cumpliendo de manera estricta el contrato <see cref="IIntentClassifier"/>.
    /// </summary>
    public sealed class NluIntentClassifier : IIntentClassifier
    {
        /// <summary>
        /// Indica si el clasificador y sus recursos lingüísticos están listos para operar.
        /// </summary>
        public bool IsReady { get; }

        /// <summary>
        /// Crea una nueva instancia del clasificador.
        /// </summary>
        /// <param name="isReady">Estado inicial de disponibilidad.</param>
        public NluIntentClassifier(bool isReady = true)
        {
            IsReady = isReady;
        }

        /// <summary>
        /// Clasifica el enunciado de texto libre devolviendo la intención, tono, confianza y latencia.
        /// Garantiza no lanzar excepciones bajo ninguna condición.
        /// </summary>
        public IntentResult Classify(string text)
        {
            var stopwatch = Stopwatch.StartNew();

            if (!IsReady || string.IsNullOrWhiteSpace(text))
            {
                stopwatch.Stop();
                float elapsedMs = (float)stopwatch.Elapsed.TotalMilliseconds;
                return IntentResult.Unknown(elapsedMs);
            }

            var (intent, confidence) = SemanticMatcher.ClassifyIntent(text);
            var tone = ToneAnalyzer.AnalyzeTone(text);

            stopwatch.Stop();
            float latencyMs = (float)stopwatch.Elapsed.TotalMilliseconds;

            return new IntentResult(intent, tone, confidence, latencyMs);
        }
    }
}
