using NpcAi.Core;
using NUnit.Framework;

namespace NpcAi.RequirementResponse.Tests
{
    /// <summary>
    /// Banco de estilo por personalidad (AD6/AD7/AD10 de <c>design.md</c>): el prefijo de
    /// revelacion y las frases de desvio son dato (<c>Data/Requirements/matices.json</c>), no
    /// una tabla <c>switch</c> en C# como hizo <c>ClinicalResponder.ConMatiz</c> (M15) —
    /// agregar una personalidad no debe tocar ni una clase (regla 7 del repo). El banco NUNCA
    /// recibe ni conoce <c>Requerimiento.Respuesta</c>: no puede filtrarla por construccion.
    /// </summary>
    public class PersonalityStyleBankTests
    {
        // Espejo de Data/Requirements/matices.json: 4 personalidades, "grosero" con 2 desvios
        // para probar la rotacion (AD7) y "introvertido" con prefijoRevelado vacio (patron
        // legitimo, no invalido).
        private const string JsonDeMaticesValido = @"{
            ""matices"": [
                { ""personalidad"": ""grosero"", ""prefijoRevelado"": ""Ya se lo dije, "",
                  ""desvios"": [""Eso no se lo voy a explicar ahora."", ""No estamos para esos detalles.""] },
                { ""personalidad"": ""empatico"", ""prefijoRevelado"": ""Claro, con gusto. "",
                  ""desvios"": [""Prefiero que primero nos conozcamos un poco mas.""] },
                { ""personalidad"": ""histerico"", ""prefijoRevelado"": ""Uf, si! "",
                  ""desvios"": [""No, no, eso todavia no!""] },
                { ""personalidad"": ""introvertido"", ""prefijoRevelado"": """",
                  ""desvios"": [""Preferiria no hablar de eso todavia.""] }
            ]
        }";

        private static readonly PersonalityId Grosero = new PersonalityId("grosero");
        private static readonly PersonalityId Empatico = new PersonalityId("empatico");
        private static readonly PersonalityId Introvertido = new PersonalityId("introvertido");
        private static readonly PersonalityId Desconocida = new PersonalityId("no-existe");

        [Test]
        public void Un_json_valido_carga_las_4_personalidades()
        {
            var cargo = PersonalityStyleBank.TryParse(JsonDeMaticesValido, out var banco);

            Assert.IsTrue(cargo, "Un JSON con las 4 personalidades DEBE cargar");
            Assert.AreEqual("Ya se lo dije, ", banco.PrefijoRevelado(Grosero));
            Assert.AreEqual("Claro, con gusto. ", banco.PrefijoRevelado(Empatico));
            Assert.AreEqual(string.Empty, banco.PrefijoRevelado(Introvertido),
                "\"introvertido\" declara prefijoRevelado vacio en el JSON: no es un fallback, es su dato");
        }

        [Test]
        public void La_rotacion_de_desvios_es_determinista_para_el_mismo_indice()
        {
            PersonalityStyleBank.TryParse(JsonDeMaticesValido, out var banco);

            var a = banco.Desvio(Grosero, 0);
            var b = banco.Desvio(Grosero, 0);
            var c = banco.Desvio(Grosero, 0);

            Assert.AreEqual(a, b, "3 llamadas con el mismo indice DEBEN dar la misma frase (AD7)");
            Assert.AreEqual(a, c, "3 llamadas con el mismo indice DEBEN dar la misma frase (AD7)");
            Assert.AreEqual("Eso no se lo voy a explicar ahora.", a);
        }

        [Test]
        public void El_indice_rota_por_modulo_sobre_la_cantidad_de_desvios()
        {
            PersonalityStyleBank.TryParse(JsonDeMaticesValido, out var banco);

            // "grosero" tiene 2 desvios: indice 0 y 1 dan frases distintas; indice 2 vuelve
            // al 0 (AD7: desvios[indice % desvios.Count]).
            Assert.AreEqual("Eso no se lo voy a explicar ahora.", banco.Desvio(Grosero, 0));
            Assert.AreEqual("No estamos para esos detalles.", banco.Desvio(Grosero, 1));
            Assert.AreEqual("Eso no se lo voy a explicar ahora.", banco.Desvio(Grosero, 2));
        }

        [Test]
        public void Un_indice_fuera_de_rango_no_lanza()
        {
            PersonalityStyleBank.TryParse(JsonDeMaticesValido, out var banco);

            Assert.DoesNotThrow(() => banco.Desvio(Grosero, 999));
            Assert.DoesNotThrow(() => banco.Desvio(Grosero, -1));
            Assert.DoesNotThrow(() => banco.Desvio(Grosero, int.MinValue));
            Assert.DoesNotThrow(() => banco.Desvio(Grosero, int.MaxValue));
        }

        [Test]
        public void Personalidad_desconocida_cae_en_el_fallback_con_texto_no_vacio()
        {
            PersonalityStyleBank.TryParse(JsonDeMaticesValido, out var banco);

            var prefijo = banco.PrefijoRevelado(Desconocida);
            var desvio = banco.Desvio(Desconocida, 0);

            Assert.AreEqual(string.Empty, prefijo, "Personalidad desconocida: prefijo vacio (AD10)");
            Assert.IsFalse(string.IsNullOrWhiteSpace(desvio), "Personalidad desconocida: desvio NO vacio (AD10)");
        }

        [Test]
        public void PersonalityId_None_cae_en_el_fallback_con_texto_no_vacio()
        {
            PersonalityStyleBank.TryParse(JsonDeMaticesValido, out var banco);

            var prefijo = banco.PrefijoRevelado(PersonalityId.None);
            var desvio = banco.Desvio(PersonalityId.None, 0);

            Assert.AreEqual(string.Empty, prefijo);
            Assert.IsFalse(string.IsNullOrWhiteSpace(desvio));
        }

        [Test]
        public void Json_basura_no_lanza_y_devuelve_false()
        {
            var resultado = true;
            Assert.DoesNotThrow(() => resultado = PersonalityStyleBank.TryParse("esto no es json {{{", out _));

            Assert.IsFalse(resultado);
        }

        [Test]
        public void Json_nulo_vacio_o_solo_espacios_devuelve_false_sin_lanzar()
        {
            Assert.IsFalse(PersonalityStyleBank.TryParse(null, out _));
            Assert.IsFalse(PersonalityStyleBank.TryParse(string.Empty, out _));
            Assert.IsFalse(PersonalityStyleBank.TryParse("   ", out _));
        }

        [Test]
        public void El_banco_de_fallback_da_texto_no_vacio_y_prefijo_vacio_para_cualquier_personalidad()
        {
            // AD10: la garantia del contrato ("Reply.Text nunca vacio con AunNoRevelado") no
            // puede depender de que matices.json exista o valide, asi que vive en codigo.
            Assert.AreEqual(string.Empty, PersonalityStyleBank.Fallback.PrefijoRevelado(Grosero));
            Assert.IsFalse(string.IsNullOrWhiteSpace(PersonalityStyleBank.Fallback.Desvio(Grosero, 0)));
            Assert.DoesNotThrow(() => PersonalityStyleBank.Fallback.Desvio(Grosero, 999));
        }
    }
}
