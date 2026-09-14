using System;
using System.Runtime.InteropServices;

namespace NpcAi.Presentation.Piper
{
    /// <summary>
    /// Bindings P/Invoke crudos de la API publica de <c>libpiper</c> (design.md, Decision 10).
    ///
    /// Firmas transcritas y verificadas contra el header real <c>piper.h</c> (repositorio
    /// OHF-Voice/piper1-gpl, obtenido el 2026-09-14 via fetch directo, no de memoria de
    /// entrenamiento) y contrastadas contra el wrapper .NET de referencia NeverMorewd/PiperSharp
    /// (bindings ya probados en produccion contra el mismo binario que vendorizamos). Mayor
    /// confianza que la nota de riesgo de <c>VoskInterop</c> (M1), pero el mismo criterio se
    /// mantiene: si algo no calza, el fallo aparece como excepcion de P/Invoke en la primera
    /// llamada real, nunca como bug silencioso.
    ///
    /// <see cref="LibraryName"/> = "piper" (sin prefijo "lib"), a diferencia de <c>VoskInterop</c>
    /// ("libvosk"): el resolutor de P/Invoke de .NET prueba nombre+".dll" en Windows y
    /// "lib"+nombre+".so" en Android/Linux como alternativas, asi que "piper" resuelve
    /// <c>piper.dll</c> (el nombre real del binario que vendorizamos, build de CI de
    /// OHF-Voice/piper1-gpl via NeverMorewd/PiperSharp, 2026-09-09) sin necesidad de renombrar
    /// nada, y resolveria <c>libpiper.so</c> el dia que haya build de Android.
    ///
    /// Las rutas y el texto se pasan como <c>byte[]</c> UTF-8 con terminador nulo (no <c>string</c>
    /// con <c>MarshalAs(LPUTF8Str)</c>), mismo criterio que <c>VoskInterop</c> (M1): la
    /// disponibilidad de ese marshaling en el runtime Mono/IL2CPP de este proyecto no se pudo
    /// verificar, y <c>byte[]</c> es blittable de forma deterministica en cualquier runtime.
    ///
    /// Solo se declara el subconjunto de <c>piper.h</c> que <see cref="PiperSpeechSynthesizer"/>
    /// necesita: quedan afuera <c>piper_create_with_options</c> (rutas g2pw/pinyin, sin uso para
    /// espanol) y <c>piper_version</c> (solo diagnostico). Los campos de fonemas/alineaciones de
    /// <see cref="FragmentoDeAudioNativo"/> si se declaran, sin leerlos, porque el layout de
    /// memoria tiene que calzar exacto con el struct nativo completo (M8 no hace lip-sync, ver
    /// Open Questions de design.md).
    /// </summary>
    internal static class PiperInterop
    {
        private const string LibraryName = "piper";

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr piper_create(
            byte[] rutaDelModeloUtf8ConNulo,
            byte[] rutaDeConfigUtf8ConNulo,
            byte[] rutaDeEspeakDataUtf8ConNulo);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void piper_free(IntPtr synth);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern OpcionesDeSintesisNativas piper_default_synthesize_options(IntPtr synth);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int piper_synthesize_start(
            IntPtr synth,
            byte[] textoUtf8ConNulo,
            ref OpcionesDeSintesisNativas opciones);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int piper_synthesize_next(IntPtr synth, ref FragmentoDeAudioNativo fragmento);

        /// <summary>PIPER_OK de piper.h.</summary>
        internal const int PiperOk = 0;

        /// <summary>PIPER_DONE de piper.h: la sintesis termino, no hay mas fragmentos.</summary>
        internal const int PiperSintesisCompleta = 1;

        /// <summary>PIPER_ERR_GENERIC de piper.h.</summary>
        internal const int PiperErrorGenerico = -1;
    }

    /// <summary>
    /// Espejo blittable de <c>piper_synthesize_options</c> (piper.h). Orden y tipos exactos —
    /// no reordenar ni agregar campos sin revisar el header.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct OpcionesDeSintesisNativas
    {
        internal int IdDeLocutor;
        internal float EscalaDeLongitud;
        internal float EscalaDeRuido;
        internal float EscalaDeRuidoDeW;
    }

    /// <summary>
    /// Espejo blittable de <c>piper_audio_chunk</c> (piper.h). Los <c>IntPtr</c> apuntan a memoria
    /// que administra el propio <c>piper_synthesizer</c> nativo y solo son validos hasta la
    /// siguiente llamada a <c>piper_synthesize_next</c>: hay que copiarlos antes de volver a
    /// llamarla (<see cref="PiperSpeechSynthesizer"/> lo hace con <c>Marshal.Copy</c>).
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct FragmentoDeAudioNativo
    {
        internal IntPtr Muestras;              // const float* — PCM mono en [-1,1]
        internal UIntPtr NumeroDeMuestras;     // size_t
        internal int TasaDeMuestreo;           // int, Hz
        internal byte EsElUltimo;              // bool de C (1 byte): != 0 es el ultimo fragmento
        internal IntPtr Fonemas;               // const char32_t* — no usado (sin lip-sync)
        internal UIntPtr NumeroDeFonemas;      // size_t — no usado
        internal IntPtr IdsDeFonema;           // const int* — no usado
        internal UIntPtr NumeroDeIdsDeFonema;  // size_t — no usado
        internal IntPtr Alineaciones;          // const int* — no usado
        internal UIntPtr NumeroDeAlineaciones; // size_t — no usado
    }
}
