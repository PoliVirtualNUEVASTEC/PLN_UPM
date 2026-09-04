using System;
using System.Collections.Concurrent;
using NpcAi.Core;

namespace NpcAi.Speech.Threading
{
    /// <summary>
    /// Implementacion real: encola desde el hilo trabajador y drena en <c>Update()</c>
    /// (design.md, Decision 3). <see cref="Post"/> solo encola: no invoca
    /// <paramref name="emitir"/> hasta que <see cref="Drenar"/> corra en el hilo principal.
    /// </summary>
    internal sealed class QueuedMainThreadPump : IMainThreadPump
    {
        private readonly ConcurrentQueue<Action> _cola = new ConcurrentQueue<Action>();

        public void Post(int generacion, Utterance frase, Action<int, Utterance> emitir)
        {
            if (emitir == null) return;
            _cola.Enqueue(() => emitir(generacion, frase));
        }

        public void Drenar()
        {
            while (_cola.TryDequeue(out var accion))
                accion();
        }
    }
}
