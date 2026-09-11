# Especificacion: bitacora-sesion-m13

## Purpose

Congela el alcance y las garantias de M13 (`Runtime/SessionLog/`): un modulo nuevo,
puramente aditivo, que registra turno por turno una sesion de entrenamiento (lo que dijo
el usuario y lo que respondio el NPC) suscribiendose a los dos canales que
`contrato-nucleo-m0` ya expone (`UtteranceChannel`, `NpcReplyChannel`), sin agregar
ningun puerto nuevo a `NpcAi.Core`. Persiste en SQLite local con escritura confirmada en
cada turno (no por lote, para no perder la sesion si la app se cierra a mitad de un
entrenamiento) y permite exportar lo persistido a texto plano.

El puerto `IReceptivityEngine`/`IDialogueGenerator`, los DTO `Utterance`/`NpcReply` y los
canales de evento los posee `contrato-nucleo-m0`/`canales-evento-nucleo-m0` y NO se
redefinen aqui. El mecanismo de verificacion son las 22 pruebas EditMode de
`Tests/EditMode/SessionLog/` (`SessionRecorderTests`, `SessionStoreContract` heredada por
`InMemorySessionStoreTests` y `SqliteSessionStoreTests`, `SessionExportTests`).

## ADDED Requirements

### Requirement: Un seam interno de persistencia, no un puerto de M0

M13 DEBE definir `ISessionStore` como una interfaz interna de `NpcAi.SessionLog`, NO
como un puerto de `Runtime/Core/Ports.cs`. Ningun otro modulo DEBE conocer
`ISessionStore`: solo `SessionRecorder` lo usa. `NpcAi.SessionLog` NO DEBE referenciar
`NpcAi.Core.Channels` — ese cableado vive exclusivamente en el sub-ensamblado
`NpcAi.SessionLog.Unity`.

#### Scenario: El nucleo no depende de canales ni amplia el contrato

- Dado el asmdef `Runtime/SessionLog/NpcAi.SessionLog.asmdef`
- Cuando se revisan sus referencias
- Entonces solo lista `NpcAi.Core` (mas los DLL vendorizados de SQLite), nunca `NpcAi.Core.Channels`, y `Runtime/Core/Ports.cs` no gana ningun miembro nuevo

### Requirement: SessionRecorder traduce eventos del contrato a turnos, con guardas

`SessionRecorder.RegistrarUtterance` DEBE crear un turno `Hablante.Usuario` y
`RegistrarRespuesta` DEBE crear un turno `Hablante.Npc`, cada uno delegado a
`ISessionStore.RegistrarTurno`. Ambos DEBEN ser no-op (no crean turno) cuando no hay
sesion activa o cuando la entrada es vacia (`Utterance.IsEmpty` / `NpcReply.IsEmpty`). La
`Secuencia` de cada turno DEBE crecer estrictamente en el orden de llegada, reiniciando a
0 en cada `IniciarSesion()`. Un turno `Usuario` NO DEBE llevar `EmotionTag`/`AnimationCue`
(cadena vacia); un turno `Npc` DEBE copiarlos tal cual vienen en el `NpcReply`.

#### Scenario: Fuera de sesion activa, registrar es no-op

- Dado un `SessionRecorder` recien creado (sin `IniciarSesion`)
- Cuando se llama `RegistrarUtterance` o `RegistrarRespuesta`
- Entonces `ObtenerTurnos()` sigue vacio

#### Scenario: Texto vacio no genera turno

- Dado un `SessionRecorder` con sesion activa
- Cuando se registra un `Utterance` o `NpcReply` vacios (solo espacios o cadenas vacias)
- Entonces no se agrega ningun turno

#### Scenario: La secuencia crece en el orden de llegada y se reinicia por sesion

- Dado un `SessionRecorder` con sesion activa
- Cuando se registran 3 turnos en orden y luego se cierra y abre una sesion nueva
- Entonces la primera sesion queda con `Secuencia` 0, 1, 2 y la segunda sesion reinicia en 0

#### Scenario: Las etiquetas distinguen hablante

- Dado un `SessionRecorder` con sesion activa
- Cuando se registra un turno de `Usuario` y uno de `Npc`
- Entonces el de `Usuario` tiene `EmotionTag`/`AnimationCue` vacios y el de `Npc` los copia del `NpcReply`

### Requirement: El doble y el adaptador real comparten la misma bateria de contrato

`InMemorySessionStore` (doble) y `SqliteSessionStore` (real) DEBEN implementar
`ISessionStore` y pasar exactamente la misma `SessionStoreContract`: sin sesion activa no
hay turnos; `RegistrarTurno` tras `IniciarSesion` acumula en orden; `FinalizarSesion` NO
DEBE borrar los turnos ya registrados; una segunda `IniciarSesion` DEBE reiniciar limpio
(sin arrastrar turnos de una sesion anterior).

#### Scenario: Paridad de comportamiento entre doble y real

- Dado `InMemorySessionStoreTests` y `SqliteSessionStoreTests`, ambas heredando `SessionStoreContract`
- Cuando corre el Test Runner EditMode
- Entonces las 4 pruebas de la base pasan igual en las dos clases

