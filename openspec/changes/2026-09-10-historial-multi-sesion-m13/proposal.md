# Propuesta: Historial multi-sesión para M13 (Runtime/SessionLog)

## Intent

`SqliteSessionStore.IniciarSesion()` hoy borra todos los turnos antes de empezar una sesión
nueva (`DeleteAll<TurnoRow>()`), y esa garantía está probada explícitamente en
`SessionStoreContract.Una_segunda_IniciarSesion_reinicia_limpio`. Eso significa que la base solo
retiene la sesión más reciente: no hay forma de revisar el entrenamiento de ayer una vez que
alguien vuelve a entrenar hoy. Esta propuesta reemplaza esa garantía por una nueva: **cada sesión
queda identificada por una etiqueta que decide quien la inicia, y todas las sesiones conviven en
la base indefinidamente**, consultables por separado.

Decisiones ya fijadas con el usuario (2026-09-10):

1. **Identificador de sesión**: etiqueta manual de texto, provista por el llamador en
   `IniciarSesion(string etiqueta)` (p. ej. `"Juan Perez - Triaje 1"`). No es un timestamp ni un
   GUID autogenerado — es responsabilidad de quien llama elegir una etiqueta que tenga sentido
   para su caso (típicamente incluyendo fecha/hora u otro dato que la haga única).
2. **Alcance**: no es solo "no borrar" — se agrega la forma de listar y consultar sesiones
   pasadas (`ListarSesiones()`, `ObtenerTurnosDeSesion(etiqueta)`), para que un futuro consumidor
   (una UI de M11, o un script del propio usuario) pueda navegarlas sin escribir SQL a mano.

Ampliación (2026-09-14), motivada por una prueba real del usuario: si la app se cierra por un
error a mitad de una sesión, al reabrir no debe hacer falta volver a escribir/seleccionar la
etiqueta correcta para seguir grabando donde se quedó — eso ya lo resuelve el propio store
(sabe cuál fue la última sesión), así que `SessionLogBehaviour` la retoma sola al habilitarse.
El efecto aceptado explícitamente por el usuario: los turnos de después del cierre quedan
concatenados con los de antes, bajo la misma etiqueta (ver decisión 1 de arriba — reusar una
etiqueta acumula, no separa).

## Scope

### In Scope

- Cambiar la firma de `ISessionStore.IniciarSesion()` a `IniciarSesion(string etiqueta)`, y
  quitar el `DeleteAll` de `SqliteSessionStore`. Mismo cambio de firma en `SessionRecorder` y en
  `SessionLogBehaviour` (`Runtime/SessionLog/Unity/`).
- Dos métodos nuevos en `ISessionStore` (y su reflejo en `SessionRecorder`):
  `ListarSesiones()` (catálogo de sesiones conocidas, más recientes primero) y
  `ObtenerTurnosDeSesion(string etiqueta)` (turnos de una sesión específica, pasada o activa).
- `ObtenerTurnos()` mantiene su significado actual — turnos de la sesión **activa** — ahora
  como un caso particular de `ObtenerTurnosDeSesion(etiquetaActiva)`.
- Nuevo DTO interno `SesionInfo` (etiqueta + fecha/hora de inicio) para `ListarSesiones()`.
- Actualizar `InMemorySessionStore` y `SqliteSessionStore` para que seguir pasando la misma
  `SessionStoreContract` (reescrita para el nuevo comportamiento) — paridad, igual que M4.
- `SessionExport.ExportarTextoPlano` gana un overload `(ISessionStore, string etiqueta)` para
  exportar una sesión pasada puntual, además del ya existente sobre la sesión activa.
- `SessionLogBehaviour.OnEnable()` llama `SessionRecorder.ReanudarUltimaSesion()`
  automáticamente, para retomar la sesión más reciente sin intervención tras un cierre no
  limpio. `SessionRecorder` gana `ReanudarUltimaSesion()` y la propiedad `EtiquetaActiva`.
  `IniciarSesion` deja de reiniciar siempre `Secuencia` en 0: continúa donde iba si la etiqueta
  ya tenía turnos.

### Out of Scope

- Cualquier UI dentro de Unity para navegar el historial (selector de sesiones, reproductor):
  eso es un consumidor futuro de `ListarSesiones()`/`ObtenerTurnosDeSesion`, no de este cambio.
- Migrar datos de bases `.db` ya creadas con el esquema viejo: no existe ningún despliegue real
  todavía (M11 sigue sin escena), así que no hay datos de producción que migrar.
- Límite o rotación de historial (p. ej. "borrar sesiones de hace más de N días"): si la base
  crece demasiado con el tiempo es un problema de operación posterior, no de este cambio.
