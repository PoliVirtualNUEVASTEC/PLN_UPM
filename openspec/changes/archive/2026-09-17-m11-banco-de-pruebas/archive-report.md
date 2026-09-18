# Archive Report: banco-de-pruebas-m11

**Change**: 2026-09-17-m11-banco-de-pruebas
**Archived**: 2026-09-18
**Status**: PASS WITH WARNINGS - ARCHIVED
**Artifact Store Mode**: hybrid (OpenSpec filesystem + Engram)

## Executive Summary

El cambio `2026-09-17-m11-banco-de-pruebas` (PR1: `NpcAi.Harness.SessionDirector`) queda archivado. Entrega la raíz de composición del escenario de emergencia: una clase C# pura que elige, secuencia, enruta y delega entre cinco puertos de M0 congelado (M2, M4, M6, M9, M15). La spec nueva `banco-de-pruebas-m11` (7 requisitos, 12 escenarios) se promocionó a `openspec/specs/banco-de-pruebas-m11/spec.md`. La carpeta del cambio se movió a `openspec/changes/archive/2026-09-17-m11-banco-de-pruebas/`. Verificación: **PASS WITH WARNINGS** (0 CRITICAL, 8 WARNING, 10 SUGGESTION). El usuario confirmó en verde la suite EditMode en Unity Test Runner (2026-09-18) y confirmó después que el total fue de 26 pruebas, coincidente con el conteo estático de 26 métodos [Test] (W1 cerrado). PR #44 mergeado a `5b78ce7` el 2026-09-18T16:12:38Z. Las tareas 19/19 están cerradas; el ciclo SDD completo puede proseguir. Cero cambio de contrato M0 (no aplica gobernanza de la regla dura 2).

## Final-State Authority Ranking

1. **Native review authority** — Sin review gate descubierto para este candidato: los artefactos de review del status nativo figuran como missing.
2. **Persisted tasks artifact** — `tasks.md`: 19/19 `[x]` (todas las tareas, incluyendo 5.1 compuerta humana confirmada en verde).
3. **Explicit final-state facts** — Según el prompt del archivo executor:
   - PR #44 mergeado en `5b78ce7` (2026-09-18T16:12:38Z), rama `feat/m11-01-session-director` tree-idéntica a `origin/main`.
   - Tarea 5.1 confirmada en verde por el usuario el 2026-09-18; el usuario confirmó después el total: 26 pruebas (W1 cerrado).
   - Commit `a4118f8` agrega los 9 `.meta` faltantes.
   - Verify verdict: `pass_with_warnings`, 0 CRITICAL, 8 WARNING (W1-W8), 10 SUGGESTION (S1-S10).
4. **Intermediate snapshots** — `apply-progress.md` (17/19 al cierre de apply), `verify-report.md` (264 líneas, SHA256 `9a90c998c6dffd4828b22fb47b394b0a74b531b3ea11c4b0f552095230cb4b79`), Engram #99 (`sdd/2026-09-17-m11-banco-de-pruebas/verify-report`). **Autoridad final**: los hechos del prompt, que prevalen sobre snapshots stale.

## Artifact Inventory

### Merged Specs

| Domain | File Path | Status | Details |
|--------|-----------|--------|---------|
| banco-de-pruebas-m11 | `openspec/specs/banco-de-pruebas-m11/spec.md` | Created | 7 requisitos, 12 escenarios; capacidad nueva (sin spec previa que reconciliar); `(propuesto)` removido x7 en traceability table |

**Merge Strategy**: Capacidad nueva. `cp` mecánico + edición (7x removidas ` (propuesto)`) + `diff` verificado (0 de propuesto, 7 requisitos, 12 escenarios). Convive con `contrato-nucleo-m0` y `canales-evento-nucleo-m0`; NO redefine ningún puerto ni DTO.

**Verificación de copia**: grep confirma 7 requisitos, 12 escenarios, cero `(propuesto)`.

### Archived Change Folder

**Location**: `openspec/changes/archive/2026-09-17-m11-banco-de-pruebas/`

