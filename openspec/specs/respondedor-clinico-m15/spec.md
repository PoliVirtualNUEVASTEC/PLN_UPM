# Especificacion: respondedor-clinico-m15

## Purpose

Congela el comportamiento observable de `NpcAi.ClinicalResponse`, la primera implementacion
real del puerto `IClinicalResponder` (contrato v2 de `NpcAi.Core`, capacidad
`respuesta-clinica-m0`). El modulo hace que el NPC responda "como el paciente del caso
clinico asignado" recuperando hechos de una tabla explicita (`Data/Cases/*.json`, M14), nunca
generando texto libre. Esta capacidad no modifica `respuesta-clinica-m0`: la satisface y
agrega requisitos propios sobre COMO se produce la respuesta (tabla de hechos, no modelo) y
que garantias extra da sobre el dato (sobrevive intacto; `clave` es estructuralmente
inalcanzable).

## ADDED Requirements

### Requirement: Conformidad con `ClinicalResponderContract` sin excepcion

`ClinicalResponder` (real) y `ScriptedClinicalResponder` (doble) DEBEN heredar de
`NpcAi.Core.Tests.ClinicalResponderContract` sin modificarla, y pasar el 100% de sus pruebas
heredadas.

#### Scenario: La bateria heredada pasa para el doble y la implementacion real

- Dado `NpcAi.ClinicalResponse.Tests`
- Cuando se ejecutan `ScriptedClinicalResponderTests` y `ClinicalResponderTests`
- Entonces ambas pasan el 100% de las pruebas heredadas de `ClinicalResponderContract`, sin
  tocar esa clase base

### Requirement: La respuesta sale de la tabla de hechos del caso, nunca se inventa

Cuando `Respond` devuelve `Handled == true`, `Reply.Text` DEBE contener, como subcadena
intacta y sin alterar, la `respuesta` de exactamente el `Hecho` de la tabla del caso asignado
cuyo `ejemplosDePregunta` cubre completamente las palabras normalizadas de la pregunta. El
matiz de personalidad (si lo hay) DEBE limitarse a un prefijo fijo alrededor de esa subcadena;
NUNCA DEBE reescribir, resumir ni generar texto que no venga de la tabla.

#### Scenario: La respuesta del caso aparece intacta sin importar la personalidad

- Dado un `ClinicalResponder` con un caso asignado y cualquiera de las 4 personalidades
  (`grosero`, `histerico`, `introvertido`, `empatico`)
- Cuando `Respond` empareja una pregunta contra un `Hecho` de la tabla
- Entonces `Reply.Text` contiene la `respuesta` de ese `Hecho` como subcadena literal

#### Scenario: `PersonalityId.None` e `introvertido` no agregan matiz

- Dado un `ClinicalResponder` con `PersonalityId.None` o `"introvertido"` asignado
- Cuando `Respond` maneja el turno
- Entonces `Reply.Text` es exactamente igual a la `respuesta` de la tabla, sin prefijo

### Requirement: El emparejamiento es determinista con un criterio de empate fijo

`ClinicalFactMatcher.Match` DEBE recorrer los `Hecho` del caso en el orden de la tabla y
devolver el indice del primero cuyo `ejemplosDePregunta` este completamente cubierto por las
palabras normalizadas de la pregunta (todas las palabras del ejemplo aparecen en la
pregunta); si ninguno aplica, DEBE devolver `-1`. Cuando mas de un `Hecho` calificaria, DEBE
ganar el de menor indice en la lista.

#### Scenario: Coincidencia esperada y no-coincidencia con un turno social

- Dado un caso con la tabla de hechos de `Data/Cases/caso-01.json`
- Cuando se normaliza y empareja "¿desde cuándo le empezó el dolor?"
- Entonces el resultado es el indice del `Hecho` con `campo == "inicio_sintoma"`
- Cuando se normaliza y empareja "buenos días, ¿cómo se siente?"
- Entonces el resultado es `-1`

#### Scenario: El empate lo resuelve el orden de la lista

- Dado dos `Hecho` cuyos `ejemplosDePregunta` calificarian ambos para la misma pregunta
- Cuando se llama `Match`
- Entonces el resultado es el indice del `Hecho` que aparece primero en la lista

