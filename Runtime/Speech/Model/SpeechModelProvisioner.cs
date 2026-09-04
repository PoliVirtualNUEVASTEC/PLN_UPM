using System;
using System.IO;
using System.IO.Compression;

namespace NpcAi.Speech.Model
{
    /// <summary>
    /// Aprovisiona el modelo empaquetado (tasks.md 3.5/3.6; design.md Decision 1: modelo
    /// como <c>TextAsset</c> comprimido + extraccion en tiempo de ejecucion). Usa
    /// <see cref="ZipArchive"/> sobre <see cref="MemoryStream"/> (.NET Standard 2.0), NO
    /// <c>ZipFile.ExtractToDirectory</c>, y rechaza cualquier entrada cuya ruta resuelta se
    /// salga de la raiz de destino (proteccion contra path traversal). Idempotente: un
    /// centinela <c>.listo</c> con el id del modelo evita volver a extraer en llamadas
    /// posteriores. La raiz de destino es siempre inyectada por quien llama (en produccion,
    /// <c>Application.persistentDataPath</c>; en pruebas, un directorio temporal) para que
    /// esta clase se mantenga sin <c>UnityEngine</c> y probable en EditMode.
    /// </summary>
    internal static class SpeechModelProvisioner
    {
        private const string NombreDelCentinela = ".listo";

        /// <summary>
        /// Descomprime <paramref name="modeloComprimido"/> dentro de
        /// <c><paramref name="raizDestino"/>/<paramref name="idDeModelo"/></c> si aun no
        /// estaba aprovisionado, y devuelve esa ruta. Si el centinela ya coincide con
        /// <paramref name="idDeModelo"/>, no vuelve a tocar el disco (no-op idempotente).
        /// </summary>
        internal static string Aprovisionar(byte[] modeloComprimido, string idDeModelo, string raizDestino)
        {
            if (modeloComprimido == null) throw new ArgumentNullException(nameof(modeloComprimido));
            if (string.IsNullOrEmpty(idDeModelo)) throw new ArgumentException("idDeModelo requerido.", nameof(idDeModelo));
            if (string.IsNullOrEmpty(raizDestino)) throw new ArgumentException("raizDestino requerido.", nameof(raizDestino));

            var destino = Path.Combine(raizDestino, idDeModelo);
            var centinela = Path.Combine(destino, NombreDelCentinela);

            if (File.Exists(centinela) && File.ReadAllText(centinela) == idDeModelo)
                return destino; // ya aprovisionado: no-op

            Directory.CreateDirectory(destino);
            ExtraerEnMemoria(modeloComprimido, destino);
            File.WriteAllText(centinela, idDeModelo);
            return destino;
        }

        private static void ExtraerEnMemoria(byte[] modeloComprimido, string destino)
        {
            var raizCompleta = Path.GetFullPath(destino + Path.DirectorySeparatorChar);

            using (var flujo = new MemoryStream(modeloComprimido))
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
