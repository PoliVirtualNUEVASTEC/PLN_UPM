using NpcAi.Scenarios.Boardroom.Unity;
using NUnit.Framework;
using UnityEngine;

namespace NpcAi.Scenarios.Boardroom.Tests
{
    /// <summary>
    /// tasks.md 1.3: <see cref="BoardroomObjectiveSettingsAsset.ToSettings"/> copia los tres
    /// campos del <c>ScriptableObject</c> al POCO sin alterarlos, y los defaults del asset
    /// coinciden con las <c>const</c> de <see cref="BoardroomObjectiveSettings"/> (AD13). Sin
    /// <c>OnValidate</c> a proposito: el clamp/minimo vive en el constructor del POCO, no en el
    /// asset (design.md, tabla de Interfaces). RED: escrito antes de que
    /// <c>BoardroomObjectiveSettingsAsset</c> exista; no compila hasta la tarea 1.4 (GREEN).
    /// Precedente: <c>VrInputSettingsAssetTests</c> (M7), <c>PresentationSettingsAssetTests</c> (M8).
    /// </summary>
    public class BoardroomObjectiveSettingsAssetTests
    {
        private BoardroomObjectiveSettingsAsset _asset;

        [SetUp]
        public void CrearInstancia()
        {
            _asset = ScriptableObject.CreateInstance<BoardroomObjectiveSettingsAsset>();
        }

        [TearDown]
        public void DestruirInstancia()
        {
            Object.DestroyImmediate(_asset);
        }

        [Test]
        public void Defaults_del_asset_coinciden_con_las_const_del_poco()
        {
            Assert.AreEqual(BoardroomObjectiveSettings.PesoDeTratoPorDefecto, _asset.pesoDeTrato);
            Assert.AreEqual(BoardroomObjectiveSettings.PesoDeCoberturaPorDefecto, _asset.pesoDeCobertura);
            Assert.AreEqual(BoardroomObjectiveSettings.PasosDeTratoPorDefecto, _asset.pasosDeTrato);
        }

        [Test]
        public void ToSettings_copia_los_tres_campos_sin_alterarlos()
        {
            _asset.pesoDeTrato = 0.35f;
            _asset.pesoDeCobertura = 0.9f;
            _asset.pasosDeTrato = 8;

            var settings = _asset.ToSettings();

            Assert.AreEqual(0.35f, settings.PesoDeTrato, 1e-4f);
            Assert.AreEqual(0.9f, settings.PesoDeCobertura, 1e-4f);
            Assert.AreEqual(8, settings.PasosDeTrato);
        }

        [Test]
        public void ToSettings_sin_editar_el_asset_reproduce_los_valores_reales_del_escenario()
        {
            // Data/Scenarios/Boardroom.asset (tarea 1.6): pesoDeTrato 0.2, pesoDeCobertura 0.75,
            // pasosDeTrato 5 — que coinciden con los defaults del asset recien creado.
            var settings = _asset.ToSettings();

            Assert.AreEqual(0.2f, settings.PesoDeTrato, 1e-4f);
            Assert.AreEqual(0.75f, settings.PesoDeCobertura, 1e-4f);
            Assert.AreEqual(5, settings.PasosDeTrato);
        }
    }
}
