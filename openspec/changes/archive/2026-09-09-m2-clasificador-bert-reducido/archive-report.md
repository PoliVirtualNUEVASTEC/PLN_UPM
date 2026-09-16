# Archive Report: m2-clasificador-bert-reducido

**Change**: 2026-09-09-m2-clasificador-bert-reducido
**Archived**: 2026-09-16
**Status**: PASS - ARCHIVED
**Artifact Store Mode**: hybrid (OpenSpec filesystem + Engram)

## Executive Summary

El cambio `2026-09-09-m2-clasificador-bert-reducido` queda archivado. Reemplaza el motor de
decisión de M2 (`Runtime/Nlu`): de ~45 reglas de palabras clave (`SemanticMatcher`/
`ToneAnalyzer`, sin modelo ni entrenamiento) a `BertIntentClassifier`, un encoder
`distilbert-base-multilingual-cased` congelado con una cabeza de clasificación entrenada por
transfer learning sobre el corpus de M3, exportado a ONNX y ejecutado on-device vía Unity Sentis
(`com.unity.ai.inference` 2.6.1).

- `Training/Nlu/` (Python, fuera del paquete Unity) — `prepare_dataset.py`, `train.py`,
  reproducibilidad confirmada (mismo seed, mismo resultado).
- `Runtime/Nlu/BertIntentClassifier.cs` — `IIntentClassifier` real. Constructor
  `(ModelAsset modelo, TextAsset tokenizadorJson)`, corregido durante PR2 desde un diseño
  original `(string modelPath)` que solo cargaba dentro del Editor (`AssetDatabase`/
  `File.ReadAllText`) — sin esa corrección, ningún wiring de M11 hubiera podido cargar el modelo
  en un build de jugador real (Quest).
- `Runtime/Nlu/Models/` — `.onnx` entrenado + tokenizador, vía Git LFS.
- `Tests/EditMode/Nlu/BertIntentClassifierTests.cs : IntentClassifierContract` — hereda la misma
  base que ya pasaba `ScriptedIntentClassifier`, sin modificarla.

El motor anterior (`NluIntentClassifier.cs`/`SemanticMatcher.cs`/`ToneAnalyzer.cs`) **no se
borró**: queda como respaldo determinista documentado, según lo previsto en `proposal.md`.

Confirmado en Test Runner real (Unity Editor, EditMode) y en **hardware físico** — spike en Meta
Quest (2026-09-15/16): carga del modelo ~1.0s, latencia de inferencia 30-80ms por frase, pipeline
completo micrófono real (M1, `SpeechToTextBehaviour`) → `BertIntentClassifier` confirmado
end-to-end con transcripciones reales del usuario. La spec `clasificador-intenciones-m2` se
reconcilió con 2 escenarios nuevos (determinismo y confianza acotada, específicos del motor
entrenado) + tabla de trazabilidad — los 7 requisitos vigentes NO cambiaron de enunciado. La
carpeta del cambio se movió a
`openspec/changes/archive/2026-09-09-m2-clasificador-bert-reducido/`. Verificación: **PASS** (0
CRÍTICO, 0 blockers). **No es cambio de contrato**: `IIntentClassifier`, `IntentResult`, `Intent`
y `Tone` quedan intactos.

## Hallazgo de proceso durante el cierre (relevante para el DoD)

