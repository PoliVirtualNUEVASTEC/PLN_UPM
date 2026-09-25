using System;

namespace NpcAi.Core
{
    /// <summary>
    /// Vector de embedding semantico de una oracion, producido por
    /// <see cref="ISentenceEmbedder"/> (M2).
    /// <para>
    /// Copia el arreglo recibido al construirse y nunca lo expone: <see cref="Length"/>,
    /// el indexador de solo lectura y <see cref="ToArray"/> (copia defensiva) son la unica
    /// superficie publica. Ningun campo publico expone el arreglo crudo (AD6): si un
    /// consumidor cacheara el vector por texto y mutara la referencia compartida,
    /// corromperia llamadas posteriores y romperia en silencio el determinismo bit a bit
    /// exigido por AD2.
    /// </para>
    /// <para>
    /// La dimension del vector NO es parte del contrato: depende del encoder de cada
    /// reentrenamiento de M2 (768 con DistilBERT, 384 con MiniLM). Se descubre en runtime
    /// leyendo <see cref="Length"/>, nunca comparando contra un numero fijo definido en
    /// <c>NpcAi.Core</c> (AD1).
    /// </para>
    /// </summary>
    public readonly struct SentenceEmbedding : IEquatable<SentenceEmbedding>
    {
        private readonly float[] _values;

        /// <summary><c>null</c> o longitud <c>0</c> produce <see cref="Empty"/>.</summary>
        public SentenceEmbedding(float[] values)
        {
            _values = values == null || values.Length == 0 ? null : (float[])values.Clone();
        }

        /// <summary>Valor por defecto: vector vacio, sin componentes.</summary>
        public static readonly SentenceEmbedding Empty = default;

        /// <summary>Numero de componentes del vector. Es <c>0</c> para <see cref="Empty"/>.</summary>
        public int Length => _values?.Length ?? 0;

        /// <summary><c>true</c> si y solo si <see cref="Length"/> es <c>0</c>.</summary>
        public bool IsEmpty => Length == 0;

        /// <summary>
        /// Componente en <paramref name="index"/>. Un indice fuera de rango lanza, misma
        /// semantica que un arreglo de C#.
        /// </summary>
        public float this[int index] => _values[index];

        /// <summary>
        /// Copia defensiva del vector completo. Mutar el arreglo devuelto NO afecta esta
        /// instancia ni ninguna llamada posterior de <see cref="ISentenceEmbedder.Embed"/>.
        /// </summary>
        public float[] ToArray() => _values == null ? Array.Empty<float>() : (float[])_values.Clone();

        /// <summary>
        /// Igualdad bit a bit (AD7): misma <see cref="Length"/> y, en cada componente, el
        /// mismo entero segun <see cref="BitConverter.SingleToInt32Bits"/>. Distingue
        /// <c>0f</c> de <c>-0f</c> (mismos bits, no mismo valor logico), coherente con el
        /// determinismo total de AD2.
        /// </summary>
        public bool Equals(SentenceEmbedding other)
        {
            var length = Length;
            if (length != other.Length)
                return false;

            for (var i = 0; i < length; i++)
            {
                if (BitConverter.SingleToInt32Bits(_values[i]) != BitConverter.SingleToInt32Bits(other._values[i]))
                    return false;
            }

            return true;
        }

        public override bool Equals(object obj) => obj is SentenceEmbedding other && Equals(other);

        /// <summary>Combinacion de los bits de todos los componentes. <see cref="Empty"/> hashea a <c>0</c>.</summary>
        public override int GetHashCode()
        {
            if (_values == null)
                return 0;

            var hash = 17;
            foreach (var component in _values)
                hash = unchecked(hash * 31 + BitConverter.SingleToInt32Bits(component));

            return hash;
        }

        public static bool operator ==(SentenceEmbedding a, SentenceEmbedding b) => a.Equals(b);
        public static bool operator !=(SentenceEmbedding a, SentenceEmbedding b) => !a.Equals(b);
    }
}
