# Especificacion: bitacora-sesion-m13

## Purpose

Congela el alcance y las garantias de M13 (`Runtime/SessionLog/`): un modulo nuevo,
puramente aditivo, que registra turno por turno una sesion de entrenamiento (lo que dijo
el usuario y lo que respondio el NPC) suscribiendose a los dos canales que
`contrato-nucleo-m0` ya expone (`UtteranceChannel`, `NpcReplyChannel`), sin agregar
ningun puerto nuevo a `NpcAi.Core`. Persiste en SQLite local con escritura confirmada en
cada turno (no por lote, para no perder la sesion si la app se cierra a mitad de un
entrenamiento) y permite exportar lo persistido a texto plano. Cada sesion se identifica
por una etiqueta que decide quien la inicia, y todas las sesiones conviven indefinidamente
en el store — no hay borrado al iniciar una sesion nueva (revision 2026-09-11: reemplaza la
garantia original de "reinicio limpio" por historial multi-sesion).

El puerto `IReceptivityEngine`/`IDialogueGenerator`, los DTO `Utterance`/`NpcReply` y los
canales de evento los posee `contrato-nucleo-m0`/`canales-evento-nucleo-m0` y NO se
redefinen aqui. El mecanismo de verificacion son las pruebas EditMode de
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
DEBE borrar los turnos ya registrados.

#### Scenario: Paridad de comportamiento entre doble y real

- Dado `InMemorySessionStoreTests` y `SqliteSessionStoreTests`, ambas heredando `SessionStoreContract`
- Cuando corre el Test Runner EditMode
- Entonces todas las pruebas de la base pasan igual en las dos clases

### Requirement: Cada etiqueta de sesion persiste independientemente (historial multi-sesion)

`IniciarSesion(string etiqueta)` NO DEBE borrar turnos de ninguna sesion anterior,
identificada por una etiqueta distinta o por la misma. `ObtenerTurnos()` (sin argumento)
DEBE seguir devolviendo unicamente los turnos de la sesion **activa** — equivalente a
`ObtenerTurnosDeSesion(etiquetaActiva)`. Reusar una etiqueta ya usada DEBE acumular los
turnos nuevos a continuacion de los anteriores bajo esa misma etiqueta (no hay
desambiguacion automatica: es responsabilidad de quien llama elegir etiquetas unicas si
quiere sesiones separadas). Una etiqueta `null`, vacia o solo espacios DEBE normalizarse a
un valor por defecto determinista, sin lanzar.

#### Scenario: Sesiones con etiquetas distintas no mezclan turnos

- Dado un `ISessionStore` con una sesion "sesion-a" y turnos registrados
- Cuando se llama `IniciarSesion("sesion-b")` y se registran turnos nuevos
- Entonces `ObtenerTurnos()` solo muestra los turnos de "sesion-b", y `ObtenerTurnosDeSesion("sesion-a")` sigue mostrando los suyos intactos

#### Scenario: Reusar la misma etiqueta acumula en vez de borrar

- Dado una sesion "sesion-a" con un turno ya registrado y cerrada con `FinalizarSesion()`
- Cuando se llama `IniciarSesion("sesion-a")` de nuevo y se registra un turno mas
- Entonces `ObtenerTurnosDeSesion("sesion-a")` tiene los dos turnos, en orden

#### Scenario: Etiqueta invalida cae a un valor por defecto

- Dado un `ISessionStore` recien creado
- Cuando se llama `IniciarSesion(null)` o `IniciarSesion("   ")`
- Entonces no lanza y la sesion queda registrada con una etiqueta por defecto

### Requirement: El historial de sesiones es consultable sin tocar SQL a mano

`ListarSesiones()` DEBE devolver el catalogo de todas las etiquetas de sesion conocidas,
mas reciente primero (por fecha de inicio). `ObtenerTurnosDeSesion(string etiqueta)` DEBE
devolver los turnos de esa sesion especifica — activa o pasada — sin verse afectado por
cual sea la sesion activa en el momento de la consulta. Una etiqueta desconocida DEBE
devolver una lista vacia, sin lanzar.

#### Scenario: Listar sesiones trae todas las iniciadas, mas reciente primero

