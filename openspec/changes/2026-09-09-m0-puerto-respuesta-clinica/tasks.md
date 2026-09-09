# Tasks: M0 — Puerto de respuesta clínica (`IClinicalResponder`)

## Review Workload Forecast

| Field | Value |
|---|---|
| Estimated changed lines | ~130 (1 struct nuevo ~40, `ClinicalResponse` ~15, `IClinicalResponder` ~25, `Version` 1, changelog `## v2` ~30, base de prueba ~50, casos en `ContractTypeTests` ~20) |
| 400-line budget risk | Bajo |
| Chained PRs recommended | No |
| Suggested split | PR único |
| Chain strategy | n/a |
| Decision needed before apply | Sí — es cambio de contrato: co-revisión del otro dueño de M0 o el asesor antes del merge (regla 3) |

### Suggested Work Units

| Unit | Meta | PR | Prueba enfocada | Frontera de rollback |
|---|---|---|---|---|
| 1 | Puerto + DTO + version + changelog + base de prueba | PR único | EditMode: `ContractTypeTests`, `ContractVersionChangelogTests`, `CoreAssemblyPurityTests`, `ClinicalResponderContract` contra stub local | Revertir el commit: `Version` vuelve a `1`, se quita `## v2`, se quitan puerto/DTO/base. Ningún módulo referencia `IClinicalResponder` todavía |

## Phase 0: Guardrails (leer antes de escribir)

- [x] 0.1 **Este es un cambio de contrato (M0).** Autoriza tocar `Runtime/Core/` y
  `Docs/CONTRACT-CHANGELOG.md`, y nada más de `Runtime/`. Antes del merge lo revisa el otro
  dueño compartido de M0 (Jefferson) o el asesor (regla 3 del `README.md` / `openspec/config.yaml`).
  **No hay ventana fija de lunes ni quorum de todos los dueños** — si el `CONTRACT-CHANGELOG.md`
  todavía dice eso en su encabezado, corregirlo es parte de este cambio (tarea 1.5).
- [x] 0.2 Frontera de escritura: `Runtime/Core/ClinicalCaseId.cs` (nuevo),
  `Runtime/Core/Dtos.cs`, `Runtime/Core/Ports.cs`, `Runtime/Core/Contract.cs`,
  `Docs/CONTRACT-CHANGELOG.md`, `Tests/EditMode/Core/ClinicalResponderContract.cs` (nuevo),
  `Tests/EditMode/Core/ContractTypeTests.cs`, y este directorio de cambio.
- [x] 0.3 **Cambio puramente aditivo.** NO renombrar, reordenar, ni cambiar el valor de
  ningún miembro de `Intent`, `Tone`, `PhysicalAction`, `Receptivity`. NO cambiar la firma
  de los 7 puertos ni la forma de los 5 DTO de v1. Si algo parece exigir eso, parar y
  avisar — sería una ruptura de contrato, otro alcance.
- [x] 0.4 `ContractTypeTests` se **amplía** (casos nuevos para los tipos nuevos), no se
  reescribe: ningún caso existente cambia. `ContractVersionChangelogTests` y
  `CoreAssemblyPurityTests` no se tocan.
- [x] 0.5 Este módulo es M0: `/sdd-ff` no aplica. El flujo completo propuesta → diseño →
  tareas → revisión humana + co-revisión de M0.
- [x] 0.6 `NpcAi.Core` compila con `noEngineReferences: true`. Los 3 tipos nuevos son C#
  puro: `string`, `bool`, `struct`, y tipos de v1. Cero `using UnityEngine`.

## Phase 1: Tipos, puerto y versión

- [x] 1.1 Crear `Runtime/Core/ClinicalCaseId.cs`: copiar `Runtime/Core/PersonalityId.cs`
  cambiando el nombre del tipo y la doc-comment (identificador de caso clínico, dato de
  M14, no enum). Igualdad `Ordinal`, `None = default`, `IsNone`, `ToString() => Value ?? "(none)"`.
- [x] 1.2 En `Runtime/Core/Dtos.cs`, agregar `readonly struct ClinicalResponse` con
  `Handled` (bool), `Reply` (`NpcReply`), constructor de dos argumentos, y
  `static ClinicalResponse NoAplica => new(false, default)`. Doc-comment: "Producido por
  M15, consumido por el enrutador; `Handled == false` significa turno no clínico".
- [x] 1.3 En `Runtime/Core/Ports.cs`, agregar `interface IClinicalResponder` (ver
  `design.md` → Interfaces / Contracts) con `IsReady`, `AssignCase(ClinicalCaseId,
  PersonalityId)`, `Respond(Utterance, IntentResult)` y las doc-comments DEBE/NO DEBE
  completas. Ubicarlo en una sección nueva `// ---- Respuesta clínica` tras "Comprensión".
- [x] 1.4 En `Runtime/Core/Contract.cs`, subir `Version` a `2`. **En el mismo commit**,
  agregar a `Docs/CONTRACT-CHANGELOG.md` la sección `## v2 — 2026-09-09 — Puerto de
  respuesta clínica (M15)` con: los 3 tipos nuevos, los invariantes del puerto en
  DEBE/NO DEBE, y una entrada nueva bajo "Asimetría de determinismo" registrando que
  `IClinicalResponder.Respond` SÍ es determinista en `Handled` y `Reply.Text` (frente a
  `IDialogueGenerator.Generate`, que no).
