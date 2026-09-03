# Tasks: M1 — Reconocimiento de voz on-device (Runtime/Speech)

## Review Workload Forecast

| Field | Value |
|---|---|
| Estimated changed lines | ~1900 total (PR1 ~400, PR2 ~500, PR3 ~550, PR4 ~350 authored; binaries/model excluded) |
| 400-line budget risk | High |
| Chained PRs recommended | Yes |
| Suggested split | PR1 → PR2 → PR3 → PR4 |
| Delivery strategy | ask-on-risk |
| Chain strategy | stacked-to-main (resolved 2026-09-01: PR1 -> main, PR2 -> PR1, PR3 -> PR2, PR4 -> PR3) |
| Session budget (800) | each PR fits alone; whole change does not |

Decision needed before apply: Yes
Chained PRs recommended: Yes
Chain strategy: stacked-to-main (resolved)
400-line budget risk: High

Design's own 3-cut split (adapter+guard / capture+segmentation+config / native) puts ~900+ lines
in cut 2 alone, over the 800 budget. Refined here into PR2 (capture/segmentation/pump) + PR3
(config/provisioner/Data). PR1–PR3 stay binary-free — design's fallback if native packaging fails.

### Suggested Work Units

| Unit | Goal | PR | Focused test command | Runtime harness | Rollback boundary |
|---|---|---|---|---|---|
| 1 | Adapter core + late-emission guard + contract twin | PR1 | EditMode: `OfflineSpeechToTextTests` + guard/clamp tests | N/A — no M11 STT scenario yet | Delete new files; `ScriptedSpeechToText`/twin untouched |
| 2 | Capture + segmentation strategies + pump | PR2 | EditMode: strategy + factory tests | N/A — no microphone in EditMode | Delete `Audio/`, `Segmentation/`, `Threading/`; PR1 stays green |
| 3 | Config asset + model provisioner + Data wiring | PR3 | EditMode: config/provisioner/wiring tests | N/A — provisioner uses in-memory zip | Delete `Config/`, `Model/`, `Data/Speech/`; revert wiring |
| 4 | Native Vosk engine + model + MonoBehaviour wrapper | PR4 | EditMode: full Speech suite (no test builds Vosk) | Manual: Quest build, es-CO offline | Revert plugin commit; adapter still works via fake engine |

## Phase 1: Adapter core, late-emission guard, contract twin (PR1)

- [x] 1.1 Create `Runtime/Speech/Properties/AssemblyInfo.cs`: `InternalsVisibleTo("NpcAi.Speech.Tests")`.
- [x] 1.2 Create `Runtime/Speech/IRecognitionEngine.cs`: interface + `RecognitionResult`.
- [x] 1.3 RED: `Tests/EditMode/Speech/OfflineSpeechToTextTests.cs` extends `SpeechToTextContract` (no class yet).
- [x] 1.4 GREEN: Create `Runtime/Speech/OfflineSpeechToText.cs` — state, generation counter, `EmitirParaPrueba`, `ProcesarResultadoDePrueba`, `GeneracionActual`; 10 inherited tests pass.
- [x] 1.5 RED: test — stale-generation result not emitted in the current window.
- [x] 1.6 RED: test — result resolving after `StopListening` not emitted.
- [x] 1.7 GREEN: implement `EmitirSiVigente` (Decision 5); 1.5–1.6 pass.
- [x] 1.8 RED: test — `Confidence`/`DurationSeconds` clamp to `[0,1]`/`>=0`; low confidence (`0.05f`) still emits.
- [x] 1.9 GREEN: implement clamping; confirm `ScriptedSpeechToTextTests` unchanged, still green.

