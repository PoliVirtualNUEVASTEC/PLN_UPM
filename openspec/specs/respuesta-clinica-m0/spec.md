# Especificacion: respuesta-clinica-m0

## Purpose

Congela el comportamiento observable del puerto `IClinicalResponder` y de los DTO
`ClinicalCaseId` y `ClinicalResponse` que el contrato v2 de `NpcAi.Core` agrega para que M15
(respondedor clinico) exista como modulo de primera clase. El puerto transporta la respuesta
del NPC "como el paciente del caso asignado" y la senal que separa el turno clinico del social
(enrutado a M6 sin cambios). El cambio es puramente aditivo: ningun enum, DTO ni puerto de v1
cambia. El esquema del caso clinico (sintomas, antecedentes, tabla de hechos) es dato de M14 y
NO forma parte del contrato: el caso viaja como `ClinicalCaseId` (string), no como objeto. La
decision de triaje (`ITriageBoard` / enum `Triage`) NO es parte de esta capacidad. El mecanismo
de verificacion es la clase base nueva `ClinicalResponderContract` y los casos nuevos de
`ContractTypeTests`; la trazabilidad es hacia adelante (esas pruebas aun no existen).

## ADDED Requirements

### Requirement: Extension aditiva del contrato a v2

`NpcAi.Core` DEBE agregar exactamente el puerto `IClinicalResponder`, el `readonly struct
ClinicalCaseId` y el `readonly struct ClinicalResponse`. Los 4 enums, los 5 DTO y los 7 puertos
de v1 NO DEBEN cambiar (nombre, valor, orden, cardinalidad, firma). `Contract.Version` DEBE
pasar de `1` a `2` y `Docs/CONTRACT-CHANGELOG.md` DEBE ganar la seccion `## v2`. Los 3 tipos
nuevos DEBEN ser C# puro (solo `string`, `bool`, `struct` y tipos de v1); `NpcAi.Core` DEBE
mantener `noEngineReferences: true` y cero referencias `NpcAi.*` ajenas.

#### Scenario: Los tipos de v1 quedan byte-identicos

- Dado el ensamblado `NpcAi.Core` tras aplicar el cambio
- Cuando se listan los miembros de `Intent`, `Tone`, `PhysicalAction`, `Receptivity` y las firmas de los 5 DTO y 7 puertos de v1
- Entonces son identicos a v1 y solo aparecen anadidos `IClinicalResponder`, `ClinicalCaseId` y `ClinicalResponse`

#### Scenario: La version queda atada al changelog

- Dado `Contract.Version`
- Cuando se lee su valor y el mayor encabezado `## v<N>` de `Docs/CONTRACT-CHANGELOG.md`
- Entonces el valor es `2` y el mayor encabezado es `## v2`

#### Scenario: Los tipos nuevos no meten Unity en el binario

- Dado reflexion sobre `NpcAi.Core`
- Cuando se inspeccionan `IClinicalResponder`, `ClinicalCaseId` y `ClinicalResponse`
- Entonces ninguno referencia `UnityEngine*`, `UnityEditor*` ni `NpcAi.*` ajeno

### Requirement: `ClinicalCaseId` es un identificador estable sobre string

`ClinicalCaseId` DEBE ser un `readonly struct` sobre `string`, espejo de `PersonalityId`:
`Value` normalizado a minusculas y sin espacios de borde; igualdad completa y `Ordinal`
(`Equals`, `GetHashCode`, `operator ==`, `operator !=`); `None = default`; `IsNone == true` solo
para `None`; `GetHashCode()` de `None` DEBE ser `0`. El numero de casos clinicos es dato de M14
y NUNCA un cambio de contrato.

#### Scenario: Normaliza el valor recibido

- Dado `new ClinicalCaseId(" Caso-01 ")`
- Cuando se lee `Value`
- Entonces es `"caso-01"`

#### Scenario: `None` es el valor por defecto y hashea a cero

- Dado `ClinicalCaseId.None`
- Cuando se consultan `IsNone` y `GetHashCode()`
- Entonces `IsNone` es `true` y `GetHashCode()` es `0`

