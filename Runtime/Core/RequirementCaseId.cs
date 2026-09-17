using System;

namespace NpcAi.Core
{
    /// <summary>
    /// Identificador de un caso de sala de juntas.
    /// <para>
    /// Deliberadamente NO es un enum: el catalogo de casos de sala de juntas es un dato de
    /// M16 (<c>Data/</c>), no parte del contrato. Identifica el CASO, no un requerimiento
    /// dentro de el: eso lo hace <see cref="RequirementId"/>.
    /// </para>
    /// </summary>
    public readonly struct RequirementCaseId : IEquatable<RequirementCaseId>
    {
        /// <summary>Identificador estable en minusculas y sin tildes, p. ej. "caso-juntas-01".</summary>
        public readonly string Value;

        public RequirementCaseId(string value)
        {
            Value = string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToLowerInvariant();
        }

        /// <summary>Valor por defecto: ningun caso de sala de juntas asignado.</summary>
        public static readonly RequirementCaseId None = default;

        public bool IsNone => string.IsNullOrEmpty(Value);

        public bool Equals(RequirementCaseId other) =>
            string.Equals(Value, other.Value, StringComparison.Ordinal);

        public override bool Equals(object obj) => obj is RequirementCaseId other && Equals(other);

        public override int GetHashCode() => Value == null ? 0 : Value.GetHashCode();

        public override string ToString() => Value ?? "(none)";

        public static bool operator ==(RequirementCaseId a, RequirementCaseId b) => a.Equals(b);
        public static bool operator !=(RequirementCaseId a, RequirementCaseId b) => !a.Equals(b);
    }
}
