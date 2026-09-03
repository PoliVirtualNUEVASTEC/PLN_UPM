using System.Collections.Generic;
using System.IO;
using System.Linq;
using NpcAi.Core;
using NpcAi.Receptivity.Unity;
using NUnit.Framework;
using UnityEditor;

namespace NpcAi.Receptivity.Tests
{
    /// <summary>
    /// M5: los 4 .asset reales de <c>Data/Personalities/</c> cargan desde disco,
    /// arman un catalogo consultable y respetan el contrato de receptividad de M4.
    /// Fija los DATOS de M5, no los numeros placeholder de
    /// <see cref="ReceptivityProfileCatalog.Standard"/>: si un .asset se guarda mal,
    /// se renombra sin corregir el id, o rompe un signo del contrato, estas pruebas
    /// caen sin tocar una sola clase.
    /// </summary>
    public class PersonalityProfilesDataTests
    {
        private static readonly string[] IdsEsperados =
            { "grosero", "histerico", "introvertido", "empatico" };

        /// <summary>Fragmento de ruta que identifica los perfiles de M5, sea package o carpeta embebida.</summary>
        private const string CarpetaPerfiles = "Data/Personalities/";

        private static List<ReceptivityProfileAsset> CargarPerfilesDeDisco()
        {
            var assets = AssetDatabase
                .FindAssets("t:" + nameof(ReceptivityProfileAsset))
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => path.Replace('\\', '/').Contains(CarpetaPerfiles))
                .Select(AssetDatabase.LoadAssetAtPath<ReceptivityProfileAsset>)
                .Where(asset => asset != null)
                .ToList();

            Assert.IsNotEmpty(assets,
                "No se encontro ningun ReceptivityProfileAsset bajo " + CarpetaPerfiles +
                ". Estas pruebas validan los .asset reales de M5; sin ellos no hay nada que fijar.");

            return assets;
        }

        [Test]
        public void Data_Personalities_trae_exactamente_los_cuatro_perfiles_de_M5()
        {
            var ids = CargarPerfilesDeDisco().Select(a => a.PersonalityId.Value).ToList();

            CollectionAssert.AreEquivalent(IdsEsperados, ids,
                "Data/Personalities/ debe tener los 4 perfiles del alcance de M5, ni mas ni menos");
        }

