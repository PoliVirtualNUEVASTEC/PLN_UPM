using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using NpcAi.Presentation; // ISpeechSynthesizer vive un namespace arriba (NpcAi.Presentation.Piper es hijo, no lo ve solo)

namespace NpcAi.Presentation.Piper
{
    /// <summary>
    /// <see cref="ISpeechSynthesizer"/> real sobre <see cref="PiperInterop"/> (design.md,
    /// Decision 10). Recibe las rutas YA aprovisionadas (modelo, config, datos de espeak-ng) por
    /// construccion: no descomprime nada — eso es trabajo de <c>VoiceProvisioner</c> (tasks.md
    /// 3.4). Un voz = una instancia: <c>vozId</c> se resuelve una vez afuera (Decision 6 de
    /// design.md, "estado de sesion, no de turno") y esta clase no vuelve a mirarlo.
    /// </summary>
    internal sealed class PiperSpeechSynthesizer : ISpeechSynthesizer, IDisposable
    {
        private readonly float _escalaDeLongitud;
        private IntPtr _sintetizador;
        private bool _disposed;

        public bool EstaListo => _sintetizador != IntPtr.Zero;

        /// <param name="rutaDelModelo">Ruta absoluta al <c>.onnx</c> de la voz, ya aprovisionada.</param>
        /// <param name="rutaDeConfig">Ruta absoluta al <c>.onnx.json</c>, o <c>null</c> para que
        /// Piper use <paramref name="rutaDelModelo"/> + ".json".</param>
        /// <param name="rutaDeEspeakData">Ruta absoluta al directorio de datos de espeak-ng, ya
        /// aprovisionado, o <c>null</c> si la voz no lo necesita.</param>
        /// <param name="velocidad">
        /// <c>PresentationSettings.Velocidad</c> (1 = normal, 2 = el doble de rapido). Piper usa
        /// la semantica opuesta ("length_scale": 0.5 = el doble de rapido), por eso se invierte
        /// una sola vez aca.
        /// </param>
        internal PiperSpeechSynthesizer(string rutaDelModelo, string rutaDeConfig, string rutaDeEspeakData, float velocidad = 1f)
        {
            _escalaDeLongitud = velocidad > 0f ? 1f / velocidad : 1f;

            if (!PuedeCrear(rutaDelModelo, rutaDeConfig, rutaDeEspeakData))
                return; // _sintetizador queda en IntPtr.Zero: EstaListo == false, degradacion segura

            _sintetizador = PiperInterop.piper_create(
                AUtf8ConNulo(rutaDelModelo),
                rutaDeConfig != null ? AUtf8ConNulo(rutaDeConfig) : null,
                rutaDeEspeakData != null ? AUtf8ConNulo(rutaDeEspeakData) : null);
        }

        public float[] Sintetizar(string texto, string vozId, int tasaDeMuestreo)
        {
            if (!EstaListo || string.IsNullOrWhiteSpace(texto))
                return Array.Empty<float>();

            var opciones = PiperInterop.piper_default_synthesize_options(_sintetizador);
            opciones.EscalaDeLongitud = _escalaDeLongitud;

            var inicioOk = PiperInterop.piper_synthesize_start(_sintetizador, AUtf8ConNulo(texto), ref opciones);
            if (inicioOk == PiperInterop.PiperErrorGenerico)
                return Array.Empty<float>();

            var muestras = new List<float>();
            while (true)
            {
                var fragmento = default(FragmentoDeAudioNativo);
                var resultado = PiperInterop.piper_synthesize_next(_sintetizador, ref fragmento);

                if (resultado == PiperInterop.PiperErrorGenerico)
                    return Array.Empty<float>();

                CopiarFragmento(fragmento, muestras);

                if (resultado == PiperInterop.PiperSintesisCompleta || fragmento.EsElUltimo != 0)
                    break;
            }

            return muestras.ToArray();
        }

        private static void CopiarFragmento(FragmentoDeAudioNativo fragmento, List<float> destino)
        {
            if (fragmento.Muestras == IntPtr.Zero) return;

            var cantidad = checked((int)fragmento.NumeroDeMuestras.ToUInt64());
            if (cantidad <= 0) return;

            var bloque = new float[cantidad];
            Marshal.Copy(fragmento.Muestras, bloque, 0, cantidad);
            destino.AddRange(bloque);
        }

        /// <summary>
        /// Validar ANTES de llamar a <c>piper_create</c>: libpiper no protege cada ruta con su
        /// propio try/catch antes de que onnxruntime/espeak-ng la toquen, asi que una ruta
        /// invalida puede tirar una excepcion nativa que cruza el limite de P/Invoke como
        /// SEHException y mata el proceso de Unity entero (riesgo documentado en
        /// NeverMorewd/PiperSharp, <c>PiperVoice.ValidatePaths</c>). Aca se resuelve sin lanzar
        /// (este modulo nunca lanza hacia <c>NpcPresenter</c>): si algo falta, <see cref="EstaListo"/>
        /// queda en <c>false</c>, misma degradacion segura que <c>SilentSpeechSynthesizer</c>.
        /// </summary>
        private static bool PuedeCrear(string rutaDelModelo, string rutaDeConfig, string rutaDeEspeakData)
        {
            if (string.IsNullOrWhiteSpace(rutaDelModelo) || !File.Exists(rutaDelModelo)) return false;
            if (rutaDeConfig != null && !File.Exists(rutaDeConfig)) return false;
            if (rutaDeConfig == null && !File.Exists(rutaDelModelo + ".json")) return false;
            if (rutaDeEspeakData != null && !Directory.Exists(rutaDeEspeakData)) return false;
            return true;
        }

        /// <summary>
        /// UTF-8 con terminador nulo: evita depender de <c>CharSet</c>/<c>MarshalAs(LPUTF8Str)</c>
        /// de P/Invoke (mismo criterio que <c>VoskRecognitionEngine.AUtf8ConNulo</c>).
        /// </summary>
        private static byte[] AUtf8ConNulo(string texto)
        {
            var bytesDeTexto = Encoding.UTF8.GetBytes(texto);
            var conNulo = new byte[bytesDeTexto.Length + 1]; // el ultimo byte queda en 0 (terminador)
            Array.Copy(bytesDeTexto, conNulo, bytesDeTexto.Length);
            return conNulo;
        }

        public void Dispose()
        {
            if (_disposed) return;

            if (_sintetizador != IntPtr.Zero)
            {
                PiperInterop.piper_free(_sintetizador);
                _sintetizador = IntPtr.Zero;
            }

            _disposed = true;
            GC.SuppressFinalize(this);
        }

        ~PiperSpeechSynthesizer()
        {
            if (_sintetizador != IntPtr.Zero)
                PiperInterop.piper_free(_sintetizador);
        }
    }
}
