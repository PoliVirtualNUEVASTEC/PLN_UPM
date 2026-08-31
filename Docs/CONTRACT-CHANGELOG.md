# Historial del contrato (`NpcAi.Core`)

Toda modificacion a `NpcAi.Core` sube `Contract.Version` y deja una entrada aqui.
Regla: los cambios de contrato se integran **solo los lunes**, en un cambio SDD propio, con aprobacion de todos los duenos de modulo.

## v1 — 2026-08-30 — Contrato inicial congelado (Sprint 0)

Primera version. Define:

- **DTO inmutables:** `Utterance`, `IntentResult`, `ReceptivityChange`, `NpcReply`, `PersonalityId`.
- **Enums:** `Intent`, `Tone`, `PhysicalAction`, `Receptivity`.
- **Siete puertos:** `ISpeechToText`, `IIntentClassifier`, `IPhysicalActionSource`, `IReceptivityEngine`, `IDialogueGenerator`, `INpcPresenter`, `IScenarioObjective`.

Decisiones de diseno que quedan registradas con la v1:

1. `PersonalityId` es un `readonly struct` sobre `string`, no un enum. **El numero de personalidades es un dato de M5, no un cambio de contrato.** Pasar de 4 a 8 personalidades no toca este archivo.
2. Todos los enums tienen el valor seguro en `0` (`Intent.Desconocida`, `Tone.Neutral`, `PhysicalAction.Ninguna`). Un valor sin inicializar nunca significa algo fuerte.
3. `Receptivity` se numera `-1 / 0 / 1` para que el orden sea comparable y las transiciones se puedan probar aritmeticamente.
4. `NpcAi.Core` se compila con `noEngineReferences: true`. El contrato es C# puro **por compilacion**, no por convencion.
5. Los canales de evento (ScriptableObject) viven en `NpcAi.Core.Channels`, un ensamblado aparte que si referencia Unity.
