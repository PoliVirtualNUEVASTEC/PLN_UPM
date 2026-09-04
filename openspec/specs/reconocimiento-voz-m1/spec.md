# Especificacion: reconocimiento-voz-m1

## Purpose

Define el comportamiento observable de la implementacion real de `ISpeechToText` en `Runtime/Speech/` (M1). No reescribe ni modifica el contrato congelado `ISpeechToText` de `contrato-nucleo-m0`, sino que agrega capacidades de segmentacion por estrategia configurable, guarda de emision tardia y emision sin filtrar por confianza sobre ese puerto existente. La implementacion DEBE pasar `SpeechToTextContract` en EditMode sin microfono ni modelo, y DEBE funcionar completamente offline en Android Quest arm64 con transcripcion es-CO real.

## ADDED Requirements

### Requirement: Estrategia de disparo configurable por escenario

La implementacion real de `ISpeechToText` DEBE soportar dos estrategias de disparo seleccionables por dato: `PulsarParaHablar` (la ventana de escucha coincide con la frase; una senal explicita de inicio/fin la delimita) y `ActividadDeVoz` (la ventana permanece abierta entre `StartListening` y `StopListening`; cada corte de silencio del motor produce un `OnUtterance`). La estrategia activa DEBE ser dato con alcance por escenario (un asset de `Data/Speech/` por escenario, p. ej. Boardroom), no un valor unico para todo el paquete. Cambiar de estrategia NO DEBE requerir recompilar.

#### Scenario: PulsarParaHablar limita la ventana a la frase

- Dado un sujeto con estrategia `PulsarParaHablar` y un suscriptor a `OnUtterance`
- Cuando se llama `StartListening`, el motor resuelve una frase y luego se llama `StopListening`
- Entonces el suscriptor recibe exactamente un `Utterance` para esa ventana

#### Scenario: ActividadDeVoz produce un Utterance por corte de silencio

- Dado un sujeto con estrategia `ActividadDeVoz`, escuchando, con un suscriptor
- Cuando el motor reporta dos cortes de silencio sin que se llame `StopListening` entre ellos
- Entonces el suscriptor recibe dos `Utterance`, uno por corte

### Requirement: Guarda de emision tardia tras StopListening

La implementacion real DEBE descartar cualquier resultado del motor que resuelva despues de `StopListening`, aun si la llamada asincrona empezo mientras se escuchaba. DEBE usar un contador de generacion que se incrementa en cada `StartListening`; el callback del motor DEBE capturar la generacion vigente al iniciar y, al momento de emitir, DEBE revalidar que la generacion coincide y que `IsListening == true`. Si alguna condicion falla, el resultado NO DEBE emitirse.

#### Scenario: Resultado tardio tras Stop se descarta

- Dado un sujeto escuchando con una llamada al motor en curso, y un suscriptor
- Cuando se llama `StopListening` antes de que el motor resuelva, y luego el motor resuelve
- Entonces el suscriptor no recibe nada

#### Scenario: Resultado de una generacion anterior no se emite en la vigente

- Dado un sujeto que hizo `Start`, `Stop` y `Start` de nuevo, con una llamada del motor pendiente de la primera ventana
- Cuando esa llamada pendiente resuelve durante la segunda ventana
- Entonces el suscriptor no recibe ese resultado

### Requirement: Paso de confianza sin filtrar, con rangos acotados

La implementacion real DEBE emitir cada `Utterance` reconocido tal cual, incluidos los de confianza baja: NO DEBE filtrar, descartar ni retener la emision segun `Confidence`. Esa decision es del consumidor (M2). Antes de construir el `Utterance`, DEBE calcular y acotar `Confidence` a `[0,1]` y `DurationSeconds` a `>= 0`.

#### Scenario: Una transcripcion de confianza baja se emite igual

- Dado un sujeto escuchando, con el motor devolviendo confianza `0.1`
- Cuando el motor resuelve
- Entonces el suscriptor recibe el `Utterance` con `Confidence == 0.1f`, sin descartarlo

#### Scenario: Confidence y DurationSeconds fuera de rango se acotan antes de emitir

- Dado un motor que reporta confianza fuera de `[0,1]` o duracion negativa
- Cuando la implementacion construye el `Utterance` a partir de ese resultado
- Entonces `Confidence` queda en `[0,1]` y `DurationSeconds` en `>= 0` antes de invocar `OnUtterance`

### Requirement: Costura de prueba determinista sin microfono ni modelo

La implementacion real DEBE construirse y pasar `SpeechToTextContract` en EditMode sin microfono, sin modelo cargado y sin hardware. DEBE exponer un metodo `internal` que empuje un resultado al mismo camino de emision que usa el motor real, despues de las guardas de ventana y de generacion, expuesto con `InternalsVisibleTo("NpcAi.Speech.Tests")`; la clase gemela de prueba lo usa para implementar `EmitTestUtterance`.

#### Scenario: El sujeto pasa el contrato sin motor real

- Dado un entorno EditMode sin microfono ni modelo cargado
- Cuando se construye la implementacion real y corre `SpeechToTextContract` sobre ella
- Entonces todas las pruebas del contrato pasan

#### Scenario: El metodo interno respeta las mismas guardas que el camino real

- Dado un sujeto que no esta escuchando
- Cuando la prueba invoca el metodo `internal` de emision de prueba
- Entonces no se emite `OnUtterance`, igual que con un resultado del motor real

### Requirement: Configuracion de STT como dato en Data/Speech/

La estrategia de disparo, los umbrales de VAD (energia, ms minimos de voz, ms de silencio para cortar, segundos maximos por frase), la referencia/ruta del modelo, la tasa de muestreo y la etiqueta de idioma DEBEN vivir como `ScriptableObject` o JSON bajo `Data/Speech/`, uno por escenario. Ninguno de estos valores DEBE estar fijo en codigo dentro de `Runtime/Speech/`. NO DEBE existir un campo de confianza minima para emitir: el requisito "Paso de confianza sin filtrar" prohibe cualquier umbral que condicione la emision de un `Utterance` a su `Confidence`.

#### Scenario: Los umbrales vienen del asset, no del codigo

- Dado dos assets de configuracion con distintos umbrales de VAD
- Cuando se construye el sujeto con cada asset
- Entonces cada sujeto opera con los umbrales de su propio asset

#### Scenario: No hay valores de configuracion embebidos en clases

- Dado el codigo fuente de `Runtime/Speech/`
- Cuando se revisa en busca de umbrales, rutas de modelo o estrategia fijos en codigo
- Entonces no se encuentra ninguno: todos provienen de `Data/Speech/`
