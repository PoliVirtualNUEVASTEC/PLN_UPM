# Apply Progress — 2026-09-09-m2-clasificador-bert-reducido

**Change**: 2026-09-09-m2-clasificador-bert-reducido (M2, modulo de IA)
**Artifact store**: hybrid
**Delivery**: auto-chain, stacked-to-main (PR1 -> main, PR2 -> PR1, PR3 -> PR2).

---

## Batch: apply PR1 — pipeline de entrenamiento offline (`Training/Nlu/`, Python)

**Date**: 2026-09-09

### Mode Resolution

- Repo `strict_tdd: true`, pero el unico test runner configurado es Unity Test
  Framework EditMode, que no ejecuta Python.
- PR1 es Python puro, fuera de Unity (`tasks.md` Fase 1, nota final): "no requiere
  Test Runner ni Unity Editor para validarse, solo correr los scripts y revisar la
  salida". No hay suite EditMode para PR1.
- Resolucion: **Strict TDD RED->GREEN->REFACTOR con Unity no aplica a los scripts
  Python de PR1.** Se aplica el hard gate de todos los modos (Work Unit Evidence).
  El RED->GREEN de contrato (`BertIntentClassifierTests : IntentClassifierContract`)
  es de PR2, no de PR1.

### Completed Tasks (PR1)

#### Fase 0 — Guardrails (leidos y respetados)
- [x] 0.1 Frontera de escritura PR1: solo `Training/Nlu/` + el directorio del cambio. Respetado.
- [x] 0.2 Frontera de escritura PR2 (leida; PR1 no toca `Runtime/`).
- [x] 0.3 Cero cambio en `Runtime/Core/` (`Ports.cs`/`Dtos.cs`/`Enums.cs`). Respetado.
- [x] 0.4 `Tests/EditMode/Core/IntentClassifierContract.cs` no se toca. Respetado (PR1 no crea pruebas).
- [x] 0.5 M2 es IA: sin `/sdd-ff`. Respetado.
- [x] 0.6 Corpus `Data/Corpus/*.json` es de M3: PR1 lo consume tal cual, no lo edita. Respetado.

#### Fase 1 — Pipeline de entrenamiento offline (PR1)
- [x] 1.1 `Training/Nlu/requirements.txt` — 8 dependencias con version fijada exacta
  (`torch==2.5.1`, `transformers==4.46.3`, `tokenizers==0.20.3`, `sentencepiece==0.2.0`,
  `onnx==1.17.0`, `onnxruntime==1.20.1`, `scikit-learn==1.5.2`, `numpy==1.26.4`),
  cada una con comentario de para que es.
- [x] 1.2 `Training/Nlu/prepare_dataset.py` — carga y unifica `emergencia.json` +
  `juntas.json`; valida esquema (`text`/`intent`/`tone`/`scenario`/`labeler` no vacios) y
  que `intent`/`tone` sean miembros de los enums de `NpcAi.Core` (lista espejada de
  `Runtime/Core/Enums.cs`); split estratificado por la clave `intent||tone` con
  **degradacion con gracia**: clases con <2 ejemplos caen enteras a train y se avisa por
  stderr, sin abortar. CLI: `--corpus-dir`, `--out-dir`, `--val-fraction`, `--seed`, con
  defaults resueltos relativos a la raiz del repo.
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
- [x] 1.6 y 1.7 — ver "Batch: apply PR2" abajo: se completaron por el usuario en sesion de
  chat (2026-09-14), no por un agente. Evidencia registrada ahi.

#### Supporting (dentro de la frontera `Training/Nlu/`, no listado en tasks.md)
- [x] `Training/Nlu/.gitignore` — excluye `data/`, `__pycache__/`, `.venv/`, `*.onnx`,
  `tokenizer/` para que las corridas de las compuertas humanas (1.6/1.7) no generen ruido
  commiteable.

### Files Changed (PR1)

| File | Action | Lines | What |
|---|---|---|---|
| `Training/Nlu/requirements.txt` | Created | 13 | Dependencias Python con version fijada + comentario por dependencia |
| `Training/Nlu/prepare_dataset.py` | Created | 174 | Carga/unifica/valida corpus; split estratificado con degradacion para clases singleton |
| `Training/Nlu/train.py` | Created | 271 | Encoder congelado (CLI arg) + dos cabezas sobre embedding compartido; per-class metrics; export ONNX; semilla fija |
| `Training/Nlu/README.md` | Created | 107 | Como instalar/correr/leer metricas; encoders candidatos; caveat `Tone.Empatico`; compuertas humanas |
| `Training/Nlu/.gitignore` | Created | 13 | Excluye artefactos generados (`data/`, `*.onnx`, `tokenizer/`, `__pycache__/`) |

