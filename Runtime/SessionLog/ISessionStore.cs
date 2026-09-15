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
        /// <summary>
        /// Activa la sesion identificada por <paramref name="etiqueta"/>. Si la etiqueta ya
        /// existe, sus turnos anteriores se conservan y los nuevos turnos se acumulan a
        /// continuacion (no hay borrado). Etiqueta nula/vacia/solo espacios cae a
        /// <see cref="EtiquetaSesion.PorDefecto"/>.
        /// </summary>
        void IniciarSesion(string etiqueta);

        /// <summary>Persiste un turno bajo la sesion activa. Se asume llamado solo mientras hay una sesion iniciada.</summary>
        void RegistrarTurno(SessionTurn turno);

        /// <summary>Cierra la sesion activa. Los turnos ya registrados permanecen.</summary>
        void FinalizarSesion();

        /// <summary>Turnos de la sesion activa, en el orden en que se registraron. Equivalente a <see cref="ObtenerTurnosDeSesion"/> con la etiqueta activa.</summary>
        IReadOnlyList<SessionTurn> ObtenerTurnos();

        /// <summary>Catalogo de todas las sesiones conocidas, mas reciente primero.</summary>
        IReadOnlyList<SesionInfo> ListarSesiones();

        /// <summary>Turnos de una sesion especifica (activa o pasada), en el orden en que se registraron. Etiqueta desconocida devuelve lista vacia.</summary>
        IReadOnlyList<SessionTurn> ObtenerTurnosDeSesion(string etiqueta);
    }
}
