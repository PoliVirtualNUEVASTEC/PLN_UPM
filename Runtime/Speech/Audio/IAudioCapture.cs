namespace NpcAi.Speech.Audio
{
    /// <summary>
    /// Costura interna de captura de audio (design.md, capa "Captura"). La implementacion
    /// real (<see cref="MicrophoneAudioCapture"/>) envuelve la API de microfono de Unity,
    /// que solo corre en el hilo principal; esta interfaz existe para que el resto del
    /// modulo (segmentacion, motor) no dependa de <c>UnityEngine.Microphone</c> directamente
    /// y para poder sustituirla en pruebas.
    /// </summary>
    internal interface IAudioCapture
    {
        bool EstaActivo { get; }

        /// <summary>Inicia la captura en el dispositivo dado, remuestreando a esta tasa.</summary>
        void Iniciar(string dispositivo, int tasaDeMuestreoObjetivoHz);

        /// <summary>
        /// Copia en <paramref name="destino"/> las muestras nuevas desde la ultima lectura,
        /// ya remuestreadas a la tasa objetivo. Devuelve cuantas muestras escribio.
        /// </summary>
        int LeerDisponibles(float[] destino);

        void Detener();
    }
}
