```yaml
schema: gentle-ai.verify-result/v1
evidence_revision: sha256:0c4308a65c982a3a1797093178ce4afc22099b9932c54aa22ec0a78d4a797bfc
verdict: pass_with_warnings
blockers: 0
critical_findings: 0
requirements: 18/18
scenarios: 58/58
test_command: gentle-ai sdd-attempt status --cwd D:/TRABAJO DE GRADO/IA NPC/Banco_de_pruebas/Banco_de_Prueba_PLN/Packages/com.poli.npc-ai --change formalizar-contrato-m0
test_exit_code: 0
test_output_hash: sha256:d6b4802a4dc1c34155e00c5b645828d8c5271898dcd53ae9eb6c9e0d61515322
build_command: git diff --numstat -- Runtime/Core Tests/EditMode/Core Docs/CONTRACT-CHANGELOG.md
build_exit_code: 0
build_output_hash: sha256:d09a6c20aac6b258f211a08b3174a72fa611b58f2e89e112adeb70d355acdcf6
```

## Verification Report

**Change**: formalizar-contrato-m0
**Version**: contract v1 (frozen; `Contract.Version == 1`)
**Mode**: Strict TDD - structural/static inspection in the agent; runtime GREEN by the human EditMode gate
**Pass**: 2 (re-verification after the prior FAIL was remediated)

### Re-verification context

The prior `verify-report.md` returned **FAIL** on one CRITICAL (DTO Utterance had zero executable
coverage) plus an un-attestable Strict-TDD GREEN, with four WARNINGs. All findings have since been
addressed. This pass re-checks every prior finding, re-runs the full structural audit, and consumes
the authoritative runtime attestation.

Unity cannot run in the agent environment and the repo has no CI (design D14), so runtime GREEN is a
human gate. The authoritative attestation for this pass:

- A human ran **EditMode > Run All in Unity 6** and recorded **100% GREEN**: **28/28 in
  `NpcAi.Core.Tests`** plus the module test DLLs.
- **D6** (`DestroyImmediate` fires `OnDisable` synchronously) is **confirmed empirically**:
  `EventChannelTests.OnDisable_limpia_los_suscriptores_y_un_Raise_posterior_no_invoca_al_handler_viejo`
  passed in that run.
- The native `gentle-ai sdd-attempt` objective for this change is settled: `complete: true`,
  generation 2, attempt 3 `outcome: passed`, `evidence_revision sha256:0c4308...bfc`; the diagnosis
  records the human EditMode green, the verify remediation, and the `AssertEnumCongelado` fix.

The envelope's `test_command` / `build_command` are the two concrete agent-side verification
commands actually executed for this pass, both exit 0: `gentle-ai sdd-attempt status` (surfaces
the settled `passed` objective whose recorded evidence is the human EditMode green) and
`git diff --numstat` over the change routes (the write-boundary / runtime-minimality audit).
Their `test_output_hash` / `build_output_hash` are the SHA-256 of the real captured output.
The Unity EditMode `Run All` itself is the human runtime gate (design D14), not agent-runnable.

The count `28/28` in `NpcAi.Core.Tests` independently corroborates that the attested run matches the
current tree: the assembly's runnable tests are exactly `ContractTypeTests` (18) +
`ContractVersionChangelogTests` (1) + `CoreAssemblyPurityTests` (2) + `EventChannelTests` (7) = 28.
The seven `*Contract.cs` bases are abstract and run in the module test DLLs, not here.

### Completeness (tasks.md)

| Metric | Value |
|--------|-------|
| Tasks total | 26 |
| Tasks complete `[x]` | 19 |
| Tasks incomplete `[ ]` | 7 |

The 7 open items are all manual gates, none of which an agent can tick:

- **1.4, 2.4, 3.7, 4.8, 5.2** - human EditMode `Run All`. **Satisfied by attestation** (the human
  ran it, 100% GREEN). The checkboxes are stale; the orchestrator/human should tick them.
- **0.6, 5.3** - governance. Contract change; per the **updated** CLAUDE.md rule 2 and the README
  "Antes de mergear" checklist (a separate governance change, see below), it needs **pre-merge
  review by the other shared M0 owner or the advisor** - no Monday window, no all-owner quorum. A
  merge-time gate, tracked separately; it does not block the SDD archive of the change artifacts.

Every `[x]` item maps to real working-tree content (verified against the diff).

### Build & Tests Execution

