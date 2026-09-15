using System.Collections.Generic;
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
        public static string ExportarTextoPlano(ISessionStore store) =>
            ExportarTextoPlano(store.ObtenerTurnos());

        /// <summary>Exporta una sesion especifica (activa o pasada), identificada por su etiqueta.</summary>
        public static string ExportarTextoPlano(ISessionStore store, string etiqueta) =>
            ExportarTextoPlano(store.ObtenerTurnosDeSesion(etiqueta));

        private static string ExportarTextoPlano(IReadOnlyList<SessionTurn> turnos)
        {
            var texto = new StringBuilder();

            foreach (var turno in turnos)
            {
                texto.Append(turno.Hablante).Append(": ").Append(turno.Texto).Append('\n');
            }

            return texto.ToString();
        }
    }
}
