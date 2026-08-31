using System;

namespace NpcAi.Core
{
    /// <summary>
    /// Identificador de una personalidad de NPC.
    /// <para>
    /// Deliberadamente NO es un enum: el catalogo de personalidades es un dato de M5
    /// (archivos en <c>Data/Personalities/</c>), no parte del contrato. Pasar de 4 a 8
    /// personalidades agrega archivos y no toca ni una linea de <c>NpcAi.Core</c>.
    /// </para>
    /// </summary>
    public readonly struct PersonalityId : IEquatable<PersonalityId>
    {
        /// <summary>Identificador estable en minusculas y sin tildes, p. ej. "grosero".</summary>
        public readonly string Value;

        public PersonalityId(string value)
        {
            Value = string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToLowerInvariant();
        }

        /// <summary>Valor por defecto: ninguna personalidad seleccionada.</summary>
        public static readonly PersonalityId None = default;

        public bool IsNone => string.IsNullOrEmpty(Value);

        public bool Equals(PersonalityId other) =>
            string.Equals(Value, other.Value, StringComparison.Ordinal);

        public override bool Equals(object obj) => obj is PersonalityId other && Equals(other);

        public override int GetHashCode() => Value == null ? 0 : Value.GetHashCode();

        public override string ToString() => Value ?? "(none)";

        public static bool operator ==(PersonalityId a, PersonalityId b) => a.Equals(b);
        public static bool operator !=(PersonalityId a, PersonalityId b) => !a.Equals(b);
    }
}
