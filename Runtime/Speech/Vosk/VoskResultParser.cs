using System;
using UnityEngine;

namespace NpcAi.Speech.Vosk
{
    /// <summary>
    /// Parseo puro del JSON que devuelve <c>vosk_recognizer_result</c>/
    /// <c>vosk_recognizer_final_result</c> (design.md: "parseo JSON, media de conf"; tasks.md
    /// 4.5). Deliberadamente separado de <see cref="VoskRecognitionEngine"/>: no toca
    /// <see cref="VoskInterop"/> ni ninguna llamada nativa, asi que es la unica pieza del
    /// motor Vosk probable en EditMode sin el binario vendorizado (tasks.md 4.2/4.3,
    /// pendientes) — ver <c>VoskResultParserTests</c>. Con <c>vosk_recognizer_set_words(rec, 1)</c>
    /// habilitado (<see cref="VoskRecognitionEngine"/> lo activa al crear el reconocedor), cada
    /// palabra trae su propio <c>conf</c> (confianza de lattice Kaldi); <see cref="RecognitionResult.ConfianzaCruda"/>
    /// es la media simple de esos valores, o <c>0</c> si no hay palabras (silencio o resultado
    /// vacio).
    /// </summary>
    internal static class VoskResultParser
    {
        /// <summary>
        /// Convierte el JSON crudo de Vosk (UTF-8, ya decodificado a <see cref="string"/> por
        /// <see cref="VoskRecognitionEngine"/>) en un <see cref="RecognitionResult"/>. Nunca
        /// lanza: JSON nulo, vacio o invalido produce texto vacio y confianza <c>0</c> — un
        /// resultado nativo corrupto no debe tumbar el hilo trabajador que lo procesa.
        /// </summary>
        internal static RecognitionResult Parsear(string json, int muestrasAlimentadas)
        {
            if (string.IsNullOrEmpty(json))
                return new RecognitionResult(string.Empty, 0f, muestrasAlimentadas);

            VoskJsonResultado resultado;
            try
            {
                resultado = JsonUtility.FromJson<VoskJsonResultado>(json);
            }
            catch (Exception)
            {
                // Frontera con el binario nativo: un JSON corrupto no debe propagar una
                // excepcion hacia el hilo trabajador que cierra la frase (design.md, Decision 3).
                return new RecognitionResult(string.Empty, 0f, muestrasAlimentadas);
            }

            var texto = resultado.text ?? string.Empty;
            var confianzaPromedio = PromedioDeConfianza(resultado.result);
            return new RecognitionResult(texto, confianzaPromedio, muestrasAlimentadas);
        }

        private static float PromedioDeConfianza(VoskJsonPalabra[] palabras)
        {
            if (palabras == null || palabras.Length == 0) return 0f;

            var suma = 0f;
            for (var i = 0; i < palabras.Length; i++)
                suma += palabras[i].conf;

            return suma / palabras.Length;
        }

        /// <summary>
        /// Forma minima del JSON de Vosk que nos interesa (campos <c>start</c>/<c>end</c> de
        /// cada palabra se ignoran a proposito: M1 no los necesita).
        /// </summary>
        [Serializable]
        private struct VoskJsonResultado
        {
            public VoskJsonPalabra[] result;
            public string text;
        }

        [Serializable]
        private struct VoskJsonPalabra
        {
            public float conf;
        }
    }
}
