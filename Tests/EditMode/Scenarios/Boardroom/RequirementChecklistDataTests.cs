using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using NpcAi.Core;

namespace NpcAi.Scenarios.Boardroom.Tests
{
    /// <summary>
    /// tasks.md 2.4: valida los 4 <c>Data/Requirements/caso-juntas-0N.json</c> REALES contra
    /// <see cref="RequirementChecklistLoader"/> (mismo patron que
    /// <c>RequirementCasesDataTests</c> de M16: <c>AssetDatabase.FindAssets</c> filtrado por
    /// carpeta y prefijo, sin depender de una escena). M10 lee el catalogo por archivo, nunca
    /// por ensamblado (regla 3): esta suite es la unica costura entre el modulo y el esquema
    /// real de M16.
    /// </summary>
    public class RequirementChecklistDataTests
    {
        private const string CarpetaRequerimientos = "Data/Requirements/";
        private const string PrefijoCasos = "caso-juntas-";

        private static readonly string[] IdsEsperados =
        {
            "caso-juntas-01", "caso-juntas-02", "caso-juntas-03", "caso-juntas-04",
        };

        private static Dictionary<string, string> CargarCasosDeDisco()
        {
            return AssetDatabase
                .FindAssets("t:TextAsset")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(path => path.Replace('\\', '/'))
                .Where(path => path.Contains(CarpetaRequerimientos))
                .Where(path => path.EndsWith(".json"))
                .Where(path => Path.GetFileName(path).StartsWith(PrefijoCasos, StringComparison.Ordinal))
                .Select(path => new
                {
                    id = Path.GetFileNameWithoutExtension(path),
                    asset = AssetDatabase.LoadAssetAtPath<TextAsset>(path),
                })
                .Where(x => x.asset != null)
                .ToDictionary(x => x.id, x => x.asset.text);
        }

        [Test]
        public void Existen_los_4_casos_y_cada_uno_proyecta_con_TryParse()
        {
            var casos = CargarCasosDeDisco();

            CollectionAssert.AreEquivalent(IdsEsperados, casos.Keys.ToList());

            foreach (var par in casos)
                Assert.IsTrue(RequirementChecklistLoader.TryParse(par.Value, out _),
                    par.Key + " deberia proyectar con RequirementChecklistLoader, sin depender de los otros 3 casos");
        }

        [Test]
        public void El_id_del_checklist_coincide_con_el_nombre_de_archivo()
        {
            foreach (var par in CargarCasosDeDisco())
            {
                Assert.IsTrue(RequirementChecklistLoader.TryParse(par.Value, out var checklist),
                    par.Key + " deberia cargar contra RequirementChecklistLoader");
                Assert.AreEqual(par.Key, checklist.Id,
                    "el id declarado en el JSON debe coincidir con el nombre de archivo");
            }
        }

        [Test]
        public void Cada_checklist_tiene_al_menos_un_requerimiento_sin_None_y_sin_duplicados()
        {
            foreach (var par in CargarCasosDeDisco())
            {
                RequirementChecklistLoader.TryParse(par.Value, out var checklist);

                Assert.GreaterOrEqual(checklist.Count, 1,
                    par.Key + ": el checklist debe tener al menos un requerimiento (AD4)");

                var vistos = new HashSet<RequirementId>();
                foreach (var id in checklist.Requerimientos)
                {
                    Assert.AreNotEqual(RequirementId.None, id,
                        par.Key + ": ningun requerimiento del checklist deberia ser RequirementId.None");
                    Assert.IsTrue(vistos.Add(id),
                        par.Key + ": el requerimiento '" + id + "' esta repetido en el checklist");
                }
            }
        }

        [Test]
        public void El_denominador_real_de_cada_caso_esta_entre_4_y_6()
        {
            // El catalogo real de M16 trae entre 4 y 6 requerimientos por caso
            // (caso-juntas-03 es el minimo, con 4). RequirementChecklistLoader no exige un
            // minimo (AD12), pero el denominador REAL de cobertura debe caer en ese rango
            // para que la aritmetica de progreso de M10 (design.md) tenga sentido.
            var casos = CargarCasosDeDisco();
            Assert.IsNotEmpty(casos, "deben existir casos reales para verificar el denominador");

            foreach (var par in casos)
            {
                RequirementChecklistLoader.TryParse(par.Value, out var checklist);

                Assert.GreaterOrEqual(checklist.Count, 4,
                    par.Key + ": el denominador real deberia ser al menos 4");
                Assert.LessOrEqual(checklist.Count, 6,
                    par.Key + ": el denominador real deberia ser a lo sumo 6");
            }
        }
    }
}
