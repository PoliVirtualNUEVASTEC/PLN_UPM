using System.Linq;
using System.Reflection;
using NpcAi.Core;
using NUnit.Framework;

namespace NpcAi.Core.Tests
{
    /// <summary>
    /// G2 del contrato v1: fija la pureza del ensamblado <c>NpcAi.Core</c> por reflexion
    /// sobre el binario ya compilado, no sobre la intencion declarada en el <c>.asmdef</c>.
    /// El ensamblado NO DEBE referenciar <c>UnityEngine*</c>/<c>UnityEditor*</c> ni ningun
    /// otro ensamblado <c>NpcAi.*</c>: es la raiz del grafo de modulos.
    /// </summary>
    public class CoreAssemblyPurityTests
    {
        private static AssemblyName[] ReferenciasDeCore() =>
            typeof(Contract).Assembly.GetReferencedAssemblies();

        [Test]
        public void NpcAi_Core_no_referencia_ensamblados_de_Unity()
        {
            var deUnity = ReferenciasDeCore()
                .Select(a => a.Name)
                .Where(n => n.StartsWith("UnityEngine") || n.StartsWith("UnityEditor"))
                .ToArray();

            Assert.IsEmpty(deUnity,
                "NpcAi.Core compila con noEngineReferences: true. Referencias Unity encontradas: " +
                string.Join(", ", deUnity));
        }

        [Test]
        public void NpcAi_Core_no_referencia_otros_modulos_NpcAi()
        {
            var propio = typeof(Contract).Assembly.GetName().Name;

            var deOtrosModulos = ReferenciasDeCore()
                .Select(a => a.Name)
                .Where(n => n.StartsWith("NpcAi.") && n != propio)
                .ToArray();

            Assert.IsEmpty(deOtrosModulos,
                "NpcAi.Core es la raiz del grafo: no debe referenciar otro modulo NpcAi.*. Encontradas: " +
                string.Join(", ", deOtrosModulos));
        }
    }
}