- Dado que se llamo `IniciarSesion("primera")` y luego `IniciarSesion("segunda")`
- Cuando se llama `ListarSesiones()`
- Entonces el resultado tiene 2 elementos, con "segunda" antes que "primera"

#### Scenario: Consultar una sesion pasada no depende de la sesion activa

- Dado una sesion "sesion-a" con turnos, cerrada, y luego "sesion-b" activa con sus propios turnos
- Cuando se llama `ObtenerTurnosDeSesion("sesion-a")`
- Entonces devuelve los turnos de "sesion-a" sin que importe que "sesion-b" este activa

### Requirement: La sesion mas reciente se retoma sola tras un cierre no limpio

`SessionLogBehaviour` DEBE llamar `SessionRecorder.ReanudarUltimaSesion()` en `OnEnable`,
sin depender de que la escena anfitriona vuelva a proveer la etiqueta correcta.
`ReanudarUltimaSesion()` DEBE activar la etiqueta mas reciente segun `ListarSesiones()` (si
existe alguna) y devolver si retomo algo; sin ninguna sesion previa DEBE ser no-op
(`SesionActiva` sigue en `false`). Al retomar una etiqueta con turnos existentes,
`IniciarSesion` NO DEBE reiniciar la numeracion de `Secuencia` a 0: DEBE continuar desde la
cantidad de turnos ya registrados bajo esa etiqueta. `SessionRecorder.EtiquetaActiva` DEBE
exponer la etiqueta activa (vacia si no hay sesion), para que un consumidor pueda mostrar
cual sesion se esta retomando.

#### Scenario: Reanudar la ultima sesion permite seguir registrando sin perder lo anterior

- Dado un `SessionRecorder` que registro turnos bajo "sesion-vieja" sin llamar `FinalizarSesion`
- Cuando se crea un `SessionRecorder` nuevo sobre el mismo store y se llama `ReanudarUltimaSesion()`
- Entonces devuelve `true`, `EtiquetaActiva` es "sesion-vieja", y un turno registrado despues queda junto a los de antes, en orden

#### Scenario: Sin sesiones previas, reanudar no activa nada

- Dado un store recien creado sin ninguna sesion iniciada
- Cuando se llama `ReanudarUltimaSesion()`
- Entonces devuelve `false` y `SesionActiva` sigue `false`

#### Scenario: Retomar una etiqueta continua la numeracion en vez de reiniciarla

- Dado una etiqueta con 2 turnos ya registrados
- Cuando se llama `IniciarSesion` de nuevo con esa misma etiqueta y se registra un turno mas
- Entonces el turno nuevo queda con `Secuencia` 2, no 0

### Requirement: Un turno sobrevive a un cierre no limpio de la sesion

`SqliteSessionStore.RegistrarTurno` DEBE confirmar cada turno en disco de inmediato (sin
transaccion abierta pendiente de commit). Si el proceso termina sin llamar
`FinalizarSesion()`, reabrir un `SqliteSessionStore` sobre el mismo archivo DEBE mostrar
el turno ya escrito en `ObtenerTurnos()`.

#### Scenario: Reabrir el archivo sin FinalizarSesion conserva el turno

- Dado un `SqliteSessionStore` con `IniciarSesion("sesion-de-prueba")` y un turno registrado, cerrado sin llamar `FinalizarSesion()`
- Cuando se crea un `SqliteSessionStore` nuevo sobre el mismo archivo
- Entonces `ObtenerTurnosDeSesion("sesion-de-prueba")` devuelve ese turno

### Requirement: La exportacion a texto es derivable 1:1 de lo persistido

`SessionExport.ExportarTextoPlano` DEBE ser un metodo puro sobre
`ISessionStore.ObtenerTurnos()` (sesion activa) o, en su overload de dos argumentos, sobre
`ISessionStore.ObtenerTurnosDeSesion(etiqueta)` (una sesion especifica): NO DEBE escribir
nada que no viniera ya del store, y DEBE funcionar igual sobre cualquier adaptador
(`InMemorySessionStore` o `SqliteSessionStore`). Sin turnos persistidos DEBE devolver
cadena vacia.

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
exponer `IniciarSesion(string etiqueta)`/`FinalizarSesion()`/`ExportarTextoPlano()`/
`ListarSesiones()`/`ObtenerTurnosDeSesion(string)` a la escena anfitriona sin decidir por
si mismo cuando arranca o termina un entrenamiento, ni que etiqueta usar.

