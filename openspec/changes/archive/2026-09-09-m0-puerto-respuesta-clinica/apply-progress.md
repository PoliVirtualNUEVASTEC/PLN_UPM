# Apply-progress — 2026-09-09-m0-puerto-respuesta-clinica

**Fase:** apply (batch 1, unico) · **Modo:** Strict TDD (Unity Test Runner EditMode) ·
**Artifact store:** hybrid · **PR:** unico · **Rama:** `docs/land-m2-m6-changes-y-actualizacion-modules` (sin commit / sin push: gate humano)

Matiz del repo: ningun agente ejecuta Unity. Las pruebas RED se escribieron antes que la
implementacion GREEN, pero **el verde de EditMode es una compuerta humana**. Ningun
resultado de Test Runner esta inventado aqui.

## Estado de tareas

| Tarea | Estado | Nota |
|---|---|---|
| 0.1–0.6 (guardrails) | [x] leidas y respetadas | Cambio aditivo; frontera de escritura respetada; `noEngineReferences` intacto |
| 1.1 `ClinicalCaseId.cs` | [x] | Espejo estructural exacto de `PersonalityId.cs` |
| 1.2 `ClinicalResponse` en `Dtos.cs` | [x] | `readonly struct`, ctor 2 args, `static NoAplica`, doc-comment M15/enrutado |
| 1.3 `IClinicalResponder` en `Ports.cs` | [x] | Seccion nueva `// ---- Respuesta clinica` tras "Comprension"; doc-comments DEBE/NO DEBE completas |
| 1.4 `Contract.Version` 1→2 + `## v2` | [x] | En el mismo work-unit; `## v2` con tipos, invariantes y asimetria de determinismo |
| 1.5 Regla de integracion del changelog | [x] | Linea 4 reemplazada por la regla vigente de `openspec/config.yaml` → `rules.proposal` (redactada en espanol, alineada al `CLAUDE.md` del repo) |
| 2.1 `ClinicalResponderContract.cs` (base + stub) | [x] | `public abstract class`, `protected abstract IClinicalResponder CreateSubject()`, `private sealed class RespondedorNoListo` |
| 2.2 Los 7 `[Test]` en espanol | [x] | Nombres exactos del plan |
| 2.3 Compila + pasa contra `RespondedorNoListo` | [x] estructura / **pendiente verde humano** | Base `abstract`: NUnit no la instancia sola; corre cuando M15 la herede. Compilacion revisada por lectura (no se pudo invocar el compilador de Unity) |
| 2.4 Casos nuevos en `ContractTypeTests.cs` | [x] | 4 `[Test]` nuevos; ningun caso de enum/DTO de v1 tocado |
| 3.1 Test Runner EditMode Run All | [ ] **MANUAL — gate humano** | Verde requerido: `ContractTypeTests`, `ContractVersionChangelogTests` (Version==2), `CoreAssemblyPurityTests`, `EventChannelTests`, las 7 bases `*Contract` previas, `ClinicalResponderContract` (via M15 / stub) |
| 3.2 Confirmar `.asmdef` de `NpcAi.Core` sin refs nuevas | [ ] **MANUAL — gate humano** | `references: []`, `noEngineReferences: true` — no se toco el `.asmdef` |
| 4.1–4.5 Cierre (add / checklist / archive / PR / registro) | [ ] **MANUAL — autor** | Incluye co-revision de M0 (Jefferson o el asesor) antes del merge |

## TDD Cycle Evidence

| Tarea | Archivo de prueba | Capa | Safety Net | RED | GREEN | TRIANGULATE | REFACTOR |
|---|---|---|---|---|---|---|---|
| 1.1 `ClinicalCaseId` | `ContractTypeTests.cs` (3 `[Test]`) | Unit | N/A (tipo nuevo) | ✅ Escrito 1º (tipo inexistente ⇒ no compila) | ⏳ **gate humano** (por lectura: normaliza, `None` hashea 0, `==` Ordinal) | ✅ 3 casos: normalizacion / `None`+hash / igualdad Ordinal vs `caso-02` | ➖ Espejo de `PersonalityId`, ya limpio |
| 1.2 `ClinicalResponse` | `ContractTypeTests.cs` (1 `[Test]`) | Unit | N/A (tipo nuevo) | ✅ Escrito 1º | ⏳ **gate humano** (por lectura: `NoAplica.Handled == false`) | ➖ Estructural: un solo comportamiento observable | ➖ None needed |
| 1.3 `IClinicalResponder` | `ClinicalResponderContract.cs` (7 `[Test]`) | Unit | N/A (puerto nuevo) | ✅ Base escrita antes de que M15 exista | ⏳ **gate humano** (base `abstract`, corre via M15; `RespondedorNoListo` cubre el camino no-listo) | ✅ 7 escenarios: IsReady no lanza / no-listo⇒NoAplica / Respond no lanza (4 entradas) / handled⇒texto+tags / determinismo / idempotencia / caso desconocido | ➖ Estilo alineado a `IntentClassifierContract` + `ReceptivityEngineContract` |
| 1.4 `Version`/`## v2` | `ContractVersionChangelogTests.cs` (sin cambio) + `ContractTypeTests.Version_del_contrato_es_dos` | Unit | ⚠️ 1 test de v1 re-pinado (ver Desviaciones) | ✅ Assert `== 2` escrito antes del bump | ⏳ **gate humano** (por lectura: regex `^##\s+v(\d+)` ⇒ max 2 == `Contract.Version`) | ➖ Un unico valor posible | ➖ None needed |

