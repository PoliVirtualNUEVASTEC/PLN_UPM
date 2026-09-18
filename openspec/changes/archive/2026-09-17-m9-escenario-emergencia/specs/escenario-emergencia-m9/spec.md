# Especificacion: escenario-emergencia-m9

## Purpose

Define los requisitos y escenarios de comportamiento de la primera implementacion real de
`IScenarioObjective` para el escenario de emergencia (M9, `Runtime/Scenarios/Emergency/`):
`TriageScenarioObjective`, que lee el bloque `clave` de `Data/Cases/*.json` (via
`TriageKeyLoader`, inyectado por `Func<ClinicalCaseId,string>`) y mide `Progress01`/`IsComplete`
como una mezcla de receptividad sostenida (`Notify(ReceptivityChange)`, el puerto) y correccion
clinica (banderas rojas descubiertas + triaje declarado, via la superficie aditiva `AssignCase`/
`RegisterRedFlag`/`DeclareTriage`/`Reset`). `IScenarioObjective` y `ReceptivityChange`
(`contrato-nucleo-m0`) no cambian. Quedan explicitamente fuera de esta entrega: la evaluacion
automatica de `clave.cierreEsperado`, la integracion fina con M15 (indice de `Hecho`/`campo` que
hizo match) y el cableado en escena (M11). Esta spec no cierra M9 por completo.

## ADDED Requirements

### Requirement: Conformidad con ScenarioObjectiveContract

`TriageScenarioObjective` DEBE heredar `NpcAi.Core.Tests.ScenarioObjectiveContract` en
`Tests/EditMode/Scenarios/Emergency/` y pasar sus 7 pruebas sin modificar la clase base: arranca
en 0 sin completar, el progreso siempre esta en `[0,1]`, `IsComplete` es verdadero si y solo si
`Progress01 == 1` (tolerancia `1e-4`) en cada paso, el progreso nunca baja de 0, la completitud es
reversible, y `Notify(default)` es no-op.

#### Scenario: Paso de la suite contractual heredada
- Dado `TriageScenarioObjectiveTests : ScenarioObjectiveContract`
- Cuando se ejecuta la bateria heredada en el Test Runner EditMode
- Entonces las 7 pruebas contractuales resultan en verde sin modificar la clase base, y ninguna
  queda inconclusa por el escape `Assume.That`

### Requirement: Carga del bloque `clave`

`TriageKeyLoader.TryParse` DEBE devolver `HasKey == true` unicamente cuando el JSON tiene un
bloque `clave` bien formado con `triajeEsperado` en `{"I".."V"}`. Ante `clave` ausente, JSON
malformado, o `triajeEsperado` fuera de ese conjunto, DEBE devolver `HasKey == false` sin lanzar
excepcion. Un `cargarJson` inyectado nulo, que lanza, o que devuelve un caso sin `clave` se trata
como "ningun caso carga".

#### Scenario: Caso feliz
- Dado un JSON con `clave` bien formado, `triajeEsperado` "II" y `banderasRojas` no vacio
- Cuando `AssignCase` invoca `cargarJson` y el resultado se parsea
- Entonces `HasKey` es verdadero y `ExpectedClosure` refleja `clave.cierreEsperado`

#### Scenario: `clave` ausente
- Dado un JSON de caso sin bloque `clave`
- Cuando se asigna ese caso
- Entonces `HasKey` es falso y no se lanza excepcion

#### Scenario: JSON malformado o carga fallida
- Dado un `cargarJson` nulo, que lanza, o que devuelve texto no-JSON
- Cuando se asigna el caso
- Entonces `HasKey` es falso y no se propaga excepcion

#### Scenario: `triajeEsperado` fuera de rango
- Dado un `clave.triajeEsperado` con un valor distinto de "I".."V" (p. ej. "VI" o vacio)
- Cuando se parsea el bloque
- Entonces `HasKey` es falso

### Requirement: Renormalizacion de pesos sin caso asignado

Mientras `HasKey` sea falso, la receptividad sostenida DEBE representar el 100% del peso de
`Progress01`, de modo que sea alcanzable `Progress01 == 1` unicamente a traves de
`Notify(ReceptivityChange)`, sin depender de la superficie aditiva.

