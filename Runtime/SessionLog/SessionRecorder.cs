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
        private int  _siguienteSecuencia;
        private bool _sesionActiva;

        public SessionRecorder(ISessionStore store)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
        }

        /// <summary>Refleja si hay una sesion abierta. Falso hasta el primer <see cref="IniciarSesion"/>.</summary>
        public bool SesionActiva => _sesionActiva;

        /// <summary>Abre una sesion nueva; reinicia la numeracion de secuencia.</summary>
        public void IniciarSesion()
        {
            _store.IniciarSesion();
            _siguienteSecuencia = 0;
            _sesionActiva = true;
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

        /// <summary>Turnos persistidos hasta ahora, en orden.</summary>
        public IReadOnlyList<SessionTurn> ObtenerTurnos() => _store.ObtenerTurnos();
    }
}
