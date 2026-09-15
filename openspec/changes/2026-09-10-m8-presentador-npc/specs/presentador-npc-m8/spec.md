# Especificacion: presentador-npc-m8

## Purpose

Congela el alcance y las garantias del presentador real de M8 (`Runtime/Presentation/`): un
`NpcPresenter` interno detras del puerto `INpcPresenter` (propiedad de `contrato-nucleo-m0`, que
NO cambia), envuelto por `NpcPresenterBehaviour : MonoBehaviour` para cablear un `AudioSource`, un
`Animator` y un `NpcReplyChannel` de escena. La sintesis de voz es una costura interna
(`ISpeechSynthesizer`) para que el nucleo compile y pase su contrato en EditMode sin motor nativo
ni voz cargada; la implementacion real (`PiperSpeechSynthesizer`, sobre `libpiper` via P/Invoke)
llega detras de esa costura sin tocarla. El aprovisionamiento de blobs comprimidos a
`Application.persistentDataPath` (`VoiceProvisioner`, voces Piper y datos de fonemizacion de
espeak-ng) sigue el mismo mecanismo que `SpeechModelProvisioner` de M1.

El mecanismo de verificacion son las 32 pruebas EditMode de `Tests/EditMode/Presentation/`
(`NpcPresenterTests : NpcPresenterContract`, `RecordingAnimationDriverTests`, `MainThreadPumpTests`,
`AnimatorDriverTests`, `PresentationSettingsAssetTests`, `NpcPresenterBehaviourTests`,
`VoiceProvisionerTests`) mas la base `NpcPresenterContract` que ya pasaba `RecordingNpcPresenter`.
La ruta del motor Piper real (P/Invoke a `libpiper`, GPL-3.0 aceptado explicitamente por decision
de gobernanza del proyecto — ver `design.md`, Decision 10) NO es automatizable en EditMode sin el
binario nativo cargado: se verifico con una prueba manual en el Editor de escritorio (audio real en
espanol, 20 repeticiones sin fugas ni bloqueo del hilo principal, confirmada por el usuario el
2026-09-14). Este documento formaliza y fija M8 ya en verde; no lo reescribe.

## ADDED Requirements

### Requirement: Un presentador nuevo detras de un puerto que no cambia

M8 DEBE entregar `NpcPresenter : INpcPresenter` (interno a `NpcAi.Presentation`) sin cambiar
ninguna firma de `INpcPresenter` ni de `NpcReply` (propiedad de `contrato-nucleo-m0`); no es un
cambio de M0. `Runtime/Presentation/Fakes/RecordingNpcPresenter.cs` DEBE seguir existiendo sin
cambios como doble determinista de referencia para M11. `NpcPresenterTests` DEBE heredar
`NpcPresenterContract` sin modificar la clase base, y DEBE construirse sin `UnityEngine` de escena
(sin `AudioSource`, sin `Animator`).

#### Scenario: El presentador real hereda el mismo contrato que el doble

- Dado `NpcPresenterTests : NpcPresenterContract` con `CreateSubject()` que arma `NpcPresenter` con dobles y `ImmediateMainThreadPump`
- Cuando se corre la bateria heredada en el Test Runner EditMode
- Entonces pasan `Reproduce_una_respuesta_normal_sin_lanzar`, `Sobrevive_a_una_respuesta_vacia` y `Sobrevive_a_reproducciones_encadenadas`, y `RecordingNpcPresenterTests` sigue en verde

### Requirement: Play(default) y texto vacio son no-op total, sin lanzar

Si `reply.IsEmpty` (texto `null`, vacio o solo espacios), `Play` NO DEBE encolar sintesis ni
animacion, y NO DEBE invocar `ISpeechSynthesizer.Sintetizar`. `Play` NUNCA DEBE lanzar, incluida la
llamada con `default(NpcReply)`.

#### Scenario: Play(default) no invoca al sintetizador ni lanza

