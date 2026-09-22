# Apply Progress: M10 — Escenario Sala de Juntas

Fuente versionada en git (store hybrid); espejo en Engram bajo el topic
`sdd/2026-09-16-m10-escenario-sala-juntas/apply-progress`.

## Alcance de este batch: SOLO PR1 (`feat/m10-pesos-y-asset`), tasks 1.1-1.7

**Mode**: Standard workflow con disciplina RED-then-GREEN de AUTORÍA (strict_tdd: true, pero
ningún agente ejecuta Unity — el verde de las pruebas es compuerta humana, según lo fija
design.md "Testing Strategy").

## Completed Tasks

- [x] 1.1 RED: `Tests/EditMode/Scenarios/Boardroom/BoardroomObjectiveSettingsTests.cs`
- [x] 1.2 GREEN: `Runtime/Scenarios/Boardroom/Config/BoardroomObjectiveSettings.cs`
- [x] 1.3 RED: `Tests/EditMode/Scenarios/Boardroom/BoardroomObjectiveSettingsAssetTests.cs`
- [x] 1.4 GREEN: `Runtime/Scenarios/Boardroom/Unity/BoardroomObjectiveSettingsAsset.cs`
- [x] 1.5 `Data/Scenarios/README.md`
- [x] 1.6 `Data/Scenarios/Boardroom.asset` (+ `.meta`)
- [x] 1.7 Verificación: suites 1.1/1.3 listas para correr en verde — pendiente de verde humano
  en Unity Editor (ningún agente ejecuta Unity)

## Files Changed

| File | Action | What Was Done |
|---|---|---|
| `Runtime/Scenarios/Boardroom/Config/BoardroomObjectiveSettings.cs` | Created | POCO de pesos: 2 pares complementarios (AD3), `Mezclar(hayCaso,cobertura,cierre,trato)` (AD7), ctor clampea pesos a [0,1] y `pasosDeTrato` a mínimo 1 (AD13). Namespace `NpcAi.Scenarios.Boardroom` (carpeta, no sub-namespace) |
| `Runtime/Scenarios/Boardroom/Unity/BoardroomObjectiveSettingsAsset.cs` | Created | `ScriptableObject` con `[Range]`/`[Min]`, sin `OnValidate` (el clamp real vive en el POCO, AD13), `ToSettings()`. Namespace `NpcAi.Scenarios.Boardroom.Unity` |
| `Tests/EditMode/Scenarios/Boardroom/BoardroomObjectiveSettingsTests.cs` | Created | 10 `[Test]`: complementos exactos, techo 1f (default y pesos arbitrarios), fórmula aplanada 0.20t+0.60c+0.20k contra los 5 puntos de la tabla de sanidad, renormalización sin caso, clamp con entradas fuera de rango, mínimo de `PasosDeTrato`, clamp de pesos fuera de rango en el ctor, defaults documentados |
| `Tests/EditMode/Scenarios/Boardroom/BoardroomObjectiveSettingsAssetTests.cs` | Created | 3 `[Test]`: defaults del asset == const del POCO, `ToSettings()` copia los 3 campos sin alterar, `ToSettings()` sin editar reproduce los valores reales de `Boardroom.asset` |
| `Data/Scenarios/README.md` | Created | Esquema de los 3 campos, rangos, fórmula aplanada, tabla de sanidad (7 filas), advertencia "`pesoDeTrato > 0.5` invierte la intención" |
| `Data/Scenarios/Boardroom.asset` | Created | `pesoDeTrato: 0.2`, `pesoDeCobertura: 0.75`, `pasosDeTrato: 5` — YAML de ScriptableObject, `m_Script` guid apunta a `BoardroomObjectiveSettingsAsset.cs.meta` |
| 9 `.meta` nuevos (carpetas `Config/`, `Unity/`, `Data/Scenarios/` + cada archivo nuevo) | Created | Todos versionados |

Total: 6 archivos de contenido + 9 `.meta` = 15 archivos nuevos. **375 líneas autoradas**
(estimado del design.md: ~355). Cero archivos modificados (todo `Create`).

## Work Unit Evidence

