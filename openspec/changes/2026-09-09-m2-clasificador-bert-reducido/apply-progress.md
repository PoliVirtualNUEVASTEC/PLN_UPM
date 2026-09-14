# Apply Progress — 2026-09-09-m2-clasificador-bert-reducido

**Change**: 2026-09-09-m2-clasificador-bert-reducido (M2, modulo de IA)
**Batch**: apply PR1 — pipeline de entrenamiento offline (`Training/Nlu/`, Python)
**Date**: 2026-09-09
**Artifact store**: hybrid
**Delivery**: auto-chain, stacked-to-main (PR1 -> main, PR2 -> PR1, PR3 -> PR2). Este batch es PR1.

## Mode Resolution

- Repo `strict_tdd: true`, pero el unico test runner configurado es Unity Test
  Framework EditMode, que no ejecuta Python.
- PR1 es Python puro, fuera de Unity (`tasks.md` Fase 1, nota final): "no requiere
  Test Runner ni Unity Editor para validarse, solo correr los scripts y revisar la
  salida". No hay suite EditMode para PR1.
- Resolucion: **Strict TDD RED->GREEN->REFACTOR con Unity no aplica a los scripts
  Python de PR1.** Se aplica el hard gate de todos los modos (Work Unit Evidence).
  El RED->GREEN de contrato (`BertIntentClassifierTests : IntentClassifierContract`)
  es de PR2, no de PR1.

## Completed Tasks (this batch)

### Fase 0 — Guardrails (leidos y respetados)
- [x] 0.1 Frontera de escritura PR1: solo `Training/Nlu/` + el directorio del cambio. Respetado.
- [x] 0.2 Frontera de escritura PR2 (leida; PR1 no toca `Runtime/`).
- [x] 0.3 Cero cambio en `Runtime/Core/` (`Ports.cs`/`Dtos.cs`/`Enums.cs`). Respetado.
- [x] 0.4 `Tests/EditMode/Core/IntentClassifierContract.cs` no se toca. Respetado (PR1 no crea pruebas).
- [x] 0.5 M2 es IA: sin `/sdd-ff`. Respetado.
- [x] 0.6 Corpus `Data/Corpus/*.json` es de M3: PR1 lo consume tal cual, no lo edita. Respetado.

### Fase 1 — Pipeline de entrenamiento offline (PR1)
- [x] 1.1 `Training/Nlu/requirements.txt` — 8 dependencias con version fijada exacta
  (`torch==2.5.1`, `transformers==4.46.3`, `tokenizers==0.20.3`, `sentencepiece==0.2.0`,
  `onnx==1.17.0`, `onnxruntime==1.20.1`, `scikit-learn==1.5.2`, `numpy==1.26.4`),
  cada una con comentario de para que es.
- [x] 1.2 `Training/Nlu/prepare_dataset.py` — carga y unifica `emergencia.json` +
  `juntas.json`; valida esquema (`text`/`intent`/`tone`/`scenario`/`labeler` no vacios) y
  que `intent`/`tone` sean miembros de los enums de `NpcAi.Core` (lista espejada de
  `Runtime/Core/Enums.cs`); split estratificado por la clave `intent||tone` con
  **degradacion con gracia**: clases con <2 ejemplos (hoy `Empatia||Empatico`) caen
  enteras a train y se avisa por stderr, sin abortar. CLI: `--corpus-dir`, `--out-dir`,
  `--val-fraction`, `--seed`, con defaults resueltos relativos a la raiz del repo.
- [x] 1.3 `Training/Nlu/train.py` — encoder pre-entrenado como `--encoder` **obligatorio,
  sin default** (para probar candidatos del spike sin editar el script); congela todos los
  parametros del encoder (`requires_grad_(False)` + `.eval()`); dos cabezas `nn.Linear` +
  softmax sobre el **mismo embedding congelado** (mean pooling enmascarado); valida contra
  el split de `prepare_dataset.py`; exporta encoder+cabezas+softmax a ONNX en
  `Runtime/Nlu/Models/intent-tone-classifier.onnx` (parametrizable `--onnx-out`; PR1 solo
  lo genera, lo commitea PR2). Semilla fija (`set_seed`: python/numpy/torch/cuda + cudnn
  determinista + `use_deterministic_algorithms`). Graba el orden de clases como metadata
  del `.onnx` y guarda el tokenizador junto al modelo (lo necesita PR2 para Sentis).
