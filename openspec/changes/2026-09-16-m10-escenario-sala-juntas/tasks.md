# Tasks: M10 — Escenario Sala de Juntas

## Review Workload Forecast

| Field | Value |
|---|---|
| Estimated changed lines | ~1,565 authored (+~110 generated `.meta`) |
| 400-line budget risk | High |
| Chained PRs recommended | Yes |
| Suggested split | PR1 → PR2 → PR3 → PR4 (valve to 5 if PR3 confirms >490 lines) |
| Delivery strategy | ask-on-risk |
| Chain strategy | `feature-branch-chain` (confirmed by Jefferson 2026-09-22) — PR1 bases on the feature/tracker branch, each child bases on its immediate predecessor, mirrors M0/M16 |

Decision needed before apply: Resolved — 4 chained PRs, feature-branch-chain, confirmed 2026-09-22
Chained PRs recommended: Yes
Chain strategy: feature-branch-chain
400-line budget risk: High

### Suggested Work Units

Focused-test pattern (human-run, EditMode; no agent executes Unity): `Unity -batchmode -runTests -projectPath <repo> -testPlatform EditMode -testFilter "<classes below>"`. Runtime harness is `N/A` for all units: M11 (compositor) does not exist yet, so no scene/runtime arnés can exercise the objective outside EditMode.

| Unit | Goal | PR | Focused test filter | Runtime harness | Rollback boundary |
|---|---|---|---|---|---|
| 1 | Weight formula + asset, isolated | PR1 `feat/m10-pesos-y-asset` | `BoardroomObjectiveSettingsTests\|BoardroomObjectiveSettingsAssetTests` | N/A (M11 pending) | Delete `Config/`, `Unity/`, `Data/Scenarios/` — no other file touched |
| 2 | Catalog projection vs. memory JSON + real files | PR2 `feat/m10-checklist-y-lector` | `RequirementChecklistLoaderTests\|RequirementChecklistDataTests` | N/A (M11 pending) | Delete `RequirementChecklist.cs`, `RequirementChecklistLoader.cs` + their 2 test files |
| 3 | Real objective: 7 inherited + additive surface | PR3 `feat/m10-objetivo-real` | `RequirementsScenarioObjectiveTests\|RequirementsSuperficieAditivaTests` | N/A (M11 pending) | Delete `RequirementsScenarioObjective.cs` + its 2 test files |
| 4 | Double parity, sanity table, docs | PR4 `feat/m10-doble-y-progreso` | `RequirementsProgresoTests\|ResetParityTests\|ScriptedScenarioObjectiveTests` | N/A (M11 pending) | Revert `Fakes/ScriptedScenarioObjective.cs`; delete `RequirementsProgresoTests.cs`, `ResetParityTests.cs`; revert `Docs/MODULES.md` |

Valve: if PR3's real diff confirms >490 lines, split into `feat/m10-objetivo-real` (class + contract test, ~220) and `feat/m10-superficie-aditiva` (~230), giving 5 PRs on the same chain pattern.

## Phase 1: Weights & Asset — PR1, base = feature/tracker branch

- [x] 1.1 RED: `Tests/EditMode/Scenarios/Boardroom/BoardroomObjectiveSettingsTests.cs` — complements exact, `Mezclar` clamp/renorm, `PasosDeTrato` min 1, ctor clamps out-of-range weights
- [x] 1.2 GREEN: `Runtime/Scenarios/Boardroom/Config/BoardroomObjectiveSettings.cs` per design interface (ctor + `Mezclar`, AD3/AD7/AD13)
- [x] 1.3 RED: `Tests/EditMode/Scenarios/Boardroom/BoardroomObjectiveSettingsAssetTests.cs` — `ToSettings()` copies 3 fields; defaults match consts
- [x] 1.4 GREEN: `Runtime/Scenarios/Boardroom/Unity/BoardroomObjectiveSettingsAsset.cs` (`ScriptableObject`, no logic)
- [x] 1.5 Create `Data/Scenarios/README.md` — field schema, ranges, flattened formula, sanity table, "`pesoDeTrato > 0.5` inverts intent" warning
- [x] 1.6 Create `Data/Scenarios/Boardroom.asset` (`pesoDeTrato 0.2`, `pesoDeCobertura 0.75`, `pasosDeTrato 5`); stage new `.meta`
- [x] 1.7 Verify: run 1.1/1.3 suites green — lista para correr, pendiente de verde humano en Unity Editor (ningún agente ejecuta Unity; ver Testing Strategy de design.md)

