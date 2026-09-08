using NUnit.Framework;

namespace NpcAi.SessionLog.Tests
{
    /// <summary>
    /// Comportamiento compartido de <see cref="ISessionStore"/>. Lo heredan
    /// <c>InMemorySessionStore</c> (doble) y <c>SqliteSessionStore</c> (real, PR2) para
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
            store.IniciarSesion();

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
            store.IniciarSesion();
            store.RegistrarTurno(Turno(0, Hablante.Usuario, "hola"));

            store.FinalizarSesion();

            Assert.AreEqual(1, store.ObtenerTurnos().Count);
        }

        [Test]
        public void Una_segunda_IniciarSesion_reinicia_limpio()
        {
            var store = CreateSubject();
            store.IniciarSesion();
            store.RegistrarTurno(Turno(0, Hablante.Usuario, "sesion vieja"));
            store.FinalizarSesion();

            store.IniciarSesion();

            Assert.AreEqual(0, store.ObtenerTurnos().Count,
                "Una sesion nueva no debe arrastrar turnos de una sesion anterior");
        }
    }
}
