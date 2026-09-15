using NpcAi.Core;
using NpcAi.Core.Tests;
using NpcAi.Presentation.Fakes;
using NpcAi.Presentation.Threading;
using NUnit.Framework;

namespace NpcAi.Presentation.Tests
{
    /// <summary>
    /// <see cref="NpcPresenter"/> contra <see cref="NpcPresenterContract"/> más las garantías
    /// nuevas del PR1: texto vacío no invoca al sintetizador, un reply válido sintetiza una vez
    /// y dispara la animación, y la síntesis se ejercita a través de la bomba.
    /// </summary>
    public class NpcPresenterTests : NpcPresenterContract
    {
        // Sujeto síncrono y determinista: despacho de síntesis en línea + bomba inmediata.
        private static NpcPresenter Nuevo(out SilentSpeechSynthesizer tts, out RecordingAnimationDriver anim)
        {
            tts  = new SilentSpeechSynthesizer();
            anim = new RecordingAnimationDriver();
            return new NpcPresenter(
                tts, anim, new ImmediateMainThreadPump(),
                vozId: "prueba", tasaDeMuestreo: 22050, despacharSintesis: a => a());
        }

        protected override INpcPresenter CreateSubject() => Nuevo(out _, out _);

        [Test]
        public void Un_reply_vacio_no_invoca_al_sintetizador_ni_lanza()
        {
            var p = Nuevo(out var tts, out var anim);

            Assert.DoesNotThrow(() => p.Play(default));
            Assert.DoesNotThrow(() => p.Play(new NpcReply("   ", "neutral", "idle")));
            Assert.DoesNotThrow(() => p.Play(new NpcReply(null, "neutral", "idle")));

            Assert.AreEqual(0, tts.Invocaciones);
            Assert.IsEmpty(anim.Aplicadas);
            Assert.IsNull(p.UltimoPcm);
        }

        [Test]
        public void Un_reply_con_texto_sintetiza_una_vez_y_dispara_la_animacion()
        {
            var p = Nuevo(out var tts, out var anim);

            p.Play(new NpcReply("Buenas, en que le ayudo?", "amable", "saludar"));

            Assert.AreEqual(1, tts.Invocaciones);
            Assert.AreEqual("Buenas, en que le ayudo?", tts.UltimoTexto);
            Assert.AreEqual("prueba", tts.UltimoVozId);
            Assert.AreEqual(22050, tts.UltimaTasa);
            Assert.IsNotNull(p.UltimoPcm);

            Assert.AreEqual(1, anim.Aplicadas.Count);
            Assert.AreEqual("amable", anim.Aplicadas[0].emotionTag);
            Assert.AreEqual("saludar", anim.Aplicadas[0].animationCue);
        }

        [Test]
        public void Diez_plays_encadenados_sintetizan_diez_veces_en_orden()
        {
            var p = Nuevo(out var tts, out var anim);

            for (var i = 0; i < 10; i++)
                p.Play(new NpcReply($"linea {i}", "neutral", "idle"));

            Assert.AreEqual(10, tts.Invocaciones);
            Assert.AreEqual(10, anim.Aplicadas.Count);
            Assert.AreEqual("linea 9", tts.UltimoTexto);
        }

        [Test]
        public void Con_la_bomba_encolada_no_hay_animacion_hasta_drenar()
        {
            var tts  = new SilentSpeechSynthesizer();
            var anim = new RecordingAnimationDriver();
            var bomba = new QueuedMainThreadPump();
            var p = new NpcPresenter(tts, anim, bomba, despacharSintesis: a => a());

            p.Play(new NpcReply("hola", "neutral", "idle"));

            Assert.AreEqual(1, tts.Invocaciones, "la sintesis corre en el despachador, no espera a Drenar");
            Assert.IsEmpty(anim.Aplicadas, "la entrega al hilo principal espera a Drenar");

            bomba.Drenar();

            Assert.AreEqual(1, anim.Aplicadas.Count);
        }

        [Test]
        public void El_pcm_no_vacio_se_entrega_al_callback_de_audio_con_la_tasa_de_la_config()
        {
            float[] recibido = null;
            var tasaRecibida = 0;

            var p = new NpcPresenter(
                new PcmFijo(new float[] { 0.1f, 0.2f, 0.3f }),
                new RecordingAnimationDriver(),
                new ImmediateMainThreadPump(),
                tasaDeMuestreo: 16000,
                despacharSintesis: a => a(),
                reproducirAudio: (pcm, tasa) => { recibido = pcm; tasaRecibida = tasa; });

            p.Play(new NpcReply("hola", "neutral", "idle"));

            Assert.IsNotNull(recibido);
            Assert.AreEqual(3, recibido.Length);
            Assert.AreEqual(16000, tasaRecibida);
        }

        [Test]
        public void El_pcm_vacio_no_llama_al_callback_de_audio_pero_si_a_la_animacion()
        {
            var llamadasDeAudio = 0;
            var anim = new RecordingAnimationDriver();

            var p = new NpcPresenter(
                new SilentSpeechSynthesizer(), anim, new ImmediateMainThreadPump(),
                despacharSintesis: a => a(),
                reproducirAudio: (_, __) => llamadasDeAudio++);

            p.Play(new NpcReply("hola", "molesto", "cruzar_brazos"));

            Assert.AreEqual(0, llamadasDeAudio);
            Assert.AreEqual(1, anim.Aplicadas.Count);
        }

        // Sintetizador de prueba que devuelve un PCM fijo (SilentSpeechSynthesizer siempre da vacío).
        private sealed class PcmFijo : ISpeechSynthesizer
        {
            private readonly float[] _pcm;
            public PcmFijo(float[] pcm) => _pcm = pcm;
            public bool EstaListo => true;
            public float[] Sintetizar(string texto, string vozId, int tasaDeMuestreo) => _pcm;
        }
    }
}
