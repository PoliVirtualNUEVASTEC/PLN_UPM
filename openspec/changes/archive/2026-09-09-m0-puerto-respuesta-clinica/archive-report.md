# Archive Report: 2026-09-09-m0-puerto-respuesta-clinica

**Change**: `2026-09-09-m0-puerto-respuesta-clinica`  
**Type**: Contract extension (v1 → v2)  
**Archive date**: 2026-09-09  
**Mode**: SDD hybrid (OpenSpec + Engram)

## Summary

This change formalizes the clinical responder port `IClinicalResponder` as a new contract surface in `NpcAi.Core` (contract v1 → v2), enabling M15 (clinical respondent module) to operate as a first-class module. The change is purely additive:
- **3 new types**: `ClinicalCaseId` (struct mirror of `PersonalityId`), `ClinicalResponse` (routing signal struct), `IClinicalResponder` (port with `IsReady`, `AssignCase`, `Respond`)
- **All v1 types untouched**: no enum, DTO, or port from contract v1 is renamed, reordered, or reshaped
- **Contract version**: bumped from 1 to 2 with corresponding `## v2` section in `Docs/CONTRACT-CHANGELOG.md`
- **Spec promoted**: new capability `respuesta-clinica-m0` moved from delta to main specs at `openspec/specs/respuesta-clinica-m0/spec.md`

**Traceability**: 8 ADDED requirements / 16 scenarios. 8 scenarios are runtime-verified now (human EditMode green 2026-09-09); 8 are forward-traced to M15 (abstract `ClinicalResponderContract` base with 7 [Test] methods, no runtime execution yet).

---

## Verification Authority

### Verification Report
- **Verdict**: PASS WITH WARNINGS — archive-eligible after M0 co-review
- **Blockers**: 0
- **Critical findings**: 0
- **Requirements**: 8/8 ✓
- **Scenarios**: 16/16 (8 now, 8 forward)
- **Engram observation ID**: #48 (`sdd/2026-09-09-m0-puerto-respuesta-clinica/verify-report`)

### Final-State Facts (per handoff, overriding intermediate snapshots)

1. **Runtime Evidence**: Human EditMode Run All 100% GREEN, 2026-09-09 (author luisk, tasks.md 3.1)
   - Covers: `ContractTypeTests` (v1 enum freeze + 4 new v2 cases), `ContractVersionChangelogTests`, `CoreAssemblyPurityTests`, 7 pre-existing `*Contract` bases
   - Does NOT execute `ClinicalResponderContract` (abstract, no M15 subclass yet); its 7 [Test] are forward-traced

2. **Task Completion**: 18/18 tasks marked [x] in `tasks.md` (authoritative; `apply-progress.md` is stale per WARNING 2 of verify-report)
   - Phases 0, 1, 2: code and tests written, committed
   - Phase 3: manual gates (EditMode, .asmdef inspection) and git boundary audit completed

3. **Co-review of M0 (repo rule 3)**: APPROVED by Luis (author and shared M0 owner) on 2026-09-09
   - Pre-merge gate satisfied; no blocker for archive

4. **SUG 1 (RESOLVED in commit 5b7c8af)**:
   - Issue: `Docs/CONTRACT-CHANGELOG.md` line 76 (frozen `## v1` section) still read "(ventana del lunes, co-aprobacion)" — contradicted the corrected line 4
   - Resolution: Removed obsolete phrasing; now points to the v2 header and the rule in line 4

5. **Commits on branch `feat/m0-puerto-respuesta-clinica`**:
   - `ba329bb` — feat: v2 contract surface + types + port + base + new tests
   - `05808bd` — chore: task checkpoint
   - `d4dfbbc` — chore: task checkpoint
   - `ffc803b` — docs: verify-report.md (sdd-verify)
   - `5b7c8af` — docs: CONTRACT-CHANGELOG.md line 76 fix (SUG 1)

---

## Warnings & Disposition

| # | Category | Issue | Disposition | Blocker for Archive? |
|---|----------|-------|------------|--------|
| W1 | INFO | Scenario count mismatch in Engram metadata | Spec.md has 16 scenarios (8 req), Engram #46 says 18; archive uses authoritative spec.md count (16) | No |
| W2 | INFO | `apply-progress.md` stale | Shows 3.1/3.2/4.x as [ ]; tasks.md (authoritative) is 18/18 [x] | No |
| W3 | FORWARD | 8/16 scenarios are forward-traced to M15 | `ClinicalResponderContract` abstract; behavioral execution deferred to M15 verify | No (by design; spec declares forward traceability) |
| W4 | FAST-FOLLOW | `Respond_no_lanza_en_ningun_estado` only exercises not-ready path | No ready subject exists in M0 (stub never ready, M15 absent); gap closes in M15 apply with 4-line AssignCase + Assume.That addition | No (M15 responsibility) |

