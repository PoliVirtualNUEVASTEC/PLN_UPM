using NUnit.Framework;
using NpcAi.SessionLog.Fakes;

namespace NpcAi.SessionLog.Tests
{
    /// <summary>
    /// La exportacion es un metodo puro sobre <see cref="ISessionStore.ObtenerTurnos"/>: se
    /// prueba contra el doble en memoria, sin SQLite, porque el comportamiento no depende del
    /// adaptador (AD8).
    /// </summary>
    public sealed class SessionExportTests
    {
        [Test]
        public void Exporta_cada_turno_persistido_en_orden_uno_por_linea()
        {
            var store = new InMemorySessionStore();
            store.IniciarSesion();
            store.RegistrarTurno(new SessionTurn(0, Hablante.Usuario, "hola", string.Empty, string.Empty));
            store.RegistrarTurno(new SessionTurn(1, Hablante.Npc, "digame en que le ayudo", "Neutral", "Idle"));

            var texto = SessionExport.ExportarTextoPlano(store);

            Assert.AreEqual("Usuario: hola\nNpc: digame en que le ayudo\n", texto);
        }

        [Test]
        public void Sin_turnos_persistidos_exporta_vacio()
        {
            var store = new InMemorySessionStore();

            var texto = SessionExport.ExportarTextoPlano(store);

            Assert.AreEqual(string.Empty, texto);
        }
    }
}
