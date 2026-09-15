namespace NpcAi.Presentation
{
    /// <summary>
    /// Costura INTERNA de M8: texto -> PCM. Nunca un puerto de <c>NpcAi.Core</c>
    /// (regla dura 3: ningun otro modulo referencia esto). La implementacion real
    /// (Piper) llega en el PR3; esta interfaz solo fija la forma para que el nucleo
    /// (<see cref="NpcPresenter"/>) compile y pase su contrato en EditMode con un doble.
    /// </summary>
    internal interface ISpeechSynthesizer
    {
        /// <summary><c>false</c> hasta que el motor real tenga su voz aprovisionada. El doble esta listo siempre.</summary>
        bool EstaListo { get; }

        /// <summary>
        /// Sintetiza <paramref name="texto"/> a PCM mono en <c>[-1, 1]</c> a
        /// <paramref name="tasaDeMuestreo"/> Hz. Devuelve un arreglo vacio (nunca <c>null</c>)
        /// si no puede: el llamador no reproduce audio, no lanza.
        /// </summary>
        float[] Sintetizar(string texto, string vozId, int tasaDeMuestreo);
    }
}
