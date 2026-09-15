using System;
using System.Threading.Tasks;
using NpcAi.Core;
using NpcAi.Presentation.Threading;

namespace NpcAi.Presentation
{
    /// <summary>
    /// Implementación real de <see cref="INpcPresenter"/>. Núcleo C# construible sin
    /// <c>AudioSource</c> ni <c>Animator</c> — esa es la propiedad que hace pasable
    /// <c>NpcPresenterContract</c> en EditMode.
    /// <para>
    /// <c>Play</c> despacha la síntesis a un trabajador (no bloquea al llamador, que suele
    /// ser el hilo principal en el callback del canal) y, al terminar, devuelve el resultado
    /// al hilo principal por la <see cref="IMainThreadPump"/>: reproduce el PCM (callback
    /// <c>reproducirAudio</c>, cableado a un <c>AudioSource</c> por <c>NpcPresenterBehaviour</c>)
    /// y dispara la animación. Sin PCM (síntesis vacía) solo anima.
    /// </para>
    /// </summary>
    internal sealed class NpcPresenter : INpcPresenter
    {
        private readonly ISpeechSynthesizer     _sintetizador;
        private readonly IAnimationDriver       _animacion;
        private readonly IMainThreadPump        _bomba;
        private readonly Action<Action>         _despacharSintesis;
        private readonly Action<float[], int>   _reproducirAudio;
        private readonly string                 _vozId;
        private readonly int                    _tasaDeMuestreo;

        /// <summary>Último PCM que llegó a la entrega en el hilo principal (observabilidad de prueba).</summary>
        internal float[] UltimoPcm { get; private set; }

        /// <param name="despacharSintesis">
        /// Cómo se ejecuta el trabajo de síntesis. Por defecto <see cref="Task.Run(Action)"/>
        /// (hilo del pool). Las pruebas inyectan un despachador en línea (<c>a =&gt; a()</c>)
        /// para que <c>Play</c> quede síncrono y determinista.
        /// </param>
        /// <param name="reproducirAudio">
        /// Entrega el PCM sintetizado al hilo principal para reproducirlo. Lo cablea
        /// <c>NpcPresenterBehaviour</c> a un <c>AudioSource</c> (PR2); por defecto es no-op
        /// (el PR1 y las pruebas del núcleo no reproducen audio).
        /// </param>
        internal NpcPresenter(
            ISpeechSynthesizer sintetizador,
            IAnimationDriver animacion,
            IMainThreadPump bomba,
            string vozId = null,
            int tasaDeMuestreo = 22050,
            Action<Action> despacharSintesis = null,
            Action<float[], int> reproducirAudio = null)
        {
            _sintetizador = sintetizador ?? throw new ArgumentNullException(nameof(sintetizador));
            _animacion    = animacion    ?? throw new ArgumentNullException(nameof(animacion));
            _bomba        = bomba        ?? throw new ArgumentNullException(nameof(bomba));
            _vozId          = vozId;
            _tasaDeMuestreo = tasaDeMuestreo;
            _despacharSintesis = despacharSintesis ?? (trabajo => Task.Run(trabajo));
            _reproducirAudio   = reproducirAudio   ?? ((_, __) => { });
        }

        public void Play(NpcReply reply)
        {
            if (reply.IsEmpty) return; // Play(default) y texto vacío/espacios: no-op total, no lanza

            var texto   = reply.Text;
            var emocion = reply.EmotionTag;
            var cue     = reply.AnimationCue;

            _despacharSintesis(() =>
            {
                var pcm = _sintetizador.Sintetizar(texto, _vozId, _tasaDeMuestreo) ?? Array.Empty<float>();
                _bomba.Post(() => Entregar(pcm, emocion, cue));
            });
        }

        private void Entregar(float[] pcm, string emocion, string cue)
        {
            UltimoPcm = pcm;
            if (pcm.Length > 0) _reproducirAudio(pcm, _tasaDeMuestreo);
            _animacion.Aplicar(emocion, cue);
        }
    }
}
