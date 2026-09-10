using System;
using System.IO;
using NUnit.Framework;
using NpcAi.SessionLog.Sqlite;

namespace NpcAi.SessionLog.Tests
{
    /// <summary>
    /// Hereda la misma bateria de <see cref="SessionStoreContract"/> que
    /// <c>InMemorySessionStoreTests</c>, mas la garantia propia de un store en disco: un turno
    /// sobrevive a que la sesion no se cierre limpiamente.
    /// </summary>
    public sealed class SqliteSessionStoreTests : SessionStoreContract
    {
        private string _rutaArchivo;
        private SqliteSessionStore _subject;

        [SetUp]
        public void SetUp()
        {
            _rutaArchivo = Path.Combine(Path.GetTempPath(), $"session-log-tests-{Guid.NewGuid():N}.db");
        }

        [TearDown]
        public void TearDown()
        {
            _subject?.Dispose();
            if (File.Exists(_rutaArchivo))
            {
                File.Delete(_rutaArchivo);
            }
        }

        protected override ISessionStore CreateSubject()
        {
            _subject?.Dispose();
            _subject = new SqliteSessionStore(_rutaArchivo);
            return _subject;
        }

        [Test]
        public void Un_turno_sobrevive_a_reabrir_el_archivo_sin_FinalizarSesion()
        {
            var primeraApertura = new SqliteSessionStore(_rutaArchivo);
            primeraApertura.IniciarSesion();
            primeraApertura.RegistrarTurno(new SessionTurn(0, Hablante.Usuario, "hola", string.Empty, string.Empty));
            primeraApertura.Dispose(); // simula el cierre abrupto: nunca se llamo FinalizarSesion

            using var segundaApertura = new SqliteSessionStore(_rutaArchivo);
            var turnos = segundaApertura.ObtenerTurnos();

            Assert.AreEqual(1, turnos.Count);
            Assert.AreEqual("hola", turnos[0].Texto);
        }
    }
}
