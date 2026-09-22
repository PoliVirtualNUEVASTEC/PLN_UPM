# Especificacion: requerimientos-juntas-m0

## Purpose

Congela `IRequirementResponder` y los tipos que el contrato v3 de `NpcAi.Core` agrega para
que M16 (catalogo + respondedor de sala de juntas) exista como modulo de primera clase. El
puerto transporta la respuesta del NPC-cliente sobre un requerimiento de negocio, gateada por
`Receptivity` (quinto dato de entrada, no dependencia inyectada), y expone que requerimiento
emparejo via `RequirementId` — corrige el hueco `campoDe(clin)` que dejo `ClinicalResponse`.
Cambio puramente aditivo: nada de v1/v2 cambia. El esquema del caso y los umbrales de
receptividad por requerimiento son dato de M16 (`Data/`), no contrato. Verificacion:
`RequirementResponderContract` y los casos nuevos de `ContractTypeTests`, ambos hacia
adelante (aun no existen).

## ADDED Requirements

### Requirement: Extension aditiva del contrato a v3

`NpcAi.Core` DEBE agregar exactamente el puerto `IRequirementResponder`, el enum
`RequirementOutcome`, el DTO `RequirementResponse` y los `readonly struct` `RequirementCaseId`
/ `RequirementId`. Los 4 enums, 7 DTO y 8 puertos previos NO DEBEN cambiar (nombre, valor,
orden, cardinalidad, firma). `Contract.Version` DEBE pasar de `2` a `3` y
`Docs/CONTRACT-CHANGELOG.md` DEBE ganar `## v3`. Los 5 tipos nuevos DEBEN ser C# puro;
`NpcAi.Core` DEBE mantener `noEngineReferences: true` y cero referencias `NpcAi.*` ajenas.

#### Scenario: Solo se anaden los 5 tipos nuevos

- Dado el ensamblado `NpcAi.Core` tras el cambio
- Cuando se comparan sus miembros contra v2
- Entonces v1/v2 quedan identicos y solo aparecen los 5 tipos nuevos, sin `UnityEngine*` ni
  `NpcAi.*` ajeno en ninguno

#### Scenario: La version ata al changelog

- Dado `Contract.Version`
- Cuando se compara con el mayor `## v<N>` de `Docs/CONTRACT-CHANGELOG.md`
- Entonces ambos son `3`

### Requirement: `RequirementCaseId` y `RequirementId` son identificadores espejo de `PersonalityId`

Ambos DEBEN ser `readonly struct` sobre `string`: `Value` normalizado
(`Trim().ToLowerInvariant()`); `None = default`; `IsNone == true` solo para `None`;
`GetHashCode()` de `None` DEBE ser `0`; igualdad completa y `Ordinal` (`Equals`,
`GetHashCode`, `operator ==`, `operator !=`). Son identidades independientes — uno identifica
el caso, el otro un requerimiento dentro del caso — y NO DEBEN confundirse por tipo.

#### Scenario: Normalizacion, `None` e igualdad Ordinal

- Dados `new RequirementCaseId(" Req-01 ")` y `new RequirementId(" precio ")`, variantes con
  espacios/mayusculas de cada uno, y sus `.None`
- Cuando se comparan `Value`, `IsNone` y `GetHashCode()`
- Entonces cada uno normaliza igual, `None` hashea a `0`, y la igualdad es `Ordinal` por valor
  normalizado

### Requirement: `RequirementOutcome` es tri-estado; `RequirementId` va poblado salvo en `NoAplica`

`RequirementResponse` DEBE exponer `Outcome`, `Reply` (`NpcReply`) y `RequirementId`, con un
estatico `RequirementResponse.NoAplica`. Invariante: `Outcome != RequirementOutcome.NoAplica`
⇒ `RequirementId.IsNone == false`.

| `Outcome` | Invariante |
|---|---|
| `NoAplica` | `RequirementId == RequirementId.None`; `Reply` sin garantias |
| `AunNoRevelado` | `RequirementId` poblado; `Reply.Text` NO DEBE ser vacio ni solo espacios |
| `Revelado` | `RequirementId` poblado; `Reply` va tal cual a M8 |

