# Especificacion: banco-de-pruebas-m11

## Purpose

Define el comportamiento observable de `SessionDirector` (`Runtime/Harness/`), la raiz de
composicion que conecta M2 (`IIntentClassifier`, alias `m2`), M4 (`IReceptivityEngine`, alias
`m4`), M6 (`IDialogueGenerator`, alias `m6`), M9 (`IScenarioObjective`, alias `m9`) y M15
(`IClinicalResponder`, alias `m15`) para un turno y para el arranque de sesion. Cubre solo el
alcance de PR1: la clase C# pura `SessionDirector`. El cableado de escena
(`Samples~/Harness/HarnessBehaviour.cs`, su README, `HarnessBehaviourWiringTests`) queda fuera y
tendra su propio addendum de spec tras el merge de PR1. `NpcAi.Core`/`NpcAi.CoreChannels` no
cambian: M11 solo consume.

## ADDED Requirements

### Requirement: Centinelas para senales parciales

`ProcesarTurno` con una `Utterance` sin `PhysicalAction` concurrente DEBE clasificar con `m2` y
evaluar con `m4` usando `PhysicalAction.Ninguna`. `ProcesarAccion` con una `PhysicalAction` sin
`Utterance` concurrente DEBE evaluar con `m4` usando `IntentResult.Unknown(...)`, sin invocar
`Classify`.

#### Scenario: Solo llega Utterance
- Dado un `SessionDirector` con sesion iniciada
- Cuando se llama `ProcesarTurno(utterance)` sin accion fisica concurrente
- Entonces `m4.Evaluate` recibe `PhysicalAction.Ninguna` como segundo argumento

#### Scenario: Solo llega PhysicalAction
- Dado un `SessionDirector` con sesion iniciada
- Cuando se llama `ProcesarAccion(action)` sin `Utterance` concurrente
- Entonces `m4.Evaluate` recibe `IntentResult.Unknown` como primer argumento y `m2.Classify` no se
  invoca

### Requirement: Silencio de la accion fisica sola

Una `PhysicalAction` que llega sola DEBE mover la receptividad via `Evaluate`, pero
`ProcesarAccion` DEBE devolver `null` y NO DEBE invocar `Respond` ni `Generate` ni producir una
`NpcReply`.

#### Scenario: La accion mueve receptividad
- Dado un `SessionDirector` con sesion iniciada y un doble local de `IReceptivityEngine` que
  mueve `Current` ante cualquier accion distinta de `Ninguna` (`ScriptedReceptivityEngine` no
  sirve aqui: su delta para `Acercarse` es cero y `Current` nunca cambiaria bajo ese doble)
- Cuando se llama `ProcesarAccion(PhysicalAction.Acercarse)`
- Entonces `m4.Current` cambia

#### Scenario: La accion no produce habla
- Dado un `SessionDirector` con sesion iniciada
- Cuando se llama `ProcesarAccion(action)` con cualquier accion distinta de `Ninguna`
- Entonces devuelve `null` y ni `m15.Respond` ni `m6.Generate` se invocan

### Requirement: Enrutado clinico/social

`ProcesarTurno` DEBE llamar primero `m15.Respond(utterance, intent)`. Con `Handled == true` DEBE
devolver `clin.Reply` sin alterarlo y NO DEBE llamar `m6.Generate`. Con `Handled == false` DEBE
llamar `m6.Generate(personalidadActual, m4.Current, intent)` y devolver ese resultado.

#### Scenario: Turno clinico manejado
- Dado un `ScriptedClinicalResponder` con `Handled == true`
- Cuando se llama `ProcesarTurno(utterance)`
- Entonces la respuesta es exactamente `clin.Reply` y `Generate` no se invoca

#### Scenario: Turno social (no clinico)
- Dado un `ScriptedClinicalResponder` con `Handled == false`
- Cuando se llama `ProcesarTurno(utterance)`
- Entonces `Generate` se invoca con `m4.Current` como receptividad y su resultado se devuelve

### Requirement: Secuencia de inicio de sesion

`IniciarSesion(caso, personalidad)` DEBE disparar juntos `m4.Reset(personalidad)`,
`m9.AssignCase(caso)` y `m15.AssignCase(caso, personalidad)`, con el mismo `ClinicalCaseId` en M9
y M15. Divergir ese caso entre M9 y M15 esta prohibido.

