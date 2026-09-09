# Propuesta: M13 — Bitácora de sesión (Runtime/SessionLog)

## Intent

`Runtime/SessionLog/` no existe todavía: hoy ninguna sesión de entrenamiento queda registrada, y
al cerrar la app se pierde toda la conversación entre el usuario y el NPC. M13 entrega un módulo
nuevo, puramente aditivo, que se suscribe a los dos canales que M0 ya expone
(`UtteranceChannel`, `NpcReplyChannel`) y persiste cada turno, uno por uno, en una base SQLite
local del dispositivo — sin acumular en memoria hasta el cierre, para no perder la sesión si la
app se cae a mitad de un entrenamiento. Al terminar, la sesión guardada puede exportarse a un
archivo de texto plano derivado de lo que ya está en la base.

Decisiones ya fijadas antes de este cambio (`Docs/MODULES.md`, confirmadas con el usuario el
2026-09-07):

1. Persistencia con **sqlite-net-pcl** (no P/Invoke manual a `sqlite3` nativo como hizo M1 con
   Vosk): trae su binario nativo empaquetado vía `SQLitePCLRaw.bundle_e_sqlite3` para las
   plataformas objetivo, evitando repetir el riesgo alto de vendorizado manual.
2. Ciclo de sesión **explícito**: `IniciarSesion()` / `FinalizarSesion()`, con la misma forma que
   `StartListening()`/`StopListening()` de `ISpeechToText` — control claro de cuándo abrir/cerrar
   el archivo SQLite y cuándo tiene sentido exportar.

## Scope

### In Scope

- Módulo nuevo `Runtime/SessionLog/`: modelo de turno, seam interno de persistencia
  (`ISessionStore`), orquestador (`SessionRecorder`) que traduce `Utterance`/`NpcReply` del
  contrato v1 a turnos de sesión.
- Doble determinista en `Runtime/SessionLog/Fakes/` (`InMemorySessionStore`), probado contra la
  misma base de pruebas que la implementación real (paridad, como M4).
- Implementación real de persistencia con `sqlite-net-pcl` (PR2).
- Sub-ensamblado `NpcAi.SessionLog.Unity` con el `MonoBehaviour` que se suscribe por Inspector a
  `UtteranceChannel`/`NpcReplyChannel` y expone `IniciarSesion()`/`FinalizarSesion()` a la escena
  anfitriona (PR3).
- Exportación a texto plano, derivada de lo ya persistido (no es una escritura paralela).
- `Tests/EditMode/SessionLog/`.

### Out of Scope

- `Runtime/Core/`, `Runtime/CoreChannels/` y cualquier otro módulo: **sin cambio de contrato**.
  M13 consume `Utterance` y `NpcReply` tal como ya existen; no se agrega ningún puerto nuevo a
  `NpcAi.Core.Ports.cs`.
- Cablear el `MonoBehaviour` de M13 dentro de una escena real de M11 (Harness): eso ocurre cuando
  esa escena exista; este cambio deja el componente listo para ese cableado.
- Multi-sesión concurrente o multi-NPC: se asume una sesión activa a la vez (una escena por
  persona, regla 4 del `README.md`). Si el proyecto crece a varias sesiones simultáneas, es un
  cambio aparte.
- Captura de `IntentResult`/`ReceptivityChange` en la bitácora: el alcance decidido son los dos
  canales que ya nombra `Docs/MODULES.md` (voz del usuario y respuesta del NPC). Ampliar el
  esquema para incluir diagnóstico de receptividad queda para un cambio posterior si se pide.
- UI de revisión/reproducción de sesiones grabadas: fuera de alcance, es un consumidor futuro de
  la base ya persistida.

## Capabilities

### New Capabilities

- `bitacora-sesion-m13`: registro persistente turno por turno de una sesión de entrenamiento
  (texto del usuario y respuesta del NPC), con exportación a texto derivada.

### Modified Capabilities

- Ninguna. `contrato-nucleo-m0` y `canales-evento-nucleo-m0` no cambian: M13 es un suscriptor
  puro de canales ya existentes.

## Approach

**Arquitectura hexagonal, igual que el resto del repo**: el núcleo (`NpcAi.SessionLog`) es C#
puro, `noEngineReferences: true`, sin saber nada de `UnityEngine` ni de `EventChannel<T>`. La
persistencia real (`ISessionStore`) es un seam **interno** del módulo — no un puerto de
`NpcAi.Core` — porque ningún otro módulo necesita conocerlo; solo `SessionRecorder` lo usa. El
único punto que toca Unity es un sub-ensamblado aparte, `NpcAi.SessionLog.Unity`, que sí referencia
`NpcAi.Core.Channels` para suscribirse a los dos canales por Inspector — exactamente la misma
partición que ya usa M4 (`NpcAi.Receptivity` puro + `NpcAi.Receptivity.Unity` como adaptador).

Esto significa que el 100% de la lógica de negocio de M13 (traducir eventos a turnos, numerar la
secuencia, ignorar turnos vacíos, ignorar eventos fuera de una sesión activa) se prueba en
EditMode sin escena, sin SQLite real y sin ningún `ScriptableObject` — el mismo estándar de
testabilidad que ya cumplen M2 y M4.

**Por qué `sqlite-net-pcl` y no un binario nativo vendorizado a mano**: M1 ya pagó ese costo con
Vosk (4 PRs, 3 bugs reales de vendorizado). `sqlite-net-pcl` es el cliente estándar de facto para
SQLite en proyectos Unity/.NET y su dependencia `SQLitePCLRaw.bundle_e_sqlite3` normalmente ya
resuelve el binario nativo por plataforma sin intervención manual. El riesgo no desaparece del
todo (falta verificar en un build IL2CPP/Android real, ver `Risks`), pero es sustancialmente menor
que repetir el patrón de M1.

