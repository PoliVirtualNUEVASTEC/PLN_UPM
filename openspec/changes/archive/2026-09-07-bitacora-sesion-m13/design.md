# Design: M13 — Bitácora de sesión

## Technical Approach

Módulo nuevo, arquitectura hexagonal: un núcleo C# puro sin `UnityEngine` (`NpcAi.SessionLog`,
`noEngineReferences: true`) contiene toda la lógica de negocio, y un sub-ensamblado aparte
(`NpcAi.SessionLog.Unity`) es el único punto que toca Unity — igual partición que ya usa M4
(`NpcAi.Receptivity` / `NpcAi.Receptivity.Unity`).

### Unidad de módulo y grafo de referencias

    NpcAi.SessionLog          -> [NpcAi.Core]                                   (noEngineReferences: true)
    NpcAi.SessionLog.Unity    -> [NpcAi.Core, NpcAi.Core.Channels, NpcAi.SessionLog]

`NpcAi.SessionLog` puede referenciar `NpcAi.Core` sin romper su pureza porque `NpcAi.Core` es en
sí mismo C# puro (`noEngineReferences: true`) — el mismo razonamiento que ya vale para
`NpcAi.Receptivity`. `NpcAi.SessionLog` **no** referencia `NpcAi.Core.Channels`: no necesita saber
qué es un `EventChannel<T>`, solo recibe `Utterance`/`NpcReply` ya resueltos como parámetros de
método. Ningún otro módulo de `Runtime/` gana ni pierde referencias.

## Architecture Decisions

| # | Decisión | Elección | Alternativas rechazadas | Razón |
|---|---|---|---|---|
| AD1 | Forma del módulo | Núcleo puro C# + sub-ensamblado `.Unity`, igual que M4 | Un solo ensamblado con `UnityEngine` desde el inicio | Deja el 100% de la lógica de traducción/orquestación testeable en EditMode sin escena ni SQLite real, igual estándar que M2/M4 |
| AD2 | ¿`ISessionStore` es un puerto de `NpcAi.Core`? | No — es un seam **interno** de `NpcAi.SessionLog`, análogo a `IRecognitionEngine`/`IAudioCapture` de M1 | Agregar `ISessionLog` a `Runtime/Core/Ports.cs` | Ningún otro módulo necesita conocer la persistencia de M13; agregarlo a M0 sería un cambio de contrato (regla 2) sin beneficio — nadie más lo consume |
| AD3 | Qué entra a un turno | `SessionTurn`: secuencia, hablante (`Usuario`/`Npc`), texto, `EmotionTag`/`AnimationCue` (vacíos para `Usuario`) | Persistir el DTO completo de `Utterance`/`NpcReply` (incluyendo `Confidence`, `DurationSeconds`) | El alcance decidido (`Docs/MODULES.md`) es la conversación en sí; los campos de diagnóstico no son parte de la bitácora pedida. Ampliar el esquema es un cambio de datos posterior, no de contrato |
| AD4 | Orden de los turnos | Contador de secuencia propio de `SessionRecorder`, incrementado en el orden en que llegan las llamadas | Timestamp de reloj de pared | Todos los `Raise` de canal ocurren en el hilo principal de Unity (patrón ya establecido por M1 con `QueuedMainThreadPump`); un contador de secuencia es determinista y no depende de la resolución del reloj del dispositivo |
| AD5 | Ciclo de sesión | `IniciarSesion()`/`FinalizarSesion()` explícitos en `SessionRecorder`, delegando a `ISessionStore` | Grabar desde el primer evento recibido, sin sesión explícita | Decisión del usuario 2026-09-07; misma forma que `StartListening()`/`StopListening()` de `ISpeechToText`. Fuera de una sesión activa, `RegistrarUtterance`/`RegistrarRespuesta` son no-op — mismo espíritu que "no emite antes de Start ni después de Stop" |
| AD6 | Turnos vacíos | `RegistrarUtterance`/`RegistrarRespuesta` ignoran silenciosamente una `Utterance`/`NpcReply` con `IsEmpty == true` | Persistir turnos vacíos y filtrarlos en la exportación | `Utterance.IsEmpty`/`NpcReply.IsEmpty` ya existen en el contrato v1; filtrar en el borde evita basura en la base y en el archivo exportado |
| AD7 | Adaptador de persistencia real | `sqlite-net-pcl` (PR2) | P/Invoke manual a `sqlite3` nativo, igual que `VoskInterop` de M1 | Decisión del usuario 2026-09-07: evitar repetir el vendorizado manual de binario nativo que le costó 4 PRs a M1. `sqlite-net-pcl` trae `SQLitePCLRaw.bundle_e_sqlite3`, que normalmente resuelve el binario por plataforma sin intervención manual |
| AD8 | Exportación a texto | Función pura sobre `ISessionStore.ObtenerTurnos()`, sin escritura paralela | Escribir a SQLite y a texto en cada turno | `Docs/MODULES.md` ya fija que el archivo de texto es una exportación derivada, no una escritura paralela — evita divergencia entre las dos fuentes |
| AD9 | Ubicación del cableado a Unity | `NpcAi.SessionLog.Unity/SessionLogBehaviour.cs`, suscrito por Inspector a `UtteranceChannel`/`NpcReplyChannel`, exponiendo `IniciarSesion()`/`FinalizarSesion()` a la escena anfitriona | Que `SessionRecorder` mismo sea un `MonoBehaviour` | Mantiene `NpcAi.SessionLog` con `noEngineReferences: true`; el `MonoBehaviour` es una envoltura delgada, mismo patrón que `SpeechToTextBehaviour` (M1) sobre `OfflineSpeechToText` |
| AD10 | Validación de aceptación | Verde EditMode verificado por humano en el Test Runner de Unity 6; validación de `SqliteSessionStore` en dispositivo/build IL2CPP como compuerta humana adicional en PR2 | Compuerta de CI | No hay CI en el repo; mismo criterio que M1, M4, M5 |