#### Scenario: Progreso total sin clave por receptividad sola
- Dado un `TriageScenarioObjective` recien creado, sin `AssignCase` invocado
- Cuando se notifican suficientes `ReceptivityChange` de mejora consecutivos
- Entonces `Progress01` alcanza 1 e `IsComplete` es verdadero

### Requirement: Mezcla de receptividad y correccion clinica con caso asignado

Con un caso asignado (`HasKey == true`), ni la receptividad sostenida sola ni la correccion
clinica sola (banderas rojas + triaje) DEBEN llevar `Progress01` a 1: ambas mitades son
necesarias, ponderadas por `TriageObjectiveSettings` (0.3 receptividad / 0.7 clinica por defecto;
dentro de la mitad clinica, 0.7 banderas / 0.3 triaje).

#### Scenario: Receptividad sostenida sola no completa
- Dado un caso asignado sin ninguna bandera roja registrada ni triaje declarado
- Cuando se notifican suficientes mejoras para agotar la racha de receptividad
- Entonces `Progress01` queda por debajo de 1

#### Scenario: Correccion clinica sola no completa
- Dado un caso asignado con todas las banderas rojas registradas y el triaje correcto declarado
- Cuando no se notifica ninguna mejora de receptividad
- Entonces `Progress01` queda por debajo de 1

#### Scenario: Ambas mitades completan
- Dado un caso asignado con todas las banderas rojas registradas, el triaje correcto declarado y
  receptividad sostenida al maximo
- Cuando se consulta `Progress01`
- Entonces `Progress01` es 1 e `IsComplete` es verdadero

### Requirement: Racha de receptividad sostenida

La racha de receptividad DEBE crecer con cada `ReceptivityChange.Improved` y reiniciarse a 0 ante
cualquier `Worsened`. `Notify(default)` o un `ReceptivityChange` sin cambio (`Changed == false`)
NO DEBEN alterar la racha.

#### Scenario: La racha crece con mejoras sostenidas
- Dado un objetivo recien creado
- Cuando se notifican N mejoras consecutivas
- Entonces la contribucion de receptividad a `Progress01` crece monotonamente hasta su tope

#### Scenario: Un empeoramiento reinicia la racha completa
- Dado una racha de mejoras ya acumulada
- Cuando se notifica un `Worsened`
- Entonces la contribucion de receptividad vuelve a 0, no se descuenta un solo paso

#### Scenario: Notify sin cambio no altera la racha
- Dado una racha de mejoras ya acumulada
- Cuando se llama `Notify(default)` o con un `ReceptivityChange` donde `From == To`
- Entonces la racha y `Progress01` quedan exactamente iguales

### Requirement: Registro de banderas rojas

`RegisterRedFlag(int index)` DEBE ser idempotente por indice (una segunda llamada al mismo indice
no suma credito adicional), DEBE rechazar indices fuera de rango de `clave.banderasRojas` sin
lanzar, y NO DEBE tener efecto antes de `AssignCase`. El credito de esta mitad DEBE ser
proporcional a la fraccion de banderas encontradas, no todo-o-nada.

#### Scenario: Registro idempotente
- Dado un caso asignado con 5 banderas rojas
- Cuando se llama `RegisterRedFlag(2)` dos veces
- Entonces `RedFlagsFound` cuenta 1, no 2

#### Scenario: Indice fuera de rango no lanza
- Dado un caso asignado con 5 banderas rojas
- Cuando se llama `RegisterRedFlag(99)` o con un indice negativo
- Entonces no se lanza excepcion y `RedFlagsFound` no cambia

#### Scenario: Sin efecto antes de asignar caso
- Dado un objetivo sin `AssignCase` invocado
- Cuando se llama `RegisterRedFlag(0)`
- Entonces no hay efecto observable y no se lanza excepcion

#### Scenario: Credito proporcional
- Dado un caso con 4 banderas rojas
- Cuando se registran 2 de las 4
- Entonces la contribucion de esta mitad a `Progress01` es aproximadamente la mitad de su peso
  maximo, no todo-o-nada

### Requirement: Declaracion de triaje

`DeclareTriage(string category)` DEBE comparar contra `clave.triajeEsperado` normalizado y DEBE
ser sobrescribible: una nueva declaracion reemplaza la anterior, pudiendo pasar de correcta a
incorrecta y viceversa.