- Dado un `NpcPresenter` con un `ISpeechSynthesizer` doble que cuenta invocaciones
- Cuando se llama `Play(default)`, `Play(new NpcReply(null, ..., ...))` y `Play(new NpcReply("   ", ..., ...))`
- Entonces el contador de invocaciones del doble queda en cero y ninguna llamada lanza

### Requirement: Un reply valido sintetiza y anima exactamente una vez

Un `NpcReply` con texto no vacio DEBE producir exactamente una llamada a
`ISpeechSynthesizer.Sintetizar` y, tras la entrega en el hilo principal, exactamente una llamada a
`IAnimationDriver.Aplicar` con el `EmotionTag`/`AnimationCue` de ese reply. Diez llamadas
encadenadas a `Play` con replies validos DEBEN producir diez sintesis, en el mismo orden.

#### Scenario: Un reply valido sintetiza una vez y dispara la animacion

- Dado un `NpcPresenter` con dobles y bomba inmediata
- Cuando se llama `Play(new NpcReply("hola", "neutral", "idle"))`
- Entonces el sintetizador se invoco una vez y el driver de animacion recibio `("neutral", "idle")`

#### Scenario: Diez llamadas encadenadas sintetizan diez veces en orden

- Dado un `NpcPresenter` con dobles y bomba inmediata
- Cuando se llama `Play` diez veces seguidas con replies validos distintos
- Entonces el sintetizador registro diez invocaciones en el mismo orden de llegada

### Requirement: La sintesis corre fuera del hilo principal; la entrega espera al drenaje de la bomba

`Play` DEBE despachar la sintesis a un trabajador (no bloquea al llamador) y, al terminar, DEBE
entregar el resultado al hilo principal via `IMainThreadPump.Post`. Con una `QueuedMainThreadPump`,
NO DEBE haber animacion ni reproduccion de audio hasta que se llame `Drenar()`.

#### Scenario: Con la bomba encolada no hay animacion hasta drenar

- Dado un `NpcPresenter` con `QueuedMainThreadPump` y despacho de sintesis en linea
- Cuando se llama `Play` con un reply valido y todavia no se llamo `Drenar()`
- Entonces el driver de animacion no recibio ninguna llamada; tras `Drenar()`, la recibe

### Requirement: El PCM vacio no reproduce audio pero si anima

Si `ISpeechSynthesizer.Sintetizar` devuelve un arreglo vacio (sintesis fallida o costura sin voz),
`NpcPresenter` NO DEBE invocar el callback de reproduccion de audio, pero SI DEBE invocar
`IAnimationDriver.Aplicar` igual: la animacion no depende de que haya audio.

#### Scenario: PCM no vacio se entrega al callback de audio con la tasa de la config

- Dado un `NpcPresenter` cuyo sintetizador doble devuelve PCM no vacio
- Cuando se llama `Play` con un reply valido
- Entonces el callback de audio recibe ese PCM y la tasa de muestreo configurada

#### Scenario: PCM vacio no llama al callback de audio pero si a la animacion

- Dado un `NpcPresenter` cuyo sintetizador doble devuelve `Array.Empty<float>()`
- Cuando se llama `Play` con un reply valido
- Entonces el callback de audio no se invoca, pero el driver de animacion si recibe la llamada

### Requirement: La animacion es un mapa de datos: cue desconocido, sin Animator o sin controller es no-op

`AnimatorDriver.Aplicar` DEBE traducir un `EmotionTag`/`AnimationCue` conocido a `SetTrigger` o
`SetBool(true)` segun el mapa de la configuracion. Un cue fuera del mapa, un `Animator` nulo, o un
`Animator` sin `runtimeAnimatorController` asignado DEBEN resultar en no-op, sin lanzar.

#### Scenario: Sin Animator no lanza

- Dado un `AnimatorDriver` construido con `Animator` nulo
- Cuando se llama `Aplicar` con cualquier tag/cue
- Entonces no lanza y no hay efecto observable

#### Scenario: Un cue fuera del mapa es no-op sin lanzar

- Dado un `AnimatorDriver` con un mapa que no contiene el cue recibido
- Cuando se llama `Aplicar` con ese cue
- Entonces no lanza y no se llama `SetTrigger` ni `SetBool`

