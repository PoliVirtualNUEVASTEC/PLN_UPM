using System;
using NpcAi.Speech.Audio;
using NUnit.Framework;

namespace NpcAi.Speech.Tests
{
    /// <summary>
    /// <see cref="Resampler"/> es la pieza pura de la capa de captura (design.md): no toca
    /// <c>UnityEngine.Microphone</c>, asi que a diferencia de <see cref="MicrophoneAudioCapture"/>
    /// si es probable en EditMode con arreglos sinteticos.
    /// </summary>
    public class ResamplerTests
    {
        [Test]
        public void Misma_tasa_devuelve_una_copia_identica()
        {
            var origen = new[] { 0.1f, 0.2f, 0.3f, 0.4f };

            var resultado = Resampler.ARemuestrear(origen, origen.Length, 16000, 16000);

            CollectionAssert.AreEqual(origen, resultado);
        }

        [Test]
        public void Factor_entero_decima_promediando_los_bloques()
        {
            // 48000 -> 16000, factor 3: cada muestra de salida es el promedio de 3 de entrada
            var origen = new[] { 0f, 0.3f, 0.6f, 0.9f, 0.9f, 0.9f };

            var resultado = Resampler.ARemuestrear(origen, origen.Length, 48000, 16000);

            Assert.AreEqual(2, resultado.Length);
            Assert.AreEqual(0.3f, resultado[0], 1e-5f);
            Assert.AreEqual(0.9f, resultado[1], 1e-5f);
        }

        [Test]
        public void Factor_no_entero_interpola_y_produce_la_longitud_esperada()
        {
            // 44100 -> 16000: factor no entero, exige interpolacion lineal
            var origen = new float[441];
            for (var i = 0; i < origen.Length; i++) origen[i] = i / 441f;

            var resultado = Resampler.ARemuestrear(origen, origen.Length, 44100, 16000);

            var longitudEsperada = (int)Math.Round(origen.Length * (16000d / 44100d));
            Assert.AreEqual(longitudEsperada, resultado.Length);
            Assert.AreEqual(origen[0], resultado[0], 1e-5f);
            Assert.AreEqual(origen[origen.Length - 1], resultado[resultado.Length - 1], 1e-3f);
        }

        [Test]
        public void Solo_procesa_la_cantidad_indicada_no_todo_el_arreglo()
        {
            var origen = new[] { 0.5f, 0.5f, 999f, 999f }; // las ultimas dos son basura fuera de rango

            var resultado = Resampler.ARemuestrear(origen, 2, 16000, 16000);

            CollectionAssert.AreEqual(new[] { 0.5f, 0.5f }, resultado);
        }

        [Test]
        public void Cantidad_cero_devuelve_arreglo_vacio()
        {
            var resultado = Resampler.ARemuestrear(Array.Empty<float>(), 0, 16000, 16000);

            Assert.AreEqual(0, resultado.Length);
        }
    }
}