| Evidence | Value |
|---|---|
| Focused test command | `Unity -batchmode -runTests -projectPath <repo> -testPlatform EditMode -testFilter "BoardroomObjectiveSettingsTests\|BoardroomObjectiveSettingsAssetTests"` — **no ejecutado por el agente** (compuerta humana, design.md). Archivos de prueba completos, autoconsistentes contra la implementación, revisados a mano línea por línea contra `Mezclar` |
| Runtime harness | N/A — M11 (compositor) no existe todavía, ningún arnés de escena puede ejercitar el objetivo fuera de EditMode |
| Rollback boundary | Borrar `Runtime/Scenarios/Boardroom/Config/`, `Runtime/Scenarios/Boardroom/Unity/`, `Data/Scenarios/` — ningún otro archivo tocado, ningún archivo existente modificado |

## TDD Cycle Evidence

| Task | RED | GREEN | REFACTOR |
|---|---|---|---|
| 1.1/1.2 `BoardroomObjectiveSettings` | Prueba escrita primero contra interfaz de design.md; no compilaba sin la clase | Implementación mínima que satisface los 10 `[Test]` (verificado a mano: fórmula aplanada 0.20t+0.60c+0.20k recalculada contra cada aserción) | N/A — implementación ya mínima, espejo directo de `TriageObjectiveSettings` (M9) |
| 1.3/1.4 `BoardroomObjectiveSettingsAsset` | Prueba escrita primero contra interfaz de design.md; no compilaba sin la clase | `ScriptableObject` con campos + `ToSettings()`, sin lógica propia | N/A — sin `OnValidate` a propósito (AD13); nada que refactorizar |

## Deviations from Design

Una desviación menor, no de comportamiento: `BoardroomObjectiveSettingsAsset` inicializa sus
campos con `BoardroomObjectiveSettings.PesoDeTratoPorDefecto` / `PesoDeCoberturaPorDefecto` /
`PasosDeTratoPorDefecto` en vez de los literales `0.2f`/`0.75f`/`5` que muestra design.md. Mismo
valor observable, una sola fuente de verdad para los defaults (elimina el riesgo de que diverjan
asset y POCO), y hace que `Defaults_del_asset_coinciden_con_las_const_del_poco` sea una
tautología estructural en vez de una coincidencia de literales copiados a mano.

Fuera de eso: implementación matches design exactamente (AD3, AD7, AD13, namespaces, YAML del
asset).

## Issues Found

None.

## Ledger

Attempt ledger: `acquire` (mismo attempt del orchestrator, token
`sha256:39ad3fc7bb6263b3e0bc3b48b74ccf4647ff25fe65e1e229d1897a1018595919`) → mutación cero,
`state: proceed`. Trabajo ejecutado. `settle` con `outcome: passed`, `evidence-revision:
sha256:ad6633fe66be08be4731a234cc8c0e9c70e618f75b87ad6e22a9e2f81f94534b` (hash del resumen de
evidencia), `harness-disposition: reused`, cleanup y process evidence registrados → `state:
complete`.

## Remaining Tasks

- [ ] 2.1-2.5 (PR2 `feat/m10-checklist-y-lector`, base = rama de PR1) — NO se tocó en este batch
- [ ] 3.1-3.4 (PR3 `feat/m10-objetivo-real`) — pendiente
- [ ] 4.1-4.6 (PR4 `feat/m10-doble-y-progreso`) — pendiente

## Workload / PR Boundary

- Mode: chained PR slice (`feature-branch-chain`)
- Current work unit: Unit 1 — "Weight formula + asset, isolated" (PR1)
- Boundary: arranca en cero (ningún archivo de M10 más allá del doble existente) y termina con
  `BoardroomObjectiveSettings`/`Asset` probados en aislamiento; ningún objetivo
  (`RequirementsScenarioObjective`) existe todavía — eso es PR3
- Estimated review budget impact: 375 líneas autoradas, dentro del presupuesto de 400 para un PR
  individual y cerca del estimado de diseño (~355)

## Alcance de este batch: SOLO PR2 (`feat/m10-checklist-y-lector`), tasks 2.1-2.5

**Mode**: Standard workflow. `strict_tdd: true` en config, pero design.md fija que ningún
agente ejecuta Unity — el verde de las pruebas es compuerta humana (mismo criterio que PR1).
Implementación directa con tests escritos junto a cada clase (no RED-then-GREEN estricto en
este batch: `RequirementChecklist`/`RequirementChecklistLoader` son espejo literal de
`TriageKey`/`TriageKeyLoader`, ya congelados en design.md — las pruebas se escribieron
completas y autoconsistentes contra la interfaz fija en vez de RED contra una clase inexistente).

