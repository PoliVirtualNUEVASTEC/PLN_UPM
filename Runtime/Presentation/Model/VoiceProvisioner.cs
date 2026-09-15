using System;
using System.IO;
using System.IO.Compression;

namespace NpcAi.Presentation.Model
{
    /// <summary>
    /// Aprovisiona cualquier blob comprimido de M8 (tasks.md 3.3/3.4; design.md Decision 2):
    /// una voz Piper (<c>&lt;idDeVoz&gt;.bytes</c>, contiene <c>.onnx</c> + <c>.onnx.json</c>) o
    /// los datos de fonemizacion (<c>EspeakNgData.bytes</c>). Mismo mecanismo que
    /// <c>NpcAi.Speech.Model.SpeechModelProvisioner</c> de M1, "aplicada tal cual" (design.md):
    /// <see cref="ZipArchive"/> sobre <see cref="MemoryStream"/> (.NET Standard 2.0), NO
    /// <c>ZipFile.ExtractToDirectory</c>, y rechaza cualquier entrada cuya ruta resuelta se
    /// salga de la raiz de destino (proteccion contra path traversal). Idempotente: un
    /// centinela <c>.listo</c> con el id del blob evita volver a extraer en llamadas
    /// posteriores. La raiz de destino es siempre inyectada por quien llama (en produccion,
    /// <c>Application.persistentDataPath</c>; en pruebas, un directorio temporal) para que
    /// esta clase se mantenga sin <c>UnityEngine</c> y probable en EditMode.
    /// </summary>
    internal static class VoiceProvisioner
    {
        private const string NombreDelCentinela = ".listo";

        /// <summary>
        /// Descomprime <paramref name="blobComprimido"/> dentro de
        /// <c><paramref name="raizDestino"/>/<paramref name="idDeBlob"/></c> si aun no
        /// estaba aprovisionado, y devuelve esa ruta. Si el centinela ya coincide con
        /// <paramref name="idDeBlob"/>, no vuelve a tocar el disco (no-op idempotente).
        /// </summary>
        internal static string Aprovisionar(byte[] blobComprimido, string idDeBlob, string raizDestino)
        {
            if (blobComprimido == null) throw new ArgumentNullException(nameof(blobComprimido));
            if (string.IsNullOrEmpty(idDeBlob)) throw new ArgumentException("idDeBlob requerido.", nameof(idDeBlob));
            if (string.IsNullOrEmpty(raizDestino)) throw new ArgumentException("raizDestino requerido.", nameof(raizDestino));

            var destino = Path.Combine(raizDestino, idDeBlob);
            var centinela = Path.Combine(destino, NombreDelCentinela);

            if (File.Exists(centinela) && File.ReadAllText(centinela) == idDeBlob)
                return destino; // ya aprovisionado: no-op

            Directory.CreateDirectory(destino);
            ExtraerEnMemoria(blobComprimido, destino);
            File.WriteAllText(centinela, idDeBlob);
            return destino;
        }

        private static void ExtraerEnMemoria(byte[] blobComprimido, string destino)
        {
            var raizCompleta = Path.GetFullPath(destino + Path.DirectorySeparatorChar);

            using (var flujo = new MemoryStream(blobComprimido))
            using (var zip = new ZipArchive(flujo, ZipArchiveMode.Read))
            {
                foreach (var entrada in zip.Entries)
                {
                    if (string.IsNullOrEmpty(entrada.Name)) continue; // entrada de directorio

                    var rutaDestino = Path.GetFullPath(Path.Combine(destino, entrada.FullName));
                    if (!rutaDestino.StartsWith(raizCompleta, StringComparison.Ordinal))
                        throw new InvalidOperationException(
                            "Entrada de zip fuera de la raiz de destino: '" + entrada.FullName + "'.");

                    var carpetaDestino = Path.GetDirectoryName(rutaDestino);
                    if (!string.IsNullOrEmpty(carpetaDestino))
                        Directory.CreateDirectory(carpetaDestino);

                    using (var origenEntrada = entrada.Open())
                    using (var destinoArchivo = File.Create(rutaDestino))
                    {
                        origenEntrada.CopyTo(destinoArchivo);
                    }
                }
            }
        }
    }
}
