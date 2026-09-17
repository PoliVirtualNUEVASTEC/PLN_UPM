using System;

namespace NpcAi.Core
{
    /// <summary>
    /// Identificador de un requerimiento dentro de un caso de sala de juntas.
    /// <para>
    /// Deliberadamente NO es un enum: la tabla de requerimientos y su receptividad minima
    /// son dato de M16 (<c>Data/</c>), no parte del contrato. Identifica el TEMA, no el
    /// contenido: el hecho del requerimiento solo viaja en <c>Reply.Text</c> y solo cuando
    /// <see cref="RequirementOutcome.Revelado"/>.
    /// </para>
    /// </summary>
    public readonly struct RequirementId : IEquatable<RequirementId>
    {
        /// <summary>Identificador estable en minusculas y sin tildes, p. ej. "presupuesto".</summary>
        public readonly string Value;

        public RequirementId(string value)
        {
            Value = string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToLowerInvariant();
        }

        /// <summary>Valor por defecto: ningun requerimiento identificado.</summary>
        public static readonly RequirementId None = default;

        public bool IsNone => string.IsNullOrEmpty(Value);

        public bool Equals(RequirementId other) =>
            string.Equals(Value, other.Value, StringComparison.Ordinal);

        public override bool Equals(object obj) => obj is RequirementId other && Equals(other);

        public override int GetHashCode() => Value == null ? 0 : Value.GetHashCode();

        public override string ToString() => Value ?? "(none)";

        public static bool operator ==(RequirementId a, RequirementId b) => a.Equals(b);
        public static bool operator !=(RequirementId a, RequirementId b) => !a.Equals(b);
    }
}
