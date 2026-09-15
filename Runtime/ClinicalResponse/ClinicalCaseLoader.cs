using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace NpcAi.ClinicalResponse
{
    /// <summary>
    /// De JSON (esquema de <c>Data/Cases/README.md</c>, M14) a <see cref="ClinicalCase"/>.
    /// Deliberadamente NO mapea el bloque <c>clave</c>: las clases <c>Raw*</c> de abajo ni
    /// siquiera lo declaran, asi que <c>JsonUtility</c> simplemente lo ignora al leer el JSON.
    /// Recibe <c>string</c>, no <c>TextAsset</c>: de donde salen esos bytes (Resources, ruta de
    /// paquete, etc.) lo decide quien inyecte el <c>Func&lt;ClinicalCaseId,string&gt;</c> de
    /// <see cref="ClinicalResponder"/> (M11), no este cargador.
    /// </summary>
    public static class ClinicalCaseLoader
    {
        private const int MinimoHechos = 8;

        public static bool TryParse(string json, out ClinicalCase caso)
        {
            caso = null;
            if (string.IsNullOrWhiteSpace(json)) return false;

            RawCase raw;
            try { raw = JsonUtility.FromJson<RawCase>(json); }
            catch (Exception) { return false; }

            if (raw == null || string.IsNullOrWhiteSpace(raw.id) || raw.paciente == null)
                return false;

            var hechos = (raw.hechos ?? Array.Empty<RawHecho>())
                .Where(h => h != null
                    && !string.IsNullOrWhiteSpace(h.campo)
                    && !string.IsNullOrWhiteSpace(h.respuesta)
                    && h.ejemplosDePregunta != null && h.ejemplosDePregunta.Length > 0)
                .Select(h => new Hecho(h.campo, h.ejemplosDePregunta, h.respuesta))
                .ToList();

            if (hechos.Count < MinimoHechos) return false;

            var sv = raw.paciente.signosVitales;
            var signosVitales = new SignosVitales(
                sv?.fcLpm ?? 0, sv?.taMmHg, sv?.frRpm ?? 0, sv?.satO2Pct ?? 0,
                sv?.glasgow, sv?.temperaturaC);

            var paciente = new Paciente(
                raw.paciente.edad,
                raw.paciente.acompanamiento,
                raw.paciente.motivoConsulta,
                raw.paciente.sintomas,
                raw.paciente.antecedentes,
                raw.paciente.alergias,
                raw.paciente.medicacionActual,
                signosVitales);

            caso = new ClinicalCase(raw.id, paciente, hechos);
            return true;
        }

        // Espejo exacto del esquema JSON de M14 (Data/Cases/README.md), SIN el bloque
        // "clave". Nombres de campo en minusculas a proposito: JsonUtility empareja por
        // nombre exacto contra las claves del JSON.
        private sealed class RawCase
        {
            public string id;
            public RawPaciente paciente;
            public RawHecho[] hechos;
        }

        private sealed class RawPaciente
        {
            public int edad;
            public string acompanamiento;
            public string motivoConsulta;
            public string[] sintomas;
            public string[] antecedentes;
            public string[] alergias;
            public string[] medicacionActual;
            public RawSignosVitales signosVitales;
        }

        private sealed class RawSignosVitales
        {
            public int fcLpm;
            public string taMmHg;
            public int frRpm;
            public int satO2Pct;
            public string glasgow;
            public string temperaturaC;
        }

        private sealed class RawHecho
        {
            public string campo;
            public string[] ejemplosDePregunta;
            public string respuesta;
        }
    }
}