Total nuevo autorado PR1: ~578 lineas (Python + README + .gitignore).

### Work Unit Evidence (PR1)

| Evidence | Value |
|---|---|
| Focused test command y resultado exacto | `python -m py_compile Training/Nlu/prepare_dataset.py Training/Nlu/train.py` -> `PY_COMPILE OK` (exit 0). `python Training/Nlu/prepare_dataset.py` corrido de verdad contra el corpus real: exit 0, 361 entradas -> train 287 / val 74. Split determinista (seed 42). |
| Runtime harness command/scenario y resultado exacto | `python train.py --encoder <hf-id>` de punta a punta = compuerta humana 1.6 (requiere GPU + descarga del encoder). NO ejecutado por el agente en PR1; SI ejecutado despues por el usuario (ver Batch PR2). |
| Rollback boundary | Borrar `Training/Nlu/` completo. Ningun modulo de `Runtime/` se ve afectado; cero cambio de contrato M0. |

### Issues Found (PR1)

- **Tamano de PR1 vs. forecast**: `tasks.md` -> Review Workload Forecast estimaba "PR1 ~150
  (Python)"; el pipeline real son ~578 lineas. Se procede segun el Review Workload Guard
  (auto-chain ya resuelto) y se deja constancia para revision humana.
- `--encoder` no tiene default a proposito (tarea 1.3): comportamiento buscado.
- Compatibilidad de operadores de Sentis 2.6.1 se valida en el spike de PR2 (tarea 2.7), no en PR1.

---

## Batch: apply PR2 — `BertIntentClassifier` real + wiring Sentis

**Date**: 2026-09-14

### Mode Resolution

- Strict TDD activo; test runner = Unity Test Framework EditMode, **GUI-only** (`Window >
  General > Test Runner`), sin CLI disponible para el agente en este entorno.
- RED (2.4) y GREEN (2.5) se escribieron siguiendo el ciclo, pero el paso "correr el Test
  Runner y confirmar verde" es una **compuerta humana**, exactamente igual que las corridas
  de `train.py` de PR1. El agente NO ejecuto Unity ni el Test Runner en ningun momento de
  este batch — no se afirma en ningun punto de este reporte que las pruebas pasaron; se
  afirma unicamente que fueron escritas siguiendo RED->GREEN y que compilan segun el
  razonamiento sobre la API real de Sentis (ver investigacion abajo).

### Bookkeeping previo (tasks 1.6 / 1.7, completadas por el usuario, no por un agente)

- **1.6 (smoke test end-to-end)**: `train.py` corrido de punta a punta para 4 candidatos de
  encoder, mismo corpus/split (`Training/Nlu/data/`, seed 42, 30 epochs, hiperparametros por
  defecto), sobre el corpus YA ampliado y rebalanceado (`Data/Corpus/emergencia.json` +
  `juntas.json`, 1200 entradas / 100 por intencion) — **desviacion positiva** respecto al
  corpus de "30/intencion" con el que se redacto originalmente esta tarea; se deja constancia
  explicita, no es un problema.

  | Encoder | Intent acc | Intent macro-F1 | Tone acc | Tone macro-F1 | Params (peso en disco) |
  |---|---|---|---|---|---|
  | sentence-transformers/paraphrase-multilingual-MiniLM-L12-v2 | 0.771 | 0.658 | 0.633 | 0.631 | ~118M (471MB fp32) |
  | distilbert-base-multilingual-cased | 0.792 | 0.679 | 0.729 | 0.720 | ~135M (542MB fp32) |
  | microsoft/Multilingual-MiniLM-L12-H384 | 0.767 | 0.656 | 0.646 | 0.628 | ~118M (471MB fp32) |
  | huawei-noah/TinyBERT_General_4L_312D | 0.600 | 0.510 | 0.550 | 0.535 | ~15.7M (62.7MB fp32) |

  Elegido para este PR2: **distilbert-base-multilingual-cased** (mejor en las 4 metricas,
  especialmente Tone). Ninguno de los 3 candidatos grandes cae en el rango "~20-60M
  parametros" de `proposal.md` (solo TinyBERT, que rindio claramente peor) — desviacion
  conocida y aceptada, pendiente del spike de Sentis/Quest (tarea 2.7); la eleccion final de
  encoder queda abierta hasta que ese spike confirme que corre aceptablemente on-device.