**Contents**:
- `proposal.md` — Intent ("sistema completo por módulo y totalmente inerte"), scope in/out (M10/M16/M9↔M15 gap explícitamente OUT), approach (delegados hacia M9, centinelas `0`, selección con semilla, reparto Runtime/Samples~, orden de inicio), riesgos (cableado de canales, brecha de índices, module-dios, dependencia de UI Quest 3), decisiones del usuario 2026-09-17 (solo emergencia scenario, sin gap M9↔M15).
- `design.md` — 13 decisiones arquitectónicas (AD1-AD13), grafo de referencias (Harness solo refs Core), superficie pública (1 tipo: SessionDirector), pseudocódigo (constructor, selección, arranque, turno, DeclararTriaje), flujo de datos, inventario de 7 archivos runtime+test, estrategia de prueba Strict TDD (5 espías locales + 2 compartidos), Open Questions.
- `tasks.md` — 19 tareas (0.1-6.3); 19/19 `[x]` al cierre del cambio (17/19 al terminar apply; 5.1 confirmada por el usuario; 6.3 en `a4118f8`).
- `specs/banco-de-pruebas-m11/spec.md` — 7 ADDED Requirements, 12 Scenarios, tabla de trazabilidad.
- `verify-report.md` — 264 líneas; verdict `pass_with_warnings`; 0 CRITICAL; matriz de cumplimiento (12/12 escenarios con prueba cubriente, atestada en verde); 8 WARNING (W1-W8), 10 SUGGESTION (S1-S10).
- `apply-progress.md` — Snapshot de Fase 0-4 y 6.1; ciclo TDD real RED→GREEN (16 → 22 → 26 pruebas) sobre arnés `dotnet test` fuera de repo; 821 líneas reales de código nuevo (dentro de ~750-900 presupuestado).

**Verificación de movimiento**: carpeta origen `openspec/changes/2026-09-17-m11-banco-de-pruebas/` confirmada inexistente tras `mv`; sibling `.meta` también movido a archive.

## Verification Outcome (Final State)

**Verdict**: PASS WITH WARNINGS

### Requirement Coverage

| Metric | Count |
|--------|-------|
| Total Requirements | 7 |
| Covered Requirements | 7/7 |
| Total Scenarios | 12 |
| Escenarios con prueba cubriente (verde atestado, no re-observado) | 12/12 |
| Critical Findings | 0 |
| Blockers | 0 |

### Test Execution

**Runtime GREEN** (compuerta humana, autoridad de estado final):
- Usuario corrió Unity Test Runner > EditMode > Run All el 2026-09-18.
- Suite `NpcAi.Harness.Tests`: 26 métodos `[Test]` (conteo estático); el usuario atestó "todo en verde" y confirmó que el total fue de 26 pruebas.
  - `SessionDirectorSesionTests`: 16.
  - `SessionDirectorTurnoTests`: 6.
  - `SessionDirectorFronterasTests`: 4.
- **Nota**: el usuario confirmó después que el total fue de 26 pruebas y quedó registrado en este informe y en `apply-progress.md` (W1 cerrado). Sigue siendo atestación del usuario: esta verificación no re-observó la ejecución.

**Agent-side**:
- `dotnet test` fuera de repo (scratchpad NUnit 3.14.0): 26/26 en verde, 0 fallidas, ~51 ms (evidencia complementaria, no sustitución de Unity Test Runner).
- `git diff --cached --stat`: diff limitado a `Runtime/Harness/`, `Tests/EditMode/Harness/`, `openspec/changes/2026-09-17-m11-banco-de-pruebas/` (confirmado antes de commit).

### Findings Disposition

**CRITICAL**: Ninguno.

**WARNING (8 total)**:

| # | Finding | Disposition | Status |
|---|---------|-------------|--------|
| W1 | Evidencia de ejecución solo atestada, sin total registrado | Resuelto: el usuario confirmó que el total fue de 26 pruebas; registrado en este informe y en `apply-progress.md` | Resuelto |
| W2 | apply-progress.md desactualizados (17/19 stale) | Resuelto: paso 3 del archivo, sección "Estado final al cierre" | Resuelto |
| W3 | AD10 (m9.Notify) sin prueba cubriente | Seguimiento (huecos de cobertura, no defecto conocido) | Abierto |
| W4 | Flujo Classify → IntentResult no afirmado | Seguimiento (candidato a pequeño cambio de solo pruebas en Harness) | Abierto |
| W5 | Turno_social no discrimina m4.Current de constante | Seguimiento (idem) | Abierto |
| W6 | Pruebas de fronteras vacías o decorativas | Seguimiento (idem) | Abierto |
| W7 | Regla dura 4 (Fakes/) sin justificación explícita | Abierto (decisión de dueños del módulo: registrar excepción o en adenda PR2, o en Docs/MODULES.md, o en config.yaml) | Abierto |
| W8 | Engram: spec/proposal bajo clave no canónica | Resuelto: paso 8 del archivo (re-save bajo claves canónicas) | Resuelto |