## Work Unit Evidence

| Evidencia | Valor |
|---|---|
| Comando de prueba enfocado y resultado exacto | `Unity Test Runner > EditMode > Run All` (Window > General > Test Runner). **NO ejecutable por el agente en este repo.** Resultado exacto: PENDIENTE de compuerta humana (tarea 3.1). No se fabrica ningun resultado |
| Comando/escenario de harness de runtime y resultado exacto | N/A — este cambio entrega superficie de contrato (C# puro), sin frontera de runtime. El unico "runtime" es el compilador de Unity + NUnit EditMode, cubierto por la fila anterior |
| Frontera de rollback | Revertir este work-unit: `Contract.Version` vuelve a `1`; se borra la seccion `## v2` y se restaura la linea 4 del changelog; se borran `Runtime/Core/ClinicalCaseId.cs` y `Tests/EditMode/Core/ClinicalResponderContract.cs`; se quita `ClinicalResponse` de `Dtos.cs`, `IClinicalResponder` de `Ports.cs` y los 4 `[Test]` nuevos + el rename de version en `ContractTypeTests.cs`. Ningun modulo referencia `IClinicalResponder` todavia (M15/M11 sin mergear) |

## Archivos cambiados

| Archivo | Accion | Lineas (+/−) | Que se hizo |
|---|---|---|---|
| `Runtime/Core/ClinicalCaseId.cs` | Nuevo | +41 | `readonly struct ClinicalCaseId : IEquatable<>` — copia estructural de `PersonalityId` (normaliza `Trim().ToLowerInvariant()`, `None = default`, `IsNone`, `Equals`/`GetHashCode`/`==`/`!=` Ordinal, `ToString() => Value ?? "(none)"`). Doc-comment: catalogo = dato de M14 |
| `Runtime/Core/Dtos.cs` | Modificado (aditivo) | +23 / −0 | `+ readonly struct ClinicalResponse` (`bool Handled`, `NpcReply Reply`, ctor 2 args, `static NoAplica => new(false, default)`). Doc-comment: producido por M15; `Handled == false` ⇒ enrutar a M6 |
| `Runtime/Core/Ports.cs` | Modificado (aditivo) | +40 / −0 | `+ interface IClinicalResponder` (`IsReady`, `AssignCase(ClinicalCaseId, PersonalityId)`, `Respond(Utterance, IntentResult)`) con doc-comments DEBE/NO DEBE completas, en seccion nueva `// ---- Respuesta clinica` |
| `Runtime/Core/Contract.cs` | Modificado | +1 / −1 | `Version` `1` → `2` |
| `Docs/CONTRACT-CHANGELOG.md` | Modificado | +53 / −1 | `+ ## v2` (tipos nuevos, invariantes DEBE/NO DEBE del puerto, entrada nueva de "Asimetria de determinismo"); linea 4 (regla de integracion) reescrita a la regla vigente |
| `Tests/EditMode/Core/ClinicalResponderContract.cs` | Nuevo | +144 | Base `abstract` + `RespondedorNoListo` local + 7 `[Test]` no abstractos; las que exigen `Handled == true` usan `Assume.That(IsReady)` |
| `Tests/EditMode/Core/ContractTypeTests.cs` | Modificado | +41 / −2 | +4 `[Test]` (`ClinicalCaseId` x3, `ClinicalResponse` x1); `Version_del_contrato_es_uno` → `Version_del_contrato_es_dos` (assert `== 2`) |
| `openspec/changes/.../tasks.md` | Modificado | +18 / −15 | Checkboxes Fase 0/1/2 marcados; nota en 2.3 |

**Diff real de contrato/codigo (sin `tasks.md` ni `apply-progress.md`):** +343 / −4  ≈ **347 lineas cambiadas.**

## Desviaciones respecto al plan

1. **`ContractTypeTests.Version_del_contrato_es_uno` fue re-pinado a `_es_dos` (assert `== 2`).**
   El plan dice "no tocar ningun `[Test]` existente" y "ampliar, no reescribir". Pero la spec
   exige de forma dura `Contract.Version` `1 → 2`, lo que **invalida directamente** ese pin
   literal (`Assert.AreEqual(1, Contract.Version)`). Se trato como test de aprobacion cuyo
   comportamiento esperado debe cambiar (patron de `strict-tdd.md`): assert `== 2` escrito
   antes del bump (RED), luego el bump (GREEN). Consistente con el Success Criteria de la
   propuesta, que acota la prohibicion a "ningun caso existente **de enums/DTO v1**" — y este
   no es un caso de enum/DTO. Ningun test de invariante de enum/DTO de v1 se toco.
   `ContractVersionChangelogTests` (no tocado) ata la version al `## v2` de forma
   independiente.
2. **`ClinicalResponderContract` es `abstract` y no se auto-ejecuta.** NUnit no instancia
   fixtures abstractos, asi que en este cambio la base solo **compila**; sus `[Test]` corren
   cuando M15 la herede (`ClinicalResponderTests` / `ScriptedClinicalResponderTests`, ver
   `openspec/changes/2026-09-09-m15-respondedor-clinico/design.md`). El stub
   `RespondedorNoListo` mantiene auto-contenidas las aserciones del camino "no listo".
   Coherente con `IntentClassifierContract` (tambien abstracto, con `ClasificadorNoListo`
   inline).

## Riesgos

- **Forecast de lineas superado ~2.7x.** `tasks.md` estimo ~130 lineas; el diff real de
  contrato/codigo es ~347. Causa: el `## v2` del changelog espeja el estilo detallado del
  `## v1`, y la base de prueba (7 `[Test]` + stub + helpers, ~144 lineas) esta en el rango de
  `ReceptivityEngineContract` (~130) y `SpeechToTextContract` (~120). Sigue **muy por debajo**
  del presupuesto de revision de 800 lineas. Se registra como discrepancia de forecast, no
  como sobre-escritura: no hay contenido de relleno.
- **`--max-changed-lines 220` del ledger de intento superado.** El objetivo del intento se
  fijo con el forecast optimista. El trabajo es minimo para satisfacer los 8 requisitos y 18
  escenarios de la spec + los 11 invariantes DEBE/NO DEBE obligatorios del prompt.
- **`CasoCualquiera = new ClinicalCaseId("caso-01")`** en la base de prueba: el doble y la
  impl real de M15 deben reconocer ese id para que corran las pruebas dependientes de
  `IsReady`. Si no, `Assume` las omite (no fallan). `ScriptedClinicalResponder` de M15
  ("listo con cualquier id no `None`") las correra; `ClinicalResponderTests` real debe armar
  su loader de prueba para conocer `"caso-01"`. Mismo patron que `ReceptivityEngineContract.Cualquiera`.
- **`.meta` de los 2 archivos nuevos**: los genera Unity al reimportar (tarea 3.x). No se
  crearon a mano.

## Pendiente para el humano

1. Abrir el proyecto Unity host y reimportar el paquete → genera
   `ClinicalCaseId.cs.meta` y `ClinicalResponderContract.cs.meta`.
2. **Test Runner → EditMode → Run All** y confirmar verde de: `ContractTypeTests` (incl.
   `Version_del_contrato_es_dos` y los 4 casos nuevos), `ContractVersionChangelogTests`
   (`Version == 2` ↔ `## v2`), `CoreAssemblyPurityTests`, `EventChannelTests`, las 7 bases
   `*Contract` previas sin cambios. `ClinicalResponderContract` no aparece como fixture
   propio hasta que M15 la herede.
3. Confirmar por inspeccion del `.asmdef` que `NpcAi.Core` no gano referencias
   (`references: []`, `noEngineReferences: true`).
4. **Co-revision de M0** (regla 3): Jefferson o el asesor revisan el delta de contrato antes
   del merge. Sin ventana fija, sin quorum.
5. `git add` solo de `Runtime/Core/`, `Docs/CONTRACT-CHANGELOG.md`, `Tests/EditMode/Core/` y
   `openspec/changes/2026-09-09-m0-puerto-respuesta-clinica/`; `git diff --cached` para
   confirmar que no se cruza a otro modulo. Commit + PR unico a `main` (regla 8), tras la
   co-revision.
6. Al archivar: crear `openspec/specs/respuesta-clinica-m0/spec.md` y `archive-report.md`;
   registrar en el doc de contexto del proyecto y en Engram "Contrato v2 — puerto
   `IClinicalResponder` para M15" (regla 10).

## Siguiente fase recomendada

`sdd-verify` — todas las tareas de autoria (Fase 0/1/2) estan completas; quedan solo
compuertas humanas (Test Runner + co-revision M0 + commit/PR).
