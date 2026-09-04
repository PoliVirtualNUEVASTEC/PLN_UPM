using System;
using System.Runtime.InteropServices;

namespace NpcAi.Speech.Vosk
{
    /// <summary>
    /// Bindings P/Invoke crudos de la API publica de <c>libvosk</c> (design.md, "Empaquetado
    /// del plugin nativo"; tasks.md 4.4). <see cref="LibraryName"/> es la constante unica que
    /// hace que <c>[DllImport]</c> resuelva <c>libvosk.so</c> en Android y <c>libvosk.dll</c> en
    /// el Editor de Windows — por eso el binario de Windows debe llamarse <c>libvosk.dll</c>, no
    /// <c>vosk.dll</c> (design.md). Esta clase no tiene logica propia: <see cref="VoskRecognitionEngine"/>
    /// (tasks.md 4.5) administra el ciclo de vida de los punteros y delega el parseo de JSON en
    /// <see cref="VoskResultParser"/>, que por eso queda probable en EditMode sin el binario
    /// nativo presente.
    ///
    /// RIESGO EXPLICITO (no adivinado en silencio): estas firmas se transcriben de la
    /// superficie publica documentada de <c>vosk_api.h</c> (repositorio <c>alphacep/vosk-api</c>)
    /// segun el conocimiento de entrenamiento de este agente; esta fase no tuvo acceso web para
    /// diferenciarlas contra el header exacto de la version que el usuario vaya a vendorizar
    /// (tasks.md 4.2/4.3, pendientes). Las dos funciones que design.md ya nombra explicitamente
    /// — <c>vosk_recognizer_accept_waveform_f</c> y <c>vosk_recognizer_set_words</c> — coinciden
    /// exactamente con lo transcrito aqui, que es la mejor corroboracion disponible sin acceso
    /// web. Si el binario vendorizado por el usuario renombro o reordeno algun simbolo, el
    /// problema aparecera como fallo de enlace/P-Invoke en la primera llamada real (nunca como
    /// un bug silencioso), porque ninguna prueba EditMode puede ejercitar estos bindings sin el
    /// <c>.so</c>/<c>.dll</c> real.
    ///
    /// Las rutas de texto (<c>vosk_model_new</c>) se pasan como <c>byte[]</c> UTF-8 con
    /// terminador nulo en vez de <c>string</c> con <c>CharSet</c>/<c>MarshalAs(LPUTF8Str)</c>:
    /// la disponibilidad de ese marshaling en el runtime Mono/IL2CPP de este proyecto no se pudo
    /// verificar (mismo criterio de riesgo que evito <c>using var</c> en PR3, ver
    /// apply-progress), y <c>byte[]</c> es blittable de forma determinista en cualquier runtime.
    /// </summary>
    internal static class VoskInterop
    {
        private const string LibraryName = "libvosk";

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr vosk_model_new(byte[] modelPathUtf8ConNulo);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void vosk_model_free(IntPtr model);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr vosk_recognizer_new(IntPtr model, float sampleRate);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void vosk_recognizer_set_words(IntPtr recognizer, int words);

        /// <summary>
        /// <c>float[]</c> es blittable y se marshala directo (design.md: "no se necesita
        /// allowUnsafeCode"). El valor de retorno (1 si Vosk detecto su propio limite de frase)
        /// se ignora a proposito: la segmentacion es de M1, no del motor (Decision 2 de
        /// design.md).
        /// </summary>
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int vosk_recognizer_accept_waveform_f(IntPtr recognizer, float[] data, int length);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr vosk_recognizer_result(IntPtr recognizer);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr vosk_recognizer_final_result(IntPtr recognizer);

        /// <summary>Limpia el lattice acumulado sin destruir/recrear el reconocedor.</summary>
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void vosk_recognizer_reset(IntPtr recognizer);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void vosk_recognizer_free(IntPtr recognizer);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void vosk_set_log_level(int logLevel);
    }
}