- Resolver colisiones de etiqueta de forma automática (ver Risks): la responsabilidad de elegir
  una etiqueta única es de quien llama `IniciarSesion`.

## Capabilities

### Modified Capabilities

- `bitacora-sesion-m13`: cambia el requisito "una segunda `IniciarSesion` reinicia limpio" por
  "cada etiqueta de sesión persiste independientemente; `IniciarSesion` nunca borra sesiones
  anteriores". Se agregan tres requisitos nuevos (`ListarSesiones`, `ObtenerTurnosDeSesion`,
  reanudación automática tras un cierre no limpio).

## Approach

Ver `design.md` para las decisiones de arquitectura (AD1-AD6). En resumen: se agrega una tabla
`SesionRow` (etiqueta + fecha de inicio) junto a la `TurnoRow` ya existente en
`SqliteSessionStore`, y cada `TurnoRow` gana una columna `Etiqueta` para poder filtrar por
sesión. `InMemorySessionStore` refleja lo mismo con un `Dictionary<string, List<SessionTurn>>`
por etiqueta. Ningún otro módulo se ve afectado: `ISessionStore` sigue siendo un seam interno de
`NpcAi.SessionLog`, sin tocar `Runtime/Core/`.

## Affected Areas

| Area | Impacto | Descripción |
|---|---|---|
| `Runtime/SessionLog/ISessionStore.cs` | Modificado | Firma de `IniciarSesion`, 2 métodos nuevos |
| `Runtime/SessionLog/SesionInfo.cs` | Nuevo | DTO interno (etiqueta + fecha de inicio) |
| `Runtime/SessionLog/SessionRecorder.cs` | Modificado | Refleja la nueva firma y los métodos nuevos |
| `Runtime/SessionLog/SessionExport.cs` | Modificado | Overload para exportar una sesión puntual |
| `Runtime/SessionLog/Fakes/InMemorySessionStore.cs` | Modificado | Multi-sesión por diccionario |
| `Runtime/SessionLog/Sqlite/SqliteSessionStore.cs` | Modificado | Tabla `SesionRow` nueva, columna `Etiqueta` en `TurnoRow`, sin `DeleteAll` |
| `Runtime/SessionLog/Unity/SessionLogBehaviour.cs` | Modificado | `IniciarSesion(string)`, pass-through de los métodos nuevos |
| `Tests/EditMode/SessionLog/SessionStoreContract.cs` | Modificado | Reemplaza la prueba de "reinicia limpio" por las de multi-sesión |
| `Tests/EditMode/SessionLog/*Tests.cs` | Modificado/Nuevo | Ajustar firmas, pruebas nuevas de listado/consulta |
| `openspec/specs/bitacora-sesion-m13/spec.md` | Modificado | Requisito de reinicio reemplazado; 2 requisitos nuevos |
| `Runtime/Core/`, resto de módulos | Sin cambio | Frontera dura: `ISessionStore` sigue sin ser puerto de M0 |

## Risks

| Riesgo | Prob. | Mitigación |
|---|---|---|
| Dos sesiones distintas terminan con la misma etiqueta (colisión del usuario) | Media | Documentado como responsabilidad del llamador; el comportamiento definido es "se acumulan bajo la misma etiqueta" (no se pierde nada, no se lanza), nunca sobrescritura silenciosa de datos |
| Cambiar la firma de `IniciarSesion` rompe al único consumidor real hasta ahora (el arnés de prueba manual del usuario, fuera del paquete) | Baja | Es exactamente el motivo de este cambio; se actualiza el arnés como parte de la validación manual, no como parte del diff del paquete |
| Crecimiento sin límite de la base con el tiempo | Baja (por ahora) | Fuera de alcance (ver Out of Scope); se revisita si se vuelve un problema real en uso prolongado |
| `ReanudarUltimaSesion()` no puede distinguir "la app se cayó" de "alguien reinició la escena a propósito" — en ambos casos retoma la última etiqueta | Media | Aceptado explícitamente por el usuario (2026-09-14): si de verdad se quiere una sesión nueva, el llamador debe llamar `IniciarSesion(etiquetaNueva)` con una etiqueta distinta, que sí gana sobre la reanudación automática |

## Rollback Plan

Todo el cambio vive dentro de `Runtime/SessionLog/` y sus pruebas. Revertir los commits deja el
sistema en el estado de M13 archivado (una sola sesión activa, sin historial) — sin afectar
ningún otro módulo, porque `ISessionStore` sigue siendo un seam interno.

## Dependencies

- `bitacora-sesion-m13` (archivado): este cambio lo modifica, no lo reabre — es un cambio SDD
  independiente que declara `bitacora-sesion-m13` como Modified Capability.
- Ninguna dependencia externa nueva: sigue usando `sqlite-net-pcl` ya vendorizado.
