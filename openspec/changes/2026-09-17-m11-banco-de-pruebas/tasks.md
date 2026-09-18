# Tasks: M11 — Banco de pruebas, PR1 (`SessionDirector`)

## Review Workload Forecast

| Field | Value |
|---|---|
| Estimated changed lines | ~750-900 (2 `.asmdef` ~35, `SessionDirector.cs` ~130-160, `EspiasDeArnes.cs` ~140-170, 3 test files ~440-500 across 12 scenarios + constructor validation) |
| 400-line budget risk | High |
| Chained PRs recommended | No — user chose `size:exception` (2026-09-18) |
| Suggested split | 3 stacked units mirroring design.md's TDD order was offered; user picked `size:exception` in one PR instead, same reasoning M9 used for a single cohesive class + its suite |
| Delivery strategy | ask-on-risk (resolved) |
| Chain strategy | **size:exception — single PR**, confirmed by user |

Decision needed before apply: Resolved — size:exception
Chained PRs recommended: No (user override)
Chain strategy: size:exception
400-line budget risk: High (accepted)

### Suggested Work Units

| Unit | Goal | Likely PR | Focused test command | Runtime harness | Rollback boundary |
|---|---|---|---|---|---|
| 1 | Foundation: both `.asmdef`, `EspiasDeArnes.cs` (5 spies), constructor+validation, `IniciarSesion`/`Arrancar`/selección por semilla | PR1a (base = tracker) | EditMode: `SessionDirectorSesionTests` | N/A — C# puro, sin escena/VR/audio | Delete `Runtime/Harness/`, `Tests/EditMode/Harness/`; nada más referencia `NpcAi.Harness` |
| 2 | Turno: `ProcesarTurno`/`ProcesarAccion`, centinelas + enrutado clínico/social | PR1b (base = PR1a) | EditMode: `SessionDirectorTurnoTests` | N/A — C# puro | Revertir los métodos de turno + su archivo de prueba; PR1a sigue en verde |
| 3 | Fronteras: `DeclararTriaje`, aserciones de límite/reflexión | PR1c (base = PR1b) | EditMode: `SessionDirectorFronterasTests` | N/A — C# puro | Revertir `DeclararTriaje` + su archivo de prueba; PR1a/PR1b siguen en verde |

Si el usuario acepta `size:exception`, las fases siguientes se aplican como un solo PR.

## Phase 0: Guardrails

- [ ] 0.1 Confirmar frontera del diff antes de escribir: solo `Runtime/Harness/`, `Tests/EditMode/Harness/`, `openspec/`. Nada en `Runtime/Core/`, `Runtime/CoreChannels/`, otro `Runtime/<Modulo>/`, `Data/`, `Samples~/`, `Docs/` (son PR2/fuera de alcance).
- [ ] 0.2 Confirmar que no hay cambio de contrato: `Ports.cs`/`Dtos.cs` (M0) sin tocar; firmas de `IScenarioObjective`/`IClinicalResponder` sin cambio (AD1).
- [x] 0.3 Ya resuelto: `spec.md` ya nombra un doble local para "Nunca registra banderas rojas" y ya explica por reflexión la ausencia de costura `INpcPresenter` en "Nunca reproduce directamente" (commit `4fe4709`, previo a este `sdd-tasks`). `design.md` Open Questions quedó desactualizado en este punto — no repetir la correccion.
- [ ] 0.4 Nota (sin acción): los `.meta` de cada `.cs`/carpeta nuevo faltarán hasta que el Unity Editor del usuario los regenere — necesita su propio commit de seguimiento, igual que el fix de M7.

## Phase 1: Foundation (asmdefs + espías)

- [ ] 1.1 Crear `Runtime/Harness/NpcAi.Harness.asmdef` — `references: ["NpcAi.Core"]`, `noEngineReferences: true` (AD12), espejo literal de `Runtime/Receptivity/NpcAi.Receptivity.asmdef`.
- [ ] 1.2 Crear `Tests/EditMode/Harness/NpcAi.Harness.Tests.asmdef` — referencia `NpcAi.Core`, `NpcAi.Harness`, `NpcAi.ClinicalResponse`, `NpcAi.Presentation`, TestRunner, `nunit.framework.dll`; solo Editor.
- [ ] 1.3 Crear `Tests/EditMode/Harness/EspiasDeArnes.cs` con los 5 espías `internal sealed` (AD11): `EspiaClasificador`, `EspiaReceptividad`, `EspiaDialogo`, `EspiaObjetivo`, `EspiaClinico` — cada uno graba exactamente lo que la tabla "Testing Strategy" de design.md nombra.

