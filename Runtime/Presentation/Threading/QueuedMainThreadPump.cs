using System;
using System.Collections.Concurrent;

namespace NpcAi.Presentation.Threading
{
    /// <summary>
    /// Implementacion real: encola desde el hilo trabajador y drena en <c>Update()</c>.
    /// <see cref="Post"/> solo encola: no ejecuta nada hasta que <see cref="Drenar"/> corra
    /// en el hilo principal.
    /// </summary>
    internal sealed class QueuedMainThreadPump : IMainThreadPump
    {
        private readonly ConcurrentQueue<Action> _cola = new ConcurrentQueue<Action>();

        public void Post(Action accion)
        {
            if (accion != null) _cola.Enqueue(accion);
        }

        public void Drenar()
        {
            while (_cola.TryDequeue(out var accion))
                accion();
        }
    }
}