> PR1 tasks (1.1–1.9) implemented and reasoned through; RED/GREEN was not executed by the agent (no CI/headless runner in this repo).
>
> **Manual EditMode verification (2026-09-01): CONFIRMED GREEN by user**, after fixing one real defect found only by the Unity compiler: `SegmentationFactoryTests.cs:17`'s public test method took `TriggerStrategy` as a parameter while that enum was declared `internal` (CS0051, inconsistent accessibility — `InternalsVisibleTo` does not exempt this rule). Fixed by making `TriggerStrategy` `public` in `Runtime/Speech/Segmentation/ISegmentationStrategy.cs`. All 37 tests (PR1: 16, PR2: 21) compile and pass in Unity Editor.

## Phase 2: Capture, segmentation strategies, main-thread pump (PR2)

- [x] 2.1 Create `Runtime/Speech/Audio/IAudioCapture.cs`, `MicrophoneAudioCapture.cs`, `Resampler.cs`.
- [x] 2.2 Create `Runtime/Speech/Segmentation/ISegmentationStrategy.cs`.
- [x] 2.3 RED: `PushToTalkStrategyTests.cs` — always `Continuar` until `MaxSegundosPorFrase`.
- [x] 2.4 GREEN: Create `Runtime/Speech/Segmentation/PushToTalkStrategy.cs`.
- [x] 2.5 RED: `VoiceActivityStrategyTests.cs` — voice+silence closes once; all-silence never closes; max-seconds forces close.
- [x] 2.6 GREEN: Create `Runtime/Speech/Segmentation/VoiceActivityStrategy.cs`.
- [x] 2.7 RED: `SegmentationFactoryTests.cs` — strategy selection by data, no recompile (success criterion 3).
- [x] 2.8 GREEN: Create `Runtime/Speech/Segmentation/SegmentationFactory.cs`.
- [x] 2.9 Create `Runtime/Speech/Threading/IMainThreadPump.cs`, `QueuedMainThreadPump.cs`, `ImmediateMainThreadPump.cs`.

> PR2 tasks (2.1–2.9) implemented; manual EditMode execution in Unity Editor (Window > General > Test Runner > EditMode > Run All) is still required to confirm actual green — no CI/headless runner exists in this repo, so RED/GREEN above was reasoned through, not executed. `SegmentationFactory`/strategies/pumps are pure C# (no `UnityEngine`), fully unit-testable with synthetic sample arrays; `MicrophoneAudioCapture` wraps `UnityEngine.Microphone` and has no automated test (design.md's own EditMode-has-no-microphone constraint) — only its pure `Resampler` helper is unit-tested. `TriggerStrategy` enum was placed in `Runtime/Speech/Segmentation/ISegmentationStrategy.cs` for PR2 (design.md shows it under the future `Config/SpeechSettings.cs`, which is PR3 scope/task 3.1 and out of bounds here); PR3 should reuse `NpcAi.Speech.Segmentation.TriggerStrategy` from `SpeechSettings.Estrategia` rather than declare a duplicate.

## Phase 3: Configuration, model provisioning, Data wiring (PR3)

- [x] 3.1 Create `Runtime/Speech/Config/SpeechSettings.cs` (POCO snapshot, no `UnityEngine`).
- [x] 3.2 RED: `SpeechSettingsAssetTests.cs` — `OnValidate` clamps out-of-range fields.
- [x] 3.3 GREEN: Create `Runtime/Speech/Config/SpeechSettingsAsset.cs` + `ToSettings()`.
- [x] 3.4 Create `Data/Speech/Boardroom.asset`, `Data/Speech/README.md` (regla dura 7, per-scenario).
- [x] 3.5 RED: `SpeechModelProvisionerTests.cs` — in-memory zip extracts once; second call no-op; `../` entry rejected.
- [x] 3.6 GREEN: Create `Runtime/Speech/Model/SpeechModelProvisioner.cs`.
- [x] 3.7 Create `Tests/EditMode/Speech/FakeRecognitionEngine.cs` (test double, Tests assembly).
- [x] 3.8 Wire `OfflineSpeechToText` real Start/Stop to capture → segmentation → engine → pump (Decision 3/4).
- [x] 3.9 RED: integration test — `PulsarParaHablar` + fake engine emits exactly one `Utterance` per window.
- [x] 3.10 GREEN: confirm 3.9 passes; `ActividadDeVoz` emits one `Utterance` per silence cut via `QueuedMainThreadPump`.

