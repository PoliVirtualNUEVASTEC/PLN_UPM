using System;

namespace NpcAi.Speech
{
    /// <summary>
    /// Costura INTERNA al modulo M1: muestras de audio -> texto + confianza. Nunca un
    /// puerto de <c>NpcAi.Core</c> (regla dura 3: ningun otro modulo referencia esto).
    /// La implementacion real (Vosk) llega en PR4; esta interfaz solo fija la forma para
    /// que el adaptador (PR1) compile contra ella y las estrategias de segmentacion y la
    /// captura (PR2) puedan alimentarla sin acoplarse a un motor concreto.
    /// </summary>
    internal interface IRecognitionEngine : IDisposable
    {
        bool EstaListo { get; }

        /// <summary>Descarta cualquier estado acumulado y abre una frase nueva.</summary>
        void Reiniciar();

        /// <summary>Alimenta muestras a 16 kHz, mono, en el rango <c>[-1, 1]</c>.</summary>
        void Alimentar(float[] muestras, int cantidad);

        /// <summary>Cierra la frase actual y devuelve el resultado acumulado.</summary>
        RecognitionResult Finalizar();
    }

    /// <summary>
    /// Resultado crudo del motor: sin clamps y sin generacion asociada. El adaptador
    /// (<see cref="OfflineSpeechToText"/>) es quien acota y revalida antes de emitir.
    /// </summary>
    internal readonly struct RecognitionResult
    {
        public readonly string Texto;
        public readonly float  ConfianzaCruda;
        public readonly int    MuestrasAlimentadas;

        public RecognitionResult(string texto, float confianzaCruda, int muestrasAlimentadas)
        {
            Texto               = texto;
            ConfianzaCruda      = confianzaCruda;
            MuestrasAlimentadas = muestrasAlimentadas;
        }
    }
}