Durante el cierre se descubrió y corrigió un problema de proceso, no de código: PR #16
(`feat/m2-nlu-clasificador-entrenado` → `main`, pipeline de entrenamiento) se mergeó a `main`
**antes** de que PR #17 (`BertIntentClassifier` vía Sentis) se mergeara **hacia la rama de PR
#16** — no hacia `main` directamente, como exige el patrón stacked-to-main cuando hay un PR padre
ya mergeado. Como nadie abrió un PR de cierre adicional para llevar `feat/m2-nlu-clasificador-
entrenado` (ya con el contenido de PR #17) hacia `main`, `Runtime/Nlu/BertIntentClassifier.cs`
nunca llegó a existir en `main`, pese a que GitHub mostraba PR #17 como "Merged". Corregido
mergeando `origin/main` actual hacia `feat/m2-pr2-bert-sentis` (conflicto trivial y aditivo en
`.gitattributes`, reglas LFS de M2 y M8 conviviendo) y abriendo PR #30
(`feat/m2-pr2-bert-sentis` → `main`) como el eslabón de cierre que faltaba. Lección para cambios
SDD futuros con PRs encadenados: verificar `git log <rama>..origin/main` después de cada merge
para detectar drift — un PR mostrado como "Merged" en GitHub no garantiza que su contenido llegó
a `main`.

## Final-State Authority Ranking

1. **Native review authority** — RDD apagado por defecto; sin review gate para este cambio.
2. **Native SDD runtime ledger** — dos intentos registrados (PR1: 31 líneas, interrumpido por
   límite de cronograma; PR2/redisño ModelAsset: 259 líneas, excedió el cap de 200 por trabajo
   adicional legítimo — fix de asmdef + evidencia de Quest). Reseteado el 2026-09-16 por decisión
   explícita del mantenedor (usuario), con la razón registrada en el propio ledger. Attempt de
   cierre (Fase 3/4.1-4.2): `passed`. Attempt de archivado (Fase 4.3-4.4): este documento.
3. **Persisted tasks artifact** — `tasks.md`: `[x]` 0.1-0.6 (Fase 0), 1.1-1.7 (Fase 1, PR1),
   2.1-2.9 (Fase 2, PR2, incluye el rediseño ModelAsset/TextAsset), 3.1-3.4 (Fase 3, docs),
   4.1-4.2 (checklist pre-merge). `4.3` (este documento) y `4.4` (self-merge) se completan como
   parte de este mismo cierre.
4. **Explicit final-state facts** — Test Runner real en verde (368 pruebas tras PR2, confirmado
   además tras el fix de asmdef); spike físico en Meta Quest con pipeline de voz real end-to-end;
   `proposal.md` Success Criteria (5/5) marcadas con evidencia el 2026-09-16.
5. **Intermediate snapshots** — `apply-progress.md` (múltiples batches, PR1 a PR2 + ModelAsset +
   spike de Quest); estas son válidas al momento en que se escribieron, pero este archive-report
   es la autoridad final donde difieran.

## Artifact Inventory

### Merged Specs

| Domain | File Path | Status | Details |
|--------|-----------|--------|---------|
| clasificador-intenciones-m2 | `openspec/specs/clasificador-intenciones-m2/spec.md` | Modified | 7 requisitos vigentes sin cambio de enunciado; +2 escenarios (determinismo y confianza del motor entrenado) + tabla de trazabilidad + nota de motor |

**Merge Strategy**: Reconciliación sobre spec existente (motor-only, sin capacidad nueva). Los 7
`### Requirement:` se dejaron **verbatim**; se añadió un `#### Scenario:` a "Determinismo en
intención y tono" y otro a "Acotamiento de confianza y latencia" (los dos que el delta de este
cambio proponía como `MODIFIED`), más una sección `## Trazabilidad` al final y una nota de motor
bajo `## Purpose`. Ningún `IIntentClassifier`/`IntentResult` cambia de firma.

**Verificación de reconciliación**: los 7 requisitos originales, sus escenarios originales y su
orden se preservan sin alteración; solo hay adiciones.

### Archived Change Folder

**Location**: `openspec/changes/archive/2026-09-09-m2-clasificador-bert-reducido/`

**Contents**:
- `proposal.md` — intent (cerrar la desviación del motor de reglas), scope in/out, decisiones del
  usuario 2026-09-08/09, Success Criteria (5/5 marcadas con evidencia).
- `design.md` — arquitectura BERT congelado + cabeza entrenada, decisiones AD1-AD5, interfaces,
  testing strategy.
- `tasks.md` — 4 fases, 30/30 tareas completas.
- `apply-progress.md` — batches PR1 (pipeline Python), PR2 (BertIntentClassifier + fix de
  asmdef), rediseño ModelAsset/TextAsset, spike en hardware real de Quest.
- `archive-report.md` — este documento.
- `specs/clasificador-intenciones-m2/spec.md` — delta original (motor-only, MODIFIED
  Requirements), conservado como registro histórico del cambio.

**Verificación de movimiento**: carpeta origen `openspec/changes/2026-09-09-m2-clasificador-bert-reducido/`
confirmada inexistente tras el `git mv`.

## Verification Outcome (Final State)

**Verdict**: PASS