## Data Flow

    UtteranceChannel.Raise(utterance)  ──┐
                                          │  (Subscribe, NpcAi.SessionLog.Unity)
    NpcReplyChannel.Raise(reply)  ───────┤
                                          ▼
                          SessionLogBehaviour (MonoBehaviour)
                                          │  RegistrarUtterance / RegistrarRespuesta
                                          ▼
                          SessionRecorder (NpcAi.SessionLog, puro)
                                          │  numera secuencia, arma SessionTurn
                                          ▼
                          ISessionStore.RegistrarTurno(turno)
                             ├── InMemorySessionStore   (Fakes/, pruebas)
                             └── SqliteSessionStore      (Sqlite/, real — PR2)
                                          │
                                          ▼
                          ObtenerTurnos() ──► exportación a texto (derivada)

## File Inventory

| Archivo | Rol | PR |
|---|---|---|
| `Runtime/SessionLog/NpcAi.SessionLog.asmdef` | Ensamblado puro | PR1 |
| `Runtime/SessionLog/SessionTurn.cs` | Modelo de turno | PR1 |
| `Runtime/SessionLog/ISessionStore.cs` | Seam interno de persistencia | PR1 |
| `Runtime/SessionLog/SessionRecorder.cs` | Orquestador: traduce y numera | PR1 |
| `Runtime/SessionLog/Fakes/InMemorySessionStore.cs` | Doble determinista | PR1 |
| `Tests/EditMode/SessionLog/NpcAi.SessionLog.Tests.asmdef` | Ensamblado de prueba | PR1 |
| `Tests/EditMode/SessionLog/SessionStoreContract.cs` | Base de comportamiento compartida (paridad) | PR1 |
| `Tests/EditMode/SessionLog/InMemorySessionStoreTests.cs` | Hereda el contrato interno | PR1 |
| `Tests/EditMode/SessionLog/SessionRecorderTests.cs` | Traducción, secuencia, guardas de sesión | PR1 |
| `Runtime/SessionLog/Sqlite/SqliteSessionStore.cs` | Adaptador real | PR2 |
| `Tests/EditMode/SessionLog/SqliteSessionStoreTests.cs` | Hereda el contrato interno | PR2 |
| `package.json` | Declarar `sqlite-net-pcl` | PR2 |
| `Runtime/SessionLog/Unity/NpcAi.SessionLog.Unity.asmdef` | Ensamblado adaptador Unity | PR3 |
| `Runtime/SessionLog/Unity/SessionLogBehaviour.cs` | Cableado por Inspector, API de sesión | PR3 |

