using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace NpcAi.ClinicalResponse
{
    /// <summary>
    /// Normaliza texto y empareja lo que pregunta la enfermera contra la tabla de
    /// <see cref="Hecho"/> de un caso. C# puro, sin dependencia de <c>NpcAi.Nlu</c> (regla 3
    /// del repo: M15 solo referencia <c>NpcAi.Core</c>) — es un normalizador propio de ~30
    /// lineas, no el <c>SemanticMatcher</c> de M2.
    /// </summary>
    public static class ClinicalFactMatcher
    {
        /// <summary>Minusculas, sin tildes, sin signos, espacios colapsados.</summary>
        public static string Normalizar(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto)) return string.Empty;

            var descompuesto = texto.ToLowerInvariant().Normalize(NormalizationForm.FormD);
            var limpio = new StringBuilder(descompuesto.Length);

            foreach (var c in descompuesto)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark)
                    continue; // tilde/diacritico descompuesto

                limpio.Append(char.IsLetterOrDigit(c) ? c : ' ');
            }

            var palabras = limpio.ToString()
                .Split((char[])null, StringSplitOptions.RemoveEmptyEntries);

            return string.Join(" ", palabras);
        }

        /// <summary>
        /// Indice del primer <see cref="Hecho"/> cuyo(s) <c>EjemplosDePregunta</c> quedan
        /// completamente cubiertos por las palabras de <paramref name="textoNormalizado"/>
        /// (todas las palabras del ejemplo aparecen en la pregunta), o <c>-1</c> si ninguno
        /// aplica. Recorrer la lista en orden y devolver el primero que califica resuelve por
        /// construccion el empate por menor indice (AD8 de <c>design.md</c>): determinismo sin
        /// depender de orden de <see cref="Dictionary{TKey,TValue}"/> ni de <see cref="Random"/>.
        /// </summary>
        public static int Match(string textoNormalizado, IReadOnlyList<Hecho> hechos)
        {
            if (string.IsNullOrWhiteSpace(textoNormalizado) || hechos == null) return -1;

            var palabrasPregunta = new HashSet<string>(
                textoNormalizado.Split((char[])null, StringSplitOptions.RemoveEmptyEntries));
            if (palabrasPregunta.Count == 0) return -1;

            for (var i = 0; i < hechos.Count; i++)
            {
                var ejemplos = hechos[i]?.EjemplosDePregunta;
                if (ejemplos == null) continue;

                foreach (var ejemplo in ejemplos)
                {
                    var palabrasEjemplo = new HashSet<string>(
                        Normalizar(ejemplo).Split((char[])null, StringSplitOptions.RemoveEmptyEntries));

                    if (palabrasEjemplo.Count > 0 && palabrasEjemplo.IsSubsetOf(palabrasPregunta))
                        return i;
                }
            }

            return -1;
        }
    }
}
