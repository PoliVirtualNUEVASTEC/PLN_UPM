using System;
using NpcAi.Core;

namespace NpcAi.Speech.Threading
{
    /// <summary>
    /// Marshalling al hilo principal (design.md, Decision 3). El hilo trabajador llama
    /// <see cref="Post"/> al cerrar una frase; el hilo principal llama <see cref="Drenar"/>
    /// dentro de <c>Update()</c> para que <paramref name="emitir"/> (en produccion, el
    /// guard de generacion del adaptador) corra siempre en el hilo principal.
    /// </summary>
    internal interface IMainThreadPump
    {
        /// <summary>
        /// Registra la emision de <paramref name="frase"/> para la generacion
        /// <paramref name="generacion"/>. Segun la implementacion, <paramref name="emitir"/>
        /// corre de inmediato o queda pendiente hasta el proximo <see cref="Drenar"/>.
        /// </summary>
        void Post(int generacion, Utterance frase, Action<int, Utterance> emitir);

        /// <summary>Ejecuta, en el hilo desde el que se llama, cualquier emision pendiente.</summary>
        void Drenar();
    }
}
