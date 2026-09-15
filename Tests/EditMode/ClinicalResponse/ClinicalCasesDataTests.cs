using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace NpcAi.ClinicalResponse.Tests
{
    /// <summary>
    /// Valida los 3 <c>Data/Cases/caso-*.json</c> reales de M14 contra el esquema que
    /// <see cref="ClinicalCaseLoader"/> consume (mismo patron que
    /// <c>MarkovDialogueGeneratorTests</c> usa con <c>Data/Dialogue/</c>). Estas son las
    /// pruebas de datos que la propuesta de M14 dejo trazadas "hacia adelante" hacia M15.
    /// </summary>
    public class ClinicalCasesDataTests
    {
        private const string CarpetaCasos = "Data/Cases/";
        private const int MinimoHechos = 8;
        private static readonly string[] TriajesValidos = { "I", "II", "III", "IV", "V" };

        private static Dictionary<string, string> CargarCasosDeDisco()
        {
            return AssetDatabase
                .FindAssets("t:TextAsset")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => path.Replace('\\', '/').Contains(CarpetaCasos))
                .Where(path => path.EndsWith(".json"))
                .Select(path => new
                {
                    id = Path.GetFileNameWithoutExtension(path),
                    asset = AssetDatabase.LoadAssetAtPath<TextAsset>(path),
                })
                .Where(x => x.asset != null)
                .ToDictionary(x => x.id, x => x.asset.text);
        }

        [Test]
        public void Data_Cases_trae_exactamente_los_tres_casos_del_catalogo()
        {
            CollectionAssert.AreEquivalent(
                new[] { "caso-01", "caso-02", "caso-03" },
                CargarCasosDeDisco().Keys.ToList());
        }

        [Test]
        public void Cada_caso_carga_y_su_id_coincide_con_el_nombre_de_archivo()
        {
            foreach (var par in CargarCasosDeDisco())
            {
                Assert.IsTrue(ClinicalCaseLoader.TryParse(par.Value, out var caso),
                    par.Key + " deberia cargar contra el esquema de ClinicalCaseLoader");
                Assert.AreEqual(par.Key, caso.Id,
                    "el id declarado en el JSON debe coincidir con el nombre de archivo");
            }
        }

        [Test]
        public void Cada_caso_tiene_al_menos_el_minimo_de_hechos_con_al_menos_dos_ejemplos()
        {
            foreach (var par in CargarCasosDeDisco())
            {
                ClinicalCaseLoader.TryParse(par.Value, out var caso);

                Assert.GreaterOrEqual(caso.Hechos.Count, MinimoHechos,
                    par.Key + " debe tener al menos " + MinimoHechos + " hechos");

                foreach (var hecho in caso.Hechos)
                    Assert.GreaterOrEqual(hecho.EjemplosDePregunta.Count, 2,
                        par.Key + ": el hecho '" + hecho.Campo + "' necesita al menos 2 ejemplosDePregunta");
            }
        }

        [Test]
        public void Cada_caso_tiene_signos_vitales_completos()
        {
            foreach (var par in CargarCasosDeDisco())
            {
                ClinicalCaseLoader.TryParse(par.Value, out var caso);
                var sv = caso.Paciente.SignosVitales;

                Assert.Greater(sv.FcLpm, 0, par.Key + ": fcLpm");
                Assert.IsFalse(string.IsNullOrWhiteSpace(sv.TaMmHg), par.Key + ": taMmHg");
                Assert.Greater(sv.FrRpm, 0, par.Key + ": frRpm");
                Assert.Greater(sv.SatO2Pct, 0, par.Key + ": satO2Pct");
                Assert.IsFalse(string.IsNullOrWhiteSpace(sv.Glasgow), par.Key + ": glasgow");
                // TemperaturaC puede ser null (dato no reportado): sin aserto de presencia.
            }
        }

        [Test]
        public void El_triaje_esperado_de_cada_caso_es_una_categoria_valida()
        {
            foreach (var par in CargarCasosDeDisco())
            {
                var clave = JsonUtility.FromJson<RawClaveWrapper>(par.Value).clave;

                Assert.IsNotNull(clave, par.Key + " debe traer el bloque clave (uso exclusivo de esta prueba de datos, no de ClinicalCase)");
                CollectionAssert.Contains(TriajesValidos, clave.triajeEsperado,
                    par.Key + ": triajeEsperado debe ser I, II, III, IV o V");
            }
        }

        [Test]
        public void Ninguna_respuesta_filtra_el_triaje_esperado_ni_una_bandera_roja()
        {
            foreach (var par in CargarCasosDeDisco())
            {
                ClinicalCaseLoader.TryParse(par.Value, out var caso);
                var clave = JsonUtility.FromJson<RawClaveWrapper>(par.Value).clave;

                foreach (var hecho in caso.Hechos)
                {
                    Assert.IsFalse(hecho.Respuesta.Contains(clave.triajeEsperado),
                        par.Key + ": la respuesta de '" + hecho.Campo + "' contiene el triajeEsperado");

                    foreach (var bandera in clave.banderasRojas ?? System.Array.Empty<string>())
                        Assert.IsFalse(hecho.Respuesta.Contains(bandera),
                            par.Key + ": la respuesta de '" + hecho.Campo + "' contiene una banderaRoja textual");
                }
            }
        }

        // Solo para esta prueba de datos: ClinicalCase (produccion) NUNCA mapea "clave".
        private sealed class RawClaveWrapper
        {
            public RawClave clave;
        }

        private sealed class RawClave
        {
            public string triajeEsperado;
            public string[] banderasRojas;
        }
    }
}
