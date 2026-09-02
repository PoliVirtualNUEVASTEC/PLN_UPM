using System.Collections.Generic;
using System.Linq;
using NpcAi.Core;
using NUnit.Framework;

namespace NpcAi.Receptivity.Tests
{
    /// <summary>
    /// Paso 2 de M4: el catalogo resuelve un id a su perfil y cae en Default
    /// para todo lo que no reconoce. Es inyectable; Standard() es solo un reparto.
    /// </summary>
    public class ReceptivityProfileCatalogTests
    {
        [Test]
        public void Un_id_conocido_devuelve_un_perfil_propio()
        {
            var catalogo = ReceptivityProfileCatalog.Standard();

            Assert.AreNotSame(ReceptivityProfile.Default,
                catalogo.PerfilDe(new PersonalityId("grosero")));
        }

        [Test]
        public void Un_id_desconocido_cae_en_Default()
        {
            var catalogo = ReceptivityProfileCatalog.Standard();

            Assert.AreSame(ReceptivityProfile.Default,
                catalogo.PerfilDe(new PersonalityId("thanos")));
        }

        [Test]
        public void None_cae_en_Default()
        {
            var catalogo = ReceptivityProfileCatalog.Standard();

            Assert.AreSame(ReceptivityProfile.Default,
                catalogo.PerfilDe(PersonalityId.None));
        }

        [Test]
        public void El_id_se_normaliza_para_la_busqueda()
        {
            var catalogo = ReceptivityProfileCatalog.Standard();

            Assert.AreSame(
                catalogo.PerfilDe(new PersonalityId("grosero")),
                catalogo.PerfilDe(new PersonalityId("  GROSERO ")));
        }

        [Test]
        public void Standard_trae_exactamente_las_cuatro_personalidades_de_M5()
        {
            var ids = ReceptivityProfileCatalog.Standard().Personalidades.ToList();

            Assert.AreEqual(4, ids.Count);
            Assert.IsTrue(ids.Contains(new PersonalityId("grosero")));
            Assert.IsTrue(ids.Contains(new PersonalityId("histerico")));
            Assert.IsTrue(ids.Contains(new PersonalityId("introvertido")));
            Assert.IsTrue(ids.Contains(new PersonalityId("empatico")));
        }

        [Test]
        public void El_catalogo_acepta_un_diccionario_nulo()
        {
            var catalogo = new ReceptivityProfileCatalog(null);

            Assert.AreSame(ReceptivityProfile.Default,
                catalogo.PerfilDe(new PersonalityId("grosero")));
        }

        [Test]
        public void Un_catalogo_inyectado_devuelve_lo_que_se_le_dio()
        {
            var mio = new ReceptivityProfile(
                umbralReceptivo: 1,
                umbralNoReceptivo: -1,
                limitePuntaje: 3,
                puntajeInicial: 0,
                porIntencion: null,
                porTono: null,
                porAccion: null);

            var catalogo = new ReceptivityProfileCatalog(new Dictionary<PersonalityId, ReceptivityProfile>
            {
                [new PersonalityId("robotico")] = mio,
            });

            Assert.AreSame(mio, catalogo.PerfilDe(new PersonalityId("robotico")));
            Assert.AreSame(ReceptivityProfile.Default, catalogo.PerfilDe(new PersonalityId("grosero")));
        }

        [Test]
        public void Los_perfiles_de_Standard_respetan_los_signos()
        {
            var catalogo = ReceptivityProfileCatalog.Standard();

            foreach (var id in catalogo.Personalidades)
            {
                var perfil = catalogo.PerfilDe(id);

                Assert.LessOrEqual(perfil.DeltaPorIntencion(Intent.SolicitudAgresiva), 0,
                    $"{id}: la agresion no debe sumar");
                Assert.GreaterOrEqual(perfil.DeltaPorIntencion(Intent.Empatia), 0,
                    $"{id}: la empatia no debe restar");
            }
        }
    }
}
