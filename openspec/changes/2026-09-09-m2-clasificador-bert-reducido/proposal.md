# Propuesta: M2 — Clasificador de intención/tono entrenado (BERT reducido + transfer learning)

## Intent

`Docs/MODULES.md` ya deja registrada una desviación de diseño sin documentar: la propuesta
archivada de M2 (`openspec/changes/archive/2026-09-01-m2-clasificador-nlu/proposal.md`,
sección "Approach") comprometió explícitamente *"Motor de Inferencia: Procesamiento local
mediante Sentis / ONNX..."* con MiniLM como candidato — y el propio comentario de
`IntentClassifierContract.cs` ya dice literalmente *"El doble de M2 y el clasificador real
(MiniLM en Sentis) heredan de aqui"*. Lo que existe hoy en `NluIntentClassifier.cs` y
`SemanticMatcher.cs` es un clasificador basado en ~45 reglas de palabras clave
(`string.Contains` sobre texto normalizado): sin modelo, sin embeddings, sin entrenamiento, y
sin ninguna conexión al corpus de M3.

Este cambio cierra esa desviación con el enfoque ya concretado en la propuesta de trabajo de
grado (versión enviada al jurado): un encoder BERT reducido (clase MiniLM/DistilBERT,
~20-60M de parámetros) **congelado**, con una **cabeza de clasificación pequeña entrenada por
transfer learning** sobre el corpus de M3, exportado a ONNX y ejecutado on-device vía Unity
Sentis. El clasificador real reemplaza `SemanticMatcher`/`ToneAnalyzer` como motor de decisión,
sin cambiar ni una firma de `IIntentClassifier`.

Decisiones ya fijadas antes de este cambio (propuesta de trabajo de grado, confirmadas con el
usuario el 2026-09-08/09):

1. **Arquitectura de comprensión**: encoder congelado + cabeza de clasificación entrenada
   (transfer learning), no fine-tuning del encoder completo — el corpus disponible por
   categoría (30 frases/intención/escenario hoy, con meta documentada de 60-100) es demasiado
   pequeño para afinar un transformer completo sin sobreajustar.
2. **Cómputo de entrenamiento**: GPU local (GTX 1660, 6 GB VRAM) es suficiente para este
   volumen de datos y este tamaño de cabeza — no se requiere Colab ni ningún servicio en la
   nube salvo para descargar el encoder pre-entrenado la primera vez.
3. **Costo real**: inferencia on-device 100% gratuita y offline (igual que hoy). El
   entrenamiento sí tiene una dependencia externa puntual y de una sola vez — descargar los
   pesos del encoder pre-entrenado desde un repositorio como Hugging Face requiere internet una
   vez; después de exportar a ONNX, el modelo corre sin conexión.

## Scope

### In Scope

- Script de entrenamiento offline (fuera de Unity, Python) en un nuevo directorio de nivel
  superior `Training/Nlu/`: carga del corpus de `Data/Corpus/*.json`, congelamiento del encoder,
  entrenamiento de la cabeza de clasificación (intención y tono), validación con un conjunto de
  prueba separado, exportación a ONNX.
- Implementación real `Runtime/Nlu/BertIntentClassifier.cs` implementando `IIntentClassifier`
  vía Sentis, cargando el `.onnx` exportado.
- Declarar la dependencia real `com.unity.ai.inference` (Sentis) en `package.json` — hoy solo
  aparece como palabra suelta en `keywords`, no como dependencia real.
- Ubicación y versionado del artefacto `.onnx` entrenado (`Runtime/Nlu/Models/`), con Git LFS
  siguiendo el precedente que M1 dejó en `.gitattributes` para binarios vendorizados.
- `Tests/EditMode/Nlu/BertIntentClassifierTests.cs : IntentClassifierContract` (hereda la misma
  base que ya pasa `ScriptedIntentClassifier`).
- Actualizar `Docs/MODULES.md` (sección M2) y `Docs/CONTRACT-CHANGELOG.md` si aplica, cerrando
  la desviación documentada.

### Out of Scope

- **`IIntentClassifier` no cambia.** Ni una firma nueva, ni un campo nuevo en `IntentResult`:
  este cambio reemplaza el *motor* de M2, no su contrato. No es cambio de M0.
- **`ScriptedIntentClassifier` (el doble) no cambia.** Ya existe, ya pasa
  `IntentClassifierContract`, y el resto del pipeline (M4, M6, M8, M11) sigue integrando contra
  él sin esperar al clasificador real.
- **M3 (corpus)**: la ampliación y rebalanceo del corpus es la actividad 3.1 del cronograma y
  puede avanzar en paralelo, pero es trabajo de M3, no de este cambio. Este cambio consume el
  corpus tal como esté cuando se entrene; ver `Data/Corpus/PENDIENTE-AMPLIACION.md` (nuevo,
  entregado junto con este cambio) para el detalle de qué falta ahí.
