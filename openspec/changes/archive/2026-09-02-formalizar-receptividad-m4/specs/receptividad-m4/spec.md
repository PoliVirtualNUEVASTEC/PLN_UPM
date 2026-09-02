# Especificacion: receptividad-m4

## Purpose

Congela los invariantes del motor real de M4 (`Runtime/Receptivity/`): como `ReceptivityEngine`
realiza el puerto `IReceptivityEngine`, como se parametriza por perfiles de personalidad, y que
garantias comparte con su doble `ScriptedReceptivityEngine`. El puerto en si lo posee
`contrato-nucleo-m0` y NO se redefine aqui; esta capacidad documenta la implementacion, no el
contrato. El mecanismo de verificacion son las pruebas EditMode de `Tests/EditMode/Receptivity/`
y el contrato heredado `ReceptivityEngineContract`. Documenta y fija el M4 ya existente y en
verde; no lo reescribe.

## ADDED Requirements

### Requirement: Motor real parametrizado por perfil

`ReceptivityEngine` DEBE implementar `IReceptivityEngine` usando unicamente `NpcAi.Core`
(ensamblado `NpcAi.Receptivity` con `noEngineReferences: true`). El estado `Current` DEBE
derivarse siempre del puntaje acumulado contra los umbrales del perfil activo
(`puntaje >= UmbralReceptivo` -> `Receptivo`; `puntaje <= UmbralNoReceptivo` -> `NoReceptivo`;
en otro caso `Neutral`). NO DEBE existir un estado inicial almacenado aparte del puntaje.
`Reset(personality)` DEBE tomar el perfil del catalogo, sembrar el puntaje con `PuntajeInicial`
del perfil (saturado) y derivar `Current`.

#### Scenario: El estado sale del puntaje contra los umbrales

- Dado un motor con el catalogo `Standard()`
- Cuando se llama `Reset(new PersonalityId("grosero"))` y luego `Reset(new PersonalityId("empatico"))`
- Entonces `grosero` arranca `NoReceptivo` y `empatico` arranca `Neutral`, cada uno segun su `PuntajeInicial` frente a sus umbrales

#### Scenario: Reset determinista e idempotente por personalidad

- Dado dos motores recien creados
- Cuando ambos hacen `Reset(pid)` con la misma personalidad, incluso repetido
- Entonces `Current` coincide entre los dos y no cambia entre `Reset` repetidos

### Requirement: Blindaje de la monotonia del contrato

Al evaluar, tras calcular el delta bruto como
`DeltaPorIntencion(intent.Intent) + DeltaPorTono(intent.Tone) + DeltaPorAccion(action)`,
`ReceptivityEngine` DEBE anular cualquier delta que contradiga el contrato de M0: con
`Intent.SolicitudAgresiva` un delta `> 0` DEBE volverse `0`; con `Intent.Empatia` un delta
`< 0` DEBE volverse `0`. Esta garantia NO DEBE depender de que los numeros del perfil sean
coherentes: un perfil mal tuneado NO DEBE poder violar la monotonia.

#### Scenario: Agresion sostenida nunca mejora la receptividad

- Dado un motor reiniciado con estado inicial `I`
- Cuando se llama 5 veces `Evaluate` con `Intent.SolicitudAgresiva` y `Tone.Agresivo`
- Entonces `(int)Current <= (int)I`

#### Scenario: Empatia sostenida nunca empeora la receptividad

- Dado un motor reiniciado con estado inicial `I`
- Cuando se llama 5 veces `Evaluate` con `Intent.Empatia` y `Tone.Empatico`
- Entonces `(int)Current >= (int)I`

### Requirement: Saturacion simetrica del puntaje

El puntaje acumulado DEBE mantenerse dentro de `[-LimitePuntaje, +LimitePuntaje]`, tomando el
valor absoluto de `LimitePuntaje`. `LimitePuntaje` se guarda tal cual lo entrega el productor
del perfil; el contenedor NO DEBE validarlo ni recortarlo.

#### Scenario: El puntaje se estanca en el limite del perfil

- Dado un motor con un perfil de `LimitePuntaje == 4`
- Cuando se llama `Evaluate` 12 veces en la misma direccion
- Entonces `Score` nunca sale de `[-4, 4]`

### Requirement: El tono modula el resultado

A diferencia del doble, `ReceptivityEngine` DEBE sumar `DeltaPorTono(intent.Tone)` al delta
bruto. La misma intencion con distinto `Tone` DEBE poder dejar distinto `Score`.

#### Scenario: Misma peticion, distinto tono, distinto puntaje

- Dado dos motores con la misma personalidad
- Cuando uno evalua `Intent.SolicitudRespetuosa` con `Tone.Respetuoso` y el otro con `Tone.Agresivo`
- Entonces el `Score` del primero es mayor que el del segundo

### Requirement: Perfil como contenedor de datos C# puro

