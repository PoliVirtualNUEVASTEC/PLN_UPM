using System.Collections.Generic;
using NpcAi.Core;
using NpcAi.Core.Tests;
using NUnit.Framework;

namespace NpcAi.Receptivity.Tests
{
    /// <summary>
    /// Paso 4 de M4. Hereda las pruebas de <see cref="ReceptivityEngineContract"/>
    /// contra el motor real y agrega la tabla de transiciones por personalidad:
    /// cada perfil de Standard() se comporta distinto y el motor toma el catalogo
    /// que se le inyecta.
    /// </summary>
    public class ReceptivityEngineTests : ReceptivityEngineContract
    {
        protected override IReceptivityEngine CreateSubject() => new ReceptivityEngine();

        private static IntentResult Intencion(Intent intent, Tone tone = Tone.Neutral)
            => new IntentResult(intent, tone, 1f, 0f);

        private static ReceptivityEngine ConPersonalidad(string id)
        {
            var motor = new ReceptivityEngine();
            motor.Reset(new PersonalityId(id));
            return motor;
        }

        // --- Estado inicial por personalidad ---

        [Test]
        public void Grosero_arranca_no_receptivo_y_empatico_arranca_neutral()
        {
            Assert.AreEqual(Core.Receptivity.NoReceptivo, ConPersonalidad("grosero").Current);
            Assert.AreEqual(Core.Receptivity.Neutral, ConPersonalidad("empatico").Current);
        }

        // --- La misma entrada mueve distinto a cada personalidad ---

        [Test]
        public void Empatico_cede_con_un_gesto_el_introvertido_necesita_dos()
        {
            var empatico = ConPersonalidad("empatico");
            var introvertido = ConPersonalidad("introvertido");
            var gesto = Intencion(Intent.Empatia, Tone.Empatico);

            empatico.Evaluate(gesto, PhysicalAction.ContactoVisual);
            introvertido.Evaluate(gesto, PhysicalAction.ContactoVisual);

            Assert.AreEqual(Core.Receptivity.Receptivo, empatico.Current, "el empatico cede con un gesto");
            Assert.AreEqual(Core.Receptivity.Neutral, introvertido.Current, "el introvertido todavia no");

            introvertido.Evaluate(gesto, PhysicalAction.ContactoVisual);
            Assert.AreEqual(Core.Receptivity.Receptivo, introvertido.Current, "con el segundo gesto si");
        }

        [Test]
        public void Grosero_castiga_la_agresion_mas_que_el_perfil_neutro()
        {
            var grosero = ConPersonalidad("grosero");
            var neutro = ConPersonalidad("id-que-no-existe-y-cae-en-default");
            var agresion = Intencion(Intent.SolicitudAgresiva, Tone.Agresivo);

            var golpeGrosero = grosero.Evaluate(agresion, PhysicalAction.Ninguna);
            var golpeNeutro = neutro.Evaluate(agresion, PhysicalAction.Ninguna);

            Assert.Less(golpeGrosero.Score, golpeNeutro.Score,
                "la misma agresion pega mas fuerte en el perfil grosero");
        }

        // --- La accion fisica (M7) es decisiva para el histerico ---

        [Test]
        public void Histerico_se_gana_con_un_gesto_de_calma()
        {
            var histerico = ConPersonalidad("histerico");

            var cambio = histerico.Evaluate(IntentResult.Unknown(), PhysicalAction.GestoCalma);

            Assert.AreEqual(Core.Receptivity.Receptivo, histerico.Current);
            Assert.IsTrue(cambio.Improved);
            Assert.AreEqual("GESTO_CALMA", cambio.ReasonCode);
        }

        [Test]
        public void Histerico_se_cierra_si_le_invaden_el_espacio()
        {
            var histerico = ConPersonalidad("histerico");

            var cambio = histerico.Evaluate(IntentResult.Unknown(), PhysicalAction.Acercarse);

            Assert.AreEqual(Core.Receptivity.NoReceptivo, histerico.Current);
            Assert.IsTrue(cambio.Worsened);
            Assert.AreEqual("ACERCAMIENTO", cambio.ReasonCode);
        }

        // --- El tono no se ignora (a diferencia del doble) ---

        [Test]
        public void El_tono_cambia_el_resultado_de_la_misma_intencion()
        {
            var conRespeto = ConPersonalidad("grosero");
            var conAgresion = ConPersonalidad("grosero");
            var peticion = Intent.SolicitudRespetuosa;

            var amable = conRespeto.Evaluate(Intencion(peticion, Tone.Respetuoso), PhysicalAction.Ninguna);
            var cortante = conAgresion.Evaluate(Intencion(peticion, Tone.Agresivo), PhysicalAction.Ninguna);

            Assert.Greater(amable.Score, cortante.Score,
                "la misma peticion dicha con respeto deja mejor puntaje que dicha con agresion");
        }

        // --- Saturacion ---

        [Test]
        public void El_puntaje_se_satura_en_el_limite_del_perfil()
        {
            var empatico = ConPersonalidad("empatico"); // LimitePuntaje = 4

            ReceptivityChange ultimo = default;
            for (var i = 0; i < 12; i++)
                ultimo = empatico.Evaluate(Intencion(Intent.Empatia, Tone.Empatico), PhysicalAction.GestoCalma);

            Assert.LessOrEqual(ultimo.Score, 4);
            Assert.GreaterOrEqual(ultimo.Score, -4);
        }

        // --- Id desconocido -> perfil neutro, sin lanzar ---

        [Test]
        public void Un_id_desconocido_cae_en_el_perfil_neutro()
        {
            var motor = new ReceptivityEngine();

            Assert.DoesNotThrow(() => motor.Reset(new PersonalityId("no-existe")));
            Assert.AreEqual(Core.Receptivity.Neutral, motor.Current);

            var cambio = motor.Evaluate(Intencion(Intent.Empatia, Tone.Empatico), PhysicalAction.ContactoVisual);

            Assert.AreEqual(Core.Receptivity.Receptivo, motor.Current);
            Assert.IsTrue(cambio.Improved);
        }

        // --- La costura para M5: el catalogo se inyecta ---

        [Test]
        public void El_motor_toma_el_catalogo_que_se_le_inyecta()
        {
            var catalogo = new ReceptivityProfileCatalog(new Dictionary<PersonalityId, ReceptivityProfile>
            {
                [new PersonalityId("robot")] = new ReceptivityProfile(
                    umbralReceptivo: 1,
                    umbralNoReceptivo: -1,
                    limitePuntaje: 10,
                    puntajeInicial: 0,
                    porIntencion: new Dictionary<Intent, int> { [Intent.AportaInformacion] = 5 },
                    porTono: null,
                    porAccion: null),
            });

            var motor = new ReceptivityEngine(catalogo);
            motor.Reset(new PersonalityId("robot"));
            Assert.AreEqual(Core.Receptivity.Neutral, motor.Current);

            motor.Evaluate(Intencion(Intent.AportaInformacion), PhysicalAction.Ninguna);

            Assert.AreEqual(Core.Receptivity.Receptivo, motor.Current,
                "manda el umbral +1 del perfil inyectado, no el de Standard()");
        }

        // --- Vocabulario de razones alineado con el doble ---

        [Test]
        public void La_agresion_directa_deja_su_codigo_de_razon()
        {
            var motor = ConPersonalidad("grosero");

            var cambio = motor.Evaluate(Intencion(Intent.SolicitudAgresiva, Tone.Agresivo), PhysicalAction.Ninguna);

            Assert.AreEqual("AGRESION_DIRECTA", cambio.ReasonCode);
        }
    }
}