## Phase 2: Checklist & Loader — PR2, base = PR1 branch

- [x] 2.1 Create `Runtime/Scenarios/Boardroom/RequirementChecklist.cs` — `Id`, ordered `RequirementId` list, internal `HashSet`, `Contains`, `Count` (AD11)
- [x] 2.2 Create `Runtime/Scenarios/Boardroom/RequirementChecklistLoader.cs` — `TryParse`, minimal `Raw*` (AD10), no min-4 gate (AD12), dedupe/discard empty ids, preserve order
- [x] 2.3 Create `Tests/EditMode/Scenarios/Boardroom/RequirementChecklistLoaderTests.cs` — valid load; null/empty/garbage/`{}`/`[]` → false; empty `id` → false; missing/empty `requerimientos` → false; dedupe keeps order; extra M16 fields ignored; single-requirement case accepted
- [x] 2.4 Create `Tests/EditMode/Scenarios/Boardroom/RequirementChecklistDataTests.cs` — 4 real `caso-juntas-0N.json` via `AssetDatabase.FindAssets`: all present, parse ok, `Id == filename`, `Count >= 1`, no `RequirementId.None`, no dup, denominator 4–6 asserted
- [x] 2.5 Verify: 2.3/2.4 suites green; stage new `.meta` — listas para correr, pendiente de verde humano en Unity Editor (ningún agente ejecuta Unity)

## Phase 3: Real Objective — PR3, base = PR2 branch

- [ ] 3.1 Create `Runtime/Scenarios/Boardroom/RequirementsScenarioObjective.cs` — both ctors; `HasCase`/`RequirementCount`/`RequirementsDisclosed`/`SummaryIsFaithful`; `Progress01`/`IsComplete`/`Notify` (AD1/AD2 symmetric ledger); `AssignCase`; `RegisterDisclosure` (AD5/AD6); `PresentSummary` (AD8/AD9); `Reset`
- [ ] 3.2 Create `Tests/EditMode/Scenarios/Boardroom/RequirementsScenarioObjectiveTests.cs : ScenarioObjectiveContract` — 7 inherited tests, zero `Assume` skips
- [ ] 3.3 Create `Tests/EditMode/Scenarios/Boardroom/RequirementsSuperficieAditivaTests.cs` — `AssignCase` (unknown id / null loader / throwing func / bad JSON never throw, resets+reseeds progress); `RegisterDisclosure` (idempotent, filters non-`Revelado`/foreign id, no-op pre-`AssignCase`, `default` safe); `PresentSummary` (exact/partial/missing/extra, overwrite, pre-`AssignCase` no-op, null/empty safe, AD8 post-disclosure staleness, AD9 empty-summary non-credit)
- [ ] 3.4 Verify: 3.2/3.3 suites green, no `Assume` skips; if diff >490 lines apply the PR3 valve split before opening the PR

## Phase 4: Double, Progress & Docs — PR4, base = PR3 branch

- [ ] 4.1 Rewrite `Runtime/Scenarios/Boardroom/Fakes/ScriptedScenarioObjective.cs` — mirror additive surface, no IO/`Data/`, embedded table of 4 real case ids, reuse `Mezclar`
- [ ] 4.2 Verify existing `Tests/EditMode/Scenarios/Boardroom/ScriptedScenarioObjectiveTests.cs` (untouched, 11 lines) still passes 7 inherited tests against the rewritten double
- [ ] 4.3 Create `Tests/EditMode/Scenarios/Boardroom/RequirementsProgresoTests.cs` — 7-row sanity table (`1e-4` tolerance), isolated ways, no-case renormalization, `clamp01`, reversible `IsComplete`, 0.04-per-unrecovered-`Worsened` penalty
- [ ] 4.4 Create `Tests/EditMode/Scenarios/Boardroom/ResetParityTests.cs` — `Reset()` on real and double: fresh-construction state, idempotent, re-`AssignCase` works after
- [ ] 4.5 Update `Docs/MODULES.md` M10 section: "solo doble" → real implementation
- [ ] 4.6 Verify: full `Tests/EditMode/Scenarios/Boardroom/` suite green; confirm no `Debug.Log` in runtime; asmdefs unchanged; stage `.meta`
