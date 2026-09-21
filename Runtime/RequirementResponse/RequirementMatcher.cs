using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace NpcAi.RequirementResponse
{
    /// <summary>
    /// Normaliza texto y empareja lo que pregunta el estudiante contra la tabla de
    /// <see cref="Requerimiento"/> de un caso. Copia adaptada de <c>ClinicalFactMatcher</c>
    /// (M15): M16 solo referencia <c>NpcAi.Core</c> (regla 3 del repo), asi que no puede
    /// reutilizar el de M15 ni el <c>SemanticMatcher</c> de M2, y moverlo a Core seria un
    /// cambio de contrato (AD11 y R5 de <c>design.md</c>: duplicacion aceptada; un bug de
    /// normalizacion hay que arreglarlo en los dos). C# puro, sin UnityEngine.
    /// </summary>
    public static class RequirementMatcher
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
        /// Indice del primer <see cref="Requerimiento"/> cuyos <c>EjemplosDePregunta</c> quedan
        /// completamente cubiertos por las palabras de <paramref name="textoNormalizado"/>
        /// (todas las palabras de algun ejemplo aparecen en la pregunta), o <c>-1</c> si ninguno
        /// aplica. Recorrer la tabla en orden y devolver el primero que califica resuelve por
        /// construccion el empate por menor indice: determinismo sin <c>Random</c> y sin
        /// depender del orden de iteracion de un <c>Dictionary</c>.
        /// </summary>
        public static int Match(string textoNormalizado, IReadOnlyList<Requerimiento> requerimientos)
        {
            if (string.IsNullOrWhiteSpace(textoNormalizado) || requerimientos == null) return -1;

            var palabrasPregunta = Palabras(textoNormalizado);
            if (palabrasPregunta.Count == 0) return -1;

            for (var i = 0; i < requerimientos.Count; i++)
            {
                var ejemplos = requerimientos[i]?.EjemplosDePregunta;
                if (ejemplos == null) continue;

                foreach (var ejemplo in ejemplos)
                {
                    var palabrasEjemplo = Palabras(Normalizar(ejemplo));

                    if (palabrasEjemplo.Count > 0 && palabrasEjemplo.IsSubsetOf(palabrasPregunta))
                        return i;
                }
            }

            return -1;
        }

        private static HashSet<string> Palabras(string textoNormalizado) =>
            new HashSet<string>(textoNormalizado.Split((char[])null, StringSplitOptions.RemoveEmptyEntries));
    }
}
