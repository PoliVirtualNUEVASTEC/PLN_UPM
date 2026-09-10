using System;
using System.Collections.Generic;

namespace NpcAi.Dialogue
{
    /// <summary>
    /// Cadena de Markov de palabras de orden 2 (bigramas) construida a partir de una lista
    /// de frases semilla. <see cref="Walk"/> genera texto nuevo por paseo aleatorio sobre
    /// la tabla de transiciones. C# puro: no referencia <c>UnityEngine</c>, para poder
    /// probarlo en aislamiento y en milisegundos.
    /// </summary>
    public sealed class MarkovChainBuilder
    {
        private static readonly char[] Separadores = { ' ', '\t', '\n', '\r' };

        // (palabra anterior, palabra actual) -> palabras que pueden seguir.
        // El sentinel null en la lista marca "fin de frase".
        private readonly Dictionary<(string, string), List<string>> _transiciones =
            new Dictionary<(string, string), List<string>>();

        // Bigramas con los que puede arrancar un paseo: el primer par de cada frase semilla.
        private readonly List<(string, string)> _inicios = new List<(string, string)>();

        /// <param name="frasesSemilla">
        /// Frases de ejemplo. Las de menos de dos palabras no aportan bigramas y se ignoran;
        /// si ninguna aporta (lista vacia, nula, o solo frases de una palabra),
        /// <see cref="Walk"/> devuelve <c>""</c> y el llamador decide el respaldo.
        /// </param>
        public MarkovChainBuilder(IReadOnlyList<string> frasesSemilla)
        {
            if (frasesSemilla == null) return;

            foreach (var frase in frasesSemilla)
            {
                if (string.IsNullOrWhiteSpace(frase)) continue;

                var palabras = frase.Split(Separadores, StringSplitOptions.RemoveEmptyEntries);
                if (palabras.Length < 2) continue;

                _inicios.Add((palabras[0], palabras[1]));

                for (var i = 1; i < palabras.Length; i++)
                {
                    var estado = (palabras[i - 1], palabras[i]);
                    var siguiente = i + 1 < palabras.Length ? palabras[i + 1] : null; // null = fin

                    if (!_transiciones.TryGetValue(estado, out var candidatos))
                    {
                        candidatos = new List<string>();
                        _transiciones[estado] = candidatos;
                    }
                    candidatos.Add(siguiente);
                }
            }
        }

        /// <summary>
        /// Un paseo aleatorio sobre la cadena. Devuelve <c>""</c> si la cadena esta vacia
        /// o si <paramref name="largoMaximoPalabras"/> es menor que 2.
        /// </summary>
        /// <param name="rng">
        /// Fuente de aleatoriedad inyectada: fijar la semilla hace el paseo reproducible en pruebas.
        /// </param>
        /// <param name="largoMaximoPalabras">Cota dura de palabras: salvaguarda contra ciclos.</param>
        public string Walk(System.Random rng, int largoMaximoPalabras)
        {
            if (rng == null) throw new ArgumentNullException(nameof(rng));
            if (_inicios.Count == 0 || largoMaximoPalabras < 2) return string.Empty;

            var (a, b) = _inicios[rng.Next(_inicios.Count)];
            var salida = new List<string> { a, b };
            var estado = (a, b);

            while (salida.Count < largoMaximoPalabras
                   && _transiciones.TryGetValue(estado, out var candidatos)
                   && candidatos.Count > 0)
            {
                var siguiente = candidatos[rng.Next(candidatos.Count)];
                if (siguiente == null) break; // fin de frase
                salida.Add(siguiente);
                estado = (estado.Item2, siguiente);
            }

            return string.Join(" ", salida);
        }
    }
}
