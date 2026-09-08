using System.Collections.Generic;

namespace NpcAi.SessionLog.Fakes
{
    /// <summary>Doble determinista de <see cref="ISessionStore"/>: guarda los turnos en una lista en memoria.</summary>
    public sealed class InMemorySessionStore : ISessionStore
    {
        private readonly List<SessionTurn> _turnos = new List<SessionTurn>();

        public void IniciarSesion() => _turnos.Clear();

        public void RegistrarTurno(SessionTurn turno) => _turnos.Add(turno);

        public void FinalizarSesion() { /* nada que cerrar en memoria */ }

        public IReadOnlyList<SessionTurn> ObtenerTurnos() => _turnos.AsReadOnly();
    }
}
