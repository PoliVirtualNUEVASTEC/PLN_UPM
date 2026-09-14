using System.Threading.Tasks;
using NpcAi.Core;
using NpcAi.Core.Channels;
using NpcAi.Presentation.Config;
using NpcAi.Presentation.Fakes;
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
        private QueuedMainThreadPump _bomba;
        private int _tasaDeMuestreo = 22050;

        /// <summary>Cuántos <c>NpcReply</c> llegaron por el canal (observabilidad de prueba).</summary>
        internal int RepliesRecibidos { get; private set; }

        private void Awake()
        {
            var config = _configuracion != null ? _configuracion.ToSettings() : new PresentationSettings();
            _tasaDeMuestreo = config.TasaDeMuestreo;

            // PR2: síntesis silenciosa. PR3: PiperSpeechSynthesizer cuando la config trae voz.
            var sintetizador = new SilentSpeechSynthesizer();
            var animacion    = new AnimatorDriver(_animator, config.Cues);
            _bomba = new QueuedMainThreadPump();

            _presenter = new NpcPresenter(
                sintetizador, animacion, _bomba,
                vozId: config.ResolverIdDeVoz(_vozId),
                tasaDeMuestreo: _tasaDeMuestreo,
                despacharSintesis: trabajo => Task.Run(trabajo),
                reproducirAudio: ReproducirEnAudioSource);
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