#### Scenario: Cada `Outcome` cumple su fila de la tabla

- Dadas tres respuestas, una por cada valor de `Outcome`
- Cuando se leen `RequirementId` y `Reply` de cada una
- Entonces cada una cumple exactamente su fila de la tabla, incluido
  `NoAplica.RequirementId.IsNone == true`

### Requirement: El puerto es seguro y determinista en cualquier estado

Leer `IsReady` NO DEBE lanzar; DEBE ser `false` hasta que `AssignCase` vincule un
`RequirementCaseId` existente. `AssignCase(RequirementCaseId, PersonalityId)` con el mismo par
DEBE ser determinista e idempotente; con un caso desconocido NO DEBE lanzar y DEBE dejar
`IsReady == false`. `Respond(Utterance, IntentResult, Receptivity)` NO DEBE lanzar en ningun
estado (sin `AssignCase`, con entradas `default`, `"!!!???"` o de 5000 caracteres). Para la
misma tupla `(RequirementCaseId, PersonalityId, Utterance, IntentResult, Receptivity)` DEBE
devolver el mismo `Outcome`, `Reply.Text` y `RequirementId`.

#### Scenario: Caso desconocido no lanza; `AssignCase` es idempotente

- Dado `AssignCase` con un `RequirementCaseId` inexistente, y por separado el mismo par
  llamado dos veces
- Cuando se leen `IsReady` y el resultado de `Respond`
- Entonces el caso desconocido no lanza y deja `IsReady == false`; el par repetido deja
  `IsReady` y la respuesta iguales antes y despues

#### Scenario: Entradas atipicas no lanzan y la misma tupla es determinista

- Dado un respondedor en cualquier estado con entradas `default`, `"!!!???"` y de 5000
  caracteres, y por separado la misma tupla `Respond` llamada varias veces
- Cuando se ejecutan esas llamadas
- Entonces ninguna lanza, y para la tupla repetida `Outcome`, `Reply.Text` y `RequirementId`
  son identicos en todas las respuestas

### Requirement: Conformidad con `RequirementResponderContract`

Toda implementacion real de `IRequirementResponder` en `Runtime/<Modulo>/` y su doble
determinista en `Runtime/<Modulo>/Fakes/` DEBEN heredar de
`NpcAi.Core.Tests.RequirementResponderContract` y pasar el 100% de sus `[Test]` heredados, sin
escena de Unity ni entorno de VR.

#### Scenario: La suite contractual pasa para el doble y la implementacion real

- Dado el ensamblado de pruebas del modulo que implementa `IRequirementResponder`
- Cuando se ejecutan las pruebas derivadas de `RequirementResponderContract`
- Entonces el 100% resulta en verde para el doble determinista y para la implementacion real

## Trazabilidad (requisito -> prueba)

Trazabilidad **hacia adelante**: `RequirementResponderContract` y los casos nuevos de
`ContractTypeTests` los crea la fase apply de este cambio; aun no existen.

| Requisito | Prueba(s) previstas |
|---|---|
| Extension aditiva del contrato a v3 | `ContractTypeTests` (casos nuevos); `ContractVersionChangelogTests` (`Version == 3` <-> `## v3`); `CoreAssemblyPurityTests` |
| `RequirementCaseId` / `RequirementId` espejo de `PersonalityId` | `ContractTypeTests` — juego completo para cada uno (normalizacion, `None`, `IsNone`, igualdad `Ordinal`, `GetHashCode()` de `None` == `0`) |
| `RequirementOutcome` tri-estado y `RequirementId` poblado | `ContractTypeTests` (congelado del enum, `RequirementResponse.NoAplica`); `RequirementResponderContract` (escenarios `AunNoRevelado` / `Revelado`) |
| El puerto es seguro y determinista | `RequirementResponderContract.AssignCase_con_caso_desconocido_*`, `...AssignCase_es_idempotente_*`, `...Respond_no_lanza_*`, `...Es_determinista_*` |
| Conformidad con `RequirementResponderContract` | Ejecucion de `NpcAi.<M16>.Tests` derivada de `RequirementResponderContract` contra el doble y la implementacion real |
