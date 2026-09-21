using System;
using System.Collections.Generic;
using UnityEngine;
using NpcAi.Core;

namespace NpcAi.RequirementResponse
{
    /// <summary>
    /// Banco de estilo por personalidad: prefijo fijo de revelacion + frases de desvio,
    /// cargado de <c>Data/Requirements/matices.json</c> (AD6 de <c>design.md</c>). Pieza propia
    /// de M16 que M15 no tiene: M15 resuelve el matiz con un <c>switch</c> privado en
    /// <c>ClinicalResponder.ConMatiz</c>, pero eso violaria la regla 7 del repo ("agregar una
    /// personalidad no debe cambiar ni una clase"), asi que aqui es dato. El banco NUNCA recibe
    /// ni conoce <see cref="Requerimiento.Respuesta"/>: "el desvio filtra el hecho" es
    /// imposible por construccion, no por prueba (mismo razonamiento que AD4 de M15 con el
    /// bloque <c>clave</c>).
    /// </summary>
    public sealed class PersonalityStyleBank
    {
        // AD10: la garantia del contrato ("con AunNoRevelado, Reply.Text NO es vacio") no puede
        // depender de que matices.json exista o valide, asi que el fallback vive en codigo, en
        // una sola frase constante, documentado como tal. Prefijo vacio: sin matiz conocido, no
        // hay estilo que anteponer.
        private const string DesvioDeFallback = "Prefiero que hablemos de eso mas adelante.";

        /// <summary>Banco sin ninguna personalidad cargada: siempre cae en el fallback de AD10.</summary>
        public static readonly PersonalityStyleBank Fallback =
            new PersonalityStyleBank(new Dictionary<string, Matiz>(StringComparer.Ordinal));

        private readonly Dictionary<string, Matiz> _matices;

        private PersonalityStyleBank(Dictionary<string, Matiz> matices)
        {
            _matices = matices;
        }

        public static bool TryParse(string json, out PersonalityStyleBank banco)
        {
            banco = null;
            if (string.IsNullOrWhiteSpace(json)) return false;

            RawBanco raw;
            try { raw = JsonUtility.FromJson<RawBanco>(json); }
            catch (Exception) { return false; }

            if (raw?.matices == null || raw.matices.Length == 0) return false;

            var matices = new Dictionary<string, Matiz>(StringComparer.Ordinal);
            foreach (var m in raw.matices)
            {
                if (m == null || string.IsNullOrWhiteSpace(m.personalidad)) continue;
                if (m.desvios == null || m.desvios.Length == 0) continue;

                matices[m.personalidad.Trim().ToLowerInvariant()] =
                    new Matiz(m.prefijoRevelado ?? string.Empty, m.desvios);
            }

            if (matices.Count == 0) return false;

            banco = new PersonalityStyleBank(matices);
            return true;
        }

        /// <summary>Prefijo fijo por personalidad; "" para personalidad desconocida o None (AD10).</summary>
        public string PrefijoRevelado(PersonalityId personalidad)
        {
            if (!personalidad.IsNone && _matices.TryGetValue(personalidad.Value, out var matiz))
                return matiz.PrefijoRevelado;

            return string.Empty;
        }

        /// <summary>
        /// Desvio determinista: <c>desvios[indiceRequerimiento % desvios.Count]</c> (AD7). NUNCA
        /// recibe ni conoce el texto del requerimiento, asi que no puede filtrarlo (AD6). Un
        /// indice negativo o fuera de rango nunca lanza: el modulo se normaliza a positivo antes
        /// de indexar.
        /// </summary>
        public string Desvio(PersonalityId personalidad, int indiceRequerimiento)
        {
            if (personalidad.IsNone || !_matices.TryGetValue(personalidad.Value, out var matiz))
                return DesvioDeFallback;

            var cantidad = matiz.Desvios.Length;
            var indice = ((indiceRequerimiento % cantidad) + cantidad) % cantidad;
            return matiz.Desvios[indice];
        }

        private readonly struct Matiz
        {
            public readonly string PrefijoRevelado;
            public readonly string[] Desvios;

            public Matiz(string prefijoRevelado, string[] desvios)
            {
                PrefijoRevelado = prefijoRevelado;
                Desvios = desvios;
            }
        }

        // Espejo del esquema JSON de matices.json. Nombres de campo en minusculas a proposito:
        // JsonUtility empareja por nombre exacto. [Serializable] es obligatorio en toda clase
        // anidada dentro de otra (mismo motivo que RequirementCaseLoader.RawCase).
        [Serializable]
        private sealed class RawBanco
        {
            public RawMatiz[] matices;
        }

        [Serializable]
        private sealed class RawMatiz
        {
            public string personalidad;
            public string prefijoRevelado;
            public string[] desvios;
        }
    }
}
