# Tasks: M0 — Puerto de requerimientos de sala de juntas (`IRequirementResponder`)

## Review Workload Forecast

| Field | Value |
|-------|-------|
| Estimated changed lines | PR1 ~195-215 · PR2 ~370-410 (al borde del presupuesto) |
| 400-line budget risk | High si fuera 1 PR (~570-625 total); PR1 Low, PR2 Medium-High |
| Chained PRs recommended | Yes |
| Suggested split | PR1 tipos+`ContractTypeTests` → PR2 puerto+contrato compartido+`Version`/changelog (rama sobre PR1) |
| Delivery strategy | ask-on-risk |
| Chain strategy | stacked-to-main |

Decision needed before apply: Yes
Chained PRs recommended: Yes
Chain strategy: stacked-to-main (2 PRs, sin rama tracker: PR1 → `main`, PR2 → rama de PR1)
400-line budget risk: High

Recalculado contra código real, no solo el ~250-300L de la propuesta: `ClinicalResponderContract.cs`
(precedente, 145L/7 tests) escala a 10 tests+2 stubs+subclase (~230-250L); el changelog `## v2`
(~72L/3 tipos) escala a 5 tipos nuevos (~90-100L). Si PR2 supera 400L al implementar, partir 2.1/2.3
(contrato compartido) en un PR2b encadenado, dejando 2.2/2.4 (puerto+version) en PR2.

### Suggested Work Units

| Unit | Goal | PR | Focused test | Runtime harness | Rollback boundary |
|---|---|---|---|---|---|
| 1 | Identificadores+enum+DTO+`ContractTypeTests` | PR1 | Test Runner, filtro `ContractTypeTests` | N/A — C# puro, sin escena/VR | Borra 2 archivos nuevos + revierte 2 hunks aditivos; sin dependientes |
| 2 | Puerto+`RequirementResponderContract`+`Version`/changelog | PR2 (base=PR1) | Test Runner, filtro `RequirementResponderContract\|ContractVersionChangelogTests` | N/A — C# puro, sin escena/VR | Revierte `Version` a `2`, borra `## v3` y el puerto; nadie lo referencia aún |

## Phase 1: Tipos y DTO (PR1)

- [x] 1.1 RED en `Tests/EditMode/Core/ContractTypeTests.cs`: 8 casos nuevos (`RequirementCaseId`×3, `RequirementId`×3, `RequirementResponse.NoAplica`, congelado `RequirementOutcome`); no compila hasta 1.2-1.5.
- [x] 1.2 GREEN — crear `Runtime/Core/RequirementCaseId.cs` (+`.meta`), copia de `ClinicalCaseId.cs`.
- [x] 1.3 GREEN — crear `Runtime/Core/RequirementId.cs` (+`.meta`), mismo molde.
- [x] 1.4 GREEN — `Runtime/Core/Enums.cs`: `+ enum RequirementOutcome{NoAplica=0,AunNoRevelado=1,Revelado=2}` al final.
- [x] 1.5 GREEN — `Runtime/Core/Dtos.cs`: `+ struct RequirementResponse` (`Outcome`, `Reply`, `RequirementId`, estático `NoAplica`).
- [x] 1.6 Verify — código escrito y revisado por lectura; `Contract.Version` sigue en `2` (confirmado, no se tocó). **Confirmado por Jefferson (2026-09-17): Test Runner EditMode corrido en el Editor, todos los tests en verde.**

1.2-1.5 son paralelizables entre sí; dependen de 1.1 solo como forma esperada, no de orden de compilación.

## Phase 2: Puerto y contrato compartido (PR2, rama sobre PR1 — depende de Fase 1)

