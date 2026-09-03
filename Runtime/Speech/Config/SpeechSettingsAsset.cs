using NpcAi.Speech.Segmentation;
using UnityEngine;

namespace NpcAi.Speech.Config
{
    /// <summary>
    /// <c>ScriptableObject</c> de configuracion por escenario (tasks.md 3.3; tabla
    /// "Data/Speech/" de design.md). Un asset por escenario bajo <c>Data/Speech/</c>
    /// (p. ej. <c>Boardroom.asset</c>), nunca un singleton global (regla dura 7: lo variable
    /// es dato, no codigo). <see cref="OnValidate"/> acota los campos numericos al rango de
    /// la tabla apenas se editan en el Inspector o se llama a mano en pruebas.
    /// Deliberadamente SIN campo de confianza minima: el requisito "Paso de confianza sin
    /// filtrar" (Decision 6 de design.md) prohibe cualquier umbral que condicione la emision
    /// de un <c>Utterance</c> a su <c>Confidence</c>.
    /// </summary>
    [CreateAssetMenu(fileName = "SpeechSettings", menuName = "NpcAi/Speech/Speech Settings", order = 0)]
    public sealed class SpeechSettingsAsset : ScriptableObject
    {
        public TriggerStrategy Estrategia = TriggerStrategy.ActividadDeVoz;
        public int TasaDeMuestreo = 16000;
        public string EtiquetaDeIdioma = "es-CO";
        public TextAsset ModeloEmpaquetado;
        public string IdDeModelo = "vosk-model-small-es-0.42";
        public string DispositivoDeMicrofono = "";
        public float UmbralDeEnergia = 0.02f;
        public int MsMinimosDeVoz = 200;
        public int MsDeSilencioParaCortar = 700;
        public int MsDePreRoll = 300;
        public float MaxSegundosPorFrase = 15f;
        public int MsMaximosDeCierre = 250;

        /// <summary>
        /// Acota los campos numericos al rango de la tabla "Data/Speech/" de design.md.
        /// <c>internal</c> (no <c>private</c>): Unity la invoca por reflexion sin importar la
        /// accesibilidad, y <c>SpeechSettingsAssetTests</c> (ensamblado amigo) la llama
        /// directamente para probarla sin pasar por el Inspector.
        /// </summary>
        internal void OnValidate()
        {
            TasaDeMuestreo         = Mathf.Clamp(TasaDeMuestreo, 8000, 48000);
            UmbralDeEnergia        = Mathf.Clamp01(UmbralDeEnergia);
            MsMinimosDeVoz         = Mathf.Clamp(MsMinimosDeVoz, 0, 2000);
            MsDeSilencioParaCortar = Mathf.Clamp(MsDeSilencioParaCortar, 100, 3000);
            MsDePreRoll            = Mathf.Clamp(MsDePreRoll, 0, 1000);
            MaxSegundosPorFrase    = Mathf.Clamp(MaxSegundosPorFrase, 1f, 60f);
            MsMaximosDeCierre      = Mathf.Clamp(MsMaximosDeCierre, 0, 2000);
        }

        /// <summary>
        /// Snapshot POCO que consume el nucleo (<see cref="OfflineSpeechToText"/>). No incluye
        /// <see cref="ModeloEmpaquetado"/>: <see cref="SpeechSettings"/> no puede referenciar
        /// <c>UnityEngine.TextAsset</c> (es un POCO sin <c>UnityEngine</c>, tasks.md 3.1); el
        /// aprovisionamiento del modelo (tasks.md 3.5/3.6) consume los bytes del
        /// <c>TextAsset</c> por separado, fuera de este snapshot.
        /// </summary>
        internal SpeechSettings ToSettings() => new SpeechSettings
        {
            Estrategia             = Estrategia,
            TasaDeMuestreo         = TasaDeMuestreo,
            EtiquetaDeIdioma       = EtiquetaDeIdioma,
            IdDeModelo             = IdDeModelo,
            DispositivoDeMicrofono = DispositivoDeMicrofono,
            UmbralDeEnergia        = UmbralDeEnergia,
            MsMinimosDeVoz         = MsMinimosDeVoz,
            MsDeSilencioParaCortar = MsDeSilencioParaCortar,
            MsDePreRoll            = MsDePreRoll,
            MaxSegundosPorFrase    = MaxSegundosPorFrase,
            MsMaximosDeCierre      = MsMaximosDeCierre
        };
    }
}
