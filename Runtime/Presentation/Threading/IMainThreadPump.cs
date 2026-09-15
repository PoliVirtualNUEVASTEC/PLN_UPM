using System;

namespace NpcAi.Presentation.Threading
{
    /// <summary>
    /// Devuelve trabajo al hilo principal. La sintesis (Piper, CPU-bound) corre en un
    /// hilo trabajador; crear el <c>AudioClip</c> y llamar <c>AudioSource.Play</c> exige
    /// hilo principal. Esta bomba es el unico punto por donde el resultado vuelve.
    /// <para>
    /// Copia deliberada del patron de M1 (<c>NpcAi.Speech.Threading</c>): M8 NO referencia
    /// M1 (regla dura 3). Si aparece un tercer consumidor, se evalua promover el tipo a
    /// <c>NpcAi.Core</c> en un cambio de contrato propio.
    /// </para>
    /// </summary>
    internal interface IMainThreadPump
    {
        /// <summary>Encola <paramref name="accion"/> para el hilo principal. <c>null</c> se ignora.</summary>
        void Post(Action accion);

        /// <summary>Ejecuta en orden todo lo encolado. Se llama desde <c>Update()</c> o desde una prueba.</summary>
        void Drenar();
    }
}
