# Propuesta: M0 — Puerto de requerimientos de sala de juntas (`IRequirementResponder`)

## Intent

En la sala de juntas el NPC es el **cliente**: ya tiene los requerimientos "en la cabeza"
y el estudiante debe ganarse su confianza para sacárselos. Verificado releyendo
`Data/Corpus/juntas.json` (los 5 bloques de tono de `AportaInformacion`): las frases del
estudiante **confirman o parafrasean** lo que el NPC ya dijo, nunca lo inventan. La
revelación de cada requerimiento está condicionada al estado de `IReceptivityEngine`.

Hoy `NpcAi.Core` no puede expresar eso:

1. Ningún puerto transporta un requerimiento de un caso de sala de juntas.
2. `IClinicalResponder` (v2) es el precedente de forma más cercano, pero **no recibe
   `Receptivity`**: por contrato no puede condicionar la revelación al estado del NPC.
3. `ClinicalResponse` **no expone qué `Hecho.Campo` emparejó** — hueco ya documentado: el
   pseudocódigo de M11 (`2026-09-09-m11-armado-sesion`) llama `m9.RegistrarHechoObtenido(campoDe(clin))`
   sin una implementación limpia de `campoDe`. El DTO nuevo no debe heredar ese hueco.

Este cambio agrega esa superficie de contrato. Es el **primer eslabón** de la cadena
**M0 (este cambio) → M16 (catálogo + respondedor) → M10 (`IScenarioObjective` real de sala
de juntas)**, y el **segundo cambio de contrato** del proyecto: sube `Contract.Version` de
`2` a `3` y estrena la sección `## v3` de `Docs/CONTRACT-CHANGELOG.md`.

## Scope

### In Scope

- Nuevo puerto `IRequirementResponder` en `Runtime/Core/Ports.cs`, espejo de
  `IClinicalResponder` más el parámetro de receptividad:
  `bool IsReady`, `void AssignCase(RequirementCaseId, PersonalityId)`,
  `RequirementResponse Respond(Utterance, IntentResult, Receptivity)`.
- Nuevo enum `RequirementOutcome` en `Runtime/Core/Enums.cs`:
  `NoAplica = 0`, `AunNoRevelado = 1`, `Revelado = 2`. Primer resultado **tri-estado** del
  contrato (todo lo existente es binario). Respeta la regla 2 de v1: el miembro `0` es el
  seguro.
- Nuevo DTO `RequirementResponse` (`readonly struct`) en `Runtime/Core/Dtos.cs`:
  `Outcome` + `Reply` (`NpcReply`) + `RequirementId` (del tipo `RequirementId`, **no**
  `string`), con un estático `RequirementResponse.NoAplica`.
- Nuevo `readonly struct RequirementCaseId` en `Runtime/Core/RequirementCaseId.cs`,
  **espejo exacto de `ClinicalCaseId` / `PersonalityId`**: `Value` normalizado
  (`Trim().ToLowerInvariant()`), `None = default`, `IsNone`, igualdad completa `Ordinal`.
- Nuevo `readonly struct RequirementId` en `Runtime/Core/RequirementId.cs`, **mismo molde
  exacto** (normalizado, `None = default`, `IsNone`, igualdad completa `Ordinal`):
  identifica **un requerimiento dentro de un caso**, no el caso. Los dos identificadores
  viven en archivos separados, igual que `ClinicalCaseId.cs` y `PersonalityId.cs`.
- Subir `Contract.Version` a `3` en `Runtime/Core/Contract.cs`.
- Agregar `## v3 — 2026-09-16 — Puerto de requerimientos de sala de juntas (M16)` a
  `Docs/CONTRACT-CHANGELOG.md`: tipos nuevos, invariantes DEBE/NO DEBE por valor de
  `RequirementOutcome`, y la obligación de determinismo del puerto.
- Nueva clase base de prueba de contrato `Tests/EditMode/Core/RequirementResponderContract.cs`
  (métodos `[Test]` **no abstractos**), ejercida en este mismo cambio contra un stub local
  mínimo, igual que `ClinicalResponderContract` hizo con `RespondedorNoListo`.
