using System;
using System.Text;

namespace NpcAi.Nlu
{
    /// <summary>
    /// Utilidades de normalización y tokenización de texto para el clasificador NLU.
    /// Diseñado para ser determinista, seguro ante entradas atípicas y sin dependencias externas.
    /// </summary>
    public static class TextPreprocessor
    {
        private static readonly char[] SpaceSeparator = { ' ' };

        /// <summary>
        /// Normaliza texto a minúsculas, remueve diacríticos/tildes y convierte puntuación en espacios limpios.
        /// </summary>
        public static string Normalize(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            var sb = new StringBuilder(text.Length);
            bool lastWasSpace = false;

            for (int i = 0; i < text.Length; i++)
            {
                char c = char.ToLowerInvariant(text[i]);

                // Remover diacríticos en español
                switch (c)
                {
                    case '\u00e1': c = 'a'; break; // á
                    case '\u00e9': c = 'e'; break; // é
                    case '\u00ed': c = 'i'; break; // í
                    case '\u00f3': c = 'o'; break; // ó
                    case '\u00fa': c = 'u'; break; // ú
                    case '\u00fc': c = 'u'; break; // ü
                    case '\u00f1': c = 'n'; break; // ñ
                }

                if (char.IsLetterOrDigit(c))
                {
                    sb.Append(c);
                    lastWasSpace = false;
                }
                else
                {
                    // Cualquier signo de puntuación o whitespace se convierte en un único espacio separador
                    if (!lastWasSpace && sb.Length > 0)
                    {
                        sb.Append(' ');
                        lastWasSpace = true;
                    }
                }
            }

            // Recortar espacio final si existe
            if (sb.Length > 0 && sb[sb.Length - 1] == ' ')
                sb.Length--;

            return sb.ToString();
        }

        /// <summary>
        /// Divide el texto normalizado en tokens de palabras individuales.
        /// </summary>
        public static string[] Tokenize(string text)
        {
            var normalized = Normalize(text);
            if (string.IsNullOrEmpty(normalized))
                return Array.Empty<string>();

            return normalized.Split(SpaceSeparator, StringSplitOptions.RemoveEmptyEntries);
        }
    }
}