- [x] 1.4 `train.py` imprime `classification_report` de scikit-learn **por clase** (precision,
  recall, F1, support) por separado para `Intent` y para `Tone` sobre el split de
  validacion, mas accuracy global, mas una nota explicita de que `Tone.Empatico` no es
  fiable con este corpus.
- [x] 1.5 `Training/Nlu/README.md` — instalacion (`pip install -r requirements.txt`),
  `prepare_dataset.py` -> `train.py`, tabla de encoders candidatos (MiniLM / DistilBERT
  multilingue / TinyBERT), como leer las metricas por clase, donde queda el `.onnx` +
  tokenizador, nota clara de que la precision en clases con pocos ejemplos (`Tone.Empatico`)
  sera mala hasta que `Data/Corpus/PENDIENTE-AMPLIACION.md` se resuelva, y que 1.6/1.7 son
  compuertas humanas.

### Supporting (dentro de la frontera `Training/Nlu/`, no listado en tasks.md)
- [x] `Training/Nlu/.gitignore` — excluye `data/`, `__pycache__/`, `.venv/`, `*.onnx`,
  `tokenizer/` para que las corridas de las compuertas humanas (1.6/1.7) no generen ruido
  commiteable.

## Human Gates — NOT done (require GPU + descarga del encoder + criterio humano)

- [ ] 1.6 Correr `train.py` de punta a punta sobre el corpus actual como prueba de humo del
  pipeline. Requiere GPU y descarga del encoder desde Hugging Face. **Produce el `.onnx` del
  que depende PR2.** `prepare_dataset.py` YA se corrio de verdad sobre el corpus real (ver
  Work Unit Evidence); falta la corrida de `train.py`.
- [ ] 1.7 Confirmar reproducibilidad: correr `train.py` dos veces sobre el mismo corpus y
  hacer diff del bloque "Chequeo de reproducibilidad" (6 frases fijas del dominio, semilla
  `--seed 42`) verificando que `Intent`/`Tone` salen identicos.

## Files Changed

| File | Action | Lines | What |
|---|---|---|---|
| `Training/Nlu/requirements.txt` | Created | 13 | Dependencias Python con version fijada + comentario por dependencia |
| `Training/Nlu/prepare_dataset.py` | Created | 174 | Carga/unifica/valida corpus; split estratificado con degradacion para clases singleton |
| `Training/Nlu/train.py` | Created | 271 | Encoder congelado (CLI arg) + dos cabezas sobre embedding compartido; per-class metrics; export ONNX; semilla fija |
| `Training/Nlu/README.md` | Created | 107 | Como instalar/correr/leer metricas; encoders candidatos; caveat `Tone.Empatico`; compuertas humanas |
| `Training/Nlu/.gitignore` | Created | 13 | Excluye artefactos generados (`data/`, `*.onnx`, `tokenizer/`, `__pycache__/`) |
| `openspec/.../tasks.md` | Modified | +20/-11 | Marca `[x]` Fase 0 y 1.1-1.5; deja 1.6/1.7 con nota de compuerta humana |
| `openspec/.../apply-progress.md` | Created | este archivo | Progreso del batch PR1 |

Total nuevo autorado: ~578 lineas (Python + README + .gitignore) + ~31 en tasks.md.

## Work Unit Evidence (all-modes hard gate)

| Evidence | Value |
|---|---|
| Focused test command y resultado exacto | `python -m py_compile Training/Nlu/prepare_dataset.py Training/Nlu/train.py` -> `PY_COMPILE OK` (exit 0), ambos parsean con `ast.parse` -> `AST PARSE OK`. Ademas `python Training/Nlu/prepare_dataset.py` corrido de verdad contra el corpus real: exit 0, 361 entradas -> train 287 / val 74 (~20.5%), `intent` estratificado 47-49 / 12-13 por clase, degradacion avisada por stderr para 3 clases singleton (`AportaInformacion\|\|Respetuoso`, `Empatia\|\|Empatico`, `PreguntaFueraDeTema\|\|Agresivo`) + aviso extra de `Tone.Empatico`. Split determinista (seed 42). |
| Runtime harness command/scenario y resultado exacto | `python train.py --encoder <hf-id>` de punta a punta = **compuerta humana 1.6** (requiere GPU + descarga del encoder + `torch`/`transformers` instalados). NO ejecutado por el agente. `prepare_dataset.py` no tiene frontera de runtime pesada (solo stdlib) y SI se ejecuto de verdad. |
| Rollback boundary | Borrar `Training/Nlu/` completo y revertir los marks `[x]` de `tasks.md` (+ borrar `apply-progress.md`). Ningun modulo de `Runtime/` se ve afectado; ningun `.onnx` commiteado todavia; cero cambio de contrato M0. |

