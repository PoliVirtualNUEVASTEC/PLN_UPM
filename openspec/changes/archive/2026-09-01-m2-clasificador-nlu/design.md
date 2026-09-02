# Design: Implementación del Clasificador de Intenciones M2 (Runtime/Nlu)

## Technical Approach

El módulo M2 (`Runtime/Nlu`) proporciona la implementación concreta de `IIntentClassifier` para procesar expresiones en lenguaje natural en español (orientadas a los escenarios de simulación VR) y mapearlas a `IntentResult` (intención, tono, confianza y latencia).

El diseño adopta una arquitectura desacoplada y modular dentro del ensamblado `NpcAi.Nlu`:
1. **Preprocesamiento y Normalización de Texto (`TextPreprocessor`)**: Limpieza de caracteres de control, normalización de espacios y preparación de tokens sin dependencias pesadas.
2. **Motor Semántico y Reglas de Dominio (`SemanticRuleMatcher`)**: Evaluación determinista de patrones sintácticos, frases clave y marcadores de cortesía/agresividad para resolución inmediata de alta precisión.
3. **Clasificador Principal (`NluIntentClassifier`)**: Implementación pública de `IIntentClassifier` que orquesta la normalización, la clasificación de intención, el análisis de tono ortogonal, el cálculo de latencia en milisegundos y la protección ante entradas inválidas o estados no inicializados.

### Grafo de Referencias del Módulo

    NpcAi.Nlu         -> [NpcAi.Core, NpcAi.Core.Channels]   (Runtime/Nlu/)
    NpcAi.Nlu.Tests   -> [NpcAi.Nlu, NpcAi.Core, NpcAi.Core.Tests,
                          UnityEngine.TestRunner, UnityEditor.TestRunner]

- Frontera estricta: `Runtime/Nlu/` y `Tests/EditMode/Nlu/`.
- Cero dependencias hacia otros módulos de características (M1, M4, M6, M7, M8, M9, M10).

## Architecture Decisions

| # | Decisión | Elección | Alternativas rechazadas | Razón |
|---|---|---|---|---|
| D1 | Separación de preprocesamiento y clasificación | Clase interna `TextPreprocessor` separada del clasificador | Todo en una sola clase monolítica | Facilita pruebas unitarias dedicadas de normalización y reutilización de buffers sin allocations innecesarias. |
| D2 | Extracción ortogonal de Tono | Motor de análisis de tono independiente de la intención (`ToneAnalyzer`) | Intención y tono acoplados en una sola matriz | El contrato M0 define `Tone` e `Intent` como dimensiones ortogonales; un usuario puede hacer una `SolicitudRespetuosa` o `SolicitudAgresiva`. |
| D3 | Manejo de resiliencia y entradas atípicas | Validación temprana y fallback seguro a `IntentResult.Unknown` | Dejar que el parser lance excepciones o use `try-catch` masivo | Cumple con la invariante contractual de no lanzar jamás ante entradas raras, strings vacíos o clasificadores no listos. |
| D4 | Medición de latencia | Uso de `System.Diagnostics.Stopwatch` de alta resolución | `Time.realtimeSinceStartup` o valores hardcodeados | Funciona en C# puro sin acoplamiento a frames de Unity y permite pruebas en milisegundos reales. |
| D5 | Herencia de pruebas de contrato | `NluIntentClassifierTests` hereda directamente de `NpcAi.Core.Tests.IntentClassifierContract` | Pruebas desconectadas sin clase base | Garantiza que tanto el doble (`ScriptedIntentClassifier`) como la implementación real satisfagan exactamente las mismas invariantes contractuales. |

## Data Flow

```
[Entrada: string text]
        │
        ▼
[NluIntentClassifier.Classify]
        │
        ├── 1. Validar IsReady y string.IsNullOrWhiteSpace
        │      └── Si inválido/vacío ──> Retorna IntentResult.Unknown(0)
        │
        ├── 2. Iniciar Stopwatch
        │
        ├── 3. TextPreprocessor.Normalize(text)
        │      └── Minúsculas, recorte de puntuación periférica, trim
        │
        ├── 4. SemanticMatcher.ClassifyIntent(normalizedText)
        │      └── Devuelve (Intent, Confidence)
        │
        ├── 5. ToneAnalyzer.AnalyzeTone(normalizedText)
        │      └── Devuelve Tone
        │
        ├── 6. Detener Stopwatch (LatencyMs)
        │
        ▼
[Salida: IntentResult(intent, tone, confidence, latencyMs)]
```

## File Changes

| Archivo | Acción | Descripción |
|---|---|---|
| `Runtime/Nlu/TextPreprocessor.cs` | Crear | Normalización de cadenas, limpieza de signos y extracción de palabras clave. |
| `Runtime/Nlu/ToneAnalyzer.cs` | Crear | Detección ortogonal de tonos (`Respetuoso`, `Agresivo`, `Empatico`, `Ansioso`, `Neutral`). |
| `Runtime/Nlu/SemanticMatcher.cs` | Crear | Reglas de clasificación y puntuación semántica para los 6 intents del contrato. |
| `Runtime/Nlu/NluIntentClassifier.cs` | Crear | Implementación real de `IIntentClassifier`. |
| `Tests/EditMode/Nlu/NluIntentClassifierTests.cs` | Crear | Pruebas unitarias y de contrato heredando de `IntentClassifierContract`. |
| `Tests/EditMode/Nlu/TextPreprocessorTests.cs` | Crear | Pruebas unitarias específicas para normalización y resiliencia de texto. |

## Interfaces / Contracts

La implementación pública expone:

```csharp
namespace NpcAi.Nlu
{
    public sealed class NluIntentClassifier : IIntentClassifier
    {
        public bool IsReady { get; }
        public IntentResult Classify(string text);
        
        // Constructor por defecto listo para uso inmediato en runtime
        public NluIntentClassifier(bool isReady = true) { ... }
    }
}
```
