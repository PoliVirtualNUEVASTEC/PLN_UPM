# Archive Report: formalizar-perfiles-m5

**Change**: 2026-09-03-formalizar-perfiles-m5
**Archived**: 2026-09-03
**Status**: PASS - ARCHIVED
**Artifact Store Mode**: hybrid (OpenSpec filesystem + Engram)

## Executive Summary

El cambio `2026-09-03-formalizar-perfiles-m5` queda archivado. Formaliza el modulo M5 (perfiles
de personalidad, `Data/Personalities/`) que ya estaba entregado por implementacion directa: 4
archivos `.asset` (`grosero`, `histerico`, `introvertido`, `empatico`) creados por el usuario en
Unity, mas la prueba de carga `PersonalityProfilesDataTests.cs` (8 pruebas EditMode) escrita por
el agente y confirmada en verde por el usuario el 2026-09-03. La spec delta
`perfiles-personalidad-m5` (7 requisitos, 10 escenarios) se copio byte-identica a
`openspec/specs/perfiles-personalidad-m5/spec.md` — capacidad nueva, sin spec previa que
reconciliar. La carpeta del cambio se movio a
`openspec/changes/archive/2026-09-03-formalizar-perfiles-m5/`. Verificacion: **PASS** (0 CRITICO,
0 blockers). Cero cambio de runtime. Cierra la *Open Question* del design de `receptividad-m4`:
los numeros concretos por personalidad se documentan en M5 ("Tuning de record"), no en M4.

## Final-State Authority Ranking

1. **Native review authority** — Sin review gate; receipt-driven development apagado por defecto
   (`gentle-ai review mode status` = off, decided by default).
2. **Persisted tasks artifact** — `tasks.md`: `[x]` 0.1-0.5, 1.1-1.4, 2.1-2.3, 3.1-3.3, 4.4.
   Abiertos por diseno: 4.1-4.3, 4.5, 4.6 (acciones del autor, pre-merge no pre-archive).
3. **Explicit final-state facts** — verify-report verdict PASS; verde EditMode humano de las 8
   pruebas de `PersonalityProfilesDataTests` (mas las ~105 de M4) en `NpcAi.Receptivity.Tests`,
   atestiguado por el usuario el 2026-09-03.
4. **Intermediate snapshots** — verify-report.md (este cambio no tiene apply-progress: sin codigo
   nuevo).

## Artifact Inventory

### Merged Specs

| Domain | File Path | Status | Details |
|--------|-----------|--------|---------|
| perfiles-personalidad-m5 | `openspec/specs/perfiles-personalidad-m5/spec.md` | Created | 7 requisitos, 10 escenarios; capacidad nueva (sin spec previa que reconciliar) |

**Merge Strategy**: Capacidad nueva (Strategy B). `cp` mecanico + `diff` vacio (byte-identico).
Convive con `contrato-nucleo-m0`, `canales-evento-nucleo-m0`, `clasificador-intenciones-m2` y
`receptividad-m4`; NO redefine el esquema del perfil ni el motor, que siguen siendo de
`receptividad-m4`, ni los tipos de `NpcAi.Core`, que son de `contrato-nucleo-m0`.

**Verificacion de copia**: `diff` (origen vs destino) sin diferencias.

### Archived Change Folder

**Location**: `openspec/changes/archive/2026-09-03-formalizar-perfiles-m5/`

**Contents**:
- `proposal.md` — intent (formalizar M5 y cerrar la Open Question de `receptividad-m4`), scope
  in/out, capacidad nueva, approach "documentar y fijar", riesgos, decisiones del usuario 2026-09-03.
- `design.md` — M5 = datos sin asmdef, grafo de referencias, decisiones AD1-AD9, flujo de la
  costura vs runtime actual con `Standard()`, tablas "Tuning de record" de las 4 personalidades,
  inventario de archivos, superficie publica consumida, threat matrix N/A, rollback.
- `tasks.md` — 21 tareas; 15 `[x]`, 6 `[ ]` (fase 4, acciones del autor salvo 4.4).
- `verify-report.md` — verdict PASS; 7/7 requisitos, 10/10 escenarios; 0 CRITICO, 0 WARNING; 0
  hallazgos corregidos.
- `specs/perfiles-personalidad-m5/spec.md` — 7 requisitos ADDED + tabla de trazabilidad.

**Verificacion de movimiento**: carpeta origen
`openspec/changes/2026-09-03-formalizar-perfiles-m5/` confirmada inexistente tras el `mv`.

