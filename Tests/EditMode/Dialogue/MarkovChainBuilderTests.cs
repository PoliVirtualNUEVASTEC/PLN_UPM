using System;
using System.Collections.Generic;
using System.Linq;
using NpcAi.Dialogue;
using NUnit.Framework;

namespace NpcAi.Dialogue.Tests
{
    /// <summary>
    /// Unidad de <see cref="MarkovChainBuilder"/>: tolera corpus degenerados sin lanzar
    /// y nunca inventa palabras fuera del vocabulario de entrada.
    /// </summary>
    public class MarkovChainBuilderTests
    {
        private static string[] Tokens(string s) =>
            s.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

        [Test]
        public void Corpus_vacio_produce_paseo_vacio_sin_lanzar()
        {
            var builder = new MarkovChainBuilder(new List<string>());

            Assert.DoesNotThrow(() =>
            {
                var salida = builder.Walk(new System.Random(1), 20);
                Assert.AreEqual(string.Empty, salida);
            });
        }

        [Test]
        public void Corpus_nulo_produce_paseo_vacio_sin_lanzar()
        {
            var builder = new MarkovChainBuilder(null);

            Assert.AreEqual(string.Empty, builder.Walk(new System.Random(1), 20));
        }

        [Test]
        public void Una_frase_de_una_sola_palabra_no_lanza_y_no_aporta_cadena()
        {
            var builder = new MarkovChainBuilder(new[] { "hola" });

            Assert.DoesNotThrow(() => builder.Walk(new System.Random(1), 20));
            Assert.AreEqual(string.Empty, builder.Walk(new System.Random(1), 20));
        }

        [Test]
        public void El_paseo_solo_usa_palabras_del_corpus_semilla()
        {
            var semilla = new[]
            {
                "me duele el pecho desde anoche",
                "me falta el aire al caminar",
                "el dolor baja por el brazo izquierdo",
            };
            var vocabulario = new HashSet<string>(semilla.SelectMany(Tokens));
            var builder = new MarkovChainBuilder(semilla);

            for (var seed = 0; seed < 200; seed++)
            {
                var salida = builder.Walk(new System.Random(seed), 30);

                Assert.IsNotEmpty(salida, "con varias frases semilla el paseo no debe ser vacio");
                foreach (var palabra in Tokens(salida))
                    Assert.IsTrue(vocabulario.Contains(palabra),
                        $"el paseo invento la palabra '{palabra}', ausente del corpus semilla");
            }
        }

        [Test]
        public void El_paseo_respeta_la_cota_de_largo_maximo()
        {
            var semilla = new[] { "uno dos tres cuatro cinco seis siete ocho nueve diez once doce" };
            var builder = new MarkovChainBuilder(semilla);

            var salida = builder.Walk(new System.Random(1), 5);

            Assert.LessOrEqual(Tokens(salida).Length, 5);
        }

        [Test]
        public void Largo_maximo_menor_que_dos_produce_paseo_vacio()
        {
            var builder = new MarkovChainBuilder(new[] { "una frase cualquiera de varias palabras" });

            Assert.AreEqual(string.Empty, builder.Walk(new System.Random(1), 1));
        }
    }
}
