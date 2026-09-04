using NpcAi.Speech;

namespace NpcAi.Speech.Tests
{
    /// <summary>
    /// Doble determinista de <see cref="IRecognitionEngine"/> para las pruebas de
    /// integracion del cableado real (tasks.md 3.7-3.10). No vive en
    /// <c>Runtime/Speech/Fakes/</c> porque esa carpeta esta reservada al doble de
    /// <c>ISpeechToText</c> (<c>ScriptedSpeechToText</c>), un puerto de <c>NpcAi.Core</c>;
    /// <see cref="IRecognitionEngine"/> es una costura <c>internal</c> exclusiva de este
    /// modulo, asi que su doble vive junto a las pruebas que lo usan.
    /// </summary>
    internal sealed class FakeRecognitionEngine : IRecognitionEngine
    {
        public bool EstaListo { get; private set; } = true;
        public int VecesReiniciado { get; private set; }
        public int VecesFinalizado { get; private set; }
        public int MuestrasAlimentadasTotal { get; private set; }

        /// <summary>Lo que devuelve <see cref="Finalizar"/>. Configurable desde la prueba.</summary>
        public RecognitionResult ResultadoAlFinalizar { get; set; }

        public void Reiniciar()
        {
            VecesReiniciado++;
            MuestrasAlimentadasTotal = 0;
        }

        public void Alimentar(float[] muestras, int cantidad)
        {
            MuestrasAlimentadasTotal += cantidad;
        }

        public RecognitionResult Finalizar()
        {
            VecesFinalizado++;
            return ResultadoAlFinalizar;
        }

        public void Dispose() { }
    }
}