`ReceptivityProfile` DEBE ser una clase inmutable sin referencia a `UnityEngine`, con
`UmbralReceptivo`, `UmbralNoReceptivo`, `LimitePuntaje`, `PuntajeInicial` y los accesores
`DeltaPorIntencion`, `DeltaPorTono` y `DeltaPorAccion`. Cada accesor DEBE devolver `0` (NO DEBE
lanzar) cuando la personalidad no puntua esa clave. `ReceptivityProfile.Default` DEBE existir y
DEBE puntuar la agresion en negativo y la empatia en positivo, para que el motor nunca opere sin
un perfil coherente.

#### Scenario: Un accesor sin tabla devuelve cero

- Dado `new ReceptivityProfile(...)` sin entrada para `Intent.Interrupcion`
- Cuando se llama `DeltaPorIntencion(Intent.Interrupcion)`
- Entonces devuelve `0` y no lanza

#### Scenario: El perfil Default es conservador

- Dado `ReceptivityProfile.Default`
- Cuando se consultan `DeltaPorIntencion(Intent.SolicitudAgresiva)` y `DeltaPorIntencion(Intent.Empatia)`
- Entonces el primero es negativo y el segundo positivo

### Requirement: Catalogo PersonalityId -> perfil, inyectable

`ReceptivityProfileCatalog` DEBE resolver un `PersonalityId` a su `ReceptivityProfile`,
devolviendo `ReceptivityProfile.Default` si no lo conoce, sin lanzar. DEBE aceptar el
diccionario de perfiles por constructor: esa es la costura por la que M5 inyectara sus datos
reales. `Standard()` DEBE armar las 4 personalidades del alcance de M5 (`grosero`, `histerico`,
`introvertido`, `empatico`) con numeros placeholder. El numero de personalidades es dato de M5 y
NUNCA un cambio de contrato de M4.

#### Scenario: Id desconocido cae en Default sin lanzar

- Dado un catalogo `Standard()`
- Cuando se llama `PerfilDe(new PersonalityId("no-existe"))`
- Entonces devuelve `ReceptivityProfile.Default` y no lanza

#### Scenario: El motor usa el catalogo inyectado

- Dado `new ReceptivityEngine(catalogo)` con un `catalogo` que define solo `"robot"` con umbral receptivo `+1`
- Cuando se hace `Reset(new PersonalityId("robot"))` y una `Evaluate` que suma `+5`
- Entonces manda el umbral del perfil inyectado, no el de `Standard()`

### Requirement: ReasonCode con vocabulario compartido con el doble

`ReceptivityEngine` y `ScriptedReceptivityEngine` DEBEN emitir el mismo `ReasonCode` para un par
`(Intent, PhysicalAction)` dado, con esta prioridad: primero la intencion, luego la accion
fisica, y si ninguna aplica `SIN_CAMBIO_RELEVANTE` cuando el delta es `0` o `AJUSTE_MENOR` en
otro caso. El `Score` PUEDE diferir entre las dos implementaciones; el `ReasonCode` NO DEBE
diferir. El vocabulario completo son 15 codigos:

| Origen | Codigos |
|---|---|
| Intencion | `AGRESION_DIRECTA`, `INTERRUPCION`, `FUERA_DE_TEMA`, `GESTO_EMPATICO`, `PETICION_RESPETUOSA`, `APORTA_INFORMACION` |
| Accion fisica | `GESTO_CALMA`, `DISTANCIAMIENTO`, `ACERCAMIENTO`, `CONTACTO_VISUAL`, `CONTACTO_FISICO`, `ENTREGA_OBJETO`, `SENALA_PANTALLA` |
| Fallback | `SIN_CAMBIO_RELEVANTE` (delta `0`), `AJUSTE_MENOR` (delta distinto de `0`) |

`ReasonCode` es diagnostico: los consumidores NO DEBEN ramificar su logica segun su contenido.

#### Scenario: El doble y el motor real reportan la misma razon

- Dado un `ScriptedReceptivityEngine` y un `ReceptivityEngine`, ambos tras `Reset` con la misma personalidad
- Cuando se evalua el mismo `(Intent, PhysicalAction)` en los dos, para cada combinacion posible
- Entonces el `ReasonCode` coincide entre las dos implementaciones

#### Scenario: Entrada sin efecto especifico cae en el fallback neutro

- Dado cualquiera de las dos implementaciones tras `Reset`
- Cuando se evalua `(Intent.Desconocida, PhysicalAction.Ninguna)`
- Entonces el `ReasonCode` es `SIN_CAMBIO_RELEVANTE` y `result.Changed` es `false`

### Requirement: Adaptador ScriptableObject aislado

