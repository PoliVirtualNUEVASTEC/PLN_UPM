using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using NpcAi.Core;

namespace NpcAi.RequirementResponse.Tests
{
    /// <summary>
    /// Valida los 4 <c>Data/Requirements/caso-juntas-0N.json</c> reales, mas el banco de
    /// desvios <c>Data/Requirements/matices.json</c>, contra el esquema y los invariantes de
    /// <c>catalogo-requerimientos-m16</c> (mismo patron que <c>ClinicalCasesDataTests</c> de
    /// M15: <c>AssetDatabase.FindAssets</c> filtrado por carpeta y prefijo, sin depender de una
    /// escena). Cada metodo traza 1:1 contra un requisito de
    /// <c>specs/catalogo-requerimientos-m16/spec.md</c> (ver su tabla de trazabilidad).
    /// </summary>
    public class RequirementCasesDataTests
    {
        private const string CarpetaRequerimientos = "Data/Requirements/";
        private const string PrefijoCasos = "caso-juntas-";
        private const string NombreArchivoMatices = "matices.json";

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

        private static string CargarMaticesDeDisco()
        {
            var path = AssetDatabase
                .FindAssets("t:TextAsset")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(p => p.Replace('\\', '/'))
                .FirstOrDefault(p => p.Contains(CarpetaRequerimientos) &&
                                      Path.GetFileName(p) == NombreArchivoMatices);

            Assert.IsNotNull(path, "Data/Requirements/matices.json debe existir en el catalogo");
            return AssetDatabase.LoadAssetAtPath<TextAsset>(path).text;
        }

        [Test]
        public void Existen_4_casos_uno_por_dominio_y_cada_uno_valida_solo()
        {
            var casos = CargarCasosDeDisco();

            CollectionAssert.AreEquivalent(IdsEsperados, casos.Keys.ToList());

            foreach (var par in casos)
                Assert.IsTrue(RequirementCaseLoader.TryParse(par.Value, out _),
                    par.Key + " debe validar de forma independiente contra RequirementCaseLoader, sin depender de los otros 3 casos");
        }

        [Test]
        public void El_id_declarado_coincide_con_el_nombre_de_archivo()
        {
            foreach (var par in CargarCasosDeDisco())
            {
                Assert.IsTrue(RequirementCaseLoader.TryParse(par.Value, out var caso),
                    par.Key + " deberia cargar contra el esquema de RequirementCaseLoader");
                Assert.AreEqual(par.Key, caso.Id,
                    "el id declarado en el JSON debe coincidir con el nombre de archivo");
            }
        }

        /// <summary>
        /// La rama negativa exacta ("un requerimiento sin campos minimos no valida") ya la
        /// cubre <c>RequirementCaseLoaderTests</c> (Fase 1, JSON sintetico en memoria). Esta
        /// prueba de DATOS confirma el complemento: que los 4 catalogos reales, tal como estan
        /// en disco, SI declaran el esquema minimo completo por cada requerimiento que
        /// sobrevive la carga — si alguno le faltara un campo, <see cref="RequirementCaseLoader"/>
        /// ya lo habria descartado en silencio (AD2) y esta prueba lo detecta aqui.
        /// </summary>
        [Test]
        public void Un_requerimiento_sin_campos_minimos_no_valida()
        {
            foreach (var par in CargarCasosDeDisco())
            {
                RequirementCaseLoader.TryParse(par.Value, out var caso);

                var idsVistos = new HashSet<string>(StringComparer.Ordinal);
                foreach (var req in caso.Requerimientos)
                {
                    Assert.IsFalse(string.IsNullOrWhiteSpace(req.Id),
                        par.Key + ": un requerimiento sin id no deberia haber sobrevivido a la carga");
                    Assert.IsTrue(idsVistos.Add(req.Id),
                        par.Key + ": el id de requerimiento '" + req.Id + "' esta repetido en el caso");
                    Assert.IsFalse(string.IsNullOrWhiteSpace(req.Respuesta),
                        par.Key + ": '" + req.Id + "' necesita una respuesta no vacia");
                    Assert.GreaterOrEqual(req.EjemplosDePregunta.Count, 1,
                        par.Key + ": '" + req.Id + "' necesita al menos un ejemploDePregunta");
                    foreach (var ejemplo in req.EjemplosDePregunta)
                        Assert.IsFalse(string.IsNullOrWhiteSpace(ejemplo),
                            par.Key + ": '" + req.Id + "' tiene un ejemploDePregunta vacio");
                    Assert.IsFalse(string.IsNullOrWhiteSpace(req.EmotionTag),
                        par.Key + ": '" + req.Id + "' debe traer emotionTag (dato o default 'neutral')");
                    Assert.IsFalse(string.IsNullOrWhiteSpace(req.AnimationCue),
                        par.Key + ": '" + req.Id + "' debe traer animationCue (dato o default 'idle')");
                }
            }
        }

        [Test]
        public void Cada_caso_cubre_los_3_niveles_de_receptividad_con_al_menos_uno_cada_uno()
        {
            foreach (var par in CargarCasosDeDisco())
            {
                RequirementCaseLoader.TryParse(par.Value, out var caso);
                var nivelesPresentes = caso.Requerimientos
                    .Select(r => r.ReceptividadMinima)
                    .Distinct()
                    .ToList();

                CollectionAssert.Contains(nivelesPresentes, Receptivity.NoReceptivo,
                    par.Key + ": falta al menos un requerimiento en NoReceptivo");
                CollectionAssert.Contains(nivelesPresentes, Receptivity.Neutral,
                    par.Key + ": falta al menos un requerimiento en Neutral");
                CollectionAssert.Contains(nivelesPresentes, Receptivity.Receptivo,
                    par.Key + ": falta al menos un requerimiento en Receptivo");
            }
        }

        [Test]
        public void Ninguna_entrada_del_banco_de_desvios_filtra_una_respuesta()
        {
            var maticesJson = CargarMaticesDeDisco();
            var raw = JsonUtility.FromJson<RawBancoWrapper>(maticesJson);

            Assert.IsNotNull(raw?.matices, "matices.json debe traer al menos una personalidad");

            var todosLosDesvios = raw.matices
                .Where(m => m != null && m.desvios != null)
                .SelectMany(m => m.desvios.Select(d => new { m.personalidad, desvio = d }))
                .ToList();

            Assert.IsNotEmpty(todosLosDesvios, "matices.json debe traer al menos un desvio para comparar");

            foreach (var par in CargarCasosDeDisco())
            {
                RequirementCaseLoader.TryParse(par.Value, out var caso);

                foreach (var req in caso.Requerimientos)
                foreach (var entrada in todosLosDesvios)
                    Assert.IsFalse(entrada.desvio.Contains(req.Respuesta),
                        par.Key + ": el desvio de '" + entrada.personalidad + "' contiene la respuesta de '" + req.Id + "'");
            }
        }

        // Solo para esta prueba de datos: PersonalityStyleBank (produccion) NUNCA expone la
        // lista completa de desvios por personalidad (solo Desvio(personalidad, indice), vía
        // rotacion AD7), asi que esta clase espejo lee matices.json en crudo — mismo patron que
        // ClinicalCasesDataTests usa para el bloque "clave" que ClinicalCase nunca mapea.
        // [Serializable] obligatorio: RawMatiz[] es un campo anidado dentro de RawBancoWrapper.
        [Serializable]
        private sealed class RawBancoWrapper
        {
            public RawMatiz[] matices;
        }

        [Serializable]
        private sealed class RawMatiz
        {
            public string personalidad;
            public string prefijoRevelado;
            public string[] desvios;
        }
    }
}
