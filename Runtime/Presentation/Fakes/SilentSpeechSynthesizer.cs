using System;

namespace NpcAi.Presentation.Fakes
{
    /// <summary>
    /// Doble determinista de la costura de sintesis. No produce audio: devuelve un arreglo
    /// vacio. Registra qué se le pidió sintetizar para que una prueba pueda afirmar sobre ello,
    /// y es la degradación segura que usa <c>NpcPresenterBehaviour</c> cuando la config no
    /// tiene voz aprovisionada (ver PR2/PR3).
    /// </summary>
    internal sealed class SilentSpeechSynthesizer : ISpeechSynthesizer
    {
        public bool EstaListo => true;

        public int    Invocaciones { get; private set; }
        public string UltimoTexto  { get; private set; }
        public string UltimoVozId  { get; private set; }
        public int    UltimaTasa   { get; private set; }

        public float[] Sintetizar(string texto, string vozId, int tasaDeMuestreo)
        {
            Invocaciones++;
            UltimoTexto = texto;
            UltimoVozId = vozId;
            UltimaTasa  = tasaDeMuestreo;
            return Array.Empty<float>();
        }
    }
}
