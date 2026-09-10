using System.IO;
using NpcAi.Core;
using NpcAi.Core.Channels;
using NpcAi.SessionLog.Sqlite;
using UnityEngine;

namespace NpcAi.SessionLog.Unity
{
    /// <summary>
    /// Envoltura <see cref="MonoBehaviour"/> de M13 (mismo patron que
    /// <c>SpeechToTextBehaviour</c> de M1): se suscribe por Inspector a
    /// <see cref="UtteranceChannel"/> y <see cref="NpcReplyChannel"/>, delega toda la logica a
    /// <see cref="SessionRecorder"/> (nucleo puro) y expone <see cref="IniciarSesion"/> /
    /// <see cref="FinalizarSesion"/> a la escena anfitriona. Este componente no decide cuando
    /// empieza o termina una sesion de entrenamiento: eso lo cablea quien lo use (p. ej. M11).
    /// </summary>
    public sealed class SessionLogBehaviour : MonoBehaviour
    {
        [SerializeField] private UtteranceChannel _canalDeUtterance;
        [SerializeField] private NpcReplyChannel _canalDeRespuesta;
        [SerializeField] private string _nombreArchivo = "session-log.db";

        private SqliteSessionStore _store;
        private SessionRecorder _recorder;

        /// <summary>Refleja <see cref="SessionRecorder.SesionActiva"/>; falso mientras el componente esta deshabilitado.</summary>
        public bool SesionActiva => _recorder != null && _recorder.SesionActiva;

        /// <summary>Cablear desde el control de la escena anfitriona que arranca el entrenamiento.</summary>
        public void IniciarSesion() => _recorder?.IniciarSesion();

        /// <summary>Cablear desde el control de la escena anfitriona que cierra el entrenamiento.</summary>
        public void FinalizarSesion() => _recorder?.FinalizarSesion();

        /// <summary>Exportacion a texto plano de lo persistido hasta ahora (AD8).</summary>
        public string ExportarTextoPlano() => _store == null ? string.Empty : SessionExport.ExportarTextoPlano(_store);

        private void OnEnable()
        {
            var ruta = Path.Combine(Application.persistentDataPath, _nombreArchivo);
            _store = new SqliteSessionStore(ruta);
            _recorder = new SessionRecorder(_store);

            if (_canalDeUtterance != null) _canalDeUtterance.Subscribe(OnUtterance);
            if (_canalDeRespuesta != null) _canalDeRespuesta.Subscribe(OnNpcReply);
        }

        private void OnDisable()
        {
            if (_canalDeUtterance != null) _canalDeUtterance.Unsubscribe(OnUtterance);
            if (_canalDeRespuesta != null) _canalDeRespuesta.Unsubscribe(OnNpcReply);

            _store?.Dispose();
            _store = null;
            _recorder = null;
        }

        private void OnUtterance(Utterance utterance) => _recorder?.RegistrarUtterance(utterance);

        private void OnNpcReply(NpcReply reply) => _recorder?.RegistrarRespuesta(reply);
    }
}
