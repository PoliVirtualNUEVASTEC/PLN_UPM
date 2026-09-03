using System;
using System.Runtime.InteropServices;
using System.Text;

namespace NpcAi.Speech.Vosk
{
    /// <summary>
    /// Implementacion real de <see cref="IRecognitionEngine"/> sobre <c>libvosk</c> (tasks.md
    /// 4.5; design.md, capa "Motor"). Carga el modelo una sola vez en el constructor y crea un
    /// unico reconocedor nativo que reutiliza durante toda la vida del objeto:
    /// <see cref="Reiniciar"/> llama <c>vosk_recognizer_reset</c> (limpia el lattice acumulado
    /// sin destruir/recrear el reconocedor), nunca recrea nada. <c>vosk_recognizer_set_words</c>
    /// se activa una sola vez al crear el reconocedor para que cada palabra traiga su propio
    /// <c>conf</c> (design.md).
    ///
    /// Ninguna prueba EditMode construye esta clase (design.md, Testing Strategy: "ninguna
    /// prueba construye VoskRecognitionEngine"): requiere el binario nativo vendorizado
    /// (tasks.md 4.2, pendiente del usuario) y el modelo aprovisionado en disco (tasks.md 4.3,
    /// pendiente); por eso compila sin ellos pero no puede ejercitarse hasta que existan. El
    /// parseo de JSON puro, que si es probable sin el binario, vive aparte en
    /// <see cref="VoskResultParser"/>.
    /// </summary>
    internal sealed class VoskRecognitionEngine : IRecognitionEngine
    {
        private const float EscalaAPcm16 = 32768f;

        private readonly IntPtr _modelo;
        private readonly IntPtr _reconocedor;
        private int _muestrasAcumuladas;
        private bool _disposed;
        private float[] _bufferEscalado = Array.Empty<float>();

        /// <param name="rutaDelModelo">
        /// Ruta de sistema de archivos al directorio del modelo ya descomprimido — la que
        /// devuelve <c>NpcAi.Speech.Model.SpeechModelProvisioner.Aprovisionar</c>, NO el
        /// <c>TextAsset</c> empaquetado.
        /// </param>
        /// <param name="tasaDeMuestreoHz">
        /// Debe coincidir con la tasa a la que se alimentan las muestras
        /// (<c>NpcAi.Speech.Config.SpeechSettings.TasaDeMuestreo</c>).
        /// </param>
        public VoskRecognitionEngine(string rutaDelModelo, float tasaDeMuestreoHz)
        {
            if (string.IsNullOrEmpty(rutaDelModelo))
                throw new ArgumentException("rutaDelModelo requerido.", nameof(rutaDelModelo));
            if (tasaDeMuestreoHz <= 0f)
                throw new ArgumentException("tasaDeMuestreoHz debe ser > 0.", nameof(tasaDeMuestreoHz));

            _modelo = VoskInterop.vosk_model_new(AUtf8ConNulo(rutaDelModelo));
            if (_modelo == IntPtr.Zero)
                throw new InvalidOperationException("vosk_model_new devolvio null para '" + rutaDelModelo + "'.");

            _reconocedor = VoskInterop.vosk_recognizer_new(_modelo, tasaDeMuestreoHz);
            if (_reconocedor == IntPtr.Zero)
            {
                VoskInterop.vosk_model_free(_modelo);
                throw new InvalidOperationException("vosk_recognizer_new devolvio null.");
            }

            VoskInterop.vosk_recognizer_set_words(_reconocedor, 1);
        }

        public bool EstaListo => !_disposed && _reconocedor != IntPtr.Zero;

        public void Reiniciar()
        {
            ThrowIfDisposed();
            VoskInterop.vosk_recognizer_reset(_reconocedor);
            _muestrasAcumuladas = 0;
        }

        /// <summary>
        /// <paramref name="muestras"/> llega en la escala normalizada de Unity ([-1,1], la que
        /// devuelve <c>AudioClip.GetData</c>), pero Kaldi (el motor detras de Vosk) espera la
        /// escala de PCM de 16 bits ([-32768,32767]) incluso en la variante float de
        /// <c>accept_waveform_f</c> — sin este reescalado, Vosk recibe audio ~32768 veces mas
        /// silencioso de lo que espera y nunca reconoce nada, aunque no lanza ni falla (bug real
        /// encontrado en Quest, tasks.md 4.7). Escala a un buffer propio en vez de mutar
        /// <paramref name="muestras"/> in-place: ese arreglo lo reutiliza el llamador
        /// (<see cref="OfflineSpeechToText.AlimentarBloqueDeAudio"/>) y ya paso por la
        /// evaluacion de <c>ISegmentationStrategy</c> (VAD), calibrada contra la escala original
        /// de Unity — mutarlo ahi correria el riesgo de descalibrar el VAD en la proxima llamada.
        /// </summary>
        public void Alimentar(float[] muestras, int cantidad)
        {
            ThrowIfDisposed();
            if (muestras == null || cantidad <= 0) return;

            if (_bufferEscalado.Length < cantidad)
                _bufferEscalado = new float[cantidad];

            for (var i = 0; i < cantidad; i++)
                _bufferEscalado[i] = muestras[i] * EscalaAPcm16;

            VoskInterop.vosk_recognizer_accept_waveform_f(_reconocedor, _bufferEscalado, cantidad);
            _muestrasAcumuladas += cantidad;
        }

        public RecognitionResult Finalizar()
        {
            ThrowIfDisposed();

            var puntero = VoskInterop.vosk_recognizer_final_result(_reconocedor);
            var json = AStringUtf8(puntero);
            var resultado = VoskResultParser.Parsear(json, _muestrasAcumuladas);

            _muestrasAcumuladas = 0;
            return resultado;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            if (_reconocedor != IntPtr.Zero)
                VoskInterop.vosk_recognizer_free(_reconocedor);

            if (_modelo != IntPtr.Zero)
                VoskInterop.vosk_model_free(_modelo);
        }

        private void ThrowIfDisposed()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(VoskRecognitionEngine));
        }

        /// <summary>
        /// UTF-8 con terminador nulo: evita depender de <c>CharSet</c>/<c>MarshalAs(LPUTF8Str)</c>
        /// de P/Invoke para el parametro de ruta (ver riesgo documentado en <see cref="VoskInterop"/>).
        /// </summary>
        private static byte[] AUtf8ConNulo(string texto)
        {
            var bytesDeTexto = Encoding.UTF8.GetBytes(texto);
            var conNulo = new byte[bytesDeTexto.Length + 1]; // el ultimo byte queda en 0 (terminador)
            Array.Copy(bytesDeTexto, conNulo, bytesDeTexto.Length);
            return conNulo;
        }

        /// <summary>
        /// Decodifica un <c>const char*</c> UTF-8 devuelto por libvosk sin
        /// <c>Marshal.PtrToStringUTF8</c> (disponibilidad incierta en el runtime Mono/IL2CPP de
        /// este proyecto — mismo criterio de riesgo que evito <c>using var</c> en PR3, ver
        /// apply-progress).
        /// </summary>
        private static string AStringUtf8(IntPtr puntero)
        {
            if (puntero == IntPtr.Zero) return string.Empty;

            var longitud = 0;
            while (Marshal.ReadByte(puntero, longitud) != 0)
                longitud++;

            if (longitud == 0) return string.Empty;

            var bytes = new byte[longitud];
            Marshal.Copy(puntero, bytes, 0, longitud);
            return Encoding.UTF8.GetString(bytes);
        }
    }
}