        [Test]
        public void Cada_archivo_se_llama_igual_que_su_personalityId()
        {
            foreach (var asset in CargarPerfilesDeDisco())
            {
                var nombreArchivo = Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(asset));

                Assert.AreEqual(nombreArchivo, asset.PersonalityId.Value,
                    "el nombre del .asset y el personalityId deben coincidir: es la clave del catalogo");
            }
        }

        [Test]
        public void BuildCatalog_resuelve_los_cuatro_ids_con_perfil_propio()
        {
            var catalogo = ReceptivityProfileAsset.BuildCatalog(CargarPerfilesDeDisco());

            CollectionAssert.AreEquivalent(
                IdsEsperados,
                catalogo.Personalidades.Select(id => id.Value).ToList());

            foreach (var id in IdsEsperados)
                Assert.AreNotSame(ReceptivityProfile.Default,
                    catalogo.PerfilDe(new PersonalityId(id)),
                    id + " debe traer su propio perfil, no el Default");
        }

        [Test]
        public void Un_id_fuera_del_catalogo_cae_en_Default()
        {
            var catalogo = ReceptivityProfileAsset.BuildCatalog(CargarPerfilesDeDisco());

            Assert.AreSame(ReceptivityProfile.Default,
                catalogo.PerfilDe(new PersonalityId("no-existe")));
            Assert.AreSame(ReceptivityProfile.Default,
                catalogo.PerfilDe(PersonalityId.None));
        }

        [Test]
        public void Cada_perfil_respeta_los_signos_del_contrato()
        {
            var catalogo = ReceptivityProfileAsset.BuildCatalog(CargarPerfilesDeDisco());

            foreach (var id in catalogo.Personalidades)
            {
                var perfil = catalogo.PerfilDe(id);

                Assert.LessOrEqual(perfil.DeltaPorIntencion(Intent.SolicitudAgresiva), 0,
                    id + ": SolicitudAgresiva nunca puede sumar puntaje");
                Assert.LessOrEqual(perfil.DeltaPorIntencion(Intent.Interrupcion), 0,
                    id + ": Interrupcion nunca puede sumar puntaje");
                Assert.LessOrEqual(perfil.DeltaPorTono(Tone.Agresivo), 0,
                    id + ": el tono Agresivo nunca puede sumar puntaje");

                Assert.GreaterOrEqual(perfil.DeltaPorIntencion(Intent.SolicitudRespetuosa), 0,
                    id + ": SolicitudRespetuosa nunca puede restar puntaje");
                Assert.GreaterOrEqual(perfil.DeltaPorIntencion(Intent.Empatia), 0,
                    id + ": Empatia nunca puede restar puntaje");
                Assert.GreaterOrEqual(perfil.DeltaPorTono(Tone.Respetuoso), 0,
                    id + ": el tono Respetuoso nunca puede restar puntaje");
                Assert.GreaterOrEqual(perfil.DeltaPorTono(Tone.Empatico), 0,
                    id + ": el tono Empatico nunca puede restar puntaje");
                Assert.GreaterOrEqual(perfil.DeltaPorAccion(PhysicalAction.GestoCalma), 0,
                    id + ": GestoCalma nunca puede restar puntaje");
            }
        }

        [Test]
        public void Cada_perfil_tiene_umbrales_y_puntaje_inicial_coherentes()
        {
            var catalogo = ReceptivityProfileAsset.BuildCatalog(CargarPerfilesDeDisco());

            foreach (var id in catalogo.Personalidades)
            {
                var perfil = catalogo.PerfilDe(id);

                Assert.Less(perfil.UmbralNoReceptivo, perfil.UmbralReceptivo,
                    id + ": el umbral no-receptivo debe quedar por debajo del receptivo");
                Assert.Greater(perfil.LimitePuntaje, 0,
                    id + ": el limite de puntaje debe ser positivo");
                Assert.GreaterOrEqual(perfil.PuntajeInicial, -perfil.LimitePuntaje,
                    id + ": el puntaje inicial no puede caer por debajo de -LimitePuntaje");
                Assert.LessOrEqual(perfil.PuntajeInicial, perfil.LimitePuntaje,
                    id + ": el puntaje inicial no puede pasar +LimitePuntaje");
            }
        }

        [Test]
        public void Cada_perfil_de_disco_entra_al_motor_real_sin_lanzar()
        {
            var motor = new ReceptivityEngine(
                ReceptivityProfileAsset.BuildCatalog(CargarPerfilesDeDisco()));

            foreach (var id in IdsEsperados)
            {
                Assert.DoesNotThrow(() => motor.Reset(new PersonalityId(id)));

                var estado = motor.Current;
                Assert.IsTrue(
                    estado == Core.Receptivity.NoReceptivo
                    || estado == Core.Receptivity.Neutral
                    || estado == Core.Receptivity.Receptivo,
                    id + ": tras Reset el motor debe quedar en un estado definido");
            }
        }

        [Test]
        public void Los_extremos_hostil_y_cooperativo_no_arrancan_en_el_mismo_estado()
        {
            var motor = new ReceptivityEngine(
                ReceptivityProfileAsset.BuildCatalog(CargarPerfilesDeDisco()));

            motor.Reset(new PersonalityId("grosero"));
            var hostil = motor.Current;
            motor.Reset(new PersonalityId("empatico"));
            var cooperativo = motor.Current;

            Assert.AreNotEqual(hostil, cooperativo,
                "grosero (extremo hostil) y empatico (extremo cooperativo) deben partir de estados distintos");
            Assert.LessOrEqual((int)hostil, (int)cooperativo,
                "el hostil no puede arrancar mas receptivo que el cooperativo");
        }
    }
}
