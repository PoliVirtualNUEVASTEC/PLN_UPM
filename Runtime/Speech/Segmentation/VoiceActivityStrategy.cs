using System;

namespace NpcAi.Speech.Segmentation
{
    /// <summary>
    /// Corta la frase por silencio (requisito "Estrategia de disparo configurable por
    /// escenario", escenario ActividadDeVoz). No cierra hasta acumular
    /// <see cref="_segundosMinimosDeVoz"/> de voz real (evita cortar en puro silencio) y,
    /// una vez que cierra, ignora el resto de la ventana hasta el siguiente
    /// <see cref="AbrirVentana"/> (evita devolver <see cref="SegmentDecision.CerrarFrase"/>
    /// mas de una vez por frase).
    /// </summary>
    internal sealed class VoiceActivityStrategy : ISegmentationStrategy
    {
        private readonly float  _umbralDeEnergia;
        private readonly double _segundosMinimosDeVoz;
        private readonly double _segundosDeSilencioParaCortar;
        private readonly double _maxSegundosPorFrase;

        private double _segundosDeVozAcumulados;
        private double _segundosDeSilencioAcumulados;
        private double _segundoAnteriorEnFrase;
        private bool   _huboVozSuficiente;
        private bool   _segmentoCerrado;

        internal VoiceActivityStrategy(
            float umbralDeEnergia,
            int msMinimosDeVoz,
            int msDeSilencioParaCortar,
            float maxSegundosPorFrase)
        {
            _umbralDeEnergia              = umbralDeEnergia;
            _segundosMinimosDeVoz         = msMinimosDeVoz / 1000d;
            _segundosDeSilencioParaCortar = msDeSilencioParaCortar / 1000d;
            _maxSegundosPorFrase          = maxSegundosPorFrase;
        }

        public void AbrirVentana()
        {
            _segundosDeVozAcumulados      = 0d;
            _segundosDeSilencioAcumulados = 0d;
            _segundoAnteriorEnFrase       = 0d;
            _huboVozSuficiente            = false;
            _segmentoCerrado              = false;
        }

        public SegmentDecision Evaluar(float[] muestras, int cantidad, double segundosEnFrase)
        {
            if (_segmentoCerrado) return SegmentDecision.Descartar;

            if (segundosEnFrase >= _maxSegundosPorFrase)
            {
                _segmentoCerrado = true;
                return SegmentDecision.CerrarFrase;
            }

            var duracionDelBloque = segundosEnFrase - _segundoAnteriorEnFrase;
            if (duracionDelBloque < 0d) duracionDelBloque = 0d;
            _segundoAnteriorEnFrase = segundosEnFrase;

            var hayVozEnEsteBloque = CalcularRms(muestras, cantidad) >= _umbralDeEnergia;

            if (hayVozEnEsteBloque)
            {
                _segundosDeVozAcumulados += duracionDelBloque;
                _segundosDeSilencioAcumulados = 0d;
                if (_segundosDeVozAcumulados >= _segundosMinimosDeVoz) _huboVozSuficiente = true;
                return SegmentDecision.Continuar;
            }

            if (!_huboVozSuficiente)
                return SegmentDecision.Descartar; // silencio antes de acumular voz minima: nada que cerrar

            _segundosDeSilencioAcumulados += duracionDelBloque;
            if (_segundosDeSilencioAcumulados >= _segundosDeSilencioParaCortar)
            {
                _segmentoCerrado = true;
                return SegmentDecision.CerrarFrase;
            }

            return SegmentDecision.Continuar;
        }

        public void CerrarVentana() { }

        private static float CalcularRms(float[] muestras, int cantidad)
        {
            if (cantidad <= 0) return 0f;
            var sumaDeCuadrados = 0d;
            for (var i = 0; i < cantidad; i++) sumaDeCuadrados += (double)muestras[i] * muestras[i];
            return (float)Math.Sqrt(sumaDeCuadrados / cantidad);
        }
    }
}
