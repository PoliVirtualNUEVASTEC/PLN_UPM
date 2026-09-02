# Propuesta: Implementación real del clasificador de intenciones M2 (Runtime/Nlu)

## Intent

El módulo M2 (`Runtime/Nlu`) es responsable de transformar texto libre del usuario en una intención estructurada (`Intent`) y un tono emocional (`Tone`), implementando el puerto contractual `IIntentClassifier`. Actualmente, el módulo solo cuenta con el doble de prueba determinista (`ScriptedIntentClassifier`).

Esta propuesta define la especificación, diseño e implementación real del clasificador de intenciones on-device (basado en inferencia local con Unity Sentis / embeddings / ONNX o tokenización de vocabulario) y su integración en el pipeline, asegurando que cumpla todas las invariantes congeladas en el contrato v1 de M0 y pase las pruebas contractuales `IntentClassifierContract`.

## Scope

### In Scope

- Especificación OpenSpec versionada (`spec.md`, `design.md`, `tasks.md`) para la capacidad `clasificador-intenciones-m2`.
- Implementación real de `IIntentClassifier` en `Runtime/Nlu/` (ej. `SentisIntentClassifier` / pipeline de inferencia on-device).
- Manejo determinista de intenciones y tonos ante entradas idénticas.
- Soporte de estado `IsReady` (carga asíncrona/segura del modelo y vocabulario sin bloquear ni lanzar excepciones).
- Manejo robusto de entradas atípicas (`null`, vacíos, símbolos, cadenas extensas) devolviendo `IntentResult.Unknown`.
- Pruebas unitarias y de contrato en `Tests/EditMode/Nlu/` heredando de `IntentClassifierContract`.
- Frontera de escritura estricta: `Runtime/Nlu/`, `Tests/EditMode/Nlu/`.

### Out of Scope

- Cambios al contrato `NpcAi.Core` (M0 está congelado en v1).
- Modificación de otros módulos de runtime (`Runtime/Speech/`, `Runtime/Receptivity/`, `Runtime/Dialogue/`, etc.).
- Creación o edición de corpus masivos de entrenamiento (pertenece a M3 `Data/Corpus/`).
- Integración visual o de escena de VR (pertenece a M8/M11).

## Capabilities

### New Capabilities

- `clasificador-intenciones-m2`: Inferencia y clasificación semántica de texto libre a `IntentResult` (intención, tono, confianza, latencia) en tiempo de ejecución de Unity.

### Modified Capabilities

- Ninguna (las especificaciones existentes en `openspec/specs/` pertenecen a M0).

## Approach

- **Arquitectura Hexagonal**: La implementación real implementa `NpcAi.Core.IIntentClassifier` y se aísla en el assembly `NpcAi.Nlu` (`NpcAi.Nlu.asmdef`).
- **Motor de Inferencia**: Procesamiento local mediante Sentis / ONNX / tokenizador de texto optimizado para Unity sin asignaciones excesivas de memoria (GC friendly).
- **Manejo de Ciclo de Vida**: Propiedad `IsReady` no lanza excepciones; ante modelo no cargado o error de inicialización, `Classify` devuelve de manera segura `IntentResult.Unknown`.
- **Determinismo**: Garantizado para `Intent` y `Tone` ante el mismo texto normalizado.
- **TDD Estricto**: La implementación real debe pasar la suite base `IntentClassifierContract` en EditMode idéntica a la que pasa `ScriptedIntentClassifier`.

## Affected Areas

| Área | Impacto | Descripción |
|---|---|---|
| `Runtime/Nlu/` | Nuevo / Modificado | Implementación del clasificador de inferencia y componentes auxiliares (tokenizador/configuración). |
| `Tests/EditMode/Nlu/` | Nuevo / Modificado | Pruebas de contrato y tests específicos de inferencia/resiliencia de M2. |

## Gobernanza y Reglas de Convivencia

- **Dueño del módulo**: Luis Miguel Canaveral Restrepo (M2).
- **Frontera de módulo**: Solo se tocan `Runtime/Nlu/` y `Tests/EditMode/Nlu/`.
- **Integración**: Flujo SDD en rama `feat/m2-nlu-classifier`, merge autónomo por el autor una vez cumplida la checklist de DoD.

## Risks

| Riesgo | Prob. | Mitigación |
|---|---|---|
| Disponibilidad del modelo ONNX / Sentis en entorno de prueba sin assets pesados | Media | Diseñar abstracción desacoplada para motor tensor / backend configurable o tokenizer testeable con modelo embebido/mockeable. |
| Latencia de inferencia en CPU | Baja | Modelo liviano optimizado (p. ej. MobileBERT / MiniLM cuantizado o clasificador lineal sobre embeddings). |
| Asignación de memoria (GC allocs) por manipulación de strings | Media | Normalización de cadenas eficiente con buffers reutilizables. |

## Success Criteria

- [ ] Todas las pruebas de `Tests/EditMode/Nlu/` pasando en verde en Unity Test Runner (EditMode).
- [ ] Implementación real pasando el 100% de los tests de `IntentClassifierContract`.
- [ ] Cero referencias a módulos ajenos a `NpcAi.Core` y `NpcAi.Core.Channels`.
- [ ] Cumplimiento total de invariantes: no lanza excepciones ante entradas inválidas o cuando `IsReady == false`.
- [ ] Artefactos SDD (`spec.md`, `design.md`, `tasks.md`) completados y validados.
