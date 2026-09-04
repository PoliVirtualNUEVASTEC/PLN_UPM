namespace NpcAi.Speech.Segmentation
{
    /// <summary>
    /// La ventana de escucha ES la frase (requisito "Estrategia de disparo configurable
    /// por escenario", escenario PulsarParaHablar): nunca corta por silencio ni descarta,
    /// solo respeta el tope duro <see cref="_maxSegundosPorFrase"/>.
    /// </summary>
    internal sealed class PushToTalkStrategy : ISegmentationStrategy
    {
        private readonly float _maxSegundosPorFrase;

        internal PushToTalkStrategy(float maxSegundosPorFrase)
        {
            _maxSegundosPorFrase = maxSegundosPorFrase;
        }

        public void AbrirVentana() { }

        public SegmentDecision Evaluar(float[] muestras, int cantidad, double segundosEnFrase)
            => segundosEnFrase >= _maxSegundosPorFrase ? SegmentDecision.CerrarFrase : SegmentDecision.Continuar;

        public void CerrarVentana() { }
    }
}
