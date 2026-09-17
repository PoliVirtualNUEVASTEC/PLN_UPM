using System;
using UnityEngine;

namespace NpcAi.Scenarios.Emergency
{
    /// <summary>
    /// De JSON (esquema de <c>Data/Cases/README.md</c>, M14) a <see cref="TriageKey"/>.
    /// Deliberadamente NO mapea <c>id</c>, <c>paciente</c> ni <c>hechos</c>: las clases
    /// <c>Raw*</c> de abajo declaran unicamente <c>clave</c>, asi que <c>JsonUtility</c>
    /// simplemente ignora el resto del JSON al leerlo (AD1). No exige el minimo de 8
    /// <c>hechos</c> que exige <c>ClinicalCaseLoader</c>: valida un contrato distinto sobre
    /// el mismo archivo (AD3). Recibe <c>string</c>, nunca <c>File.ReadAllText</c>: de donde
    /// salen esos bytes lo decide quien inyecte el <c>Func&lt;ClinicalCaseId,string&gt;</c>
    /// de <see cref="TriageScenarioObjective"/>.
    /// </summary>
    public static class TriageKeyLoader
    {
        private static readonly string[] TriajesValidos = { "I", "II", "III", "IV", "V" };

        /// <summary>
        /// <c>true</c> solo si hay bloque "clave" bien formado con <c>triajeEsperado</c> en
        /// <c>{"I".."V"}</c>. Nunca lanza: JSON nulo/vacio/malformado o "clave" ausente
        /// devuelven <c>false</c>.
        /// </summary>
        public static bool TryParse(string json, out TriageKey clave)
        {
            clave = null;
            if (string.IsNullOrWhiteSpace(json)) return false;

            RawCase raw;
            try { raw = JsonUtility.FromJson<RawCase>(json); }
            catch (Exception) { return false; }

            if (raw?.clave == null) return false;
            if (Array.IndexOf(TriajesValidos, raw.clave.triajeEsperado) < 0) return false;

            clave = new TriageKey(
                raw.clave.triajeEsperado,
                raw.clave.tiempoAtencion,
                raw.clave.banderasRojas,
                raw.clave.cierreEsperado);
            return true;
        }

        // Espejo del esquema JSON de M14 (Data/Cases/README.md), declarando UNICAMENTE
        // "clave". Nombres de campo en minusculas a proposito: JsonUtility empareja por
        // nombre exacto contra las claves del JSON. [Serializable] es obligatorio: sin el,
        // JsonUtility no garantiza poblar una clase anidada dentro de otra de forma
        // consistente (mismo motivo que ClinicalCaseLoader).
        [Serializable]
        private sealed class RawCase
        {
            public RawClave clave;
        }

        [Serializable]
        private sealed class RawClave
        {
            public string triajeEsperado;
            public string tiempoAtencion;
            public string[] banderasRojas;
            public string cierreEsperado;
        }
    }
}