**SUGGESTION (10 total)**: Afirmación completa de respuesta clínica (S1), cobertura de EspiaClinico.UltimaPersonalidad (S2), AD5/AD3 sin prueba (S3), triangulación de PhysicalAction (S4), orden de efectos no fijado (S5), documentación (tasks.md 6.2 actualizado en paso 2; apply-progress.md "Rollback boundary" mención) (S6), precedente NpcAi.Harness.Tests (S7), etiqueta "(propuesto)" en spec.md removida (paso 5) (S8), columnas TDD (S9), politica uniforme de `.meta` (S10). Todos documentados en `verify-report.md` líneas 243-254.

## Runtime Changes

**Nuevo en Runtime**:
- `Runtime/Harness/NpcAi.Harness.asmdef` — `references: ["NpcAi.Core"]`, `noEngineReferences: true` (AD12).
- `Runtime/Harness/SessionDirector.cs` — 166 líneas, clase `sealed` con constructor (10 parámetros, validación AD3), `IniciarSesion()` x2 (AD5), `SiguienteCaso()` (lista filtrada AD7), `Arrancar()` (AD8), `ProcesarTurno()` (AD9/AD10), `ProcesarAccion()` (centinela, AD10), `DeclararTriaje()`, propiedades `CasoActual`/`PersonalidadActual`/`Progreso`.

**Nuevo en Tests**:
- `Tests/EditMode/Harness/NpcAi.Harness.Tests.asmdef` — refs a `NpcAi.Core`, `NpcAi.Harness`, `NpcAi.ClinicalResponse`, `NpcAi.Presentation`, TestRunner; solo Editor.
- `Tests/EditMode/Harness/EspiasDeArnes.cs` — 156 líneas, 5 espías locales `internal sealed` (no en Fakes/, conforme a AD11).
- `Tests/EditMode/Harness/SessionDirector*Tests.cs` — 3 archivos, 26 métodos, 456 líneas totales.

**Total nuevo**: 821 líneas en 7 archivos (2 `.asmdef` + 5 `.cs`), dentro de ~750-900 presupuestado.

**Fuera de alcance (Docs/MODULES.md M11 section)**: Diferido explícitamente a PR2 (decision en proposal.md).

**Write boundary**: todo bajo `openspec/changes/2026-09-17-m11-banco-de-pruebas/` (ahora archive). Cero modificación de `Runtime/Core/`, `Runtime/CoreChannels/`, otro `Runtime/<Modulo>/`. **No es cambio de contrato**: sin gate de gobernanza previo.

## Task Completion Gate

**Status**: PASS (19/19 tareas cerradas)

| Fase | Checkboxes | Estado | Nota |
|------|-----------|--------|------|
| 0. Guardrails | 0.1-0.4 | `[x]` | Frontera confirmada; no es cambio de contrato; `.meta` generados después (6.3) |
| 1. Foundation | 1.1-1.3 | `[x]` | Ambos `.asmdef` + 5 espías |
| 2. TDD — Constructor/arranque/selección | 2.1-2.4 | `[x]` | 16 pruebas en verde, RED→GREEN real |
| 3. TDD — Turno/centinelas/enrutado | 3.1-3.2 | `[x]` | 6 pruebas en verde, 22 totales |
| 4. TDD — Fronteras | 4.1-4.2 | `[x]` | 4 pruebas en verde, 26 totales |
| 5. Compuerta humana | 5.1 | `[x]` | Usuario confirmó en verde; total confirmado: 26 pruebas (W1 cerrado) |
| 6. Cierre | 6.1-6.3 | `[x]` | Diff acotado; `.meta` en a4118f8 |

## Edits Made Before Archive (Steps 2-5)

