using System;
using System.Collections.Generic;

namespace NpcAi.Presentation.Config
{
    /// <summary>Cómo aplicar un cue: como <c>SetTrigger</c> o como <c>SetBool(true)</c>.</summary>
    public enum TipoDeParametroDeAnimacion { Trigger, Bool }

    /// <summary>Entrada serializable del mapa de voces: el <c>vozId</c> de un NPC y la voz Piper que le toca.</summary>
    [Serializable]
    public struct EntradaDeVoz
    {
        public string vozId;
        public string idDeVoz;
    }

    /// <summary>Entrada serializable del mapa de cues: un <c>EmotionTag</c>/<c>AnimationCue</c> y su parámetro del <c>Animator</c>.</summary>
    [Serializable]
    public struct EntradaDeCue
    {
        public string cue;
        public string parametro;
        public TipoDeParametroDeAnimacion tipo;
    }

    /// <summary>Un cue del <c>NpcReply</c> mapeado a un parámetro del <c>Animator</c> del rig anfitrión.</summary>
    internal readonly struct ParametroDeAnimacion
    {
        public readonly string Nombre;
        public readonly TipoDeParametroDeAnimacion Tipo;

        public ParametroDeAnimacion(string nombre, TipoDeParametroDeAnimacion tipo)
        {
            Nombre = nombre;
            Tipo   = tipo;
        }
    }

    /// <summary>
    /// Snapshot POCO de la configuración de presentación de un escenario. Sin <c>UnityEngine</c>
    /// a propósito: es lo que consumen <see cref="NpcPresenter"/> y <see cref="AnimatorDriver"/>,
    /// que deben poder construirse y probarse en EditMode sin un <c>ScriptableObject</c> cargado.
    /// <see cref="PresentationSettingsAsset.ToSettings"/> es la única fábrica real; en pruebas
    /// se construye directamente con inicializadores.
    /// </summary>
    internal sealed class PresentationSettings
    {
        public int    TasaDeMuestreo { get; set; } = 22050;
        public float  Velocidad      { get; set; } = 1f;
        public string VozPorDefecto  { get; set; } = "";

        /// <summary><c>vozId</c> de un NPC (de <c>Data/Npcs/*.json</c>, M11) -> id de la voz Piper.</summary>
        public IReadOnlyDictionary<string, string> Voces { get; set; }

        /// <summary><c>EmotionTag</c> o <c>AnimationCue</c> de un <c>NpcReply</c> -> parámetro del <c>Animator</c>.</summary>
        public IReadOnlyDictionary<string, ParametroDeAnimacion> Cues { get; set; }

        /// <summary>
        /// Resuelve el <c>vozId</c> del NPC contra el mapa. Vacío, nulo o ausente ->
        /// <see cref="VozPorDefecto"/> (que puede ser <c>""</c>).
        /// </summary>
        public string ResolverIdDeVoz(string vozId)
        {
            if (!string.IsNullOrEmpty(vozId)
                && Voces != null
                && Voces.TryGetValue(vozId, out var id)
                && !string.IsNullOrEmpty(id))
                return id;

            return VozPorDefecto ?? "";
        }
    }
}
