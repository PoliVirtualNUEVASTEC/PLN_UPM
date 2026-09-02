using NUnit.Framework;

namespace NpcAi.Nlu.Tests
{
    [TestFixture]
    public class TextPreprocessorTests
    {
        [TestCase(null, "")]
        [TestCase("", "")]
        [TestCase("   ", "")]
        [TestCase("\t \n \r", "")]
        public void Normaliza_entradas_vacias_o_nulas_a_string_vacio(string entrada, string esperado)
        {
            var resultado = TextPreprocessor.Normalize(entrada);
            Assert.AreEqual(esperado, resultado);
        }

        [TestCase("HOLA", "hola")]
        [TestCase("Por Favor", "por favor")]
        public void Convierte_a_minusculas(string entrada, string esperado)
        {
            var resultado = TextPreprocessor.Normalize(entrada);
            Assert.AreEqual(esperado, resultado);
        }

        [TestCase("rápido", "rapido")]
        [TestCase("información", "informacion")]
        [TestCase("vergüenza", "verguenza")]
        [TestCase("año", "ano")]
        public void Remueve_tildes_y_caracteres_especiales_espanol(string entrada, string esperado)
        {
            var resultado = TextPreprocessor.Normalize(entrada);
            Assert.AreEqual(esperado, resultado);
        }

        [TestCase("¡Hola, doctor! ¿Cómo está?", "hola doctor como esta")]
        [TestCase("Por favor... necesito ayuda!!!", "por favor necesito ayuda")]
        [TestCase("datos: 120/80 - paciente estable;", "datos 120 80 paciente estable")]
        public void Limpia_puntuacion_y_colapsa_espacios(string entrada, string esperado)
        {
            var resultado = TextPreprocessor.Normalize(entrada);
            Assert.AreEqual(esperado, resultado);
        }

        [Test]
        public void Tokeniza_palabras_correctamente()
        {
            var tokens = TextPreprocessor.Tokenize("Por favor, necesito los datos del paciente");
            CollectionAssert.AreEqual(
                new[] { "por", "favor", "necesito", "los", "datos", "del", "paciente" },
                tokens
            );
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void Tokeniza_vacio_devuelve_array_vacio(string entrada)
        {
            var tokens = TextPreprocessor.Tokenize(entrada);
            Assert.IsNotNull(tokens);
            Assert.AreEqual(0, tokens.Length);
        }
    }
}
