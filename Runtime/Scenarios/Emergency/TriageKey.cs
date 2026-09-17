using System;
using System.Collections.Generic;

namespace NpcAi.Scenarios.Emergency
{
    /// <summary>
    /// Espejo del bloque "clave" de <c>Data/Cases/*.json</c> (verificado contra
    /// <c>caso-01.json</c>). POCO sin <c>UnityEngine</c>: quien lo pobla es
    /// <see cref="TriageKeyLoader"/>. Espejo de <c>ClinicalCase</c>/<c>Hecho</c> (M15):
    /// nulos se normalizan a vacio, nunca se propagan.
    /// </summary>
    public sealed class TriageKey
    {
        /// <summary>"I".."V", ya normalizado por <see cref="TriageKeyLoader"/>.</summary>
        public string TriajeEsperado { get; }

        /// <summary>Texto libre, p. ej. "&lt; 30 min".</summary>
        public string TiempoAtencion { get; }

        /// <summary>Lista ORDENADA y estable: el indice es el contrato de <c>RegisterRedFlag</c>.</summary>
        public IReadOnlyList<string> BanderasRojas { get; }

        /// <summary>Parrafo libre. No se califica (Scope Out de la propuesta).</summary>
        public string CierreEsperado { get; }

        public TriageKey(
            string triajeEsperado,
            string tiempoAtencion,
            IReadOnlyList<string> banderasRojas,
            string cierreEsperado)
        {
            TriajeEsperado = triajeEsperado ?? string.Empty;
            TiempoAtencion = tiempoAtencion ?? string.Empty;
            BanderasRojas = banderasRojas ?? Array.Empty<string>();
            CierreEsperado = cierreEsperado ?? string.Empty;
        }
    }
}