La unica clase de M4 que referencia `UnityEngine` DEBE ser `ReceptivityProfileAsset`, en el
sub-ensamblado `NpcAi.Receptivity.Unity` (`noEngineReferences: false`). `NpcAi.Receptivity` DEBE
mantener `noEngineReferences: true`. `ReceptivityProfileAsset` DEBE exponer `ToProfile()` (metodo
de instancia que proyecta sus campos a un `ReceptivityProfile`, sin tomar ninguna decision) y
`BuildCatalog(IEnumerable<ReceptivityProfileAsset>)` (metodo estatico que arma un
`ReceptivityProfileCatalog` a partir de los assets de M5, ignorando los `null` y quedandose con
el ultimo si dos comparten id). `ReceptivityEngine` NO DEBE conocer este tipo.

#### Scenario: El nucleo de M4 no referencia el motor grafico

- Dado `Runtime/Receptivity/NpcAi.Receptivity.asmdef`
- Cuando se lee `noEngineReferences`
- Entonces es `true`

#### Scenario: ToProfile proyecta los campos del asset

- Dado un `ReceptivityProfileAsset` con umbrales y tablas definidos en el Inspector
- Cuando se llama `ToProfile()`
- Entonces el `ReceptivityProfile` resultante devuelve esos mismos umbrales y deltas

#### Scenario: BuildCatalog arma el catalogo desde los assets de M5

- Dado un arreglo de `ReceptivityProfileAsset`, uno por personalidad de M5
- Cuando se llama `ReceptivityProfileAsset.BuildCatalog(assets)`
- Entonces el `ReceptivityProfileCatalog` resultante resuelve cada `personalityId` a su perfil y cae en `Default` para un id ausente

### Requirement: El doble hereda el mismo contrato

`ScriptedReceptivityEngine` DEBE vivir en `Runtime/Receptivity/Fakes/`, ser determinista y C#
puro, y DEBE pasar `ReceptivityEngineContract` — la misma clase base de prueba que hereda el
motor real. PUEDE ignorar el `Tone` y usar una tabla de deltas y umbrales mas simple que el
motor real.

#### Scenario: Doble y motor real pasan el mismo contrato de puerto

- Dado `ScriptedReceptivityEngine` y `ReceptivityEngine`, cada uno como sujeto de `ReceptivityEngineContract`
- Cuando corre la suite del contrato
- Entonces las 9 pruebas pasan para las dos implementaciones

## Trazabilidad (requisito -> prueba en verde)

| Requisito | Prueba(s) que ya lo demuestran |
|---|---|
| Motor real parametrizado por perfil | `ReceptivityEngineContract.Current_es_Neutral_antes_del_primer_Reset`, `...Reset_repetido...idempotente`; `ReceptivityEngineTests.Grosero_arranca_no_receptivo_y_empatico_arranca_neutral` |
| Blindaje de la monotonia del contrato | `ReceptivityEngineContract.La_agresion_sostenida_nunca_mejora_la_receptividad`, `...La_empatia_sostenida_nunca_empeora_la_receptividad` |
| Saturacion simetrica del puntaje | `ReceptivityEngineTests.El_puntaje_se_satura_en_el_limite_del_perfil` |
| El tono modula el resultado | `ReceptivityEngineTests.El_tono_cambia_el_resultado_de_la_misma_intencion` |
| Perfil como contenedor de datos C# puro | `ReceptivityProfileTests.El_perfil_Default_castiga_la_agresion_y_premia_la_empatia`, `...Las_entradas_sin_puntuar_devuelven_cero`, `...Los_accesores_toleran_diccionarios_nulos`, `...El_contenedor_guarda_los_umbrales_tal_cual` |
| Catalogo PersonalityId -> perfil, inyectable | `ReceptivityProfileCatalogTests.Un_id_desconocido_cae_en_Default`, `...None_cae_en_Default`, `...Standard_trae_exactamente_las_cuatro_personalidades_de_M5`, `...Un_catalogo_inyectado_devuelve_lo_que_se_le_dio`; `ReceptivityEngineTests.El_motor_toma_el_catalogo_que_se_le_inyecta` |
| ReasonCode con vocabulario compartido | `RazonParityTests.El_doble_y_el_motor_real_reportan_la_misma_razon` (56 casos, `Intent` x `PhysicalAction`); `ReceptivityEngineContract.Una_intencion_desconocida_no_mueve_el_estado` |
| Adaptador ScriptableObject aislado | `ReceptivityProfileAssetTests.ToProfile_traslada_umbrales_y_puntaje_inicial`, `...ToProfile_traslada_las_tres_tablas_de_delta`, `...BuildCatalog_arma_un_catalogo_consultable_por_id`, `...Del_asset_al_motor_real_sin_tocar_una_clase`; `NpcAi.Receptivity.asmdef` con `noEngineReferences: true` |
| El doble hereda el mismo contrato | `ScriptedReceptivityEngineTests : ReceptivityEngineContract`; `ReceptivityEngineTests : ReceptivityEngineContract` |