- [x] 2.1 RED — crear `Tests/EditMode/Core/RequirementResponderContract.cs`: clase abstracta, 10 `[Test]`, 2 stubs (`RespondedorDeRequerimientosNoListo`, `RespondedorDeRequerimientosDePrueba`), subclase concreta `RequirementResponderContractStubTests`; no compila (`IRequirementResponder` no existe).
- [x] 2.2 GREEN — `Runtime/Core/Ports.cs`: `+ interface IRequirementResponder` (`IsReady`, `AssignCase`, `Respond`) en sección nueva tras "Respuesta clinica". Depende de 2.1.
- [x] 2.3 Verify — los 10 `[Test]` de `RequirementResponderContractStubTests` escritos y revisados por lectura contra el precedente compilado (`IClinicalResponder`/`ClinicalResponderContract`); **verde en Unity aun no confirmado por Jefferson** (mismo patron que PR1: sin Editor/CLI en esta sesion).
- [x] 2.4 Commit atómico — `Runtime/Core/Contract.cs` `Version` `2→3`; `+ ## v3` en `Docs/CONTRACT-CHANGELOG.md`; renombrar `Version_del_contrato_es_dos`→`Version_del_contrato_es_tres` (valor `3`) en `ContractTypeTests.cs`. Commiteado en `feat/m0-requerimientos-puerto` (commit `c542e96`), 145 líneas, dentro del presupuesto — la clase de contrato compartida se separó en PR2b (ver abajo).
- [x] 2.5 Verify — diff de PR2 acotado a `Runtime/Core/Ports.cs`, `Runtime/Core/Contract.cs`, `Docs/CONTRACT-CHANGELOG.md`, `Tests/EditMode/Core/ContractTypeTests.cs` (solo el pin). Verde en Unity aún pendiente de que Jefferson corra el Test Runner (mismo patrón que PR1).

### ALERTA DE PRESUPUESTO — PR2 midio mas de 400 lineas reales

Medido con `git diff --stat` sobre los archivos que este cambio (PR2) toco, **excluyendo** los
archivos de PR1 (`RequirementCaseId.cs`, `RequirementId.cs`, `Enums.cs`, `Dtos.cs`, y las 8
pruebas nuevas ya existentes en `ContractTypeTests.cs`):

| Archivo | Lineas cambiadas (add+del) |
|---|---|
| `Runtime/Core/Ports.cs` | 45 (45 add) |
| `Runtime/Core/Contract.cs` | 2 (1 add + 1 del) |
| `Docs/CONTRACT-CHANGELOG.md` | 96 (96 add) |
| `Tests/EditMode/Core/ContractTypeTests.cs` (solo el renombre del pin, sin las 8 pruebas de PR1) | 8 (4 add + 4 del) |
| `Tests/EditMode/Core/RequirementResponderContract.cs` (archivo nuevo) | 260 (260 add) |
| `Tests/EditMode/Core/RequirementResponderContract.cs.meta` (archivo nuevo) | 2 (2 add) |
| **Total PR2** | **413** |

413 > 400 (presupuesto de revision). **Resuelto (2026-09-17): Jefferson confirmó el corte de
contingencia PR2b.** Rama final: `feat/m0-requerimientos-tipos` (PR1, 203L + 951L de docs
OpenSpec) → `feat/m0-requerimientos-puerto` (PR2, 145L, puerto+`Version`/changelog/pin) →
`feat/m0-requerimientos-contrato` (PR2b, 262L, `RequirementResponderContract`). Los tres
commits ya existen localmente, cada uno en su propia rama apilada — nada mergeado a `main`
ni pusheado todavía.

## Phase 3: Cierre

- [x] 3.1 Test Runner EditMode completo (Window > General > Test Runner) sobre PR2/PR2b — **confirmado por Jefferson (2026-09-17): todo en verde**, incluidas `ContractVersionChangelogTests` y `RequirementResponderContractStubTests`.
- [x] 3.2 Co-revisión de M0 — **APROBADA por Luis Miguel Cañaveral Restrepo** por comentario en los PR #39/#40/#41 (2026-09-17). Nota: el PR de cierre #42 (necesario porque #40/#41 se mergearon dentro de sus ramas base, no a `main`) trae el mismo código ya aprobado y no recibió comentario propio — Jefferson decidió que la aprobación ya dada alcanza, sin pedir una re-revisión puntual de #42.
- [x] 3.3 Push de las 3 ramas y PRs abiertos: #39, #40, #41 (encadenados) + #42 (cierre real hacia `main`). Los 4 mergeados; `main` verificado con el contenido completo.

Nota: `Docs/MODULES.md` NO se toca — `design.md` no lo pide, y el precedente v2 (`respuesta-clinica-m0`) tampoco lo actualizó (sigue diciendo "7 puertos"/`Version` `1`); es deuda preexistente, fuera de alcance de este cambio.
