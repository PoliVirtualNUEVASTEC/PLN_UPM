using System.Text;

namespace NpcAi.SessionLog
{
    /// <summary>
    /// Exportacion a texto plano de una sesion ya persistida. Metodo puro sobre
    /// <see cref="ISessionStore.ObtenerTurnos"/>: no escribe nada que no viniera ya del store, y
    /// funciona igual sobre cualquier adaptador (memoria o SQLite).
    /// </summary>
    public static class SessionExport
    {
        public static string ExportarTextoPlano(ISessionStore store)
        {
            var turnos = store.ObtenerTurnos();
            var texto = new StringBuilder();

            foreach (var turno in turnos)
            {
                texto.Append(turno.Hablante).Append(": ").Append(turno.Texto).Append('\n');
            }

            return texto.ToString();
        }
    }
}
