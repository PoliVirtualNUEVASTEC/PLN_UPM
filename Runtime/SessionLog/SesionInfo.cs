using System;

namespace NpcAi.SessionLog
{
    /// <summary>
    /// Metadato de una sesion conocida por un <see cref="ISessionStore"/>: su etiqueta, cuando
    /// empezo, y si se cerro limpio (<see cref="ISessionStore.FinalizarSesion"/> se llego a
    /// llamar) o quedo interrumpida (la app se cerro sin cerrarla).
    /// </summary>
    public readonly struct SesionInfo
    {
        public readonly string   Etiqueta;
        public readonly DateTime IniciadaEn;
        public readonly bool     Cerrada;

        public SesionInfo(string etiqueta, DateTime iniciadaEn, bool cerrada)
        {
            Etiqueta   = etiqueta ?? string.Empty;
            IniciadaEn = iniciadaEn;
            Cerrada    = cerrada;
        }
    }
}
