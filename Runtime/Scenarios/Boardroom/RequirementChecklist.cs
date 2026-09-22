using System;
using System.Collections.Generic;
using NpcAi.Core;

namespace NpcAi.Scenarios.Boardroom
{
    /// <summary>
    /// Proyeccion MINIMA del catalogo de M16 (AD10): el id del caso y los ids de sus
    /// requerimientos, nada mas. POCO sin UnityEngine; quien lo pobla es
    /// <see cref="RequirementChecklistLoader"/>. Espejo de TriageKey (M9), pero guarda
    /// RequirementId y no string (AD11): el constructor del id ya normaliza (trim +
    /// minusculas), asi que la comparacion queda normalizada en los dos lados sin escribir
    /// un normalizador propio.
    /// </summary>
    public sealed class RequirementChecklist
    {
        private readonly HashSet<RequirementId> _idsUnicos;

        /// <summary>Id del caso, p. ej. "caso-juntas-03". Solo diagnostico.</summary>
        public string Id { get; }

        /// <summary>Lista ORDENADA y estable, sin duplicados ni ids vacios.</summary>
        public IReadOnlyList<RequirementId> Requerimientos { get; }

        /// <summary>Denominador de la via de cobertura. Nunca 0 en un checklist valido (AD4).</summary>
        public int Count => Requerimientos.Count;

        public RequirementChecklist(string id, IReadOnlyList<RequirementId> requerimientos)
        {
            Id = id ?? string.Empty;
            Requerimientos = requerimientos ?? Array.Empty<RequirementId>();
            _idsUnicos = new HashSet<RequirementId>(Requerimientos);
        }

        /// <summary>O(1). Base de "ese requerimiento no pertenece al caso asignado".</summary>
        public bool Contains(RequirementId id) => _idsUnicos.Contains(id);
    }
}
