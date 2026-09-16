using NpcAi.VrInput;
using NUnit.Framework;
using UnityEngine;

namespace NpcAi.VrInput.Tests
{
    /// <summary>
    /// tasks.md 2.1/2.3: <see cref="VrInputSettingsAsset.OnValidate"/> acota los campos fuera de
    /// rango de la tabla "Configuracion" de design.md antes de que el asset se guarde, incluidos
    /// los dos rangos cruzados (liberacion de mirada &gt;= cono de mirada; distancia de alejarse
    /// &gt;= distancia de acercarse + 0.1), y <see cref="VrInputSettingsAsset.ToSettings"/> copia
    /// el asset a un snapshot <see cref="VrInputSettings"/>. RED: escrito antes de que
    /// <c>VrInputSettingsAsset</c> exista; no compila hasta la tarea 2.3 (GREEN). Precedente:
    /// <c>SpeechSettingsAssetTests</c> (M1).
    /// </summary>
    public class VrInputSettingsAssetTests
    {
        private VrInputSettingsAsset _asset;

        [SetUp]
        public void CrearInstancia()
        {
            _asset = ScriptableObject.CreateInstance<VrInputSettingsAsset>();
        }

        [TearDown]
        public void DestruirInstancia()
        {
            Object.DestroyImmediate(_asset);
        }

        [Test]
        public void GradosDelConoDeMirada_se_acota_entre_1_y_90()
        {
            _asset.GradosDelConoDeMirada = 999f;
            _asset.OnValidate();
            Assert.AreEqual(90f, _asset.GradosDelConoDeMirada);

            _asset.GradosDelConoDeMirada = -5f;
            _asset.OnValidate();
            Assert.AreEqual(1f, _asset.GradosDelConoDeMirada);
        }

        [Test]
        public void GradosDeLiberacionDeMirada_se_acota_entre_1_y_120()
        {
            _asset.GradosDelConoDeMirada = 10f;

            _asset.GradosDeLiberacionDeMirada = 999f;
            _asset.OnValidate();
            Assert.AreEqual(120f, _asset.GradosDeLiberacionDeMirada);

            _asset.GradosDelConoDeMirada = 1f;
            _asset.GradosDeLiberacionDeMirada = -5f;
            _asset.OnValidate();
            Assert.AreEqual(1f, _asset.GradosDeLiberacionDeMirada);
        }

        [Test]
        public void GradosDeLiberacionDeMirada_nunca_queda_por_debajo_del_cono_de_mirada()
        {
            _asset.GradosDelConoDeMirada = 45f;
            _asset.GradosDeLiberacionDeMirada = 10f; // menor que el cono: banda invertida

            _asset.OnValidate();

            Assert.AreEqual(45f, _asset.GradosDelConoDeMirada);
            Assert.AreEqual(45f, _asset.GradosDeLiberacionDeMirada);
        }

        [Test]
        public void SegundosDePermanenciaDeMirada_se_acota_entre_0_y_5()
        {
            _asset.SegundosDePermanenciaDeMirada = -1f;
            _asset.OnValidate();
            Assert.AreEqual(0f, _asset.SegundosDePermanenciaDeMirada);

            _asset.SegundosDePermanenciaDeMirada = 999f;
            _asset.OnValidate();
            Assert.AreEqual(5f, _asset.SegundosDePermanenciaDeMirada);
        }

        [Test]
        public void MetrosParaAcercarse_se_acota_entre_0_1_y_10()
        {
            _asset.MetrosParaAcercarse = 0f;
            _asset.OnValidate();
            Assert.AreEqual(0.1f, _asset.MetrosParaAcercarse);

            _asset.MetrosParaAcercarse = 999f;
            _asset.OnValidate();
            Assert.AreEqual(10f, _asset.MetrosParaAcercarse);
        }

        [Test]
        public void MetrosParaAlejarse_se_acota_entre_0_2_y_20()
        {
            _asset.MetrosParaAcercarse = 0.1f;

            _asset.MetrosParaAlejarse = 0f;
            _asset.OnValidate();
            Assert.AreEqual(0.2f, _asset.MetrosParaAlejarse);

            _asset.MetrosParaAcercarse = 0.1f;
            _asset.MetrosParaAlejarse = 999f;
            _asset.OnValidate();
            Assert.AreEqual(20f, _asset.MetrosParaAlejarse);
        }

        [Test]
        public void MetrosParaAlejarse_nunca_queda_por_debajo_de_acercarse_mas_0_1()
        {
            _asset.MetrosParaAcercarse = 5f;
            _asset.MetrosParaAlejarse = 5f; // banda invertida: igual al umbral de acercarse

            _asset.OnValidate();

            Assert.AreEqual(5f, _asset.MetrosParaAcercarse);
            Assert.AreEqual(5.1f, _asset.MetrosParaAlejarse, 0.0001f);
        }

        [Test]
        public void SegundosDeEnfriamientoDeContacto_se_acota_entre_0_y_10()
        {
            _asset.SegundosDeEnfriamientoDeContacto = -1f;
            _asset.OnValidate();
            Assert.AreEqual(0f, _asset.SegundosDeEnfriamientoDeContacto);

            _asset.SegundosDeEnfriamientoDeContacto = 999f;
            _asset.OnValidate();
            Assert.AreEqual(10f, _asset.SegundosDeEnfriamientoDeContacto);
        }

        [Test]
        public void ToSettings_copia_los_campos_del_asset_al_snapshot()
        {
            _asset.GradosDelConoDeMirada = 15f;
            _asset.GradosDeLiberacionDeMirada = 25f;
            _asset.SegundosDePermanenciaDeMirada = 0.8f;
            _asset.MetrosParaAcercarse = 1.5f;
            _asset.MetrosParaAlejarse = 2.5f;
            _asset.SegundosDeEnfriamientoDeContacto = 1.2f;

            var settings = _asset.ToSettings();

            Assert.AreEqual(15f, settings.GradosDelConoDeMirada);
            Assert.AreEqual(25f, settings.GradosDeLiberacionDeMirada);
            Assert.AreEqual(0.8f, settings.SegundosDePermanenciaDeMirada);
            Assert.AreEqual(1.5f, settings.MetrosParaAcercarse);
            Assert.AreEqual(2.5f, settings.MetrosParaAlejarse);
            Assert.AreEqual(1.2f, settings.SegundosDeEnfriamientoDeContacto);
        }
    }
}