- **M6 (generador de diálogo)**: cambio aparte (`2026-09-09-m6-generador-markov`), un módulo por
  cambio (regla 1).
- Reentrenar o versionar el modelo automáticamente ante cada actualización del corpus: en esta
  primera entrega el entrenamiento es un paso manual que un dueño del módulo ejecuta y commitea;
  automatizarlo (CI de entrenamiento) es un cambio posterior si se necesita.
- Cuantización o poda adicional del modelo más allá de usar una variante ya reducida
  (MiniLM/DistilBERT-class): optimizaciones de tamaño/latencia más agresivas quedan para un
  cambio posterior si el spike de Fase 1 muestra que hace falta.

## Capabilities

### New Capabilities

- Ninguna nueva a nivel de puerto: `clasificador-intenciones-m2` ya existe
  (`openspec/specs/clasificador-intenciones-m2/spec.md`) y sigue vigente sin cambios — la spec
  formaliza comportamiento observable del puerto, no el enfoque de implementación.

### Modified Capabilities

- `clasificador-intenciones-m2`: la implementación real que satisface esta spec cambia de
  reglas de palabras clave a un modelo entrenado. El comportamiento observable (determinismo,
  rango de `Confidence`, no lanzar, `Unknown` para vacío) no cambia — sigue siendo la misma
  spec, ahora satisfecha por un motor distinto.

## Approach

**El seam ya existe y ya está probado**: `IIntentClassifier` es la única superficie que ve el
resto del sistema. `BertIntentClassifier` es un adaptador nuevo detrás de esa interfaz —
exactamente el mismo patrón que `VoskRecognitionEngine` es para `ISpeechToText` en M1. Ningún
otro módulo cambia una línea.

**Por qué encoder congelado + cabeza entrenada, y no fine-tuning completo**: con ~30 ejemplos
por intención por escenario hoy (y una meta documentada de 60-100), afinar los millones de
parámetros de un transformer completo sobreajusta casi con certeza. Congelar el encoder y
entrenar solo una cabeza pequeña (una capa densa + softmax sobre el embedding de salida) es la
forma estándar de transfer learning para datasets chicos, reduce drásticamente el riesgo de
sobreajuste, y es lo bastante liviano para entrenar en CPU o en una GPU de consumo (ver
`Risks`).

**División de trabajo entrenamiento/runtime**: el entrenamiento vive en `Training/Nlu/` (Python,
fuera del paquete Unity — Sentis no entrena, solo infiere) y produce un artefacto `.onnx`
versionado. El runtime (`Runtime/Nlu/BertIntentClassifier.cs`) solo carga ese artefacto y corre
inferencia — no hay entrenamiento en el dispositivo ni en el Editor de Unity.

**Tono e intención**: el corpus etiqueta ambos por frase (`{text, intent, tone, ...}`). La
cabeza de clasificación entrenada tiene dos salidas (una por cada enum), compartiendo el mismo
embedding congelado — dos softmax pequeños en vez de dos modelos separados, más liviano y más
rápido de entrenar con este volumen de datos.

**`TextPreprocessor`/`SemanticMatcher`/`ToneAnalyzer` no se borran todavía**: quedan en el
repo como referencia y como respaldo determinista documentado (útil, por ejemplo, si el modelo
no carga — ver `Risks`), pero `NluIntentClassifier.cs` deja de ser el punto de entrada real: el
punto de entrada real pasa a ser `BertIntentClassifier`. Decidir si se borran del todo es una
limpieza posterior, fuera de alcance de este cambio.

## Affected Areas

| Área | Impacto | Descripción |
|---|---|---|
| `Training/Nlu/` | Nuevo | Script de entrenamiento offline (Python), fuera del paquete Unity |
| `Runtime/Nlu/BertIntentClassifier.cs` | Nuevo | Implementación real vía Sentis |
| `Runtime/Nlu/Models/` | Nuevo (binario, LFS) | Artefacto `.onnx` entrenado |
| `Tests/EditMode/Nlu/BertIntentClassifierTests.cs` | Nuevo | Hereda `IntentClassifierContract` |
| `package.json` | Modificado | Declarar dependencia real `com.unity.ai.inference` |
| `.gitattributes` | Modificado | Regla LFS para `Runtime/Nlu/Models/**/*.onnx` |
| `Docs/MODULES.md` | Modificado | Cerrar la desviación documentada en la sección M2 |
| `Runtime/Nlu/SemanticMatcher.cs`, `ToneAnalyzer.cs`, `NluIntentClassifier.cs` | Sin cambio | Quedan como referencia/respaldo; dejan de ser el motor real |
| `Runtime/Core/`, `Runtime/CoreChannels/` | Sin cambio | `IIntentClassifier` no cambia |

## Risks