## Deviations from Design

Ninguna en el enfoque tecnico: encoder congelado (AD1), dos cabezas sobre un
embedding compartido (AD2), export a ONNX (AD3), metricas por clase fuera de NUnit
(AD7) — todo implementado como en `design.md`. Adiciones menores no contempladas
explicitamente en `tasks.md` pero coherentes con el diseno:

1. `train.py` guarda el tokenizador junto al `.onnx` y graba el orden de clases
   como metadata del `.onnx`. Motivo: PR2 necesita el tokenizador para Sentis y no
   debe depender del orden implicito del script; de-riesga el mapeo indice->enum.
2. `.gitignore` en `Training/Nlu/` (ver arriba). Motivo: higiene para las
   compuertas humanas.
3. `prepare_dataset.py` escribe `split_summary.json` ademas de los `.jsonl`.
   Motivo: dar visibilidad de la distribucion por clase sin re-parsear.

## Issues Found

- **Tamano de PR1 vs. forecast**: `tasks.md` -> Review Workload Forecast estimaba
  "PR1 ~150 (Python)". El pipeline real (validacion de esquema + estratificacion
  con degradacion + entrenamiento de dos cabezas + metricas por clase + export
  ONNX + reproducibilidad) son ~578 lineas nuevas en 5 archivos. Supera el
  presupuesto de 400 lineas de revision y el `--max-changed-lines 300` del ledger.
  PR1 ya es la rebanada autonoma minima del chain stacked-to-main (los 3 scripts +
  requirements se necesitan juntos para que el pipeline corra); no se puede
  sub-rebanar sin romper "scope autonomo con verificacion". Se procede segun el
  Review Workload Guard (la estrategia resuelve a chained/stacked slices) y se deja
  constancia para que la revision humana lo pondere.
- El `--encoder` no tiene default a proposito (tarea 1.3). Correr `train.py` sin
  `--encoder` falla con mensaje claro de argparse — es el comportamiento buscado.
- Sentis 2.6.1 y el `--opset` (default 14) del export: la compatibilidad de
  operadores del encoder elegido se valida en el spike de PR2 (tarea 2.7), no en
  PR1. `train.py` fuerza `attn_implementation="eager"` para un grafo mas portable.

## Remaining Tasks (change-wide)

- [ ] 1.6, 1.7 — compuertas humanas de PR1 (arriba). Producen el `.onnx` para PR2.
- [ ] Fase 2 (PR2): `BertIntentClassifier` real + wiring Sentis + LFS + `package.json`.
- [ ] Fase 3 (PR3): documentacion (`Docs/MODULES.md`, `PENDIENTE-AMPLIACION.md`).
- [ ] Fase 4: cierre por PR (git add acotado, checklist, archivado del cambio).

## Workload / PR Boundary

- Mode: stacked PR slice (PR1 de 3), `stacked-to-main`.
- Current work unit: "Pipeline de entrenamiento offline (Python)".
- Boundary: empieza de un `Training/Nlu/` inexistente; termina con
  `requirements.txt` + `prepare_dataset.py` + `train.py` + `README.md` (+ `.gitignore`)
  coherentes con `tasks.md` Fase 1. No incluye correr el entrenamiento (compuerta humana).
- Estimated review budget impact: ~578 lineas nuevas + ~31 en tasks.md; por encima
  del presupuesto de 400 — ver Issues. Cero C# de produccion, cero ejecucion de
  runtime, cero cambio de contrato.

## Status

Fase 0: 6/6 leidas. Fase 1: 5/7 implementables hechas (1.1-1.5); 1.6-1.7 son
compuertas humanas. Listo para verify de PR1 (los scripts), sabiendo que el `.onnx`
solo existe tras la compuerta humana 1.6.
