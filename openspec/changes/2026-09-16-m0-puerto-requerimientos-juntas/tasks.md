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
- [ ] 2.4 Commit atómico — `Runtime/Core/Contract.cs` `Version` `2→3`; `+ ## v3` en `Docs/CONTRACT-CHANGELOG.md`; renombrar `Version_del_contrato_es_dos`→`Version_del_contrato_es_tres` (valor `3`) en `ContractTypeTests.cs`. **Código ya escrito en el working tree** (los 3 cambios ya estan hechos), pero el `git commit` real queda EN PAUSA: ver "Alerta de presupuesto" abajo — depende de que Jefferson confirme como agrupar el commit/PR antes de crearlo.
- [ ] 2.5 Verify — `ContractVersionChangelogTests` en verde; diff acotado a `Runtime/Core/`, `Docs/CONTRACT-CHANGELOG.md`, `Tests/EditMode/Core/`, `openspec/`. **Bloqueado** por la alerta de presupuesto: el diff real mide mas de 400 lineas (ver abajo), asi que el alcance final del commit/PR aun no esta confirmado.

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

413 > 400 (presupuesto de revision). El codigo esta completo y razonado en verde (2.1-2.3
listos; 2.4 escrito en el working tree pero sin `git commit`). **NO se forzo en un solo commit
y NO se implemento el corte PR2b por cuenta propia**, tal como instruyo Jefferson. Opciones
para que Jefferson confirme antes de continuar a 2.4 (el commit real)/2.5/Fase 3:

1. **Corte de contingencia PR2b** (el que ya sugeria esta tabla arriba): separar
   `RequirementResponderContract.cs` (+.meta) — 262 lineas, la mayor parte del exceso — en un
   PR2b encadenado sobre PR2, dejando en PR2 solo el puerto (`Ports.cs`, 45L) + el commit
   atomico de version/changelog/pin (`Contract.cs` + `CONTRACT-CHANGELOG.md` + pin en
   `ContractTypeTests.cs`, 106L) = **151L en PR2**, muy por debajo del presupuesto. PR2b
   quedaria en 262L, tambien debajo de 400.
2. **`size:exception`**: aceptar 413L en un solo PR2, dejando constancia explicita de la
   excepcion (igual que permite la guia de presupuesto de revision).
3. Otra particion que Jefferson prefiera.

Sin esa confirmacion, el agente de apply se detiene aqui: no crea el commit atomico de 2.4 ni
continua a 2.5 ni a la Fase 3.

## Phase 3: Cierre

- [ ] 3.1 Test Runner EditMode completo (Window > General > Test Runner); confirmar 0 `Debug.Log` en lo agregado.
- [ ] 3.2 Enviar a co-revisión de M0 (Luis Miguel Cañaveral Restrepo o el asesor Luis Fernando González Alvarán) antes de mergear.

Nota: `Docs/MODULES.md` NO se toca — `design.md` no lo pide, y el precedente v2 (`respuesta-clinica-m0`) tampoco lo actualizó (sigue diciendo "7 puertos"/`Version` `1`); es deuda preexistente, fuera de alcance de este cambio.
