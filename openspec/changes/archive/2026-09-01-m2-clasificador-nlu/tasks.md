# Tasks: Implementación del Clasificador de Intenciones M2 (Runtime/Nlu)

## Review Workload Forecast

| Campo | Valor |
|---|---|
| Líneas estimadas | ~250-350 líneas autorales (Runtime + Tests) |
| Riesgo presupuesto 400 líneas | Bajo (dentro del límite) |
| PRs encadenados recomendados | No (un solo PR) |
| Estrategia de entrega | single-pr |

## Phase 0: Guardrails y Alcance

- [x] 0.1 Frontera estricta de escritura: solo `Runtime/Nlu/` y `Tests/EditMode/Nlu/`.
- [x] 0.2 No tocar `Runtime/Core/` ni otros módulos (`Runtime/Speech/`, `Runtime/Dialogue/`, etc.).
- [x] 0.3 El fake `Runtime/Nlu/Fakes/ScriptedIntentClassifier.cs` permanece intacto y determinista.
- [x] 0.4 Cero llamadas a `Debug.Log` en el código de runtime de `Runtime/Nlu/`.

## Phase 1: Preprocesamiento de Texto (TextPreprocessor)

- [x] 1.1 RED: Crear pruebas unitarias `Tests/EditMode/Nlu/TextPreprocessorTests.cs` (normalización de espacios, minúsculas, remoción de puntuación atípica, tolerancia a null y cadenas vacías).
- [x] 1.2 GREEN: Implementar `Runtime/Nlu/TextPreprocessor.cs` con métodos estáticos eficientes de limpieza y tokenización.
- [x] 1.3 Refactor & Optimización: Asegurar cero allocations innecesarias en el ciclo de limpieza.

## Phase 2: Análisis de Tono y Coincidencia Semántica (ToneAnalyzer & SemanticMatcher)

- [x] 2.1 RED: Crear pruebas unitarias para `ToneAnalyzer` y `SemanticMatcher` en `Tests/EditMode/Nlu/SemanticMatcherTests.cs` cubriendo los 5 tonos y las 6 intenciones activas.
- [x] 2.2 GREEN: Implementar `Runtime/Nlu/ToneAnalyzer.cs` para clasificación ortogonal del tono emocional (`Respetuoso`, `Agresivo`, `Empatico`, `Ansioso`, `Neutral`).
- [x] 2.3 GREEN: Implementar `Runtime/Nlu/SemanticMatcher.cs` para puntuación y clasificación de intenciones con cálculo de confianza acotada en `[0, 1]`.

## Phase 3: Clasificador Principal y Pruebas de Contrato (NluIntentClassifier)

- [x] 3.1 RED: Crear `Tests/EditMode/Nlu/NluIntentClassifierTests.cs` heredando de `NpcAi.Core.Tests.IntentClassifierContract` y agregando casos específicos de resiliencia y latencia.
- [x] 3.2 GREEN: Implementar `Runtime/Nlu/NluIntentClassifier.cs` implementando `IIntentClassifier` con `IsReady`, medición de latencia mediante `Stopwatch` y fallback a `IntentResult.Unknown`.
- [x] 3.3 Gate humano: Verificar en Unity Test Runner (EditMode) que tanto `ScriptedIntentClassifierTests` como `NluIntentClassifierTests` pasan al 100% en verde.