- **1.7 (reproducibilidad)**: `sentence-transformers/paraphrase-multilingual-MiniLM-L12-v2`
  corrido dos veces con `--seed 42` sobre el mismo corpus; la curva de loss por epoca, ambos
  bloques de `classification_report`, y el bloque "Chequeo de reproducibilidad" (6
  `REPRO_PHRASES` fijas) salieron byte-identicos entre las dos corridas. Determinismo
  confirmado.

### Completed Tasks (PR2)

- [x] 2.1 `com.unity.ai.inference: 2.6.1` agregado a `dependencies` en `package.json` (antes
  solo aparecia como palabra suelta en `keywords`).
- [x] 2.2 Regla LFS `Runtime/Nlu/Models/**/*.onnx` agregada en `.gitattributes`, mismo patron
  que M1 (Vosk). Confirmado activo con `git lfs track` (ver Work Unit Evidence).
- [x] 2.3 Reemplazados los artefactos que habia en `Runtime/Nlu/Models/` (que resultaron ser
  del candidato MiniLM `minilm-ms.onnx`, no del elegido — coincidencia de tamano de archivo
  confirmada byte a byte, ver Deviations) por el candidato correcto:
  `Training/Nlu/candidates/distilbert.onnx` -> `Runtime/Nlu/Models/intent-tone-classifier.onnx`,
  `Training/Nlu/candidates/distilbert-tokenizer/` -> `Runtime/Nlu/Models/tokenizer/`. NO
  commiteado todavia (Fase 4, accion del autor).
- [x] 2.4 (RED) `Tests/EditMode/Nlu/BertIntentClassifierTests.cs : IntentClassifierContract`
  creado. `CreateSubject()` construye `new BertIntentClassifier("Packages/com.poli.npc-ai/
  Runtime/Nlu/Models/intent-tone-classifier.onnx")`. No compilaba antes de 2.5 (la clase no
  existia) — RED valido.
- [x] 2.5 (GREEN) `Runtime/Nlu/BertIntentClassifier.cs` creado, implementando
  `IIntentClassifier` + `IDisposable` via Sentis. Detalle tecnico abajo.
- [x] 2.6 Determinismo: mitigado por diseno con `BackendType.CPU` explicito en el `Worker`
  (no `GPUCompute`), precisamente para evitar el riesgo de orden de reduccion no determinista
  en GPU que la tarea senala. Confirmacion final en el Test Runner real queda en 2.7.
- [ ] 2.7 — **NO intentada por el agente, es la compuerta humana explicita de esta tarea**: correr
  la bateria de pruebas en el Test Runner real de Unity para confirmar que Sentis 2.6.1 soporta
  todos los operadores del grafo de distilbert (spike tecnico "1.2" del cronograma). Es
  literalmente lo que el usuario pidio probar en las Quest a continuacion de este batch.
- [x] 2.8 Confirmado con `rg BertIntentClassifier` en todo el repo: solo aparecen la clase
  nueva, su prueba, y menciones en prosa de documentos OpenSpec (este cambio y M6/M15). Cero
  wiring/instanciacion automatica en `Runtime/`.

### Detalle tecnico de 2.5 (para que el revisor humano de 2.7 sepa que validar)

Investigacion previa a escribir codigo (leido en el paquete `com.unity.ai.inference@2.6.1`
instalado en `Library/PackageCache/`, no supuesto):

1. **Namespace real**: el paquete Sentis 2.6.1 se llama en codigo `Unity.InferenceEngine`
   (no `Unity.Sentis`), con asmdefs `Unity.InferenceEngine` y
   `Unity.InferenceEngine.Tokenization`. Agregadas ambas referencias a
   `Runtime/Nlu/NpcAi.Nlu.asmdef`.
2. **Tokenizador**: el paquete trae un tokenizador WordPiece/HuggingFace completo
   (`Unity.InferenceEngine.Tokenization.Parsers.HuggingFace.HuggingFaceParser`, con un sample
   oficial `Samples~/Tokenizer - All Mini LM` para un caso casi identico). Se usa
   `HuggingFaceParser.GetDefault().Parse(tokenizerJsonTexto)` para parsear
   `Runtime/Nlu/Models/tokenizer/tokenizer.json` directo — **no se reimplemento WordPiece a
   mano** (evita un riesgo grande de bugs de tokenizacion). Verificado con Python que el
   `tokenizer.json` de distilbert es `model.type=WordPiece`, `normalizer=BertNormalizer`,
   `pre_tokenizer=BertPreTokenizer`, `post_processor=TemplateProcessing`,
   `decoder=WordPiece` — los mismos tipos que el sample oficial construye a mano, lo que da
   confianza en que el parser automatico los reconoce.
