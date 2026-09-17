# Archive Report: m9-escenario-emergencia

**Change**: 2026-09-17-m9-escenario-emergencia
**Archived**: 2026-09-17
**Status**: PASS - ARCHIVED
**Artifact Store Mode**: hybrid (OpenSpec filesystem + Engram)

## Executive Summary

El cambio `2026-09-17-m9-escenario-emergencia` queda archivado. Entrega la primera implementación
real de `IScenarioObjective` para el escenario de emergencia (triaje): `TriageScenarioObjective`
mide `Progress01`/`IsComplete` como una mezcla de receptividad sostenida (0.3) y corrección
clínica (0.7 = 0.7 banderas rojas descubiertas + 0.3 triaje declarado correcto), leyendo el
bloque `clave` de `Data/Cases/*.json` que tres documentos distintos (`Data/Cases/README.md`, la
sección M14 de `Docs/MODULES.md`, `Runtime/ClinicalResponse/ClinicalCase.cs`) ya declaraban
propiedad exclusiva de M9, pero que nadie leía hasta ahora.

- `Runtime/Scenarios/Emergency/TriageKey.cs` + `TriageKeyLoader.cs` — POCO y lector mínimo del
  bloque `clave`, espejo exacto de `ClinicalCase`/`ClinicalCaseLoader` (M9 no referencia
  `NpcAi.ClinicalResponse`: esos tipos son privados e inaccesibles de todas formas).
- `Runtime/Scenarios/Emergency/Config/TriageObjectiveSettings.cs` — pesos como fracciones
  complementarias (`PesoDeReceptividad`/`PesoClinico`, `PesoDeBanderasRojas`/`PesoDeTriaje`), que
  garantizan que la suma sea exactamente `1.0f` en punto flotante — `IsComplete` nunca depende de
  la tolerancia `1e-4` del contrato.
- `Runtime/Scenarios/Emergency/TriageScenarioObjective.cs` — implementación real del puerto +
  superficie aditiva (`AssignCase`, `RegisterRedFlag`, `DeclareTriage`, `Reset()`), mismo patrón
  de "puerto congelado + clase concreta más rica" que `BertIntentClassifier` (M2) y
  `VrInputBehaviour` (M7) ya usan en este repositorio.
- `Runtime/Scenarios/Emergency/Fakes/ScriptedScenarioObjective.cs` (el doble) **no se tocó**:
  sigue disponible como respaldo determinista, según lo previsto en `proposal.md`.

Confirmado en Test Runner real (Unity Editor, EditMode): las 4 clases de prueba nuevas pasan en
verde junto con el resto de la suite completa del proyecto, sin necesidad de ajustar ningún
`.asmdef` (la verificación de la tarea 0.4 leyó los archivos reales y confirmó que no había gap,
a diferencia del defecto real que sí apareció en M7 PR3). **M9 no tiene compuerta física**: es
lógica de servidor pura, cero dependencia de VR/XR/audio/`MonoBehaviour`. La spec
`escenario-emergencia-m9` se promovió tal cual (capacidad nueva, sin spec previa que reconciliar).
La carpeta del cambio se movió a `openspec/changes/archive/2026-09-17-m9-escenario-emergencia/`.
Verificación: **PASS** (0 CRÍTICO, 0 blockers). **No es cambio de contrato**: `IScenarioObjective`
y `ReceptivityChange` quedan intactos.

## Decisiones de producto/pedagógicas registradas (sin validar con el asesor)

- Pesos por defecto: 0.3 receptividad sostenida / 0.7 corrección clínica; dentro de lo clínico,
  0.7 banderas rojas / 0.3 triaje declarado. Inclinados a propósito hacia lo clínico: es el motivo
  por el que `clave` existe y está marcada como exclusiva de M9.
- Crédito por bandera roja proporcional (fracción encontrada), no todo-o-nada ni ponderado por
  severidad — ponderar por severidad exigiría un campo nuevo en el esquema de M14, fuera de
  alcance de este cambio.
- Racha de receptividad plena a los 5 `Improved` consecutivos (`RachaParaReceptividadPlena`) —
  elegido para que la prueba heredada `Notify_con_el_valor_por_defecto_es_no_op` (3 mejoras) siga
  siendo una sonda significativa y no quede saturada de antemano.
- Todos viven en `TriageObjectiveSettings`, inyectable por constructor — cambiarlos no toca una
  línea de lógica de puntaje.

## Final-State Authority Ranking

1. **Native review authority** — RDD apagado por defecto; sin review gate para este cambio.
2. **Persisted tasks artifact** — `tasks.md`: `[x]` 0.1-0.4 (Fase 0), 1.1-1.3 (Fase 1, lector),
   2.1 (Fase 2, config), 3.1-3.5 (Fase 3, núcleo + superficie aditiva), 4.1 (Fase 4, compuerta
   humana), 5.1-5.2 (Fase 5, docs). `4.3`/`4.4` de Fase 6 se completan como parte de este mismo
   cierre.
