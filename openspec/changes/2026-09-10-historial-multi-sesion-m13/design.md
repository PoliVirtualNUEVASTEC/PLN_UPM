# Design: Historial multi-sesión para M13

## Technical Approach

`SessionTurn` (lo que dice cada turno) **no cambia** — sigue siendo Secuencia, Hablante, Texto,
EmotionTag, AnimationCue. Lo que cambia es cómo `ISessionStore` agrupa los turnos: en vez de "un
único cajón que se vacía en cada `IniciarSesion`", pasa a ser "un cajón por etiqueta, que nunca
se vacía solo".

## Architecture Decisions

| # | Decisión | Elección | Alternativas rechazadas | Razón |
|---|---|---|---|---|
| AD1 | Dónde vive la etiqueta de sesión | En el `ISessionStore` (parámetro de `IniciarSesion`, columna interna de almacenamiento), no en `SessionTurn` | Agregar `Etiqueta` como campo de `SessionTurn` | `SessionTurn` es el DTO público que ya usa `SessionRecorder`/`SessionRecorderTests`; cambiarlo rompe su forma actual sin necesidad. La etiqueta es un concepto de "a qué sesión pertenece este turno", que es responsabilidad del store, no del turno en sí |
| AD2 | Normalización de la etiqueta | `null`/vacío/solo espacios → constante `EtiquetaSesion.PorDefecto` (`"sesion-sin-etiqueta"`); de lo contrario, `Trim()` | Lanzar excepción ante etiqueta inválida | Ningún método de M13 lanza ante entrada rara (mismo espíritu que `Utterance`/`NpcReply` vacíos ya se ignoran silenciosamente en `SessionRecorder`); una etiqueta faltante no debe tumbar la app de entrenamiento |
| AD3 | Reusar una etiqueta ya existente | Se seguirá acumulando turnos bajo esa misma etiqueta (no se borra, no se duplica la fila de sesión) | Rechazar la segunda `IniciarSesion` con la misma etiqueta (no-op); o autogenerar un sufijo para desambiguar | Rechazar silenciosamente perdería turnos que el llamador cree que se están grabando; autogenerar sufijos contradice la decisión del usuario de que la etiqueta la elige quien llama. Acumular es lo único que no pierde datos ni inventa comportamiento no pedido |
| AD4 | Esquema en SQLite | Tabla nueva `SesionRow` (`Etiqueta` PK, `IniciadaEn`) + columna `Etiqueta` en `TurnoRow` existente | Una sola tabla con `GROUP BY Etiqueta` para listar sesiones | Una tabla de sesiones aparte permite que `ListarSesiones()` sea barato (no escanea todos los turnos) y registra la fecha de inicio aunque una sesión todavía no tenga turnos |
| AD5 | Orden de `ListarSesiones()` | Más reciente primero (`IniciadaEn` descendente) | Orden alfabético por etiqueta | Es lo útil para revisar historial: lo último entrenado arriba |
| AD6 | `ObtenerTurnos()` (sin argumento) | Sigue significando "turnos de la sesión activa" — azúcar sintáctico sobre `ObtenerTurnosDeSesion(etiquetaActiva)` | Eliminarlo y forzar siempre a pasar la etiqueta | Ningún consumidor existente (`SessionRecorder`, pruebas ya escritas) debe reescribirse más de lo necesario; mantiene el mismo patrón que ya usa `SessionRecorder.ObtenerTurnos()` |
| AD7 | Reanudación tras un cierre no limpio | `SessionLogBehaviour.OnEnable()` llama `SessionRecorder.ReanudarUltimaSesion()` automáticamente, sin esperar que la escena anfitriona vuelva a llamar `IniciarSesion` con la etiqueta correcta | Dejar que la sesión quede inactiva hasta que alguien la reinicie a mano; o recordar la última etiqueta en un `PlayerPrefs`/archivo aparte | Decisión del usuario (2026-09-14): tras un crash, no debe hacer falta reintroducir la etiqueta para no perder de vista la conversación. El propio store ya sabe cuál fue la última sesión (`ListarSesiones()` ordenado por fecha) — no hace falta un mecanismo de persistencia adicional. Efecto aceptado: los turnos nuevos quedan concatenados con los de antes del cierre, bajo la misma etiqueta (ver AD3) |
| AD8 | Numeración de `Secuencia` al retomar una etiqueta | `SessionRecorder.IniciarSesion` calcula `_siguienteSecuencia` como `ObtenerTurnosDeSesion(etiqueta).Count`, no siempre 0 | Reiniciar siempre en 0 (comportamiento previo) | Reiniciar en 0 al reusar/retomar una etiqueta generaba `Secuencia` duplicada entre los turnos viejos y los nuevos bajo la misma etiqueta — inofensivo para el orden real (que se rige por `Id`/orden de inserción, no por `Secuencia`), pero incorrecto como diagnóstico. Se corrige junto con AD7 porque `ReanudarUltimaSesion` expone el caso con más frecuencia |

