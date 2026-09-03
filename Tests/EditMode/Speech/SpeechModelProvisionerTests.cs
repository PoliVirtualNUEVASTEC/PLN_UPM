using System;
using System.IO;
using System.IO.Compression;
using NpcAi.Speech.Model;
using NUnit.Framework;

namespace NpcAi.Speech.Tests
{
    /// <summary>
    /// tasks.md 3.5/3.6: <see cref="SpeechModelProvisioner"/> descomprime un modelo
    /// empaquetado en memoria (design.md, Decision 1: "TextAsset + extraccion") a una raiz
    /// inyectada, sin volver a extraer si ya esta aprovisionado, y rechaza entradas de zip
    /// que intenten salir de la raiz de destino (traversal). RED: escrito antes de que
    /// <c>SpeechModelProvisioner</c> exista; no compila hasta la tarea 3.6 (GREEN).
    /// </summary>
    public class SpeechModelProvisionerTests
    {
        private string _raizDeLaPrueba;

        [SetUp]
        public void ConfigurarRaizTemporal()
        {
            _raizDeLaPrueba = Path.Combine(Path.GetTempPath(), "NpcAiSpeechTests_" + Guid.NewGuid().ToString("N"));
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
            var zip = ConstruirZipDeUnaEntrada("final.mdl", "contenido-modelo");

            var destino = SpeechModelProvisioner.Aprovisionar(zip, "modelo-de-prueba", _raizDeLaPrueba);

            Assert.IsTrue(File.Exists(Path.Combine(destino, "final.mdl")));
            Assert.AreEqual("contenido-modelo", File.ReadAllText(Path.Combine(destino, "final.mdl")));
        }

        [Test]
        public void Aprovisionar_no_vuelve_a_extraer_en_la_segunda_llamada()
        {
            var zipValido = ConstruirZipDeUnaEntrada("final.mdl", "contenido-modelo");
            var destino = SpeechModelProvisioner.Aprovisionar(zipValido, "modelo-de-prueba", _raizDeLaPrueba);
            File.Delete(Path.Combine(destino, "final.mdl"));

            // Ni siquiera es un zip valido: si la segunda llamada intentara re-extraer, esto
            // lanzaria. El aprovisionamiento idempotente debe reconocer el centinela y
            // devolver la misma ruta sin tocar el archivo invalido.
            var zipInvalido = new byte[] { 0x00, 0x01, 0x02 };

            string destinoSegundaLlamada = null;
            Assert.DoesNotThrow(() =>
                destinoSegundaLlamada = SpeechModelProvisioner.Aprovisionar(zipInvalido, "modelo-de-prueba", _raizDeLaPrueba));

            Assert.AreEqual(destino, destinoSegundaLlamada);
            Assert.IsFalse(File.Exists(Path.Combine(destino, "final.mdl"))); // no-op: no volvio a extraer
        }

        [Test]
        public void Aprovisionar_rechaza_una_entrada_que_intenta_salir_de_la_raiz()
        {
            var zip = ConstruirZipDeUnaEntrada("../evil.txt", "fuera-de-la-raiz");

            Assert.Throws<InvalidOperationException>(() =>
                SpeechModelProvisioner.Aprovisionar(zip, "modelo-de-prueba", _raizDeLaPrueba));

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