### Requirement: `clave` es estructuralmente inalcanzable para M15

`ClinicalCase` (y los tipos que carga `ClinicalCaseLoader`) NO DEBEN declarar ningun campo
correspondiente al bloque `clave` del esquema de M14 (`triajeEsperado`, `tiempoAtencion`,
`banderasRojas`, `cierreEsperado`). Ninguna `Hecho.Respuesta` de los casos reales de
`Data/Cases/` DEBE contener el `triajeEsperado` de su propio caso ni el texto literal de una
de sus `banderasRojas`.

#### Scenario: El catalogo real no filtra la clave de evaluacion

- Dado cada uno de los 3 `Data/Cases/caso-*.json` reales
- Cuando se inspecciona cada `Hecho.Respuesta` contra el `triajeEsperado` y las
  `banderasRojas` de su propio bloque `clave`
- Entonces ninguna `Respuesta` contiene ese texto

### Requirement: Un turno no clinico nunca se fuerza a coincidir

Ante una `Utterance` que no empareja ningun `Hecho` de la tabla del caso asignado, `Respond`
DEBE devolver `ClinicalResponse.NoAplica` (`Handled == false`), sin importar el `IntentResult`
recibido.

#### Scenario: Un saludo no es un turno clinico

- Dado un `ClinicalResponder` con un caso asignado
- Cuando `Respond` recibe "Buenos días, ¿cómo está?"
- Entonces devuelve `Handled == false`

### Requirement: `AssignCase` nunca lanza pase lo que pase con el cargador inyectado

`ClinicalResponder` recibe un `Func<ClinicalCaseId,string>` por constructor para resolver los
bytes del caso. Si ese delegado es `null`, lanza una excepcion al invocarse, o devuelve un
JSON que no valida contra el esquema minimo (`ClinicalCaseLoader.TryParse` devuelve `false`),
`AssignCase` NO DEBE propagar la excepcion y DEBE dejar `IsReady` en `false`.

#### Scenario: Un delegado que lanza o un JSON invalido no rompen el respondedor

- Dado un `ClinicalResponder` construido con un `cargarJson` que lanza, devuelve `null`, o
  devuelve texto que no es JSON valido para un `ClinicalCaseId` dado
- Cuando se llama `AssignCase` con ese id
- Entonces no se propaga ninguna excepcion e `IsReady` queda en `false`

## Trazabilidad (requisito -> prueba)

| Requisito | Prueba(s) |
|---|---|
| Conformidad con `ClinicalResponderContract` sin excepcion | `ScriptedClinicalResponderTests`, `ClinicalResponderTests` (heredan la base completa) |
| La respuesta sale de la tabla de hechos, nunca se inventa | `ClinicalResponderTests.La_respuesta_del_caso_siempre_aparece_intacta_en_el_texto`, `...PersonalityId_None_no_agrega_matiz`, `...Introvertido_tampoco_agrega_matiz` |
| El emparejamiento es determinista con un criterio de empate fijo | `ClinicalFactMatcherTests.Match_encuentra_el_campo_que_pregunta_por_el_inicio_del_sintoma`, `...Match_no_encuentra_nada_en_un_turno_social`, `...En_empate_de_cobertura_gana_el_hecho_de_menor_indice` |
| `clave` es estructuralmente inalcanzable para M15 | `ClinicalCasesDataTests.Ninguna_respuesta_filtra_el_triaje_esperado_ni_una_bandera_roja` (mas la ausencia del campo en `ClinicalCase`, verificable por inspeccion del tipo) |
| Un turno no clinico nunca se fuerza a coincidir | `ClinicalResponderTests.Un_saludo_no_es_un_turno_clinico`, `ScriptedClinicalResponderTests.Un_saludo_no_es_un_turno_clinico` |
| `AssignCase` nunca lanza pase lo que pase con el cargador inyectado | `ClinicalResponderContract.AssignCase_con_caso_desconocido_no_lanza_y_deja_no_listo` (heredada, ejercida contra un cargador que devuelve `null` para ids desconocidos) |