3. **Explicit final-state facts** — Test Runner real en verde (4 clases de prueba nuevas + el
   resto de la suite), confirmado por el usuario el 2026-09-17.
4. **Intermediate snapshots** — `apply-progress.md` (un solo batch, Fases 0-3, más la
   actualización de la compuerta humana); válido en el momento en que se escribió, este
   archive-report es la autoridad final donde difiera.

## Artifact Inventory

### Merged Specs

| Domain | File Path | Status | Details |
|--------|-----------|--------|---------|
| escenario-emergencia-m9 | `openspec/specs/escenario-emergencia-m9/spec.md` | Created | 9 requisitos, 21 escenarios; capacidad nueva (sin spec previa que reconciliar) |

**Merge Strategy**: Capacidad nueva (Strategy B, igual que M6/M7). Copia mecánica, `diff` vacío
(byte-idéntica) — verificado directamente antes de este documento.

### Archived Change Folder

**Location**: `openspec/changes/archive/2026-09-17-m9-escenario-emergencia/`

**Contents**:
- `proposal.md` — intent (cerrar la desviación de `clave` sin leer), scope in/out, decisiones del
  usuario 2026-09-17 (superficie aditiva, mezcla receptividad+clínica, pesos finales 0.3/0.7 y
  0.7/0.3), Success Criteria.
- `design.md` — 11 decisiones de arquitectura (AD1-AD11) con alternativas rechazadas, tipos
  exactos, pseudocódigo de la fórmula de `Progress01`, tabla de archivos, flujo de datos,
  estrategia de pruebas.
- `tasks.md` — 6 fases, 20 tareas, todas completas.
- `apply-progress.md` — un batch (Fases 0-3) más la confirmación de la compuerta humana (4.1).
- `archive-report.md` — este documento.
- `specs/escenario-emergencia-m9/spec.md` — copia histórica del delta original.

**Verificación de movimiento**: carpeta origen `openspec/changes/2026-09-17-m9-escenario-emergencia/`
confirmada inexistente tras el `git mv`.

## Verification Outcome (Final State)

**Verdict**: PASS

| Metric | Count |
|--------|-------|
| Total Requirements | 9 |
| Covered Requirements | 9/9 |
| Total Scenarios | 21 |
| Passing Scenarios | 21/21 (en verde en Test Runner real) |
| Critical Findings | 0 |
| Blockers | 0 |

### Test Execution

**Runtime GREEN** (compuerta humana, autoridad de estado final):
- Unity Editor Test Runner, EditMode, rama `feat/m9-escenario-emergencia`: `TriageKeyLoaderTests`,
  `TriageScenarioObjectiveTests`, `TriageProgresoTests`, `TriageSuperficieAditivaTests` en verde,
  junto con el resto de la suite completa del proyecto, confirmado por el usuario 2026-09-17.
- `ScriptedScenarioObjectiveTests` (el doble, preexistente) no se vio afectado: el doble no se
  tocó en ningún momento de este cambio.

**Agent-side**:
- `git diff main..feat/m9-escenario-emergencia --stat`: 1137 líneas insertadas en 10 archivos de
  código/tests + planeación; diff acotado a `Runtime/Scenarios/Emergency/`,
  `Tests/EditMode/Scenarios/Emergency/`, `Docs/MODULES.md` y `openspec/`. Cero `Runtime/Core/`,
  `Runtime/CoreChannels/`, `Runtime/ClinicalResponse/`, `Runtime/Scenarios/Boardroom/`, `Data/`.
- Verificado directo por el orquestador (no solo el reporte del sub-agente): fórmula de
  `Progress01` correcta a mano (receptividad=1, banderas=1, triaje=1 → exactamente 1.0);
  `AssignCase`/`Reset()` descartan el progreso clínico sin tocar la racha de receptividad (AD8);
  ambos `.asmdef` (runtime y tests) confirmados sin gap real, a diferencia de M7 PR3.

### Findings

**CRITICAL**: Ninguno. **WARNING**: Ninguno.

**SUGGESTION** (no bloqueante, heredadas de `design.md`/`proposal.md`):
1. Evaluación automática de `clave.cierreEsperado`: se carga y se expone en lectura, pero no
   mueve `Progress01` — calificar un párrafo libre exige comparación semántica, decisión completa
   aparte.
2. Integración más rica con M15: que `IClinicalResponder.Respond` exponga qué `Hecho`/`Campo`
   hizo *match*, para que M9 no dependa de que el compositor reporte a mano qué bandera roja se
   superfició. Cambio SDD aparte sobre M15 (regla dura 1), no bloqueante.
3. Pesos y racha por defecto son juicio pedagógico sin validar con el asesor — viven en
   `TriageObjectiveSettings`, inyectable, cambiarlos no toca lógica.
4. El cableado real en escena (quién llama `AssignCase`/`RegisterRedFlag`/`DeclareTriage`) es
   trabajo de M11, que todavía no existe.

## Runtime Changes