#### Scenario: Un Animator sin controller es no-op sin lanzar

- Dado un `AnimatorDriver` con un `Animator` real pero sin `runtimeAnimatorController`
- Cuando se llama `Aplicar` con un cue presente en el mapa
- Entonces no lanza y no se llama `SetTrigger` ni `SetBool`

### Requirement: Suscripcion al canal sin fugas

`NpcPresenterBehaviour` DEBE suscribirse a su `NpcReplyChannel` en `OnEnable` y desuscribirse en
`OnDisable`. Tras `OnDisable`, un `Raise` posterior del canal NO DEBE incrementar el contador de
replies recibidos. Si no hay canal asignado, cablear y descablear NO DEBEN lanzar.

#### Scenario: OnEnable suscribe y OnDisable desuscribe sin fugas

- Dado un `NpcPresenterBehaviour` cableado con un `NpcReplyChannel`
- Cuando se hace `Raise` antes y despues de `OnDisable`
- Entonces solo el `Raise` anterior a `OnDisable` incrementa `RepliesRecibidos`

#### Scenario: Cablear sin canal asignado no lanza

- Dado un `NpcPresenterBehaviour` sin `NpcReplyChannel` asignado
- Cuando se llama `CablearParaPrueba` y `DescablearParaPrueba`
- Entonces ninguna de las dos llamadas lanza

### Requirement: La configuracion de presentacion es dato, acotado en OnValidate

`PresentationSettingsAsset.OnValidate` DEBE acotar `TasaDeMuestreo` a `[8000, 48000]` y `Velocidad`
a `[0.5, 2]`. `ToSettings` DEBE copiar los escalares y armar los mapas de voces y cues a partir de
los arreglos serializables, ignorando entradas cuya clave (`vozId`, `cue`/`parametro`) este vacia.
`PresentationSettings.ResolverIdDeVoz` DEBE devolver el id mapeado para un `vozId` conocido, y
`VozPorDefecto` para cualquier `vozId` vacio, nulo o ausente del mapa.

#### Scenario: TasaDeMuestreo y Velocidad se acotan

- Dado un `PresentationSettingsAsset` con `TasaDeMuestreo` y `Velocidad` fuera de rango
- Cuando se llama `OnValidate`
- Entonces `TasaDeMuestreo` queda en `[8000, 48000]` y `Velocidad` en `[0.5, 2]`

#### Scenario: ToSettings arma los mapas e ignora entradas incompletas

- Dado un `PresentationSettingsAsset` con entradas de `Voces`/`Cues` validas e incompletas mezcladas
- Cuando se llama `ToSettings`
- Entonces los mapas resultantes solo contienen las entradas completas

#### Scenario: ResolverIdDeVoz cae al valor por defecto

- Dado una `PresentationSettings` con un mapa de voces y un `VozPorDefecto`
- Cuando se llama `ResolverIdDeVoz` con un `vozId` vacio, nulo o ausente del mapa
- Entonces devuelve `VozPorDefecto`

### Requirement: El aprovisionamiento de blobs comprimidos es seguro e idempotente

`VoiceProvisioner.Aprovisionar` DEBE descomprimir un blob (`byte[]`) dentro de
`<raizDestino>/<idDeBlob>/`, usando `ZipArchive` sobre `MemoryStream` (nunca
`ZipFile.ExtractToDirectory`). DEBE rechazar con excepcion cualquier entrada cuya ruta resuelta se
salga de esa raiz (proteccion contra path traversal), sin escribir nada fuera de ella. DEBE ser
idempotente: si ya existe un centinela `.listo` con el mismo `idDeBlob`, una segunda llamada NO
DEBE volver a tocar el disco, incluso si el blob recibido es invalido.

#### Scenario: Extrae los archivos del zip en memoria

- Dado un blob zip valido con una entrada de archivo
- Cuando se llama `Aprovisionar` con ese blob y una raiz de destino vacia
- Entonces el archivo aparece en `<raiz>/<idDeBlob>/` con su contenido original