### Requirement: Un turno sobrevive a un cierre no limpio de la sesion

`SqliteSessionStore.RegistrarTurno` DEBE confirmar cada turno en disco de inmediato (sin
transaccion abierta pendiente de commit). Si el proceso termina sin llamar
`FinalizarSesion()`, reabrir un `SqliteSessionStore` sobre el mismo archivo DEBE mostrar
el turno ya escrito en `ObtenerTurnos()`.

#### Scenario: Reabrir el archivo sin FinalizarSesion conserva el turno

- Dado un `SqliteSessionStore` con `IniciarSesion()` y un turno registrado, cerrado sin llamar `FinalizarSesion()`
- Cuando se crea un `SqliteSessionStore` nuevo sobre el mismo archivo
- Entonces `ObtenerTurnos()` devuelve ese turno

### Requirement: La exportacion a texto es derivable 1:1 de lo persistido

`SessionExport.ExportarTextoPlano` DEBE ser un metodo puro sobre
`ISessionStore.ObtenerTurnos()`: NO DEBE escribir nada que no viniera ya del store, y
DEBE funcionar igual sobre cualquier adaptador (`InMemorySessionStore` o
`SqliteSessionStore`). Sin turnos persistidos DEBE devolver cadena vacia.

#### Scenario: Exporta cada turno persistido en orden, uno por linea

- Dado un store con turnos de `Usuario` y `Npc` ya registrados
- Cuando se llama `SessionExport.ExportarTextoPlano(store)`
- Entonces el resultado tiene una linea `Hablante: Texto` por turno, en el mismo orden

#### Scenario: Sin turnos persistidos exporta vacio

- Dado un store recien creado sin turnos
- Cuando se llama `SessionExport.ExportarTextoPlano(store)`
- Entonces el resultado es cadena vacia

### Requirement: El unico punto que toca Unity es el sub-ensamblado adaptador

El cableado a `UtteranceChannel`/`NpcReplyChannel` y cualquier referencia a
`UnityEngine`/`Application.persistentDataPath` DEBE vivir exclusivamente en
`NpcAi.SessionLog.Unity` (`SessionLogBehaviour`), nunca en `NpcAi.SessionLog`. El
`MonoBehaviour` DEBE suscribirse en `OnEnable` y desuscribirse en `OnDisable`, y DEBE
exponer `IniciarSesion()`/`FinalizarSesion()`/`ExportarTextoPlano()` a la escena
anfitriona sin decidir por si mismo cuando arranca o termina un entrenamiento.

#### Scenario: SessionLogBehaviour delega toda la logica al nucleo puro

- Dado un `SessionLogBehaviour` habilitado con los dos canales asignados por Inspector
- Cuando el canal de `Utterance` o de `NpcReply` dispara `Raise`
- Entonces el `SessionRecorder` interno registra el turno correspondiente, y al deshabilitar el componente se desuscribe de ambos canales

## Trazabilidad (requisito -> prueba en verde)

| Requisito | Prueba(s) que ya lo demuestran |
|---|---|
| Un seam interno de persistencia, no un puerto de M0 | Inspeccion de `Runtime/Core/Ports.cs` (sin cambio) y `Runtime/SessionLog/NpcAi.SessionLog.asmdef` (referencias) |
| SessionRecorder traduce eventos del contrato a turnos, con guardas | `SessionRecorderTests` (11): `Arranca_sin_sesion_activa`, `IniciarSesion_deja_la_sesion_activa`, `RegistrarUtterance_fuera_de_sesion_activa_es_no_op`, `RegistrarRespuesta_fuera_de_sesion_activa_es_no_op`, `RegistrarUtterance_vacia_no_genera_turno`, `RegistrarRespuesta_vacia_no_genera_turno`, `Tras_FinalizarSesion_registrar_es_no_op`, `La_secuencia_crece_en_el_orden_de_llegada`, `Turno_de_usuario_no_lleva_emocion_ni_animacion`, `Turno_de_npc_copia_emocion_y_animacion`, `IniciarSesion_de_nuevo_reinicia_la_numeracion` |
| El doble y el adaptador real comparten la misma bateria de contrato | `SessionStoreContract` (4 pruebas) heredada por `InMemorySessionStoreTests` y `SqliteSessionStoreTests` |
| Un turno sobrevive a un cierre no limpio de la sesion | `SqliteSessionStoreTests.Un_turno_sobrevive_a_reabrir_el_archivo_sin_FinalizarSesion` |
| La exportacion a texto es derivable 1:1 de lo persistido | `SessionExportTests.Exporta_cada_turno_persistido_en_orden_uno_por_linea`, `...Sin_turnos_persistidos_exporta_vacio` |
| El unico punto que toca Unity es el sub-ensamblado adaptador | Inspeccion de `Runtime/SessionLog/Unity/SessionLogBehaviour.cs` y su asmdef (unicas referencias a `UnityEngine`/`NpcAi.Core.Channels` del modulo); sin prueba EditMode automatizada (mismo precedente que `SpeechToTextBehaviour` de M1) — verificacion manual pendiente (tasks.md 3.3) |
