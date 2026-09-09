# Design: M2 — Clasificador de intención/tono entrenado

## Technical Approach

Un adaptador nuevo (`BertIntentClassifier`) detrás del puerto `IIntentClassifier` que ya existe.
El entrenamiento vive fuera del paquete Unity, en `Training/Nlu/` (Python): carga el corpus,
congela un encoder pre-entrenado tipo MiniLM/DistilBERT, entrena una cabeza de clasificación
pequeña para `Intent` y otra para `Tone` sobre el mismo embedding, valida contra un split de
prueba, y exporta a ONNX. El runtime solo infiere.

### Unidad de módulo y grafo de referencias

    NpcAi.Nlu   -> [NpcAi.Core]                      (sin cambio; noEngineReferences NO aplica
                                                       aquí porque Sentis SÍ es UnityEngine —
                                                       ver AD1)

`Runtime/Nlu` ya referencia `UnityEngine` indirectamente a través de Sentis (Sentis es un
paquete Unity, `com.unity.ai.inference`), así que `NpcAi.Nlu` no tiene la restricción
`noEngineReferences: true` que sí tiene `NpcAi.Core` — esto no cambia con este cambio, ya era
así desde que M2 se formalizó. Ningún otro ensamblado gana ni pierde referencias.

`Training/Nlu/` vive fuera de cualquier `.asmdef`: es Python, no C#, y no lo empaqueta Unity.

## Architecture Decisions

| # | Decisión | Elección | Alternativas rechazadas | Razón |
|---|---|---|---|---|
| AD1 | Encoder congelado vs. fine-tuning completo | Congelar el encoder, entrenar solo una cabeza de clasificación pequeña | Fine-tuning de todos los parámetros del encoder | Con ~30-100 ejemplos por clase el fine-tuning completo sobreajusta; congelar y entrenar solo la cabeza es la práctica estándar de transfer learning para datasets chicos y reduce el costo de cómputo a algo que corre en una GPU de consumo |
| AD2 | Una cabeza para Intent+Tone vs. dos modelos separados | Un encoder compartido, dos cabezas de clasificación pequeñas (una por enum) | Dos modelos completos independientes | Ambas tareas comparten la misma representación semántica de la frase; dos cabezas pequeñas sobre un embedding compartido es más liviano de entrenar y de exportar que duplicar el encoder |
| AD3 | Formato de intercambio | ONNX, exportado una vez tras entrenar | Cargar pesos PyTorch directamente en runtime | Sentis (`com.unity.ai.inference`) consume ONNX; es además el formato que ya comprometió la propuesta archivada de M2 |
| AD4 | Dónde vive el entrenamiento | `Training/Nlu/` en la raíz del repo, fuera de `Runtime/` | Un notebook fuera del repo, sin versionar | El pipeline de entrenamiento es parte del proceso reproducible del proyecto (regla del jurado: "el proyecto no debe cambiar" implica poder re-derivar el modelo); versionarlo en el propio repo, junto al corpus que consume, evita que el paso de entrenamiento dependa de una máquina o cuenta personal |
| AD5 | Ubicación y versionado del `.onnx` | `Runtime/Nlu/Models/`, trackeado con Git LFS | Commitear el binario sin LFS; o no versionarlo y generarlo por CI | No hay CI en el repo (config ya lo documenta); el precedente de M1 con los binarios de Vosk ya resolvió este mismo problema — se repite el patrón en vez de inventar uno nuevo |
| AD6 | Respaldo si el modelo no carga | `SemanticMatcher`/`ToneAnalyzer`/`NluIntentClassifier` quedan en el repo sin borrar, como referencia | Borrarlos ahora que hay implementación real | `IsReady` del contrato ya exige que `Classify` nunca lance si el modelo no está listo; mantener el camino de reglas fijas disponible (aunque `BertIntentClassifier` sea el punto de entrada real) documenta una opción de respaldo de bajo riesgo. Decidir si se eliminan del todo es limpieza posterior, no parte de este cambio |
| AD7 | Validación de aceptación | Verde EditMode humano (`IntentClassifierContract` heredado) + inspección manual de métricas de precisión del split de validación (fuera de EditMode, en el script de entrenamiento) | Compuerta de CI | Igual criterio que el resto del repo: no hay CI; la novedad aquí es que además hay una métrica de calidad de modelo (precisión) que EditMode no puede verificar — se reporta aparte, en el resultado del script de entrenamiento |

