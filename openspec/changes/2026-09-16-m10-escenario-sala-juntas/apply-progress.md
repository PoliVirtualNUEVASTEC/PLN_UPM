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

## Status

12/22 tareas completas (PR1 + PR2 completos). Ready for next batch (PR3, tasks 3.1-3.4) —
rama/PR de PR2 aún no creados, eso lo maneja el orchestrator.
