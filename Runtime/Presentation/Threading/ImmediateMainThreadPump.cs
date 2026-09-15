using System;

namespace NpcAi.Presentation.Threading
{
    /// <summary>
    /// Implementacion inmediata y por defecto: ejecuta la accion de forma sincronica dentro
    /// de <see cref="Post"/>. No hay bucle de jugador que drene nada, asi que
    /// <see cref="Drenar"/> no hace nada. Es la que usan las pruebas y cualquier hosting en
    /// C# puro sin Unity.
    /// </summary>
    internal sealed class ImmediateMainThreadPump : IMainThreadPump
    {
        public void Post(Action accion) => accion?.Invoke();

        public void Drenar() { }
    }
}