## Affected Areas

| Area | Impacto | Descripción |
|---|---|---|
| `Runtime/SessionLog/` | Nuevo | Núcleo puro: modelo de turno, `ISessionStore`, `SessionRecorder` |
| `Runtime/SessionLog/Fakes/` | Nuevo | `InMemorySessionStore`, doble determinista |
| `Runtime/SessionLog/Sqlite/` | Nuevo (PR2) | `SqliteSessionStore` real con `sqlite-net-pcl` |
| `Runtime/SessionLog/Unity/` | Nuevo (PR3) | `NpcAi.SessionLog.Unity`, `SessionLogBehaviour` |
| `Tests/EditMode/SessionLog/` | Nuevo | Contrato interno compartido + pruebas de cada adaptador |
| `package.json` | Modificado (PR2) | Declarar la dependencia `sqlite-net-pcl` |
| `Runtime/Core/`, `Runtime/CoreChannels/`, resto de módulos | Sin cambio | Frontera dura: un cambio, un módulo |

## Risks

| Riesgo | Prob. | Mitigación |
|---|---|---|
| `sqlite-net-pcl`/`SQLitePCLRaw` no resuelve solo el binario nativo en un build Android/IL2CPP para Quest | Media | Seam `ISessionStore`: si falla, se cambia de librería sin tocar `SessionRecorder` ni las pruebas del núcleo. Validación manual en hardware antes de cerrar PR2, igual que M1 hizo con Vosk |
| Escribir a SQLite en cada turno (turno por turno, no por lote) introduce latencia perceptible durante la conversación | Baja | El motivo de negocio (no perder la sesión ante un crash) pesa más que el ahorro de I/O; si aparece latencia real se mide en M11 y se decide batching como cambio aparte |
| Orden de turnos incorrecto si `Utterance` y `NpcReply` llegan en el mismo frame | Baja | Todos los `Raise` de canal ya ocurren en el hilo principal de Unity (M1 usa `QueuedMainThreadPump` para garantizarlo); `SessionRecorder` numera con un contador de secuencia propio en el orden de llegada, no por reloj de pared |
| Ningún archivo `.unity` de M11 existe todavía para cablear el `MonoBehaviour` en una escena real | Alta (ya conocido) | Fuera de alcance de este cambio (ver Out of Scope); PR3 deja el componente listo, el cableado ocurre cuando M11 tenga escena |
| Sin CI ni hardware en fases de agente | Alta | El núcleo se prueba en EditMode vía el seam `ISessionStore`; la validación de `SqliteSessionStore` en dispositivo es compuerta humana (igual que M1) |

## Rollback Plan

Todo `Runtime/SessionLog/` es código nuevo y aditivo: revertir los commits del cambio deja el
sistema exactamente como está hoy. Ningún módulo existente depende de M13 — el acoplamiento es en
la dirección opuesta (M13 depende de `UtteranceChannel`/`NpcReplyChannel`, no al revés). No hay
migración de datos ni sube `Contract.Version`.

## Dependencies

- `contrato-nucleo-m0` y `canales-evento-nucleo-m0`, archivados y estables: definen `Utterance`,
  `NpcReply`, `UtteranceChannel`, `NpcReplyChannel`. M13 no los modifica.
- `sqlite-net-pcl` (PR2): librería de terceros nueva para el repo, primera dependencia declarada
  en `package.json` además del binario vendorizado de Vosk (M1).
- Unity 6 + Test Runner para la validación EditMode humana de cada PR.
- Dispositivo Android/Quest (o build IL2CPP) para validar `SqliteSessionStore` en PR2, igual que
  M1 necesitó hardware para validar Vosk.

## Success Criteria

- [ ] `SessionRecorder` traduce cada `Utterance` no vacía a un turno `Usuario` y cada `NpcReply`
      no vacía a un turno `Npc`, en orden de llegada, solo mientras hay una sesión activa.
- [ ] `InMemorySessionStore` y `SqliteSessionStore` pasan exactamente la misma batería de pruebas
      de comportamiento (paridad, como `RazonParityTests` de M4).
- [ ] Un turno ya escrito en `SqliteSessionStore` sobrevive a que la sesión no se cierre
      limpiamente (se abre una segunda vez el mismo store y el turno sigue ahí) — la garantía que
      motiva "turno por turno" en primer lugar.
- [ ] La exportación a texto es derivable 1:1 de lo que hay en el store: no escribe nada que no
      viniera ya de `ObtenerTurnos()`.
- [ ] El diff de cada PR no toca ninguna ruta fuera de `Runtime/SessionLog/`,
      `Tests/EditMode/SessionLog/`, `package.json` (solo PR2) y `openspec/`.
- [ ] El Test Runner EditMode completo (`Run All`) sigue en verde para M0–M5 después de cada PR.

## Decisiones del usuario (confirmadas 2026-09-07)

1. **Dueño de M13**: asignado; el nombre no se registra en este documento ni en `README.md`/
   `Docs/MODULES.md` (decisión explícita del usuario).
2. **Librería de persistencia**: `sqlite-net-pcl`, recomendado sobre P/Invoke manual — evita
   repetir el riesgo de vendorizado nativo que le costó 4 PRs a M1 con Vosk.
3. **Ciclo de sesión**: explícito, `IniciarSesion()`/`FinalizarSesion()`, con la misma forma que
   `StartListening()`/`StopListening()` de `ISpeechToText` — no ligado implícitamente al ciclo de
   vida de la escena.
