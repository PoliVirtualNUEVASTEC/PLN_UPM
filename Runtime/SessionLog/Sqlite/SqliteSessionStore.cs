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
    /// llamar <see cref="FinalizarSesion"/>. Cada etiqueta de sesion persiste indefinidamente
    /// (AD3, AD4): <see cref="IniciarSesion"/> nunca borra sesiones anteriores.
    /// </summary>
    public sealed class SqliteSessionStore : ISessionStore, IDisposable
    {
        private sealed class SesionRow
        {
            [PrimaryKey]
            public string Etiqueta { get; set; } = string.Empty;
            public DateTime IniciadaEn { get; set; }
            public bool Cerrada { get; set; }
        }

        private sealed class TurnoRow
        {
            [PrimaryKey, AutoIncrement]
            public int Id { get; set; }
            public string Etiqueta { get; set; } = string.Empty;
            public int Secuencia { get; set; }
            public int Hablante { get; set; }
            public string Texto { get; set; } = string.Empty;
            public string EmotionTag { get; set; } = string.Empty;
            public string AnimationCue { get; set; } = string.Empty;
        }

        private readonly SQLiteConnection _conexion;
        private string _etiquetaActiva;

        /// <param name="rutaArchivo">
        /// Ruta absoluta al archivo .db. En Unity real es responsabilidad del llamador pasar una
        /// ruta bajo <c>Application.persistentDataPath</c>; en pruebas EditMode se inyecta una
        /// ruta temporal para no depender de la API de Unity.
        /// </param>
        public SqliteSessionStore(string rutaArchivo)
        {
            _conexion = new SQLiteConnection(rutaArchivo);
            _conexion.CreateTable<SesionRow>();
            _conexion.CreateTable<TurnoRow>();
        }

        public void IniciarSesion(string etiqueta)
        {
            etiqueta = EtiquetaSesion.Normalizar(etiqueta);

            var existente = _conexion.Find<SesionRow>(etiqueta);
            if (existente == null)
            {
                _conexion.Insert(new SesionRow { Etiqueta = etiqueta, IniciadaEn = DateTime.UtcNow, Cerrada = false });
            }
            else if (existente.Cerrada)
            {
                existente.Cerrada = false; // se esta reactivando: ya no cuenta como cerrada
                _conexion.Update(existente);
            }

            _etiquetaActiva = etiqueta;
        }

        public void RegistrarTurno(SessionTurn turno)
        {
            _conexion.Insert(new TurnoRow
            {
                Etiqueta = _etiquetaActiva,
                Secuencia = turno.Secuencia,
                Hablante = (int)turno.Hablante,
                Texto = turno.Texto,
                EmotionTag = turno.EmotionTag,
                AnimationCue = turno.AnimationCue,
            });
        }

        public void FinalizarSesion()
        {
            // Los turnos ya quedaron confirmados en disco al registrarse; solo marcamos la
            // sesion como cerrada limpiamente, para que ReanudarUltimaSesion no la retome sola.
            if (_etiquetaActiva == null) return;

            var fila = _conexion.Find<SesionRow>(_etiquetaActiva);
            if (fila == null) return;

            fila.Cerrada = true;
            _conexion.Update(fila);
        }

        public IReadOnlyList<SessionTurn> ObtenerTurnos() => ObtenerTurnosDeSesion(_etiquetaActiva);

        public IReadOnlyList<SesionInfo> ListarSesiones()
        {
            return _conexion.Table<SesionRow>()
                .OrderByDescending(r => r.IniciadaEn)
                .Select(r => new SesionInfo(r.Etiqueta, r.IniciadaEn, r.Cerrada))
                .ToList();
        }

        public IReadOnlyList<SessionTurn> ObtenerTurnosDeSesion(string etiqueta)
        {
            if (etiqueta == null) return Array.Empty<SessionTurn>();

            etiqueta = EtiquetaSesion.Normalizar(etiqueta);
            return _conexion.Table<TurnoRow>()
                .Where(r => r.Etiqueta == etiqueta)
                .OrderBy(r => r.Id)
                .Select(r => new SessionTurn(r.Secuencia, (Hablante)r.Hablante, r.Texto, r.EmotionTag, r.AnimationCue))
                .ToList();
        }

        public void Dispose() => _conexion.Close();
    }
}