- **Agent-side (executed here, exit 0)**: `test_command` = `gentle-ai sdd-attempt status ...`
  returns the settled `passed` runtime-attempt objective; `build_command` = `git diff --numstat`
  over `Runtime/Core`, `Tests/EditMode/Core`, `Docs/CONTRACT-CHANGELOG.md` is the write-boundary /
  runtime-minimality audit. Both hashes are of the real captured output.
- **Runtime GREEN (human gate, design D14)**: a human ran EditMode `Run All` in Unity 6 -
  28/28 in `NpcAi.Core.Tests` + the module test DLLs, 100% GREEN. This is the sole runtime
  authority (Strict TDD, no CI, no Unity in the agent). The native `sdd-attempt` objective is
  settled `passed` with that green as its evidence.
- **Build**: the Unity/Roslyn compile is subsumed by that EditMode run - NUnit cannot execute
  without a clean compile, and 28/28 ran.
- Every "COMPLIANT" below means a covering test exists, its logic was reasoned through
  statically, and it sits in the assembly the human attested GREEN.
- **Coverage**: no coverage tool in this stack - analysis skipped (not a failure).

### Write-Boundary Audit

`git status` / `git diff --numstat` - change `formalizar-contrato-m0`:

| Path | +/- | Route |
|------|-----|-------|
| `Docs/CONTRACT-CHANGELOG.md` | +61 -0 | Docs/CONTRACT-CHANGELOG.md OK |
| `Runtime/Core/Dtos.cs` | +2 -2 | Runtime/Core/ OK |
| `Runtime/Core/Ports.cs` | +39 -6 | Runtime/Core/ OK |
| `Tests/EditMode/Core/ContractTypeTests.cs` | +196 -0 | Tests/EditMode/Core/ OK |
| `Tests/EditMode/Core/DialogueGeneratorContract.cs` | +18 -0 | Tests/EditMode/Core/ OK |
| `Tests/EditMode/Core/IntentClassifierContract.cs` | +48 -0 | Tests/EditMode/Core/ OK |
| `Tests/EditMode/Core/NpcAi.Core.Tests.asmdef` | +1 -0 | Tests/EditMode/Core/ OK |
| `Tests/EditMode/Core/ReceptivityEngineContract.cs` | +23 -0 | Tests/EditMode/Core/ OK |
| `Tests/EditMode/Core/ScenarioObjectiveContract.cs` | +51 -0 | Tests/EditMode/Core/ OK |
| `Tests/EditMode/Core/SpeechToTextContract.cs` | +67 -0 | Tests/EditMode/Core/ OK |
| `Tests/EditMode/Core/ContractVersionChangelogTests.cs` (new, 48 L) | new | Tests/EditMode/Core/ OK |
| `Tests/EditMode/Core/CoreAssemblyPurityTests.cs` (new, 47 L) | new | Tests/EditMode/Core/ OK |
| `Tests/EditMode/Core/EventChannelTests.cs` (new, 144 L) | new | Tests/EditMode/Core/ OK |

- 10 modified + 3 new tracked-content files (+ 3 `.meta`). Totals: **506 insertions / 8 deletions**
  tracked + ~239 lines in the 3 new files, roughly **745 authored lines**.
- Every path is under one of the 4 authorized routes: `Runtime/Core/`, `Runtime/CoreChannels/`,
  `Tests/EditMode/Core/`, `Docs/CONTRACT-CHANGELOG.md`.
- `Runtime/CoreChannels/` - **untouched**; only characterized by `EventChannelTests` (G3 GREEN with
  zero runtime change).
- Zero `Runtime/*/Fakes/`; zero other module.
- **Out of scope, excluded from this boundary audit** (a separate governance change): `CLAUDE.md`
  (+1 -1), `README.md` (+25 -3), untracked `openspec/config.yaml`. These carry the module-owner
  table and the self-merge / pre-merge-review rules - module-owner governance, not the M0 contract.
  Untracked `.atl/ .claude/ .copilot/ .mcp.json AppData/ openspec/` pre-date or are orthogonal.

**Result: PASS.**

### Runtime-Minimality Audit

- **`Contract.Version` unchanged - PASS.** `Runtime/Core/Contract.cs` is not in the diff; still
  `public const int Version = 1;`. No `const` -> `static readonly`.
- **`Dtos.cs` diff equals exactly the two null-coalescing guards - PASS.**
  `ReasonCode = reasonCode ?? string.Empty;` (`ReceptivityChange` ctor) and
  `Text = text ?? string.Empty;` (`NpcReply` ctor). `EmotionTag` / `AnimationCue` were already
  guarded before this change. No other hunk.