- Cobertura aditiva en `Tests/EditMode/Core/ContractTypeTests.cs`: `RequirementCaseId` y
  `RequirementId` (normalización, `None`, `IsNone`, igualdad `Ordinal`, `GetHashCode()` de
  `None` es `0`) — el mismo juego de casos para cada uno, como ya existe para
  `ClinicalCaseId`; `RequirementResponse.NoAplica`; y congelado del enum nuevo (nombre,
  valor, orden, cardinalidad).

### Out of Scope

- **Implementación de M16.** Catálogo, cargador, matcher, respondedor real y su doble son
  el cambio `2026-09-16-m16-catalogo-respondedor-juntas`.
- **El esquema del caso de sala de juntas.** El puerto recibe un `RequirementCaseId`
  (string), no un DTO con el contenido. La tabla de requerimientos y **la receptividad
  mínima por requerimiento** (mecánica Opción B ya decidida) son **dato de M16**
  (`Data/`), no contrato. Así el esquema evoluciona sin tocar `NpcAi.Core`.
- **`IScenarioObjective` no cambia una línea.** El precedente del repo (pseudocódigo de
  M11: `m9.RegistrarHechoObtenido(...)` sobre el tipo **concreto** de M9, no sobre el
  puerto compartido) indica que la señal "qué se registró" viaja por API propia del
  escenario. Si M10 sigue ese patrón se decide en el cambio de M10, no aquí.
- **El enrutado** (quién llama a M16 antes o después de M6 en la sala de juntas). No
  existe hoy un armador de sesión para sala de juntas; es cambio futuro.
- **`Intent` / `Tone` / `Receptivity` no cambian.** El tipo de pregunta de requerimiento
  no se agrega como valor de `Intent`: lo resuelve M16 contra su tabla.
- **M6, M15 y `Runtime/CoreChannels/` intactos.** Este cambio no agrega `EventChannel`.
- **El texto de la frase de desvío** de `AunNoRevelado` (qué dice el NPC) es contenido de
  M16, no del contrato.

## Capabilities

### New Capabilities

- `requerimientos-juntas-m0` (nuevo, `openspec/specs/requerimientos-juntas-m0/spec.md`, se
  crea al archivar): formaliza el puerto `IRequirementResponder`, el enum
  `RequirementOutcome` y los DTO `RequirementCaseId` / `RequirementId` /
  `RequirementResponse`, con invariantes DEBE/NO DEBE y trazabilidad a
  `RequirementResponderContract` y `ContractTypeTests`.

### Modified Capabilities

- `contrato-nucleo-m0`: pasa de 8 puertos a 9, de 7 DTO a 10 (`+RequirementCaseId`,
  `+RequirementId`, `+RequirementResponse`) y de 4 enums a 5 (`+RequirementOutcome`).
  `Contract.Version` `2 → 3`. Todo lo previo queda **byte-idéntico**: este cambio es
  puramente aditivo.

## Approach

**Aditivo, nunca destructivo.** No se toca ningún enum, DTO ni puerto de v1/v2.
`ContractTypeTests` recibe solo casos nuevos, con una única excepción esperada: el pin
literal de versión (`Version_del_contrato_es_dos`, hoy `Assert.AreEqual(2, Contract.Version)`)
se renombra y actualiza a la nueva versión, exactamente como hizo el cambio v1→v2 con su
propio pin. Ese test existe justamente para forzar esta edición en cada bump; no es una
excepción al principio aditivo, es su mecanismo de verificación.

**Receptividad como parámetro, no como dependencia inyectada.** El 100% de los puertos
actuales de `NpcAi.Core` reciben datos por valor y ninguno mantiene referencia viva a otro
puerto (`IClinicalResponder` recibe `IntentResult`, no `IIntentClassifier`;
`IScenarioObjective.Notify` recibe `ReceptivityChange`, no `IReceptivityEngine`). El
gating queda como un quinto dato determinista de entrada, sin referencia de ensamblado a
`NpcAi.Receptivity`.

**Tri-estado explícito en vez de `bool`.** `RequirementOutcome` separa tres situaciones que
un `Handled` binario confunde:

| `Outcome` | Significado de contrato |
|---|---|
| `NoAplica` | El turno no es de requerimientos: el llamador enruta a M6. `Reply` sin garantías y `RequirementId` DEBE ser `RequirementId.None` |
| `AunNoRevelado` | La pregunta emparejó un requerimiento, pero la receptividad actual no alcanza su umbral. `Reply.Text` DEBE ser no vacío (desvío, nunca silencio) y `RequirementId` DEBE venir **poblado** |
| `Revelado` | El NPC dice el hecho. `Reply` va tal cual a M8 y `RequirementId` DEBE venir poblado, identificando el requerimiento revelado |

