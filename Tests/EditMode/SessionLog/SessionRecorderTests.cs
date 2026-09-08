using NpcAi.Core;
using NpcAi.SessionLog.Fakes;
using NUnit.Framework;

namespace NpcAi.SessionLog.Tests
{
    public class SessionRecorderTests
    {
        private static SessionRecorder CreateSubject(out InMemorySessionStore store)
        {
            store = new InMemorySessionStore();
            return new SessionRecorder(store);
        }

        [Test]
        public void Arranca_sin_sesion_activa()
        {
            var recorder = CreateSubject(out _);

            Assert.IsFalse(recorder.SesionActiva);
        }

        [Test]
        public void IniciarSesion_deja_la_sesion_activa()
        {
            var recorder = CreateSubject(out _);

            recorder.IniciarSesion();

            Assert.IsTrue(recorder.SesionActiva);
        }

        [Test]
        public void RegistrarUtterance_fuera_de_sesion_activa_es_no_op()
        {
            var recorder = CreateSubject(out _);

            recorder.RegistrarUtterance(new Utterance("hola", 1f, 1f));

            Assert.AreEqual(0, recorder.ObtenerTurnos().Count);
        }

        [Test]
        public void RegistrarRespuesta_fuera_de_sesion_activa_es_no_op()
        {
            var recorder = CreateSubject(out _);

            recorder.RegistrarRespuesta(new NpcReply("claro", "receptivo", "asentir"));

            Assert.AreEqual(0, recorder.ObtenerTurnos().Count);
        }

        [Test]
        public void RegistrarUtterance_vacia_no_genera_turno()
        {
            var recorder = CreateSubject(out _);
            recorder.IniciarSesion();

            recorder.RegistrarUtterance(new Utterance("   ", 0f, 0f));

            Assert.AreEqual(0, recorder.ObtenerTurnos().Count);
        }

        [Test]
        public void RegistrarRespuesta_vacia_no_genera_turno()
        {
            var recorder = CreateSubject(out _);
            recorder.IniciarSesion();

            recorder.RegistrarRespuesta(new NpcReply("", "", ""));

            Assert.AreEqual(0, recorder.ObtenerTurnos().Count);
        }

        [Test]
        public void Tras_FinalizarSesion_registrar_es_no_op()
        {
            var recorder = CreateSubject(out _);
            recorder.IniciarSesion();
            recorder.FinalizarSesion();

            recorder.RegistrarUtterance(new Utterance("hola", 1f, 1f));

            Assert.AreEqual(0, recorder.ObtenerTurnos().Count);
            Assert.IsFalse(recorder.SesionActiva);
        }

        [Test]
        public void La_secuencia_crece_en_el_orden_de_llegada()
        {
            var recorder = CreateSubject(out _);
            recorder.IniciarSesion();

            recorder.RegistrarUtterance(new Utterance("hola", 1f, 1f));
            recorder.RegistrarRespuesta(new NpcReply("digame", "neutral", "idle"));
            recorder.RegistrarUtterance(new Utterance("necesito ayuda", 1f, 1f));

            var turnos = recorder.ObtenerTurnos();
            Assert.AreEqual(3, turnos.Count);
            Assert.AreEqual(0, turnos[0].Secuencia);
            Assert.AreEqual(1, turnos[1].Secuencia);
            Assert.AreEqual(2, turnos[2].Secuencia);
        }

        [Test]
        public void Turno_de_usuario_no_lleva_emocion_ni_animacion()
        {
            var recorder = CreateSubject(out _);
            recorder.IniciarSesion();

            recorder.RegistrarUtterance(new Utterance("hola", 1f, 1f));

            var turno = recorder.ObtenerTurnos()[0];
            Assert.AreEqual(Hablante.Usuario, turno.Hablante);
            Assert.AreEqual(string.Empty, turno.EmotionTag);
            Assert.AreEqual(string.Empty, turno.AnimationCue);
        }

        [Test]
        public void Turno_de_npc_copia_emocion_y_animacion()
        {
            var recorder = CreateSubject(out _);
            recorder.IniciarSesion();

            recorder.RegistrarRespuesta(new NpcReply("digame", "molesto", "cruzar_brazos"));

            var turno = recorder.ObtenerTurnos()[0];
            Assert.AreEqual(Hablante.Npc, turno.Hablante);
            Assert.AreEqual("molesto", turno.EmotionTag);
            Assert.AreEqual("cruzar_brazos", turno.AnimationCue);
        }

        [Test]
        public void IniciarSesion_de_nuevo_reinicia_la_numeracion()
        {
            var recorder = CreateSubject(out _);
            recorder.IniciarSesion();
            recorder.RegistrarUtterance(new Utterance("hola", 1f, 1f));
            recorder.FinalizarSesion();

            recorder.IniciarSesion();
            recorder.RegistrarUtterance(new Utterance("de nuevo", 1f, 1f));

            var turnos = recorder.ObtenerTurnos();
            Assert.AreEqual(1, turnos.Count);
            Assert.AreEqual(0, turnos[0].Secuencia);
        }
    }
}