## Interfaces / Contracts

Sin firmas nuevas en `NpcAi.Core`. Superficie pública nueva, interna a M13:

```csharp
// NpcAi.SessionLog (puro)
public enum Hablante { Usuario, Npc }

public readonly struct SessionTurn
{
    public readonly int Secuencia;
    public readonly Hablante Hablante;
    public readonly string Texto;
    public readonly string EmotionTag;   // vacío para turnos de Usuario
    public readonly string AnimationCue; // vacío para turnos de Usuario
}

public interface ISessionStore
{
    void IniciarSesion();
    void RegistrarTurno(SessionTurn turno);
    void FinalizarSesion();
    IReadOnlyList<SessionTurn> ObtenerTurnos();
}

public sealed class SessionRecorder
{
    public bool SesionActiva { get; }
    public void IniciarSesion();
    public void RegistrarUtterance(Utterance utterance);   // no-op si !SesionActiva o IsEmpty
    public void RegistrarRespuesta(NpcReply reply);         // no-op si !SesionActiva o IsEmpty
    public void FinalizarSesion();
    public IReadOnlyList<SessionTurn> ObtenerTurnos();
}
```

## Testing Strategy

| Archivo | Qué fija |
|---|---|
| `SessionStoreContract.cs` (abstracta) | Comportamiento compartido de `ISessionStore`: sin sesión activa `ObtenerTurnos()` empieza vacío; `RegistrarTurno` tras `IniciarSesion` acumula en orden; `FinalizarSesion` no borra lo ya escrito; una segunda `IniciarSesion` no filtra turnos de la sesión anterior a la nueva (reinicia limpio) |
| `InMemorySessionStoreTests` | Hereda `SessionStoreContract` |
| `SqliteSessionStoreTests` (PR2) | Hereda `SessionStoreContract`; además, un turno sobrevive a reabrir el mismo archivo `.db` sin `FinalizarSesion()` limpio (la garantía de "turno por turno") |
| `SessionRecorderTests` | `Utterance`/`NpcReply` vacías no generan turno; eventos fuera de sesión activa son no-op; secuencia estrictamente creciente en orden de llegada; `Usuario` vs `Npc` mapea correctamente `EmotionTag`/`AnimationCue` |

Ejecución: Test Runner de Unity 6 (EditMode > Run All). PR1 sin escena, sin SQLite real, sin VR.

## Migration / Rollout

No se requiere migración: módulo aditivo, no toca `Runtime/Core/`/`Runtime/CoreChannels/`, no es
cambio de contrato. Se integra como cambio propio con checklist "Antes de mergear" del `README.md`.

Rollback: `git revert` de los commits de cada PR elimina el módulo; ningún otro módulo depende de
M13 en ninguna dirección.

## Open Questions

- [ ] Si más adelante se pide capturar `IntentResult`/`ReceptivityChange` en la bitácora (para
      análisis, no solo transcripción), es una ampliación de `SessionTurn` y de
      `SessionLogBehaviour` — cambio propio de M13, no toca `NpcAi.Core`.
- [ ] Validar en build IL2CPP/Android real que `SQLitePCLRaw.bundle_e_sqlite3` resuelve el binario
      nativo sin vendorizado manual (PR2, compuerta humana, igual que M1 con Vosk).
