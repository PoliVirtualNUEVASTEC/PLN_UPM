using System;
using System.Collections.Generic;
using System.Linq;

namespace NpcAi.SessionLog.Fakes
{
    /// <summary>
    /// Doble determinista de <see cref="ISessionStore"/>: guarda los turnos de cada sesion en un
    /// diccionario por etiqueta, en memoria. Ninguna etiqueta se borra al iniciar otra (AD3).
    /// </summary>
    public sealed class InMemorySessionStore : ISessionStore
    {
        private readonly Dictionary<string, List<SessionTurn>> _turnosPorEtiqueta = new Dictionary<string, List<SessionTurn>>();
        private readonly Dictionary<string, DateTime> _iniciadaEnPorEtiqueta = new Dictionary<string, DateTime>();
        private readonly Dictionary<string, bool> _cerradaPorEtiqueta = new Dictionary<string, bool>();
        private string _etiquetaActiva;

        public void IniciarSesion(string etiqueta)
        {
            etiqueta = EtiquetaSesion.Normalizar(etiqueta);

            if (!_turnosPorEtiqueta.ContainsKey(etiqueta))
            {
                _turnosPorEtiqueta[etiqueta] = new List<SessionTurn>();
                _iniciadaEnPorEtiqueta[etiqueta] = DateTime.UtcNow;
            }

            _cerradaPorEtiqueta[etiqueta] = false; // se esta activando: ya no cuenta como cerrada
            _etiquetaActiva = etiqueta;
        }

        public void RegistrarTurno(SessionTurn turno) => _turnosPorEtiqueta[_etiquetaActiva].Add(turno);

        public void FinalizarSesion()
        {
            if (_etiquetaActiva != null) _cerradaPorEtiqueta[_etiquetaActiva] = true;
        }

        public IReadOnlyList<SessionTurn> ObtenerTurnos() => ObtenerTurnosDeSesion(_etiquetaActiva);

        public IReadOnlyList<SesionInfo> ListarSesiones() =>
            _iniciadaEnPorEtiqueta
                .OrderByDescending(par => par.Value)
                .Select(par => new SesionInfo(par.Key, par.Value, _cerradaPorEtiqueta[par.Key]))
                .ToList();

        public IReadOnlyList<SessionTurn> ObtenerTurnosDeSesion(string etiqueta)
        {
            if (etiqueta == null) return Array.Empty<SessionTurn>();

            etiqueta = EtiquetaSesion.Normalizar(etiqueta);
            return _turnosPorEtiqueta.TryGetValue(etiqueta, out var turnos)
                ? turnos.AsReadOnly()
                : Array.Empty<SessionTurn>();
        }
    }
}