#### Scenario: Igualdad Ordinal por valor normalizado

- Dado dos `ClinicalCaseId` construidos con `"caso-01"` y `" CASO-01 "`
- Cuando se comparan con `==`
- Entonces son iguales, y ambos son distintos de uno construido con `"caso-02"`

### Requirement: `ClinicalResponse` lleva la senal de enrutado

`ClinicalResponse` DEBE ser un `readonly struct` con `Handled` (bool) y `Reply` (`NpcReply`).
`ClinicalResponse.NoAplica` DEBE tener `Handled == false`. `Handled == false` DEBE significar
"turno no clinico": el llamador enruta a `IDialogueGenerator` (M6) y `Reply` NO tiene garantias.
`Handled == true` DEBE significar que `Reply` va tal cual a M8.

#### Scenario: `NoAplica` no esta manejado

- Dado `ClinicalResponse.NoAplica`
- Cuando se lee `Handled`
- Entonces es `false`

### Requirement: `IsReady` y `AssignCase` son seguros y deterministas

Leer `IsReady` NO DEBE lanzar en ningun estado. `IsReady` DEBE ser `false` hasta que
`AssignCase` vincule un `ClinicalCaseId` existente. `AssignCase` con el mismo `(ClinicalCaseId,
PersonalityId)` DEBE ser determinista e idempotente. `AssignCase` con un `ClinicalCaseId`
desconocido NO DEBE lanzar y DEBE dejar `IsReady == false`.

#### Scenario: Leer `IsReady` sin caso asignado no lanza

- Dado un respondedor recien construido sin `AssignCase`
- Cuando se lee `IsReady`
- Entonces devuelve `false` sin lanzar

#### Scenario: `AssignCase` es idempotente con el mismo par

- Dado un respondedor listo tras `AssignCase(caso, personalidad)`
- Cuando se vuelve a llamar `AssignCase` con el mismo par y luego `Respond` con una entrada fija
- Entonces `IsReady` y el `ClinicalResponse` devuelto son iguales antes y despues de la segunda llamada

#### Scenario: Caso desconocido deja el respondedor no listo

- Dado `AssignCase(new ClinicalCaseId("no-existe"), personalidad)`
- Cuando termina la llamada
- Entonces no lanzo e `IsReady` es `false`

### Requirement: `Respond` nunca lanza y degrada a `NoAplica`

`Respond` NO DEBE lanzar en ningun estado: sin `AssignCase`, con `Utterance` vacio o `default`,
con `IntentResult` `default`, con texto de solo simbolos o con una cadena de 5000 caracteres.
Con `IsReady == false`, `Respond` DEBE devolver `ClinicalResponse.NoAplica` (`Handled == false`).

#### Scenario: Sin caso asignado devuelve `NoAplica`

- Dado un respondedor con `IsReady == false`
- Cuando se llama `Respond(default, default)`
- Entonces devuelve `ClinicalResponse.NoAplica` sin lanzar

#### Scenario: Entradas atipicas no lanzan

- Dado un respondedor en cualquier estado
- Cuando se llama `Respond` con `Utterance` vacio, con `IntentResult` `default`, con `"!!!???"` y con una cadena de 5000 caracteres
- Entonces ninguna llamada lanza y cada resultado es un `ClinicalResponse` valido

### Requirement: Una respuesta manejada es presentable

Cuando `Respond` devuelve `Handled == true`, `Reply.Text` NO DEBE ser vacio ni estar compuesto
solo por espacios, y `Reply.EmotionTag` y `Reply.AnimationCue` NO DEBEN ser `null`.

#### Scenario: `Handled == true` trae texto y tags validos

- Dado un respondedor con `IsReady == true`
- Cuando `Respond` devuelve un `ClinicalResponse` con `Handled == true`
- Entonces `Reply.Text` no es vacio ni solo espacios y `Reply.EmotionTag` y `Reply.AnimationCue` no son `null`

### Requirement: `Respond` es determinista (asimetria con `IDialogueGenerator`)

