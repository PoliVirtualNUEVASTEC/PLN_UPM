# Especificacion: respondedor-requerimientos-m16

## Purpose

Congela el comportamiento observable de `NpcAi.RequirementResponse`, la primera
implementacion real del puerto `IRequirementResponder` (contrato v3 de `NpcAi.Core`,
capacidad `requerimientos-juntas-m0`, aun sin archivar). El modulo hace que el NPC se
comporte como el cliente de la sala de juntas que ya tiene los requerimientos del proyecto
"en la cabeza" y los revela progresivamente segun la receptividad ganada, recuperando texto
de una tabla explicita (`Data/Requirements/*.json`, capacidad `catalogo-requerimientos-m16`),
nunca generando texto libre. Esta capacidad no modifica `requerimientos-juntas-m0`: la
satisface heredando `RequirementResponderContract` sin tocarla, y agrega requisitos propios
sobre COMO se produce la revelacion (tabla + puerta de receptividad, no modelo).

## ADDED Requirements

### Requirement: Conformidad con `RequirementResponderContract` sin excepcion, salvo divergencia documentada de datos

`RequirementResponder` (real) y `ScriptedRequirementResponder` (doble) DEBEN heredar de
`NpcAi.Core.Tests.RequirementResponderContract` sin modificarla. `ScriptedRequirementResponder`
DEBE pasar el 100% de sus 10 pruebas heredadas, porque el doble puede usar datos sinteticos
libremente. `RequirementResponder` (real) DEBE pasar en verde toda prueba heredada cuyo
escenario no dependa de un requerimiento de "presupuesto" que `Data/Requirements/*.json` no
declare; las pruebas heredadas que SI dependen de esa frase fija DEBEN quedar en `Assume`
(inconclusas, nunca rojas ni forzadas a verde) porque el catalogo real transcribe unicamente
los requerimientos que narra `Data/Corpus/juntas.json`, sin inventar contenido para satisfacer
un caso de prueba. Esta divergencia es deliberada (decision de Jefferson, 2026-09-18), no un
defecto a ocultar ni a resolver agregando datos ficticios.

#### Scenario: El doble pasa el 100% de la bateria heredada

- Dado `NpcAi.RequirementResponse.Tests.ScriptedRequirementResponderTests`
- Cuando se ejecuta contra `RequirementResponderContract`
- Entonces las 10 pruebas heredadas pasan en verde, sin omitir ninguna por `Assume`

#### Scenario: La implementacion real pasa las pruebas que no dependen de "presupuesto"

- Dado `NpcAi.RequirementResponse.Tests.RequirementResponderTests` contra el catalogo real
  (4 casos transcritos de `juntas.json`, ninguno con un requerimiento de "presupuesto")
- Cuando se ejecutan las 10 pruebas heredadas de `RequirementResponderContract`
- Entonces las pruebas que no dependen de la frase fija "cual es el presupuesto del proyecto"
  pasan en verde, y las que si dependen quedan en `Assume` (inconclusas), nunca en rojo

### Requirement: La revelacion sale de la tabla del caso, nunca se inventa

Cuando `Respond` devuelve `Outcome == RequirementOutcome.Revelado`, `Reply.Text` DEBE
contener, como subcadena intacta y sin alterar, la `respuesta` de exactamente el
requerimiento de la tabla del caso asignado que empareja. El matiz de personalidad (si lo
hay) DEBE limitarse a un prefijo o sufijo fijo alrededor de esa subcadena; NUNCA DEBE
reescribir, resumir ni generar texto que no venga de la tabla. `EmotionTag`/`AnimationCue`
DEBEN salir de una tabla fija, no derivarse del texto.

#### Scenario: La respuesta del caso aparece intacta sin importar la personalidad

- Dado un `RequirementResponder` con un caso asignado y `Receptivity.Receptivo`
- Cuando `Respond` empareja una pregunta contra un requerimiento de la tabla cuya
  `receptividadMinima` ya se alcanzo
- Entonces `Reply.Text` contiene la `respuesta` de ese requerimiento como subcadena literal

### Requirement: El emparejamiento es determinista con un criterio de empate fijo

`RequirementMatcher.Match` DEBE recorrer los requerimientos del caso en el orden de la
tabla y devolver el primero cuyos `ejemplosDePregunta` cubran completamente las palabras
normalizadas (minusculas invariantes, sin tildes, sin signos) de la `Utterance`; si ninguno
aplica, DEBE devolver el centinela "sin coincidencia". Ante empate, DEBE ganar el
requerimiento de menor indice en la lista.

#### Scenario: Coincidencia esperada y no-coincidencia con un turno social

- Dado un caso con tabla de requerimientos real
- Cuando se normaliza y empareja una pregunta de dominio conocida
- Entonces el resultado es el `RequirementId` del requerimiento correspondiente
- Cuando se normaliza y empareja un saludo social
- Entonces no hay coincidencia

### Requirement: El emparejamiento no depende de `Intent`

`Respond` DEBE decidir que requerimiento (si alguno) aplica usando solo el texto normalizado
de `Utterance` contra los `ejemplosDePregunta`. El `IntentResult` recibido NO DEBE cambiar
cual requerimiento empareja ni si el resultado es `NoAplica`.

#### Scenario: La misma frase empareja igual con distinto `Intent`

- Dado el mismo texto de `Utterance` emparejable contra un requerimiento
- Cuando `Respond` se llama una vez con `Intent.AportaInformacion` y otra con
  `IntentResult.Unknown()`
- Entonces el `RequirementId` resultante es el mismo en ambos casos