#### Scenario: Declaracion correcta
- Dado un caso con `triajeEsperado` "II"
- Cuando se llama `DeclareTriage("II")`
- Entonces el componente de triaje contribuye su peso completo a `Progress01`

#### Scenario: Sobrescritura de correcto a incorrecto
- Dado una declaracion previa correcta
- Cuando se llama `DeclareTriage` de nuevo con una categoria distinta a `triajeEsperado`
- Entonces el componente de triaje deja de contribuir y `Progress01` baja en consecuencia

### Requirement: Reversibilidad de IsComplete por ambas vias

Con `Progress01 == 1` ya alcanzado, `IsComplete` DEBE poder volver a falso tanto por un
`ReceptivityChange.Worsened` como por declarar un triaje incorrecto despues de haber declarado
uno correcto.

#### Scenario: Reversion por empeoramiento de receptividad
- Dado un objetivo ya completo
- Cuando se notifica un `Worsened`
- Entonces `Progress01` baja de 1 e `IsComplete` es falso

#### Scenario: Reversion por triaje incorrecto tras uno correcto
- Dado un objetivo ya completo con triaje correcto declarado
- Cuando se llama `DeclareTriage` con una categoria distinta a la esperada
- Entonces `Progress01` baja de 1 e `IsComplete` es falso

### Requirement: Reinicio de progreso al reasignar caso o resetear

`Reset()` y una nueva llamada a `AssignCase` DEBEN descartar el progreso de banderas rojas y la
declaracion de triaje del caso anterior. La racha de receptividad sigue sus propias reglas y no
esta atada al caso asignado.

#### Scenario: Reasignar caso descarta banderas y triaje previos
- Dado un caso con banderas registradas y triaje declarado correctamente
- Cuando se llama `AssignCase` con un `ClinicalCaseId` distinto
- Entonces `RedFlagsFound` y el estado de triaje quedan en su valor inicial para el nuevo caso

#### Scenario: `Reset()` vuelve al estado sin caso asignado
- Dado un caso asignado con progreso clinico parcial
- Cuando se llama `Reset()`
- Entonces `HasKey` vuelve a falso y `Progress01` vuelve a depender solo de receptividad
  (renormalizado al 100%)

## Trazabilidad (requisito -> prueba)

| Requisito | Prueba EditMode |
|---|---|
| Conformidad con ScenarioObjectiveContract | 7 pruebas heredadas de `ScenarioObjectiveContract` sin cambios |
| Carga del bloque `clave` | `Cargar_clave_bien_formada_deja_HasKey_verdadero`, `Clave_ausente_deja_HasKey_falso`, `Json_malformado_o_carga_fallida_no_lanza`, `TriajeEsperado_fuera_de_I_a_V_deja_HasKey_falso` (propuesto) |
| Renormalizacion de pesos sin caso asignado | `Sin_caso_asignado_receptividad_sola_completa_el_progreso` (propuesto) |
| Mezcla de receptividad y correccion clinica | `Receptividad_sola_no_completa_con_caso_asignado`, `Correccion_clinica_sola_no_completa`, `Ambas_mitades_completan_el_progreso` (propuesto) |
| Racha de receptividad sostenida | `La_racha_crece_con_mejoras_sostenidas`, `Un_empeoramiento_reinicia_la_racha_completa`, `Notify_sin_cambio_no_altera_la_racha` (propuesto) |
| Registro de banderas rojas | `RegisterRedFlag_es_idempotente_por_indice`, `RegisterRedFlag_con_indice_fuera_de_rango_no_lanza`, `RegisterRedFlag_antes_de_AssignCase_no_tiene_efecto`, `El_credito_por_bandera_roja_es_proporcional` (propuesto) |
| Declaracion de triaje | `DeclareTriage_correcto_contribuye_el_peso_completo`, `DeclareTriage_sobrescribe_de_correcto_a_incorrecto` (propuesto) |
| Reversibilidad de IsComplete por ambas vias | `IsComplete_se_revierte_por_Worsened_tras_completar`, `IsComplete_se_revierte_por_triaje_incorrecto_tras_acertar` (propuesto) |
| Reinicio de progreso al reasignar caso o resetear | `Reasignar_caso_descarta_banderas_y_triaje_previos`, `Reset_vuelve_al_estado_sin_caso_asignado` (propuesto) |
