using System;
using UnityEngine;

namespace NpcAi.Speech.Audio
{
    /// <summary>
    /// Envuelve <c>UnityEngine.Microphone</c> (design.md, capa "Captura"): anillo de 1 s,
    /// lectura incremental por posicion, remuestreo a la tasa objetivo via
    /// <see cref="Resampler"/>. Solo corre en el hilo principal porque
    /// <c>Microphone.GetPosition</c>/<c>AudioClip.GetData</c> son API de Unity. Sin prueba
    /// automatizada en EditMode: design.md documenta que EditMode no tiene microfono
    /// (seccion "Testing Strategy"); <see cref="Resampler"/>, la pieza pura de esta capa,
    /// si tiene su propia prueba dedicada.
    /// </summary>
    internal sealed class MicrophoneAudioCapture : IAudioCapture
    {
        private const int SegundosDeAnillo = 1;

        private string     _dispositivo;
        private int         _tasaDeMuestreoObjetivoHz;
        private int         _tasaDeMuestreoNativaHz;
        private int         _ultimaPosicionLeida;
        private AudioClip  _clip;

        public bool EstaActivo { get; private set; }

        public void Iniciar(string dispositivo, int tasaDeMuestreoObjetivoHz)
        {
            _dispositivo              = dispositivo;
            _tasaDeMuestreoObjetivoHz = tasaDeMuestreoObjetivoHz;

            Microphone.GetDeviceCaps(dispositivo, out var frecuenciaMinima, out var frecuenciaMaxima);
            _tasaDeMuestreoNativaHz = ResolverTasaNativa(tasaDeMuestreoObjetivoHz, frecuenciaMinima, frecuenciaMaxima);

            _clip = Microphone.Start(dispositivo, true, SegundosDeAnillo, _tasaDeMuestreoNativaHz);
            _ultimaPosicionLeida = 0;
            EstaActivo = true;
        }

        public int LeerDisponibles(float[] destino)
        {
            if (!EstaActivo || _clip == null) return 0;

            var posicionActual = Microphone.GetPosition(_dispositivo);
            var muestrasNuevas = CalcularMuestrasNuevas(posicionActual, _ultimaPosicionLeida, _clip.samples);
            if (muestrasNuevas <= 0) return 0;

            var crudas = new float[muestrasNuevas];
            _clip.GetData(crudas, _ultimaPosicionLeida);
            _ultimaPosicionLeida = posicionActual;

            var remuestreadas = Resampler.ARemuestrear(crudas, muestrasNuevas, _tasaDeMuestreoNativaHz, _tasaDeMuestreoObjetivoHz);
            var aCopiar = Math.Min(remuestreadas.Length, destino.Length);
            Array.Copy(remuestreadas, destino, aCopiar);
            return aCopiar;
        }

        public void Detener()
        {
            if (!EstaActivo) return;
            Microphone.End(_dispositivo);
            EstaActivo = false;
            _clip = null;
        }

        private static int ResolverTasaNativa(int deseada, int frecuenciaMinima, int frecuenciaMaxima)
        {
            if (frecuenciaMaxima == 0) return deseada; // el dispositivo no reporta limite
            if (deseada >= frecuenciaMinima && deseada <= frecuenciaMaxima) return deseada;
            return frecuenciaMaxima;
        }

        private static int CalcularMuestrasNuevas(int posicionActual, int ultimaLeida, int totalMuestrasDelAnillo)
        {
            if (posicionActual == ultimaLeida) return 0;
            if (posicionActual > ultimaLeida) return posicionActual - ultimaLeida;
            return (totalMuestrasDelAnillo - ultimaLeida) + posicionActual; // el anillo dio la vuelta
        }
    }
}
