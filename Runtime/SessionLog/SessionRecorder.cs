using System;
using System.Collections.Generic;
using NpcAi.Core;

namespace NpcAi.SessionLog
{
    /// <summary>
    /// Traduce <see cref="Utterance"/> (M1) y <see cref="NpcReply"/> (M6) del contrato v1 a
    /// <see cref="SessionTurn"/> y los delega a un <see cref="ISessionStore"/>. Cero dependencia
    /// de Unity: el cableado a los canales de evento vive en <c>NpcAi.SessionLog.Unity</c>.
    /// </summary>
    public sealed class SessionRecorder
    {
        private readonly ISessionStore _store;
        private int    _siguienteSecuencia;
        private bool   _sesionActiva;
        private string _etiquetaActiva;

        public SessionRecorder(ISessionStore store)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
        }

        /// <summary>Refleja si hay una sesion abierta. Falso hasta el primer <see cref="IniciarSesion"/>.</summary>
        public bool SesionActiva => _sesionActiva;

        /// <summary>Etiqueta de la sesion activa; cadena vacia si no hay ninguna.</summary>
        public string EtiquetaActiva => _sesionActiva ? _etiquetaActiva : string.Empty;

        /// <summary>
        /// Abre (o retoma) la sesion identificada por <paramref name="etiqueta"/>. Si la etiqueta
        /// ya tenia turnos (se esta retomando, no arrancando de cero), la numeracion de secuencia
        /// continua donde iba en vez de reiniciar en 0.
        /// </summary>
        public void IniciarSesion(string etiqueta)
        {
            etiqueta = EtiquetaSesion.Normalizar(etiqueta);

            _store.IniciarSesion(etiqueta);
            _siguienteSecuencia = _store.ObtenerTurnosDeSesion(etiqueta).Count;
            _etiquetaActiva = etiqueta;
            _sesionActiva = true;
        }

        /// <summary>
        /// Retoma automaticamente la sesion mas reciente conocida por el store, pero solo si
        /// quedo interrumpida (nunca se llamo <see cref="FinalizarSesion"/> sobre ella). Si la
        /// ultima sesion se cerro limpio, o nunca hubo ninguna, es no-op: quien use esto debe
        /// llamar <see cref="IniciarSesion"/> con una etiqueta nueva para empezar. Devuelve si
        /// efectivamente retomo algo.
        /// </summary>
        public bool ReanudarUltimaSesion()
        {
            var sesiones = _store.ListarSesiones();
            if (sesiones.Count == 0) return false;

            var ultima = sesiones[0]; // ListarSesiones ya viene mas reciente primero
            if (ultima.Cerrada) return false;

            IniciarSesion(ultima.Etiqueta);
            return true;
        }

        /// <summary>
        /// Registra un turno de <see cref="Hablante.Usuario"/>. No-op si no hay sesion activa o
        /// si <paramref name="utterance"/> esta vacia (<see cref="Utterance.IsEmpty"/>).
        /// </summary>
        public void RegistrarUtterance(Utterance utterance)
        {
            if (!_sesionActiva || utterance.IsEmpty) return;

            _store.RegistrarTurno(new SessionTurn(
                _siguienteSecuencia++, Hablante.Usuario, utterance.Text, string.Empty, string.Empty));
        }

        /// <summary>
        /// Registra un turno de <see cref="Hablante.Npc"/>. No-op si no hay sesion activa o si
        /// <paramref name="reply"/> esta vacia (<see cref="NpcReply.IsEmpty"/>).
        /// </summary>
        public void RegistrarRespuesta(NpcReply reply)
        {
            if (!_sesionActiva || reply.IsEmpty) return;

            _store.RegistrarTurno(new SessionTurn(
                _siguienteSecuencia++, Hablante.Npc, reply.Text, reply.EmotionTag, reply.AnimationCue));
        }

        /// <summary>Cierra la sesion activa. No-op si ya estaba cerrada.</summary>
        public void FinalizarSesion()
        {
            if (!_sesionActiva) return;

            _store.FinalizarSesion();
            _sesionActiva = false;
        }

        /// <summary>Turnos de la sesion activa, en orden.</summary>
        public IReadOnlyList<SessionTurn> ObtenerTurnos() => _store.ObtenerTurnos();

        /// <summary>Catalogo de todas las sesiones conocidas, mas reciente primero.</summary>
        public IReadOnlyList<SesionInfo> ListarSesiones() => _store.ListarSesiones();

        /// <summary>Turnos de una sesion especifica (activa o pasada).</summary>
        public IReadOnlyList<SessionTurn> ObtenerTurnosDeSesion(string etiqueta) => _store.ObtenerTurnosDeSesion(etiqueta);
    }
}
