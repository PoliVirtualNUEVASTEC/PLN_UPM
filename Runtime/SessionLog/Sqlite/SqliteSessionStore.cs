using System;
using System.Collections.Generic;
using System.Linq;
using SQLite;

namespace NpcAi.SessionLog.Sqlite
{
    /// <summary>
    /// Adaptador real de <see cref="ISessionStore"/> sobre un archivo SQLite local, via
    /// sqlite-net-pcl. Cada <see cref="RegistrarTurno"/> se confirma en disco de inmediato
    /// (sin transaccion abierta), para que un turno sobreviva aunque la app se cierre antes de
    /// llamar <see cref="FinalizarSesion"/>.
    /// </summary>
    public sealed class SqliteSessionStore : ISessionStore, IDisposable
    {
        private sealed class TurnoRow
        {
            [PrimaryKey, AutoIncrement]
            public int Id { get; set; }
            public int Secuencia { get; set; }
            public int Hablante { get; set; }
            public string Texto { get; set; } = string.Empty;
            public string EmotionTag { get; set; } = string.Empty;
            public string AnimationCue { get; set; } = string.Empty;
        }

        private readonly SQLiteConnection _conexion;

        /// <param name="rutaArchivo">
        /// Ruta absoluta al archivo .db. En Unity real es responsabilidad del llamador pasar una
        /// ruta bajo <c>Application.persistentDataPath</c>; en pruebas EditMode se inyecta una
        /// ruta temporal para no depender de la API de Unity.
        /// </param>
        public SqliteSessionStore(string rutaArchivo)
        {
            _conexion = new SQLiteConnection(rutaArchivo);
            _conexion.CreateTable<TurnoRow>();
        }

        public void IniciarSesion()
        {
            _conexion.DeleteAll<TurnoRow>();
        }

        public void RegistrarTurno(SessionTurn turno)
        {
            _conexion.Insert(new TurnoRow
            {
                Secuencia = turno.Secuencia,
                Hablante = (int)turno.Hablante,
                Texto = turno.Texto,
                EmotionTag = turno.EmotionTag,
                AnimationCue = turno.AnimationCue,
            });
        }

        public void FinalizarSesion()
        {
            // Nada que cerrar: cada turno ya quedo confirmado en disco al registrarse.
        }

        public IReadOnlyList<SessionTurn> ObtenerTurnos()
        {
            return _conexion.Table<TurnoRow>()
                .OrderBy(r => r.Id)
                .Select(r => new SessionTurn(r.Secuencia, (Hablante)r.Hablante, r.Texto, r.EmotionTag, r.AnimationCue))
                .ToList();
        }

        public void Dispose() => _conexion.Close();
    }
}
