using System;
using NpcAi.Core;

namespace NpcAi.Speech.Threading
{
    /// <summary>
    /// Implementacion inmediata y por defecto (design.md, Decision 3): ejecuta
    /// <paramref name="emitir"/> de forma sincronica dentro de <see cref="Post"/>. No hay
    /// bucle de jugador que drene nada, asi que <see cref="Drenar"/> no hace nada. Es la
    /// que usan las pruebas y cualquier hosting en C# puro sin Unity.
    /// </summary>
    internal sealed class ImmediateMainThreadPump : IMainThreadPump
    {
        public void Post(int generacion, Utterance frase, Action<int, Utterance> emitir)
            => emitir?.Invoke(generacion, frase);

        public void Drenar() { }
    }
}
