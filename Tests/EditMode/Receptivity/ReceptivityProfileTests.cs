using NpcAi.Core;
using NUnit.Framework;

namespace NpcAi.Receptivity.Tests
{
    /// <summary>
    /// Paso 1 de M4: el perfil es un contenedor de datos. Estas pruebas fijan que
    /// el perfil Default es coherente y que los accesores nunca lanzan.
    /// </summary>
    public class ReceptivityProfileTests
    {
        [Test]
        public void El_perfil_Default_castiga_la_agresion_y_premia_la_empatia()
        {
            var perfil = ReceptivityProfile.Default;

            Assert.LessOrEqual(perfil.DeltaPorIntencion(Intent.SolicitudAgresiva), 0,
                "La agresion nunca debe sumar puntaje");
            Assert.GreaterOrEqual(perfil.DeltaPorIntencion(Intent.Empatia), 0,
                "La empatia nunca debe restar puntaje");
        }

        [Test]
        public void Las_entradas_sin_puntuar_devuelven_cero()
        {
            var perfil = ReceptivityProfile.Default;

            // El contrato del motor exige que una intencion desconocida y la
            // ausencia de accion no muevan el estado: eso empieza aqui, en 0.
            Assert.AreEqual(0, perfil.DeltaPorIntencion(Intent.Desconocida));
            Assert.AreEqual(0, perfil.DeltaPorTono(Tone.Neutral));
            Assert.AreEqual(0, perfil.DeltaPorAccion(PhysicalAction.Ninguna));
        }

        [Test]
        public void Los_accesores_toleran_diccionarios_nulos()
        {
            var perfil = new ReceptivityProfile(
                umbralReceptivo: 1,
                umbralNoReceptivo: -1,
                limitePuntaje: 3,
                puntajeInicial: 0,
                porIntencion: null,
                porTono: null,
                porAccion: null);

            Assert.AreEqual(0, perfil.DeltaPorIntencion(Intent.Empatia));
            Assert.AreEqual(0, perfil.DeltaPorTono(Tone.Agresivo));
            Assert.AreEqual(0, perfil.DeltaPorAccion(PhysicalAction.GestoCalma));
        }

        [Test]
        public void El_contenedor_guarda_los_umbrales_tal_cual()
        {
            // Igual que Utterance: el rango es contrato del productor (M5), no
            // invariante del contenedor. El ctor guarda lo que recibe.
            var perfil = new ReceptivityProfile(
                umbralReceptivo: 7,
                umbralNoReceptivo: -3,
                limitePuntaje: 9,
                puntajeInicial: -2,
                porIntencion: null,
                porTono: null,
                porAccion: null);

            Assert.AreEqual(7, perfil.UmbralReceptivo);
            Assert.AreEqual(-3, perfil.UmbralNoReceptivo);
            Assert.AreEqual(9, perfil.LimitePuntaje);
            Assert.AreEqual(-2, perfil.PuntajeInicial);
        }
    }
}
