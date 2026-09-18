using NUnit.Framework;

namespace NpcAi.Scenarios.Emergency.Tests
{
    /// <summary>
    /// <see cref="TriageKeyLoader.TryParse"/> contra JSON embebido en memoria (nunca
    /// <c>Data/Cases/</c>). Espejo de las pruebas de <c>ClinicalCaseLoader</c> (M15): caso
    /// feliz, <c>clave</c> ausente, JSON malformado, <c>triajeEsperado</c> fuera de rango,
    /// y los casos borde propios de M9 (AD3, AD11).
    /// </summary>
    public class TriageKeyLoaderTests
    {
        private const string JsonCasoFeliz = @"{
            ""id"": ""caso-01"",
            ""clave"": {
                ""triajeEsperado"": ""II"",
                ""tiempoAtencion"": ""< 30 min"",
                ""banderasRojas"": [""Bandera uno"", ""Bandera dos""],
                ""cierreEsperado"": ""Explicar la clasificacion obtenida.""
            }
        }";

        [Test]
        public void TryParse_con_clave_bien_formada_devuelve_true_y_puebla_todos_los_campos()
        {
            var resultado = TriageKeyLoader.TryParse(JsonCasoFeliz, out var clave);

            Assert.IsTrue(resultado);
            Assert.AreEqual("II", clave.TriajeEsperado);
            Assert.AreEqual("< 30 min", clave.TiempoAtencion);
            Assert.AreEqual(2, clave.BanderasRojas.Count);
            Assert.AreEqual("Bandera uno", clave.BanderasRojas[0]);
            Assert.AreEqual("Bandera dos", clave.BanderasRojas[1]);
            Assert.AreEqual("Explicar la clasificacion obtenida.", clave.CierreEsperado);
        }

        [Test]
        public void TryParse_sin_bloque_clave_devuelve_false()
        {
            const string json = @"{ ""id"": ""caso-01"" }";

            var resultado = TriageKeyLoader.TryParse(json, out var clave);

            Assert.IsFalse(resultado);
            Assert.IsNull(clave);
        }

        [Test]
        public void TryParse_con_json_malformado_no_lanza_y_devuelve_false()
        {
            const string json = "esto no es json";

            bool resultado = false;
            Assert.DoesNotThrow(() => resultado = TriageKeyLoader.TryParse(json, out _));
            Assert.IsFalse(resultado);
        }

        [Test]
        public void TryParse_con_json_nulo_o_vacio_no_lanza_y_devuelve_false()
        {
            Assert.DoesNotThrow(() => TriageKeyLoader.TryParse(null, out _));
            Assert.IsFalse(TriageKeyLoader.TryParse(null, out _));
            Assert.IsFalse(TriageKeyLoader.TryParse("", out _));
            Assert.IsFalse(TriageKeyLoader.TryParse("   ", out _));
        }

        [TestCase("VI")]
        [TestCase("")]
        [TestCase("2")]
        public void TryParse_con_triajeEsperado_fuera_de_I_a_V_devuelve_false(string triajeInvalido)
        {
            var json = @"{ ""clave"": { ""triajeEsperado"": """ + triajeInvalido + @""", ""banderasRojas"": [] } }";

            var resultado = TriageKeyLoader.TryParse(json, out var clave);

            Assert.IsFalse(resultado);
            Assert.IsNull(clave);
        }

        [Test]
        public void TryParse_con_banderasRojas_ausente_igual_carga_como_lista_vacia()
        {
            const string json = @"{ ""clave"": { ""triajeEsperado"": ""III"" } }";

            var resultado = TriageKeyLoader.TryParse(json, out var clave);

            Assert.IsTrue(resultado);
            Assert.AreEqual(0, clave.BanderasRojas.Count);
        }

        [Test]
        public void TryParse_con_banderasRojas_vacio_igual_carga()
        {
            const string json = @"{ ""clave"": { ""triajeEsperado"": ""III"", ""banderasRojas"": [] } }";

            var resultado = TriageKeyLoader.TryParse(json, out var clave);

            Assert.IsTrue(resultado);
            Assert.AreEqual(0, clave.BanderasRojas.Count);
        }

        [Test]
        public void TryParse_con_hechos_insuficientes_igual_carga_la_clave()
        {
            // AD3: TriageKeyLoader valida solo "clave", nunca "hechos" (eso es criterio de
            // ClinicalCaseLoader/M15, un contrato distinto sobre el mismo archivo). Un solo
            // "hecho" (muy por debajo del minimo de 8 que exige M15) no debe impedir la carga.
            const string json = @"{
                ""hechos"": [ { ""campo"": ""unico"", ""ejemplosDePregunta"": [""x""], ""respuesta"": ""y"" } ],
                ""clave"": { ""triajeEsperado"": ""IV"", ""banderasRojas"": [""a""] }
            }";

            var resultado = TriageKeyLoader.TryParse(json, out var clave);

            Assert.IsTrue(resultado);
            Assert.AreEqual("IV", clave.TriajeEsperado);
        }
    }
}