`AunNoRevelado` le da a M10 la telemetría pedagógica que hoy no existe: distinguir "iba por
buen camino pero no se ganó la confianza" de "no dijo nada relevante". Por eso
`RequirementId` viene poblado también en ese estado: `Outcome != NoAplica` ⇒
`RequirementId.IsNone == false`. La regla que impide filtrar el hecho no ganado es
**textual, no estructural**: el `RequirementId` identifica el tema, y el contenido del
requerimiento solo aparece en `Reply.Text` cuando `Outcome == Revelado`.

**`RequirementId` como tipo propio, no `string`.** Corrige de raíz el hueco `campoDe(clin)`
(M10 sabrá **qué** requerimiento se reveló sin inferirlo de `EmotionTag` / `AnimationCue`) y
mantiene la política de identificadores del repo: todo identificador de dominio de
`NpcAi.Core` es un `readonly struct` normalizado con igualdad `Ordinal`, nunca un `string`
crudo sin invariantes (`PersonalityId`, `ClinicalCaseId`). El costo es un tipo nuevo más;
el beneficio es que `RequirementId.None` es un centinela tipado y la comparación de
requerimientos no depende de que cada consumidor recuerde normalizar.

**Nota de C#: el campo `RequirementId` del DTO y el tipo `RequirementId` pueden llamarse
igual.** No es un conflicto: es la regla de "nombres simples idénticos a nombres de tipo"
(*Color Color*), y **el repo ya la usa en el contrato congelado de v1** —
`Runtime/Core/Dtos.cs:23-24` declara `public readonly Intent Intent;` y
`public readonly Tone Tone;` dentro de `IntentResult`, y la línea 38 usa `Intent.Desconocida`
(acceso estático por nombre de tipo) en el mismo archivo. Precaución operativa, no de
contrato: dentro de miembros **de instancia** de `RequirementResponse`, usar `this.RequirementId`
cuando se quiera el campo, para que el lector no dude.

**El caso viaja como id, no como objeto** — misma decisión que `ClinicalCaseId` y
`PersonalityId`. Pasar de 3 a 20 casos de sala de juntas agrega archivos en `Data/` de M16
y no toca `NpcAi.Core`.

**Determinismo obligatorio, como M15.** `Respond` DEBE ser determinista en `Outcome`,
`Reply.Text` y `RequirementId` para la misma tupla
`(RequirementCaseId, PersonalityId, Utterance, IntentResult, Receptivity)`. Un requerimiento
de negocio no cambia de redacción entre turnos: M16 usa plantillas, no Markov.

## Affected Areas

| Área | Impacto | Descripción |
|---|---|---|
| `Runtime/Core/Ports.cs` | Modificado (aditivo) | `+ interface IRequirementResponder` |
| `Runtime/Core/Enums.cs` | Modificado (aditivo) | `+ enum RequirementOutcome` (centinela en `0`) |
| `Runtime/Core/Dtos.cs` | Modificado (aditivo) | `+ struct RequirementResponse` |
| `Runtime/Core/RequirementCaseId.cs` | Nuevo | `readonly struct` espejo de `ClinicalCaseId` (identifica el **caso**) |
| `Runtime/Core/RequirementId.cs` | Nuevo | `readonly struct` con el mismo molde (identifica un **requerimiento** dentro del caso) |
| `Runtime/Core/Contract.cs` | Modificado | `Version` `2 → 3` |
| `Docs/CONTRACT-CHANGELOG.md` | Modificado | `+ ## v3` |
| `Tests/EditMode/Core/RequirementResponderContract.cs` | Nuevo | Base de prueba de contrato del puerto |
| `Tests/EditMode/Core/ContractTypeTests.cs` | Modificado (aditivo) | Casos nuevos para los 3 tipos nuevos |
| `Runtime/Core/` (4 enums previos, 8 puertos, 7 DTO) | Sin cambio | Byte-idénticos |
| `Runtime/CoreChannels/` | Sin cambio | No se agrega `EventChannel` |
| M1–M15 existentes | Sin cambio | Ningún módulo referencia `IRequirementResponder` todavía |

