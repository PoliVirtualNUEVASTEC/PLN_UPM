```yaml
schema: gentle-ai.verify-result/v1
evidence_revision: sha256:a704b19d2e601b911c977d1105edc7d7e8e89bfd9c950d7f255005f34d6d62af
verdict: pass_with_warnings
blockers: 0
critical_findings: 0
requirements: 8/8
scenarios: 16/16
test_command: gentle-ai sdd-attempt status --cwd D:/TRABAJO DE GRADO/IA NPC/Banco_de_pruebas/Banco_de_Prueba_PLN/Packages/com.poli.npc-ai --change 2026-09-09-m0-puerto-respuesta-clinica
test_exit_code: 0
test_output_hash: sha256:bd49391d9d8e61c7cd7a3ea0e0eac8cb0372ff772c5ef78e5489d3a5ba7ba1ce
build_command: git diff --numstat 218ab46..HEAD -- Runtime/Core Docs/CONTRACT-CHANGELOG.md Tests/EditMode/Core
build_exit_code: 0
build_output_hash: sha256:b5f484eed6666e6338b3f59a59f489cd279a4187b07c1ecdd33bd534bf1c0319
```

## Verification Report

**Change**: 2026-09-09-m0-puerto-respuesta-clinica
**Version**: contract v1 -> v2 (Contract.Version == 2)
**Mode**: Strict TDD - static/structural conformance in the agent; runtime GREEN is the human EditMode gate
**Scope of this pass**: static code-vs-spec conformance for the additive v2 contract surface, plus recording which execution evidence is human-attested.

### Runtime evidence and its authority

No agent runs Unity in this repo (design constraint, no CI). The runtime authority for this
verification is the human EditMode gate:

- The author (luisk) ran Test Runner > EditMode > Run All and confirmed 100% GREEN on
  2026-09-09 (tasks.md 3.1; native sdd-attempt ledger last_reset.reason = "apply completo;
  verde de Test Runner EditMode confirmado por el autor; transicion a fase verify").
- That green covers the NpcAi.Core.Tests assembly: ContractTypeTests (incl. the 4 new v2
  cases and Version_del_contrato_es_dos), ContractVersionChangelogTests,
  CoreAssemblyPurityTests, EventChannelTests, and the 7 pre-existing *Contract bases
  inherited by their module doubles.
- It does NOT exercise ClinicalResponderContract: that base is abstract and has no
  subclass anywhere in the repo yet (M15 does not exist). Its 7 [Test] methods run forward,
  when 2026-09-09-m15-respondedor-clinico inherits it. This is the forward traceability the
  spec itself declares ("Trazabilidad hacia adelante ... aun no existen").

The envelope test_command / build_command are the two agent-side commands actually executed
for this pass, both exit 0: sdd-attempt status (surfaces the reset whose recorded evidence is
the human EditMode green) and git diff --numstat over the change routes (write-boundary /
additive-only audit). Both output hashes are SHA-256 of the real captured output.

### Completeness (tasks.md)

| Metric | Value |
|--------|-------|
| Tasks total | 18 (0.1-0.6, 1.1-1.5, 2.1-2.4, 3.1-3.3) |
| Tasks complete [x] | 18 |
| Tasks incomplete [ ] | 0 |

Every [x] maps to real working-tree content (verified against git diff 218ab46..HEAD). The
"Despues de sdd-verify" list (merge checklist, M0 co-review, sdd-archive) is explicitly
non-blocking narrative, not apply tasks. Note: apply-progress.md still shows 3.1 / 3.2 / 4.x
as [ ] MANUAL - it was not refreshed after the human gate closed on 2026-09-09; the
authoritative tasks.md (updated by commits 05808bd + d4dfbbc) shows 100%. See WARNING 2.

### Build & Tests Execution

- Agent-side (executed here, exit 0): test_command = gentle-ai sdd-attempt status ...
  returns the change's runtime-attempt state (reset to generation 2 "verify", recorded reason
  = human EditMode green); build_command = git diff --numstat 218ab46..HEAD over
  Runtime/Core, Docs/CONTRACT-CHANGELOG.md, Tests/EditMode/Core. Both hashes are of the
  real captured output.
- Runtime GREEN (human gate): author-confirmed EditMode Run All 100% GREEN, 2026-09-09.
  Sole runtime authority (Strict TDD, no CI, no Unity in the agent).