- **`Ports.cs` diff is XML-doc only - PASS.** All five hunks sit inside `/// <summary>` blocks
  (`IIntentClassifier`, `IReceptivityEngine`, `IDialogueGenerator`, `IScenarioObjective`). No
  signature, member, or behavior change; interface member sets identical to `a7a0590`.
- **No new `abstract` members on the `*Contract.cs` bases - PASS.** Additions are non-abstract
  `[Test]` methods plus one `private sealed class ClasificadorNoListo : IIntentClassifier` local to
  `IntentClassifierContract.cs`.
- **`NpcAi.Core.Tests.asmdef` - PASS.** One line added: `"NpcAi.Core.Channels"` in `references`
  (G3 / design D6).

### Remediation Closure - prior-FAIL findings

| # | Prior finding | Severity | Fix verified | Status |
|---|---|---|---|---|
| 1 | `DTO Utterance` had **zero** test coverage (2 MUST scenarios) | CRITICAL | `ContractTypeTests.Utterance_IsEmpty_refleja_el_texto` (null/`""`/`"   "` -> `IsEmpty` true; real text -> false) and `Utterance_guarda_los_campos_sin_recortar_ni_validar_rangos` (`new Utterance("hola",5f,-2f)` -> fields verbatim, ranges not clamped). Matches the `Utterance` struct. No runtime change. | **CLOSED** - both scenarios COMPLIANT |
| 2 | G12 `Politica de igualdad de los DTO` scenarios **unimplemented** | WARNING 1 | `Solo_PersonalityId_implementa_IEquatable_en_la_v1` (reflection: the 4 DTO do not implement `IEquatable<T>`; `PersonalityId` does - matches source) + `Los_DTO_usan_igualdad_estructural_por_defecto` (`AreEqual`/`AreNotEqual` on default `NpcReply` struct equality) + helper `ImplementaIEquatable`. | **CLOSED** - both G12 scenarios COMPLIANT |
| 3 | PersonalityId "None e IsNone" only partial | WARNING 4 | `PersonalityId_nulo_o_solo_espacios_es_None` (null/`""`/`"   "` and `.None` all `IsNone`; false-case via the pre-existing test). | **CLOSED** for "None e IsNone" |
| 4 | D6 `OnDisable` synchrony unresolved | WARNING 2 | Empirically confirmed in the human EditMode run; recorded in the `sdd-attempt` diagnosis. | **CLOSED** |
| 5 | `apply-progress` not retrievable in the executor | WARNING 3 | `mem_*` still unavailable and no `apply-progress.md` on disk, but the native `sdd-attempt status` ledger (3 attempts, per-attempt diagnosis + cleanup/process evidence) was used as the apply record. | **MITIGATED** - see WARNING 4 |
| 6 | Extra: human gate first failed `El_enum_Receptivity_esta_congelado_en_la_v1` - **test defect** (`AssertEnumCongelado` compared positionally vs `Enum.GetNames`, which orders by unsigned magnitude, so `NoReceptivo = -1` sorts last) | - | Helper rewritten: `Zip(GetNames, GetValues)` (same order -> correct name<->value pairing), then `OrderBy(nombre, StringComparer.Ordinal)` on both sides, compare set + cardinality. Explicit `AreEqual(-1,(int)NoReceptivo)` / `AreEqual(1,(int)Receptivo)` retained. `Receptivity` enum unchanged and correct. Re-run all green. | **CLOSED** - helper is order-independent and sound |

### TDD Compliance

| Check | Result | Details |
|-------|--------|---------|
| TDD Evidence reported | OK | `sdd-attempt` ledger + tasks.md "Notas TDD" + per-file diff inspection |
| All requirements have tests | OK | 18/18 requirements have at least one covering test |
| RED confirmed (tests exist) | OK | All 13 changed/new test files present and read |
| GREEN confirmed (tests pass) | OK | Human EditMode `Run All` 28/28 in `NpcAi.Core.Tests` + module DLLs |
| Genuine RED->GREEN | OK | G5 / G6 only. `new NpcReply(null,null,null)` -> `AreEqual("", Text)` and `new ReceptivityChange(Neutral,Neutral,0,null)` -> `AreEqual("", ReasonCode)` both FAIL on pre-change `Dtos.cs`, pass after the two guards. tasks.md 3.2 confirms they were authored before the edit. |
| Characterization tests (green on first run) | OK expected | Everything else, incl. the Utterance + G12 remediation (`Utterance` byte-identical to `a7a0590`). Consistent with tasks.md "Notas TDD". |
| Rule 0.4 - no module double reddened by a new inherited `[Test]` | OK | Confirmed empirically (module test DLLs GREEN) and by static read of all 7 doubles (none edited): `Current` field-inits to `Neutral` + idempotent `Reset`; G8 asserts only non-empty `Text`; `Classify` never throws; `Emit` respects `IsListening`, multicast null-safe; both `ScriptedScenarioObjective` clamp steps to `[0,4]`, `IsComplete <=> Progress01 == 1`, `Notify(default)` no-op, reversible. |