> PR3 tasks (3.1-3.10) implemented; manual EditMode execution in Unity Editor (Window/General/
> Test Runner/EditMode/Run All) is still required to confirm actual green — no CI/headless
> runner exists in this repo, so RED/GREEN above was reasoned through, not executed.
>
> **Deviation from design.md, flagged (not silent):** `OfflineSpeechToText`'s wired constructor
> does NOT hold a reference to `IAudioCapture`. Task 3.8 says "Wire ... Start/Stop to capture ->
> segmentation -> engine -> pump", but owning `IAudioCapture` directly would have required a new
> `FakeIAudioCapture` test double not listed anywhere in tasks.md (only `FakeRecognitionEngine`,
> 3.7, is scoped for PR3), and EditMode has no microphone to drive a real one deterministically.
> Instead, `OfflineSpeechToText` exposes `internal void AlimentarBloqueDeAudio(float[], int)` as
> the entry seam for each captured block; `StartListening`/`StopListening` own segmentation +
> engine + pump exactly as Decision 3/4 describe. The actual `MicrophoneAudioCapture` (PR2) reads
> and calls this seam from the still-to-be-built `SpeechToTextBehaviour.Update()` (PR4, task
> 4.6) — PR4 must call `AlimentarBloqueDeAudio` with each block `IAudioCapture.LeerDisponibles`
> returns, and drive `bomba.Drenar()` (already exposed via `QueuedMainThreadPump`/`ImmediateMainThreadPump`)
> each frame if using the queued pump.

## Phase 4: Native Vosk engine, model asset, wrapper, licensing (PR4)

- [x] 4.1 Add `Runtime/Speech/Plugins/LICENSE-vosk-apache-2.0.txt` (+ `NOTICE.txt` if upstream provides one).
- [x] 4.2 Add `Plugins/Android/arm64-v8a/libvosk.so` + `Plugins/Windows/x86_64/libvosk.dll` with committed `.meta`.
- [x] 4.3 Add `Plugins/Models/vosk-model-small-es-0.42.bytes`; reference from `Boardroom.asset`'s `ModeloEmpaquetado`.
- [x] 4.4 Create `Runtime/Speech/Vosk/VoskInterop.cs` (`[DllImport("libvosk")]` bindings).
- [x] 4.5 Create `Runtime/Speech/Vosk/VoskRecognitionEngine.cs` implementing `IRecognitionEngine`.
- [x] 4.6 Create `Runtime/Speech/SpeechToTextBehaviour.cs`: mic permission, provisioning trigger, pump drain in `Update()`, `UtteranceChannel` publish.
- [x] 4.7 MANUAL (Quest hardware): Android build transcribes es-CO offline; `Confidence` in `[0,1]`, `DurationSeconds >= 0`. **CONFIRMED 2026-09-03** on a physical Meta Quest 3. Real logcat output: `texto="prueba de sonido uno" confidence=1,00 duration=2,88s`, `texto="prueba de sonido dos" confidence=1,00 duration=3,28s`, `texto="prueba de sonido tres" confidence=0,99 duration=3,68s` — correct Spanish transcription, `Confidence` in `[0,1]`, `DurationSeconds >= 0`, fully offline. Three real bugs were found and fixed to get here (see Engram `sdd/m1-speech-to-text/apply-progress` and `sdd/m1-speech-to-text/vosk-scaling-bugfix` for the full debugging trail): (1) the vendored Android `libvosk.so` was actually an x86_64 Linux build, not ARM64 — replaced with the correct binary from the official `com.alphacephei:vosk-android:0.3.75` Maven AAR; (2) Development Build wasn't enabled, silently suppressing all managed `Debug.Log` output; (3) audio samples reached Vosk in Unity's normalized `[-1,1]` scale instead of the PCM16 `[-32768,32767]` scale Kaldi expects — fixed in `VoskRecognitionEngine.Alimentar()` by scaling into a dedicated buffer (kept separate from the VAD-calibrated original buffer). Also fixed along the way: a too-short first test session, and a `-d` dump-at-end logcat capture losing early output to Quest's high-volume ring buffer (use streaming `adb logcat > file` + Ctrl+C instead), and a lifecycle-ordering bug in the disposable test harness itself (`MostrarUtterance.OnEnable()` calling `StartListening()` before `SpeechToTextBehaviour.Start()` had a chance to arm the pipeline — fixed by retrying in `Update()` instead).