## Data Flow

    SessionLogBehaviour.IniciarSesion("Juan - Triaje 1")
      -> SessionRecorder.IniciarSesion(etiqueta)
        -> ISessionStore.IniciarSesion(etiqueta)
             (SqliteSessionStore: INSERT OR IGNORE en SesionRow; InMemorySessionStore: crea el diccionario si no existe)
             -> guarda `etiqueta` como "activa"

    SessionRecorder.RegistrarUtterance(u) / RegistrarRespuesta(r)
      -> ISessionStore.RegistrarTurno(turno)   // igual que hoy, pero la fila queda tageada con la etiqueta activa

    // Para revisar historial (nuevo):
    SessionRecorder.ListarSesiones() -> [SesionInfo("Juan - Triaje 1", 2026-09-10T14:32), SesionInfo("Ana - Emergencia", 2026-09-09T10:05), ...]
    SessionRecorder.ObtenerTurnosDeSesion("Ana - Emergencia") -> turnos de esa sesión, sin tocar la sesión activa actual

    // Cierre no limpio y reapertura (nuevo, AD7):
    (la app se cierra a mitad de "Juan - Triaje 1", nadie llamo FinalizarSesion)
    SessionLogBehaviour.OnEnable()  // la app vuelve a abrir
      -> SessionRecorder.ReanudarUltimaSesion()
           -> ListarSesiones()[0].Etiqueta == "Juan - Triaje 1"
           -> IniciarSesion("Juan - Triaje 1")   // misma etiqueta: continua, no borra (AD3) ni reinicia Secuencia (AD8)
      -> SesionActiva == true, EtiquetaActiva == "Juan - Triaje 1", sin que nadie la haya vuelto a escribir

## Interfaces

```csharp
// Runtime/SessionLog/SesionInfo.cs (nuevo)
public readonly struct SesionInfo
{
    public readonly string   Etiqueta;
    public readonly DateTime IniciadaEn;
}

// Runtime/SessionLog/ISessionStore.cs (modificado)
public interface ISessionStore
{
    void IniciarSesion(string etiqueta);
    void RegistrarTurno(SessionTurn turno);
    void FinalizarSesion();
    IReadOnlyList<SessionTurn> ObtenerTurnos();
    IReadOnlyList<SesionInfo> ListarSesiones();
    IReadOnlyList<SessionTurn> ObtenerTurnosDeSesion(string etiqueta);
}
```

`SessionRecorder` expone los mismos 3 métodos de lectura nuevos/cambiados, delegando 1:1 al
store — sigue sin lógica de negocio propia sobre el historial, solo guardas de sesión activa
para `IniciarSesion`/`RegistrarUtterance`/`RegistrarRespuesta` (sin cambio ahí). Suma
`ReanudarUltimaSesion()` (AD7, sí tiene lógica propia: consulta `ListarSesiones()` y delega a
`IniciarSesion` con la más reciente) y la propiedad de solo lectura `EtiquetaActiva` (para que un
consumidor —hoy el arnés de prueba, mañana una UI real— pueda mostrar "retomando sesión: X").

## File Inventory

- `Runtime/SessionLog/SesionInfo.cs` — nuevo DTO.
- `Runtime/SessionLog/ISessionStore.cs` — firma modificada + 2 métodos nuevos.
- `Runtime/SessionLog/SessionRecorder.cs` — refleja lo anterior, más `ReanudarUltimaSesion()`, `EtiquetaActiva`, y numeración de `Secuencia` que continúa en vez de reiniciar (AD8).
- `Runtime/SessionLog/SessionExport.cs` — overload `ExportarTextoPlano(ISessionStore, string etiqueta)`.
- `Runtime/SessionLog/Fakes/InMemorySessionStore.cs` — reescrito sobre `Dictionary<string, List<SessionTurn>>`.
- `Runtime/SessionLog/Sqlite/SqliteSessionStore.cs` — tabla `SesionRow` nueva, `TurnoRow.Etiqueta` nueva, sin `DeleteAll`.
- `Runtime/SessionLog/Unity/SessionLogBehaviour.cs` — `IniciarSesion(string)`, pass-through de listado/consulta, `EtiquetaActiva`, y llama `ReanudarUltimaSesion()` en `OnEnable` (AD7).
- `Tests/EditMode/SessionLog/SessionStoreContract.cs` — reescrita para el nuevo comportamiento.
- `Tests/EditMode/SessionLog/{InMemorySessionStoreTests,SqliteSessionStoreTests,SessionRecorderTests,SessionExportTests}.cs` — ajustes de firma + pruebas nuevas.

## Testing Strategy

Misma exigencia que el M13 original: el 100% de la lógica de multi-sesión se prueba en EditMode
sin escena. La `SessionStoreContract` reescrita cubre:

- Dos etiquetas distintas no mezclan sus turnos.
- `ListarSesiones()` incluye todas las etiquetas iniciadas, más reciente primero.
- `ObtenerTurnosDeSesion` de una etiqueta pasada no se ve afectada por la sesión activa actual.
- Reusar una etiqueta acumula (no borra) los turnos anteriores de esa etiqueta.
- Etiqueta `null`/vacía cae al valor por defecto sin lanzar.

`InMemorySessionStoreTests` y `SqliteSessionStoreTests` heredan esa base, igual que hoy, más la
prueba dedicada de supervivencia a reabrir el archivo (sin cambios, sigue aplicando).

`SessionRecorderTests` agrega, para AD7/AD8:

- Reusar una etiqueta ya usada continúa la numeración de `Secuencia` en vez de reiniciar en 0.
- `ReanudarUltimaSesion()` retoma la etiqueta más reciente y permite seguir registrando turnos
  con un `SessionRecorder` nuevo sobre el mismo store (simulando la app reabriéndose).
- `ReanudarUltimaSesion()` sin ninguna sesión previa no activa nada (`SesionActiva` sigue falso).

## Open Questions

Ninguna: las dos decisiones de diseño (etiqueta manual, alcance con listado/consulta) ya las fijó
el usuario antes de escribir esta propuesta.