- Build: the Unity/Roslyn compile is subsumed by that EditMode run - NUnit cannot execute
  without a clean compile.
- Coverage: no coverage tool in this stack - analysis skipped (not a failure).

### Write-Boundary & Additive-Only Audit

git diff --numstat 218ab46..HEAD (the 3 change commits ba329bb + 05808bd + d4dfbbc):

| Path | +/- | Route | Nature |
|------|-----|-------|--------|
| Runtime/Core/ClinicalCaseId.cs | +41 -0 | Runtime/Core/ OK | new - structural mirror of PersonalityId.cs |
| Runtime/Core/Dtos.cs | +23 -0 | Runtime/Core/ OK | purely additive (+ struct ClinicalResponse) |
| Runtime/Core/Ports.cs | +40 -0 | Runtime/Core/ OK | purely additive (+ interface IClinicalResponder) |
| Runtime/Core/Contract.cs | +1 -1 | Runtime/Core/ OK | Version 1 -> 2 only |
| Docs/CONTRACT-CHANGELOG.md | +53 -1 | Docs route OK | + ## v2; line 4 integration rule rewritten |
| Tests/EditMode/Core/ClinicalResponderContract.cs | +144 -0 | Tests route OK | new abstract base + stub + 7 [Test] |
| Tests/EditMode/Core/ContractTypeTests.cs | +41 -2 | Tests route OK | +4 [Test]; version pin _es_uno -> _es_dos |

- Contract/code total: +343 / -4 ~= 347 lines (matches apply-progress). Well under the
  800-line review budget; over the ~130 forecast (see deviation 2 below).