3. **Orden de clases / metadata ONNX (desviacion de design.md)**: `design.md` proponia leer
   `intent_classes`/`tone_classes` desde los `metadata_props` que `train.py` graba en el
   `.onnx`. Confirmado leyendo `Editor/ONNX/ONNXModelConverter.cs` del paquete instalado que
   esos metadata solo se emiten via un evento Editor-only (`MetadataLoaded`), **no accesible
   desde `Model`/`Worker` en tiempo de ejecucion** — no hay API publica de runtime para
   leerlos. Se opto por usar el indice de salida directo (`(Intent)argmax`, `(Tone)argmax`),
   apoyandose en que `train.py` YA deja constancia explicita de que el orden de
   `INTENT_CLASSES`/`TONE_CLASSES` coincide 1:1 con `Runtime/Core/Enums.cs` — mismo efecto
   practico (nunca se hardcodea un mapeo independiente del enum real), sin depender de una
   API que no existe.
4. **Carga del `.onnx` (limitacion real, no resuelta del todo)**: Sentis no tiene un
   conversor ONNX->`Model` en tiempo de ejecucion fuera del Editor (el unico importador,
   `ONNXModelConverter`, vive en `Editor/ONNX/`, Editor-only). Dentro del Editor — que es el
   contexto real de `Tests/EditMode` — el `.onnx` commiteado ya fue importado
   automaticamente por Unity a un `ModelAsset` nativo, y `BertIntentClassifier` lo carga via
   `UnityEditor.AssetDatabase.LoadAssetAtPath<ModelAsset>(modelPath)` (guardado tras
   `#if UNITY_EDITOR`, por lo que compila igual en un build de jugador). **Fuera del Editor
   (build real, wiring de M11) esta via NO funciona todavia** — se deja un fallback a
   `ModelLoader.Load(path)` (que solo lee el formato binario propio `.sentis`, no ONNX
   crudo) documentado como pendiente de resolver cuando exista la integracion real. Esto es
   exactamente el tipo de cosa que el spike de la tarea 2.7 deberia terminar de confirmar o
   refutar en la practica.
5. **Tensores/inferencia**: `input_ids`/`attention_mask` como `Tensor<int>` (Sentis
   representa enteros como `DataType.Int`, 32 bits, incluso cuando el ONNX declara int64 —
   confirmado por el codigo de casteo `IntFromInt64` en el importador); `Worker` con
   `BackendType.CPU`; `SetInput`/`Schedule`/`PeekOutput("intent_probs"|"tone_probs")` +
   `DownloadToArray()` (bloqueante, copia propia, no requiere disponer el tensor de salida —
   el `Worker` sigue siendo dueno de esos, solo se disponen los tensores de entrada que crea
   este adaptador). El mean pooling enmascarado YA esta dentro del grafo ONNX exportado por
   `train.py`; el adaptador no lo reimplementa.

Todo lo anterior es razonamiento sobre la API publica leida directamente del paquete
instalado, no ejecucion real — la tarea 2.7 es exactamente la compuerta que confirma o
refuta estas cinco decisiones en la practica.

### Files Changed (PR2)

| File | Action | What |
|---|---|---|
| `package.json` | Modified | Agregado `dependencies.com.unity.ai.inference = "2.6.1"` |
| `.gitattributes` | Modified | Regla LFS para `Runtime/Nlu/Models/**/*.onnx` |
| `Runtime/Nlu/Models/intent-tone-classifier.onnx` | Replaced (untracked, LFS pendiente) | MiniLM (equivocado) -> distilbert (elegido), 539125543 bytes |
| `Runtime/Nlu/Models/tokenizer/*` | Replaced (untracked) | Tokenizador sentencepiece equivocado -> tokenizador WordPiece correcto de distilbert (`special_tokens_map.json`, `tokenizer.json`, `tokenizer_config.json`, `vocab.txt`) |
| `Runtime/Nlu/NpcAi.Nlu.asmdef` | Modified | + referencias `Unity.InferenceEngine`, `Unity.InferenceEngine.Tokenization` |
| `Runtime/Nlu/BertIntentClassifier.cs` | Created (~155 lineas) | Implementacion real de `IIntentClassifier` via Sentis |
| `Tests/EditMode/Nlu/BertIntentClassifierTests.cs` | Created (~65 lineas) | Hereda `IntentClassifierContract` + 3 pruebas propias |
| `openspec/.../tasks.md` | Modified | `[x]` en 1.6, 1.7, 2.1-2.6, 2.8; 2.7 sigue `[ ]` |
| `openspec/.../apply-progress.md` | Modified | Este archivo — fusiona PR1 (existente) + PR2 (nuevo) |

