using System;
using System.Collections.Generic;
using NpcAi.Core;
using UnityEngine;

namespace NpcAi.Receptivity.Unity
{
    /// <summary>
    /// Perfil de receptividad de una personalidad, editable en el Inspector.
    /// Es solo dato: <see cref="ToProfile"/> lo proyecta al tipo puro que consume
    /// el motor y no toma una sola decision. Aqui vive TODO el contacto de M4 con
    /// UnityEngine; el motor, el perfil y el catalogo siguen siendo C# puro.
    /// <para>
    /// M5 crea cuatro de estos assets (grosero, histerico, introvertido, empatico)
    /// sin escribir codigo.
    /// </para>
    /// </summary>
    [CreateAssetMenu(
        fileName = "ReceptivityProfile",
        menuName = "NpcAi/Receptividad/Perfil de personalidad")]
    public sealed class ReceptivityProfileAsset : ScriptableObject
    {
        [Tooltip("Id estable en minusculas y sin tildes, p. ej. \"grosero\". Es la clave del catalogo.")]
        public string personalityId;

        [Header("Umbrales")]
        public int umbralReceptivo = 2;
        public int umbralNoReceptivo = -2;
        [Tooltip("Cota simetrica: el puntaje se satura en [-limite, +limite].")]
        public int limitePuntaje = 4;
        [Tooltip("Puntaje de arranque. El estado inicial sale de este puntaje contra los umbrales.")]
        public int puntajeInicial;

        [Header("Tablas de delta (lo que no este listado suma 0)")]
        public IntentDelta[] porIntencion = Array.Empty<IntentDelta>();
        public ToneDelta[] porTono = Array.Empty<ToneDelta>();
        public ActionDelta[] porAccion = Array.Empty<ActionDelta>();

        /// <summary>Id normalizado de esta personalidad. Clave con la que entra al catalogo.</summary>
        public PersonalityId PersonalityId => new PersonalityId(personalityId);

        /// <summary>Proyeccion al tipo puro. Sin logica: copia y traduce.</summary>
        public ReceptivityProfile ToProfile()
        {
            var intenciones = new Dictionary<Intent, int>();
            foreach (var e in porIntencion ?? Array.Empty<IntentDelta>())
                intenciones[e.intent] = e.delta;

            var tonos = new Dictionary<Tone, int>();
            foreach (var e in porTono ?? Array.Empty<ToneDelta>())
                tonos[e.tone] = e.delta;

            var acciones = new Dictionary<PhysicalAction, int>();
            foreach (var e in porAccion ?? Array.Empty<ActionDelta>())
                acciones[e.action] = e.delta;

            return new ReceptivityProfile(
                umbralReceptivo,
                umbralNoReceptivo,
                limitePuntaje,
                puntajeInicial,
                intenciones,
                tonos,
                acciones);
        }

        /// <summary>
        /// Arma un catalogo puro a partir de los assets. Ignora <c>null</c>; si dos
        /// assets comparten id, gana el ultimo. Un id ausente cae en
        /// <see cref="ReceptivityProfile.Default"/> al consultarse.
        /// </summary>
        public static ReceptivityProfileCatalog BuildCatalog(IEnumerable<ReceptivityProfileAsset> assets)
        {
            var perfiles = new Dictionary<PersonalityId, ReceptivityProfile>();

            if (assets != null)
            {
                foreach (var asset in assets)
                    if (asset != null)
                        perfiles[asset.PersonalityId] = asset.ToProfile();
            }

            return new ReceptivityProfileCatalog(perfiles);
        }

        [Serializable]
        public struct IntentDelta
        {
            public Intent intent;
            public int delta;
        }

        [Serializable]
        public struct ToneDelta
        {
            public Tone tone;
            public int delta;
        }

        [Serializable]
        public struct ActionDelta
        {
            public PhysicalAction action;
            public int delta;
        }
    }
}
