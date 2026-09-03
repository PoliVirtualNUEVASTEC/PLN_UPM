using System;

namespace NpcAi.Speech.Segmentation
{
    /// <summary>
    /// Unico <c>switch</c> del modulo entre estrategias de disparo (design.md, seccion
    /// "Data/Speech/": "cambiar de estrategia no debe requerir recompilar"). Recibe los
    /// umbrales como parametros porque en PR2 todavia no existe el snapshot
    /// <c>SpeechSettings</c> (PR3, tasks.md 3.1); cuando exista, quien llame a
    /// <see cref="Crear"/> le pasara los campos de ese snapshot sin que esta clase cambie,
    /// que es exactamente el criterio de exito 3.
    /// </summary>
    internal static class SegmentationFactory
    {
        internal static ISegmentationStrategy Crear(
            TriggerStrategy estrategia,
            float umbralDeEnergia,
            int msMinimosDeVoz,
            int msDeSilencioParaCortar,
            float maxSegundosPorFrase)
        {
            switch (estrategia)
            {
                case TriggerStrategy.PulsarParaHablar:
                    return new PushToTalkStrategy(maxSegundosPorFrase);

                case TriggerStrategy.ActividadDeVoz:
                    return new VoiceActivityStrategy(umbralDeEnergia, msMinimosDeVoz, msDeSilencioParaCortar, maxSegundosPorFrase);

                default:
                    throw new ArgumentOutOfRangeException(nameof(estrategia), estrategia, "Estrategia de disparo desconocida.");
            }
        }
    }
}