### TDD Cycle Evidence (Strict TDD)

| Task | Test File | Layer | Safety Net | RED | GREEN | TRIANGULATE | REFACTOR |
|---|---|---|---|---|---|---|---|
| 2.4/2.5 | `Tests/EditMode/Nlu/BertIntentClassifierTests.cs` | EditMode (heredado de `IntentClassifierContract`, 8 pruebas) + 3 propias | N/A (archivo nuevo) | Escrito — referenciaba `BertIntentClassifier`, inexistente hasta 2.5 | Escrito para pasar segun lectura de API; **NO ejecutado** (Test Runner es GUI-only, compuerta humana 2.7) | Cubierto por las 8 pruebas heredadas del contrato (cada una es un escenario distinto de la spec) + 3 pruebas propias (modelo carga, ruta inexistente, clasificacion de dominio valida) | Extraidas constantes de nombres de tensor, helper `MaximoIndice`, helper `Acotar`, helpers `AIntent`/`ATone` para evitar magia numerica |

**Nota de honestidad obligatoria**: la columna GREEN de la tabla de arriba NO significa "el
Test Runner confirmo verde". Significa "escrito para pasar, razonado contra la API real del
paquete instalado, pendiente de ejecucion real en el Editor (tarea 2.7)". Esto se declara
explicitamente porque el enunciado de este batch lo exige y porque es la unica lectura
honesta posible sin acceso a Unity Editor.

### Test Summary

- **Total tests escritos**: 8 heredados de `IntentClassifierContract` (sin modificarla) + 3
  propios de `BertIntentClassifierTests` = 11.
- **Total tests confirmados pasando por el agente**: 0 — el agente no tiene Test Runner.
- **Layers usados**: EditMode (Unity Test Framework), 11.
- **Pure functions creadas**: `MaximoIndice`, `Acotar`, `AIntent`, `ATone`, `LatenciaMs`,
  `AArregloDeInts` — todas estaticas y sin efectos secundarios.

### Work Unit Evidence (all-modes hard gate, PR2)

| Evidence | Value |
|---|---|
| Focused test command y resultado exacto | Ninguno ejecutable por el agente: Unity Test Framework EditMode es GUI-only en este entorno (`Window > General > Test Runner`), sin CLI disponible. Verificacion de compilacion hecha por lectura directa de la API del paquete instalado en `Library/PackageCache/com.unity.ai.inference@9a123aee5df7/` (ver "Detalle tecnico de 2.5"), no por ejecucion. |
| Runtime harness command/scenario y resultado exacto | `Window > General > Test Runner > EditMode > NpcAi.Nlu.Tests.BertIntentClassifierTests` = compuerta humana (tarea 2.7). NO ejecutado por el agente. Es el proximo paso literal que el usuario pidio ("probemos el modelo dentro de las Quest"), y este PR2 deja el codigo listo para ese paso pero no lo reemplaza. |
| Rollback boundary | Borrar `Runtime/Nlu/BertIntentClassifier.cs`, `Tests/EditMode/Nlu/BertIntentClassifierTests.cs`, `Runtime/Nlu/Models/`; revertir `package.json`, `.gitattributes`, `Runtime/Nlu/NpcAi.Nlu.asmdef`. `NluIntentClassifier` (reglas por palabra clave) sigue siendo el motor real disponible, exactamente igual que hoy — ningun otro modulo referencia `BertIntentClassifier` (tarea 2.8). |

### Deviations from Design (PR2)

1. **Artefactos equivocados encontrados en `Runtime/Nlu/Models/` antes de empezar**: el
   `.onnx` (470376934 bytes) y el `tokenizer/` que ya estaban en esa carpeta correspondian
   al candidato `minilm-ms.onnx` (mismo tamano exacto en bytes, tokenizador basado en
   sentencepiece de vocabulario ~250k, incompatible con WordPiece), no al candidato elegido
   (`distilbert`, 539125543 bytes, tokenizador WordPiece de 119547 entradas). Se
   reemplazaron por los correctos (tarea 2.3) antes de escribir el resto del codigo. No se
   modifico ni se investigo por que habian quedado ahi (posible residuo de una prueba manual
   anterior a este batch) — se deja constancia porque es relevante para cualquiera que revise
   el diff de `Runtime/Nlu/Models/`.
