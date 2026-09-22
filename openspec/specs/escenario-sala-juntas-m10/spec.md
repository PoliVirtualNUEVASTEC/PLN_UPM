# Especificacion: escenario-sala-juntas-m10

## Purpose

Congela el comportamiento observable de `RequirementsScenarioObjective`, implementacion real
de `IScenarioObjective` (`contrato-nucleo-m0`) para la sala de juntas (M10,
`Runtime/Scenarios/Boardroom/`). Mide `Progress01`/`IsComplete` mezclando tres vias —
cobertura del catalogo de M16 (`RegisterDisclosure`), cierre fiel (`PresentSummary`) y trato
sostenido (`Notify`) — sobre el checklist que resuelve `RequirementCaseId`. `IScenarioObjective`
y `ScenarioObjectiveContract` no cambian, se heredan sin modificar. Fuera de alcance: convertir
voz en `RequirementId` (compositor M11), evaluacion semantica del resumen y perfiles de
receptividad de Boardroom.

## ADDED Requirements

### Requirement: Conformidad con ScenarioObjectiveContract

`RequirementsScenarioObjective` (real) y `ScriptedScenarioObjective` (doble) DEBEN heredar
`ScenarioObjectiveContract` sin modificarla y pasar sus 7 pruebas en verde, ninguna omitida por
`Assume`.

#### Scenario: Paso de la suite heredada en real y doble

- Dado `RequirementsScenarioObjectiveTests` y `ScriptedScenarioObjectiveTests`, cada una
  `: ScenarioObjectiveContract`
- Cuando se ejecuta la bateria heredada en EditMode
- Entonces las 7 pruebas pasan en ambas, ninguna inconclusa por `Assume`

### Requirement: Progreso mezcla tres vias con pesos externos y clamp a 1

`Progress01` DEBE mezclar cobertura, cierre y trato con pesos complementarios que clampan la
suma a exactamente 1. Los pesos y umbrales DEBEN vivir en `Data/Scenarios/Boardroom.asset`,
nunca como constante C# de tuning.

#### Scenario: Las tres completas llevan a 1; cualquiera incompleta queda por debajo

- Dado un caso asignado con checklist cubierto, cierre fiel presentado y trato al maximo
- Cuando se consulta `Progress01`
- Entonces `Progress01 == 1` (tolerancia `1e-4`) e `IsComplete` es verdadero
- Cuando solo dos de las tres vias estan satisfechas
- Entonces `Progress01 < 1`

### Requirement: Renormalizacion de trato sin caso asignado

Mientras `HasCase` sea falso, el trato sostenido DEBE representar el 100% del peso de
`Progress01`.

#### Scenario: Progreso total sin caso por trato solo

- Dado un objetivo recien creado, sin `AssignCase` invocado
- Cuando se notifican suficientes mejoras consecutivas
- Entonces `Progress01` alcanza 1 e `IsComplete` es verdadero

### Requirement: AssignCase resuelve el checklist sin lanzar

`AssignCase(RequirementCaseId)` DEBE dejar `HasCase == true` solo si el cargador inyectado
resuelve un checklist valido para ese id. Un cargador `null`, que lanza, o un id desconocido
DEBEN dejar `HasCase == false` sin propagar excepcion.

#### Scenario: Id conocido carga el checklist; id desconocido no lanza

- Dado un `Func<RequirementCaseId,string>` que resuelve los 4 casos reales
- Cuando se llama `AssignCase` con un id conocido
- Entonces `HasCase` es verdadero y `RequirementCount` refleja el total del caso
- Cuando el id es desconocido, o el cargador es `null` o lanza
- Entonces `HasCase` es falso y no se propaga excepcion

### Requirement: RegisterDisclosure filtra, es idempotente y nunca lanza

`RegisterDisclosure(Core.RequirementResponse)` DEBE acreditar cobertura solo cuando
`Outcome == RequirementOutcome.Revelado` y el `RequirementId` pertenece al caso asignado. Una
segunda llamada con el mismo `RequirementId` NO DEBE sumar credito. NUNCA DEBE lanzar, incluso
antes de `AssignCase` o con `default`.

#### Scenario: Solo Revelado del caso cuenta, una vez; nunca lanza

