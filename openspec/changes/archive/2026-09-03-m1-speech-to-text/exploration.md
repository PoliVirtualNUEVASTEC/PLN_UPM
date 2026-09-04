# Exploration: M1 — Real `ISpeechToText` implementation

## Current state

`Runtime/Speech/` (asmdef `NpcAi.Speech`, references `NpcAi.Core` + `NpcAi.Core.Channels`) currently
holds only `Fakes/ScriptedSpeechToText.cs`. No real implementation exists yet.

The frozen v1 contract (`Runtime/Core/Ports.cs`, `Runtime/Core/Dtos.cs`,
`openspec/specs/contrato-nucleo-m0/spec.md`) fixes:

- `Utterance { Text, Confidence [0,1], DurationSeconds >= 0 }` — the struct does not clamp these
  ranges; that is the producer's (M1's) responsibility.
- `ISpeechToText { OnUtterance, IsListening, StartListening(), StopListening() }` with a precise
  listening-window state machine: no emission before the first `StartListening`/after
  `StopListening`, idempotent `Stop`, fan-out to all subscribers, no throw without subscribers.

`Tests/EditMode/Core/SpeechToTextContract.cs` is the shared abstract base class both the fake and
the future real implementation must inherit. Its own doc comment already names "Whisper/Sentis o
Vosk" as the M0 author's expected direction — a hint, not a binding decision.

`Runtime/CoreChannels/Channels.cs` already ships `UtteranceChannel : EventChannel<Utterance>` —
the Inspector-wiring path for M1's output already exists in the frozen contract.

**Stack:** Unity `6000.5.4f1`. Sentis (`com.unity.ai.inference` 2.6.1) is already a dependency,
used by M2 for on-device NLU (MiniLM), but not wired for M1. `package.json` declares no
`dependencies`. Testing is EditMode-only via the manual Unity Editor Test Runner; no CI/headless
runner is wired in this repo.

`Runtime/VrInput/` (M7) was checked and confirmed out of scope for audio: it only covers physical
`PhysicalAction` input, nothing about microphone capture. **M1 owns raw audio capture itself.**

No target VR headset/platform (Meta Quest/Android vs. PCVR/Windows) is documented anywhere in the
repo. This is the single highest-leverage open question — it directly gates backend viability.

## Candidate approaches

| # | Approach | Notes | Effort |
|---|----------|-------|--------|
| 1 | `UnityEngine.Windows.Speech.DictationRecognizer` | Zero extra dependencies. Verified via Unity docs: **Windows 10 only** — no Android/Quest support. Likely disqualified pending platform confirmation. | Low |
| 2 | Cloud STT API (Azure / Google / hosted Whisper) | Strong accuracy and Spanish support; needs reliable network in the room, adds round-trip latency, raises an unresolved "secrets in a ScriptableObject" question. | Medium |
| 3 | Local on-device via Sentis (Whisper-family ONNX) | Offline, reuses the existing Sentis dependency, matches the doc-comment hint and the project's "on-device AI" framing. Unmeasured latency/footprint on VR hardware; no in-repo precedent for audio-input Sentis inference. | High |
| 4 | Vosk (native plugin) | Lightweight offline, official Spanish models. Needs a native plugin per target platform; zero precedent in this repo. | Medium-High |

## Contract vs. async reconciliation

No M0 contract gap was found. `StartListening`/`StopListening` do not need to block; `OnUtterance`
can fire whenever async work (network call or model inference) completes, as long as it happens on
the main thread — the same pattern the fake already uses in `Emit()`. The one thing a real
implementation must add on its own: guard against a late async result arriving **after**
`StopListening()` was called, since the contract forbids emitting after Stop (e.g. a generation
counter, or re-checking `IsListening` before invoking `OnUtterance`), mirroring the fake's existing
guard.

## Open questions for `sdd-propose`

1. **Target VR platform** — Meta Quest (Android, standalone) vs. PCVR (Windows-tethered) vs. both.
   Gates every other decision below.
2. Online (cloud STT) vs. offline (on-device) requirement.
3. Latency budget — none is set yet; M11 (Harness) is expected to start measuring it around
   Sprint 13-14.
4. Spanish-language (es-CO) recognition accuracy requirement — `Data/Corpus/` is entirely Spanish.
5. Listening-window trigger UX — push-to-talk vs. voice-activity detection (VAD).
6. If a cloud backend is chosen: how API credentials/secrets are handled (not a ScriptableObject
   concern per project convention, since those are meant for non-secret variable data).
7. If Sentis is chosen: whether `package.json` needs an explicit `dependencies` entry for
   `com.unity.ai.inference` (currently undeclared there despite being used by M2).

## Risks

- No CI/headless test runner: a real M1 implementation needs a deterministic test seam (mirroring
  the fake's `EmitTestUtterance`) that does not require a live microphone, network call, or loaded
  model — this is `sdd-design`'s responsibility to define.
- Target-platform ambiguity risks building a backend that is incompatible with the actual delivery
  device.
- Both the Vosk/native-plugin and the Sentis-audio-inference paths are unprecedented in this repo —
  either introduces non-trivial integration risk.

## Ready for proposal

Yes, once the target-platform and online/offline questions are resolved with the user.
