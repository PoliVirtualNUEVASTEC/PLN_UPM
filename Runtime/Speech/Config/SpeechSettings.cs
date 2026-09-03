using NpcAi.Speech.Segmentation;

namespace NpcAi.Speech.Config
{
    /// <summary>
    /// Snapshot POCO de la configuracion de un escenario (tasks.md 3.1). Sin <c>UnityEngine</c>
    /// a proposito: es lo que consume el nucleo (<see cref="OfflineSpeechToText"/>,
    /// <see cref="SegmentationFactory"/>), que debe seguir siendo probable en EditMode sin
    /// depender de un <c>ScriptableObject</c> cargado. <see cref="SpeechSettingsAsset.ToSettings"/>
    /// (tasks.md 3.3) es la unica fabrica de instancias reales; en pruebas se construye
    /// directamente con inicializadores de objeto.
    /// </summary>
    internal sealed class SpeechSettings
    {
        public TriggerStrategy Estrategia { get; set; }
        public int TasaDeMuestreo { get; set; }
        public string EtiquetaDeIdioma { get; set; }
        public string IdDeModelo { get; set; }
        public string DispositivoDeMicrofono { get; set; }
        public float UmbralDeEnergia { get; set; }
        public int MsMinimosDeVoz { get; set; }
        public int MsDeSilencioParaCortar { get; set; }
        public int MsDePreRoll { get; set; }
        public float MaxSegundosPorFrase { get; set; }
        public int MsMaximosDeCierre { get; set; }
    }
}