#### Scenario: Los tres efectos se disparan con el mismo caso
- Dado dobles espia para M4, M9 y M15
- Cuando se llama `IniciarSesion(caso, personalidad)`
- Entonces los tres se invocan y el `ClinicalCaseId` recibido por M9 y por M15 es identico

### Requirement: Seleccion reproducible por semilla

Con el mismo catalogo y semilla, dos `SessionDirector` DEBEN elegir el mismo par `(caso,
personalidad)`. Una sesion nueva NO DEBE repetir el `ClinicalCaseId` inmediatamente anterior. La
seleccion DEBE usar `System.Random`, nunca `UnityEngine.Random`.

#### Scenario: Misma semilla y catalogo repiten el par
- Dado dos `SessionDirector` con el mismo catalogo y la misma semilla
- Cuando ambos seleccionan sesion
- Entonces el par elegido es identico

#### Scenario: No se repite el caso inmediatamente anterior
- Dado un `SessionDirector` cuya sesion anterior eligio `caso-01`
- Cuando inicia una sesion nueva con el mismo catalogo y semilla
- Entonces el caso elegido es distinto de `caso-01`

### Requirement: Fronteras que SessionDirector no cruza

`SessionDirector` NO DEBE invocar `INpcPresenter.Play` en ningun turno ni sesion, y NO DEBE
invocar `RegisterRedFlag` sobre el objetivo del escenario en esta entrega.

#### Scenario: Nunca reproduce directamente
- Dado que `SessionDirector` no declara ningun parametro de constructor asignable a
  `INpcPresenter` (no tiene esa costura: `RecordingNpcPresenter` no puede estar "en la cadena de
  dobles" porque no hay cadena que lo reciba)
- Cuando se inspecciona el constructor publico de `SessionDirector`
- Entonces ningun parametro es de tipo `INpcPresenter` ni relacionado

#### Scenario: Nunca registra banderas rojas
- Dado un doble local que expone `AssignCase`/`DeclareTriage`-equivalentes por delegado y cuenta
  cuantas veces se invoca una funcion de registro de bandera roja (`ScriptedScenarioObjective` no
  sirve aqui: es `sealed` y no declara `RegisterRedFlag`)
- Cuando se ejecuta una sesion completa de turnos
- Entonces el contador permanece en cero

### Requirement: SessionDirector es C# puro

`SessionDirector` NO DEBE ser un `MonoBehaviour` y DEBE ser construible y probable enteramente en
EditMode, sin ninguna escena de Unity cargada. Su `NpcAi.Harness.asmdef` DEBE referenciar
unicamente `NpcAi.Core`.

#### Scenario: Instanciacion sin escena
- Dado los cinco puertos como dobles deterministas, sin escena Unity cargada
- Cuando se construye `SessionDirector` por su constructor publico
- Entonces la instancia opera y `ProcesarTurno`/`ProcesarAccion` responden sin lanzar

## Trazabilidad (requisito -> prueba)

| Requisito | Prueba EditMode |
|---|---|
| Centinelas para senales parciales | `SoloUtterance_evalua_con_PhysicalAction_Ninguna`, `SoloPhysicalAction_evalua_con_IntentResult_Unknown` (propuesto) |
| Silencio de la accion fisica sola | `AccionFisica_mueve_receptividad`, `AccionFisica_no_produce_respuesta_hablada` (propuesto) |
| Enrutado clinico/social | `Turno_clinico_manejado_no_llama_Generate`, `Turno_social_llama_Generate_con_receptividad_actual` (propuesto) |
| Secuencia de inicio de sesion | `IniciarSesion_dispara_Reset_AssignCase_M9_AssignCase_M15_con_mismo_caso` (propuesto) |
| Seleccion reproducible por semilla | `MismaSemillaYCatalogo_elige_mismo_par`, `SesionNueva_no_repite_caso_anterior` (propuesto) |
| Fronteras que SessionDirector no cruza | `ConstructorPublico_no_declara_ningun_parametro_INpcPresenter`, `SesionCompleta_nunca_incrementa_el_contador_de_bandera_roja_del_espia_local` (propuesto) |
| SessionDirector es C# puro | `Construible_y_operativo_sin_escena_Unity` (propuesto) |
