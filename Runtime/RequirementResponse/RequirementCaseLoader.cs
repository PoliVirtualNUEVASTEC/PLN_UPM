using System;
using System.Collections.Generic;
using UnityEngine;
using NpcAi.Core;

namespace NpcAi.RequirementResponse
{
    /// <summary>
    /// De JSON (esquema de <c>Data/Requirements/README.md</c>, M16) a
    /// <see cref="RequirementCase"/>. Espejo de <c>ClinicalCaseLoader</c> (M14/M15), con dos
    /// diferencias propias de M16: AD1 (<c>receptividadMinima</c> viaja como texto en el JSON,
    /// mapeado por nombre exacto a <see cref="Receptivity"/> via
    /// <see cref="TryMapearReceptividad"/>) y AD2 (un requerimiento con la puerta rota se
    /// descarta, no aborta el caso completo). Recibe <c>string</c>, no <c>TextAsset</c>: de
    /// donde salen esos bytes (Resources, ruta de paquete, etc.) lo decide quien inyecte el
    /// <c>Func&lt;RequirementCaseId,string&gt;</c> del respondedor real (Fase 3 de este
    /// cambio), no este cargador.
    /// </summary>
    public static class RequirementCaseLoader
    {
        private const int MinimoRequerimientos = 4;

        public static bool TryParse(string json, out RequirementCase caso)
        {
            caso = null;
            if (string.IsNullOrWhiteSpace(json)) return false;

            RawCase raw;
            try { raw = JsonUtility.FromJson<RawCase>(json); }
            catch (Exception) { return false; }

            if (raw == null || string.IsNullOrWhiteSpace(raw.id) || raw.cliente == null)
                return false;

            var requerimientos = new List<Requerimiento>();
            foreach (var r in raw.requerimientos ?? Array.Empty<RawRequerimiento>())
            {
                if (r == null) continue;
                if (string.IsNullOrWhiteSpace(r.id)) continue;
                if (string.IsNullOrWhiteSpace(r.respuesta)) continue;
                if (r.ejemplosDePregunta == null || r.ejemplosDePregunta.Length == 0) continue;
                // AD2: una receptividadMinima invalida o ausente descarta SOLO este
                // requerimiento, no el caso completo. Si tras descartar quedan menos del
                // minimo, el caso entero falla mas abajo (fail-closed real: un requerimiento
                // con la puerta rota no existe, asi que es imposible que se revele).
                if (!TryMapearReceptividad(r.receptividadMinima, out var nivel)) continue;

                requerimientos.Add(new Requerimiento(
                    r.id, nivel, r.ejemplosDePregunta, r.respuesta, r.emotionTag, r.animationCue));
            }

            if (requerimientos.Count < MinimoRequerimientos) return false;

            var cliente = new Cliente(
                raw.cliente.empresa, raw.cliente.rol, raw.cliente.proyecto, raw.cliente.contexto);

            caso = new RequirementCase(raw.id, cliente, requerimientos);
            return true;
        }

        // AD1: receptividadMinima viaja como string en el JSON ("NoReceptivo" / "Neutral" /
        // "Receptivo"), NUNCA como campo tipado Receptivity en RawRequerimiento: JsonUtility
        // mapea enums por valor numerico, no por nombre, asi que un "Receptivo" contra un
        // campo Receptivity dejaria el valor en 0 (Neutral) EN SILENCIO, aflojando o
        // endureciendo la puerta sin que nada falle. El switch sobre string ya es ordinal por
        // definicion del lenguaje: el catalogo debe escribir el nombre exacto del enum.
        private static bool TryMapearReceptividad(string nombre, out Receptivity nivel)
        {
            switch (nombre)
            {
                case "NoReceptivo": nivel = Receptivity.NoReceptivo; return true;
                case "Neutral":     nivel = Receptivity.Neutral;     return true;
                case "Receptivo":   nivel = Receptivity.Receptivo;   return true;
                default:            nivel = default;                return false;
            }
        }

        // Espejo del esquema JSON de M16 (Data/Requirements/README.md). Nombres de campo en
        // minusculas a proposito: JsonUtility empareja por nombre exacto contra las claves del
        // JSON. [Serializable] es obligatorio aqui: sin el, JsonUtility no garantiza poblar una
        // clase anidada dentro de otra (RawCliente dentro de RawCase, RawRequerimiento[]
        // dentro de RawCase, etc.) de forma consistente.
        [Serializable]
        private sealed class RawCase
        {
            public string id;
            public RawCliente cliente;
            public RawRequerimiento[] requerimientos;
        }

        [Serializable]
        private sealed class RawCliente
        {
            public string empresa;
            public string rol;
            public string proyecto;
            public string contexto;
        }

        [Serializable]
        private sealed class RawRequerimiento
        {
            public string id;
            public string receptividadMinima;
            public string[] ejemplosDePregunta;
            public string respuesta;
            public string emotionTag;
            public string animationCue;
        }
    }
}