## Risks

| Riesgo | Prob. | Mitigación |
|---|---|---|
| El diff supera el precedente v2 (~130 líneas): enum tri-estado + su congelado, **dos** identificadores nuevos con su juego completo de casos en `ContractTypeTests`, y los escenarios extra de la base de prueba. Estimación de orden: **~2× el precedente (~250-300 líneas)**, todavía dentro de las 400 del presupuesto pero con menos margen que antes de fijar `RequirementId` como tipo propio | Media-Alta | Presupuesto de revisión de la sesión: 400 líneas. `sdd-tasks` DEBE emitir el pronóstico explícito con el conteo real; si sale Alto, se parte en PRs encadenados (corte natural: PR1 = identificadores + enum + DTO + `ContractTypeTests`; PR2 = puerto + base de contrato + `Version`/changelog). **Nota, no bloqueante** |
| `ContractVersionChangelogTests` falla si se sube `Contract.Version` sin `## v3` (o viceversa) | Alta si se hace en pasos sueltos | El bump y la entrada del changelog van en **el mismo commit** |
| El puerto se diseña sin M16 delante y luego no calza | Media | `RequirementResponderContract` se escribe en este cambio y se ejerce con un stub local, así el puerto se valida como usable antes de mergear. Corregirlo después sería otro cambio de contrato (v4), no una edición silenciosa |
| Nombres desalineados con lo que M16 decida para su carpeta de datos (`Data/...`) | Media | `RequirementCaseId` ya se eligió como nombre de dominio (no de escenario) justo para no atarse a la carpeta; el cambio de M16 alinea su `Data/` con este id |
| Primer enum agregado después del congelado de v1: invita a creer que los enums son extensibles a voluntad | Baja | El changelog v3 registra que **agregar** un enum es aditivo, pero renombrar/reordenar/cambiar valor de uno existente sigue siendo ruptura |
| Ampliar `NpcAi.Core` invita a meter también la señal hacia `IScenarioObjective` o el enrutador | Media | Out of Scope explícito + `Success Criteria` que acota el diff |
| `Contract.Version` es `const`: un consumidor no recompilado reporta `2` | Baja (conocida, aceptada en v1) | Sin cambio de política; se hereda el riesgo aceptado |

## Rollback Plan

`IRequirementResponder`, `RequirementOutcome`, `RequirementResponse`, `RequirementCaseId` y
`RequirementId` son adiciones puras. Revertir los commits de este cambio:

- Baja `Contract.Version` a `2` y quita la sección `## v3` del changelog —
  `ContractVersionChangelogTests` vuelve a verde con `Version == 2`.
- Quita el puerto, el enum, los dos identificadores, el DTO y la base de prueba (los dos
  archivos de identificador son nuevos: se borran enteros, no se editan).
- Ningún módulo depende de `IRequirementResponder` por nombre todavía (M16 y M10 no están
  mergeados), así que nada más se rompe. No hay migración de datos.

## Dependencies

- `contrato-nucleo-m0` y `respuesta-clinica-m0`, archivados y estables: este cambio los
  extiende y copia su forma.
- **Co-revisión de M0 (regla 2 del `CLAUDE.md` del paquete, proceso no contenido técnico):**
  antes del merge lo revisa el otro dueño compartido de M0, **Luis Miguel Cañaveral
  Restrepo**, o, en su defecto, el asesor **Luis Fernando González Alvarán**. Sin ventana
  fija ni quorum de todos los dueños.
- Ninguna dependencia de código nuevo. Los cambios de **M16 y M10 dependen de este**, no al
  revés.

## Success Criteria

- [ ] `Contract.Version == 3` y `Docs/CONTRACT-CHANGELOG.md` tiene `## v3`;
      `ContractVersionChangelogTests` en verde.
- [ ] `CoreAssemblyPurityTests` sigue en verde: los 5 tipos nuevos son C# puro, sin
      `UnityEngine` ni `NpcAi.*` ajeno (`noEngineReferences: true` intacto).
- [ ] `ContractTypeTests` en verde con los casos nuevos — el juego completo para
      **`RequirementCaseId` y para `RequirementId`** (normalización, `None`, `IsNone`,
      igualdad `Ordinal`, `GetHashCode()` de `None` es `0`), `RequirementResponse.NoAplica`
      y el congelado de `RequirementOutcome` — sin modificar ningún caso de v1/v2 salvo el
      pin literal de versión (`Version_del_contrato_es_dos` → su equivalente para `3`),
      igual que el cambio v1→v2 editó el suyo.