**Nuevos** (módulo M9, `Runtime/Scenarios/Emergency/`):
- `TriageKey.cs`, `TriageKeyLoader.cs` — lector del bloque `clave`.
- `Config/TriageObjectiveSettings.cs` — pesos inyectables.
- `TriageScenarioObjective.cs : IScenarioObjective` — motor real + superficie aditiva.

**Sin cambio**: `IScenarioObjective`, `ReceptivityChange` (`Runtime/Core/`, capacidad
`contrato-nucleo-m0`); `Fakes/ScriptedScenarioObjective.cs` (doble de referencia);
`Runtime/ClinicalResponse/` (M15); `Runtime/Scenarios/Boardroom/` (M10). **No es cambio de
contrato**: sin gate de gobernanza previo.

**Write boundary**: `Runtime/Scenarios/Emergency/` + `Tests/EditMode/Scenarios/Emergency/` +
`Docs/MODULES.md` + `openspec/`. Cero `Runtime/Core/`, cero otros módulos.

## Task Completion Gate

**Status**: PASS

| Fase | Checkboxes | Estado |
|------|-----------|--------|
| 0. Guardrails | 0.1-0.4 | `[x]` |
| 1. Lector de clave | 1.1-1.3 | `[x]` |
| 2. Config | 2.1 | `[x]` |
| 3. Núcleo + superficie aditiva | 3.1-3.5 | `[x]` |
| 4. Compuerta humana | 4.1 | `[x]` confirmado en Test Runner real |
| 5. Documentación | 5.1-5.2 | `[x]` `Docs/MODULES.md` actualizado, frontera de diff confirmada |
| 6. Cierre | — | `[x]` este documento (`sdd-archive`): spec promovida + carpeta movida + PR (ver Next Steps) |

## Archiving Decisions

- **Merge Strategy**: capacidad nueva (Strategy B) — copia mecánica, sin reconciliación.
- **Nombre de carpeta**: se conserva `2026-09-17-m9-escenario-emergencia` (fecha de propuesta).
- **Integridad**: verificado `diff` vacío entre el delta original y la spec promovida antes de
  escribir este documento; `git mv` confirmado (origen inexistente).
- **Entrega**: un solo PR con `size:exception` aceptado explícitamente por el usuario 2026-09-17
  (~1137 líneas totales incluida documentación de planeación, por encima del presupuesto de
  sesión de 800 pero sin ameritar una cadena como M7 — módulo cohesivo, sin capa de envoltura).

## Relación con otros cambios

- **Cierra**: la desviación de M9 documentada en `Docs/MODULES.md` — el bloque `clave` de M14
  existía sin lector desde que M14 se entregó.
- **Depende de**: `contrato-nucleo-m0` (`IScenarioObjective`, `ReceptivityChange`) — no
  modificado. `Data/Cases/*.json` (M14) como fuente de datos, solo lectura.
- **Habilita**: M11 (Harness) puede instanciar `TriageScenarioObjective` con un
  `Func<ClinicalCaseId,string>` inyectado cuando esa escena exista.
- **Habilitado por**: una integración más rica con M15 (exponer qué `Hecho` hizo *match*) mejoraría
  la precisión de `RegisterRedFlag`, pero es un cambio SDD aparte, no bloqueante.

## Next Steps

### Cierre del DoD del módulo M9 (acción del autor)

1. Abrir Unity Editor una vez para que autogenere los `.meta` de
   `openspec/specs/escenario-emergencia-m9/spec.md` (la copia recién promovida) — el resto de los
   `.meta` de este cambio ya se generaron y commitearon junto con el código.
2. PR abierto y mergeado a `main` por el autor (regla 8, self-merge) — ver PR asociado a la rama
   `feat/m9-escenario-emergencia`.

### Residual (no bloqueante)

- Reentrenar/ajustar los pesos de `TriageObjectiveSettings` cuando el asesor los revise.
- Cambio SDD de seguimiento sobre M15 para exponer qué `Hecho` hizo *match* (mejora la precisión
  de `RegisterRedFlag`, no bloquea nada de lo entregado aquí).
- Evaluación automática de `clave.cierreEsperado`: decisión completa aparte (comparación
  semántica).
- Cableado real en escena: espera a M11.

## Conclusion

**El cambio 2026-09-17-m9-escenario-emergencia está archivado y el ciclo SDD está completo.** M9
pasa de "solo doble" (paso fijo ±1, idéntico a M10) a una implementación real que mide
específicamente el escenario de triaje: receptividad sostenida más corrección clínica contra la
`clave` de M14 que llevaba desde su entrega sin ningún lector. `openspec/specs/escenario-emergencia-m9/spec.md`
es ahora la fuente de verdad del comportamiento observable de M9. Confirmado en Test Runner real
sin ningún gap de `.asmdef` — a diferencia de M2 y M7 en esta misma sesión, esta vez la
verificación previa (leer los archivos reales antes de asumir) no encontró ningún defecto que
corregir.
