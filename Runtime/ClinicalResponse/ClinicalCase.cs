using System;
using System.Collections.Generic;

namespace NpcAi.ClinicalResponse
{
    /// <summary>
    /// Caso clinico ya cargado y validado, listo para que <see cref="ClinicalResponder"/> lo
    /// consulte. Espejo del bloque <c>paciente</c>/<c>hechos</c> del esquema de M14
    /// (<c>Data/Cases/README.md</c>). Deliberadamente SIN el bloque <c>clave</c>
    /// (<c>triajeEsperado</c>, <c>banderasRojas</c>, <c>cierreEsperado</c>): ese dato es de
    /// evaluacion exclusiva de M9, y al no existir aqui es estructuralmente imposible que
    /// M15 lo filtre en una respuesta.
    /// </summary>
    public sealed class ClinicalCase
    {
        public string Id { get; }
        public Paciente Paciente { get; }
        public IReadOnlyList<Hecho> Hechos { get; }

        public ClinicalCase(string id, Paciente paciente, IReadOnlyList<Hecho> hechos)
        {
            Id       = id ?? string.Empty;
            Paciente = paciente;
            Hechos   = hechos ?? Array.Empty<Hecho>();
        }
    }

    public sealed class Paciente
    {
        public int Edad { get; }
        public string Acompanamiento { get; }
        public string MotivoConsulta { get; }
        public IReadOnlyList<string> Sintomas { get; }
        public IReadOnlyList<string> Antecedentes { get; }
        public IReadOnlyList<string> Alergias { get; }
        public IReadOnlyList<string> MedicacionActual { get; }
        public SignosVitales SignosVitales { get; }

        public Paciente(
            int edad, string acompanamiento, string motivoConsulta,
            IReadOnlyList<string> sintomas, IReadOnlyList<string> antecedentes,
            IReadOnlyList<string> alergias, IReadOnlyList<string> medicacionActual,
            SignosVitales signosVitales)
        {
            Edad              = edad;
            Acompanamiento    = acompanamiento ?? string.Empty;
            MotivoConsulta    = motivoConsulta ?? string.Empty;
            Sintomas          = sintomas ?? Array.Empty<string>();
            Antecedentes      = antecedentes ?? Array.Empty<string>();
            Alergias          = alergias ?? Array.Empty<string>();
            MedicacionActual  = medicacionActual ?? Array.Empty<string>();
            SignosVitales     = signosVitales;
        }
    }

    /// <summary>
    /// <c>TaMmHg</c>, <c>Glasgow</c> y <c>TemperaturaC</c> son texto (no numero) a proposito:
    /// los dos primeros por su formato compuesto ("163/99", "15/15"); <c>TemperaturaC</c>
    /// porque el dato puede faltar (<c>null</c> en el JSON) y <c>JsonUtility</c> no soporta
    /// <c>null</c> en un campo numerico — ver <c>Data/Cases/README.md</c>.
    /// </summary>
    public sealed class SignosVitales
    {
        public int FcLpm { get; }
        public string TaMmHg { get; }
        public int FrRpm { get; }
        public int SatO2Pct { get; }
        public string Glasgow { get; }
        public string TemperaturaC { get; }

        public SignosVitales(
            int fcLpm, string taMmHg, int frRpm, int satO2Pct, string glasgow, string temperaturaC)
        {
            FcLpm        = fcLpm;
            TaMmHg       = taMmHg ?? string.Empty;
            FrRpm        = frRpm;
            SatO2Pct     = satO2Pct;
            Glasgow      = glasgow ?? string.Empty;
            TemperaturaC = temperaturaC;
        }
    }

    /// <summary>
    /// Una entrada de la tabla de recuperacion: el contrato M14 &lt;-&gt; M15.
    /// <see cref="ClinicalFactMatcher"/> compara <see cref="EjemplosDePregunta"/> contra lo que
    /// pregunta la enfermera; si empareja, <see cref="Respuesta"/> sale tal cual (en primera
    /// persona) hacia el <c>NpcReply</c>.
    /// </summary>
    public sealed class Hecho
    {
        public string Campo { get; }
        public IReadOnlyList<string> EjemplosDePregunta { get; }
        public string Respuesta { get; }

        public Hecho(string campo, IReadOnlyList<string> ejemplosDePregunta, string respuesta)
        {
            Campo              = campo ?? string.Empty;
            EjemplosDePregunta = ejemplosDePregunta ?? Array.Empty<string>();
            Respuesta          = respuesta ?? string.Empty;
        }
    }
}
