using System.Collections.Generic;

namespace NpcAi.SessionLog
{
    /// <summary>
    /// Seam interno de persistencia de M13. No es un puerto de <c>NpcAi.Core</c>: ningun otro
    /// modulo lo conoce, solo <see cref="SessionRecorder"/> lo usa. <see cref="InMemorySessionStore"/>
    /// (doble) y <c>SqliteSessionStore</c> (real) son sus dos adaptadores.
    /// </summary>
    public interface ISessionStore
    {
        /// <summary>Abre una sesion nueva; descarta cualquier turno de una sesion anterior no cerrada.</summary>
        void IniciarSesion();

        /// <summary>Persiste un turno. Se asume llamado solo mientras la sesion esta iniciada.</summary>
        void RegistrarTurno(SessionTurn turno);

        /// <summary>Cierra la sesion activa. Los turnos ya registrados permanecen.</summary>
        void FinalizarSesion();

        /// <summary>Turnos persistidos, en el orden en que se registraron.</summary>
        IReadOnlyList<SessionTurn> ObtenerTurnos();
    }
}
