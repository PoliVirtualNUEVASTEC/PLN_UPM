using System;
using System.IO;
using System.Threading.Tasks;
using NpcAi.Core;
using NpcAi.Core.Channels;
using NpcAi.Presentation.Config;
using NpcAi.Presentation.Fakes;
using NpcAi.Presentation.Model;
using NpcAi.Presentation.Piper;
using NpcAi.Presentation.Threading;
using UnityEngine;

namespace NpcAi.Presentation
{
    /// <summary>
    /// Envoltura <see cref="MonoBehaviour"/> de M8. En <see cref="OnEnable"/>/<see cref="OnDisable"/>
    /// se suscribe y desuscribe de un <see cref="NpcReplyChannel"/>; en <see cref="Awake"/> arma el
    /// <see cref="NpcPresenter"/> con la config del escenario, un <c>AudioSource</c> y un
    /// <c>Animator</c> inyectados por la escena; en <see cref="Update"/> drena la bomba. El rig y su
    /// controller son del proyecto anfitrión: este componente solo los maneja.
    /// <para>
    /// El botón/flujo que inicia la conversación y M11 (que publica en el canal) se cablean en la
    /// escena anfitriona: M8 no referencia otros módulos.
    /// </para>
    /// </summary>
    public sealed class NpcPresenterBehaviour : MonoBehaviour
    {
        // internal (con [SerializeField]): Unity los serializa y muestra en el Inspector, y las
        // pruebas (ensamblado amigo) los cablean sin reflexión.
        [SerializeField] internal NpcReplyChannel _canal;
        [SerializeField] internal PresentationSettingsAsset _configuracion;

        [SerializeField] private AudioSource _fuenteDeAudio;
        [SerializeField] private Animator _animator;

        [Tooltip("vozId del NPC de esta escena (de Data/Npcs/*.json, M11). Vacío = voz por defecto de la config.")]
        [SerializeField] private string _vozId;

        private NpcPresenter _presenter;
        private ISpeechSynthesizer _sintetizador;
        private QueuedMainThreadPump _bomba;
        private int _tasaDeMuestreo = 22050;

        /// <summary>Id fijo de carpeta para los datos de espeak-ng aprovisionados (compartido entre voces).</summary>
        private const string IdDeDatosDeEspeak = "espeak-ng-data";

        /// <summary>Cuántos <c>NpcReply</c> llegaron por el canal (observabilidad de prueba).</summary>
        internal int RepliesRecibidos { get; private set; }

        private void Awake()
        {
            var config = _configuracion != null ? _configuracion.ToSettings() : new PresentationSettings();
            _tasaDeMuestreo = config.TasaDeMuestreo;

            _sintetizador = CrearSintetizador(config.Velocidad);
            var animacion = new AnimatorDriver(_animator, config.Cues);
            _bomba = new QueuedMainThreadPump();

            _presenter = new NpcPresenter(
                _sintetizador, animacion, _bomba,
                vozId: config.ResolverIdDeVoz(_vozId),
                tasaDeMuestreo: _tasaDeMuestreo,
                despacharSintesis: trabajo => Task.Run(trabajo),
                reproducirAudio: ReproducirEnAudioSource);
        }

        /// <summary>
        /// <see cref="PiperSpeechSynthesizer"/> si la config trae una voz Piper empaquetada
        /// (<see cref="PresentationSettingsAsset.VozEmpaquetada"/> + <c>IdDeVoz</c>);
        /// <see cref="SilentSpeechSynthesizer"/> (degradación segura) si no — mismo criterio que
        /// el resto de M8, nunca lanzar por falta de config (tasks.md 3.6). Aprovisiona la voz y,
        /// si la config la trae, los datos de espeak-ng (<see cref="VoiceProvisioner"/>,
        /// tasks.md 3.4) antes de construir el sintetizador real.
        /// </summary>
        private ISpeechSynthesizer CrearSintetizador(float velocidad)
        {
            if (_configuracion == null
                || _configuracion.VozEmpaquetada == null
                || string.IsNullOrEmpty(_configuracion.IdDeVoz))
                return new SilentSpeechSynthesizer();

            var raiz = Path.Combine(Application.persistentDataPath, "NpcAi", "Presentation");

            var rutaDeLaVoz = VoiceProvisioner.Aprovisionar(_configuracion.VozEmpaquetada.bytes, _configuracion.IdDeVoz, raiz);
            Resources.UnloadAsset(_configuracion.VozEmpaquetada); // design.md Decision 2, igual que M1

            string rutaDeEspeakData = null;
            if (_configuracion.DatosDeEspeak != null)
            {
                rutaDeEspeakData = VoiceProvisioner.Aprovisionar(_configuracion.DatosDeEspeak.bytes, IdDeDatosDeEspeak, raiz);
                Resources.UnloadAsset(_configuracion.DatosDeEspeak);
            }

            var rutaDelModelo = Path.Combine(rutaDeLaVoz, _configuracion.IdDeVoz + ".onnx");
            var rutaDeConfigDeVoz = Path.Combine(rutaDeLaVoz, _configuracion.IdDeVoz + ".onnx.json");

            return new PiperSpeechSynthesizer(rutaDelModelo, rutaDeConfigDeVoz, rutaDeEspeakData, velocidad);
        }

        /// <summary>Libera el handle nativo de Piper si el sintetizador construido lo tiene (SilentSpeechSynthesizer no implementa IDisposable: no-op).</summary>
        private void OnDestroy()
        {
            (_sintetizador as IDisposable)?.Dispose();
        }

        private void OnEnable()
        {
            if (_canal != null) _canal.Subscribe(OnReply);
        }

        private void OnDisable()
        {
            if (_canal != null) _canal.Unsubscribe(OnReply);
        }

        private void Update() => _bomba?.Drenar();

        private void OnReply(NpcReply reply)
        {
            RepliesRecibidos++;
            _presenter?.Play(reply);
        }

        private void ReproducirEnAudioSource(float[] pcm, int tasa)
        {
            if (_fuenteDeAudio == null || pcm == null || pcm.Length == 0) return;

            var clip = AudioClip.Create("npc-tts", pcm.Length, 1, tasa, false);
            clip.SetData(pcm, 0);
            _fuenteDeAudio.clip = clip;
            _fuenteDeAudio.Play();
        }

        /// <summary>
        /// Fuerza <see cref="Awake"/> + <see cref="OnEnable"/> para pruebas EditMode:
        /// <c>GameObject.SetActive(true)</c> sobre un objeto recién creado no dispara estos
        /// callbacks de forma confiable dentro de un método de prueba síncrono (el mensaje de
        /// activación puede quedar pendiente hasta el próximo tick del Editor). Las pruebas
        /// cablean los campos y llaman esto en vez de depender de la activación real.
        /// </summary>
        internal void CablearParaPrueba()
        {
            Awake();
            OnEnable();
        }

        /// <summary>Fuerza <see cref="OnDisable"/> para pruebas (ver <see cref="CablearParaPrueba"/>).</summary>
        internal void DescablearParaPrueba() => OnDisable();
    }
}