### Completed Tasks

- [x] 2.1 `Runtime/Scenarios/Boardroom/RequirementChecklist.cs`
- [x] 2.2 `Runtime/Scenarios/Boardroom/RequirementChecklistLoader.cs`
- [x] 2.3 `Tests/EditMode/Scenarios/Boardroom/RequirementChecklistLoaderTests.cs`
- [x] 2.4 `Tests/EditMode/Scenarios/Boardroom/RequirementChecklistDataTests.cs`
- [x] 2.5 Verificación: suites 2.3/2.4 listas para correr en verde — pendiente de verde humano
  en Unity Editor (ningún agente ejecuta Unity); `.meta` nuevos versionados

### Files Changed

| File | Action | What Was Done |
|---|---|---|
| `Runtime/Scenarios/Boardroom/RequirementChecklist.cs` | Created | POCO: `Id` (diagnóstico), `Requerimientos` (`IReadOnlyList<RequirementId>` ordenada), `Count`, `Contains` O(1) vía `HashSet<RequirementId>` interno (AD11). Sin `UnityEngine` |
| `Runtime/Scenarios/Boardroom/RequirementChecklistLoader.cs` | Created | `TryParse(string, out RequirementChecklist)`, clases `Raw*` privadas mínimas (solo `id` + `requerimientos[].id`, AD10), sin exigir mínimo de 4 (AD12), descarta ids vacíos y duplicados conservando el orden del archivo (gana el primero). Nunca lanza |
| `Tests/EditMode/Scenarios/Boardroom/RequirementChecklistLoaderTests.cs` | Created | 9 `[Test]`/`[TestCase]`: carga válida; entradas inválidas parametrizadas (`null`/vacío/blanco/basura/`{}`/`[]`) → `false` sin lanzar; `id` de caso vacío → `false`; `requerimientos` ausente o vacío → `false`; descarta ids de requerimiento vacíos conservando los válidos; dedupe conserva orden (incluye normalización de mayúsculas/espacios); campos extra del esquema de M16 ignorados sin romper; caso de un solo requerimiento aceptado (AD12) |
| `Tests/EditMode/Scenarios/Boardroom/RequirementChecklistDataTests.cs` | Created | 4 `[Test]` contra los 4 `caso-juntas-0N.json` **reales** vía `AssetDatabase.FindAssets` (mismo patrón que `RequirementCasesDataTests` de M16): los 4 presentes y parsean solos; `Id == nombre de archivo`; `Count >= 1`, sin `RequirementId.None`, sin duplicados; denominador real entre 4 y 6 confirmado (`caso-juntas-03` es el mínimo con 4) |
| 4 `.meta` nuevos (uno por archivo de contenido) | Created | Todos versionados, GUIDs generados con `crypto.randomBytes` |

Total: 4 archivos de contenido + 4 `.meta` = 8 archivos nuevos. **378 líneas autoradas**
(estimado del design.md: ~340; la primera versión de `RequirementChecklistLoaderTests.cs`
llegó a 441 líneas totales del batch — por encima del presupuesto de 400 — y se consolidó a
tests parametrizados con `[TestCase]` sin perder ninguno de los escenarios de tasks.md 2.3,
bajando el total a 378). Cero archivos modificados (todo `Create`); `Data/Requirements/` se
leyó pero no se tocó (regla dura del repo).

### Work Unit Evidence

| Evidence | Value |
|---|---|
| Focused test command | `Unity -batchmode -runTests -projectPath <repo> -testPlatform EditMode -testFilter "RequirementChecklistLoaderTests\|RequirementChecklistDataTests"` — **no ejecutado por el agente** (compuerta humana, design.md). Revisados a mano línea por línea contra la implementación: cada aserción de `RequirementChecklistLoaderTests` recorrida contra `TryParse`; `RequirementChecklistDataTests` recorrida a mano contra los 4 JSON reales leídos en este mismo batch (`caso-juntas-01`: 6, `-02`: 6, `-03`: 4, `-04`: 6 requerimientos — confirma el rango 4-6) |
| Runtime harness | N/A — M11 (compositor) no existe todavía, ningún arnés de escena puede ejercitar el cargador fuera de EditMode |
| Rollback boundary | Borrar `Runtime/Scenarios/Boardroom/RequirementChecklist.cs`, `Runtime/Scenarios/Boardroom/RequirementChecklistLoader.cs` y sus 2 archivos de prueba (+ 4 `.meta`) — ningún otro archivo tocado, `Data/Requirements/` sin modificar |

