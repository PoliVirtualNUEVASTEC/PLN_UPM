# Tasks: M13 — Bitácora de sesión (Runtime/SessionLog)

## Review Workload Forecast

| Field | Value |
|---|---|
| Estimated changed lines | PR1 ~250, PR2 ~200 (excluyendo la librería de terceros), PR3 ~150 |
| 400-line budget risk | Bajo por PR |
| Chained PRs recommended | Sí |
| Suggested split | PR1 → PR2 → PR3 |
| Chain strategy | stacked-to-main (PR1 → main, PR2 → PR1, PR3 → PR2) |

### Suggested Work Units

| Unit | Meta | PR | Prueba enfocada | Frontera de rollback |
|---|---|---|---|---|
| 1 | Núcleo puro + doble en memoria | PR1 | EditMode: `SessionRecorderTests`, `InMemorySessionStoreTests` | Borrar `Runtime/SessionLog/` completo; ningún otro módulo se ve afectado |
| 2 | Adaptador SQLite real | PR2 | EditMode: `SqliteSessionStoreTests` (hereda `SessionStoreContract`) | Borrar `Runtime/SessionLog/Sqlite/`; PR1 sigue en verde con el doble |
| 3 | Sub-ensamblado Unity + cableado por Inspector | PR3 | Manual: escena de prueba suscrita a los dos canales | Borrar `Runtime/SessionLog/Unity/`; PR1/PR2 siguen en verde |

## Phase 0: Guardrails (leer antes de escribir)

- [x] 0.1 Frontera de escritura de PR1: SOLO `Runtime/SessionLog/` (sin `Sqlite/` ni `Unity/`
  todavía), `Tests/EditMode/SessionLog/` y `openspec/changes/2026-09-07-bitacora-sesion-m13/`.
  Cualquier edición en `Runtime/Core/`, `Runtime/CoreChannels/` o cualquier otro módulo de
  `Runtime/` está fuera de alcance: parar y avisar.
- [x] 0.2 Cero puerto nuevo en `Runtime/Core/Ports.cs`. `ISessionStore` es un seam interno de
  `NpcAi.SessionLog`, no del contrato compartido (AD2).
- [x] 0.3 `NpcAi.SessionLog` no referencia `NpcAi.Core.Channels` — el cableado a canales vive
  exclusivamente en el sub-ensamblado `NpcAi.SessionLog.Unity` (PR3).
- [x] 0.4 No es cambio de contrato: sin ventana fija ni co-aprobación de todos los dueños.

## Phase 1: Núcleo puro + doble en memoria (PR1)

- [x] 1.1 Crear `Runtime/SessionLog/NpcAi.SessionLog.asmdef` (`noEngineReferences: true`,
  referencias: `["NpcAi.Core"]`).
- [x] 1.2 Crear `Runtime/SessionLog/SessionTurn.cs`: `enum Hablante { Usuario, Npc }` +
  `readonly struct SessionTurn` (Secuencia, Hablante, Texto, EmotionTag, AnimationCue).
- [x] 1.3 Crear `Runtime/SessionLog/ISessionStore.cs`: `IniciarSesion`, `RegistrarTurno`,
  `FinalizarSesion`, `ObtenerTurnos`.
- [x] 1.4 Crear `Tests/EditMode/SessionLog/NpcAi.SessionLog.Tests.asmdef`.
- [x] 1.5 RED: `Tests/EditMode/SessionLog/SessionStoreContract.cs` (base abstracta,
  `CreateSubject()`) — sin sesión activa, `ObtenerTurnos()` empieza vacío; `RegistrarTurno` tras
  `IniciarSesion` acumula en orden; `FinalizarSesion` no borra lo ya escrito; una segunda
  `IniciarSesion` reinicia limpio.
- [x] 1.6 GREEN: Crear `Runtime/SessionLog/Fakes/InMemorySessionStore.cs`; crear
  `InMemorySessionStoreTests : SessionStoreContract` — hereda y pasa las 4 pruebas.
- [x] 1.7 RED: `SessionRecorderTests.cs` — `RegistrarUtterance`/`RegistrarRespuesta` con texto
  vacío no generan turno; fuera de sesión activa son no-op; secuencia estrictamente creciente en
  orden de llegada; `Usuario` no lleva `EmotionTag`/`AnimationCue`, `Npc` sí.
- [x] 1.8 GREEN: Crear `Runtime/SessionLog/SessionRecorder.cs` implementando las guardas de AD5/AD6.
- [x] 1.9 Confirmar que el resto del Test Runner (M0–M5) sigue en verde: PR1 no toca ninguna ruta
  fuera de `Runtime/SessionLog/`/`Tests/EditMode/SessionLog/`.

> PR1 (1.1–1.9) razonado e implementado por el agente; RED/GREEN no se ejecutó en el agente (sin
> CI/runner headless en este repo, igual que M1/M4/M5) — **verificación manual en Unity Editor
> pendiente del usuario** (Test Runner > EditMode > Run All).

## Phase 2: Adaptador SQLite real (PR2)

