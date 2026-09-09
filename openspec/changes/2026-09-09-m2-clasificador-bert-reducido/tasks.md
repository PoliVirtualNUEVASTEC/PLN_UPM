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

- [ ] 0.1 Frontera de escritura de PR1: SOLO `Training/Nlu/` y este directorio de cambio
  (`openspec/changes/2026-09-09-m2-clasificador-bert-reducido/`). Nada en `Runtime/` todavía.
- [ ] 0.2 Frontera de escritura de PR2: `Runtime/Nlu/BertIntentClassifier.cs`,
  `Runtime/Nlu/Models/`, `Tests/EditMode/Nlu/BertIntentClassifierTests.cs`, `package.json`,
  `.gitattributes`. **No tocar `Runtime/Nlu/SemanticMatcher.cs`, `ToneAnalyzer.cs`,
  `NluIntentClassifier.cs`, `TextPreprocessor.cs`** (Out of Scope — quedan como referencia).
- [ ] 0.3 Cero cambio en `Runtime/Core/Ports.cs`, `Dtos.cs`, `Enums.cs`. `IIntentClassifier`,
  `IntentResult`, `Intent`, `Tone` no cambian una firma. Si algo de este cambio parece requerir
  tocar `Runtime/Core/` o `Runtime/CoreChannels/`, parar y avisar — eso es cambio de contrato y
  necesita co-revisión de todos los dueños de módulo (regla del repo), fuera de lo que este
  cambio autoriza.
- [ ] 0.4 `Tests/EditMode/Core/IntentClassifierContract.cs` no se modifica en ningún PR:
  `BertIntentClassifierTests` hereda, no edita, la base.
- [ ] 0.5 Este módulo es IA (M2): `/sdd-ff` no aplica — cada PR pasa por el flujo completo
  propuesta → diseño → tareas → revisión humana, sin atajos.
- [ ] 0.6 El corpus (`Data/Corpus/*.json`) es de M3, no de este cambio: PR1 lo consume tal cual
  está en `main` en el momento de entrenar; no se edita el corpus dentro de este cambio (ver
  `Data/Corpus/PENDIENTE-AMPLIACION.md`, entregado aparte).

## Phase 1: Pipeline de entrenamiento offline (PR1)

- [ ] 1.1 Crear `Training/Nlu/requirements.txt` (framework de entrenamiento, biblioteca del
  encoder pre-entrenado, exportador ONNX — versiones fijadas, no rangos abiertos).
- [ ] 1.2 Crear `Training/Nlu/prepare_dataset.py`: carga y unifica
  `Data/Corpus/emergencia.json` + `Data/Corpus/juntas.json`, valida el esquema
  (`text`/`intent`/`tone`/`scenario`/`labeler`), hace el split train/validación (estratificado
  por `intent` y por `tone` en la medida en que el tamaño de cada clase lo permita).
- [ ] 1.3 Crear `Training/Nlu/train.py`: carga el encoder pre-entrenado candidato (parámetro de
  línea de comandos, no hardcodeado — para poder probar los 2-3 candidatos del spike de Fase 1
  sin editar el script), lo congela, entrena la cabeza de `Intent` y la cabeza de `Tone` sobre el
  mismo embedding, valida contra el split de prueba, exporta a ONNX.
- [ ] 1.4 El script de validación imprime precisión por clase (no solo agregada) — necesario para
  ver explícitamente qué tan mal le va a `Tone.Empatico` mientras el corpus no se amplíe (ver
  `Risks` en `proposal.md`).
- [ ] 1.5 Crear `Training/Nlu/README.md`: cómo instalar dependencias, cómo correr
  `prepare_dataset.py` → `train.py`, qué encoders probar, cómo leer las métricas impresas, dónde
  queda el `.onnx` resultante.
- [ ] 1.6 Correr el pipeline una vez de punta a punta sobre el corpus actual (30/intención/
  escenario) como prueba de humo del pipeline mismo — **no** como entrega de un modelo de
  producción; documentar en el resultado que la precisión en clases con pocos ejemplos
  (`Tone.Empatico` sobre todo) será baja hasta que `Data/Corpus/PENDIENTE-AMPLIACION.md` se
  resuelva.
- [ ] 1.7 Confirmar reproducibilidad (Success Criteria de `proposal.md`): correr `train.py` dos
  veces sobre el mismo corpus y comparar que el modelo resultante da el mismo `Intent`/`Tone`
  para un conjunto fijo de frases de prueba.

> PR1 es Python puro, fuera de Unity: no requiere Test Runner ni Unity Editor para validarse,
> solo correr los scripts y revisar la salida.

## Phase 2: `BertIntentClassifier` real + wiring Sentis (PR2)

- [ ] 2.1 Agregar `com.unity.ai.inference` (Sentis) a `dependencies` en `package.json` — hoy solo
  existe como palabra suelta en `keywords`, no como dependencia real.
- [ ] 2.2 Agregar la regla LFS para `Runtime/Nlu/Models/**/*.onnx` en `.gitattributes`, siguiendo
  el precedente ya usado para los binarios de Vosk de M1. Confirmar con `git lfs track` que la
  regla queda activa antes de commitear el primer `.onnx`.
- [ ] 2.3 Copiar el `.onnx` producido por PR1 (Fase 1) a `Runtime/Nlu/Models/
  intent-tone-classifier.onnx` y commitearlo vía LFS.
- [ ] 2.4 RED: `Tests/EditMode/Nlu/BertIntentClassifierTests.cs : IntentClassifierContract` —
  `CreateSubject()` carga el `.onnx` commiteado vía Sentis.
- [ ] 2.5 GREEN: Crear `Runtime/Nlu/BertIntentClassifier.cs` implementando `IIntentClassifier`
  (constructor recibe `modelPath`; `IsReady` falso hasta terminar de cargar; `Classify(text)` con
  la misma firma que hoy) hasta pasar la batería heredada completa
  (`Reporta_si_esta_listo_sin_lanzar`, `Texto_vacio_devuelve_Desconocida_y_no_lanza`,
  `La_confianza_siempre_esta_entre_cero_y_uno`, `La_latencia_nunca_es_negativa`,
  `Es_determinista_para_la_misma_entrada`, `Nunca_lanza_con_entradas_raras`,
  `Classify_no_lanza_en_ningun_estado`, `Un_clasificador_no_listo_devuelve_Unknown`).
- [ ] 2.6 Prestar atención específica a `Es_determinista_para_la_misma_entrada`: si la inferencia
  de Sentis introduce no-determinismo (orden de reducción en GPU, por ejemplo), esta prueba lo
  detecta — resolverlo antes de mergear, no documentar la falla como aceptable.
- [ ] 2.7 MANUAL (Editor de Unity): confirmar que Sentis soporta todos los operadores del encoder
  elegido, corriendo la batería de pruebas en el Test Runner real (no solo revisión de código) —
  este es el spike técnico ya planeado en el cronograma ("1.2 Spike técnico de Sentis y selección
  del stack"); si el encoder candidato falla aquí, volver a Fase 1 con otro candidato antes de
  continuar PR2.
- [ ] 2.8 Confirmar que ningún otro punto del código pasa a instanciar `BertIntentClassifier`
  todavía de forma automática: la integración real en una escena de composición queda para M11
  (Harness) cuando exista, según `design.md` → Migration/Rollout.

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