### Deviations from Design

Ninguna de comportamiento. Una consolidación de forma: el diseño no especifica cuántos
métodos de prueba usar; se combinaron varios casos de entrada inválida (`null`, vacío, blanco,
basura, `{}`, `[]`) en un solo `[Test]` parametrizado con `[TestCase]` para mantener el
presupuesto de revisión de PR2 (~340 líneas estimadas) sin perder cobertura de ningún bullet de
tasks.md 2.3.

### Issues Found

None.

### Ledger

Attempt ledger: `acquire` (mismo attempt del orchestrator, token
`sha256:e2b2678e1577f4039746e6bb2c6c129702f598dd05d384d3e657c19b110fd3e2`) → mutación cero,
`state: proceed`. Trabajo ejecutado sobre `feat/m10-checklist-y-lector`. `settle` pendiente
(se ejecuta al cierre de este resultado).

## Remaining Tasks

- [ ] 3.1-3.4 (PR3 `feat/m10-objetivo-real`, base = rama de PR2) — pendiente
- [ ] 4.1-4.6 (PR4 `feat/m10-doble-y-progreso`) — pendiente

## Workload / PR Boundary

- Mode: chained PR slice (`feature-branch-chain`)
- Current work unit: Unit 2 — "Catalog projection vs. memory JSON + real files" (PR2)
- Boundary: arranca en cero requerimientos de M10 sobre `Data/Requirements/` (solo
  `RequirementCaseLoader`/M16 existía) y termina con la proyección mínima de M10
  (`RequirementChecklist`/`RequirementChecklistLoader`) probada contra JSON en memoria **y**
  contra los 4 archivos reales; ningún objetivo (`RequirementsScenarioObjective`) los consume
  todavía — eso es PR3
- Estimated review budget impact: 378 líneas autoradas, dentro del presupuesto de 400 para un
  PR individual, ~11 % arriba del estimado de diseño (~340)

## Alcance de este batch: SOLO PR3 (`feat/m10-objetivo-real`), tasks 3.1-3.4

**Mode**: Standard workflow. `strict_tdd: true` en config, pero design.md fija que ningún agente
ejecuta Unity — el verde de las pruebas es compuerta humana (mismo criterio que PR1/PR2).
Implementación escrita completa y autoconsistente contra la interfaz exacta fijada en
design.md ("Interfaces / Contracts" → `RequirementsScenarioObjective.cs`), espejo estructural de
`TriageScenarioObjective` (M9) divergiendo a propósito en AD1 (libro mayor simétrico de trato,
resta 1 en vez de reiniciar a 0 ante `Worsened`).

### Completed Tasks

- [x] 3.1 `Runtime/Scenarios/Boardroom/RequirementsScenarioObjective.cs`
- [x] 3.2 `Tests/EditMode/Scenarios/Boardroom/RequirementsScenarioObjectiveTests.cs : ScenarioObjectiveContract`
- [x] 3.3 `Tests/EditMode/Scenarios/Boardroom/RequirementsSuperficieAditivaTests.cs`
- [x] 3.4 Verificación: suites 3.2/3.3 listas para correr en verde — pendiente de verde humano en
  Unity Editor (ningún agente ejecuta Unity); cero `Assume` omitidos confirmado por inspección
  manual línea por línea. **Conteo real reportado, sin decidir split (instrucción explícita del
  orchestrator).**

### Files Changed