Para la misma tupla `(ClinicalCaseId, PersonalityId, Utterance, IntentResult)`, llamadas
sucesivas a `Respond` DEBEN devolver el mismo `Handled` y el mismo `Reply.Text`
(`Reply.EmotionTag`, `Reply.AnimationCue` y cualquier latencia NO estan obligados). Es una
asimetria deliberada frente a `IDialogueGenerator.Generate`, que NO esta obligado a ser
determinista. `Docs/CONTRACT-CHANGELOG.md` v2 DEBE registrar esta asimetria bajo "Asimetria de
determinismo".

#### Scenario: Repetir la misma entrada da el mismo hecho

- Dado un respondedor listo con un caso y una personalidad fijos
- Cuando se llama `Respond` dos o mas veces con la misma `Utterance` e `IntentResult`
- Entonces `Handled` es identico en todas las respuestas y `Reply.Text` es identico en todas

#### Scenario: El changelog v2 registra la asimetria

- Dado la seccion `## v2` de `Docs/CONTRACT-CHANGELOG.md`
- Cuando se lee su nota de "Asimetria de determinismo"
- Entonces indica que `IClinicalResponder.Respond` es determinista en `Handled` y `Reply.Text`, frente a `IDialogueGenerator.Generate` que no lo es

### Requirement: Conformidad con `ClinicalResponderContract`

Toda implementacion real de `IClinicalResponder` en `Runtime/<Modulo>/` y su doble determinista
en `Runtime/<Modulo>/Fakes/` DEBEN heredar de `NpcAi.Core.Tests.ClinicalResponderContract` y
pasar el 100% de sus `[Test]` heredados, sin escena de Unity ni entorno de VR. Las pruebas que
exigen `Handled == true` DEBEN usar `Assume.That(IsReady)` para no fallar contra un doble no
listo.

#### Scenario: La suite contractual pasa para el doble y la implementacion real

- Dado el ensamblado de pruebas del modulo que implementa `IClinicalResponder`
- Cuando se ejecutan las pruebas derivadas de `ClinicalResponderContract`
- Entonces el 100% resulta en verde para el doble determinista y para la implementacion real

## Trazabilidad (requisito -> prueba)

Trazabilidad **hacia adelante**: `ClinicalResponderContract` y los casos nuevos de
`ContractTypeTests` los crea la fase apply de este cambio; aun no existen.

| Requisito | Prueba(s) previstas |
|---|---|
| Extension aditiva del contrato a v2 | `ContractTypeTests` (casos nuevos, sin tocar los de v1); `ContractVersionChangelogTests` (`Version == 2` <-> `## v2`); `CoreAssemblyPurityTests` |
| `ClinicalCaseId` es un identificador estable sobre string | `ContractTypeTests` — casos nuevos de `ClinicalCaseId` (normalizacion, `None`, `IsNone`, igualdad `Ordinal`, `GetHashCode()` de `None` == `0`) |
| `ClinicalResponse` lleva la senal de enrutado | `ContractTypeTests` — casos nuevos de `ClinicalResponse` (`NoAplica.Handled == false`) |
| `IsReady` y `AssignCase` son seguros y deterministas | `ClinicalResponderContract.Reporta_si_esta_listo_sin_lanzar`, `...AssignCase_es_idempotente_con_el_mismo_par`, `...AssignCase_con_caso_desconocido_no_lanza_y_deja_no_listo` |
| `Respond` nunca lanza y degrada a `NoAplica` | `ClinicalResponderContract.Sin_caso_asignado_Respond_devuelve_NoAplica`, `...Respond_no_lanza_en_ningun_estado` |
| Una respuesta manejada es presentable | `ClinicalResponderContract.Cuando_responde_el_texto_no_es_vacio_y_los_tags_no_son_nulos` |
| `Respond` es determinista (asimetria con `IDialogueGenerator`) | `ClinicalResponderContract.Es_determinista_en_Handled_y_en_el_texto_para_la_misma_entrada`; inspeccion de `Docs/CONTRACT-CHANGELOG.md` `## v2` |
| Conformidad con `ClinicalResponderContract` | Ejecucion de `NpcAi.<M15>.Tests` derivada de `ClinicalResponderContract` contra el doble y la implementacion real |