## Verification Outcome (Final State)

**Verdict**: PASS

### Requirement Coverage

| Metric | Count |
|--------|-------|
| Total Requirements | 7 |
| Covered Requirements | 7/7 |
| Total Scenarios | 10 |
| Passing Scenarios | 10/10 |
| Critical Findings | 0 |
| Blockers | 0 |

El escenario 7 ("Standard sigue siendo el reparto en codigo y coincide con los assets") combina
dos pruebas EditMode en verde con verificacion **por inspeccion** de la equivalencia numerica
1:1 (tablas "Tuning de record" del design + task 2.3). El resto traza a una prueba automatica.

### Test Execution

**Runtime GREEN** (compuerta humana, autoridad de estado final):
- El usuario corrio EditMode > Run All en Unity 6 el 2026-09-03 ("ya corrieron y todos los test
  estan en verde").
- Ensamblado `NpcAi.Receptivity.Tests`: las 8 pruebas nuevas de `PersonalityProfilesDataTests`
  (`Data_Personalities_trae_exactamente_los_cuatro_perfiles_de_M5`,
  `Cada_archivo_se_llama_igual_que_su_personalityId`,
  `BuildCatalog_resuelve_los_cuatro_ids_con_perfil_propio`,
  `Un_id_fuera_del_catalogo_cae_en_Default`, `Cada_perfil_respeta_los_signos_del_contrato`,
  `Cada_perfil_tiene_umbrales_y_puntaje_inicial_coherentes`,
  `Cada_perfil_de_disco_entra_al_motor_real_sin_lanzar`,
  `Los_extremos_hostil_y_cooperativo_no_arrancan_en_el_mismo_estado`) mas las ~105 de M4.
- Este cambio no agrega runtime ni pruebas: no hay `sdd-attempt` que consumir.

**Agent-side**:
- `gentle-ai sdd-status 2026-09-03-formalizar-perfiles-m5 --json`: `proposal/specs/design/tasks = done`.
- `rg` de los 8 metodos de `PersonalityProfilesDataTests` + los 4 tests de M4 citados en la
  trazabilidad: todos existen.
- `git status --porcelain`: el diff del cambio solo toca
  `openspec/changes/2026-09-03-formalizar-perfiles-m5/`, `Data/Personalities/*.asset` y
  `Tests/EditMode/Receptivity/PersonalityProfilesDataTests.cs`. Cero `Runtime/`.
- Tablas "Tuning de record" vs los 4 `.asset`: coincidencia campo a campo.

### Findings

**CRITICAL**: Ninguno.

**WARNING**: Ninguno.

**SUGGESTION** (no bloqueante, del verify-report):
1. Cuando un bootstrap de M8/M11 inyecte el catalogo real via `BuildCatalog(assets)`, decidir si
   `ReceptivityEngine()` (ctor sin args con `Standard()`) sigue existiendo o se marca obsoleto.
2. Si a futuro se quiere blindar la equivalencia numerica del escenario 7, una prueba podria
   comparar `BuildCatalog(assets)` contra `Standard()` perfil por perfil.

## Runtime Changes

**Ninguno.** `Runtime/` no esta en el diff de este cambio. El esquema del perfil
(`ReceptivityProfileAsset`, `ReceptivityProfile`, `ReceptivityProfileCatalog`) es de
`receptividad-m4` y no se toca. `ReceptivityEngine()` sigue usando
`ReceptivityProfileCatalog.Standard()`: M5 entrega dato inerte con su costura (`BuildCatalog`)
probada; el cableado al motor es de un modulo posterior (M8/M11).

**Write boundary**: `openspec/changes/2026-09-03-formalizar-perfiles-m5/` + `Data/Personalities/`
(modulo M5) + `Tests/EditMode/Receptivity/PersonalityProfilesDataTests.cs` (prueba de aceptacion
de M5, reusa el asmdef de M4). Cero `Runtime/`. **No es cambio de contrato** (no toca
`Runtime/Core/`): sin gate de gobernanza previo.

## Task Completion Gate

**Status**: PASS

| Fase | Checkboxes | Estado | Nota |
|------|-----------|--------|------|
| 0. Guardrails | 0.1-0.5 | `[x]` | Frontera confirmada; no es cambio de contrato; una sola unidad de trabajo |
| 1. Artefactos | 1.1-1.4 | `[x]` | proposal, design, spec delta, tasks escritos |
| 2. Trazabilidad | 2.1-2.3 | `[x]` | 7 requisitos -> prueba en verde; sin duplicar invariantes de `receptividad-m4`; tuning verificado 1:1 |
| 3. Verificacion ligera | 3.1-3.3 | `[x]` | verde EditMode atestiguado; verify-report PASS; dispatcher `--json` limpio |
| 4. Cierre | 4.4 | `[x]` | `sdd-archive` inline (merge + move + report) |
| 4. Cierre | 4.1-4.3, 4.5, 4.6 | `[ ]` | Acciones del autor: `git add` + `git diff --cached`, decision RDD/receipt, checklist "Antes de mergear", PR + self-merge, decisiones a Engram. Pre-merge, no pre-archive |

## Archiving Decisions

- **Merge Strategy B** (capacidad nueva): `cp` mecanico + `diff` vacio. No reemplaza nada;
  convive con las specs de M0, M2 y M4.
- **Nombre de carpeta**: formato ISO `2026-09-03-formalizar-perfiles-m5`.
- **Integridad**: copia de spec `diff` vacio -> PASS; movimiento de carpeta, origen inexistente
  -> PASS.

## Deliverables

### openspec/specs/perfiles-personalidad-m5/spec.md

**7 Requisitos**:
1. Alcance — cuatro personalidades, cero clases
2. El esquema es propiedad de M4
3. Los datos respetan los signos del contrato de receptividad
4. Umbrales y puntaje inicial coherentes
5. Los assets arman el catalogo sin tocar el motor
6. Los extremos son distinguibles
7. El tuning por personalidad es dato de M5

**10 Escenarios**: 9 con prueba EditMode cubridora en verde; 1 (escenario del requisito 7)
combina 2 pruebas verdes con verificacion por inspeccion de la equivalencia numerica.

## Relacion con otros cambios

- **Cierra**: la *Open Question* del design de `2026-09-02-formalizar-receptividad-m4` — "decidir
  si `receptividad-m4` recibe escenarios con numeros concretos por personalidad o si M5 documenta
  su tuning aparte". Resuelto: **M5 lo documenta aparte** (design "Tuning de record"). El design
  de M4 queda con esa pregunta respondida; su spec no se toco.
- **Depende de**: `receptividad-m4` (esquema `ReceptivityProfileAsset`, `ToProfile()`,
  `BuildCatalog(...)`, `ReceptivityProfile.Default`) y `contrato-nucleo-m0` (`PersonalityId`,
  enums `Intent`/`Tone`/`PhysicalAction`).

## Next Steps

### Cierre del DoD del modulo M5 (acciones del autor — Jefferson Estiven Aristizabal Quiceno)

1. `git add openspec/changes/archive/2026-09-03-formalizar-perfiles-m5/
   openspec/specs/perfiles-personalidad-m5/ Data/Personalities/*.asset
   Tests/EditMode/Receptivity/PersonalityProfilesDataTests.cs` (+ `.meta` correspondientes) y
   `git diff --cached`; confirmar que el diff no sale de esas rutas.
2. **Receipt `--projection staged`**: decidir explicitamente entre encender RDD
   (`gentle-ai review mode enable`) y generar el receipt, o entregar bajo politica de repo normal
   con RDD apagado. Ninguna de las dos la hace el agente.
3. Checklist "Antes de mergear" del README.
4. PR abierto y mergeado a `main` por el autor (regla 8). Rama: `feat/m5-perfiles-personalidad`.
5. Decisiones registradas en Engram (hecho en esta sesion) y en este `archive-report.md` (que
   hace de documento de contexto de la formalizacion, igual que en M0 y M4).

### Residual

- Open Questions del design (no bloqueantes): decision sobre `ReceptivityEngine()` + `Standard()`
  cuando M8/M11 cablee el catalogo real; futuros ajustes de tuning entran como cambio propio de
  M5 (editar el `.asset` + la tabla del design), sin tocar `receptividad-m4`.

## Conclusion

**El cambio 2026-09-03-formalizar-perfiles-m5 esta archivado y el ciclo SDD esta completo.**
M5 queda formalizado con una spec ejecutable trazada a 8 pruebas EditMode nuevas en verde (mas
las de M4). `openspec/specs/perfiles-personalidad-m5/spec.md` es ahora la fuente de verdad del
alcance y las garantias de los datos de personalidad, y de su tuning de record. Queda pendiente
el cierre manual del DoD del modulo (git add, receipt/RDD, checklist y PR) por parte del autor.