| File | Action | What Was Done |
|---|---|---|
| `Runtime/Scenarios/Boardroom/RequirementsScenarioObjective.cs` | Created | Implementación real de `IScenarioObjective` (segunda, hermana de `TriageScenarioObjective`). Dos ctores (sin parámetros = sujeto sin caso; rico = pesos + `Func<RequirementCaseId,string>`). Lecturas aditivas `HasCase`/`RequirementCount`/`RequirementsDisclosed`/`SummaryIsFaithful` (AD8/AD9, recalculada en cada lectura). `Progress01` delega en `BoardroomObjectiveSettings.Mezclar` (AD7) con `cobertura = revelados/checklist.Count`, `cierre = SummaryIsFaithful ? 1 : 0`, `trato = _trato/PasosDeTrato`. `Notify` = libro mayor simétrico (AD1/AD2): `Improved` satura en `PasosDeTrato`, `Worsened` resta 1 con piso en 0, `!Changed` no-op. `AssignCase` limpia siempre y solo siembra el trato lleno si la carga tiene éxito (id conocido + `RequirementChecklistLoader.TryParse` exitoso); nunca lanza (id `None`, cargador `null`, función que lanza, JSON inválido o checklist vacío → `HasCase` falso). `RegisterDisclosure(Core.RequirementResponse)` acredita solo `Outcome == Revelado` de un id del caso asignado, idempotente por `HashSet`, "Core." calificado a propósito (AD6, previene shadowing futuro con el namespace `NpcAi.RequirementResponse` de M16). `PresentSummary` sobrescribible, sin efecto antes de `AssignCase`, `null`/vacío seguros. `Reset()` vuelve al estado recién construido, idempotente. 179 líneas |
| `Tests/EditMode/Scenarios/Boardroom/RequirementsScenarioObjectiveTests.cs` | Created | `: ScenarioObjectiveContract`, `CreateSubject() => new RequirementsScenarioObjective()` — hereda las 7 pruebas sin modificar la base, espejo de `TriageScenarioObjectiveTests`. 18 líneas |
| `Tests/EditMode/Scenarios/Boardroom/RequirementsSuperficieAditivaTests.cs` | Created | 22 `[Test]`: `AssignCase` (8 — id desconocido, cargador `null`, función que lanza, JSON basura, `RequirementCaseId.None`, checklist vacío AD4, éxito puebla `HasCase`/`RequirementCount`, reasignación descarta progreso previo y resiembra el trato lleno AD1); `RegisterDisclosure` (6 — acredita `Revelado` del caso, filtra otros `Outcome` y `NoAplica`, filtra id ajeno, idempotente, no-op antes de `AssignCase`, `default` seguro); `PresentSummary` (8 — coincidencia exacta acredita aun con cobertura parcial, falta un id no acredita, sobra un id no acredita, sobrescribible, no-op antes de `AssignCase`, `null` seguro, vacío sin revelaciones no acredita AD9, revelar después de cerrar vuelve el cierre infiel y re-presentar lo recupera AD8). Espejo estructural de `TriageSuperficieAditivaTests`. 322 líneas |
| `Runtime/Scenarios/Boardroom/RequirementsScenarioObjective.cs.meta`, `Tests/.../RequirementsScenarioObjectiveTests.cs.meta`, `Tests/.../RequirementsSuperficieAditivaTests.cs.meta` | Created | 3 `.meta` nuevos, GUIDs generados con `/dev/urandom`, todos versionados |

Total: 3 archivos de contenido + 3 `.meta` = 6 archivos nuevos. **519 líneas autoradas**
(179+18+322) frente a ~450 estimadas en design.md — **supera la válvula explícita de 490 líneas**
del forecast de `sdd-tasks`. Cero archivos modificados (todo `Create`); `Runtime/Core/` intacto.

### Work Unit Evidence

| Evidence | Value |
|---|---|
| Focused test command | `Unity -batchmode -runTests -projectPath <repo> -testPlatform EditMode -testFilter "RequirementsScenarioObjectiveTests\|RequirementsSuperficieAditivaTests"` — **no ejecutado por el agente** (compuerta humana, design.md). Verificación manual: la tabla de sanidad de `Progress01` recalculada a mano contra `Mezclar(hayCaso, cobertura, cierre, trato)` para cada assert numérico (p. ej. 0.2/0.6/0.7/0.8/0.96 de design.md); los 7 tests heredados de `ScenarioObjectiveContract` recorridos contra el libro mayor simétrico (`_trato` arranca en 0 sin `AssignCase`, satura en `PasosDeTrato`, nunca baja de 0) confirmando que ninguno requiere `Assume` |
| Runtime harness | N/A — M11 (compositor) no existe todavía, ningún arnés de escena puede ejercitar el objetivo fuera de EditMode |
| Rollback boundary | Borrar `Runtime/Scenarios/Boardroom/RequirementsScenarioObjective.cs` y sus 2 archivos de prueba (+ 3 `.meta`) — ningún otro archivo tocado, `Fakes/ScriptedScenarioObjective.cs` (PR4) sin tocar |

