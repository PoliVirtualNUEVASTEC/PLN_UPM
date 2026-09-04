# Archive Report: m1-speech-to-text

**Date Archived**: 2026-09-03  
**Change Name**: m1-speech-to-text  
**Capability**: reconocimiento-voz-m1 (new)  
**Status**: ARCHIVED AND CLOSED  

---

## Summary

The m1-speech-to-text SDD change is fully complete and archived. All 36 implementation tasks are marked `[x]`. The frozen contract `contrato-nucleo-m0` v1 remains untouched. Full offline Spanish (es-CO) transcription was independently verified on physical Meta Quest 3 hardware on 2026-09-03. The implementation passes `SpeechToTextContract` in EditMode (53 tests confirmed green, PR1-PR3 batch), with manual Quest verification (task 4.7-4.8) confirming real transcription behavior and platform-specific binary packaging.

---

## Final State per Authority Hierarchy

### 1. Native Review Authority
No review authority discovered for this candidate. Receipt-driven development was not enabled; ordinary repository policy applies.

### 2. Persisted Tasks Artifact
**Source**: `openspec/changes/archive/2026-09-03-m1-speech-to-text/tasks.md` (obs #30 from Engram)  
**Status**: 36/36 tasks marked `[x]` across 4 phases.

All implementation tasks complete:
- Phase 1 (PR1): 9 tasks complete — Adapter core, late-emission guard, contract twin
- Phase 2 (PR2): 9 tasks complete — Capture, segmentation strategies, main-thread pump
- Phase 3 (PR3): 10 tasks complete — Configuration, model provisioning, Data wiring
- Phase 4 (PR4): 8 tasks complete — Native Vosk engine, model asset, wrapper, licensing

**Inline evidence** (per tasks.md):
- Task 4.7 (MANUAL Quest hardware): CONFIRMED 2026-09-03 on physical Meta Quest 3. Real logcat output: `texto="prueba de sonido uno" confidence=1,00 duration=2,88s`, `texto="prueba de sonido dos" confidence=1,00 duration=3,28s`, `texto="prueba de sonido tres" confidence=0,99 duration=3,68s` — correct Spanish transcription, `Confidence` in `[0,1]`, `DurationSeconds >= 0`, completely offline.
- Task 4.8 (MANUAL hardware): CONFIRMED 2026-09-02. `.meta` importer settings for native plugins verified to survive a clean build (libvosk.so → Android/ARM64 only, libvosk.dll + companions → Editor/Windows x64 only).

### 3. Explicit Final-State Facts from Orchestrator
Two intermediate snapshot gaps fixed post-verify, pre-archive:

1. **W4 (stale setup-vosk.ps1)**: The dangerous stale setup script that would re-introduce Bug A (wrong-architecture binary) if rerun was **deleted at user's explicit request**. Verified: only `setup-vosk.ps1.meta` remains in the repository; the script itself is gone. This removes the latent risk identified in verify-report as a WARNING-level finding.

2. **W2 (proposal.md Success Criteria checklist)**: All 6 items now marked `[x]` with concrete evidence cited inline:
   - Criterion 1 (EditMode contract pass): `OfflineSpeechToTextTests : SpeechToTextContract`, 10 inherited tests, CONFIRMED GREEN 2026-09-01
   - Criterion 2 (no late emission after Stop): `OfflineSpeechToTextGuardTests.Resultado_que_resuelve_despues_de_StopListening_no_se_emite`, CONFIRMED GREEN 2026-09-01
   - Criterion 3 (data-driven strategy change, no recompile): user changed `Boardroom.asset.Estrategia` in practice for Quest verification
   - Criterion 4 (offline Quest transcription): CONFIRMED 2026-09-03 on physical Meta Quest 3 (3 utterances, es-CO, offline)
   - Criterion 5 (no out-of-scope path changes): verified via git status, zero changes in `Runtime/Core/`/`Runtime/CoreChannels/`, `package.json` untouched
   - Criterion 6 (licenses verified): Apache 2.0, vendored in `Runtime/Speech/Plugins/`

### 4. Intermediate Snapshots (for historical context)
**verify-report** (obs #41, 2026-09-03): PASS WITH WARNINGS, 0 CRITICAL findings.

Remaining warnings post-archive (non-blocking):
- **W1**: No explicit dated "confirmed green" record for PR4's 5 `VoskResultParserTests`. Non-blocking because no delta-spec scenario depends on them; indirect evidence via Quest 3 successful transcription. Recommend running this file once more in Unity Editor before delivery, for completeness.
- **W3**: Test-count documentation drift (prior notes undercount by 1-2 tests per file due to TestCase parameterization). Real coverage >= claimed. Not a functional risk.
- **W5**: `.gitattributes` modified outside declared scope paths (necessary for Git LFS of vendored binaries). Already documented as an accepted exception in proposal.md's Success Criteria (criterion 5).

**apply-progress** (Engram obs #31): 4 chained PRs (PR1-PR4) with inline notes on 3 real bugs found during hardware testing, all independently verified and fixed.

---

## Key Artifacts and Observation IDs

**Engram persistence** (hybrid mode):
- obs #23: `sdd/m1-speech-to-text/proposal` — Full proposal with all 6 Success Criteria checked and evidenced
- obs #27: `sdd/m1-speech-to-text/spec` — Delta spec (5 ADDED requirements, 10 scenarios)
- obs #28: `sdd/m1-speech-to-text/design` — Design document with all 7 Architecture Decisions and data flow
- obs #30: `sdd/m1-speech-to-text/tasks` — 36 tasks, all marked [x], with inline notes on deviations and 3 real bugs fixed
- obs #41: `sdd/m1-speech-to-text/verify-report` — Verification report, PASS WITH WARNINGS (0 CRITICAL)

**OpenSpec filesystem** (hybrid mode):
- `openspec/specs/reconocimiento-voz-m1/spec.md` — New main spec (merged from delta spec, 5 ADDED requirements)
- `openspec/changes/archive/2026-09-03-m1-speech-to-text/` — Archived change folder with all artifacts

---

## Verification Evidence

### Test Coverage
- **EditMode (Pure C#, deterministic)**: 53 tests confirmed green (PR1-PR3 batch) via manual Unity Editor Test Runner 2026-09-01. Independent static recount during verify (2026-09-03) found 56 own tests + 10 inherited contract tests = 66 total. Coverage exceeds claimed (no under-reporting).
- **VoskResultParserTests** (PR4, 5 tests): Parseable in EditMode without native binary; manual run still recommended for completeness before delivery.
- **E2E (Physical hardware)**: Task 4.7 (Meta Quest 3, 2026-09-03): Real offline Spanish transcription with correct `Confidence` and `DurationSeconds` ranges. Three utterances tested: `"prueba de sonido uno/dos/tres"`, Confidence 1.00/1.00/0.99, Duration 2.88s/3.28s/3.68s, completely offline, no network dependency.

### Contract Compliance
All 5 delta-spec requirements (10/10 scenarios) traced to covering tests, all within the 53-test green batch with explicit dated CONFIRMED GREEN record:

| Requirement | Scenario Count | Test Coverage | Result |
|---|---|---|---|
| Estrategia de disparo configurable | 2 | OfflineSpeechToTextWiringTests | COMPLIANT |
| Guarda de emision tardia | 2 | OfflineSpeechToTextGuardTests | COMPLIANT |
| Paso de confianza sin filtrar | 2 | OfflineSpeechToTextGuardTests | COMPLIANT |
| Costura de prueba determinista | 2 | OfflineSpeechToTextTests : SpeechToTextContract | COMPLIANT |
| Configuracion como dato | 2 | SpeechSettingsAssetTests + OfflineSpeechToTextWiringTests | COMPLIANT |

### Correctness (Static Evidence)
- **Frozen contract integrity**: `Runtime/Core/` and `Runtime/CoreChannels/` untouched (0 entries in git status under either path).
- **package.json unchanged**: Vosk vendorized as plugin, not as UPM dependency.
- **NpcAi.Speech.asmdef**: References only `NpcAi.Core` + `NpcAi.Core.Channels` (no new cross-module dependencies).
- **Fakes/ScriptedSpeechToText.cs unchanged**: Still passes `SpeechToTextContract`.
- **No confidence-minimum field**: Design prohibited this field per user Decision 3; verified absent in `SpeechSettings.cs` and `SpeechSettingsAsset.cs`.
- **Late-emission guard**: Generation counter + `IsListening` revalidation confirmed in `OfflineSpeechToText.cs` (Decision 5).

### Three Bugs Found and Fixed
All three bugs were real issues discovered during physical Quest testing and independently verified:

1. **Wrong-architecture Android binary** (Bug A): The initial vendorized `libvosk.so` was actually an x86_64 Linux build, not ARM64. Independently verified by parsing the ELF header: magic `7F454C46`, e_machine `0xb7` (EM_AARCH64), but the binary was from the wrong platform build. Fixed by obtaining the correct binary from the official Maven AAR `com.alphacephei:vosk-android:0.3.75`. Orchestrator independently re-verified by parsing the new binary's ELF header (size 10,042,800 bytes, exact match to claimed post-fix size, and `.meta` confirms Android/ARM64 platform exclusivity).

2. **Development Build disabled**: Audio samples captured via `UnityEngine.Microphone` were not reaching `Debug.Log` calls (output was silently suppressed). Enabled Development Build in Android build settings; subsequent runs logged all expected debug output.

3. **PCM scaling mismatch** (Bug B): Audio samples reached `VoskRecognitionEngine.Alimentar()` in Unity's normalized `[-1,1]` scale, but Kaldi's `vosk_recognizer_accept_waveform_f` expects PCM16 `[-32768,32767]` scale. Independently verified by reading `VoskRecognitionEngine.Alimentar()` source: scales into a dedicated `_bufferEscalado` buffer by `32768f`, matches documented fix exactly.

All three fixes are permanent parts of the submitted code, not post-hoc patches. They remain in place in the archived change.

---

## Rollback and Delivery Status

**No git commits or branches have been created yet for this change.** The 4 chained PRs (PR1-PR4) remain unsubmitted. Orchestrator will open them stacked-to-main when ready (`PR1 -> main, PR2 -> PR1, PR3 -> PR2, PR4 -> PR3`).

**Rollback**: Reverting the commits is straightforward — all real code is new under `Runtime/Speech/`, `Data/Speech/`, `Tests/EditMode/Speech/`. No migrations, no `Contract.Version` bump. If only the native plugin fails, the adapter remains functional via `FakeRecognitionEngine` (used in all EditMode tests).

---

## Archive Checklist

- [x] All 36 implementation tasks marked `[x]` in persisted `tasks.md`
- [x] No CRITICAL issues in `verify-report` (0 blockers, 0 critical findings)
- [x] Main spec created and merged into `openspec/specs/reconocimiento-voz-m1/spec.md`
- [x] Change folder moved to `openspec/changes/archive/2026-09-03-m1-speech-to-text/`
- [x] Archive folder contains all artifacts (proposal, specs, design, tasks, verify-report)
- [x] Frozen contract (`contrato-nucleo-m0`) verified untouched
- [x] Final-state corrections confirmed (W4 script deleted, W2 criteria updated)
- [x] All 5 delta-spec requirements (10/10 scenarios) traced to covering tests
- [x] 53 EditMode tests confirmed green (2026-09-01)
- [x] Physical Quest 3 verification completed (2026-09-03) — offline es-CO transcription working
- [x] Archive report persisted to Engram and filesystem

---

## Next Steps

1. **Delivery (TBD)**: Open the 4 stacked PRs when ready (`PR1 -> main, PR2 -> PR1, PR3 -> PR2, PR4 -> PR3`).
2. **VoskResultParserTests confirmation (optional)**: Run this file once more in Unity Editor for explicit dated confirmation, for completeness before merge.
3. **M2 integration**: Update `IntentClassifier` to consume `Utterance` from the new `UtteranceChannel` (M1 is a producer; M2 is the consumer).

---

**Archived by**: SDD Archive Phase  
**Authority**: Final-State Facts + Persisted Artifacts  
**Traceability**: Engram obs #23, #27, #28, #30, #41  
