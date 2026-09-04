using NpcAi.Speech;
using NpcAi.Speech.Vosk;
using NUnit.Framework;

namespace NpcAi.Speech.Tests
{
    /// <summary>
    /// tasks.md 4.4/4.5: <see cref="VoskResultParser"/> es la unica pieza del motor Vosk (PR4)
    /// que no toca <c>[DllImport]</c> ni requiere el binario nativo — parseo puro del JSON que
    /// devuelve <c>vosk_recognizer_result</c>/<c>vosk_recognizer_final_result</c> (design.md:
    /// "parseo JSON, media de conf"). El resto de <c>VoskInterop</c>/<c>VoskRecognitionEngine</c>
    /// no tiene prueba EditMode: design.md (Testing Strategy) documenta que "ninguna prueba
    /// construye VoskRecognitionEngine", y sin <c>libvosk.so</c>/<c>.dll</c> vendorizado
    /// (tasks.md 4.2/4.3, pendiente del usuario) no hay nada nativo que llamar. RED: escrito
    /// antes de que <c>VoskResultParser</c> exista; no compila hasta que se cree (GREEN).
    /// </summary>
    public class VoskResultParserTests
    {
        [Test]
        public void Parsear_calcula_la_confianza_como_el_promedio_de_conf_por_palabra()
        {
            const string json = "{\"result\":[{\"conf\":1.0,\"word\":\"hola\"},{\"conf\":0.75,\"word\":\"mundo\"}],\"text\":\"hola mundo\"}";

            var resultado = VoskResultParser.Parsear(json, muestrasAlimentadas: 16000);

            Assert.AreEqual("hola mundo", resultado.Texto);
            Assert.AreEqual(0.875f, resultado.ConfianzaCruda, 1e-4f);
            Assert.AreEqual(16000, resultado.MuestrasAlimentadas);
        }

        [Test]
        public void Parsear_sin_palabras_devuelve_confianza_cero_pero_conserva_el_texto()
        {
            const string json = "{\"text\":\"\"}";

            var resultado = VoskResultParser.Parsear(json, muestrasAlimentadas: 0);

            Assert.AreEqual(string.Empty, resultado.Texto);
            Assert.AreEqual(0f, resultado.ConfianzaCruda);
        }

        [Test]
        public void Parsear_json_nulo_o_vacio_no_lanza_y_devuelve_resultado_vacio()
        {
            var resultadoNulo = VoskResultParser.Parsear(null, muestrasAlimentadas: 5);
            var resultadoVacio = VoskResultParser.Parsear(string.Empty, muestrasAlimentadas: 5);

            Assert.AreEqual(string.Empty, resultadoNulo.Texto);
            Assert.AreEqual(0f, resultadoNulo.ConfianzaCruda);
            Assert.AreEqual(5, resultadoNulo.MuestrasAlimentadas);

            Assert.AreEqual(string.Empty, resultadoVacio.Texto);
            Assert.AreEqual(0f, resultadoVacio.ConfianzaCruda);
        }

        [Test]
        public void Parsear_json_invalido_no_lanza_y_devuelve_resultado_vacio()
        {
            const string basura = "no es json";

            RecognitionResult resultado = default;
            Assert.DoesNotThrow(() => resultado = VoskResultParser.Parsear(basura, muestrasAlimentadas: 3));

            Assert.AreEqual(string.Empty, resultado.Texto);
            Assert.AreEqual(0f, resultado.ConfianzaCruda);
        }

        [Test]
        public void Parsear_conserva_muestrasAlimentadas_sin_tocarlas()
        {
            const string json = "{\"text\":\"prueba\"}";

            var resultado = VoskResultParser.Parsear(json, muestrasAlimentadas: 4242);

            Assert.AreEqual(4242, resultado.MuestrasAlimentadas);
        }
    }
}
