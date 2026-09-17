# Tasks: M9 — Escenario Sala de Emergencia (implementación real de `IScenarioObjective`)

## Review Workload Forecast

| Field | Value |
|---|---|
| Estimated changed lines | ~840-950 (runtime+tests ~770-870, `Docs/MODULES.md` ~50-70); spec.md al archivar (~205) excluido del riesgo, copia idéntica ya revisada, mismo trato que M7 PR4 |
| 400-line budget risk | Alto frente al default del skill (400); frente al presupuesto de sesión (800) es más ajustado: el rango cruza 800, no queda cómodamente debajo |
| Chained PRs recommended | Sí (sugerido, no forzado por hábito — ver nota) |
| Suggested split | 3 unidades ligeras: lector → objetivo+superficie aditiva → docs/cierre; alternativa: `size:exception` en un solo PR si el equipo acepta el excedente moderado |
| Delivery strategy | ask-on-risk |
| Chain strategy | pending — el usuario debe elegir |

Decision needed before apply: Yes
Chained PRs recommended: No — decidido con el usuario 2026-09-17: un solo PR con `size:exception`
aceptado explícitamente, dado el excedente moderado (~840-950 vs. 800) y que el módulo es una
unidad cohesiva (una clase + su lector + su config, todo relacionado).
Chain strategy: no aplica (un solo PR)
400-line budget risk: High frente al default de 400 del skill; aceptado como size:exception frente
al presupuesto de sesión de 800

**A diferencia de M7** (~1200-1400 líneas, 50%+ sobre cualquier presupuesto): M9 no tiene capa de
envoltura `MonoBehaviour`, así que el excedente es más chico. No califica como "cómodamente bajo
800": el estimado cruza esa línea, así que se marca el riesgo en vez de aceptar un PR único en
silencio. La decisión de cadena vs. `size:exception` queda para el usuario/orquestador.

### Suggested Work Units

| Unit | Goal | PR | Focused test command | Runtime harness | Rollback boundary |
|---|---|---|---|---|---|
| 1 | Lector de `clave`: `TriageKey`, `TriageKeyLoader`, `TriageObjectiveSettings` (~295 líneas) | PR1 | EditMode: `TriageKeyLoaderTests` | N/A — C# puro, JSON en memoria, sin escena | Borrar los 3 archivos + su prueba; nada más depende de ellos todavía |
| 2 | `TriageScenarioObjective`: contrato heredado + mezcla + superficie aditiva (~545 líneas) | PR2 | EditMode: `TriageScenarioObjectiveTests` + `TriageProgresoTests` + `TriageSuperficieAditivaTests` | N/A — C# puro, sin escena/VR/audio | Borrar `TriageScenarioObjective.cs` + sus 3 pruebas; M9 vuelve a "solo doble" |
| 3 | Docs, spec y cierre (~70 líneas autoría + 205 copia excluida) | PR3 | Manual: revisión de que `Docs/MODULES.md` queda consistente | N/A — sin código | Revertir el commit de docs/spec; no toca `Runtime/` |

## Phase 0: Guardrails (leer antes de escribir)

- [ ] 0.1 Confirmar frontera del diff: solo `Runtime/Scenarios/Emergency/`, `Tests/EditMode/Scenarios/Emergency/`, `Docs/` y `openspec/`. Nada en `Runtime/Core/`, `Runtime/CoreChannels/`, `Runtime/ClinicalResponse/`, `Runtime/Scenarios/Boardroom/`, `Data/`.
- [ ] 0.2 Confirmar que NO es cambio de contrato: `IScenarioObjective`/`ReceptivityChange` (M0) no cambian firma.
- [ ] 0.3 Confirmar que `Tests/EditMode/Core/ScenarioObjectiveContract.cs` no se modifica.
- [ ] 0.4 Confirmar que `Fakes/ScriptedScenarioObjective.cs` y ambos `.asmdef` (runtime y tests) quedan intactos — ya referencian `NpcAi.Core` + `NpcAi.Core.Channels`, ninguna referencia nueva.

## Phase 1: TDD — Lector de `clave` (PR1)

- [ ] 1.1 RED: `Tests/EditMode/Scenarios/Emergency/TriageKeyLoaderTests.cs` — caso feliz; `clave` ausente; JSON malformado; `triajeEsperado` "VI"/vacío; `banderasRojas` ausente/`[]`; JSON con `hechos` insuficientes (AD3: igual carga). JSON embebido como constante `string`, sin tocar `Data/Cases/`.
- [ ] 1.2 GREEN: `Runtime/Scenarios/Emergency/TriageKey.cs` — POCO, nulos → vacío (espejo de `Hecho`).
- [ ] 1.3 GREEN: `Runtime/Scenarios/Emergency/TriageKeyLoader.cs` — `TryParse` + `RawCase`/`RawClave` privados anidados (AD1), valida `triajeEsperado` contra `{"I".."V"}` hasta verde en 1.1.

## Phase 2: `TriageObjectiveSettings` (PR1)

- [ ] 2.1 GREEN: `Runtime/Scenarios/Emergency/Config/TriageObjectiveSettings.cs` — dos fracciones complementarias (`PesoDeReceptividad=0.3f`, `PesoDeBanderasRojas=0.7f`, AD4), `RachaParaReceptividadPlena=5` (AD7), `Clamp01` en el constructor. Sin prueba dedicada (no está en el File Inventory de `design.md`): su corrección se verifica indirectamente en 3.5 vía los techos exactos 0.3/0.7 de `Progress01`.