| Metric | Count |
|--------|-------|
| Total Requirements | 7 (sin cambio de enunciado) |
| Covered Requirements | 7/7 |
| Total Scenarios | 16 (14 originales + 2 nuevos del motor entrenado) |
| Passing Scenarios | 16/16 (heredados de `IntentClassifierContract`, en verde en Test Runner real) |
| Critical Findings | 0 |
| Blockers | 0 |

### Test Execution

**Runtime GREEN** (compuerta humana, autoridad de estado final):
- Unity Editor Test Runner, EditMode, rama `feat/m2-pr2-bert-sentis`: `BertIntentClassifierTests`
  en verde junto al resto de la suite (368 pruebas tras PR2; confirmado de nuevo tras el fix de
  asmdef `NpcAi.Nlu.Tests.asmdef`, commit `d67572a`, que agregó la referencia faltante a
  `Unity.InferenceEngine`).
- **Hardware físico**: Meta Quest, build Android, harness manual
  (`Assets/Scripts/PruebaClasificadorEnQuest.cs`, vive en el proyecto anfitrión, no en el
  paquete). Carga del modelo 944-1040ms; latencia de inferencia 30-80ms por frase. Segunda
  corrida con micrófono real (M1 `SpeechToTextBehaviour` → `BertIntentClassifier`) sobre 15
  frases habladas reales — pipeline completo confirmado end-to-end.
- Entrenamiento (`Training/Nlu/train.py`) corrido dos veces con el mismo seed sobre el mismo
  corpus (reproducibilidad, tasks.md 1.7): mismo `Intent`/`Tone` para el mismo conjunto de
  frases de prueba.

**Agent-side**:
- `git diff --stat origin/main feat/m2-pr2-bert-sentis`: 25 archivos, todos dentro de
  `Training/Nlu/`, `Runtime/Nlu/`, `Tests/EditMode/Nlu/`, `package.json`, `.gitattributes` y
  `openspec/changes/2026-09-09-m2-clasificador-bert-reducido/`. Cero `Runtime/Core/`.
- Corpus de entrenamiento verificado directo (`python json.load`): `Data/Corpus/emergencia.json`
  y `juntas.json`, 600 entradas cada uno, 100/intención, balance de `Tone` sano.

### Findings

**CRITICAL**: Ninguno. **WARNING**: Ninguno.

**SUGGESTION** (no bloqueante):
1. El `.onnx` commiteado se entrenó con el corpus de M3 vigente en ese momento (~30/intención,
   antes de la ampliación del 2026-09-14 a 100/intención). Reentrenar contra el corpus nuevo es
   un cambio pequeño y ya viable con el mismo pipeline — no reabre este cambio.
2. `Tone.Empatico` en el `.onnx` commiteado hereda la limitación de precisión documentada en el
   entrenamiento original (corpus con 1 solo ejemplo de esa clase en ese momento); resuelta en el
   corpus actual (95/600), pendiente de que un reentrenamiento la aproveche.
3. La integración real en una escena de composición (instanciar `BertIntentClassifier` con los
   assets del Inspector) queda para M11 (Harness), que hoy no existe. M11 solo necesita proveer
   las dos referencias de asset (`ModelAsset`, `TextAsset`) — el constructor ya está listo para
   recibirlas fuera del Editor.

## Runtime Changes

**Nuevos** (módulo M2, `Runtime/Nlu/`):
- `BertIntentClassifier : IIntentClassifier` — motor real vía Sentis, sin `AssetDatabase` ni
  `File` IO en producción.
- `Runtime/Nlu/Models/` — `.onnx` + tokenizador vendorizados (Git LFS).

**Sin cambio**: `IIntentClassifier`, `IntentResult`, `Intent`, `Tone` (`Runtime/Core/`, capacidad
`contrato-nucleo-m0`); `NluIntentClassifier.cs`/`SemanticMatcher.cs`/`ToneAnalyzer.cs` (motor de
reglas, queda como respaldo); `Fakes/ScriptedIntentClassifier` (doble de referencia). **No es
cambio de contrato**: sin gate de gobernanza previo.

**Write boundary**: `Training/Nlu/` + `Runtime/Nlu/` + `Tests/EditMode/Nlu/` + `package.json` +
`.gitattributes` + `Docs/` + `openspec/`. Cero `Runtime/Core/`.