**TDD Compliance: 7/7.**

### Test Layer Distribution

| Layer | Tests | Files | Tools |
|-------|-------|-------|-------|
| Unit / contract (EditMode, NUnit) | 28 runnable in `NpcAi.Core.Tests` (+ inherited `*Contract.cs` `[Test]`s in the 7 module test DLLs) | 11 `.cs` in `Tests/EditMode/Core/` (4 concrete + 7 abstract bases) | Unity Test Framework / NUnit |
| Integration | 0 | - | none in stack |
| E2E | 0 | - | M11 `Samples~/Harness` is manual wiring |

### Changed File Coverage

Coverage analysis skipped - no coverage tool in this stack (Unity EditMode, no CI). Not a failure.

### Assertion Quality

Audited all 13 changed/new test files for banned patterns (tautologies, orphan empty checks,
type-only assertions, ghost loops, smoke-only, implementation-detail coupling, mock-heavy).

| File | Observation | Severity |
|------|-------------|----------|
| `ContractTypeTests.cs` (Utterance, G12, PersonalityId, enum-freeze) | Real construction + value/reflection assertions with true/false variance and positive/negative companions. | none |
| `CoreAssemblyPurityTests.cs` | `Assert.IsEmpty(forbiddenRefs)` - correct shape for a purity invariant, not an orphan empty check. | none |
| `ScenarioObjectiveContract.cs` `IsComplete...si_y_solo_si...` | Loop runs a fixed 50 iterations (not over a possibly-empty collection); assertion executes every iteration. | none |
| `ScenarioObjectiveContract.cs` `La_completitud_es_reversible` | Guards the "never completed" case with `Assume.That` (inconclusive, not false-pass), then asserts `Progress01 < 1` and `IsComplete == false`. | none |
| `DialogueGeneratorContract.cs` `Generate_no_esta_obligado_a_ser_determinista` | Deliberately asserts only `IsFalse(IsEmpty)` on two calls - correct for a non-normative "PUEDEN diferir" scenario; asserting equality would redden the deterministic M6 double (rule 0.4). | SUGGESTION only |

No tautology, no assertion-without-production-code, no ghost loop, no mock-heavy test.
**Assertion quality: 0 CRITICAL, 0 WARNING.**

### Quality Metrics