#### Scenario: SessionLogBehaviour delega toda la logica al nucleo puro

- Dado un `SessionLogBehaviour` habilitado con los dos canales asignados por Inspector
- Cuando el canal de `Utterance` o de `NpcReply` dispara `Raise`
- Entonces el `SessionRecorder` interno registra el turno correspondiente, y al deshabilitar el componente se desuscribe de ambos canales

## Trazabilidad (requisito -> prueba en verde)

| Requisito | Prueba(s) que ya lo demuestran |
|---|---|
| Un seam interno de persistencia, no un puerto de M0 | Inspeccion de `Runtime/Core/Ports.cs` (sin cambio) y `Runtime/SessionLog/NpcAi.SessionLog.asmdef` (referencias) |
| SessionRecorder traduce eventos del contrato a turnos, con guardas | `SessionRecorderTests`: `Arranca_sin_sesion_activa`, `IniciarSesion_deja_la_sesion_activa`, `RegistrarUtterance_fuera_de_sesion_activa_es_no_op`, `RegistrarRespuesta_fuera_de_sesion_activa_es_no_op`, `RegistrarUtterance_vacia_no_genera_turno`, `RegistrarRespuesta_vacia_no_genera_turno`, `Tras_FinalizarSesion_registrar_es_no_op`, `La_secuencia_crece_en_el_orden_de_llegada`, `Turno_de_usuario_no_lleva_emocion_ni_animacion`, `Turno_de_npc_copia_emocion_y_animacion`, `IniciarSesion_de_nuevo_con_otra_etiqueta_reinicia_la_numeracion` |
| El doble y el adaptador real comparten la misma bateria de contrato | `SessionStoreContract` heredada por `InMemorySessionStoreTests` y `SqliteSessionStoreTests` |
| Cada etiqueta de sesion persiste independientemente (historial multi-sesion) | `SessionStoreContract.Sesiones_distintas_no_mezclan_turnos`, `...Reusar_la_misma_etiqueta_acumula_turnos_en_vez_de_borrar`, `...Etiqueta_null_o_vacia_usa_el_valor_por_defecto`; `SessionRecorderTests.ObtenerTurnosDeSesion_trae_una_sesion_pasada_sin_afectar_la_activa` |
| El historial de sesiones es consultable sin tocar SQL a mano | `SessionStoreContract.ListarSesiones_incluye_todas_las_iniciadas_mas_reciente_primero`, `...ObtenerTurnosDeSesion_no_se_ve_afectado_por_cambiar_de_sesion_activa`; `SessionRecorderTests.ListarSesiones_refleja_las_sesiones_que_paso_por_el_recorder` |
| La sesion mas reciente se retoma sola tras un cierre no limpio | `SessionRecorderTests.ReanudarUltimaSesion_retoma_la_etiqueta_mas_reciente_y_permite_seguir_registrando`, `...ReanudarUltimaSesion_sin_sesiones_previas_no_activa_nada`, `...IniciarSesion_con_etiqueta_ya_usada_continua_la_numeracion_en_vez_de_reiniciar` |
| Un turno sobrevive a un cierre no limpio de la sesion | `SqliteSessionStoreTests.Un_turno_sobrevive_a_reabrir_el_archivo_sin_FinalizarSesion` |
| La exportacion a texto es derivable 1:1 de lo persistido | `SessionExportTests.Exporta_cada_turno_persistido_en_orden_uno_por_linea`, `...Sin_turnos_persistidos_exporta_vacio`, `...Exporta_una_sesion_pasada_sin_afectar_la_sesion_activa` |
| El unico punto que toca Unity es el sub-ensamblado adaptador | Inspeccion de `Runtime/SessionLog/Unity/SessionLogBehaviour.cs` y su asmdef (unicas referencias a `UnityEngine`/`NpcAi.Core.Channels` del modulo); sin prueba EditMode automatizada (mismo precedente que `SpeechToTextBehaviour` de M1) — verificacion manual pendiente (tasks.md 3.3) |
