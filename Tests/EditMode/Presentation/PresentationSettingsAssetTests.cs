using NpcAi.Presentation.Config;
using NUnit.Framework;
using UnityEngine;

namespace NpcAi.Presentation.Tests
{
    /// <summary>
    /// <see cref="PresentationSettingsAsset.OnValidate"/> acota los campos numéricos y
    /// <see cref="PresentationSettingsAsset.ToSettings"/> copia el asset a un snapshot POCO,
    /// armando los mapas de voces y de cues.
    /// </summary>
    public class PresentationSettingsAssetTests
    {
        private PresentationSettingsAsset _asset;

        [SetUp]
        public void Crear() => _asset = ScriptableObject.CreateInstance<PresentationSettingsAsset>();

        [TearDown]
        public void Destruir() => Object.DestroyImmediate(_asset);

        [Test]
        public void TasaDeMuestreo_se_acota_entre_8000_y_48000()
        {
            _asset.TasaDeMuestreo = 999999;
            _asset.OnValidate();
            Assert.AreEqual(48000, _asset.TasaDeMuestreo);

            _asset.TasaDeMuestreo = 1;
            _asset.OnValidate();
            Assert.AreEqual(8000, _asset.TasaDeMuestreo);
        }

        [Test]
        public void Velocidad_se_acota_entre_0_5_y_2()
        {
            _asset.Velocidad = 9f;
            _asset.OnValidate();
            Assert.AreEqual(2f, _asset.Velocidad);

            _asset.Velocidad = 0.1f;
            _asset.OnValidate();
            Assert.AreEqual(0.5f, _asset.Velocidad);
        }

        [Test]
        public void ToSettings_copia_escalares_y_arma_los_mapas()
        {
            _asset.TasaDeMuestreo = 22050;
            _asset.Velocidad = 1.2f;
            _asset.VozPorDefecto = "es-default";
            _asset.Voces = new[]
            {
                new EntradaDeVoz { vozId = "mayor", idDeVoz = "es_ES-mayor-medium" },
                new EntradaDeVoz { vozId = "joven", idDeVoz = "es_MX-joven-medium" },
            };
            _asset.Cues = new[]
            {
                new EntradaDeCue { cue = "molesto", parametro = "Enojo",   tipo = TipoDeParametroDeAnimacion.Bool },
                new EntradaDeCue { cue = "asentir", parametro = "Asentir", tipo = TipoDeParametroDeAnimacion.Trigger },
            };

            var s = _asset.ToSettings();

            Assert.AreEqual(22050, s.TasaDeMuestreo);
            Assert.AreEqual(1.2f, s.Velocidad);
            Assert.AreEqual("es_ES-mayor-medium", s.ResolverIdDeVoz("mayor"));
            Assert.AreEqual("es_MX-joven-medium", s.ResolverIdDeVoz("joven"));
            Assert.AreEqual("es-default", s.ResolverIdDeVoz("desconocido"));
            Assert.AreEqual("es-default", s.ResolverIdDeVoz(null));

            Assert.IsTrue(s.Cues.ContainsKey("molesto"));
            Assert.AreEqual("Asentir", s.Cues["asentir"].Nombre);
            Assert.AreEqual(TipoDeParametroDeAnimacion.Trigger, s.Cues["asentir"].Tipo);
        }

        [Test]
        public void ToSettings_ignora_entradas_incompletas()
        {
            _asset.Voces = new[] { new EntradaDeVoz { vozId = "", idDeVoz = "x" } };
            _asset.Cues = new[]
            {
                new EntradaDeCue { cue = "molesto", parametro = "", tipo = TipoDeParametroDeAnimacion.Bool },
            };

            var s = _asset.ToSettings();

            Assert.IsFalse(s.Cues.ContainsKey("molesto"));
            Assert.AreEqual("", s.ResolverIdDeVoz("")); // vacío -> VozPorDefecto, que por defecto es ""
        }
    }
}