| Riesgo | Prob. | Mitigación |
|---|---|---|
| El corpus actual (30/intención/escenario) es insuficiente para que la cabeza generalice, sobre todo en `Tone.Empatico` (1 ejemplo en `emergencia.json`, 0 en `juntas.json`) | Alta | Ver `Data/Corpus/PENDIENTE-AMPLIACION.md`: ampliar y rebalancear el corpus (actividad 3.1) es un prerrequisito real, no solo deseable, antes de entrenar en serio. Este cambio puede avanzar con un primer entrenamiento exploratorio sobre el corpus actual para validar el pipeline, sabiendo que la precisión en `Empatico` va a ser mala hasta que el corpus se amplíe |
| `com.unity.ai.inference` (Sentis) 2.6.1 no soporta algún operador que el encoder elegido necesite | Media | Hacer el spike técnico de Fase 1 (ya planeado en el cronograma: "1.2 Spike técnico de Sentis y selección del stack") con el encoder candidato real antes de comprometerse; si falla, hay varias variantes MiniLM/DistilBERT/TinyBERT para probar |
| GTX 1660 (6 GB) resulta insuficiente en la práctica (poco probable para una cabeza pequeña, pero no verificado en hardware real todavía) | Baja | Seam ya aislado (`Training/Nlu/`, fuera de Unity): si hace falta más cómputo, se corre el mismo script en Colab sin tocar el resto del cambio |
| Artefacto `.onnx` versionado en Git sin LFS infla el repo | Media | `.gitattributes` ya tiene el precedente de LFS para binarios de M1; este cambio agrega la regla equivalente para `Runtime/Nlu/Models/**/*.onnx` antes de commitear el primer modelo |
| Regresión de comportamiento observable (determinismo, rango de `Confidence`) al pasar de reglas fijas a un modelo | Media | `BertIntentClassifierTests` hereda `IntentClassifierContract` sin modificarla: si el modelo introduce no-determinismo en `Intent`/`Tone`, la prueba `Es_determinista_para_la_misma_entrada` lo detecta antes de mergear |

## Rollback Plan

`BertIntentClassifier` es una implementación nueva y aditiva de un puerto que ya existe;
`NluIntentClassifier`/`SemanticMatcher`/`ToneAnalyzer` no se tocan. Revertir los commits de este
cambio deja el clasificador real en el estado de reglas por palabra clave que ya está en
`main` hoy — el sistema sigue funcionando (peor, pero funcionando), porque ningún otro módulo
depende de `BertIntentClassifier` por nombre, solo de `IIntentClassifier`. No hay migración de
datos ni cambio de `Contract.Version`.

## Dependencies

- `contrato-nucleo-m0`, archivado y estable: define `IIntentClassifier`, `IntentResult`,
  `Intent`, `Tone`. Este cambio no lo modifica.
- Corpus de M3 (`Data/Corpus/*.json`), ya commiteado en `main` — pero con la ampliación
  pendiente descrita en `Data/Corpus/PENDIENTE-AMPLIACION.md`.
- `com.unity.ai.inference` (Sentis) 2.6.1, ya referenciado en el contexto de `openspec/config.yaml`
  pero no declarado todavía en `package.json`.
- Entorno Python para `Training/Nlu/` (no versionado en el paquete Unity; se documenta el
  `requirements.txt` correspondiente dentro de `Training/Nlu/`).
- GPU local (GTX 1660) o, como respaldo, un notebook Colab con GPU gratuita.

## Success Criteria

- [ ] `BertIntentClassifier` pasa exactamente la misma batería `IntentClassifierContract` que
      `ScriptedIntentClassifier`, sin modificar la clase base.
- [ ] El pipeline de entrenamiento en `Training/Nlu/` es reproducible: correrlo dos veces sobre
      el mismo corpus produce un modelo con el mismo comportamiento observable (mismo
      `Intent`/`Tone` para el mismo texto de entrada, dentro de la tolerancia de determinismo
      que exige el contrato).
- [ ] El modelo exportado corre en el Editor de Unity vía Sentis sin conexión a internet.
- [ ] `Docs/MODULES.md` (sección M2) deja de describir la desviación como "sin documentar": pasa
      a describir el estado real (modelo entrenado, corpus usado, limitaciones conocidas por
      clase de tono).
- [ ] El diff de este cambio no toca `Runtime/Core/`, `Runtime/CoreChannels/`, ni ninguna carpeta
      fuera de `Training/Nlu/`, `Runtime/Nlu/`, `Tests/EditMode/Nlu/`, `package.json`,
      `.gitattributes`, `Docs/` y `openspec/`.

## Decisiones del usuario (confirmadas 2026-09-08/09)

1. **Enfoque de comprensión**: BERT reducido (encoder congelado) + cabeza de clasificación
   entrenada por transfer learning, no fine-tuning completo del encoder.
2. **Cómputo de entrenamiento**: GPU local (GTX 1660, 6 GB) suficiente para este volumen; Colab
   queda como respaldo, no como plan primario.
3. **Costo/dependencias**: inferencia 100% offline y sin costo recurrente; el entrenamiento
   tiene una dependencia puntual de internet (descarga del encoder pre-entrenado) que no se
   repite después de exportar a ONNX.