- [ ] `RequirementResponderContract` compila y pasa contra un stub mínimo local; queda
      lista para que M16 (real y doble) la herede.
- [ ] El contrato declara una regla DEBE/NO DEBE por cada valor de `RequirementOutcome`,
      incluidas la de `AunNoRevelado` (`Reply.Text` no vacío) y la de `RequirementId`
      poblado siempre que `Outcome != NoAplica`.
- [ ] El diff no toca nada fuera de `Runtime/Core/`, `Docs/CONTRACT-CHANGELOG.md`,
      `Tests/EditMode/Core/` y `openspec/`. Cero archivos de `Runtime/ClinicalResponse/`,
      `Runtime/Scenarios/` o `Runtime/Nlu/`.
- [ ] Sin `Debug.Log` en el código agregado.
- [ ] Revisado antes del merge por el otro dueño compartido de M0 o el asesor (regla 2).

## Decisiones del usuario (confirmadas 2026-09-16)

1. Cadena de tres cambios encadenados: **M0 (este) → M16 → M10**. Este cambio es SOLO el
   puerto de contrato.
2. El puerto nuevo va como **cambio SDD de contrato aparte**, antes de proponer M16
   (regla 2 del `CLAUDE.md` del paquete).
3. Nombre del puerto: **`IRequirementResponder`** (nombre de dominio, no de escenario,
   mismo nivel que `IClinicalResponder`).
4. Nombre del ID: **`RequirementCaseId`** (sobre `BoardroomCaseId` y `BusinessCaseId`;
   `business case` es ambiguo).
5. DTO **tri-estado explícito** (`NoAplica` / `AunNoRevelado` / `Revelado`), con el
   requerimiento revelado expuesto explícitamente.
6. **Receptividad como parámetro** de `Respond`, no inyección de `IReceptivityEngine`.
7. Mecánica de revelación = **Opción B** (cada requerimiento tiene su receptividad mínima).
   El contrato solo la habilita; la tabla de umbrales es dato de M16.
8. La **clase de prueba de contrato compartida va en este cambio**, no en el de M16
   (precedente exacto de `ClinicalResponderContract`).
9. **Alcance puramente aditivo**: no se toca `IScenarioObjective` ni ningún puerto
   existente.

## Decisiones cerradas en la ronda de propuesta (Jefferson, 2026-09-16)

La ronda de preguntas de propuesta se cerró el mismo día. **No queda ninguna pregunta
abierta**: las tres respuestas ya están incorporadas arriba (Scope, Approach, Affected
Areas, Risks y Success Criteria).

10. **`RequirementId` viene poblado también en `AunNoRevelado`.** Invariante de contrato:
    `Outcome != NoAplica` ⇒ `RequirementId.IsNone == false`. M10 y la bitácora M13 reciben
    la señal de "identificó el tema correcto" aunque el hecho todavía no se haya revelado.
    Lo que protege el hecho no ganado es el texto (`Reply.Text` solo lo dice en
    `Revelado`), no la ausencia del identificador.
11. **`RequirementId` es un `readonly struct` propio, no un `string`.** Archivo nuevo
    `Runtime/Core/RequirementId.cs`, mismo molde exacto que `RequirementCaseId` /
    `ClinicalCaseId` / `PersonalityId` (valor normalizado, `None = default`, `IsNone`,
    igualdad completa `Ordinal`). Este cambio agrega entonces **dos** identificadores:
    `RequirementCaseId` (el caso) y `RequirementId` (un requerimiento dentro del caso).
    **Sin conflicto de nombres**: que el campo del DTO se llame igual que su tipo es la
    regla *Color Color* de C#, ya usada en el contrato congelado de v1
    (`Runtime/Core/Dtos.cs:23-24`: `public readonly Intent Intent;` y
    `public readonly Tone Tone;`).
12. **`AssignCase` conserva `PersonalityId`.** La firma queda
    `AssignCase(RequirementCaseId, PersonalityId)`, espejo de `IClinicalResponder`: la
    personalidad matiza cómo responde el NPC, igual que en la sala de triaje.