- [x] 2.1 ~~Declarar `sqlite-net-pcl` en `package.json` (`dependencies`)~~ — **decisión revisada
  con el usuario (2026-09-08): vendorizado manual**, igual que M1 hizo con Vosk, en vez de
  resolución vía UPM/NuGet. `sqlite-net-pcl` no es en realidad un paquete UPM: el AD original
  subestimó esto. DLLs administrados (`SQLite-net.dll`, `SQLitePCLRaw.core.dll`,
  `SQLitePCLRaw.provider.e_sqlite3.dll`, `SQLitePCLRaw.batteries_v2.dll`) y nativos
  (`e_sqlite3.dll` Windows x86_64, `libe_sqlite3.so` Android arm64-v8a) copiados a
  `Runtime/SessionLog/Sqlite/` con sus `.meta` de importación (mismo patrón que
  `Runtime/Speech/Plugins/` de M1). **Nota de seguridad**: la versión de `sqlite-net-pcl`
  (1.9.172) depende por defecto de `SQLitePCLRaw.bundle_green` 2.1.2, cuyo binario nativo tiene
  una vulnerabilidad conocida de severidad alta (CVE-2025-6965, corrupción de memoria en SQLite
  < 3.50.2). Se forzaron los paquetes nativos (`SQLitePCLRaw.lib.e_sqlite3` /
  `.lib.e_sqlite3.android`) a la versión 2.1.13, verificada binariamente para embeber SQLite
  3.53.3 (parchado). `package.json` no se toca — no hay dependencia UPM que declarar.
- [x] 2.2 Crear `Runtime/SessionLog/Sqlite/SqliteSessionStore.cs` implementando `ISessionStore`
  sobre una ruta de archivo inyectada por constructor (en Unity real, bajo
  `Application.persistentDataPath`; en pruebas EditMode, una ruta temporal).
- [x] 2.3 RED/GREEN: `Tests/EditMode/SessionLog/SqliteSessionStoreTests.cs : SessionStoreContract`
  contra un archivo `.db` temporal por prueba (crear y borrar en `[SetUp]`/`[TearDown]`).
- [x] 2.4 RED/GREEN: prueba dedicada — un turno escrito con `RegistrarTurno` sobrevive a reabrir
  el mismo archivo sin haber llamado `FinalizarSesion()` (garantía de "turno por turno").
- [ ] 2.5 MANUAL (dispositivo o build IL2CPP): confirmar que el binario nativo vendorizado
  (`libe_sqlite3.so` arm64-v8a) carga correctamente en un build IL2CPP/Android real. Si falla,
  revisar AD7 y considerar el respaldo de P/Invoke manual documentado en `Risks`.
- [x] 2.6 Función de exportación a texto (AD8): método puro sobre `ISessionStore.ObtenerTurnos()`,
  con prueba de que el resultado es derivable 1:1 de los turnos ya persistidos.

> PR2 (2.1–2.4, 2.6) razonado e implementado por el agente; RED/GREEN no se ejecutó en el agente
> (sin CI/runner headless en este repo, igual que PR1) — **verificación manual en Unity Editor
> pendiente del usuario** (Test Runner > EditMode > Run All). 2.5 sigue pendiente: requiere
> dispositivo o build IL2CPP real.

## Phase 3: Sub-ensamblado Unity + cableado (PR3)

- [x] 3.1 Crear `Runtime/SessionLog/Unity/NpcAi.SessionLog.Unity.asmdef` (referencias:
  `NpcAi.Core`, `NpcAi.Core.Channels`, `NpcAi.SessionLog`).
- [x] 3.2 Crear `Runtime/SessionLog/Unity/SessionLogBehaviour.cs`: campos `[SerializeField]` para
  `UtteranceChannel`/`NpcReplyChannel`, `Subscribe` en `OnEnable`, `Unsubscribe` en `OnDisable`,
  expone `IniciarSesion()`/`FinalizarSesion()` a la escena anfitriona (mismo patrón que
  `SpeechToTextBehaviour` de M1 sobre `OfflineSpeechToText`).
- [ ] 3.3 MANUAL: escena de prueba desechable (fuera del diff del paquete, igual que el arnés que
  M1 usó para validar `SpeechToTextBehaviour`) que dispara `Raise` manual sobre los dos canales y
  confirma que `SessionLogBehaviour` registra los turnos.
- [ ] 3.4 Cablear en la escena real de M11 cuando exista (fuera de alcance de este cambio; M11
  sigue sin escena `.unity` commiteada al momento de este PR — dejar anotado en `archive-report.md`).

> PR3 (3.1–3.2) razonado e implementado por el agente. Sin prueba EditMode automatizada para el
> `MonoBehaviour` — mismo precedente que `SpeechToTextBehaviour` de M1, que tampoco la tiene: el
> ciclo `OnEnable`/`OnDisable` y `Application.persistentDataPath` no son unit-testables sin
> escena. **Verificación pendiente del usuario**: (a) que el proyecto compile sin errores tras
> importar el nuevo sub-ensamblado, (b) Test Runner EditMode > Run All sigue en verde (no debería
> cambiar el conteo, este PR no agrega pruebas nuevas), y (c) 3.3 — la escena de prueba manual —
> cuando quieras validar el cableado end-to-end.

## Phase 4: Cierre (acciones del autor, cada PR)

- [ ] 4.1 `git add` solo de las carpetas del PR correspondiente; `git diff --cached` antes de
  cualquier commit, confirmando que no se cruza a otro módulo.
- [ ] 4.2 Checklist "Antes de mergear" del `README.md` (pruebas propias en verde, diff acotado,
  spec/design/tasks archivados, rama al día con `main`, decisiones registradas).
- [ ] 4.3 Al cerrar el último PR: mover este cambio a `openspec/changes/archive/` con su
  `spec.md` en `openspec/specs/bitacora-sesion-m13/` y su `archive-report.md`, y actualizar la
  sección M13 de `Docs/MODULES.md` de "decidido, no iniciado" a su estado real.
- [ ] 4.4 PR mergeado a `main` por el autor (regla 8).

## Notas

- Ningún agente ejecuta Unity: cada verde de Test Runner es compuerta humana, igual que en M1,
  M4 y M5.
- PR1 (este avance) no tiene dependencia externa ni SQLite real: es núcleo puro, testeable en
  milisegundos.
