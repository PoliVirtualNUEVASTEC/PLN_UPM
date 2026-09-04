using System;

namespace NpcAi.Speech.Audio
{
    /// <summary>
    /// Remuestreo a la tasa objetivo del motor (16 kHz), documentado en design.md,
    /// seccion "Empaquetado del plugin nativo": decimacion promediada cuando el factor de
    /// conversion es entero, interpolacion lineal en cualquier otro caso. Puro, sin
    /// <c>UnityEngine</c>: probable en EditMode sin microfono.
    /// </summary>
    internal static class Resampler
    {
        internal static float[] ARemuestrear(float[] origen, int cantidad, int tasaOrigenHz, int tasaDestinoHz)
        {
            if (cantidad <= 0) return Array.Empty<float>();

            if (tasaOrigenHz == tasaDestinoHz)
                return CopiarRango(origen, cantidad);

            if (tasaOrigenHz > tasaDestinoHz && tasaOrigenHz % tasaDestinoHz == 0)
                return DecimarPromediado(origen, cantidad, tasaOrigenHz / tasaDestinoHz);

            return InterpolarLineal(origen, cantidad, tasaOrigenHz, tasaDestinoHz);
        }

        private static float[] CopiarRango(float[] origen, int cantidad)
        {
            var destino = new float[cantidad];
            Array.Copy(origen, destino, cantidad);
            return destino;
        }

        private static float[] DecimarPromediado(float[] origen, int cantidad, int factor)
        {
            var cantidadDestino = cantidad / factor;
            var destino = new float[cantidadDestino];
            for (var i = 0; i < cantidadDestino; i++)
            {
                var suma = 0d;
                var inicio = i * factor;
                for (var j = 0; j < factor; j++) suma += origen[inicio + j];
                destino[i] = (float)(suma / factor);
            }
            return destino;
        }

        private static float[] InterpolarLineal(float[] origen, int cantidad, int tasaOrigenHz, int tasaDestinoHz)
        {
            if (cantidad == 1) return CopiarRango(origen, 1);

            var razon = (double)tasaDestinoHz / tasaOrigenHz;
            var cantidadDestino = Math.Max(1, (int)Math.Round(cantidad * razon));
            var destino = new float[cantidadDestino];
            var pasoEnOrigen = (double)(cantidad - 1) / Math.Max(1, cantidadDestino - 1);

            for (var i = 0; i < cantidadDestino; i++)
            {
                var posicion = i * pasoEnOrigen;
                var indiceInferior = (int)Math.Floor(posicion);
                var indiceSuperior = Math.Min(indiceInferior + 1, cantidad - 1);
                var fraccion = posicion - indiceInferior;
                destino[i] = (float)(origen[indiceInferior] * (1d - fraccion) + origen[indiceSuperior] * fraccion);
            }
            return destino;
        }
    }
}