## Data Flow

    Data/Corpus/*.json (M3)
              │
              ▼  Training/Nlu/train.py
    ┌─────────────────────────────────────────┐
    │ 1. Cargar y unir emergencia.json +       │
    │    juntas.json                            │
    │ 2. Split train/validación                │
    │ 3. Encoder pre-entrenado (congelado) →    │
    │    embeddings                             │
    │ 4. Entrenar cabeza Intent + cabeza Tone   │
    │ 5. Validar contra split de prueba         │
    │ 6. Exportar encoder+cabezas a ONNX        │
    └─────────────────────────────────────────┘
              │
              ▼
    Runtime/Nlu/Models/intent-tone-classifier.onnx  (commiteado, LFS)
              │
              ▼  Unity Sentis, en runtime
    BertIntentClassifier : IIntentClassifier
              │  Classify(text) → IntentResult
              ▼
    (M4 Receptividad, M6 Diálogo, M8 Presentación — sin cambios)

## File Inventory

| Archivo | Rol |
|---|---|
| `Training/Nlu/requirements.txt` | Dependencias Python del pipeline de entrenamiento |
| `Training/Nlu/prepare_dataset.py` | Carga y unifica `Data/Corpus/*.json`, hace el split train/validación |
| `Training/Nlu/train.py` | Congela el encoder, entrena las dos cabezas, valida, exporta ONNX |
| `Training/Nlu/README.md` | Cómo correr el entrenamiento, qué encoder usar, cómo leer las métricas |
| `Runtime/Nlu/Models/intent-tone-classifier.onnx` | Artefacto entrenado (LFS) |
| `Runtime/Nlu/BertIntentClassifier.cs` | Implementación real de `IIntentClassifier` vía Sentis |
| `Tests/EditMode/Nlu/BertIntentClassifierTests.cs` | Hereda `IntentClassifierContract` |
| `package.json` | Agregar `com.unity.ai.inference` a `dependencies` |
| `.gitattributes` | Regla LFS para `Runtime/Nlu/Models/**/*.onnx` |

## Interfaces / Contracts

Sin cambios en `NpcAi.Core`. Superficie nueva, interna a `NpcAi.Nlu`:

```csharp
// Runtime/Nlu/BertIntentClassifier.cs
public sealed class BertIntentClassifier : IIntentClassifier
{
    public BertIntentClassifier(string modelPath); // carga el .onnx vía Sentis
    public bool IsReady { get; }                    // false hasta que el modelo termine de cargar
    public IntentResult Classify(string text);       // igual firma que hoy
}
```

## Testing Strategy

| Archivo | Qué fija |
|---|---|
| `IntentClassifierContract` (sin cambios) | Comportamiento observable del puerto: nunca lanza, `Unknown` para vacío, determinismo en `Intent`/`Tone`, `Confidence` en `[0,1]`, `LatencyMs` ≥ 0 |
| `BertIntentClassifierTests` (nuevo) | Hereda `IntentClassifierContract` sin modificarla; `CreateSubject()` carga el `.onnx` commiteado |
| Métricas de precisión (fuera de NUnit) | Reportadas por `Training/Nlu/train.py` al final del entrenamiento: precisión de `Intent` y de `Tone` sobre el split de validación, por clase — para poder ver específicamente qué tan mal le va a `Tone.Empatico` mientras el corpus no se amplíe |

Ejecución EditMode: Test Runner de Unity 6, igual que el resto del repo. El entrenamiento y sus
métricas se ejecutan aparte, en Python, y no forman parte del Test Runner.

## Migration / Rollout

Sin migración de datos ni de contrato. `BertIntentClassifier` es una implementación nueva del
mismo puerto: se integra reemplazando el punto donde hoy se instancia `NluIntentClassifier` en
cualquier composición real (M11 Harness cuando exista su escena), sin tocar la interfaz.

Rollback: revertir los commits de este cambio deja `NluIntentClassifier` (reglas por palabra
clave) como implementación real, igual que hoy.

## Open Questions

- [ ] ¿Qué encoder pre-entrenado específico (MiniLM-L6, DistilBERT multilingüe, TinyBERT) da
      mejor precisión en español colombiano hablado con este corpus? Se resuelve en el spike
      técnico de Fase 1 ("1.2 Spike técnico de Sentis y selección del stack"), probando 2-3
      candidatos contra el corpus actual antes de comprometerse.
- [ ] ¿La cabeza de `Tone` necesita una clase "sin suficientes datos" explícita mientras
      `Empatico` tenga tan pocos ejemplos, o se acepta baja precisión en esa clase hasta que
      `Data/Corpus/PENDIENTE-AMPLIACION.md` se resuelva? Queda para cuando haya una primera
      métrica real que discutir, no se decide a priori en este documento.
