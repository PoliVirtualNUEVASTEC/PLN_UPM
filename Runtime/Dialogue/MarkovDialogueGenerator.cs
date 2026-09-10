using System;
using System.Collections.Generic;
using System.Linq;
using NpcAi.Core;
using UnityEngine;

namespace NpcAi.Dialogue
{
    /// <summary>
    /// Implementacion real de <see cref="IDialogueGenerator"/> por cadenas de Markov.
    /// Indexa un corpus semilla por <c>(personalidad, receptividad)</c> — 12 bloques — y
    /// genera cada respuesta por paseo aleatorio sobre la cadena del bloque. NO es
    /// determinista, a proposito (ver <see cref="IDialogueGenerator"/>). Nunca devuelve
    /// texto vacio: si el paseo degenera, reintenta y, en ultimo caso, devuelve una frase
    /// semilla del mismo bloque tal cual.
    /// </summary>
    public sealed class MarkovDialogueGenerator : IDialogueGenerator
    {
        private const int MaxPalabras = 40;
        private const int MaxReintentos = 5;
        private const string RespaldoDuro = "Prefiero no responder ahora.";

        private static readonly Core.Receptivity[] Estados =
        {
            Core.Receptivity.Receptivo,
            Core.Receptivity.Neutral,
            Core.Receptivity.NoReceptivo,
        };

        private readonly Dictionary<(string, Core.Receptivity), Bloque> _bloques =
            new Dictionary<(string, Core.Receptivity), Bloque>();

        // Respaldo para PersonalityId.None o ids que no estan en el corpus: junta las frases
        // de todas las personalidades para cada estado de receptividad.
        private readonly Dictionary<Core.Receptivity, Bloque> _respaldoPorEstado =
            new Dictionary<Core.Receptivity, Bloque>();

        private readonly System.Random _rng = new System.Random();

        /// <param name="corpusPorPersonalidad">
        /// Clave: id de personalidad (<c>PersonalityId.Value</c>). Valor: el <see cref="TextAsset"/>
        /// del JSON de esa personalidad (<c>Data/Dialogue/&lt;id&gt;.json</c>). El llamador carga
        /// los assets; este tipo no toca el <c>AssetDatabase</c> (no existe en runtime).
        /// </param>
        public MarkovDialogueGenerator(IReadOnlyDictionary<string, TextAsset> corpusPorPersonalidad)
        {
            var acumuladoPorEstado = Estados.ToDictionary(e => e, _ => new List<string>());

            if (corpusPorPersonalidad != null)
            {
                foreach (var par in corpusPorPersonalidad)
                {
                    if (par.Value == null || string.IsNullOrWhiteSpace(par.Value.text)) continue;

                    var id = new PersonalityId(par.Key).Value;
                    if (id == null) continue;

                    CorpusJson datos;
                    try { datos = JsonUtility.FromJson<CorpusJson>(par.Value.text); }
                    catch (Exception) { continue; }
                    if (datos == null) continue;

                    Registrar(id, Core.Receptivity.Receptivo,   datos.Receptivo,   acumuladoPorEstado);
                    Registrar(id, Core.Receptivity.Neutral,     datos.Neutral,     acumuladoPorEstado);
                    Registrar(id, Core.Receptivity.NoReceptivo, datos.NoReceptivo, acumuladoPorEstado);
                }
            }

            foreach (var estado in Estados)
                _respaldoPorEstado[estado] = new Bloque(acumuladoPorEstado[estado]);
        }

        private void Registrar(
            string id, Core.Receptivity estado, string[] frases,
            IReadOnlyDictionary<Core.Receptivity, List<string>> acumuladoPorEstado)
        {
            var limpias = (frases ?? Array.Empty<string>())
                .Where(f => !string.IsNullOrWhiteSpace(f))
                .Select(f => f.Trim())
                .ToList();
            if (limpias.Count == 0) return;

            _bloques[(id, estado)] = new Bloque(limpias);
            acumuladoPorEstado[estado].AddRange(limpias);
        }

        public NpcReply Generate(PersonalityId personality, Core.Receptivity receptivity, IntentResult intent)
        {
            var bloque = ResolverBloque(personality, receptivity);
            var texto = GenerarTexto(bloque);
            var (emocion, animacion) = EtiquetasDe(receptivity);
            return new NpcReply(texto, emocion, animacion);
        }

        private Bloque ResolverBloque(PersonalityId personality, Core.Receptivity receptivity)
        {
            if (!personality.IsNone
                && _bloques.TryGetValue((personality.Value, receptivity), out var propio))
                return propio;

            if (_respaldoPorEstado.TryGetValue(receptivity, out var respaldo) && !respaldo.Vacio)
                return respaldo;

            // Ningun bloque para este estado: cae en cualquier respaldo no vacio, o Nada.
            return _respaldoPorEstado.Values.FirstOrDefault(b => !b.Vacio) ?? Bloque.Nada;
        }

        private string GenerarTexto(Bloque bloque)
        {
            if (bloque.Vacio) return RespaldoDuro;

            for (var intento = 0; intento < MaxReintentos; intento++)
            {
                var texto = bloque.Cadena.Walk(_rng, MaxPalabras);
                if (!string.IsNullOrWhiteSpace(texto)) return texto;
            }

            // El paseo degenero (bloque semilla muy chico): frase semilla verbatim.
            return bloque.FraseSemillaAlAzar(_rng);
        }

        private static (string emocion, string animacion) EtiquetasDe(Core.Receptivity receptivity) =>
            receptivity switch
            {
                Core.Receptivity.Receptivo   => ("receptivo", "asentir"),
                Core.Receptivity.NoReceptivo => ("molesto", "cruzar_brazos"),
                _                            => ("neutral", "idle"),
            };

        /// <summary>Un bloque <c>(personalidad, estado)</c>: sus frases semilla y su cadena de Markov.</summary>
        private sealed class Bloque
        {
            public static readonly Bloque Nada = new Bloque(new List<string>());

            private readonly IReadOnlyList<string> _frases;

            public MarkovChainBuilder Cadena { get; }
            public bool Vacio => _frases.Count == 0;

            public Bloque(IReadOnlyList<string> frases)
            {
                _frases = frases ?? (IReadOnlyList<string>)Array.Empty<string>();
                Cadena = new MarkovChainBuilder(_frases);
            }

            public string FraseSemillaAlAzar(System.Random rng) =>
                _frases.Count == 0 ? RespaldoDuro : _frases[rng.Next(_frases.Count)];
        }

        [Serializable]
        private sealed class CorpusJson
        {
            public string personalidad;
            public string[] Receptivo;
            public string[] Neutral;
            public string[] NoReceptivo;
        }
    }
}
