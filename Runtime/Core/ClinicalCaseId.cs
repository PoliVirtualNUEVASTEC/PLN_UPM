using System;

namespace NpcAi.Core
{
    /// <summary>
    /// Identificador de un caso clinico.
    /// <para>
    /// Deliberadamente NO es un enum: el catalogo de casos clinicos es un dato de M14
    /// (archivos en <c>Data/Cases/</c>), no parte del contrato. Pasar de 3 a 20 casos
    /// agrega archivos y no toca ni una linea de <c>NpcAi.Core</c>. M15 resuelve el
    /// contenido del caso (sintomas, antecedentes, tabla de hechos) contra este id.
    /// </para>
    /// </summary>
    public readonly struct ClinicalCaseId : IEquatable<ClinicalCaseId>
    {
        /// <summary>Identificador estable en minusculas y sin tildes, p. ej. "caso-01".</summary>
        public readonly string Value;

        public ClinicalCaseId(string value)
        {
            Value = string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToLowerInvariant();
        }

        /// <summary>Valor por defecto: ningun caso clinico asignado.</summary>
        public static readonly ClinicalCaseId None = default;

        public bool IsNone => string.IsNullOrEmpty(Value);

        public bool Equals(ClinicalCaseId other) =>
            string.Equals(Value, other.Value, StringComparison.Ordinal);

        public override bool Equals(object obj) => obj is ClinicalCaseId other && Equals(other);

        public override int GetHashCode() => Value == null ? 0 : Value.GetHashCode();

        public override string ToString() => Value ?? "(none)";

        public static bool operator ==(ClinicalCaseId a, ClinicalCaseId b) => a.Equals(b);
        public static bool operator !=(ClinicalCaseId a, ClinicalCaseId b) => !a.Equals(b);
    }
}