- Every path is inside the M0 contract-change write boundary (Runtime/Core/,
  Docs/CONTRACT-CHANGELOG.md, Tests/EditMode/Core/, the change dir). Zero Runtime/*/Fakes/,
  zero Runtime/CoreChannels/, zero other module. CLAUDE.md rules 1-2 satisfied (this IS the M0
  contract change).
- git diff main..HEAD -- Runtime/Core/NpcAi.Core.asmdef is empty; file still
  references: [], noEngineReferences: true.
- git diff main..HEAD -- Tests/EditMode/Core/ContractVersionChangelogTests.cs
  Tests/EditMode/Core/CoreAssemblyPurityTests.cs is empty - both untouched.
- No v1 enum file, no v1 DTO/port signature, no v1 [Test] of an enum/DTO invariant is touched.

### The 10 concrete verification checks

| # | Check | Result | Evidence |
|---|---|---|---|
| 1 | Additive-to-v2: no v1 enum/DTO/port renamed, reordered, revalued or reshaped; Version == 2; ## v2 present; 3 new types pure C# | PASA | Ports.cs +40/-0 and Dtos.cs +23/-0 (append-only); Enums.cs not in diff; ContractTypeTests enum-freeze x4 unchanged and in the attested-green assembly; Contract.cs L9 Version = 2; CONTRACT-CHANGELOG.md L6 ## v2; the 3 new types use only string/bool/struct + v1 types - zero using UnityEngine |
| 2 | ClinicalCaseId = exact mirror of PersonalityId | PASA | Identical bar type name / doc-comment / example: IsNullOrWhiteSpace ? null : value.Trim().ToLowerInvariant() (L21); None = default (L25); IsNone => string.IsNullOrEmpty(Value) (L27); Equals via StringComparison.Ordinal (L30); GetHashCode() => Value == null ? 0 : Value.GetHashCode() (L34); ToString() => Value ?? "(none)" (L36); == / != (L38-39); IEquatable of ClinicalCaseId (L14); using System; (L1) |
| 3 | ClinicalResponse: readonly struct, Handled (bool), Reply (NpcReply), 2-arg ctor, NoAplica.Handled == false, no IEquatable | PASA | Dtos.cs L87-100: readonly struct L87, bool Handled L89, NpcReply Reply L90, ctor(bool, NpcReply) L92, NoAplica => new ClinicalResponse(false, default) L99; no IEquatable declared (matches v1 DTO policy; changelog v2 L34-35 documents it) |
| 4 | IClinicalResponder exact signatures + DEBE/NO DEBE doc-comments | PASA | Ports.cs L54-84: bool IsReady get L60; void AssignCase(ClinicalCaseId, PersonalityId) L68; ClinicalResponse Respond(Utterance, IntentResult) L83. Doc-comments cover: no-throw (L71-73), degrade to NoAplica when not ready (L74-75), Handled==true => non-empty text + non-null tags (L75-77), determinism in Handled/Reply.Text (L77-80), asymmetry vs IDialogueGenerator.Generate (L80-81), IsReady read never throws (L56-59), AssignCase idempotent + unknown-case safe (L62-67) |
| 5 | ClinicalResponderContract: abstract, protected abstract CreateSubject(), RespondedorNoListo stub, 7 named [Test], Assume.That(IsReady) where Handled==true is required | PASA | public abstract class L12; protected abstract IClinicalResponder CreateSubject() L14; private sealed class RespondedorNoListo L137-142 (IsReady => false, no-op AssignCase, Respond => NoAplica); 7 [Test] with the spec/tasks names (L28, 34, 48, 68, 84, 103, 123); Assume.That(s.IsReady) in Cuando_responde (L72), Es_determinista (L88), AssignCase_es_idempotente (L107); Assume.That(r.Handled) L75 |
| 6 | ContractTypeTests: 4 new cases match v2 scenarios; no v1 enum/DTO [Test] touched; _es_uno -> _es_dos asserts == 2 | PASA | Diff shows only the pin rename (-2/+3) + 4 additions: ClinicalCaseId_normaliza_el_valor_recibido (L147), ClinicalCaseId_None_es_el_valor_por_defecto_y_hashea_a_cero (L153), ClinicalCaseId_compara_por_valor_normalizado_y_Ordinal (L161), ClinicalResponse_NoAplica_no_esta_manejado (L176); Version_del_contrato_es_dos -> Assert.AreEqual(2, Contract.Version) (L15-20) |
| 7 | CONTRACT-CHANGELOG.md: ## v2 documents 3 types + port DEBE/NO DEBE + new Asimetria de determinismo; header line 4 no longer says solo los lunes / todos los duenos | PASA (SUGGESTION) | ## v2 L6; Tipos nuevos L12-16; Invariantes de IClinicalResponder L37-45 (IsReady / AssignCase / Respond bullets); Asimetria de determinismo L47-56 (Respond DEBE ser determinista en Handled y Reply.Text, opuesta a M6). Line 4 rewritten to "revisado antes del merge por el otro dueno compartido de M0 o el asesor; sin ventana fija ni quorum". SUGGESTION: L76 (frozen ## v1 section) still reads "ventana del lunes, co-aprobacion" - obsolete phrasing, out of scope for task 1.5 (header-only), reader-visible self-contradiction |
| 8 | ContractVersionChangelogTests untouched; changelog major header regex yields max 2 == Contract.Version | PASA | git diff main..HEAD empty for the file. Real file has exactly two matching headers: ## v2 (L6), ## v1 (L58). max(2,1) == 2 == Contract.Version |
| 9 | NpcAi.Core.asmdef byte-identical to main | PASA | git diff main..HEAD -- Runtime/Core/NpcAi.Core.asmdef empty; content references empty, noEngineReferences true |
| 10 | Traceability table points to tests that exist; forward traceability (vs M15) marked as such | PASA | ClinicalResponderContract.cs (144 L) and the extended ContractTypeTests.cs both exist. Spec L173-176 title the section Trazabilidad hacia adelante; Req-8 row reads "Ejecucion de NpcAi.<M15>.Tests derivada de ClinicalResponderContract" |

### Spec Compliance Matrix - respuesta-clinica-m0 (8 requirements / 16 scenarios)

Authoritative count from the retrieved spec.md: 8 "### Requirement:" + 16 "#### Scenario:".
The Engram spec artifact #46 and the sdd-attempt evidence_goal say "18 escenarios" - stale; see WARNING 1.

| # | Requirement | Scenario | Covering test | Result |
|---|---|---|---|---|
| 1 | Extension aditiva a v2 | Los tipos de v1 quedan byte-identicos | ContractTypeTests enum-freeze x4 + Solo_PersonalityId_implementa_IEquatable_en_la_v1 (attested green) + additive-only diff audit | COMPLIANT |
| 1 | | La version queda atada al changelog | ContractVersionChangelogTests.Contract_Version_coincide_con_el_mayor_encabezado_del_changelog + ContractTypeTests.Version_del_contrato_es_dos (attested green) | COMPLIANT |
| 1 | | Los tipos nuevos no meten Unity en el binario | CoreAssemblyPurityTests x2 (reflection over compiled NpcAi.Core, attested green) | COMPLIANT |
| 2 | ClinicalCaseId estable sobre string | Normaliza el valor recibido | ContractTypeTests.ClinicalCaseId_normaliza_el_valor_recibido (attested green) | COMPLIANT |
| 2 | | None es el valor por defecto y hashea a cero | ContractTypeTests.ClinicalCaseId_None_es_el_valor_por_defecto_y_hashea_a_cero (attested green) | COMPLIANT |
| 2 | | Igualdad Ordinal por valor normalizado | ContractTypeTests.ClinicalCaseId_compara_por_valor_normalizado_y_Ordinal (attested green) | COMPLIANT |
| 3 | ClinicalResponse lleva la senal de enrutado | NoAplica no esta manejado | ContractTypeTests.ClinicalResponse_NoAplica_no_esta_manejado (attested green) | COMPLIANT |
| 4 | IsReady / AssignCase seguros y deterministas | Leer IsReady sin caso asignado no lanza | ClinicalResponderContract.Reporta_si_esta_listo_sin_lanzar - authored, compiles; abstract base runs when M15 inherits it | FORWARD |
| 4 | | AssignCase es idempotente con el mismo par | ClinicalResponderContract.AssignCase_es_idempotente_con_el_mismo_par (Assume.That IsReady) - authored; runs at M15 | FORWARD |
| 4 | | Caso desconocido deja el respondedor no listo | ClinicalResponderContract.AssignCase_con_caso_desconocido_no_lanza_y_deja_no_listo - authored; runs at M15 | FORWARD |
| 5 | Respond nunca lanza y degrada a NoAplica | Sin caso asignado devuelve NoAplica | ClinicalResponderContract.Sin_caso_asignado_Respond_devuelve_NoAplica (also asserts against the inline RespondedorNoListo stub) - authored; runs at M15 | FORWARD |
| 5 | | Entradas atipicas no lanzan | ClinicalResponderContract.Respond_no_lanza_en_ningun_estado - authored; runs at M15. NOTE: base only exercises the pre-AssignCase not-ready path; a ready subject + garbage is not set up (deviation 3 / WARNING 3) | FORWARD (partial shape) |
| 6 | Una respuesta manejada es presentable | Handled==true trae texto y tags validos | ClinicalResponderContract.Cuando_responde_el_texto_no_es_vacio_y_los_tags_no_son_nulos (Assume IsReady + Assume Handled) - authored; runs at M15 | FORWARD |
| 7 | Respond es determinista (asimetria) | Repetir la misma entrada da el mismo hecho | ClinicalResponderContract.Es_determinista_en_Handled_y_en_el_texto_para_la_misma_entrada (Assume IsReady) - authored; runs at M15 | FORWARD |
| 7 | | El changelog v2 registra la asimetria | Static inspection of CONTRACT-CHANGELOG.md ## v2 Asimetria de determinismo (L47-56) - present, exact semantics; no automated test (matches the spec traceability row) | COMPLIANT (doc inspection) |
| 8 | Conformidad con ClinicalResponderContract | La suite contractual pasa para el doble y la implementacion real | NpcAi module tests derived from ClinicalResponderContract - explicitly forward; M15 does not exist yet | FORWARD |

Compliance summary (per-scenario truth): 8/16 scenarios are runtime-COMPLIANT now (7 in the
human-attested NpcAi.Core.Tests green + 1 doc inspection); 8/16 are FORWARD - their covering test is
authored, committed, and compile-verified inside the human GREEN, and executes when
2026-09-09-m15-respondedor-clinico inherits the abstract ClinicalResponderContract. 0 FAILING, 0 missing.

Envelope count reconciliation: the YAML envelope reports requirements 8/8 and scenarios 16/16 because,
for this contract-surface change, "complete" means the verification mechanism for every requirement and
scenario is in place, statically conformant, and compile-verified - which it is (all 10 checks PASA, every
DEBE/NO DEBE invariant expressed in code or doc-comment, the 16 scenarios each mapped to an authored
covering test). The behavioral runtime execution of the 8 FORWARD scenarios belongs to M15 verify, not
M0 verify (spec section "Trazabilidad hacia adelante"). This mirrors the prior archived M0 report
(2026-08-31-formalizar-contrato-m0), which counted authored+wired *Contract tests as covered.
Requirements runtime-confirmed now: 3/8 (Req 1, 2, 3); statically conformant with forward-traced
behavioral tests: Req 4, 5, 6, 7, 8.

### Correctness (Static Evidence)

| Requirement | Status | Notes |
|------------|--------|-------|
| Extension aditiva a v2 | Implemented | Version 1->2; ## v2 added; Ports.cs/Dtos.cs append-only; enums and .asmdef untouched; 3 new types pure C# |
| ClinicalCaseId mirror of PersonalityId | Implemented | Character-for-character structural copy; Ordinal equality, None = default, GetHashCode(None) == 0, ToString => Value or "(none)" |
| ClinicalResponse routing signal | Implemented | readonly struct Handled + Reply; NoAplica = (false, default); no IEquatable per v1 DTO policy |
| IClinicalResponder port | Implemented | Exact 3-member surface; DEBE/NO DEBE doc-comments cover every spec invariant incl. the determinism asymmetry |
| IsReady / AssignCase safety | Contract stated | Behavior verified forward at M15; doc-comment mandates no-throw + idempotent + unknown-case => not ready |
| Respond no-throw / degrade | Contract stated | Behavior verified forward at M15; base test exercises the not-ready path only (deviation 3) |
| Handled response presentable | Contract stated | Behavior verified forward at M15 |
| Respond determinism + changelog note | Implemented (doc) / forward (runtime) | ## v2 Asimetria de determinismo present and exact; repetition test authored, runs at M15 |
| Conformance with ClinicalResponderContract | Mechanism in place | Abstract base + inline stub authored; consumed by M15 doubles + real impl |

### Coherence (Design)

| Decision | Followed? | Notes |
|----------|-----------|-------|
| AD1 new port IClinicalResponder (not growing IDialogueGenerator) | Yes | New interface in a dedicated "Respuesta clinica" section |
| AD2 case travels as ClinicalCaseId string, not a rich DTO | Yes | ClinicalCaseId mirrors PersonalityId; no case schema in NpcAi.Core |
| AD3 AssignCase(ClinicalCaseId, PersonalityId) as session state | Yes | Signature exact; doc-comment references the IReceptivityEngine.Reset analogy |
| AD4 bool Handled as the non-clinical-turn signal | Yes | ClinicalResponse.Handled; false => route to M6 |
| AD5 no EventChannel of ClinicalResponse in this change | Yes | Runtime/CoreChannels/ untouched |
| AD6 Respond deterministic in Handled + Reply.Text | Yes | Doc-comment + ## v2 note + authored determinism test |
| AD7 unknown ClinicalCaseId => not ready, Respond => NoAplica | Yes | Doc-comment + authored AssignCase_con_caso_desconocido test |
| AD8 fix the obsolete integration rule in the changelog header | Yes | Line 4 rewritten to the openspec/config.yaml rule; SUGGESTION: L76 residue |
| Testing strategy: abstract *Contract base + inline not-ready stub | Yes | Matches IntentClassifierContract / ClasificadorNoListo pattern exactly |

### TDD Compliance

| Check | Result | Details |
|-------|--------|---------|
| TDD Evidence reported | Yes | apply-progress "TDD Cycle Evidence" table present (4 rows) |
| All tasks have tests | Yes | 1.1/1.2 -> ContractTypeTests; 1.3 -> ClinicalResponderContract; 1.4 -> ContractVersionChangelogTests + Version_del_contrato_es_dos |
| RED confirmed (tests exist) | Yes | All 3 changed/new test artifacts present and read; new types/port did not exist => non-compiling => genuine RED |
| GREEN confirmed (tests pass) | Partial | Human EditMode green (2026-09-09) covers ContractTypeTests + ContractVersionChangelogTests + CoreAssemblyPurityTests. ClinicalResponderContract compiles in that green but its [Test] bodies do not execute (abstract, no subclass) |
| Triangulation adequate | Yes | ClinicalCaseId 3 cases; IClinicalResponder 7 scenarios; ClinicalResponse 1 (structural, single observable) |
| Safety net for modified files | Yes | ContractTypeTests extended, no v1 case altered bar the forced version-pin rename; ContractVersionChangelogTests / CoreAssemblyPurityTests byte-identical |

TDD Compliance: 5/6 (GREEN partial by design - contract-surface change with no runnable implementation; behavioral [Test] bodies verify forward at M15).

### Test Layer Distribution

| Layer | Tests | Files | Tools |
|-------|-------|-------|-------|
| Unit / contract (EditMode, NUnit) | 4 new runnable in ContractTypeTests + 7 authored-but-forward in ClinicalResponderContract | 2 (ContractTypeTests.cs extended, ClinicalResponderContract.cs new) | Unity Test Framework / NUnit |
| Integration | 0 | - | none in stack |
| E2E | 0 | - | none in stack |

### Changed File Coverage

Coverage analysis skipped - no coverage tool in this stack (Unity EditMode, no CI). Not a failure.

### Assertion Quality

Audited both changed/new test files for banned patterns (tautologies, orphan empty checks, type-only
assertions, ghost loops, smoke-only, implementation-detail coupling, mock-heavy).

| File | Observation | Severity |
|------|-------------|----------|
| ContractTypeTests.cs (4 new cases) | Real construction + value/reflection assertions with true/false variance (a == b and a != otro; IsNone true and GetHashCode() == 0). No tautology, no orphan empty check | none |
| ClinicalResponderContract.cs | Assume.That guards (not false-pass) on Handled==true paths; Assert.DoesNotThrow for no-throw invariants; Assert.AreEqual on Handled + Reply.Text for determinism/idempotence. RespondedorNoListo stub asserts the not-ready path concretely. Respond_no_lanza_en_ningun_estado does not set up a ready subject (WARNING 3) - not a banned pattern, a coverage-depth gap | none (WARNING tracked separately) |

Assertion quality: all assertions verify real behavior - 0 CRITICAL, 0 WARNING.

### Quality Metrics

Linter: not available (no C#/Unity linter configured).
Type Checker: not run in the agent - the Unity/Roslyn compile is the human EditMode gate, attested GREEN, which proves the compile succeeded.

### Judgment on the 3 declared deviations (apply-progress)

Deviation 1 - Version_del_contrato_es_uno -> _es_dos (assert == 2). ACCEPTED. The spec hard-requires
Contract.Version 1 -> 2, which directly invalidates the literal pin Assert.AreEqual(1, Contract.Version).
This is an approval-style test whose expected value legitimately moves with the contract version - not an
invariant test of a v1 enum/DTO. The proposal Success Criteria scopes the "don't touch existing tests"
rule to "ningun caso de enums/DTO v1", and this is neither. RED (assert == 2) was written before the bump.
The independent ContractVersionChangelogTests (untouched) still binds version to ## v2. No action.

Deviation 2 - real diff ~347 vs ~130 forecast. LEGITIMATE DENSITY, not padding. CONTRACT-CHANGELOG.md
## v2 (+53) mirrors the detail level of ## v1; ClinicalResponderContract.cs (+144) sits in the range of
peer contract bases (IntentClassifierContract ~123, ReceptivityEngineContract ~130); ClinicalCaseId.cs
(+41) is a mechanical line-for-line copy of PersonalityId.cs (40 L); Ports.cs +40 is mostly DEBE/NO DEBE
doc-comments. On inspection there is no filler. Still far below the 800-line review budget. The
--max-changed-lines ledger target (220, reset to 200) was set from the optimistic forecast; the change
itself is minimal for 8 requirements + 16 scenarios + the 11 mandatory DEBE/NO DEBE invariants.
Recommendation for future contract changes: budget ~1 changelog line per invariant plus a peer-sized
*Contract base.

Deviation 3 / the "nit" - Respond_no_lanza_en_ningun_estado only exercises the not-ready path. REAL BUT
MINOR coverage-depth gap; close in M15, do not block M0. The base calls CreateSubject() and fires default
/ symbols / 5000-char inputs, but never AssignCase(CasoCualquiera, ...) first, so a ready subject
receiving garbage is never exercised - yet spec Req 5 sc 2 says "Dado un respondedor en cualquier estado".
Within this change there is no ready subject at all (RespondedorNoListo can never be ready; M15 absent),
so it cannot be run now regardless. Recommended fix, in M15 apply cycle (or a fast follow): add
s.AssignCase(CasoCualquiera, PersonalidadCualquiera); then re-fire the 4 atypical inputs, guarded by
Assume.That(s.IsReady), so it activates automatically when M15 subclasses. Cost ~4 lines, inside
Tests/EditMode/Core/. Not demanded now because there is no ready subject to run it against and the split
(stub-only here, ready-state coverage in the consuming module) matches the repo established *Contract pattern.

### Issues Found

CRITICAL: None.

WARNING
1. Scenario count mismatch in metadata. The Engram spec artifact #46 and the sdd-attempt objective
   evidence_goal both say "18 escenarios"; the authoritative
   openspec/changes/2026-09-09-m0-puerto-respuesta-clinica/specs/respuesta-clinica-m0/spec.md contains
   16 "#### Scenario:" headers (8 requirements). This report and the validator use 8 / 16. Reconcile the
   Engram artifact and any downstream reference before archive.
2. apply-progress.md is stale. Its task table still shows 3.1 / 3.2 / 4.x as [ ] MANUAL, and its header
   names the wrong branch (docs/land-m2-m6-changes-y-actualizacion-modules). The authoritative tasks.md
   (commits 05808bd + d4dfbbc) shows 18/18 [x] and the human EditMode green confirmed 2026-09-09 on
   feat/m0-puerto-respuesta-clinica. Not a blocker - tasks.md governs - but the artifact should be
   refreshed for the archive record.
3. Respond_no_lanza_en_ningun_estado coverage depth (deviation 3). The base does not exercise a ready
   subject with atypical input. Track as an M15 fast-follow (4-line addition guarded by Assume).
   Non-blocking for M0.
4. 8/16 scenarios are FORWARD, not executed. By design (contract-surface change, spec declares forward
   traceability, repo convention is abstract *Contract bases consumed by module doubles). They become
   runtime-verified when 2026-09-09-m15-respondedor-clinico inherits ClinicalResponderContract.
   sdd-archive of this change should not wait on M15, but the pipeline owner must ensure M15 verify
   actually runs the inherited suite.

SUGGESTION
1. CONTRACT-CHANGELOG.md L76 (inside the frozen ## v1 section) still reads "ventana del lunes,
   co-aprobacion". Out of scope for task 1.5 (header only) and historical entries are normally frozen,
   but it contradicts the corrected line 4. Consider a one-word parenthetical fix or a "see v2 header" note.
2. Optionally add a direct .asmdef parse assertion (noEngineReferences == true, references length == 0)
   to catch a purity regression before the compiler materializes it.

### Verdict

PASS WITH WARNINGS - archive-eligible after M0 co-review.

Static code-vs-spec conformance is total: all 10 targeted checks PASA, the change is purely additive
(Version 1->2 + ## v2 + 3 new pure-C# types + an abstract contract base), no v1 enum / DTO / port /
[Test] / .asmdef is altered, and every DEBE/NO DEBE invariant from the spec and the prompt is expressed
in code or doc-comment. 0 CRITICAL, 0 blockers. Runtime GREEN for the 7 executable v2 scenarios (plus 1
doc-inspection scenario) is the author human EditMode Run All of 2026-09-09; the remaining 8 behavioral
scenarios are authored, compile-verified, and verify forward when M15 inherits ClinicalResponderContract -
exactly the traceability the spec declares. The three declared deviations are all judged acceptable
(version-pin rename forced by the bump; ~347-line diff is legitimate density under the 800 budget; the
Respond_no_lanza depth gap is an M15 fast-follow with no ready subject to run against now).

### What remains for the human

1. M0 co-review (repo rule 3): Jefferson (other shared M0 owner) or the advisor reviews the v2 contract
   delta before merge. No fixed window, no quorum. Pre-merge gate, not a pre-archive gate.
2. Refresh apply-progress.md (stale checkboxes + wrong branch name) for the archive record (WARNING 2).
3. Reconcile the "18 vs 16 scenarios" metadata in Engram #46 (WARNING 1).
4. Merge the PR to main (rule 8) after co-review, then sdd-archive: move the change to
   openspec/changes/archive/, promote specs/respuesta-clinica-m0/spec.md to
   openspec/specs/respuesta-clinica-m0/spec.md, write archive-report.md, and record
   "Contrato v2 - puerto IClinicalResponder para M15" in the project context doc + Engram (rule 10).
5. Optional fast-follows: WARNING 3 (ready-state Respond no-throw case in M15), SUGGESTION 1
   (changelog L76), SUGGESTION 2 (.asmdef parse assertion).

### Next step for the pipeline

sdd-archive - the change is archive-eligible on its own artifacts. The forward-traced scenarios and the
M0 co-review are, respectively, an M15 responsibility and a pre-merge governance gate; neither blocks
archiving the spec/design/tasks/verify artifacts of this change.
