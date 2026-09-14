# Delta para clasificador-intenciones-m2

## Purpose

Re-ancla la especificacion `clasificador-intenciones-m2` para el cambio
`2026-09-09-m2-clasificador-bert-reducido`, que **reemplaza el motor** de M2: de
~45 reglas de palabras clave a un encoder BERT reducido congelado + cabeza de
clasificacion entrenada (transfer learning), exportada a ONNX y ejecutada via
Unity Sentis.

El cambio es **motor-only**:

- `IIntentClassifier`, `IntentResult`, `Intent` y `Tone` (contrato
  `contrato-nucleo-m0`) NO cambian ni una firma. No es cambio de M0.
- El **comportamiento observable NO cambia**. Los siete requisitos vigentes
  siguen en vigor **verbatim**, ahora satisfechos por un modelo entrenado en
  lugar de reglas fijas.
- El enfoque de implementacion (encoder, entrenamiento, ONNX, Sentis) vive en
  `design.md`, NO en esta spec.

## Nota de revision (no normativa)

Revisados los siete requisitos de `openspec/specs/clasificador-intenciones-m2/spec.md`;
ninguno cambia su enunciado:

| Requisito vigente | Estado tras el cambio de motor |
|---|---|
| Consulta de disponibilidad (IsReady) | Vigente. `true` solo con el modelo ONNX y el vocabulario cargados en Sentis; leerlo nunca lanza. |
| Clasificacion de entradas nulas, vacias o en blanco | Vigente sin cambios; independiente del motor. |
| Determinismo en intencion y tono | Vigente; re-anclado abajo en MODIFIED. |
| Acotamiento de confianza y latencia | Vigente; re-anclado abajo en MODIFIED. |
| Resiliencia ante entradas atipicas o ruidosas | Vigente sin cambios. |
| Comportamiento ante clasificador no listo | Vigente. Si el `.onnx` no carga via Sentis, `IsReady` queda `false` y `Classify` devuelve `IntentResult.Unknown` sin lanzar. |
| Conformidad con IntentClassifierContract | Vigente. `BertIntentClassifier` hereda la misma base `IntentClassifierContract` que el doble `ScriptedIntentClassifier`, sin modificarla. |

**Fuera del alcance de la spec del puerto** (son *Success Criteria* / criterios de
aceptacion de `design.md`, NO `### Requirement:` aqui): reproducibilidad del
pipeline de `Training/Nlu/`, ejecucion 100% offline del `.onnx`, y precision por
clase de `Intent`/`Tone` (en especial `Tone.Empatico`). Miden calidad de modelo y
de proceso de entrenamiento, no comportamiento observable de `IIntentClassifier`.

## MODIFIED Requirements

### Requirement: Determinismo en intencion y tono

Para un mismo texto de entrada identico, sucesivas llamadas a `Classify` DEBEN
devolver exactamente el mismo `Intent` y el mismo `Tone`. La confianza
(`Confidence`) y la latencia (`LatencyMs`) no estan obligadas a ser identicas pero
deben cumplir sus rangos validos.

(Previously: este invariante lo satisfacia un clasificador de reglas de palabras
clave; ahora lo satisface un encoder BERT reducido congelado + cabeza de
clasificacion entrenada, ejecutados via Sentis. Lo exigido NO cambia.)

#### Scenario: Repeticion de inferencia con la misma entrada

- Dado un clasificador de intenciones listo
- Y un texto de entrada como `"por favor ayudeme con el paciente"`
- Cuando se llama a `Classify` dos o mas veces con ese mismo texto
- Entonces el `Intent` de todas las respuestas es identico
- Y el `Tone` de todas las respuestas es identico

#### Scenario: El motor entrenado en Sentis sigue siendo determinista

- Dado un `BertIntentClassifier` listo con el modelo entrenado cargado via Sentis
- Cuando se llama a `Classify` dos o mas veces con el mismo texto del dominio
- Entonces el `Intent` y el `Tone` son identicos en todas las respuestas
- Y cualquier diferencia queda acotada a `Confidence` y `LatencyMs` dentro de sus rangos

### Requirement: Acotamiento de confianza y latencia

Para cualquier clasificacion ejecutada, el valor de `Confidence` DEBE estar
estrictamente en el rango cerrado `[0.0, 1.0]`. El valor de `LatencyMs` DEBE ser
mayor o igual a cero (`>= 0f`).

(Previously: la confianza salia de una puntuacion derivada de reglas; ahora sale
de la cabeza de clasificacion entrenada. El rango exigido NO cambia.)

#### Scenario: Confianza normalizada

- Dado un clasificador de intenciones listo
- Cuando clasifica textos variados del dominio (solicitudes, agresiones, preguntas)
- Entonces para cada resultado se cumple `Confidence >= 0f` y `Confidence <= 1f`

#### Scenario: Latencia no negativa

- Dado un clasificador de intenciones listo
- Cuando clasifica cualquier texto
- Entonces `LatencyMs` es `>= 0f`

#### Scenario: La confianza que reporta el modelo entrenado permanece acotada

- Dado un `BertIntentClassifier` listo
- Cuando clasifica textos variados del dominio del corpus de M3
- Entonces cada `Confidence` es un numero finito en `[0.0, 1.0]`
- Y ningun resultado expone `NaN`, infinito ni un valor fuera de rango

## Trazabilidad (requisito -> prueba)

Mecanismo de verificacion: `Tests/EditMode/Nlu/BertIntentClassifierTests.cs :
IntentClassifierContract` (la crea la fase apply, PR2), que **hereda sin
modificar** `Tests/EditMode/Core/IntentClassifierContract.cs` — la misma base que
ya pasa `ScriptedIntentClassifier`.

| Requisito | Prueba heredada (base sin cambios) |
|---|---|
| Consulta de disponibilidad (IsReady) | `Reporta_si_esta_listo_sin_lanzar` |
| Clasificacion de entradas nulas, vacias o en blanco | `Texto_vacio_devuelve_Desconocida_y_no_lanza` |
| Determinismo en intencion y tono | `Es_determinista_para_la_misma_entrada` |
| Acotamiento de confianza y latencia | `La_confianza_siempre_esta_entre_cero_y_uno`, `La_latencia_nunca_es_negativa` |
| Resiliencia ante entradas atipicas o ruidosas | `Nunca_lanza_con_entradas_raras` |
| Comportamiento ante clasificador no listo | `Classify_no_lanza_en_ningun_estado`, `Un_clasificador_no_listo_devuelve_Unknown` |
| Conformidad con IntentClassifierContract | Suite `NpcAi.Nlu.Tests` derivada de `IntentClassifierContract`, 100% en verde |