### Requirement: La puerta de receptividad decide `Revelado` vs `AunNoRevelado`

Con un requerimiento emparejado, `Outcome == Revelado` DEBE cumplirse si y solo si
`(int)receptivity >= (int)receptividadMinima` del requerimiento (comparacion aritmetica
sobre `Receptivity.NoReceptivo = -1`, `Neutral = 0`, `Receptivo = 1`); esto incluye
requerimientos con `receptividadMinima == NoReceptivo`, que se revelan incluso con
`Receptivity.NoReceptivo`. Si empareja pero no alcanza el umbral, `Outcome` DEBE ser
`AunNoRevelado`, `Reply.Text` DEBE ser un desvio no vacio tomado del banco por
`PersonalityId` (nunca la `respuesta` real), y `RequirementId` DEBE quedar poblado con el
requerimiento emparejado.

#### Scenario: Bajo umbral desvia sin filtrar el hecho

- Dado un requerimiento con `receptividadMinima == Receptivo` emparejado con
  `Receptivity.Neutral`
- Cuando `Respond` maneja el turno
- Entonces `Outcome == AunNoRevelado`, `Reply.Text` no esta vacio, no contiene la
  `respuesta` del requerimiento, y `RequirementId` esta poblado

#### Scenario: Un requerimiento trivial se revela incluso con receptividad negativa

- Dado un requerimiento con `receptividadMinima == NoReceptivo` emparejado con
  `Receptivity.NoReceptivo`
- Cuando `Respond` maneja el turno
- Entonces `Outcome == Revelado`

### Requirement: Un turno no relacionado nunca se fuerza a coincidir

Ante una `Utterance` que no empareja ningun requerimiento de la tabla del caso asignado, o
con `IsReady == false`, `Respond` DEBE devolver `RequirementResponse.NoAplica`
(`RequirementId.None`), sin importar el `IntentResult` recibido.

#### Scenario: Un turno de control no es de requerimientos

- Dado un `RequirementResponder` con un caso asignado
- Cuando `Respond` recibe una `Utterance` sin relacion con el dominio del caso
- Entonces devuelve `Outcome == NoAplica` con `RequirementId.None`

### Requirement: `AssignCase` nunca lanza pase lo que pase con el cargador inyectado

`RequirementResponder` recibe un delegado para resolver los bytes del caso por
`RequirementCaseId`. Si ese delegado es `null`, lanza una excepcion al invocarse, o devuelve
un JSON que no valida contra el esquema minimo, `AssignCase` NO DEBE propagar la excepcion y
DEBE dejar `IsReady` en `false`.

#### Scenario: Un `RequirementCaseId` desconocido no rompe el respondedor

- Dado un `RequirementResponder` recien construido
- Cuando se llama `AssignCase` con un `RequirementCaseId` inexistente
- Entonces no se propaga ninguna excepcion e `IsReady` queda en `false`

### Requirement: Sin memoria entre turnos, determinismo estructural

`Respond` NO DEBE recordar que requerimientos revelo en llamadas previas: la progresion la
gobierna exclusivamente el parametro `Receptivity` de cada llamada. Tres llamadas
consecutivas con la tupla identica `(RequirementCaseId, PersonalityId, Utterance,
IntentResult, Receptivity)` DEBEN producir `Outcome`, `Reply.Text` y `RequirementId`
identicos. `AssignCase` es el unico estado (de sesion, no de turno) y DEBE ser idempotente
ante el mismo par repetido.

#### Scenario: La misma entrada nunca cambia de resultado

- Dado un `RequirementResponder` listo
- Cuando se llama `Respond` tres veces con la misma tupla de entrada
- Entonces `Outcome`, `Reply.Text` y `RequirementId` son identicos en las tres llamadas

## Trazabilidad (requisito -> prueba)

| Requisito | Prueba(s) |
|---|---|
| Conformidad con `RequirementResponderContract` (con divergencia documentada) | `ScriptedRequirementResponderTests` (10/10 verde); `RequirementResponderTests` (verde salvo `Assume` en los casos ligados a "presupuesto") |
| La revelacion sale de la tabla, nunca se inventa | `RequirementResponderTests.La_respuesta_del_caso_siempre_aparece_intacta_en_el_texto` |
| El emparejamiento es determinista con un criterio de empate fijo | `RequirementMatcherTests.Match_encuentra_el_requerimiento_esperado`, `...No_encuentra_nada_en_un_turno_social`, `...En_empate_gana_el_de_menor_indice` |
| El emparejamiento no depende de `Intent` | `RequirementMatcherTests.El_Intent_no_cambia_el_emparejamiento` |
| La puerta de receptividad decide `Revelado` vs `AunNoRevelado` | `RequirementDisclosurePolicyTests` (3 niveles x 3 valores de `Receptivity`), `RequirementResponderContract.Baja_receptividad_y_alta_receptividad_no_dan_el_mismo_texto` |
| Un turno no relacionado nunca se fuerza a coincidir | `RequirementResponderContract.El_RequirementId_esta_poblado_si_y_solo_si_el_turno_aplica` |
| `AssignCase` nunca lanza pase lo que pase con el cargador inyectado | `RequirementResponderContract.AssignCase_con_caso_desconocido_no_lanza_y_deja_no_listo` |
| Sin memoria entre turnos, determinismo estructural | `RequirementResponderContract.Es_determinista_en_Outcome_texto_y_RequirementId_para_la_misma_entrada`, `...AssignCase_es_idempotente_con_el_mismo_par` |
