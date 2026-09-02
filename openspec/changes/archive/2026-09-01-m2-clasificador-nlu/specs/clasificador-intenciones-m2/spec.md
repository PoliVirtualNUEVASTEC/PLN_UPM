# Especificacion: clasificador-intenciones-m2

## Purpose

Define los requisitos y escenarios de comportamiento para la implementación real del clasificador de lenguaje natural (M2, `Runtime/Nlu`), responsable de clasificar enunciados de texto libre en intenciones (`Intent`) y tonos (`Tone`) estructurados mediante la interfaz contractual `IIntentClassifier`.

## ADDED Requirements

### Requirement: Consulta de disponibilidad (IsReady)

La propiedad `IsReady` DEBE devolver `true` únicamente cuando el modelo de inferencia y los recursos de tokenización/vocabulario estén completamente cargados y listos para clasificar. La lectura de `IsReady` NO DEBE lanzar excepciones bajo ninguna condición ni en ningún estado de inicialización.

#### Scenario: Estado listo tras carga exitosa
- Dado un clasificador de intenciones correctamente inicializado con sus datos/modelo
- Cuando se consulta la propiedad `IsReady`
- Entonces devuelve `true` sin lanzar excepciones

#### Scenario: Estado no listo antes de inicializar o ante fallo
- Dado un clasificador de intenciones instanciado sin modelo cargado o en proceso de carga
- Cuando se consulta la propiedad `IsReady`
- Entonces devuelve `false` sin lanzar excepciones

---

### Requirement: Clasificación de entradas nulas, vacías o en blanco

Ante entradas `null`, cadenas vacías `""` o cadenas compuestas exclusivamente por espacios en blanco, `Classify` NO DEBE lanzar excepciones y DEBE devolver `IntentResult.Unknown` con `Intent == Intent.Desconocida`, `Tone == Tone.Neutral` y `Confidence == 0f`.

#### Scenario: Entrada nula
- Dado un clasificador de intenciones listo
- Cuando se ejecuta `Classify(null)`
- Entonces devuelve `Intent.Desconocida` con `Confidence == 0f` y `Tone == Tone.Neutral`

#### Scenario: Entrada vacía
- Dado un clasificador de intenciones listo
- Cuando se ejecuta `Classify("")`
- Entonces devuelve `Intent.Desconocida` con `Confidence == 0f` y `Tone == Tone.Neutral`

#### Scenario: Entrada con solo espacios
- Dado un clasificador de intenciones listo
- Cuando se ejecuta `Classify("   \t  \n ")`
- Entonces devuelve `Intent.Desconocida` con `Confidence == 0f` y `Tone == Tone.Neutral`

---

### Requirement: Determinismo en intención y tono

Para un mismo texto de entrada idéntico, sucesivas llamadas a `Classify` DEBEN devolver exactamente el mismo `Intent` y el mismo `Tone`. La confianza (`Confidence`) y la latencia (`LatencyMs`) no están obligadas a ser idénticas pero deben cumplir sus rangos válidos.

#### Scenario: Repetición de inferencia con la misma entrada
- Dado un clasificador de intenciones listo
- Y un texto de entrada como `"por favor ayudeme con el paciente"`
- Cuando se llama a `Classify` dos o más veces con ese mismo texto
- Entonces el `Intent` de todas las respuestas es idéntico
- Y el `Tone` de todas las respuestas es idéntico

---

### Requirement: Acotamiento de confianza y latencia

Para cualquier clasificación ejecutada, el valor de `Confidence` DEBE estar estrictamente en el rango cerrado `[0.0, 1.0]`. El valor de `LatencyMs` DEBE ser mayor o igual a cero (`>= 0f`).

#### Scenario: Confianza normalizada
- Dado un clasificador de intenciones listo
- Cuando clasifica textos variados del dominio (solicitudes, agresiones, preguntas)
- Entonces para cada resultado se cumple `Confidence >= 0f` y `Confidence <= 1f`

#### Scenario: Latencia no negativa
- Dado un clasificador de intenciones listo
- Cuando clasifica cualquier texto
- Entonces `LatencyMs` es `>= 0f`

---

### Requirement: Resiliencia ante entradas atípicas o ruidosas

`Classify` NO DEBE lanzar excepciones ante secuencias de símbolos, signos de puntuación repetidos, cadenas extremadamente largas o números sin contexto. Ante entradas ininteligibles o no reconocibles, DEBE devolver un `IntentResult` válido (degradando a `Intent.Desconocida` si no supera los umbrales de confianza).

#### Scenario: Entrada de símbolos y puntuación
- Dado un clasificador de intenciones listo
- Cuando se clasifica `"¡¡¡???... %$$#"`
- Entonces la llamada finaliza sin lanzar excepciones y devuelve un `IntentResult` válido

#### Scenario: Cadena extremadamente larga
- Dado un clasificador de intenciones listo
- Y una cadena de más de 5000 caracteres
- Cuando se ejecuta `Classify`
- Entonces no produce desbordamiento ni excepciones y devuelve un `IntentResult` válido

#### Scenario: Entrada numérica
- Dado un clasificador de intenciones listo
- Cuando se clasifica `"12345 67890"`
- Entonces no lanza excepciones y devuelve un `IntentResult` válido

---

### Requirement: Comportamiento ante clasificador no listo

Cuando `IsReady` es `false`, la ejecución de `Classify` NO DEBE lanzar excepciones y DEBERÍA devolver `IntentResult.Unknown(latencyMs)` con `Confidence == 0f`.

#### Scenario: Inferencia con clasificador no inicializado
- Dado un clasificador cuyo `IsReady` es `false`
- Cuando se llama a `Classify("por favor")`
- Entonces no lanza excepciones
- Y devuelve `Intent.Desconocida` con `Confidence == 0f`

---

### Requirement: Conformidad con IntentClassifierContract

Toda implementación real del clasificador en `Runtime/Nlu/` DEBE heredar de `NpcAi.Core.Tests.IntentClassifierContract` en su suite de pruebas (`Tests/EditMode/Nlu/`) y pasar satisfactoriamente todas las pruebas contractuales heredadas sin requerir entorno de VR ni escena de Unity.

#### Scenario: Paso de la suite contractual
- Dado el ensamblado de pruebas `NpcAi.Nlu.Tests`
- Cuando se ejecutan las pruebas derivadas de `IntentClassifierContract`
- Entonces el 100% de las pruebas contractuales resultan en verde (Pass)
