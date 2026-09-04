using System;
using NpcAi.Core;
using NpcAi.Core.Channels;
using NpcAi.Speech.Audio;
using NpcAi.Speech.Config;
using NpcAi.Speech.Model;
using NpcAi.Speech.Threading;
using NpcAi.Speech.Vosk;
using UnityEngine;
#if UNITY_ANDROID
using UnityEngine.Android;
#endif

namespace NpcAi.Speech
{
    /// <summary>
    /// Envoltura <see cref="MonoBehaviour"/> (design.md, capa "Envoltura"; tasks.md 4.6): pide
    /// el permiso de microfono, dispara el aprovisionamiento del modelo (PR3,
    /// <see cref="SpeechModelProvisioner"/>) hacia <see cref="Application.persistentDataPath"/>,
    /// arma el sujeto cableado (<see cref="OfflineSpeechToText"/> + <see cref="VoskRecognitionEngine"/>
    /// + <see cref="QueuedMainThreadPump"/>) y en cada <see cref="Update"/> drena la bomba, lee
    /// <see cref="IAudioCapture.LeerDisponibles"/> y alimenta
    /// <see cref="OfflineSpeechToText.AlimentarBloqueDeAudio"/> — la costura que dejo PR3 (ver
    /// la Deviation registrada en apply-progress: <c>OfflineSpeechToText</c> no posee la
    /// captura, esta clase es quien la lee y empuja cada bloque). Publica cada
    /// <see cref="Utterance"/> en <see cref="_canalDeUtterance"/>. El boton de
    /// pulsar-para-hablar de la escena anfitriona se cablea por Inspector a
    /// <see cref="StartListening"/>/<see cref="StopListening"/> (design.md: "M1 no referencia
    /// NpcAi.VrInput").
    ///
    /// El microfono real solo se activa entre <see cref="StartListening"/> y
    /// <see cref="StopListening"/> (no arranca solo por existir el componente): en un Quest,
    /// capturar audio de forma continua sin que el usuario haya iniciado la escucha es un
    /// costo de bateria y una expectativa de privacidad que este componente no debe asumir
    /// por defecto.
    /// </summary>
    public sealed class SpeechToTextBehaviour : MonoBehaviour
    {
        [SerializeField] private SpeechSettingsAsset _configuracion;
        [SerializeField] private UtteranceChannel _canalDeUtterance;

        private SpeechSettings _configResuelta;
        private OfflineSpeechToText _sujeto;
        private IRecognitionEngine _motor;
        private IAudioCapture _captura;
        private QueuedMainThreadPump _bomba;
        private float[] _bufferDeCaptura;

        /// <summary>Refleja <see cref="ISpeechToText.IsListening"/> del sujeto cableado; <c>false</c> antes de que el pipeline termine de armarse (p. ej. mientras se espera el permiso de microfono).</summary>
        public bool IsListening => _sujeto != null && _sujeto.IsListening;

        /// <summary>Cablear desde el boton de pulsar-para-hablar de la escena anfitriona (design.md).</summary>
        public void StartListening()
        {
            if (_sujeto == null) return;
            _captura.Iniciar(_configResuelta.DispositivoDeMicrofono, _configResuelta.TasaDeMuestreo);
            _sujeto.StartListening();
        }

        /// <summary>Cablear desde el boton de pulsar-para-hablar de la escena anfitriona (design.md).</summary>
        public void StopListening()
        {
            if (_sujeto == null) return;
            _sujeto.StopListening();
            _captura.Detener();
        }

        private void Start()
        {
#if UNITY_ANDROID
            if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
            {
                var callbacks = new PermissionCallbacks();
                callbacks.PermissionGranted += OnMicrophonePermissionGranted;
                Permission.RequestUserPermission(Permission.Microphone, callbacks);
                return;
            }
#endif
            IniciarPipeline();
        }

#if UNITY_ANDROID
        private void OnMicrophonePermissionGranted(string permissionName) => IniciarPipeline();
#endif

        /// <summary>
        /// Aprovisiona el modelo (tasks.md 3.5/3.6) y arma sujeto + motor + captura + bomba
        /// (tasks.md 3.8, 4.5). Solo corre una vez que el permiso de microfono, si aplica, ya
        /// se concedio.
        /// </summary>
        private void IniciarPipeline()
        {
            if (_configuracion == null)
                throw new InvalidOperationException(nameof(_configuracion) + " no esta asignado en el Inspector.");
            if (_configuracion.ModeloEmpaquetado == null)
                throw new InvalidOperationException("ModeloEmpaquetado no esta asignado en " + nameof(_configuracion) + ".");

            _configResuelta = _configuracion.ToSettings();

            var rutaDelModelo = SpeechModelProvisioner.Aprovisionar(
                _configuracion.ModeloEmpaquetado.bytes,
                _configResuelta.IdDeModelo,
                Application.persistentDataPath);
            Resources.UnloadAsset(_configuracion.ModeloEmpaquetado); // design.md, Decision 1

            _motor = new VoskRecognitionEngine(rutaDelModelo, _configResuelta.TasaDeMuestreo);
            _bomba = new QueuedMainThreadPump();
            _sujeto = new OfflineSpeechToText(_configResuelta, _motor, _bomba);
            _sujeto.OnUtterance += PublicarEnCanal;

            _captura = new MicrophoneAudioCapture();
            _bufferDeCaptura = new float[_configResuelta.TasaDeMuestreo];
        }

        private void Update()
        {
            _bomba?.Drenar();

            if (_captura == null || !_captura.EstaActivo || _sujeto == null) return;

            var cantidad = _captura.LeerDisponibles(_bufferDeCaptura);
            if (cantidad > 0)
                _sujeto.AlimentarBloqueDeAudio(_bufferDeCaptura, cantidad);
        }

        private void OnDestroy()
        {
            if (_sujeto != null)
                _sujeto.OnUtterance -= PublicarEnCanal;

            _captura?.Detener();
            _motor?.Dispose();
        }

        private void PublicarEnCanal(Utterance utterance)
        {
            if (_canalDeUtterance != null)
                _canalDeUtterance.Raise(utterance);
        }
    }
}