> **Manual test harness built 2026-09-02 (host project, outside this package — not part of any
> PR's diff)**, needed to exercise `SpeechToTextBehaviour` since no scene wired it before:
> - `Assets/Scripts/UtteranceChannel.asset` (host project) — a `UtteranceChannel` instance created
>   via the Editor's Create menu (`NPC AI > Canales > Utterance`). Originally created inside
>   `Packages/com.poli.npc-ai/` by mistake (user was navigating the Project window's Packages
>   view, not Assets), moved to `Assets/Scripts/` by the orchestrator (file+`.meta` move together,
>   GUID preserved, verified safe since it's a pure relocation not a from-scratch YAML authorship).
> - `Assets/Scripts/MostrarUtterance.cs` (host project, throwaway/disposable, NOT part of
>   `com.poli.npc-ai`) — subscribes to the channel, calls `StartListening()` in `OnEnable`,
>   `Debug.Log`s each `Utterance` (text/confidence/duration) for `adb logcat` inspection. Can be
>   deleted once 4.7 is confirmed.
> - `Assets/Scenes/SampleScene.unity` — `SpeechTest` GameObject with `SpeechToTextBehaviour`
>   (`_configuracion` → `Data/Speech/Boardroom.asset`, `_canalDeUtterance` → the channel asset
>   above) and `MostrarUtterance` (`speech`/`canal` wired to the same two). `Boardroom.asset`'s
>   `Estrategia` changed from `PulsarParaHablar` (0) to `ActividadDeVoz` (1) so no push-to-talk
>   input is needed — orchestrator verified this in the actual asset file (`Estrategia: 1`).
>   Orchestrator cross-checked every GUID reference in the saved scene file against the real
>   asset `.meta`s before greenlighting the build — no dangling/mismatched references.
> - A stray `Assets/SpeechSettings.asset` (accidental duplicate `SpeechSettingsAsset` instance,
>   created by the user while exploring the Create menu) was removed at the user's request —
>   `Boardroom.asset` is the one actual config asset in use.
- [x] 4.8 MANUAL (Quest hardware): committed `.meta` importer settings survive a clean build (no missing `.so`/wrong ABI).

> **4.8 completed 2026-09-02.** User installed Android Build Support (SDK/NDK/OpenJDK) via Unity
> Hub — it was missing, which is why "Android" didn't initially appear as a selectable platform
> in the Plugin Inspector. After that, user set each native plugin's platform in the Inspector.
> Orchestrator verified the resulting `.meta` files directly (not just trusting the user's
> report): `libvosk.so.meta` → `Android: enabled: 1, CPU: ARM64`, every other platform
> (`Any`, `Editor`, `Linux64`, `OSXUniversal`, `WebGL`, `Win`, `Win64`) `enabled: 0`.
> `libvosk.dll.meta` + all 3 companion DLLs (`libgcc_s_seh-1.dll`, `libstdc++-6.dll`,
> `libwinpthread-1.dll`) → `Editor: enabled: 1, CPU: x86_64, OS: Windows`, `Android`/`Any`/every
> Standalone target `enabled: 0`. No platform overlap between the Android and Windows binaries.
> Only 4.7 (actual Quest hardware build + offline es-CO transcription) remains — that verification
> cannot happen without the physical device.

> **4.2/4.3 completed 2026-09-02 (orchestrator, after user vendored the real files).** The user
> downloaded the real `libvosk.so`/`.dll` (+ 3 Windows MinGW runtime companions:
> `libgcc_s_seh-1.dll`, `libstdc++-6.dll`, `libwinpthread-1.dll`) and the unzipped
> `vosk-model-small-es-0.42/` model directory, but flat under `Plugins/` rather than in the
> expected per-platform structure, and as loose files rather than the single `.bytes` TextAsset
> design.md's Decision 1 calls for. Reorganized:
> - Moved `libvosk.so` (+ `.meta`) to `Plugins/Android/arm64-v8a/`.
> - Moved `libvosk.dll` + the 3 MinGW companion DLLs (+ their `.meta`s) to `Plugins/Windows/x86_64/`.
>   Unity needs the companions alongside `libvosk.dll` to resolve its runtime dependencies on
>   Windows (Editor use). Added `.meta` for every new folder (`Android`, `Android/arm64-v8a`,
>   `Windows`, `Windows/x86_64`, `Models`), matching this repo's existing minimal-meta convention.
> - Zipped the raw model directory's contents (excluding `.meta` files, no top-level folder
>   prefix so extraction lands files directly under the destination) into
>   `Plugins/Models/vosk-model-small-es-0.42.bytes` (40,108,085 bytes, 14 entries) using Python's
>   `zipfile` — matches exactly what `SpeechModelProvisioner.ExtraerEnMemoria` expects. Deleted
>   the now-redundant raw `vosk-model-small-es-0.42/` directory (58 MB of loose files Unity would
>   otherwise try to import as hundreds of individual generic assets).
>   Generated a fresh GUID (`e7c5aaa574fd42a0bfc3a0701fcea637`), checked for collisions (none),
>   wrote its `.meta`, and wired `Data/Speech/Boardroom.asset`'s `ModeloEmpaquetado` from
>   `{fileID: 0}` to `{fileID: 4900000, guid: e7c5aaa574fd42a0bfc3a0701fcea637, type: 3}`.
> - `vosk_api.h` left in place at `Plugins/` root — harmless reference documentation, not part of
>   the runtime pipeline (Unity will import it as a generic text asset; no build impact).
>
> **What 4.2/4.3 do NOT yet cover**: the hand-authored 2-line `.meta` stubs for the native
> libraries carry no `PluginImporter`/`platformData` block — Unity has not actually imported
> these yet. On first Editor open, Unity will auto-import them; the conventional
> `Android/arm64-v8a/` and `Windows/x86_64/` folder names should steer its automatic platform
> detection correctly, but this is exactly what task 4.8 already exists to verify and commit
> (Inspector platform checkboxes: `libvosk.so` → Android/ARM64 only; `libvosk.dll` + companions →
> Editor/Windows x64 only) — do not consider 4.8 satisfied by this reorganization alone.

> **PR4 code-only batch (2026-09-01): 4.1, 4.4, 4.5, 4.6 implemented.** 4.2 and 4.3 stay
> unchecked on purpose — they need the real `libvosk.so`/`libvosk.dll` and the real
> `vosk-model-small-es-0.42` model file, which the user sources and vendors themselves from
> upstream (alphacep.com / vosk-api GitHub releases); the agent did not create placeholder or
> fake binaries. 4.7 and 4.8 stay unchecked as MANUAL Quest-hardware checks the agent cannot
> perform. Details:
>
> - **4.1**: `LICENSE-vosk-apache-2.0.txt` has the full, verbatim Apache License 2.0 text plus
>   a short attribution note. **NOTICE question resolved 2026-09-01** (orchestrator, with web
>   access): checked `github.com/alphacep/vosk-api` root directly — only `.gitignore`,
>   `.travis.yml`, `CMakeLists.txt`, `COPYING`, `README.md`. No `NOTICE`/`NOTICE.txt` exists
>   upstream, so Apache 2.0 Section 4(d) is not triggered; the LICENSE file alone satisfies
>   attribution. Updated inline in the license file itself.
> - **4.4**: `VoskInterop.cs` binds `vosk_model_new`/`_free`, `vosk_recognizer_new`,
>   `_set_words`, `_accept_waveform_f`, `_result`, `_final_result`, `_reset`, `_free`, and
>   `_set_log_level` against `libvosk`. **Verified 2026-09-01** (orchestrator, with web access):
>   fetched the real `vosk_api.h` from `alphacep/vosk-api` and diffed all 9 signatures
>   line-by-line against this file — every one matches exactly, including `vosk_set_log_level`
>   which the agent added beyond design.md's two explicitly-named functions. No longer an
>   unverified risk; a symbol mismatch at first real use (4.7) would now only mean the user's
>   vendored binary is a materially different/incompatible build, not a transcription error here.
> - **4.5**: `VoskRecognitionEngine` implements `IRecognitionEngine`: loads the model once,
>   creates one native recognizer reused for the object's lifetime (`Reiniciar()` calls
>   `vosk_recognizer_reset`, never recreates), and decodes `vosk_recognizer_final_result`'s
>   UTF-8 JSON manually (no `Marshal.PtrToStringUTF8`/`LPUTF8Str`, since their availability on
>   this project's Mono/IL2CPP target could not be verified — same risk-avoidance stance as
>   PR3's `using var` avoidance). JSON parsing itself was factored out into a separate pure
>   class, `VoskResultParser` (not in tasks.md's original file list, added because it is
>   genuinely testable without the native binary — see below).
> - **4.6**: `SpeechToTextBehaviour` requests `RECORD_AUDIO` on Android only (`#if
>   UNITY_ANDROID`, via `PermissionCallbacks`), then provisions the model
>   (`SpeechModelProvisioner.Aprovisionar` against `Application.persistentDataPath`), builds the
>   wired `OfflineSpeechToText` + `VoskRecognitionEngine` + `QueuedMainThreadPump`, drains the
>   pump every `Update()`, reads `IAudioCapture.LeerDisponibles` and feeds
>   `OfflineSpeechToText.AlimentarBloqueDeAudio`, and republishes `OnUtterance` onto
>   `UtteranceChannel`. Deviation from a literal reading of 4.6 (flagged, not silent): the
>   microphone is started/stopped from `StartListening`/`StopListening` (the same methods the
>   host scene's push-to-talk button wires to), not from component `Start()`/`OnDestroy()` —
>   an always-on microphone on a Quest headset is a battery and privacy concern this component
>   should not assume by default, and nothing in design.md requires continuous capture before
>   the user opts in.
> - **New file beyond the original list, in scope of PR4's own goal**:
>   `Runtime/Speech/Vosk/VoskResultParser.cs` — pure JSON-to-`RecognitionResult` parsing (no
>   `[DllImport]`, no native call), covered by real RED/GREEN EditMode tests
>   (`Tests/EditMode/Speech/VoskResultParserTests.cs`, 5 tests). This is the only slice of the
>   Vosk engine that can be genuinely exercised in EditMode without the native binary present,
>   per the strict-TDD instruction to test genuinely pure logic where practical instead of
>   inventing fake tests for the native-call parts.
>
> Manual EditMode execution in Unity Editor (Window > General > Test Runner > EditMode > Run
> All) is still required to confirm the 5 new `VoskResultParserTests` compile and pass — no
> CI/headless runner exists in this repo. `VoskInterop`/`VoskRecognitionEngine`/
> `SpeechToTextBehaviour` cannot be exercised by any automated test until 4.2/4.3 land and a
> human runs the Quest build (4.7/4.8).
