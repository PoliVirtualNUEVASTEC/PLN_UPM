# Apply Progress: M0 — Puerto de embeddings semanticos de oraciones (`ISentenceEmbedder`)

**Change**: `2026-09-25-m0-puerto-embeddings-semanticos`
**Mode**: Strict TDD — source written and reviewed by the agent; runtime GREEN is the human
Unity Editor EditMode gate (no Unity Editor/CLI available in this apply session, same
constraint as the precedent `2026-09-16-m0-puerto-requerimientos-juntas`).
**Batch**: 1st apply run. No prior apply-progress existed for this change.

## Completed Tasks (10/12; 2 remaining are human-only)

- [x] 1.1 RED — 6 new `ContractTypeTests` cases for `SentenceEmbedding` (section `// --- v4:
      embeddings de oraciones ---`).
- [x] 1.2 GREEN — `Runtime/Core/SentenceEmbedding.cs` (+`.meta`) created per design.md AD6/AD7.
- [x] 1.3 Verify (source-level) — reviewed against `ClinicalCaseId.cs`/`RequirementId.cs` file
      conventions and `ContractTypeTests.cs` case patterns.
- [x] 2.1 RED — `Tests/EditMode/Core/SentenceEmbedderContract.cs` (+`.meta`) created: abstract
      base, 10 `[Test]`, 2 stubs, concrete subclass.
- [x] 2.2 GREEN — `ISentenceEmbedder` added to `Runtime/Core/Ports.cs`, immediately after
      `IIntentClassifier`.
- [x] 2.3 Verify (source-level) — reviewed against `RequirementResponderContract.cs`.
- [x] 3.1 Atomic contract bump — `Contract.Version` `3 -> 4`, `## v4` changelog entry, pin
      rename, all in the working tree as one logical unit (actual `git commit` not run this
      session — see Risks).
- [x] 3.2 Verify (source-level) — `Version == 4` ties to the new `## v4` heading;
      `noEngineReferences: true` untouched; no new `NpcAi.*` references (both new files use
      only `System.*`).
- [x] 4.2 Real diff measured — see Diff Measurement below.
- [x] 4.4 Diff scope confirmed — see Scope Confirmation below.
- [ ] 4.1 Full EditMode Test Runner run — human-only gate, not run by any agent.
- [ ] 4.3 M0 cross-review request — human/organizational action, out of this agent's scope.
      `Docs/CONTRACT-CHANGELOG.md` `## v4` already carries the `PENDIENTE — <quien, cuando,
      donde>` placeholder to fill in when it happens.

## Files Changed