- [x] 1.5 En `Docs/CONTRACT-CHANGELOG.md`, corregir la línea de regla de integración: de
  "los cambios de contrato se integran **solo los lunes**... con aprobacion de todos los
  duenos de modulo" a la regla vigente ("cambio SDD propio, revisado antes del merge por el
  otro dueño compartido de M0 o el asesor; sin ventana fija ni quorum"). Debe coincidir
  palabra por palabra con la intención de `openspec/config.yaml` → `rules.proposal`.

## Phase 2: Base de prueba de contrato

- [x] 2.1 RED/estructura: crear `Tests/EditMode/Core/ClinicalResponderContract.cs`,
  `public abstract class ClinicalResponderContract` en `namespace NpcAi.Core.Tests`, con
  `protected abstract IClinicalResponder CreateSubject();` y un `private sealed class
  RespondedorNoListo : IClinicalResponder` local (patrón `ClasificadorNoListo`):
  `IsReady => false`, `AssignCase` no-op, `Respond => ClinicalResponse.NoAplica`.
- [x] 2.2 Escribir los `[Test]` no abstractos (nombres en español, estilo del repo):
  - `Reporta_si_esta_listo_sin_lanzar`
  - `Sin_caso_asignado_Respond_devuelve_NoAplica`
  - `Respond_no_lanza_en_ningun_estado` (utterance `default`, `IntentResult` `default`,
    texto `"!!!"`, cadena de 5000 chars)
  - `Cuando_responde_el_texto_no_es_vacio_y_los_tags_no_son_nulos`
  - `Es_determinista_en_Handled_y_en_el_texto_para_la_misma_entrada`
  - `AssignCase_es_idempotente_con_el_mismo_par`
  - `AssignCase_con_caso_desconocido_no_lanza_y_deja_no_listo`
- [x] 2.3 Confirmar que `ClinicalResponderContract` compila y **pasa contra
  `RespondedorNoListo`** en lo que aplica (las pruebas que exigen `Handled == true` usan
  `Assume.That(subject.IsReady)` para no fallar con el stub, igual que
  `ScenarioObjectiveContract.La_completitud_es_reversible` usa `Assume`).
  Estructura escrita y revisada por lectura; la base es `abstract` (NUnit no la instancia
  sola), asi que su ejecucion real ocurre cuando M15 la herede. **Verde de Test Runner
  EditMode pendiente de compuerta humana (tarea 3.1).**
- [x] 2.4 Ampliar `Tests/EditMode/Core/ContractTypeTests.cs` con los casos de la tarea del
  `design.md` → Testing Strategy para `ClinicalCaseId` y `ClinicalResponse`. No tocar
  ningún `[Test]` existente.

## Phase 3: Verificación de contrato (tareas de apply)

- [ ] 3.1 MANUAL (Editor de Unity): Test Runner → EditMode → Run All. En verde deben
  quedar `ContractTypeTests`, `ContractVersionChangelogTests` (con `Version == 2`),
  `CoreAssemblyPurityTests`, `EventChannelTests`, las 7 bases `*Contract` previas
  (sin cambios) y `ClinicalResponderContract` (contra el stub local).
  **Compuerta humana — ningún agente ejecuta Unity. Es el único bloqueo real de `sdd-verify`.**
- [x] 3.2 Confirmar por inspección del `.asmdef` que `NpcAi.Core` no ganó ninguna
  referencia. Verificado 2026-09-09: `Runtime/Core/NpcAi.Core.asmdef` tiene `references: []`
  y `noEngineReferences: true`; `git diff main -- Runtime/Core/NpcAi.Core.asmdef` es vacío.
- [x] 3.3 `git add` solo de la frontera de M0
  (`Runtime/Core/`, `Docs/CONTRACT-CHANGELOG.md`, `Tests/EditMode/Core/`,
  `openspec/changes/2026-09-09-m0-puerto-respuesta-clinica/`) y `git diff --cached`
  confirmando que no se cruza a otro módulo. Hecho: commit `ba329bb` en la rama
  `feat/m0-puerto-respuesta-clinica`, diff acotado a esas rutas.

## Después de `sdd-verify` (NO son tareas de apply — no bloquean verify)

Estos pasos van **después** de que `sdd-verify` pase, en orden: verify → checklist de
merge + co-revisión de M0 → merge → archive.

- Checklist "Antes de mergear" del `README.md`, **incluido el punto 7** (cambio de
  contrato: revisado antes del merge por el otro dueño de M0 — Jefferson — o el asesor).
- PR mergeado a `main` por el autor (regla 8), tras la co-revisión de M0.
- `sdd-archive`: mover el cambio a `openspec/changes/archive/`, promover
  `specs/respuesta-clinica-m0/spec.md` a `openspec/specs/respuesta-clinica-m0/spec.md`,
  dejar `archive-report.md`.
- Registrar en el documento de contexto del proyecto la decisión "Contrato v2 — puerto
  `IClinicalResponder` para M15" (regla 10). En Engram ya quedó como
  `sdd/2026-09-09-m0-puerto-respuesta-clinica/apply-progress`.

## Notas

- Este cambio **no entrega comportamiento**: entrega superficie de contrato. El valor real
  aparece cuando `2026-09-09-m15-respondedor-clinico` implementa el puerto.
- `2026-09-09-m14-catalogo-casos-clinicos` puede avanzar **en paralelo** con este cambio:
  M14 es dato y no depende del puerto, solo comparte el vocabulario `ClinicalCaseId`
  (el nombre de archivo de cada caso == su `ClinicalCaseId.Value`).
- Ningún agente ejecuta Unity ni sube `Contract.Version` de forma autónoma: cada verde de
  Test Runner y la co-revisión de M0 son compuertas humanas.