1. **tasks.md, tarea 6.2**: Reemplazado `(pendiente de verificar justo antes de abrir el PR)` con `(verificado al abrir el PR #44: 0 commits detras de main, 7 adelante)`.
2. **apply-progress.md**: Agregada sección "## Estado final al cierre (2026-09-18)" confirmando 19/19, compuerta humana confirmada, `.meta` en a4118f8, PR #44 mergeado, snapshot histórico.
3. **PENDIENTES-SIGUIENTE-CHAT.md**: Insertada nota de cierre al tope indicando cambio archivado, verify PASS_WITH_WARNINGS, no actuar en pendientes.
4. **Promoted spec**: `openspec/specs/banco-de-pruebas-m11/spec.md` creado, byte-idéntico excepto 7x `(propuesto)` removidas de tabla de trazabilidad.
5. **Folder moved**: `openspec/changes/2026-09-17-m11-banco-de-pruebas/` → archive, sibling `.meta` también movido.

## Deliverables

### openspec/specs/banco-de-pruebas-m11/spec.md

**7 Requisitos ADDED**:
1. Centinelas para señales parciales
2. Silencio de la acción física sola
3. Enrutado clínico/social
4. Secuencia de inicio de sesión
5. Selección reproducible por semilla
6. Fronteras que SessionDirector no cruza
7. SessionDirector es C# puro

**12 Escenarios**: todos con prueba EditMode cubriente (verde atestado por el usuario) (ver tabla de trazabilidad al final de spec.md).

## Archiving Decisions

- **Merge Strategy**: Capacidad nueva. `cp` mecánico + edición (`(propuesto)` removido) + `diff` verificado → PASS.
- **Nombre de carpeta**: formato ISO `2026-09-17-m11-banco-de-pruebas`.
- **Integridad**: copia de spec `grep` verificado; movimiento de carpeta, origen inexistente + sibling `.meta` movido → PASS.
- **Clave Engram**: re-save bajo claves canónicas (`sdd/2026-09-17-m11-banco-de-pruebas/proposal`, `spec`, `archive-report`), no bajo claves stale (`sdd/m11-banco-de-pruebas/*`).

## Verification Evidence

- **verify-report.md**: ubicación `openspec/changes/archive/2026-09-17-m11-banco-de-pruebas/verify-report.md`, SHA256 `9a90c998c6dffd4828b22fb47b394b0a74b531b3ea11c4b0f552095230cb4b79`, Engram #99.
- **Engram observations**: #88 (explore, stale key), #90 (proposal, stale key), #91 (spec, stale key), #92 (design), #94 (tasks), #95 (apply-progress). Nuevas observaciones bajo claves canónicas en paso 8.
- **PR URL**: https://github.com/PoliVirtualNUEVASTEC/PLN_UPM/pull/44 (merged into `5b78ce7`).

## Next Steps

### Inmediato (orquestador / humano)

1. ~~`sdd-verify`~~ — ya ejecutado, verdict `pass_with_warnings` en `verify-report.md`.
2. ~~`sdd-archive`~~ — en ejecución (este informe).

### Residual (no bloquea archivo, futuros chats)

- ~~**W1**~~: cerrado; el usuario confirmó que el total fue de 26 pruebas (registrado en este informe y en `apply-progress.md`).
- **W3-W6**: Pequeño cambio de solo pruebas en `Tests/EditMode/Harness/` (no toca `SessionDirector.cs`).
- **W7**: Dueños del módulo deciden cómo registrar la excepción de la regla dura 4 (Fakes/).
- **PR2**: `Samples~/Harness/`, `HarnessBehaviourWiringTests`, README, `Docs/MODULES.md` M11. Ciclo SDD separado (proposal+spec+design abreviados, dado que arquitectura ya fijada por PR1).

## Conclusion

**El cambio 2026-09-17-m11-banco-de-pruebas (PR1) está archivado y el ciclo SDD está completo.** M11 (banco de pruebas) queda instrumentado con una raíz de composición funcional (`SessionDirector`) y una spec trazada a 26 pruebas EditMode (verde atestado por el usuario; total confirmado: 26). La capacidad nueva `banco-de-pruebas-m11` vive en `openspec/specs/banco-de-pruebas-m11/spec.md`. Cero cambio de contrato M0. Los 8 WARNING y 10 SUGGESTION quedan registrados como seguimiento y como precedente para futuros cambios. Queda pendiente PR2 (escena, cáscara, documentación) en un ciclo SDD separado.