**Suggestions** (non-blocking):
- SUGGESTION 1: Add optional direct `.asmdef` parse assertion (`noEngineReferences == true`, `references` length == 0)
- SUGGESTION 2: Consider documenting why the 18→16 scenario reconciliation happened in Engram (optional metadata note)

---

## Metrics

| Metric | Value |
|--------|-------|
| ADDED requirements | 8 |
| Total scenarios | 16 |
| Code + test diff | +343 / -4 lines (~347 net) |
| Review budget (800 lines) | **Low risk** (43% utilized) |
| Tasks total | 18 |
| Tasks complete [x] | 18 |
| Write boundary violations | 0 (M0 routes only: `Runtime/Core/`, `Docs/CONTRACT-CHANGELOG.md`, `Tests/EditMode/Core/`, change directory) |
| v1 enum/DTO/port signatures altered | 0 (purely additive) |
| CRITICAL findings | 0 |
| Blockers | 0 |
| Verify verdict | PASS WITH WARNINGS |

---

## Spec Promotion & Archive Contents

### Promoted Spec
- **Source**: `openspec/changes/2026-09-09-m0-puerto-respuesta-clinica/specs/respuesta-clinica-m0/spec.md`
- **Destination**: `openspec/specs/respuesta-clinica-m0/spec.md`
- **Mechanical copy verification**: diff -r empty ✓

### Archive Directory
Path: `openspec/changes/archive/2026-09-09-m0-puerto-respuesta-clinica/`

Contents (moved via `git mv`):
- `proposal.md` — scope, approach, rollback
- `design.md` — architectural decisions, port signatures, testing strategy
- `tasks.md` — 18 implementation tasks (18/18 complete ✓)
- `apply-progress.md` — apply-phase snapshot (stale; archive includes for audit trail)
- `verify-report.md` — verification report (canonical version in Engram #48)
- `specs/respuesta-clinica-m0/spec.md` — delta spec (now also in main specs)
- `archive-report.md` (this file) — final audit trail

**Mechanical move verification**: diff -r between pre-move snapshot and archived content shows no differences ✓

---

## Engram Observation IDs (SDD Artifact Traceability)

| Phase | Artifact | Engram ID | Topic Key |
|-------|----------|-----------|-----------|
| spec | Specification | #46 | `sdd/2026-09-09-m0-puerto-respuesta-clinica/spec` |
| verify | Verification Report | #48 | `sdd/2026-09-09-m0-puerto-respuesta-clinica/verify-report` |
| archive | This Archive Report | (saving now) | `sdd/2026-09-09-m0-puerto-respuesta-clinica/archive-report` |

---

## What Remains for the Pipeline

1. **M0 Co-review gate**: ALREADY SATISFIED (Luis approved 2026-09-09)
2. **Merge PR to main**: Author (luisk) merges the PR on `feat/m0-puerto-respuesta-clinica` to `main` after co-review gate closes
3. **M15 responsibility** (2026-09-09-m15-respondedor-clinico SDD):
   - Inherit `ClinicalResponderContract` in the M15 module doubles and implementation
   - Execute the 8 forward-traced scenarios (W3, W4 disposition)
   - Add ready-state coverage for `Respond_no_lanza_en_ningun_estado` (4-line fast-follow, optional)
4. **Project context doc**: Record decision "Contrato v2 — puerto `IClinicalResponder` para M15" (CLAUDE.md rule 10)

---

## Compliance Gates

| Gate | Status | Evidence |
|------|--------|----------|
| **Task Completion** | ✓ PASS | 18/18 [x] in tasks.md |
| **Native Review Receipt** | ✓ ORDINARY POLICY | No formal review gate; co-review approved by Luis (rule 3) |
| **CRITICAL Issues** | ✓ NONE | verify-report: 0 critical_findings |
| **Blockers** | ✓ NONE | verify-report: 0 blockers |
| **Spec Promotion** | ✓ PASS | Mechanical copy to `openspec/specs/respuesta-clinica-m0/spec.md`, diff verified |
| **Archive Move** | ✓ PASS | `git mv` to `openspec/changes/archive/`, source removed, diff verified |
| **Write Boundary** | ✓ PASS | Only M0 contract routes touched; no cross-module contamination |

---

## Next Step

**Status**: Archive complete and closed.

The SDD cycle for change `2026-09-09-m0-puerto-respuesta-clinica` is CLOSED. The spec, design, tasks, and verification artifacts are now in the audit trail. The merged PR will be the runtime delivery vehicle. M15 will inherit the contract base and execute forward-traced scenarios.

No further SDD phases are needed for M0.
