using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NpcAi.Core;
using NUnit.Framework;

namespace NpcAi.Core.Tests
{
    /// <summary>
    /// G1 del contrato v1: ata <see cref="Contract.Version"/> al mayor encabezado
    /// <c>## v&lt;N&gt;</c> de <c>Docs/CONTRACT-CHANGELOG.md</c>. La ruta es fija y por
    /// eso fragil: si el paquete se renombra o el archivo no esta, la prueba llama
    /// <see cref="Assert.Ignore(string)"/> en vez de fallar (decision D4 del diseno).
    /// </summary>
    public class ContractVersionChangelogTests
    {
        private const string RutaRelativa =
            "Packages/com.poli.npc-ai/Docs/CONTRACT-CHANGELOG.md";

        [Test]
        public void Contract_Version_coincide_con_el_mayor_encabezado_del_changelog()
        {
            var ruta = Path.GetFullPath(RutaRelativa);

            if (!File.Exists(ruta))
            {
                Assert.Ignore($"No existe el changelog en '{ruta}'; no se puede atar la version.");
            }
            else
            {
                var encabezados = Regex.Matches(
                    File.ReadAllText(ruta), @"^##\s+v(\d+)", RegexOptions.Multiline);

                Assert.That(encabezados.Count, Is.GreaterThan(0),
                    "El changelog no tiene ningun encabezado con el formato '## v<N>'.");

                var mayorVersion = encabezados
                    .Cast<Match>()
                    .Select(m => int.Parse(m.Groups[1].Value))
                    .Max();

                Assert.AreEqual(Contract.Version, mayorVersion,
                    "Contract.Version debe ser igual al mayor '## v<N>' del changelog. " +
                    "Si subes la version, agrega su entrada al changelog; y a la inversa.");
            }
        }
    }
}
