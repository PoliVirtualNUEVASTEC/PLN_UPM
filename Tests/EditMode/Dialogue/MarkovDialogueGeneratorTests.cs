using System.Collections.Generic;
using System.IO;
using System.Linq;
using NpcAi.Core;
using NpcAi.Core.Tests;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace NpcAi.Dialogue.Tests
{
    /// <summary>
    /// <see cref="MarkovDialogueGenerator"/> contra el contrato de <see cref="IDialogueGenerator"/>,
    /// cargando el corpus semilla real de <c>Data/Dialogue/</c> desde disco (mismo patron que
    /// <c>PersonalityProfilesDataTests</c> usa con los <c>.asset</c> de M5).
    /// </summary>
    public class MarkovDialogueGeneratorTests : DialogueGeneratorContract
    {
        private const string CarpetaCorpus = "Data/Dialogue/";

        private static readonly string[] IdsEsperados =
            { "grosero", "histerico", "introvertido", "empatico" };

        protected override IDialogueGenerator CreateSubject()
        {
            var corpus = CargarCorpusDeDisco();

            Assert.IsNotEmpty(corpus,
                "No se encontro ningun .json bajo " + CarpetaCorpus +
                ". Estas pruebas usan el corpus real de M6 (PR1); sin el no hay nada que sembrar.");

            return new MarkovDialogueGenerator(corpus);
        }

        private static Dictionary<string, TextAsset> CargarCorpusDeDisco()
        {
            return AssetDatabase
                .FindAssets("t:TextAsset")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => path.Replace('\\', '/').Contains(CarpetaCorpus))
                .Where(path => path.EndsWith(".json"))
                .Select(path => new
                {
                    id = Path.GetFileNameWithoutExtension(path),
                    asset = AssetDatabase.LoadAssetAtPath<TextAsset>(path),
                })
                .Where(x => x.asset != null)
                .ToDictionary(x => x.id, x => x.asset);
        }

        [Test]
        public void El_corpus_de_disco_trae_exactamente_las_cuatro_personalidades_de_M5()
        {
            CollectionAssert.AreEquivalent(IdsEsperados, CargarCorpusDeDisco().Keys.ToList());
        }

        [Test]
        public void Ninguna_personalidad_ni_estado_devuelve_texto_vacio_en_volumen()
        {
            var g = CreateSubject();

            foreach (var id in IdsEsperados)
            foreach (Core.Receptivity estado in System.Enum.GetValues(typeof(Core.Receptivity)))
                for (var i = 0; i < 250; i++)
                {
                    var reply = g.Generate(new PersonalityId(id), estado, IntentResult.Unknown());
                    Assert.IsFalse(reply.IsEmpty, $"{id} / {estado} / iteracion {i}");
                }
        }

        [Test]
        public void Un_id_desconocido_usa_el_respaldo_y_no_lanza()
        {
            var g = CreateSubject();

            Assert.DoesNotThrow(() =>
            {
                foreach (Core.Receptivity estado in System.Enum.GetValues(typeof(Core.Receptivity)))
                {
                    var reply = g.Generate(new PersonalityId("no-existe"), estado, IntentResult.Unknown());
                    Assert.IsFalse(reply.IsEmpty, $"estado {estado}");
                }
            });
        }
    }
}
