using NpcAi.Speech.Config;
using NpcAi.Speech.Segmentation;
using NUnit.Framework;
using UnityEngine;

namespace NpcAi.Speech.Tests
{
    /// <summary>
    /// tasks.md 3.2/3.3: <see cref="SpeechSettingsAsset.OnValidate"/> acota los campos fuera
    /// de rango de la tabla "Data/Speech/" de design.md antes de que el asset se guarde, y
    /// <see cref="SpeechSettingsAsset.ToSettings"/> copia el asset a un snapshot
    /// <see cref="SpeechSettings"/>. RED: escrito antes de que <c>SpeechSettingsAsset</c>
    /// exista; no compila hasta la tarea 3.3 (GREEN).
    /// </summary>
    public class SpeechSettingsAssetTests
    {
        private SpeechSettingsAsset _asset;

        [SetUp]
        public void CrearInstancia()
        {
            _asset = ScriptableObject.CreateInstance<SpeechSettingsAsset>();
        }

        [TearDown]
        public void DestruirInstancia()
        {
            Object.DestroyImmediate(_asset);
        }

        [Test]
        public void TasaDeMuestreo_fuera_de_rango_se_acota_entre_8000_y_48000()
        {
            _asset.TasaDeMuestreo = 999999;
            _asset.OnValidate();
            Assert.AreEqual(48000, _asset.TasaDeMuestreo);

            _asset.TasaDeMuestreo = 1;
            _asset.OnValidate();
            Assert.AreEqual(8000, _asset.TasaDeMuestreo);
        }

        [Test]
        public void UmbralDeEnergia_se_acota_entre_0_y_1()
        {
            _asset.UmbralDeEnergia = 5f;
            _asset.OnValidate();
            Assert.AreEqual(1f, _asset.UmbralDeEnergia);

            _asset.UmbralDeEnergia = -2f;
            _asset.OnValidate();
            Assert.AreEqual(0f, _asset.UmbralDeEnergia);
        }

        [Test]
        public void MsMinimosDeVoz_se_acota_entre_0_y_2000()
        {
            _asset.MsMinimosDeVoz = -50;
            _asset.OnValidate();
            Assert.AreEqual(0, _asset.MsMinimosDeVoz);

            _asset.MsMinimosDeVoz = 9000;
            _asset.OnValidate();
            Assert.AreEqual(2000, _asset.MsMinimosDeVoz);
        }

        [Test]
        public void MsDeSilencioParaCortar_se_acota_entre_100_y_3000()
        {
            _asset.MsDeSilencioParaCortar = 0;
            _asset.OnValidate();
            Assert.AreEqual(100, _asset.MsDeSilencioParaCortar);

            _asset.MsDeSilencioParaCortar = 5000;
            _asset.OnValidate();
            Assert.AreEqual(3000, _asset.MsDeSilencioParaCortar);
        }

        [Test]
        public void MsDePreRoll_se_acota_entre_0_y_1000()
        {
            _asset.MsDePreRoll = -1;
            _asset.OnValidate();
            Assert.AreEqual(0, _asset.MsDePreRoll);

            _asset.MsDePreRoll = 5000;
            _asset.OnValidate();
            Assert.AreEqual(1000, _asset.MsDePreRoll);
        }

        [Test]
        public void MaxSegundosPorFrase_se_acota_entre_1_y_60()
        {
            _asset.MaxSegundosPorFrase = 0f;
            _asset.OnValidate();
            Assert.AreEqual(1f, _asset.MaxSegundosPorFrase);

            _asset.MaxSegundosPorFrase = 999f;
            _asset.OnValidate();
            Assert.AreEqual(60f, _asset.MaxSegundosPorFrase);
        }

        [Test]
        public void MsMaximosDeCierre_se_acota_entre_0_y_2000()
        {
            _asset.MsMaximosDeCierre = -10;
            _asset.OnValidate();
            Assert.AreEqual(0, _asset.MsMaximosDeCierre);

            _asset.MsMaximosDeCierre = 9000;
            _asset.OnValidate();
            Assert.AreEqual(2000, _asset.MsMaximosDeCierre);
        }

        [Test]
        public void ToSettings_copia_los_campos_del_asset_al_snapshot()
        {
            _asset.Estrategia             = TriggerStrategy.PulsarParaHablar;
            _asset.TasaDeMuestreo         = 22050;
            _asset.EtiquetaDeIdioma       = "es-CO";
            _asset.IdDeModelo             = "modelo-x";
            _asset.DispositivoDeMicrofono = "mic-1";
            _asset.UmbralDeEnergia        = 0.1f;
            _asset.MsMinimosDeVoz         = 150;
            _asset.MsDeSilencioParaCortar = 600;
            _asset.MsDePreRoll            = 250;
            _asset.MaxSegundosPorFrase    = 20f;
            _asset.MsMaximosDeCierre      = 300;

            var settings = _asset.ToSettings();

            Assert.AreEqual(TriggerStrategy.PulsarParaHablar, settings.Estrategia);
            Assert.AreEqual(22050, settings.TasaDeMuestreo);
            Assert.AreEqual("es-CO", settings.EtiquetaDeIdioma);
            Assert.AreEqual("modelo-x", settings.IdDeModelo);
            Assert.AreEqual("mic-1", settings.DispositivoDeMicrofono);
            Assert.AreEqual(0.1f, settings.UmbralDeEnergia);
            Assert.AreEqual(150, settings.MsMinimosDeVoz);
            Assert.AreEqual(600, settings.MsDeSilencioParaCortar);
            Assert.AreEqual(250, settings.MsDePreRoll);
            Assert.AreEqual(20f, settings.MaxSegundosPorFrase);
            Assert.AreEqual(300, settings.MsMaximosDeCierre);
        }

        [Test]
        public void No_hay_campo_de_confianza_minima_para_emitir()
        {
            // Requisito "Configuracion de STT como dato en Data/Speech/": el campo de
            // confianza minima fue eliminado deliberadamente (Decision 6 de design.md).
            var tipo = typeof(SpeechSettingsAsset);
            Assert.IsNull(tipo.GetField("ConfianzaMinima"));
            Assert.IsNull(tipo.GetField("ConfianzaMinimaParaEmitir"));
        }
    }
}