**Linter**: not available (no C#/Unity linter configured).
**Type Checker**: not run in the agent (the Unity/Roslyn compile is the human EditMode gate -
attested GREEN, which proves the compile succeeded).

### Spec Compliance Matrix - contrato-nucleo-m0 (16 requirements / 51 scenarios)

| # | Requirement | Scenario | Test | Result |
|---|---|---|---|---|
| 1 | Version del contrato | La version publicada es 1 | ContractTypeTests.Version_del_contrato_es_uno + ContractVersionChangelogTests | COMPLIANT |
| 1 | | La version coincide con el changelog | ContractVersionChangelogTests: regex on the sole "## v1" header -> AreEqual(1,1) | COMPLIANT |
| 1 | | Sin changelog la prueba se ignora | same test, Assert.Ignore branch (structural; file exists) | COMPLIANT (structural) |
| 2 | Pureza del ensamblado NpcAi.Core | Sin dependencias de Unity ni de otros modulos | CoreAssemblyPurityTests (2 reflection tests) | COMPLIANT |
| 3 | Enums congelados en v1 | Cada enum expone exactamente los miembros fijados | ContractTypeTests 4 freeze tests via order-independent AssertEnumCongelado; tables match Enums.cs + changelog | COMPLIANT |
| 3 | | El valor cero es el seguro | Los_enums_tienen_el_valor_seguro_en_cero | COMPLIANT |
| 3 | | Receptivity esta ordenada -1/0/1 | La_receptividad_esta_ordenada + El_enum_Receptivity_esta_congelado_en_la_v1 (-1/1 explicit) | COMPLIANT |
| 4 | DTO Utterance | IsEmpty refleja el texto | ContractTypeTests.Utterance_IsEmpty_refleja_el_texto (new - remediation) | COMPLIANT |
| 4 | | El struct no recorta rangos | ContractTypeTests.Utterance_guarda_los_campos_sin_recortar_ni_validar_rangos (new - remediation) | COMPLIANT |
| 5 | DTO IntentResult | Unknown por defecto es seguro | IntentResult_Unknown_es_seguro + new LatencyMs == 0f assertion | COMPLIANT |
| 5 | | Unknown preserva la latencia | IntentResult_Unknown_preserva_la_latencia_y_mantiene_lo_seguro (new) | COMPLIANT |
| 6 | DTO ReceptivityChange | La direccion se calcula a partir de From y To | ReceptivityChange_reporta_direccion | COMPLIANT |
| 6 | | El constructor normaliza ReasonCode nulo | ReceptivityChange_normaliza_ReasonCode_nulo_a_cadena_vacia (new, genuine RED->GREEN) | COMPLIANT |
| 7 | DTO NpcReply | El constructor normaliza los tres string nulos | NpcReply_normaliza_los_tres_string_nulos_a_cadena_vacia (new, genuine RED->GREEN) | COMPLIANT |
| 7 | | IsEmpty refleja el texto util | true-branch via the all-nulls test; false-branch via DialogueGeneratorContract.Nunca_devuelve_texto_vacio (IsFalse(reply.IsEmpty) on non-empty Text) | COMPLIANT (false-branch via the port contract) |
| 8 | Identificador PersonalityId | Normaliza y compara por valor | PersonalityId_normaliza_y_compara_por_valor | COMPLIANT |
| 8 | | None e IsNone | PersonalityId_nulo_o_solo_espacios_es_None (new - remediation) + IsFalse(a.IsNone) in the pre-existing test | COMPLIANT |
| 8 | | Igualdad completa (Equals, ==, !=, GetHashCode) | Equals via Assert.AreEqual(a,b) - passing/GREEN; ==, !=, GetHashCode not separately asserted | COMPLIANT (PARTIAL-depth; pre-existing, outside G1-G15 and this change's file scope) |
| 9 | Politica de igualdad de los DTO | Los cuatro DTO no declaran igualdad propia | Solo_PersonalityId_implementa_IEquatable_en_la_v1 (new - remediation) | COMPLIANT |
| 9 | | Igualdad estructural por defecto | Los_DTO_usan_igualdad_estructural_por_defecto (new - remediation) | COMPLIANT |
| 10 | Puerto ISpeechToText | Estado inicial y transiciones | Arranca_sin_escuchar + Start_y_Stop_cambian_el_estado | COMPLIANT |
| 10 | | No emite fuera de la ventana de escucha | No_emite_nada_despues_de_StopListening + No_emite_nada_antes_del_primer_StartListening (new) | COMPLIANT |
| 10 | | Reemite lo que escucha | Emite_lo_que_escucha_mientras_esta_escuchando | COMPLIANT |
| 10 | | Stop idempotente y reanudacion | Parar_dos_veces_no_lanza + La_secuencia_Start_Stop_Start_reanuda_la_emision (new) | COMPLIANT |
| 10 | | Fan-out y ausencia de suscriptores | Entrega_cada_Utterance_a_todos_los_suscriptores + Emitir_sin_suscriptores_no_lanza (new) | COMPLIANT |
| 11 | Puerto IPhysicalActionSource | Entrega a los suscriptores vigentes | PhysicalActionSourceContract.Entrega_la_accion_a_quien_esta_suscrito | COMPLIANT |
| 11 | | Un handler desuscrito deja de recibir | No_entrega_nada_despues_de_desuscribirse | COMPLIANT |
| 11 | | Ninguna no es un evento | Ninguna_no_es_un_evento | COMPLIANT |
| 11 | | Emitir sin suscriptores no lanza | Emitir_sin_suscriptores_no_lanza | COMPLIANT |
| 12 | Puerto IIntentClassifier | Texto vacio o nulo devuelve Desconocida | Texto_vacio_devuelve_Desconocida_y_no_lanza (TestCase null/empty/spaces) | COMPLIANT |
| 12 | | Determinismo parcial | Es_determinista_para_la_misma_entrada | COMPLIANT |
| 12 | | Clasificador no listo | Un_clasificador_no_listo_devuelve_Unknown + Classify_no_lanza_en_ningun_estado (new; local ClasificadorNoListo stub) | COMPLIANT |
| 12 | | Entradas raras no rompen los rangos | Nunca_lanza_con_entradas_raras + La_confianza_siempre_esta_entre_cero_y_uno + La_latencia_nunca_es_negativa | COMPLIANT |
| 13 | Puerto IReceptivityEngine | Neutral hasta Reset, y Reset determinista | Current_es_Neutral_antes_del_primer_Reset + Reset_repetido_con_la_misma_personalidad_es_idempotente (new) + Reset_es_determinista_para_la_misma_personalidad | COMPLIANT |
| 13 | | Evaluate reporta el estado y una razon | La_transicion_reporta_el_estado_previo_y_el_nuevo + Toda_transicion_trae_una_razon | COMPLIANT |
| 13 | | Monotonia bajo presion sostenida | La_agresion_sostenida_nunca_mejora + La_empatia_sostenida_nunca_empeora | COMPLIANT |
| 13 | | Entrada neutra no mueve el estado | Una_intencion_desconocida_no_mueve_el_estado | COMPLIANT |
| 13 | | Reset restaura el estado inicial | Reset_vuelve_al_mismo_estado_despues_de_maltratar_al_NPC | COMPLIANT |
| 14 | Puerto IDialogueGenerator | Nunca devuelve texto vacio | Nunca_devuelve_texto_vacio | COMPLIANT |
| 14 | | Etiquetas nunca nulas | Las_etiquetas_nunca_son_nulas | COMPLIANT |
| 14 | | Funciona sin personalidad | Funciona_sin_personalidad_asignada | COMPLIANT |
| 14 | | Receptivo y NoReceptivo difieren | Un_NPC_no_receptivo_no_responde_igual_que_uno_receptivo | COMPLIANT |
| 14 | | No se exige determinismo | Generate_no_esta_obligado_a_ser_determinista (new; characterization) | COMPLIANT |
| 15 | Puerto INpcPresenter | Reproduce una respuesta normal | NpcPresenterContract.Reproduce_una_respuesta_normal_sin_lanzar | COMPLIANT |
| 15 | | Sobrevive a una respuesta vacia | Sobrevive_a_una_respuesta_vacia | COMPLIANT |
| 15 | | Sobrevive a reproducciones encadenadas | Sobrevive_a_reproducciones_encadenadas | COMPLIANT |
| 16 | Puerto IScenarioObjective | Estado inicial | Arranca_sin_progreso_y_sin_completar | COMPLIANT |
| 16 | | Progreso siempre acotado | El_progreso_siempre_esta_entre_cero_y_uno + El_progreso_no_baja_de_cero_por_maltrato | COMPLIANT |
| 16 | | IsComplete si y solo si el progreso es total | IsComplete_es_verdadero_si_y_solo_si_el_progreso_es_total (new) + Completar_implica_progreso_total | COMPLIANT |
| 16 | | La completitud es reversible | La_completitud_es_reversible (new) | COMPLIANT |
| 16 | | Notify con el valor por defecto es no-op | Notify_con_el_valor_por_defecto_es_no_op (new) | COMPLIANT |

### Spec Compliance Matrix - canales-evento-nucleo-m0 (2 requirements / 7 scenarios)

| # | Requirement | Scenario | Test | Result |
|---|---|---|---|---|
| 17 | Semantica de EventChannel | Raise sin suscriptores no lanza | EventChannelTests.Raise_sin_suscriptores_no_lanza | COMPLIANT |
| 17 | | Raise entrega el payload a cada suscriptor | Raise_entrega_el_mismo_payload_a_cada_suscriptor | COMPLIANT |
| 17 | | Unsubscribe corta la entrega | Unsubscribe_corta_la_entrega_a_ese_handler | COMPLIANT |
| 17 | | Subscribe y Unsubscribe con null no lanzan | Subscribe_y_Unsubscribe_con_null_no_lanzan_ni_alteran_la_lista | COMPLIANT |
| 17 | | OnDisable limpia los suscriptores | OnDisable_limpia_los_suscriptores_y_un_Raise_posterior_no_invoca_al_handler_viejo - D6 empirically confirmed | COMPLIANT |
| 18 | Canales concretos del nucleo | Los 5 canales existen con su tipo de payload | Los_cinco_canales_concretos_son_sealed_y_heredan_EventChannel_del_tipo_esperado (IsSealed + BaseType); matches Channels.cs | COMPLIANT |
| 18 | | Un canal concreto se comporta como la base | Un_canal_concreto_se_comporta_como_la_base (IntentResultChannel Subscribe->Raise->Unsubscribe->Raise) | COMPLIANT |

**Compliance summary: 58/58 scenarios have a passing covering test; 18/18 requirements covered; 0 UNTESTED/GAP, 0 DOC-ONLY.** One scenario is PARTIAL-depth - PersonalityId "Igualdad completa": `Equals` is exercised via `Assert.AreEqual(a,b)` and is GREEN, but `==` / `!=` / `GetHashCode` are not separately asserted. It is counted as covered (a passing covering assertion exists), is pre-existing, and sits outside the G1-G15 gap list and this change's File Changes scope; it did not regress. See WARNING 1.

### G1-G15 traceability (Success Criterion)

| Gap | Closed by | State |
|---|---|---|
| G1 | ContractVersionChangelogTests + single "## v1" changelog header | closed |
| G2 | CoreAssemblyPurityTests (2 reflection tests) | closed |
| G3 | EventChannelTests (7) + NpcAi.Core.Tests.asmdef reference; Runtime/CoreChannels/ unchanged | closed |
| G4 | ContractTypeTests 4 enum-freeze tests via order-independent AssertEnumCongelado | closed |
| G5 | NpcReply.Text ?? string.Empty guard + genuine RED->GREEN test | closed |
| G6 | ReceptivityChange.ReasonCode ?? string.Empty guard + genuine RED->GREEN test | closed |
| G7 | ReceptivityEngineContract +2 [Test] + Ports.cs XML-doc | closed |
| G8 | DialogueGeneratorContract +1 characterization [Test] + Ports.cs XML-doc | closed |
| G9 | IntentClassifierContract +2 [Test] + local ClasificadorNoListo stub + Ports.cs XML-doc | closed |
| G10 | SpeechToTextContract +5 [Test] | closed |
| G11 | ScenarioObjectiveContract +3 [Test] + Ports.cs XML-doc | closed |
| G12 | changelog "Politica de igualdad de los DTO" (deferred to v2) + 2 reflection/structural tests locking the v1 state (remediation) | closed (impl still deferred; v1-state assertions present) |
| G13 | ContractTypeTests latency-preservation + LatencyMs == 0 on default | closed |
| G14 | changelog - float clamp is producer contract, deferred to v2 | deferred & recorded |
| G15 | Docs/CONTRACT-CHANGELOG.md v1 rewrite - per-port invariants, Score/ReasonCode as untyped diagnostic, determinism asymmetry, sentinels, DTO equality policy, const note, enforcement pointer | closed |

G1-G15: 13 closed, G14 explicitly deferred+recorded, G12 now has both the v2 deferral note and the v1-state assertions. Success Criterion met.

### Correctness (Static Evidence)

| Area | Status | Notes |
|---|---|---|
| Contract frozen at v1 | OK | Contract.cs untouched; const int Version = 1 |
| Two null guards, non-breaking | OK | No correct consumer could depend on receiving null; IsEmpty already treated null as empty via IsNullOrWhiteSpace |
| XML-doc invariants on 4 ports | OK | DEBERIA for the not-ready path, DEBE for no-throw - matches spec MUST/SHOULD wording |
| Enum tables (test + changelog) match Enums.cs | OK | Intent 7, Tone 5, PhysicalAction 8, Receptivity 3 (-1, 0, 1) - exact |
| Changelog v1 rewrite completeness (G15) | OK | All sub-sections present; new sections are h3, so the ContractVersionChangelogTests regex still captures only "## v1" |
| EventChannel<T> semantics unchanged | OK | Null guards, guarded _listeners?.Invoke, OnDisable => _listeners = null all pre-existing; 5 concrete channels sealed : EventChannel<T> |
| Utterance struct vs new tests | OK | Fields stored raw by ctor; IsEmpty => string.IsNullOrWhiteSpace(Text) - new tests match exactly |
| PersonalityId : IEquatable<PersonalityId> | OK | Confirmed in source; underpins Solo_PersonalityId_implementa_IEquatable_en_la_v1 |

### Coherence (Design)

| Decision | Followed? | Notes |
|---|---|---|
| D2 non-abstract [Test] only | Yes | Verified against every base diff - zero new abstract members |
| D3 local not-ready stub inside Tests/EditMode/Core/ | Yes | private sealed class ClasificadorNoListo in IntentClassifierContract.cs |
| D4 version<->changelog via fixed path + Assert.Ignore | Yes | Implemented as specified |
| D5 assembly-purity reflection | Yes | 2 tests; optional .asmdef parse check not added (SUGGESTION) |
| D6 channel tests via CreateInstance + DestroyImmediate for OnDisable | Yes | Synchrony confirmed empirically this pass |
| D7 two ?? string.Empty guards, version stays 1 | Yes | Exactly the Dtos.cs diff |
| D8 IsComplete pure function of Progress01, reversible, Notify(default) no-op | Yes | Tests assert the bi-conditional and reversibility, not stickiness |
| D9 one freeze test per enum, explicit -1/1 for Receptivity | Yes | Plus the helper fix so ordering is not asserted against source position |
| D10 qualify Core.Receptivity in new/modified test files | Yes | Present in ContractTypeTests.cs, EventChannelTests.cs, ReceptivityEngineContract.cs |
| D11/D13 single size:exception PR, Version stays const | Yes | No static readonly change |
| D14 human EditMode gate is the acceptance authority | Yes | Attested GREEN this pass |

### Issues Found

**CRITICAL**: None. (Prior CRITICAL - DTO Utterance zero coverage - is CLOSED.)

**WARNING**
1. PersonalityId "Igualdad completa" scenario is PARTIAL. Equals is exercised via Assert.AreEqual(a,b); operator ==, operator !=, and GetHashCode have no covering assertion. Pre-existing, not introduced by this change, outside the declared G1-G15 / File Changes scope; does not block archive. Cheap additive close: 3 asserts in the existing PersonalityId_normaliza_y_compara_por_valor, inside the authorized Tests/EditMode/Core/ route.
2. tasks.md checkboxes are stale for the completed EditMode gates. 1.4 / 2.4 / 3.7 / 4.8 / 5.2 are still [ ] but the human EditMode Run All (their exit criterion) is done and GREEN. Orchestrator/human should tick them so the artifact matches reality before archive.
3. Governance gates 0.6 / 5.3 remain open and are a pre-merge requirement. Under the updated rule (CLAUDE.md rule 2 + README "Antes de mergear"), a contract change needs review by the other shared M0 owner or the advisor before the author merges - no Monday window, no all-owner quorum. Not an SDD-archive blocker, but must happen before the change lands on main.
4. apply-progress is still not retrievable as an Engram/OpenSpec artifact in this executor (mem_* unavailable, no apply-progress.md on disk). The TDD-evidence cross-check used the native gentle-ai sdd-attempt ledger + working-tree inspection instead. Findings stand on source + the human attestation; the apply narrative was not audited line-for-line.

**SUGGESTION**
1. Add the optional D5 .asmdef parse check (noEngineReferences == true, references.length == 0) to catch a purity regression before the compiler materializes it.
2. Generate_no_esta_obligado_a_ser_determinista is a deliberately weak characterization test (asserts only non-empty). Correct for a non-normative "PUEDEN diferir" scenario; no action, recorded for the reviewer.
3. Authored diff is roughly 745 lines (506 tracked + ~239 new), over the 400-line budget - already accepted as size:exception by the user (2026-08-31). No action.
4. A one-line new NpcReply("x","","") -> Assert.IsFalse(IsEmpty) in ContractTypeTests would make Req 7 scenario 2 self-evident rather than covered via the port contract.

### Verdict

**PASS WITH WARNINGS - archive-ready.**

The prior CRITICAL (DTO Utterance) and all four prior WARNINGs are closed or mitigated: Utterance now has both MUST scenarios covered, G12's v1-state has two locking tests, PersonalityId "None e IsNone" is covered, D6 is empirically confirmed, and the AssertEnumCongelado test defect is fixed with an order-independent helper. The structural audit is clean: write boundary inside the 4 authorized routes, Contract.Version frozen at 1 (const), Dtos.cs is exactly the two documented null guards, Ports.cs is XML-doc only, no new abstract members, G5/G6 are genuine RED->GREEN, no module double is reddened, the G1 regex is robust, and G1-G15 are closed or explicitly deferred. Runtime GREEN is attested by the human EditMode Run All (28/28 in NpcAi.Core.Tests + module DLLs), and the native sdd-attempt objective is settled passed.

Residual warnings are non-blocking: one pre-existing PARTIAL-depth equality scenario outside this change's scope, stale tasks.md checkboxes for already-done human gates, and the governance co-review that is a pre-merge (not pre-archive) gate.

### Pending (not an agent action)

1. Orchestrator/human: tick tasks.md 1.4 / 2.4 / 3.7 / 4.8 / 5.2 to record the completed EditMode green run.
2. Before merging to main: governance co-review by the other shared M0 owner or the advisor (tasks 0.6 / 5.3; CLAUDE.md rule 2 as updated).
3. Optional: close WARNING 1 (PersonalityId ==/!=/GetHashCode asserts) and SUGGESTION 1 (D5 .asmdef check) in a follow-up - both inside Tests/EditMode/Core/, no runtime change.

### Next steps for the pipeline

sdd-archive - the change is archive-eligible. The remaining items are checkbox reconciliation and a pre-merge governance gate, neither of which blocks archiving the spec/design/tasks artifacts.