2. **Metadata ONNX no legible desde runtime (ver punto 3 de "Detalle tecnico")**: desviacion
   documentada de la nota de de-riesgo de `design.md`, con mismo efecto practico via el orden
   de indice ya alineado con `Runtime/Core/Enums.cs`.
3. **Carga de `.onnx` por ruta de archivo, limitada al Editor (ver punto 4 de "Detalle
   tecnico")**: hallazgo tecnico real, no una desviacion de conveniencia — Sentis
   estructuralmente no ofrece conversion ONNX->Model en tiempo de ejecucion fuera del Editor.
   La firma del constructor (`BertIntentClassifier(string modelPath)`) se mantuvo exactamente
   como la fija `design.md`; el gap queda documentado para cuando M11 haga el wiring real,
   NO resuelto en este PR2 (fuera de alcance: tasks.md 2.8 solo pide confirmar que nadie
   instancia la clase todavia).
4. **`IDisposable` agregado a `BertIntentClassifier`**: no esta en la interfaz
   `IIntentClassifier` ni en `design.md`, pero es necesario porque la clase retiene un
   `Worker` de Sentis (recurso no administrado, memoria GPU/CPU nativa). Adicion aditiva,
   no rompe el contrato — mismo patron que `VoskRecognitionEngine` en `Runtime/Speech/`.

### Issues Found (PR2)

- El agente no puede confirmar que las 11 pruebas efectivamente pasan — ver Mode Resolution
  y TDD Cycle Evidence. Este es el issue mas importante del batch: todo lo demas depende de
  que la tarea 2.7 (compuerta humana) confirme o refute las cinco decisiones tecnicas del
  "Detalle tecnico de 2.5".
- El modelo elegido (distilbert, ~135M parametros / 542MB fp32) es sustancialmente mas
  grande que el rango "~20-60M" de `proposal.md` — ya senalado como desviacion aceptada y
  pendiente del spike en la bookkeeping de 1.6 arriba; repetido aqui porque afecta
  directamente la latencia/viabilidad que 2.7 debe evaluar en Quest.

### Remaining Tasks (change-wide)

- [ ] 2.7 — compuerta humana: correr el Test Runner real, confirmar/ajustar las 5 decisiones
  tecnicas de "Detalle tecnico de 2.5", y evaluar viabilidad on-device (Quest) del tamano del
  modelo elegido.
- [ ] Fase 3 (PR3): documentacion (`Docs/MODULES.md`, `PENDIENTE-AMPLIACION.md`).
- [ ] Fase 4: cierre por PR (git add acotado por PR, checklist, commit LFS del `.onnx`,
  archivado del cambio).

### Workload / PR Boundary (PR2)

- Mode: stacked PR slice (PR2 de 3), `stacked-to-main` (PR2 -> PR1).
- Current work unit: "`BertIntentClassifier` real + wiring Sentis".
- Boundary: empieza de `Runtime/Nlu/BertIntentClassifier.cs` inexistente y artefactos
  equivocados en `Runtime/Nlu/Models/`; termina con el adaptador + prueba escritos, el
  `.onnx`/tokenizador correctos en su lugar (sin commitear todavia), `package.json`/
  `.gitattributes`/asmdef actualizados. No incluye correr el Test Runner (compuerta humana
  2.7) ni el wiring real de M11.
- Estimated review budget impact: ~155 lineas nuevas de produccion + ~65 de prueba + diffs
  chicos en `package.json`/`.gitattributes`/asmdef + el binario `.onnx` via LFS (excluido del
  presupuesto de 400 lineas por ser generado, no autorado) — dentro del ~300 estimado en el
  forecast de `tasks.md` para PR2.

## Status

PR1: Fase 0 (6/6) + Fase 1 (7/7, incluyendo 1.6/1.7 via compuerta humana del usuario).
PR2: 7/8 tareas de Fase 2 completas (2.1-2.6, 2.8); 2.7 es la compuerta humana pendiente,
explicitamente no intentada por el agente. Listo para verify de PR2 con esa salvedad
explicita, y para que el usuario corra 2.7 en el Editor real.