- Dado un caso asignado
- Cuando se registra un `Outcome != Revelado`, o un `RequirementId` ajeno al caso
- Entonces `RequirementsDisclosed` no cambia
- Cuando se registra dos veces el mismo `RequirementId` revelado, o se llama sin `AssignCase`
  previo, o con `default`
- Entonces cuenta una sola vez y no se lanza excepcion

### Requirement: La via de trato preserva credito con peso bajo (~0.2)

La via de trato DEBE sembrarse llena al llamar `AssignCase`, bajar con cada `Worsened` y
recuperarse con cada `Improved`, con peso bajo (~0.2) fijado en el asset. Sin caso asignado
conserva la racha estilo M9 para no omitir ninguna prueba heredada.

#### Scenario: AssignCase siembra el credito lleno; Worsened lo baja, Improved lo recupera

- Dado un caso recien asignado
- Cuando se consulta la contribucion de trato antes de cualquier `Notify`
- Entonces esta al maximo de su peso
- Cuando se notifica un `Worsened` y luego un `Improved`
- Entonces la contribucion baja y despues se recupera

### Requirement: PresentSummary acredita el cierre solo con coincidencia exacta

`PresentSummary(IReadOnlyCollection<RequirementId>)` DEBE acreditar el cierre si y solo si el
conjunto presentado es exactamente el revelado en la sesion (ni falta ni sobra). Es
sobrescribible, sin efecto antes de `AssignCase`, y NUNCA lanza con `null` o vacio.

#### Scenario: Exactitud acredita, aun con cobertura parcial; faltar o sobrar no acredita

- Dado un caso asignado con un subconjunto de requerimientos revelados
- Cuando se presenta exactamente ese conjunto
- Entonces el cierre contribuye su peso completo aunque la cobertura sea parcial
- Cuando falta un id revelado, o sobra uno que el cliente nunca revelo
- Entonces el cierre no contribuye

#### Scenario: Sobrescribible, sin efecto antes de AssignCase, nunca lanza

- Dado un cierre fiel ya presentado
- Cuando se presenta un conjunto distinto, o se llama antes de `AssignCase`, o con `null`/vacio
- Entonces gana la ultima llamada valida, sin efecto antes de `AssignCase`, sin excepcion

### Requirement: Reset() real, idempotente y en paridad real/doble

`Reset()` DEBE devolver al estado recien construido (`HasCase == false`, `Progress01 == 0`) y
ser idempotente. Real y doble DEBEN converger al mismo estado observable.

#### Scenario: Reset vuelve al estado inicial, idempotente en ambos

- Dado un caso asignado con progreso parcial en las tres vias
- Cuando se llama `Reset()` una o dos veces seguidas
- Entonces `HasCase` vuelve a falso, `Progress01` vuelve a 0, real y doble coinciden

## Trazabilidad (requisito -> prueba)

| Requisito | Prueba(s) |
|---|---|
| Conformidad con ScenarioObjectiveContract | 7 pruebas heredadas, real y doble |
| Progreso mezcla tres vias, pesos externos | `RequirementsProgresoTests` (completas / incompletas) |
| Renormalizacion sin caso asignado | `RequirementsProgresoTests.Sin_caso_trato_solo_completa` |
| AssignCase resuelve checklist sin lanzar | `RequirementChecklistLoaderTests`, `RequirementsSuperficieAditivaTests.AssignCase_*` |
| RegisterDisclosure filtra, idempotente, nunca lanza | `RequirementsSuperficieAditivaTests.RegisterDisclosure_*` |
| Via de trato preserva credito, peso bajo | `RequirementsProgresoTests.Via_de_trato_*` |
| PresentSummary acredita solo con coincidencia exacta | `RequirementsSuperficieAditivaTests.PresentSummary_*` |
| Reset() real, idempotente, en paridad | `ResetParityTests` |

## Nota abierta para sdd-design

El valor exacto del peso de trato (~0.2 confirmado) y su reparto con cobertura/cierre, la
aritmetica precisa de "credito preservado" (cuanto baja un `Worsened`, cuanto recupera un
`Improved`), y el esquema minimo de `RequirementChecklist` quedan para `design.md`, sin reabrir
`proposal.md`.
