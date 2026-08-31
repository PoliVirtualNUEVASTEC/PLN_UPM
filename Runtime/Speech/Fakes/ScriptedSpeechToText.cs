using System;
using System.Collections.Generic;
using NpcAi.Core;

namespace NpcAi.Speech.Fakes
{
    /// <summary>
    /// Doble determinista de M1. No toca el microfono: reproduce una cola de frases
    /// que la prueba (o el banco de pruebas M11) empuja a mano.
    /// </summary>
    public sealed class ScriptedSpeechToText : ISpeechToText
    {
        private readonly Queue<Utterance> _queue = new Queue<Utterance>();

        public event Action<Utterance> OnUtterance;
        public bool IsListening { get; private set; }

        public void StartListening() => IsListening = true;
        public void StopListening()  => IsListening = false;

        /// <summary>Encola una frase para emitirla luego con <see cref="Flush"/>.</summary>
        public void Enqueue(string text, float confidence = 1f, float durationSeconds = 1f)
            => _queue.Enqueue(new Utterance(text, confidence, durationSeconds));

        /// <summary>
        /// Emite una frase. Si no esta escuchando no emite nada: es la invariante que
        /// la prueba de contrato verifica sobre cualquier implementacion real.
        /// </summary>
        public bool Emit(Utterance utterance)
        {
            if (!IsListening) return false;
            OnUtterance?.Invoke(utterance);
            return true;
        }

        /// <summary>Emite todo lo encolado, en orden.</summary>
        public int Flush()
        {
            var count = 0;
            while (_queue.Count > 0)
                if (Emit(_queue.Dequeue())) count++;
            return count;
        }
    }
}
