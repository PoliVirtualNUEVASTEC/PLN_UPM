# Archive Report: M16 — Catálogo y respondedor de requerimientos de sala de juntas

**Change**: `2026-09-16-m16-catalogo-respondedor-juntas`  
**Archived**: 2026-09-22  
**Status**: ✅ **COMPLETE** — 31/31 tasks, PASS WITH WARNINGS  
**Module**: `NpcAi.RequirementResponse` (`Runtime/RequirementResponse/`, `Data/Requirements/`, `Tests/EditMode/RequirementResponse/`)  
**Owner**: Jefferson Estiven Aristizábal Quiceno  
**Engram Artifacts**:
- Proposal: [#77](sdd/2026-09-16-m16-catalogo-respondedor-juntas/proposal)
- Spec: [#82](sdd/2026-09-16-m16-catalogo-respondedor-juntas/spec)
- Design: [#83](sdd/2026-09-16-m16-catalogo-respondedor-juntas/design)
- Tasks: [#84](sdd/2026-09-16-m16-catalogo-respondedor-juntas/tasks)
- Verify-Report: [#100](sdd/2026-09-16-m16-catalogo-respondedor-juntas/verify-report)

## Cycle Summary

M16 is the second link in the chain **M0 (merged) → M16 (this change) → M10**. M0 delivered the port `IRequirementResponder` and contract v3 (PRs #39–#42, merged 2026-09-16). This change implements the port for the first time, making the NPC behave as the client of the boardroom case, progressively revealing requirements based on receptivity gained. The implementation is data-driven: requirement disclosure gates and personality styles live in `Data/Requirements/*.json`, not in C# code (repository rule 7: "adding a personality must not change any class").

### Two New Capabilities

1. **`respondedor-requerimientos-m16`** — Observable behavior of `NpcAi.RequirementResponse`: inherits `RequirementResponderContract` from M0 without modification, adds 8 requirements on HOW revelation happens (table lookup, never invention; receptivity gate per requirement; deterministic matching; personality style as prefix/suffix only). Covers 10 inherited test scenarios plus 6 new scenarios.

2. **`catalogo-requerimientos-m16`** — Schema and invariants for `Data/Requirements/*.json`: one file per case, transcribed from the 4 domains narrated in `Data/Corpus/juntas.json` (soccer tournament, shop, school, airline). Five requirements enforce minimum schema per requirement, filename = `id`, coverage of all 3 receptivity levels per case, exactly 4 cases, and that deviation phrases never leak the actual answer.

## Implementation Status

**All 31 tasks completed** (Phases 1–4, PRs #45, #47, #48, #50, #51, #52, #53):

- **Phase 1 (PR #45)**: `RequirementCase` POCOs, `RequirementCaseLoader` with TDD RED/GREEN, minimum receptivity mapping (text names, not enum values). ✅
- **Phase 2a (PR #47)**: `RequirementMatcher` (text normalization, deterministic tie-breaking), `RequirementDisclosurePolicy` (gate logic). ✅
- **Phase 2b (PR #48)**: `Fakes/ScriptedRequirementResponder` with synthetic "presupuesto" requirement to satisfy all 10 inherited contract tests. ✅
- **Phase 3a (PR #50)**: `PersonalityStyleBank` (style bank as JSON, never C# switch), `matices.json` (4 personalities from M5). ✅
- **Phase 3b (PR #51)**: `RequirementResponder` (real adapter, ignores `Intent` by design), its tests with real catalog (4 cases from `juntas.json`, no invented content). ✅
- **Phase 4a (PR #52)**: 2 cases transcribed. ✅
- **Phase 4b (PR #53)**: 2 remaining cases, `RequirementCasesDataTests`, `Docs/MODULES.md` M16 section. ✅

**Metrics**:
- 67 total test cases (NUnit): 63 green, 0 red, 4 Assume-inconclusive (expected, documented in spec).
- 46 new files changed, 2102 lines added + 13 deleted (git diff –stat).
- 7 PRs merged to `main` (2026-09-21), all via GitHub "Merge pull request" commits.

## Verification Outcome

**Verdict: PASS WITH WARNINGS** (per verify-report #100, 2026-09-22 15:04:50 UTC)

- ✅ All 13 requirements of both specs (respondedor-requerimientos-m16 + catalogo-requerimientos-m16) are compliant.
- ✅ All 15 scenarios pass or are spec-sanctioned Assume-inconclusive.
- ✅ Zero CRITICAL issues, zero CRITICAL findings.
- ⚠️ 3 WARNINGs (all documentation/citation, no functional impact):
  1. **Traceability citation correction** (WARNING 1): Spec table cited non-existent `RequirementMatcherTests.El_Intent_no_cambia_el_emparejamiento`; actual covering test is `ScriptedRequirementResponderTests.El_Intent_no_cambia_el_requerimiento_emparejado`. **FIXED in this archive commit**: corrected citation in `respondedor-requerimientos-m16/spec.md` table row before promoting to `openspec/specs/`.
  2. **Docs stale status** (WARNING 2): `Docs/MODULES.md` M16 section said "no PR merged to main yet" and listed only 5 of 7 PRs. **FIXED in this archive commit**: updated to reflect all 7 PRs merged and verification state.
  3. **Proposal Success Criterion not amended** (WARNING 3): Original proposal criterion #1 said "sin omisiones por Assume" (no Assume skips); later decision #66 (2026-09-18, Jefferson) authorized 4 spec-sanctioned inconclusive tests for real `RequirementResponder` using non-fabricated catalog (no invented "presupuesto"). The ratified spec `respondedor-requerimientos-m16` formalizes this as a first-class Requirement 1 scenario. Proposal itself was not retroactively amended. **No action**: spec supersedes proposal per OpenSpec precedence; this is noted for future audit context only.

## Textual Corrections Applied at Archive Time

Per verify-report recommendations (WARNINGs 1–2), two mechanical text fixes were folded into this same archive commit (no new code):

1. **Line 162, `respondedor-requerimientos-m16/spec.md` traceability table**:
   - OLD: `RequirementMatcherTests.El_Intent_no_cambia_el_emparejamiento` (method does not exist)
   - NEW: `ScriptedRequirementResponderTests.El_Intent_no_cambia_el_requerimiento_emparejado` (actual covering test)
   - File copied to `openspec/specs/respondedor-requerimientos-m16/spec.md` with this correction applied.

2. **Lines 634–638, `Docs/MODULES.md` M16 section**:
   - OLD: "implementación real completa, en revisión — ningún PR mergeado a `main` todavía"
   - NEW: "implementación real completa, verificada y mergeada a `main` (2026-09-21). 7 PR encadenados... todos mergeados: PR1 (#45)... PR4b (#53). Verificación: 67 pruebas, 0 rojas, 4 Assume-inconclusive (esperadas)."

## Specs Merged to Main

Two new capability specs are now in `openspec/specs/` (promoted from delta to source of truth):

- `openspec/specs/respondedor-requerimientos-m16/spec.md` — 8 requirements + 10 scenarios, covering the real adapter.
- `openspec/specs/catalogo-requerimientos-m16/spec.md` — 5 requirements + 5 scenarios, covering the JSON catalog schema.

No existing specs were modified (this is a new module; M0 contract v3 is unchanged).

## Architecture Decisions Confirmed

All 11 Architecture Decisions (AD1–AD11) from design.md are implemented correctly:

- **AD1**: `receptividadMinima` serialized as text name in JSON, mapped by exact string match (not enum ordinal).
- **AD2**: Invalid requirement discarded per-requirement; case fails only if fewer than 4 remain.
- **AD3**: Level coverage (3 levels per case) enforced by data tests, not loader.
- **AD4**: `RequirementDisclosurePolicy.Decidir` never returns `NoAplica` (only `Revelado` or `AunNoRevelado`).
- **AD5**: `Intent` parameter ignored by design (text match only).
- **AD6**: Style bank is JSON (`matices.json`), not a C# switch — rule 7 compliance.
- **AD7**: Deterministic rotation by table index (safe modulo arithmetic).
- **AD8**: Optional tags default to `"neutral"`/`"idle"`.
- **AD9**: IO injected via delegate (M10/M11 provides byte loader).
- **AD10**: `matices.json` invalid/missing falls back to non-empty constant in code.
- **AD11**: Matcher duplicates ~70 lines from `ClinicalFactMatcher` (accepted; rule 3 compliance).

## Risks and Resolutions

**Original HIGH risks (design.md, "Riesgos")**:

1. **R1 — "Presupuesto" not in real catalog** (HIGH): Real `RequirementResponder` does not have a "presupuesto" requirement because `Data/Corpus/juntas.json` does not narrate it. Inherited contract tests depending on that phrase remain inconclusive (Assume) by design (decision #66). **RESOLVED**: Spec now makes this a first-class scenario in Requirement 1 ("divergencia documentada"). Double has synthetic presupuesto for test completeness (rule 4 allowance).

2. **R2 — `NoReceptivo` always reveals** (HIGH): Requirement with `receptividadMinima == NoReceptivo` will never produce `AunNoRevelado` (always `Revelado`). **RESOLVED**: Spec acknowledges this in Requirement 5 scenario; only umbrals `Neutral` and `Receptivo` produce observable difference in text.

3. **R3 — Line count budget** (HIGH): ~1.528 lines ≈ 3.8× budget of 400. **RESOLVED**: 4 PRs (then split into 7) per design; decision strategy `ask-on-risk`; Jefferson approved stacked chain.

4. **R4–R7** (MEDIUM): Addressed by design (details in design.md). **RESOLVED**: No regressions in verification.

**NEW risks identified in verify-report**:

None. Verification passed; WARNINGs are documentation-currency only.

## Source of Truth Update

The following capability specs are now frozen in `openspec/specs/` and serve as authoritative reference:

- `openspec/specs/respondedor-requerimientos-m16/spec.md` (13 requirements from both capability views)
- `openspec/specs/catalogo-requerimientos-m16/spec.md` (5 requirements)

`openspec/specs/` is the single source of truth for both capabilities going forward. `Data/Requirements/` is the source of truth for catalog values (extensible by data edits only, per rule 7).

## Completion Checklist

- [x] All 31 implementation tasks marked complete (tasks.md)
- [x] 67/67 test cases execution verified (63 green + 4 spec-sanctioned inconclusive, 0 red)
- [x] Diff confirms zero contact with `Runtime/Core/`, `Runtime/CoreChannels/`, `Runtime/ClinicalResponse/`, `Runtime/Scenarios/`, `Runtime/Nlu/`, `Data/Corpus/`
- [x] All `.meta` files versionedfor new folders and files
- [x] Archive contains all artifacts: proposal.md, design.md, tasks.md, apply-progress.md, verify-report.md, specs/
- [x] Change folder moved from `openspec/changes/` to `openspec/changes/archive/`
- [x] Archive folder verified identical to pre-move snapshot via `diff -r`
- [x] Two specs promoted to `openspec/specs/` with textual corrections applied
- [x] `Docs/MODULES.md` updated with current state
- [x] SDD cycle complete

## Next Steps

- **M10** (Sala de Juntas) is now unblocked and can proceed. M10 depends on M16 for the requirement respondedor and will wire it into its own enrutador.
- **M11** (Harness) will eventually wire both M16 (real `RequirementResponder`) and M10 (scenario orchestrator) into the live boardroom scene.
- The 4 Assume-inconclusive inherited tests in `RequirementResponderTests` will remain inconclusive until/unless `Data/Corpus/juntas.json` is extended to narrate a "presupuesto" requirement (data extensibility, rule 7; outside this change's scope).

---

**Archive finalized by**: sdd-archive executor  
**Verification authority**: verify-report #100 (PASS WITH WARNINGS)  
**Repository state**: `main` branch, post-merge of all 7 PRs (as of 2026-09-21)