| File | Action | What Was Done |
|------|--------|----------------|
| `Runtime/Core/SentenceEmbedding.cs` | Created | `readonly struct` DTO: `Length`, `IsEmpty`, read-only indexer, `ToArray()` defensive copy, bit-exact `IEquatable<SentenceEmbedding>`. No public property exposes the raw backing array (binding clarification honored — no `Vector` property as the spec's loose wording suggested). |
| `Runtime/Core/SentenceEmbedding.cs.meta` | Created | New GUID `8f2e719bd63e449896009c1442832700`. |
| `Runtime/Core/Ports.cs` | Modified | Added `ISentenceEmbedder` (`IsReady`, `Embed(string)`) immediately after `IIntentClassifier`, per AD9. No other port touched. |
| `Runtime/Core/Contract.cs` | Modified | `Version` `3 -> 4`. |
| `Docs/CONTRACT-CHANGELOG.md` | Modified | Inserted `## v4` above `## v3`, verbatim from design.md's draft (Tipos nuevos, invariantes, Asimetria de determinismo, Advertencia de calidad, Decisiones y proceso with the co-review PENDIENTE placeholder preserved). |
| `Tests/EditMode/Core/SentenceEmbedderContract.cs` | Created | Abstract contract base, 10 `[Test]` methods, 2 private stubs (`EmbebedorDeOracionesNoListo`, `EmbebedorDeOracionesDePrueba`), concrete `SentenceEmbedderContractStubTests` subclass. |
| `Tests/EditMode/Core/SentenceEmbedderContract.cs.meta` | Created | New GUID `1012a4984be745fb8faf81df0653f8d0`. |
| `Tests/EditMode/Core/ContractTypeTests.cs` | Modified | Added 6 `SentenceEmbedding_*` cases; renamed pin `Version_del_contrato_es_tres` -> `Version_del_contrato_es_cuatro` (value `4`). `Solo_PersonalityId_implementa_IEquatable_en_la_v1` left untouched per design (closed v1 list). |

## TDD Cycle Evidence

| Task | Test File | Layer | Safety Net | RED | GREEN | TRIANGULATE | REFACTOR |
|------|-----------|-------|------------|-----|-------|-------------|----------|
| 1.1/1.2 | `ContractTypeTests.cs` (6 new cases) | Unit (NUnit EditMode) | N/A (additive only, no existing case edited) | Written first (reference `SentenceEmbedding`, which did not exist) | Written; **execution pending human Unity Test Runner** — no Unity Editor/CLI in this session | 6 cases cover: default/hash-zero, null/empty-array, defensive copy on construct, defensive copy on `ToArray`, bit-exact equality incl. `0f`/`-0f`, equal-hash-equal | N/A — DTO is already minimal per AD6/AD7 |
| 2.1/2.2 | `SentenceEmbedderContract.cs` (10 tests) | Unit (NUnit EditMode) | N/A (new file) | Written first (reference `ISentenceEmbedder`, which did not exist until 2.2) | Written; **execution pending human Unity Test Runner** | 10 tests cover IsReady-safety, no-throw battery, not-ready->Empty, blank-text->Empty, ready+text->non-empty, length-consistency across 3 distinct inputs, finiteness, determinism, defensive-copy isolation, non-degeneracy | N/A — base class mirrors `RequirementResponderContract` structure exactly |
| 3.1 | `ContractTypeTests.cs` (pin rename) + `Contract.cs` + `CONTRACT-CHANGELOG.md` | N/A (structural/config) | N/A | N/A — pin rename is a 1:1 substitution, no new behavior | Written; **execution pending human Unity Test Runner** (`ContractVersionChangelogTests`, unedited, will read the new `## v4` heading) | Skipped: single deterministic outcome (a version integer and a changelog heading), no branching logic | N/A |

**Honest limitation, stated explicitly per the orchestrator's instruction**: this session has no
Unity Editor or CLI test runner. Every GREEN cell above means "written and reviewed against the
compiling precedent (`RequirementResponderContract.cs`, `ClinicalCaseId.cs`/`RequirementId.cs`,
`ContractTypeTests.cs`)," never "executed and observed passing." Task 4.1 (full EditMode run) is
the pending human gate that turns this into a confirmed GREEN, exactly as documented for the
immediate precedent change `2026-09-16-m0-puerto-requerimientos-juntas`.

### Test Summary
- **Total tests written**: 16 (6 `ContractTypeTests` cases + 10 `SentenceEmbedderContract` tests)
- **Total tests passing**: 0 confirmed by execution (Unity Editor unavailable to any agent this session); 16 written and source-reviewed as expected-to-pass
- **Layers used**: Unit (NUnit EditMode) x16
- **Approval tests** (refactoring): None — no refactoring tasks, all additive
- **Pure functions created**: `SentenceEmbedding` is a fully pure/immutable value type; `ISentenceEmbedder` stubs (`EmbebedorDeOracionesNoListo`, `EmbebedorDeOracionesDePrueba`) are deterministic pure functions of their input text

## Work Unit Evidence

| Evidence | U1 (`SentenceEmbedding` + `ContractTypeTests`) | U2 (`ISentenceEmbedder` + `SentenceEmbedderContract` + bump) |
|---|---|---|
| Focused test command and exact result | Test Runner, filter `ContractTypeTests` — not executed this session (no Unity Editor/CLI); written and reviewed, 6/6 new cases expected green, v1-v3 cases untouched | Test Runner, filter `SentenceEmbedderContract\|ContractVersionChangelogTests` — not executed this session; written and reviewed, 10/10 expected green against `SentenceEmbedderContractStubTests` |
| Runtime harness command/scenario and exact result | N/A — pure C#, no scene/VR (per tasks.md) | N/A — pure C#, no scene/VR (per tasks.md) |
| Rollback boundary | Delete `SentenceEmbedding.cs`+`.meta`, revert the 6 `ContractTypeTests` cases; `Contract.Version` stays `3`, no `## v4` | Revert `Version` to `3`, delete `## v4`, delete the port and `SentenceEmbedderContract.cs`+`.meta`; no module references it yet |

## Diff Measurement (Task 4.2)

Measured with `git diff --stat` scoped to `Runtime/Core/`, `Tests/EditMode/Core/`,
`Docs/CONTRACT-CHANGELOG.md`, including the 2 new untracked files (measured via
`git add -N` then `git diff --stat`, then reverted with `git reset` — no content touched,
files remain untracked as before):

```
Docs/CONTRACT-CHANGELOG.md                         |  65 ++++++
Runtime/Core/Contract.cs                           |   2 +-
Runtime/Core/Ports.cs                              |  28 +++
Runtime/Core/SentenceEmbedding.cs                  |  93 +++++++++
Runtime/Core/SentenceEmbedding.cs.meta             |   2 +
Tests/EditMode/Core/ContractTypeTests.cs           |  70 ++++++-
Tests/EditMode/Core/SentenceEmbedderContract.cs    | 225 +++++++++++++++++++++
Tests/EditMode/Core/SentenceEmbedderContract.cs.meta |   2 +
8 files changed, 483 insertions(+), 4 deletions(-)
```

**Total: 487 authored lines** (483 + 4). This is under the 800-line session budget (61% of
budget), close to the design's `Total ~460-620` forecast. **Contingency NOT triggered**:
`SentenceEmbedderContract.cs` stays in the single PR; no PR2 split needed, unlike the
precedent `2026-09-16-m0-puerto-requerimientos-juntas` (which did exceed its 400-line default
budget and did split into PR2/PR2b).

## Scope Confirmation (Task 4.4)

`git status --short` confirms this change touched exactly:
`Runtime/Core/Contract.cs`, `Runtime/Core/Ports.cs`, `Runtime/Core/SentenceEmbedding.cs(.meta)`,
`Tests/EditMode/Core/ContractTypeTests.cs`, `Tests/EditMode/Core/SentenceEmbedderContract.cs(.meta)`,
`Docs/CONTRACT-CHANGELOG.md`. All other modified/untracked paths in the working tree
(`Docs/MODULES.md`, `Runtime/Harness/*`, `Samples~/Harness/*`, `Training/*`, `Data/Speech/*`,
various `openspec/changes/*` and `openspec/specs/*` `.meta` files, `openspec/config.yaml`)
belong to other in-progress work on this branch (`feat/m11-03-compositor-arnes`) and were not
touched, read for editing, or otherwise modified by this change.

## Deviations from Design

None — implementation matches design.md exactly, including the binding clarification from the
orchestrator (no public property exposing the raw backing array; only `Length`, indexer,
`ToArray()`).

One implementation choice not fully specified by design.md: the stub
`EmbebedorDeOracionesDePrueba`'s word-to-bucket hash uses a simple sum-of-character-codes modulo
8 cubetas, rather than `string.GetHashCode()`. This avoids any dependency on .NET's per-process
string hash randomization and keeps the stub's determinism trivially reasonable by inspection.
Design.md only specified "hashing trivial de palabras a 8 cubetas con conteos enteros," which
this satisfies.

## Issues Found

None in the written code. Verified by direct computation that `FraseDePrueba` ("me duele el
pecho desde ayer") and `FraseDeControl` ("que clima hace hoy") produce different bucket vectors
under the stub's hash function, so `Textos_distintos_no_dan_el_mismo_vector` is a real,
non-tautological assertion (not trivially true by construction).

## Remaining Tasks

- [ ] 4.1 Human runs Unity Test Runner EditMode (Run All) and confirms green for
      `NpcAi.Core.Tests`, including all 6 new `ContractTypeTests` cases, all 10
      `SentenceEmbedderContractStubTests` cases, the renamed version pin, and
      `ContractVersionChangelogTests`/`CoreAssemblyPurityTests`.
- [ ] 4.3 Human requests M0 cross-review from Luis Miguel Canaveral Restrepo (or the advisor)
      and records the approval, replacing the `PENDIENTE` placeholder in
      `Docs/CONTRACT-CHANGELOG.md` `## v4`.
- [ ] Git commits — the confirmed plan (single PR, 2 sequential commits: commit 1 = U1 files,
      commit 2 = U2 files) was NOT executed in this apply session. Per the repo's global
      CLAUDE.md hard rule ("Only create commits when requested by the user... NEVER commit
      changes unless the user explicitly asks you to"), no `git commit` was run absent an
      explicit commit instruction. The working tree is ready for that exact 2-commit split
      whenever committing is explicitly requested:
      - Commit 1 (U1): `Runtime/Core/SentenceEmbedding.cs`, `Runtime/Core/SentenceEmbedding.cs.meta`, the 6 new cases in `Tests/EditMode/Core/ContractTypeTests.cs`.
      - Commit 2 (U2): `Runtime/Core/Ports.cs`, `Tests/EditMode/Core/SentenceEmbedderContract.cs`, `Tests/EditMode/Core/SentenceEmbedderContract.cs.meta`, `Runtime/Core/Contract.cs`, `Docs/CONTRACT-CHANGELOG.md`, and the pin-rename hunk of `ContractTypeTests.cs`.

## Workload / PR Boundary

- Mode: single PR (per `ask-on-risk` resolution recorded in tasks.md), 2 sequential commits
- Current work unit: U1 + U2, both completed in this batch
- Boundary: this batch starts from an untouched `NpcAi.Core`/`NpcAi.Core.Tests` (contract v3)
  and ends with contract v4 fully specified in source, ready for the human Test Runner gate
- Estimated review budget impact: 487 authored lines, 61% of the session's 800-line budget;
  well under — no chained PR needed

## Status

10/12 tasks complete. The 2 remaining tasks (4.1, full EditMode run; 4.3, M0 cross-review
request) are explicit human-only gates per this project's convention (no agent runs Unity, and
cross-review is a human/organizational action outside this agent's scope); the changelog's
co-review placeholder is left correctly pending. **Ready for `sdd-verify`** once the human
confirms Test Runner green — verify can still run its own independent requirements/spec-compliance check against the written
source in the meantime, same pattern as the precedent's 1st verify pass.