#### Scenario: No vuelve a extraer si ya esta aprovisionado

- Dado un blob ya aprovisionado (centinela `.listo` presente) cuyo archivo extraido se borro despues
- Cuando se llama `Aprovisionar` de nuevo con el mismo `idDeBlob`, pasando un blob invalido
- Entonces no lanza, devuelve la misma ruta, y el archivo sigue sin existir (no volvio a extraer)

#### Scenario: Rechaza una entrada que intenta salir de la raiz

- Dado un blob zip con una entrada cuya ruta es `../evil.txt`
- Cuando se llama `Aprovisionar` con ese blob
- Entonces lanza `InvalidOperationException` y no se crea ningun archivo fuera de la raiz de destino

## Trazabilidad (requisito -> prueba en verde)

| Requisito | Prueba(s) que ya lo demuestran |
|---|---|
| Un presentador nuevo detras de un puerto que no cambia | `NpcPresenterContract.Reproduce_una_respuesta_normal_sin_lanzar`, `...Sobrevive_a_reproducciones_encadenadas` (heredadas por `NpcPresenterTests`); `RecordingNpcPresenterTests` sigue en verde; inspeccion del diff (no toca `Runtime/Core/`) |
| Play(default) y texto vacio son no-op total, sin lanzar | `NpcPresenterContract.Sobrevive_a_una_respuesta_vacia` (heredado); `NpcPresenterTests.Un_reply_vacio_no_invoca_al_sintetizador_ni_lanza` |
| Un reply valido sintetiza y anima exactamente una vez | `NpcPresenterTests.Un_reply_con_texto_sintetiza_una_vez_y_dispara_la_animacion`, `...Diez_plays_encadenados_sintetizan_diez_veces_en_orden` |
| La sintesis corre fuera del hilo principal; la entrega espera al drenaje de la bomba | `NpcPresenterTests.Con_la_bomba_encolada_no_hay_animacion_hasta_drenar` |
| El PCM vacio no reproduce audio pero si anima | `NpcPresenterTests.El_pcm_no_vacio_se_entrega_al_callback_de_audio_con_la_tasa_de_la_config`, `...El_pcm_vacio_no_llama_al_callback_de_audio_pero_si_a_la_animacion` |
| La animacion es un mapa de datos: cue desconocido, sin Animator o sin controller es no-op | `AnimatorDriverTests.Sin_animator_no_lanza`, `...Con_mapa_nulo_o_tags_nulos_no_lanza`, `...Con_animator_sin_controller_es_no_op_y_no_lanza`; `RecordingAnimationDriverTests.Registra_cada_llamada_en_orden`, `...Tolera_tags_nulos_sin_lanzar` |
| Suscripcion al canal sin fugas | `NpcPresenterBehaviourTests.OnEnable_suscribe_al_canal_y_OnDisable_lo_desuscribe`, `...Cablear_sin_canal_asignado_no_lanza` |
| La configuracion de presentacion es dato, acotado en OnValidate | `PresentationSettingsAssetTests.TasaDeMuestreo_se_acota_entre_8000_y_48000`, `...Velocidad_se_acota_entre_0_5_y_2`, `...ToSettings_copia_escalares_y_arma_los_mapas`, `...ToSettings_ignora_entradas_incompletas` |
| El aprovisionamiento de blobs comprimidos es seguro e idempotente | `VoiceProvisionerTests.Aprovisionar_extrae_los_archivos_del_zip_en_memoria`, `...Aprovisionar_no_vuelve_a_extraer_en_la_segunda_llamada`, `...Aprovisionar_rechaza_una_entrada_que_intenta_salir_de_la_raiz` |
| Motor Piper real (`PiperSpeechSynthesizer` sobre `libpiper`) con degradacion segura a `SilentSpeechSynthesizer` sin voz configurada | No automatizable en EditMode sin el binario nativo cargado; verificado con prueba manual en el Editor de escritorio (audio real en espanol, 20 repeticiones sin fugas ni bloqueo del hilo principal), confirmada por el usuario el 2026-09-14 (tasks.md 3.7) |