## Phase 2: TDD — Constructor, arranque y selección por semilla (PR1a)

- [ ] 2.1 RED: `Tests/EditMode/Harness/SessionDirectorSesionTests.cs` — constructor: cada uno de los 5 puertos + 2 delegados nulo → `ArgumentNullException`; `casos`/`personalidades` nulos o vacíos → `ArgumentException` (AD3).
- [ ] 2.2 RED (mismo archivo): escenarios 7-9 de la spec — los 3 efectos de `IniciarSesion` con el mismo `ClinicalCaseId` en M9/M15; misma semilla+catálogo repiten el par (bucle ~20 sesiones, fija la secuencia completa); sesión nueva no repite el caso anterior (+ borde: catálogo de 1 caso, AD7).
- [ ] 2.3 RED (mismo archivo): `Progreso` refleja `_m9.Progress01` sin cachear — prueba de diseño no nombrada por la spec.
- [ ] 2.4 GREEN: `Runtime/Harness/SessionDirector.cs` — constructor + validación (AD3), campos, `IniciarSesion()`/`IniciarSesion(caso, personalidad)`, `SiguienteCaso()` (lista filtrada, AD7), `Arrancar()` (AD8: actualiza `_ultimoCaso` en ambos caminos), `Progreso` — pseudocódigo exacto de design.md.

## Phase 3: TDD — Turno: centinelas y enrutado (PR1b)

- [ ] 3.1 RED: `Tests/EditMode/Harness/SessionDirectorTurnoTests.cs` — 6 escenarios: solo `Utterance` evalúa con `PhysicalAction.Ninguna`; solo `PhysicalAction` evalúa con `IntentResult.Unknown` sin llamar `Classify`; la acción mueve `m4.Current` (con `EspiaReceptividad`, no `ScriptedReceptivityEngine`: su delta para `Acercarse` es cero); la acción devuelve `null` sin llamar `Respond`/`Generate`; turno clínico (`Handled==true`) no llama `Generate`; turno social llama `Generate` con `m4.Current`.
- [ ] 3.2 GREEN: `ProcesarTurno`/`ProcesarAccion` en `SessionDirector.cs` — `_m9.Notify` en un solo sitio antes de la rama (AD10), enrutado `clin.Handled ? clin.Reply : m6.Generate(...)`.

## Phase 4: TDD — Fronteras (PR1c)

- [ ] 4.1 RED: `Tests/EditMode/Harness/SessionDirectorFronterasTests.cs` — por reflexión, ningún parámetro del constructor público es `INpcPresenter` (más una instancia sin costura de `RecordingNpcPresenter`, per 0.3); `EspiaObjetivo.BanderasRegistradas == 0` tras una sesión completa; instanciación y operación sin escena (`Assert.DoesNotThrow` antes de `IniciarSesion`, AD9); `DeclararTriaje` hace pass-through crudo al delegado.
- [ ] 4.2 GREEN: `DeclararTriaje(string)` en `SessionDirector.cs` — una línea, delega en `_declararTriaje`.

## Phase 5: Compuerta humana

- [ ] 5.1 MANUAL (Unity Editor — la ejecuta y registra el usuario, no el agente): Test Runner > EditMode > Run All en verde con las Fases 1-4 completas; registrar el total en `apply-progress.md`. No aplica compuerta física: sin escena/VR/audio en PR1.

## Phase 6: Cierre

- [ ] 6.1 `git add` acotado a `Runtime/Harness/`, `Tests/EditMode/Harness/`, `openspec/changes/2026-09-17-m11-banco-de-pruebas/`; `git diff --cached --stat` antes de cualquier commit; confirmar cero líneas fuera de esas rutas.
- [ ] 6.2 Checklist "Antes de mergear": pruebas propias en verde (5.1), diff acotado (6.1), ningún `.asmdef` ajeno ni `Runtime/Core`/`Runtime/CoreChannels` tocado, rama al día con `main`.
- [ ] 6.3 Commit de seguimiento para los `.meta` faltantes (0.4) una vez el usuario abra Unity Editor.

## Notas

- Cero `Debug.Log` en `Runtime/Harness/` es un error de compilación, no una revisión manual (AD12: `noEngineReferences: true` excluye `UnityEngine.dll`).
- La spec declara 7 requisitos y 12 escenarios; si `sdd-verify` espera otro número, el desajuste está en el conteo de la orquestación (design.md, Open Questions), no en la spec ni en estas tareas.
- El folder stale `openspec/changes/2026-09-09-m11-armado-sesion/` ya no existe (confirmado por glob); no requiere tarea de borrado.
