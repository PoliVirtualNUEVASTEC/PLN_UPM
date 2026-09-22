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

## Status

7/22 tareas completas (PR1 completo). Ready for next batch (PR2, tasks 2.1-2.5) — rama/PR de
PR1 aún no creados, eso lo maneja el orchestrator.
