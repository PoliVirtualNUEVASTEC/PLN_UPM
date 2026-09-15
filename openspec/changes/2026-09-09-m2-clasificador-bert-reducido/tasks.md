# Tasks: M2 — Clasificador de intención/tono entrenado (BERT reducido + transfer learning)

## Review Workload Forecast

| Field | Value |
|---|---|
| Estimated changed lines | PR1 ~150 (Python, sin contar pesos descargados), PR2 ~300 (C# + `.onnx` binario vía LFS), PR3 ~120 (docs) |
| 400-line budget risk | Bajo por PR |
| Chained PRs recommended | Sí |
| Suggested split | PR1 → PR2 → PR3 |
| Chain strategy | stacked-to-main (PR1 → main, PR2 → PR1, PR3 → PR2) |

### Suggested Work Units

| Unit | Meta | PR | Prueba enfocada | Frontera de rollback |
|---|---|---|---|---|
| 1 | Pipeline de entrenamiento offline (Python) | PR1 | Manual: correr `train.py` dos veces sobre el mismo corpus y comparar métricas/predicciones | Borrar `Training/Nlu/` completo; ningún módulo de `Runtime/` se ve afectado |
| 2 | `BertIntentClassifier` real + wiring Sentis | PR2 | EditMode: `BertIntentClassifierTests : IntentClassifierContract` | Borrar `Runtime/Nlu/BertIntentClassifier.cs` y `Runtime/Nlu/Models/`; revertir `package.json`/`.gitattributes`; `NluIntentClassifier` sigue siendo el motor real, igual que hoy |
| 3 | Documentación (`Docs/MODULES.md`, `PENDIENTE-AMPLIACION.md`) | PR3 | Manual: revisión de que `Docs/MODULES.md` ya no describe la desviación como "sin documentar" | Revertir el commit de docs; no afecta código |

## Phase 0: Guardrails (leer antes de escribir)

- [x] 0.1 Frontera de escritura de PR1: SOLO `Training/Nlu/` y este directorio de cambio
  (`openspec/changes/2026-09-09-m2-clasificador-bert-reducido/`). Nada en `Runtime/` todavía.
- [x] 0.2 Frontera de escritura de PR2: `Runtime/Nlu/BertIntentClassifier.cs`,
  `Runtime/Nlu/Models/`, `Tests/EditMode/Nlu/BertIntentClassifierTests.cs`, `package.json`,
  `.gitattributes`. **No tocar `Runtime/Nlu/SemanticMatcher.cs`, `ToneAnalyzer.cs`,
  `NluIntentClassifier.cs`, `TextPreprocessor.cs`** (Out of Scope — quedan como referencia).
- [x] 0.3 Cero cambio en `Runtime/Core/Ports.cs`, `Dtos.cs`, `Enums.cs`. `IIntentClassifier`,
  `IntentResult`, `Intent`, `Tone` no cambian una firma. Si algo de este cambio parece requerir
  tocar `Runtime/Core/` o `Runtime/CoreChannels/`, parar y avisar — eso es cambio de contrato y
  necesita co-revisión de todos los dueños de módulo (regla del repo), fuera de lo que este
  cambio autoriza.
- [x] 0.4 `Tests/EditMode/Core/IntentClassifierContract.cs` no se modifica en ningún PR:
  `BertIntentClassifierTests` hereda, no edita, la base.
- [x] 0.5 Este módulo es IA (M2): `/sdd-ff` no aplica — cada PR pasa por el flujo completo
  propuesta → diseño → tareas → revisión humana, sin atajos.
- [x] 0.6 El corpus (`Data/Corpus/*.json`) es de M3, no de este cambio: PR1 lo consume tal cual
  está en `main` en el momento de entrenar; no se edita el corpus dentro de este cambio (ver
  `Data/Corpus/PENDIENTE-AMPLIACION.md`, entregado aparte).

## Phase 1: Pipeline de entrenamiento offline (PR1)

- [x] 1.1 Crear `Training/Nlu/requirements.txt` (framework de entrenamiento, biblioteca del
  encoder pre-entrenado, exportador ONNX — versiones fijadas, no rangos abiertos).
- [x] 1.2 Crear `Training/Nlu/prepare_dataset.py`: carga y unifica
  `Data/Corpus/emergencia.json` + `Data/Corpus/juntas.json`, valida el esquema
  (`text`/`intent`/`tone`/`scenario`/`labeler`), hace el split train/validación (estratificado
  por `intent` y por `tone` en la medida en que el tamaño de cada clase lo permita).
- [x] 1.3 Crear `Training/Nlu/train.py`: carga el encoder pre-entrenado candidato (parámetro de
  línea de comandos, no hardcodeado — para poder probar los 2-3 candidatos del spike de Fase 1
  sin editar el script), lo congela, entrena la cabeza de `Intent` y la cabeza de `Tone` sobre el
  mismo embedding, valida contra el split de prueba, exporta a ONNX.
- [x] 1.4 El script de validación imprime precisión por clase (no solo agregada) — necesario para
  ver explícitamente qué tan mal le va a `Tone.Empatico` mientras el corpus no se amplíe (ver
  `Risks` en `proposal.md`).
- [x] 1.5 Crear `Training/Nlu/README.md`: cómo instalar dependencias, cómo correr
  `prepare_dataset.py` → `train.py`, qué encoders probar, cómo leer las métricas impresas, dónde
  queda el `.onnx` resultante.
- [x] 1.6 Correr el pipeline una vez de punta a punta sobre el corpus actual (30/intención/
  escenario) como prueba de humo del pipeline mismo — **no** como entrega de un modelo de
  producción; documentar en el resultado que la precisión en clases con pocos ejemplos
  (`Tone.Empatico` sobre todo) será baja hasta que `Data/Corpus/PENDIENTE-AMPLIACION.md` se
  resuelva.
  <!-- COMPUERTA HUMANA (usuario, 2026-09-14): corrida de punta a punta para 4 candidatos de
  encoder sobre el corpus YA ampliado (1200 entradas / 100 por intencion, no el corpus de 30/
  intencion original de esta tarea — desviacion positiva, ver apply-progress.md "Batch: apply
  PR2"). Elegido distilbert-base-multilingual-cased (mejor accuracy/F1 en Intent y Tone de los
  4 candidatos). Ninguno de los 3 candidatos "grandes" cae en el rango ~20-60M de proposal.md;
  queda pendiente de confirmar en el spike de Sentis (tarea 2.7) si esto es aceptable on-device. -->
- [x] 1.7 Confirmar reproducibilidad (Success Criteria de `proposal.md`): correr `train.py` dos
  veces sobre el mismo corpus y comparar que el modelo resultante da el mismo `Intent`/`Tone`
  para un conjunto fijo de frases de prueba.
  <!-- COMPUERTA HUMANA (usuario, 2026-09-14): train.py corrido dos veces con
  sentence-transformers/paraphrase-multilingual-MiniLM-L12-v2 --seed 42 sobre el mismo corpus;
  curva de loss por epoca, ambos classification_report y el bloque "Chequeo de
  reproducibilidad" (6 REPRO_PHRASES) salieron byte-identicos en ambas corridas. Determinismo
  confirmado. -->


> PR1 es Python puro, fuera de Unity: no requiere Test Runner ni Unity Editor para validarse,
> solo correr los scripts y revisar la salida.

## Phase 2: `BertIntentClassifier` real + wiring Sentis (PR2)

- [x] 2.1 Agregar `com.unity.ai.inference` (Sentis) a `dependencies` en `package.json` — hoy solo
  existe como palabra suelta en `keywords`, no como dependencia real.
- [x] 2.2 Agregar la regla LFS para `Runtime/Nlu/Models/**/*.onnx` en `.gitattributes`, siguiendo
  el precedente ya usado para los binarios de Vosk de M1. Confirmar con `git lfs track` que la
  regla queda activa antes de commitear el primer `.onnx`.
  <!-- apply PR2 (2026-09-14): confirmado con `git lfs track` — "runtime/nlu/models/**/*.onnx"
  listado como patron activo, junto a los 3 patrones de M1. -->
- [x] 2.3 Copiar el `.onnx` producido por PR1 (Fase 1) a `Runtime/Nlu/Models/
  intent-tone-classifier.onnx` y commitearlo vía LFS.
  <!-- apply PR2 (2026-09-14): copiado el candidato elegido (distilbert-base-multilingual-cased,
  Training/Nlu/candidates/distilbert.onnx + distilbert-tokenizer/), reemplazando el contenido
  MiniLM que habia quedado (por error, de una corrida anterior) en Runtime/Nlu/Models/. El commit
  via LFS queda para Fase 4 (accion del autor), como indica la nota de la tarea. -->
- [x] 2.4 RED: `Tests/EditMode/Nlu/BertIntentClassifierTests.cs : IntentClassifierContract` —
  `CreateSubject()` carga el `.onnx` commiteado vía Sentis.
- [x] 2.5 GREEN: Crear `Runtime/Nlu/BertIntentClassifier.cs` implementando `IIntentClassifier`
  (constructor recibe `modelPath`; `IsReady` falso hasta terminar de cargar; `Classify(text)` con
  la misma firma que hoy) hasta pasar la batería heredada completa
  (`Reporta_si_esta_listo_sin_lanzar`, `Texto_vacio_devuelve_Desconocida_y_no_lanza`,
  `La_confianza_siempre_esta_entre_cero_y_uno`, `La_latencia_nunca_es_negativa`,
  `Es_determinista_para_la_misma_entrada`, `Nunca_lanza_con_entradas_raras`,
  `Classify_no_lanza_en_ningun_estado`, `Un_clasificador_no_listo_devuelve_Unknown`).
  <!-- apply PR2 (2026-09-14): implementado usando el Tokenizer HuggingFace incluido en
  com.unity.ai.inference 2.6.1 (Unity.InferenceEngine.Tokenization.Parsers.HuggingFace.
  HuggingFaceParser, parsea tokenizer.json directo, sin reimplementar WordPiece a mano). NO
  ejecutado en el Test Runner real por el agente (sin acceso al Editor) — pendiente de la
  compuerta humana 2.7. Ver Deviations en apply-progress.md ("Batch: apply PR2") por dos
  hallazgos tecnicos relevantes: (a) Sentis no expone los metadata_props del ONNX en su API de
  runtime (se uso el orden de indice directo, ya alineado 1:1 con Enums.cs); (b) la carga de un
  .onnx crudo por ruta de archivo solo funciona dentro del Editor via AssetDatabase — fuera del
  Editor (build real) esta pendiente de resolver en el wiring de M11. -->
- [x] 2.6 Prestar atención específica a `Es_determinista_para_la_misma_entrada`: si la inferencia
  de Sentis introduce no-determinismo (orden de reducción en GPU, por ejemplo), esta prueba lo
  detecta — resolverlo antes de mergear, no documentar la falla como aceptable.
  <!-- apply PR2 (2026-09-14): mitigado por diseno usando BackendType.CPU (no GPUCompute) en el
  Worker, precisamente para evitar el riesgo de orden de reduccion no determinista en GPU. Sigue
  pendiente CONFIRMAR en el Test Runner real (compuerta humana 2.7) que esto basta en la
  practica; si no basta, es motivo de volver a esta tarea antes de mergear, como pide el
  enunciado. -->
- [x] 2.7 MANUAL (Editor de Unity): confirmar que Sentis soporta todos los operadores del encoder
  elegido, corriendo la batería de pruebas en el Test Runner real (no solo revisión de código) —
  este es el spike técnico ya planeado en el cronograma ("1.2 Spike técnico de Sentis y selección
  del stack"); si el encoder candidato falla aquí, volver a Fase 1 con otro candidato antes de
  continuar PR2.
  <!-- compuerta humana (2026-09-14): confirmado por el usuario en Unity Editor Test Runner,
  EditMode, rama feat/m2-pr2-bert-sentis — BertIntentClassifierTests aparece y pasa en verde
  junto con el resto de la suite (368 tests). Sentis soporta los operadores de
  distilbert-base-multilingual-cased sin fallas; queda descartado el riesgo de tener que volver
  a Fase 1 con otro candidato. La mitigación de determinismo de la tarea 2.6 (BackendType.CPU)
  queda confirmada en la práctica, no solo por diseño. -->
- [x] 2.8 Confirmar que ningún otro punto del código pasa a instanciar `BertIntentClassifier`
  todavía de forma automática: la integración real en una escena de composición queda para M11
  (Harness) cuando exista, según `design.md` → Migration/Rollout.
  <!-- apply PR2 (2026-09-14): `rg BertIntentClassifier` en el repo solo encuentra la clase
  misma, su prueba, y menciones en documentos OpenSpec (proposal/design/spec/tasks de este
  cambio y de M6/M15, que solo la referencian en prosa). Cero wiring real. -->
- [x] 2.9 Corregir el constructor de `BertIntentClassifier` de `string modelPath` a
  `ModelAsset modelo, TextAsset tokenizadorJson`: la tarea 2.5 original dejó un gap real, no
  cosmético — el camino de carga solo funcionaba dentro del Editor (`AssetDatabase` para el
  `.onnx`, `File.ReadAllText` para el `tokenizer.json`), y ninguna de las dos APIs existe en un
  build de jugador real (Quest). `design.md` → Migration/Rollout diferiría incorrectamente esta
  corrección al "wiring de M11"; eso es un error de asignación de responsabilidad: M11 solo
  puede pasar lo que el constructor de M2 acepta, así que si el constructor solo aceptaba una
  ruta de archivo, ningún código de composición de M11 podía resolverlo en un build real. La
  corrección debía vivir dentro de la propia clase de M2.
  <!-- apply PR2 (2026-09-14, continuación): constructor cambiado a
  `BertIntentClassifier(ModelAsset modelo, TextAsset tokenizadorJson)`. `CargarModelo` ahora es
  `modelo != null ? ModelLoader.Load(modelo) : null` (sin `#if UNITY_EDITOR`, sin
  `AssetDatabase`, sin fallback a `ModelLoader.Load(string)`). `CargarTokenizador` ahora es
  `HuggingFaceParser.GetDefault().Parse(tokenizadorJson.text)` cuando `tokenizadorJson != null`
  (sin `File.Exists`/`File.ReadAllText`/`Path.Combine`). `Tests/EditMode/Nlu/
  BertIntentClassifierTests.cs` actualizado: `CreateSubject()` resuelve el `.onnx` y el
  `tokenizer.json` commiteados vía `AssetDatabase.LoadAssetAtPath<ModelAsset>`/
  `<TextAsset>` (uso correcto aquí — código de prueba siempre corre en el Editor) y los pasa al
  nuevo constructor; la prueba de "ruta inexistente" se reemplazó por
  `Un_modelo_o_tokenizador_nulo_deja_el_clasificador_no_listo_y_no_lanza` (casos `null,null` /
  `modelo,null` / `null,tokenizador`), mismo comportamiento esperado que antes
  (`IsReady` falso, `Classify` devuelve `Unknown`, nunca lanza).

  CONFIRMADO en el Test Runner real (2026-09-15, ver apply-progress.md "Compuerta humana del
  constructor nuevo — resultado"): `BertIntentClassifierTests` paso en verde con el constructor
  `ModelAsset`/`TextAsset`. En el camino se encontro y corrigio un gap real de asmdef
  (`Tests/EditMode/Nlu/NpcAi.Nlu.Tests.asmdef` no referenciaba `Unity.InferenceEngine`,
  commit `d67572a`) — exactamente el tipo de error que solo un compilador real detecta. -->

> PR2 depende del `.onnx` de PR1. No requiere reentrenar dentro de PR2 — solo consume el artefacto
> ya producido.

## Phase 3: Documentación y cierre (PR3)

- [ ] 3.1 Actualizar la sección M2 de `Docs/MODULES.md`: reemplazar la descripción de la
  desviación sin documentar por el estado real (modelo entrenado, encoder usado, corpus con el
  que se entrenó, precisión por clase del último entrenamiento, limitación conocida en
  `Tone.Empatico`).
- [ ] 3.2 Si el repo tiene `Docs/CONTRACT-CHANGELOG.md`, confirmar que este cambio no requiere
  entrada ahí (no toca `Runtime/Core/`) — si existe y por algún motivo sí aplica, documentarlo;
  si no, dejar constancia en `archive-report.md` de que se revisó y no aplica.
- [ ] 3.3 Confirmar que `Data/Corpus/PENDIENTE-AMPLIACION.md` (entregado junto con este cambio,
  fuera de este directorio) ya existe antes de cerrar — `proposal.md` lo referencia como
  entregable y no puede quedar como referencia colgante.
- [ ] 3.4 Revisar que el diff acumulado de PR1+PR2+PR3 respeta exactamente la lista de
  `Success Criteria` de `proposal.md` (ninguna carpeta fuera de `Training/Nlu/`, `Runtime/Nlu/`,
  `Tests/EditMode/Nlu/`, `package.json`, `.gitattributes`, `Docs/`, `openspec/`).

## Phase 4: Cierre (acciones del autor, cada PR)

- [ ] 4.1 `git add` solo de las carpetas del PR correspondiente; `git diff --cached` antes de
  cualquier commit, confirmando que no se cruza a otro módulo.
- [ ] 4.2 Checklist "Antes de mergear" del `README.md` (pruebas propias en verde, diff acotado,
  spec/design/tasks archivados, rama al día con `main`, decisiones registradas).
- [ ] 4.3 Al cerrar el último PR: mover este cambio a `openspec/changes/archive/`, actualizar
  `openspec/specs/clasificador-intenciones-m2/spec.md` si el comportamiento observable documentado
  cambió (no debería, según `proposal.md` → Capabilities), y dejar `archive-report.md` con las
  métricas finales de precisión por clase.
- [ ] 4.4 PR mergeado a `main` por el autor (regla 8).

## Notas

- Ningún agente ejecuta Unity ni entrena el modelo de forma autónoma: cada verde de Test Runner y
  cada corrida de `train.py` es una compuerta humana, igual que en M1, M4 y M5.
- PR1 es el único que puede avanzar sin esperar nada de Unity — es una buena unidad para empezar
  en paralelo con el spike técnico de Sentis (Fase 1 del cronograma).
- Si `Data/Corpus/PENDIENTE-AMPLIACION.md` se resuelve (corpus ampliado y rebalanceado) después de
  que este cambio ya se mergeó, reentrenar y reemplazar el `.onnx` es un cambio nuevo y pequeño,
  no una reapertura de este — el pipeline de PR1 ya queda listo para correrse de nuevo.