## Phase 3: TDD — `TriageScenarioObjective` (PR2)

- [ ] 3.1 RED: `TriageScenarioObjectiveTests.cs : ScenarioObjectiveContract` — `CreateSubject()` = constructor sin parámetros; las 7 pruebas heredadas deben pasar vía renormalización (`HasKey==false`). No modifica la clase base.
- [ ] 3.2 RED: `TriageProgresoTests.cs` — techos exactos con caso asignado: 0.3 (solo receptividad), 0.7 (solo clínica), 1.0 (ambas, `IsComplete`); crédito proporcional (2/4 banderas); racha crece y satura en N=5; `Worsened` la reinicia a 0; `Notify(default)`/`From==To` no-op; reversión por `Worsened` y por triaje incorrecto tras acertar.
- [ ] 3.3 RED: `TriageSuperficieAditivaTests.cs` — `RegisterRedFlag` idempotente por índice; índice 99/negativo no lanza; sin efecto antes de `AssignCase`; `DeclareTriage` sobrescribible y normalizada (`"ii"`, `" II "`); `cargarJson` nulo/que lanza/caso sin `clave` → `HasKey` falso sin excepción; `AssignCase(ClinicalCaseId.None)`; reasignar caso descarta banderas/triaje previos; `Reset()` vuelve al estado recién construido (AD8) y es idempotente.
- [ ] 3.4 GREEN: `Runtime/Scenarios/Emergency/TriageScenarioObjective.cs` — 2 constructores, `Notify`/`AssignCase`/`RegisterRedFlag`/`DeclareTriage`/`Reset`, `Progress01`/`IsComplete` exactos (pseudocódigo de `design.md`), `HasKey`/`RedFlagsFound`/`RedFlagCount`/`ExpectedClosure`. Incluir `RedFlagCount` aunque la spec no lo nombra: sin él el rango válido de `RegisterRedFlag` es inobservable desde otro ensamblado (decisión de este tasks.md, costo cero); promoverlo a la spec al archivar si el usuario lo confirma.
- [ ] 3.5 Confirmar en verde 3.1-3.3, incluidos los techos exactos `0f`/`0.3f`/`0.7f`/`1.0f` de la tabla "Aritmética" de `design.md` (verificación indirecta de 2.1).

## Phase 4: Compuerta humana

- [ ] 4.1 MANUAL (Unity Editor — la ejecuta y la registra el usuario, no el agente): Test Runner > EditMode > Run All con las Fases 1-3 completas; registrar el total en `apply-progress.md`. **No aplica compuerta física/hardware**: M9 es lógica de servidor, cero VR/XR/audio/`MonoBehaviour`.

## Phase 5: Documentación (PR3)

- [ ] 5.1 `Docs/MODULES.md` (sección M9) — cerrar "solo doble"; documentar el consumo real de `clave` que `Data/Cases/README.md`, la sección M14 y `ClinicalCase.cs` ya daban por hecho; nombrar lo diferido: evaluación de `cierreEsperado`, integración rica con M15 (índice/`campo` de *match*), cableado de M11.
- [ ] 5.2 Confirmar frontera de diff completa (Fases 0-5): nada fuera de `Runtime/Scenarios/Emergency/`, `Tests/EditMode/Scenarios/Emergency/`, `Docs/` y `openspec/`.

## Phase 6: Cierre (acciones del autor)

- [ ] 6.1 `git add` solo de las carpetas de este cambio; `git diff --cached` antes de cualquier commit.
- [ ] 6.2 Checklist "Antes de mergear" del `README.md`: pruebas propias en verde, diff acotado, `ScriptedScenarioObjective` sigue pasando el contrato sin cambios, rama al día con `main`, decisiones registradas en Engram y en el documento de contexto (regla 10).
- [ ] 6.3 `openspec/specs/escenario-emergencia-m9/spec.md` — copia directa del spec de este cambio (capacidad nueva, sin spec previa que reconciliar, mismo patrón que M7 PR4); mover este directorio a `openspec/changes/archive/` y escribir `archive-report.md`.
- [ ] 6.4 PR(s) final(es) mergeado(s) a `main` por el autor (regla 8, self-merge).

## Notas

- `RachaParaReceptividadPlena=5` y los pesos 0.3/0.7 · 0.7/0.3 son juicio pedagógico, sin validar
  con el asesor todavía; viven en `TriageObjectiveSettings` inyectable, cambiarlos no toca lógica
  de puntaje (Open Questions de `design.md`).
- `RedFlagCount` es un añadido de este `tasks.md` más allá de la spec original (ver 3.4).
- `design.md` (Open Questions) todavía describe como pendiente la discrepancia "techo 0.4" de
  `proposal.md` líneas 166/206 — esas dos frases ya fueron corregidas a "techo 0.3" antes de este
  cambio; la nota de `design.md` quedó obsoleta y puede limpiarse en un cambio posterior o al
  archivar, no bloquea esta entrega.
- Única compuerta humana: EditMode (4.1). A diferencia de M1/M2/M7, M9 no tiene compuerta física.