### Deviations from Design

Ninguna de comportamiento; implementación matches la interfaz exacta de design.md
("Interfaces / Contracts" → `RequirementsScenarioObjective.cs`) firma por firma. Una nota de
ambigüedad resuelta: el texto narrativo del XML doc de `AssignCase` en design.md dice "siembra el
crédito de trato LLENO" sin calificar, pero la sección "Aritmética del progreso" (más precisa)
dice explícitamente "`AssignCase` **exitoso**: `_trato = PasosDeTrato`". Se implementó la versión
precisa: el trato se resetea a 0 al inicio de todo `AssignCase` y solo se resiembra a
`PasosDeTrato` si la carga tiene éxito — consistente con el spec (el escenario de trato solo está
definido "dado un caso recién asignado", es decir, exitosamente asignado) y sin dejar `_trato`
lleno tras una carga fallida (lo que inflaría `Progress01` con `HasCase == false`, violando la
renormalización).

### Issues Found

None.

### Ledger

Attempt ledger: `acquire` (mismo attempt del orchestrator, token
`sha256:3861dc04b176a08e097e76d8c8574fc8b6d11e3f00de040933bbd3254819802c`) → mutación cero,
`state: proceed`. Trabajo ejecutado sobre `feat/m10-objetivo-real`. `settle` con
`outcome: passed`, `evidence-revision:
sha256:2be5ec0730f1b08c78f60c34715d989e027dd98791c97e307d168036d7fffdc8`,
`harness-disposition: reused`, cleanup y process evidence registrados → `state: complete`.

### Split Decision (orchestrator, 2026-09-22)

Fresh-context contract validator corrió una sola vez contra todo el batch de PR3 (los 3 archivos
juntos, antes de partir) — **PASS**: conformidad de contrato, AD1/AD2 (libro mayor simétrico,
confirmado como decremento real y no reset a 0), AD8/AD9 (recalculo de `SummaryIsFaithful` en cada
lectura), confinamiento de alcance, 519 líneas confirmadas exactas. Jefferson confirmó partir en 2
PRs (válvula ya prevista en design.md/tasks.md), mismo patrón `PR3a`/`PR3b` que M16 ya usó para su
propio exceso de presupuesto:

- **PR3a** `feat/m10-objetivo-real` (base: PR2): tareas 3.1-3.2 — `RequirementsScenarioObjective.cs`
  (179) + `RequirementsScenarioObjectiveTests.cs` (18) = **197 líneas**.
- **PR3b** `feat/m10-superficie-aditiva` (base: PR3a): tareas 3.3-3.4 —
  `RequirementsSuperficieAditivaTests.cs` (322) = **322 líneas**.

El split es un corte de commit/rama, no una reimplementación: el código ya escrito y validado no
cambió una línea.

## Remaining Tasks

- [ ] 4.1-4.6 (PR4 `feat/m10-doble-y-progreso`, base = rama de PR3b) — pendiente

## Workload / PR Boundary

- Mode: chained PR slice (`feature-branch-chain`) — **válvula de 490 líneas superada (519),
  partido en PR3a (197) + PR3b (322), confirmado por Jefferson 2026-09-22**
- Current work unit: Unit 3 — "Real objective: 7 inherited + additive surface" (PR3a + PR3b)
- Boundary: arranca en cero implementaciones reales de `IScenarioObjective` para M10 (solo el
  doble `±1` existía) y termina con `RequirementsScenarioObjective` completo, pasando los 7 tests
  heredados y su superficie aditiva propia probada; el doble sigue sin rediseñar — eso es PR4
- Estimated review budget impact: 519 líneas totales partidas en 2 PRs de 197 y 322, ambos dentro
  del presupuesto de 400 individualmente

## Alcance de este batch: SOLO PR4 (`feat/m10-doble-y-progreso`), tasks 4.1-4.6 — ÚLTIMO PR de implementación

**Mode**: Strict TDD de autoría. Para `ResetParityTests.cs` el RED real es "el doble viejo (paso
fijo ±1) no tiene `HasCase`/`AssignCase`/etc. — no compila", el GREEN es la reescritura (tarea 4.1).
Para `RequirementsProgresoTests.cs` la clase real ya está completa desde PR3a/3b — es cobertura de
caracterización, no un ciclo RED-then-GREEN de producción nueva.

