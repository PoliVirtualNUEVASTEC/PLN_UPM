using System;
using System.Collections.Generic;
using UnityEngine;
using NpcAi.Core;

namespace NpcAi.Scenarios.Boardroom
{
    /// <summary>
    /// De JSON (esquema de <c>Data/Requirements/README.md</c>, M16) a
    /// <see cref="RequirementChecklist"/>. Deliberadamente NO mapea "cliente",
    /// "respuesta", "receptividadMinima", "ejemplosDePregunta" ni los tags: las clases
    /// <c>Raw*</c> de abajo declaran unicamente lo minimo, asi que <c>JsonUtility</c> ignora
    /// el resto del archivo (AD10, espejo de <c>TriageKeyLoader</c>). NO exige el minimo de 4
    /// requerimientos que exige <c>RequirementCaseLoader</c>: valida un contrato distinto
    /// sobre el mismo archivo (AD12). Recibe <c>string</c>, nunca <c>File.ReadAllText</c>.
    /// </summary>
    public static class RequirementChecklistLoader
    {
        /// <summary>
        /// <c>true</c> solo si hay "id" no vacio y al menos un "requerimientos[].id" valido.
        /// Descarta ids vacios y duplicados conservando el orden del archivo (gana el
        /// primero). Nunca lanza: JSON nulo/vacio/malformado devuelve <c>false</c>.
        /// </summary>
        public static bool TryParse(string json, out RequirementChecklist checklist)
        {
            checklist = null;
            if (string.IsNullOrWhiteSpace(json)) return false;

            RawCase raw;
            try { raw = JsonUtility.FromJson<RawCase>(json); }
            catch (Exception) { return false; }

            if (raw == null || string.IsNullOrWhiteSpace(raw.id)) return false;

            var requerimientos = new List<RequirementId>();
            var vistos = new HashSet<RequirementId>();
            foreach (var r in raw.requerimientos ?? Array.Empty<RawRequerimiento>())
            {
                if (r == null || string.IsNullOrWhiteSpace(r.id)) continue;

                var id = new RequirementId(r.id);
                if (!vistos.Add(id)) continue; // duplicado: gana el primero, orden preservado

                requerimientos.Add(id);
            }

            if (requerimientos.Count == 0) return false;

            checklist = new RequirementChecklist(raw.id, requerimientos);
            return true;
        }

        // Espejo MINIMO del esquema de M16. Nombres en minusculas a proposito: JsonUtility
        // empareja por nombre exacto. [Serializable] es obligatorio en toda clase anidada.
        [Serializable]
        private sealed class RawCase
        {
            public string id;
            public RawRequerimiento[] requerimientos;
        }

        [Serializable]
        private sealed class RawRequerimiento
        {
            public string id;   // lo UNICO que M10 necesita de cada requerimiento
        }
    }
}
