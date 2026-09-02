using System.Collections.Generic;
using NpcAi.Core;
using NpcAi.Receptivity.Unity;
using NUnit.Framework;
using UnityEngine;

namespace NpcAi.Receptivity.Tests
{
    /// <summary>
    /// Paso 5 de M4: el asset es solo dato. Estas pruebas fijan que ToProfile()
    /// traduce fielmente y que BuildCatalog() arma un catalogo consultable, todo
    /// sin que el motor cambie una linea.
    /// </summary>
    public class ReceptivityProfileAssetTests
    {
        private readonly List<ReceptivityProfileAsset> _creados = new List<ReceptivityProfileAsset>();

        private ReceptivityProfileAsset NuevoAsset(string id)
        {
            var asset = ScriptableObject.CreateInstance<ReceptivityProfileAsset>();
            asset.personalityId = id;
            _creados.Add(asset);
            return asset;
        }

        [TearDown]
        public void LimpiarAssets()
        {
            foreach (var asset in _creados)
                Object.DestroyImmediate(asset);
            _creados.Clear();
        }

        [Test]
        public void ToProfile_traslada_umbrales_y_puntaje_inicial()
        {
            var asset = NuevoAsset("grosero");
            asset.umbralReceptivo = 4;
            asset.umbralNoReceptivo = -2;
            asset.limitePuntaje = 5;
            asset.puntajeInicial = -2;

            var perfil = asset.ToProfile();

            Assert.AreEqual(4, perfil.UmbralReceptivo);
            Assert.AreEqual(-2, perfil.UmbralNoReceptivo);
            Assert.AreEqual(5, perfil.LimitePuntaje);
            Assert.AreEqual(-2, perfil.PuntajeInicial);
        }

        [Test]
        public void ToProfile_traslada_las_tres_tablas_de_delta()
        {
            var asset = NuevoAsset("empatico");
            asset.porIntencion = new[]
            {
                new ReceptivityProfileAsset.IntentDelta { intent = Intent.Empatia, delta = 3 },
            };
            asset.porTono = new[]
            {
                new ReceptivityProfileAsset.ToneDelta { tone = Tone.Agresivo, delta = -2 },
            };
            asset.porAccion = new[]
            {
                new ReceptivityProfileAsset.ActionDelta { action = PhysicalAction.GestoCalma, delta = 1 },
            };

            var perfil = asset.ToProfile();

            Assert.AreEqual(3, perfil.DeltaPorIntencion(Intent.Empatia));
            Assert.AreEqual(-2, perfil.DeltaPorTono(Tone.Agresivo));
            Assert.AreEqual(1, perfil.DeltaPorAccion(PhysicalAction.GestoCalma));
            Assert.AreEqual(0, perfil.DeltaPorIntencion(Intent.SolicitudAgresiva), "lo no listado suma 0");
        }

        [Test]
        public void PersonalityId_se_normaliza()
        {
            var asset = NuevoAsset("  GROSERO ");

            Assert.AreEqual(new PersonalityId("grosero"), asset.PersonalityId);
        }

        [Test]
        public void Un_asset_sin_tablas_proyecta_un_perfil_que_no_lanza()
        {
            var asset = NuevoAsset("vacio");

            var perfil = asset.ToProfile();

            Assert.AreEqual(0, perfil.DeltaPorIntencion(Intent.Empatia));
            Assert.AreEqual(0, perfil.DeltaPorTono(Tone.Empatico));
            Assert.AreEqual(0, perfil.DeltaPorAccion(PhysicalAction.GestoCalma));
        }

        [Test]
        public void BuildCatalog_arma_un_catalogo_consultable_por_id()
        {
            var grosero = NuevoAsset("grosero");
            grosero.porIntencion = new[]
            {
                new ReceptivityProfileAsset.IntentDelta { intent = Intent.SolicitudAgresiva, delta = -3 },
            };
            var empatico = NuevoAsset("empatico");
            empatico.porIntencion = new[]
            {
                new ReceptivityProfileAsset.IntentDelta { intent = Intent.SolicitudAgresiva, delta = -1 },
            };

            var catalogo = ReceptivityProfileAsset.BuildCatalog(new[] { grosero, empatico });

            Assert.AreEqual(-3,
                catalogo.PerfilDe(new PersonalityId("grosero")).DeltaPorIntencion(Intent.SolicitudAgresiva));
            Assert.AreEqual(-1,
                catalogo.PerfilDe(new PersonalityId("empatico")).DeltaPorIntencion(Intent.SolicitudAgresiva));
            Assert.AreSame(ReceptivityProfile.Default,
                catalogo.PerfilDe(new PersonalityId("histerico")));
        }

        [Test]
        public void BuildCatalog_tolera_null_y_elementos_null()
        {
            Assert.DoesNotThrow(() => ReceptivityProfileAsset.BuildCatalog(null));

            var catalogo = ReceptivityProfileAsset.BuildCatalog(new ReceptivityProfileAsset[] { null });

            Assert.AreSame(ReceptivityProfile.Default, catalogo.PerfilDe(new PersonalityId("grosero")));
        }

        [Test]
        public void Del_asset_al_motor_real_sin_tocar_una_clase()
        {
            var asset = NuevoAsset("robot");
            asset.umbralReceptivo = 1;
            asset.umbralNoReceptivo = -1;
            asset.limitePuntaje = 10;
            asset.puntajeInicial = 0;
            asset.porIntencion = new[]
            {
                new ReceptivityProfileAsset.IntentDelta { intent = Intent.Empatia, delta = 5 },
            };

            var motor = new ReceptivityEngine(ReceptivityProfileAsset.BuildCatalog(new[] { asset }));
            motor.Reset(new PersonalityId("robot"));
            Assert.AreEqual(Core.Receptivity.Neutral, motor.Current);

            motor.Evaluate(new IntentResult(Intent.Empatia, Tone.Neutral, 1f, 0f), PhysicalAction.Ninguna);

            Assert.AreEqual(Core.Receptivity.Receptivo, motor.Current,
                "el umbral +1 del asset manda de punta a punta");
        }
    }
}