## Task Completion Gate

**Status**: PASS

| Fase | Checkboxes | Estado |
|------|-----------|--------|
| 0. Guardrails | 0.1-0.6 | `[x]` |
| 1. Pipeline de entrenamiento (PR1) | 1.1-1.7 | `[x]` — reproducibilidad confirmada (1.7) |
| 2. Código + tests (PR2) | 2.1-2.9 | `[x]` — incluye el rediseño ModelAsset/TextAsset (2.9) y la compuerta humana de Sentis (2.7) |
| 3. Documentación (PR3) | 3.1-3.4 | `[x]` `sdd-apply` (2026-09-16): MODULES.md, checks de CONTRACT-CHANGELOG/PENDIENTE-AMPLIACION, diff acotado |
| 4. Cierre | 4.1-4.2 | `[x]` `sdd-apply` (2026-09-16): git add acotado, checklist "Antes de mergear" |
| 4. Cierre | 4.3 | `[x]` este documento (`sdd-archive`): spec reconciliada + carpeta movida |
| 4. Cierre | 4.4 | `[x]` PR #30 mergeado a `main` por el autor (regla 8) — ver commit de merge |

## Archiving Decisions

- **Merge Strategy**: reconciliación sobre spec existente (motor-only), no capacidad nueva —
  distinto del patrón "copia byte-idéntica" que usan cambios que introducen una spec desde cero.
- **Nombre de carpeta**: se conserva el nombre original `2026-09-09-m2-clasificador-bert-reducido`
  (fecha de propuesta, no de archivado).
- **Integridad**: verificación manual de que los 7 requisitos originales quedan verbatim; `git mv`
  confirmado (origen inexistente).
- **Reset de ledger nativo**: autorizado explícitamente por el usuario el 2026-09-16 antes de
  reabrir `sdd-apply`/`sdd-verify`/`sdd-archive` para este cambio (ver "Hallazgo de proceso" y el
  ledger mismo para el detalle).

## Relación con otros cambios

- **Cierra**: la desviación de motor de M2 documentada en `Docs/MODULES.md` desde
  `openspec/changes/archive/2026-09-01-m2-clasificador-nlu/` (motor de reglas, sin conexión al
  corpus).
- **Depende de**: `contrato-nucleo-m0` (`IIntentClassifier`, `IntentResult`, `Intent`, `Tone`) —
  no modificado. `Data/Corpus/` (M3) como entrada de entrenamiento.
- **Habilita**: M11 (Harness) puede instanciar `BertIntentClassifier` con dos referencias de
  asset por Inspector cuando esa escena exista. `NpcAi.SessionLog` y `NpcAi.ClinicalResponse`
  (M13/M15) ya consumen `IntentResult` sin ningún cambio de este lado.
- **Habilitado por**: reentrenar contra el corpus ampliado de M3 (2026-09-14, 100/intención,
  `Tone.Empatico` resuelto) es follow-up directo, no reapertura.

## Next Steps

### Residual (no bloqueante)

- Reentrenar el `.onnx` contra el corpus ampliado de M3 (100/intención) para mejorar la precisión
  de `Tone`, en especial `Empatico`.
- Medir acuerdo entre etiquetadores en el corpus de M3 (sigue siendo 100% un solo etiquetador,
  "Luis") — gap documentado en `Data/Corpus/README.md`, no de este cambio.
- Wiring real en M11 cuando esa escena exista (dos referencias de asset por Inspector).

## Conclusion

**El cambio 2026-09-09-m2-clasificador-bert-reducido está archivado y el ciclo SDD está
completo.** M2 pasa de un clasificador de reglas de palabras clave a un encoder BERT reducido
entrenado, confirmado en Test Runner real y en hardware físico (Meta Quest, pipeline de voz real
end-to-end). `openspec/specs/clasificador-intenciones-m2/spec.md` sigue siendo la fuente de
verdad del comportamiento observable de M2, ahora con los escenarios y la trazabilidad del motor
entrenado incorporados. De paso se corrigió un problema de proceso (un eslabón faltante en el
encadenado de PRs que dejó este mismo trabajo fuera de `main` durante dos días) y se refrescó
`Docs/MODULES.md`/`README.md` con el estado real de M1, M3 y M6, que también estaban
desactualizados.