### Completed Tasks

- [x] 4.1 Reescribir `Runtime/Scenarios/Boardroom/Fakes/ScriptedScenarioObjective.cs`
- [x] 4.2 Verificar `ScriptedScenarioObjectiveTests.cs` (sin tocar, 11 líneas) sigue pasando los 7 heredados
- [x] 4.3 `Tests/EditMode/Scenarios/Boardroom/RequirementsProgresoTests.cs`
- [x] 4.4 `Tests/EditMode/Scenarios/Boardroom/ResetParityTests.cs`
- [x] 4.5 `Docs/MODULES.md` — sección M10 reescrita
- [x] 4.6 Verificación: cero `Debug.Log`, cero diff en asmdefs, `.meta` versionados

### Files Changed

| File | Action | What Was Done |
|---|---|---|
| `Runtime/Scenarios/Boardroom/Fakes/ScriptedScenarioObjective.cs` | **Modified** (único archivo no nuevo de todo el cambio) | Reescrito de "paso fijo ±1 sobre 4 pasos" (calco de M9) a espejo completo de la superficie de `RequirementsScenarioObjective`. Sin IO ni `Data/`: `Dictionary<string, RequirementId[]>` embebida con los 4 casos reales, verificada id por id contra `Data/Requirements/caso-juntas-0N.json`. Reusa `BoardroomObjectiveSettings.Mezclar` (AD7) para paridad aritmética, mismo criterio que `ScriptedRequirementResponder` (M16) |
| `Tests/EditMode/Scenarios/Boardroom/RequirementsProgresoTests.cs` | Created | 10 `[Test]`: tabla de sanidad completa de 7 filas (`1e-4`), incluida la fila "cierre temprano honesto" (t=1,c=0.5,k=1→0.70) agregada tras el gate check del orchestrator para que el archivo cubra las 7 filas de design.md literalmente, no 6; renormalización sin caso (fila 7); penalización exacta de 0.04 por `Worsened` no recuperado + reversión de `IsComplete`; clamp `[0,1]` bajo 20 notifies alternados; saturación/piso del trato |
| `Tests/EditMode/Scenarios/Boardroom/ResetParityTests.cs` | Created | 7 `[Test]`: `Reset()` real y doble vuelven al estado recién construido desde progreso parcial, idempotencia en ambos, `AssignCase` vuelve a funcionar después de `Reset()`, convergencia observable real/doble |
| `Docs/MODULES.md` | Modified | Sección M10: "solo doble" → implementación real completa (arquitectura, 5 PRs con números #56-#59 + PR4, ~76 pruebas EditMode del módulo) |

### Issues Found

Gate check del orchestrator (validador de contrato en contexto fresco) marcó **PASS** general con un
hallazgo no bloqueante: `RequirementsProgresoTests.cs` cubría 5 de las 7 filas literalmente más
renormalización, mezclando la fila "cierre temprano honesto" (t=1,c=0.5,k=1→0.70) con la fila de
cobertura parcial sola (t=1,c=0.5,k=0→0.5). Corregido antes de commitear: se agregó
`Fila2b_cierre_temprano_honesto_con_cobertura_parcial` como prueba dedicada.

### Ledger

Attempt ledger: `acquire` (token `sha256:819979656c3fdf4cae8be4563cf807f4deae627b59a236b3388c37e884c9b6a9`)
→ mutación cero, `state: proceed`. Trabajo ejecutado sobre `feat/m10-doble-y-progreso`. `settle` con
`outcome: passed`, `evidence-revision: sha256:79f5499fffe1476e7b6fb0cb9239efbc8099b32d990670ebbba80b2342d5481d`,
`harness-disposition: reused` → `state: complete`.

## Status

**22/22 tareas completas — los 5 PRs de M10 tienen código listo.** PR1 (#56), PR2 (#57), PR3a (#58),
PR3b (#59) abiertos en GitHub, self-merge pendiente de Jefferson. PR4 (`feat/m10-doble-y-progreso`)
validado (PASS + 1 hallazgo corregido), listo para commit/push/PR. Próximo paso real tras el PR4:
`sdd-verify` (no otro `sdd-apply`) → `sdd-archive`.
