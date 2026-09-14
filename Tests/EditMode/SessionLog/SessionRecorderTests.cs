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

            recorder.IniciarSesion("sesion-a");

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
            recorder.IniciarSesion("sesion-a");

            recorder.RegistrarUtterance(new Utterance("   ", 0f, 0f));

            Assert.AreEqual(0, recorder.ObtenerTurnos().Count);
        }

        [Test]
        public void RegistrarRespuesta_vacia_no_genera_turno()
        {
            var recorder = CreateSubject(out _);
            recorder.IniciarSesion("sesion-a");

            recorder.RegistrarRespuesta(new NpcReply("", "", ""));

            Assert.AreEqual(0, recorder.ObtenerTurnos().Count);
        }

        [Test]
        public void Tras_FinalizarSesion_registrar_es_no_op()
        {
            var recorder = CreateSubject(out _);
            recorder.IniciarSesion("sesion-a");
            recorder.FinalizarSesion();

            recorder.RegistrarUtterance(new Utterance("hola", 1f, 1f));

            Assert.AreEqual(0, recorder.ObtenerTurnos().Count);
            Assert.IsFalse(recorder.SesionActiva);
        }

        [Test]
        public void La_secuencia_crece_en_el_orden_de_llegada()
        {
            var recorder = CreateSubject(out _);
            recorder.IniciarSesion("sesion-a");

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
            recorder.IniciarSesion("sesion-a");

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
            recorder.IniciarSesion("sesion-a");

            recorder.RegistrarRespuesta(new NpcReply("digame", "molesto", "cruzar_brazos"));

            var turno = recorder.ObtenerTurnos()[0];
            Assert.AreEqual(Hablante.Npc, turno.Hablante);
            Assert.AreEqual("molesto", turno.EmotionTag);
            Assert.AreEqual("cruzar_brazos", turno.AnimationCue);
        }

        [Test]
        public void IniciarSesion_de_nuevo_con_otra_etiqueta_reinicia_la_numeracion()
        {
            var recorder = CreateSubject(out _);
            recorder.IniciarSesion("sesion-a");
            recorder.RegistrarUtterance(new Utterance("hola", 1f, 1f));
            recorder.FinalizarSesion();

            recorder.IniciarSesion("sesion-b");
            recorder.RegistrarUtterance(new Utterance("de nuevo", 1f, 1f));

            var turnos = recorder.ObtenerTurnos();
            Assert.AreEqual(1, turnos.Count);
            Assert.AreEqual(0, turnos[0].Secuencia);
        }

        [Test]
        public void ListarSesiones_refleja_las_sesiones_que_paso_por_el_recorder()
        {
            var recorder = CreateSubject(out _);
            recorder.IniciarSesion("sesion-a");
            recorder.IniciarSesion("sesion-b");

            var sesiones = recorder.ListarSesiones();

            Assert.AreEqual(2, sesiones.Count);
        }

        [Test]
        public void ObtenerTurnosDeSesion_trae_una_sesion_pasada_sin_afectar_la_activa()
        {
            var recorder = CreateSubject(out _);
            recorder.IniciarSesion("sesion-a");
            recorder.RegistrarUtterance(new Utterance("de la sesion a", 1f, 1f));
            recorder.FinalizarSesion();

            recorder.IniciarSesion("sesion-b");

            var turnosDeA = recorder.ObtenerTurnosDeSesion("sesion-a");
            Assert.AreEqual(1, turnosDeA.Count);
            Assert.AreEqual("de la sesion a", turnosDeA[0].Texto);
            Assert.AreEqual(0, recorder.ObtenerTurnos().Count);
        }

        [Test]
        public void IniciarSesion_con_etiqueta_ya_usada_continua_la_numeracion_en_vez_de_reiniciar()
        {
            var recorder = CreateSubject(out _);
            recorder.IniciarSesion("sesion-a");
            recorder.RegistrarUtterance(new Utterance("primero", 1f, 1f));
            recorder.RegistrarUtterance(new Utterance("segundo", 1f, 1f));
            recorder.FinalizarSesion();

            // Simula reabrir tras un cierre no limpio: se retoma la misma etiqueta.
            recorder.IniciarSesion("sesion-a");
            recorder.RegistrarUtterance(new Utterance("tercero", 1f, 1f));

            var turnos = recorder.ObtenerTurnosDeSesion("sesion-a");
            Assert.AreEqual(3, turnos.Count);
            Assert.AreEqual(0, turnos[0].Secuencia);
            Assert.AreEqual(1, turnos[1].Secuencia);
            Assert.AreEqual(2, turnos[2].Secuencia, "la secuencia debe continuar, no reiniciar en 0");
        }

        [Test]
        public void ReanudarUltimaSesion_retoma_la_etiqueta_mas_reciente_y_permite_seguir_registrando()
        {
            var recorder = CreateSubject(out var store);
            recorder.IniciarSesion("sesion-vieja");
            recorder.RegistrarUtterance(new Utterance("antes del cierre", 1f, 1f));
            // Simula un cierre por error: nunca se llamo FinalizarSesion ni se creo un nuevo
            // SessionRecorder "en frio" sobre el mismo store, como pasaria tras reabrir la app.
            var recorderReabierto = new SessionRecorder(store);

            var retomo = recorderReabierto.ReanudarUltimaSesion();

            Assert.IsTrue(retomo);
            Assert.IsTrue(recorderReabierto.SesionActiva);
            recorderReabierto.RegistrarUtterance(new Utterance("despues del cierre", 1f, 1f));

            var turnos = recorderReabierto.ObtenerTurnosDeSesion("sesion-vieja");
            Assert.AreEqual(2, turnos.Count);
            Assert.AreEqual("antes del cierre", turnos[0].Texto);
            Assert.AreEqual("despues del cierre", turnos[1].Texto);
        }

        [Test]
        public void ReanudarUltimaSesion_no_retoma_una_sesion_que_se_cerro_limpio()
        {
            var recorder = CreateSubject(out var store);
            recorder.IniciarSesion("sesion-cerrada-bien");
            recorder.RegistrarUtterance(new Utterance("hola", 1f, 1f));
            recorder.FinalizarSesion(); // cierre limpio: no deberia retomarse sola despues

            var recorderReabierto = new SessionRecorder(store);
            var retomo = recorderReabierto.ReanudarUltimaSesion();

            Assert.IsFalse(retomo);
            Assert.IsFalse(recorderReabierto.SesionActiva);
        }

        [Test]
        public void ReanudarUltimaSesion_sin_sesiones_previas_no_activa_nada()
        {
            var recorder = CreateSubject(out _);

            var retomo = recorder.ReanudarUltimaSesion();

            Assert.IsFalse(retomo);
            Assert.IsFalse(recorder.SesionActiva);
        }
    }
}
