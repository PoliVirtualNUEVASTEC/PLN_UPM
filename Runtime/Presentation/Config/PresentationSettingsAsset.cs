using System;
using System.Collections.Generic;
using UnityEngine;

namespace NpcAi.Presentation.Config
{
    /// <summary>
    /// <c>ScriptableObject</c> de configuración de presentación por escenario (un asset bajo
    /// <c>Data/Presentation/</c>, nunca un singleton global — regla dura 7). <see cref="OnValidate"/>
    /// acota los campos numéricos apenas se editan en el Inspector o se llama a mano en pruebas.
    /// El rig y su <c>AnimatorController</c> son del proyecto anfitrión; acá solo vive el mapa
    /// <c>cue -> parámetro</c> como dato.
    /// </summary>
    [CreateAssetMenu(fileName = "PresentationSettings", menuName = "NpcAi/Presentation/Presentation Settings", order = 0)]
    public sealed class PresentationSettingsAsset : ScriptableObject
    {
        [Tooltip("Hz del PCM que produce la síntesis. Piper suele emitir a 22050.")]
        public int TasaDeMuestreo = 22050;

        [Tooltip("Multiplicador de velocidad de habla.")]
        public float Velocidad = 1f;

        [Tooltip("vozId a usar cuando el NPC no trae uno o no está en el mapa.")]
        public string VozPorDefecto = "";

        [Tooltip("vozId de un NPC (Data/Npcs/*.json, de M11) -> id de la voz Piper.")]
        public EntradaDeVoz[] Voces = Array.Empty<EntradaDeVoz>();

        [Tooltip("EmotionTag o AnimationCue de un NpcReply -> parámetro del Animator del rig anfitrión.")]
        public EntradaDeCue[] Cues = Array.Empty<EntradaDeCue>();

        [Tooltip("Voz Piper empaquetada (.bytes). Se asigna en el PR3.")]
        public TextAsset VozEmpaquetada;

        [Tooltip("Id de la voz empaquetada (carpeta de aprovisionamiento). Se usa en el PR3.")]
        public string IdDeVoz = "";

        /// <summary>
        /// <c>internal</c> (no <c>private</c>): Unity la invoca por reflexión sin importar la
        /// accesibilidad, y las pruebas (ensamblado amigo) la llaman directamente para probarla
        /// sin pasar por el Inspector.
        /// </summary>
        internal void OnValidate()
        {
            TasaDeMuestreo = Mathf.Clamp(TasaDeMuestreo, 8000, 48000);
            Velocidad      = Mathf.Clamp(Velocidad, 0.5f, 2f);
        }

        /// <summary>Snapshot POCO que consumen el núcleo y el driver de animación.</summary>
        internal PresentationSettings ToSettings()
        {
            var voces = new Dictionary<string, string>();
            foreach (var v in Voces)
                if (!string.IsNullOrEmpty(v.vozId))
                    voces[v.vozId] = v.idDeVoz;

            var cues = new Dictionary<string, ParametroDeAnimacion>();
            foreach (var c in Cues)
                if (!string.IsNullOrEmpty(c.cue) && !string.IsNullOrEmpty(c.parametro))
                    cues[c.cue] = new ParametroDeAnimacion(c.parametro, c.tipo);

            return new PresentationSettings
            {
                TasaDeMuestreo = TasaDeMuestreo,
                Velocidad      = Velocidad,
                VozPorDefecto  = VozPorDefecto ?? "",
                Voces          = voces,
                Cues           = cues,
            };
        }
    }
}
