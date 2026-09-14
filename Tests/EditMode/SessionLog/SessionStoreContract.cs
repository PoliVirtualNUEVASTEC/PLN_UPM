using System.Threading;
using NUnit.Framework;

namespace NpcAi.SessionLog.Tests
{
    /// <summary>
    /// Comportamiento compartido de <see cref="ISessionStore"/>. Lo heredan
    /// <c>InMemorySessionStore</c> (doble) y <c>SqliteSessionStore</c> (real) para
    /// garantizar paridad entre adaptadores, igual que <c>RazonParityTests</c> hace para M4.
    /// </summary>
    public abstract class SessionStoreContract
    {
        protected abstract ISessionStore CreateSubject();

        private static SessionTurn Turno(int secuencia, Hablante hablante, string texto) =>
            new SessionTurn(secuencia, hablante, texto, string.Empty, string.Empty);

        [Test]
        public void Sin_sesion_activa_no_hay_turnos()
        {
            var store = CreateSubject();

            Assert.AreEqual(0, store.ObtenerTurnos().Count);
        }

        [Test]
        public void RegistrarTurno_tras_IniciarSesion_acumula_en_orden()
        {
            var store = CreateSubject();
            store.IniciarSesion("sesion-a");

            store.RegistrarTurno(Turno(0, Hablante.Usuario, "hola"));
            store.RegistrarTurno(Turno(1, Hablante.Npc, "digame en que le ayudo"));

            var turnos = store.ObtenerTurnos();
            Assert.AreEqual(2, turnos.Count);
            Assert.AreEqual("hola", turnos[0].Texto);
            Assert.AreEqual(Hablante.Usuario, turnos[0].Hablante);
            Assert.AreEqual("digame en que le ayudo", turnos[1].Texto);
            Assert.AreEqual(Hablante.Npc, turnos[1].Hablante);
        }

        [Test]
        public void FinalizarSesion_no_borra_los_turnos_ya_registrados()
        {
            var store = CreateSubject();
            store.IniciarSesion("sesion-a");
            store.RegistrarTurno(Turno(0, Hablante.Usuario, "hola"));

            store.FinalizarSesion();

            Assert.AreEqual(1, store.ObtenerTurnos().Count);
        }

        [Test]
        public void Sesiones_distintas_no_mezclan_turnos()
        {
            var store = CreateSubject();
            store.IniciarSesion("sesion-a");
            store.RegistrarTurno(Turno(0, Hablante.Usuario, "de la sesion a"));

            store.IniciarSesion("sesion-b");
            store.RegistrarTurno(Turno(0, Hablante.Usuario, "de la sesion b"));

            Assert.AreEqual(1, store.ObtenerTurnos().Count,
                "ObtenerTurnos() sin argumento debe reflejar solo la sesion activa");
            Assert.AreEqual("de la sesion b", store.ObtenerTurnos()[0].Texto);

            var turnosDeA = store.ObtenerTurnosDeSesion("sesion-a");
            Assert.AreEqual(1, turnosDeA.Count);
            Assert.AreEqual("de la sesion a", turnosDeA[0].Texto);
        }

        [Test]
        public void ObtenerTurnosDeSesion_no_se_ve_afectado_por_cambiar_de_sesion_activa()
        {
            var store = CreateSubject();
            store.IniciarSesion("sesion-a");
            store.RegistrarTurno(Turno(0, Hablante.Usuario, "hola"));
            store.FinalizarSesion();

            store.IniciarSesion("sesion-b");
            store.RegistrarTurno(Turno(0, Hablante.Usuario, "otra cosa"));

            var turnosDeA = store.ObtenerTurnosDeSesion("sesion-a");
            Assert.AreEqual(1, turnosDeA.Count);
            Assert.AreEqual("hola", turnosDeA[0].Texto);
        }

        [Test]
        public void Reusar_la_misma_etiqueta_acumula_turnos_en_vez_de_borrar()
        {
            var store = CreateSubject();
            store.IniciarSesion("sesion-a");
            store.RegistrarTurno(Turno(0, Hablante.Usuario, "primero"));
            store.FinalizarSesion();

            store.IniciarSesion("sesion-a");
            store.RegistrarTurno(Turno(1, Hablante.Usuario, "segundo"));

            var turnos = store.ObtenerTurnosDeSesion("sesion-a");
            Assert.AreEqual(2, turnos.Count);
            Assert.AreEqual("primero", turnos[0].Texto);
            Assert.AreEqual("segundo", turnos[1].Texto);
        }

        [Test]
        public void ListarSesiones_incluye_todas_las_iniciadas_mas_reciente_primero()
        {
            var store = CreateSubject();
            store.IniciarSesion("primera");
            Thread.Sleep(20); // margen para que el timestamp de la segunda sea estrictamente mayor
            store.IniciarSesion("segunda");

            var sesiones = store.ListarSesiones();

            Assert.AreEqual(2, sesiones.Count);
            Assert.AreEqual("segunda", sesiones[0].Etiqueta);
            Assert.AreEqual("primera", sesiones[1].Etiqueta);
        }

        [Test]
        public void Etiqueta_null_o_vacia_usa_el_valor_por_defecto()
        {
            var store = CreateSubject();
            store.IniciarSesion("   ");
            store.RegistrarTurno(Turno(0, Hablante.Usuario, "hola"));

            var sesiones = store.ListarSesiones();
            Assert.AreEqual(1, sesiones.Count);
            Assert.AreEqual("sesion-sin-etiqueta", sesiones[0].Etiqueta);
        }

        [Test]
        public void Una_sesion_nueva_no_esta_cerrada()
        {
            var store = CreateSubject();
            store.IniciarSesion("sesion-a");

            Assert.IsFalse(store.ListarSesiones()[0].Cerrada);
        }

        [Test]
        public void FinalizarSesion_marca_la_sesion_activa_como_cerrada()
        {
            var store = CreateSubject();
            store.IniciarSesion("sesion-a");

            store.FinalizarSesion();

            Assert.IsTrue(store.ListarSesiones()[0].Cerrada);
        }

        [Test]
        public void IniciarSesion_de_nuevo_sobre_una_etiqueta_cerrada_la_reactiva()
        {
            var store = CreateSubject();
            store.IniciarSesion("sesion-a");
            store.FinalizarSesion();

            store.IniciarSesion("sesion-a");

            Assert.IsFalse(store.ListarSesiones()[0].Cerrada);
        }
    }
}
