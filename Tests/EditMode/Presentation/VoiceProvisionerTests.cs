using System;
using System.IO;
using System.IO.Compression;
using NpcAi.Presentation.Model;
using NUnit.Framework;

namespace NpcAi.Presentation.Tests
{
    /// <summary>
    /// tasks.md 3.4: <see cref="VoiceProvisioner"/> descomprime un blob empaquetado (voz Piper o
    /// datos de espeak-ng, design.md Decision 2: "TextAsset + extraccion") a una raiz inyectada,
    /// sin volver a extraer si ya esta aprovisionado, y rechaza entradas de zip que intenten
    /// salir de la raiz de destino (traversal). Espejo directo de
    /// <c>NpcAi.Speech.Tests.SpeechModelProvisionerTests</c> (M1).
    /// </summary>
    public class VoiceProvisionerTests
    {
        private string _raizDeLaPrueba;

        [SetUp]
        public void ConfigurarRaizTemporal()
        {
            _raizDeLaPrueba = Path.Combine(Path.GetTempPath(), "NpcAiPresentationTests_" + Guid.NewGuid().ToString("N"));
        }

        [TearDown]
        public void LimpiarRaizTemporal()
        {
            if (Directory.Exists(_raizDeLaPrueba))
                Directory.Delete(_raizDeLaPrueba, recursive: true);
        }

        [Test]
        public void Aprovisionar_extrae_los_archivos_del_zip_en_memoria()
        {
            var zip = ConstruirZipDeUnaEntrada("es_MX-ald-medium.onnx", "contenido-de-la-voz");

            var destino = VoiceProvisioner.Aprovisionar(zip, "es_MX-ald-medium", _raizDeLaPrueba);

            Assert.IsTrue(File.Exists(Path.Combine(destino, "es_MX-ald-medium.onnx")));
            Assert.AreEqual("contenido-de-la-voz", File.ReadAllText(Path.Combine(destino, "es_MX-ald-medium.onnx")));
        }

        [Test]
        public void Aprovisionar_no_vuelve_a_extraer_en_la_segunda_llamada()
        {
            var zipValido = ConstruirZipDeUnaEntrada("es_MX-ald-medium.onnx", "contenido-de-la-voz");
            var destino = VoiceProvisioner.Aprovisionar(zipValido, "es_MX-ald-medium", _raizDeLaPrueba);
            File.Delete(Path.Combine(destino, "es_MX-ald-medium.onnx"));

            // Ni siquiera es un zip valido: si la segunda llamada intentara re-extraer, esto
            // lanzaria. El aprovisionamiento idempotente debe reconocer el centinela y
            // devolver la misma ruta sin tocar el archivo invalido.
            var zipInvalido = new byte[] { 0x00, 0x01, 0x02 };

            string destinoSegundaLlamada = null;
            Assert.DoesNotThrow(() =>
                destinoSegundaLlamada = VoiceProvisioner.Aprovisionar(zipInvalido, "es_MX-ald-medium", _raizDeLaPrueba));

            Assert.AreEqual(destino, destinoSegundaLlamada);
            Assert.IsFalse(File.Exists(Path.Combine(destino, "es_MX-ald-medium.onnx"))); // no-op: no volvio a extraer
        }

        [Test]
        public void Aprovisionar_rechaza_una_entrada_que_intenta_salir_de_la_raiz()
        {
            var zip = ConstruirZipDeUnaEntrada("../evil.txt", "fuera-de-la-raiz");

            Assert.Throws<InvalidOperationException>(() =>
                VoiceProvisioner.Aprovisionar(zip, "es_MX-ald-medium", _raizDeLaPrueba));

            var rutaFueraDeLaRaiz = Path.Combine(_raizDeLaPrueba, "evil.txt");
            Assert.IsFalse(File.Exists(rutaFueraDeLaRaiz));
        }

        private static byte[] ConstruirZipDeUnaEntrada(string ruta, string contenido)
        {
            var flujo = new MemoryStream();
            using (var zip = new ZipArchive(flujo, ZipArchiveMode.Create, true))
            {
                var entrada = zip.CreateEntry(ruta);
                using (var escritor = new StreamWriter(entrada.Open()))
                {
                    escritor.Write(contenido);
                }
            }
            return flujo.ToArray();
        }
    }
}
