# Propuesta: M1 — Reconocimiento de voz on-device para Quest (Runtime/Speech)

## Intent

`Runtime/Speech/` contiene hoy unicamente el doble `ScriptedSpeechToText`: el pipeline arranca
en texto y el usuario no puede hablarle al NPC. M1 entrega la primera implementacion real de
`ISpeechToText` para que la simulacion de sala de juntas capture voz es-CO y alimente a M2
sobre `Data/Corpus/`. Restricciones de producto ya fijadas por el usuario: **Meta Quest
standalone (Android arm64)**, **100% offline** y **estrategia de escucha configurable** (VAD
o pulsar-para-hablar), no una UX unica.

## Scope

### In Scope

- Implementacion real de `ISpeechToText` en `Runtime/Speech/`, como clase C# plana construible
  sin microfono ni modelo, con envoltura `MonoBehaviour` opcional para publicar en `UtteranceChannel`.
- Captura de audio propia de M1 (`UnityEngine.Microphone`, 16 kHz mono).
- Motor de reconocimiento offline detras de una costura **interna al modulo** (`IRecognitionEngine`),
  nunca un puerto nuevo en `NpcAi.Core`.
- Dos estrategias de disparo seleccionables por dato: `PulsarParaHablar` y `ActividadDeVoz` (VAD).
- Guarda de emision tardia (contador de generacion + revalidacion de `IsListening`) y marshalling
  al hilo principal de Unity.
- `Data/Speech/` — ScriptableObject de configuracion STT.
- `Tests/EditMode/Speech/` — clase gemela que hereda `SpeechToTextContract`, mas pruebas de guarda
  tardia, clamps y segmentacion.
- `package.json` — declarar la dependencia del motor elegido (el campo `dependencies` hoy no existe).

### Out of Scope

- PCVR/Windows y `UnityEngine.Windows.Speech.DictationRecognizer` (Windows 10 unicamente; descartado
  por plataforma).
- Cualquier respaldo en la nube o dependencia de red, y por tanto todo manejo de secretos/API keys.
- `Runtime/Core/`, `Runtime/CoreChannels/` y cualquier otro modulo (M2, M7, M11).
- Wake word, diarizacion, traduccion, puntuacion enriquecida, adaptacion de vocabulario.
- Presupuesto numerico de latencia: lo mide M11 (Harness), no este cambio.
- Modificar `ScriptedSpeechToText`: se confirma valido tal cual y debe seguir pasando el contrato.

## Capabilities

### New Capabilities

- `reconocimiento-voz-m1`: captura, segmentacion por estrategia configurable, transcripcion offline
  y emision de `Utterance` bajo el contrato v1.

### Modified Capabilities

- None. `contrato-nucleo-m0` y `canales-evento-nucleo-m0` no cambian.

## Approach

**Recomendacion: Vosk como motor primario, Sentis/Whisper como respaldo documentado, ambos detras
de la misma costura interna.**

| Criterio | Vosk (plugin nativo) | Sentis + Whisper ONNX |
|---|---|---|
| Perfil de latencia | Streaming en CPU; resultado final en el corte de silencio | Lote: la ventana del encoder es fija de 30 s aunque la frase dure 2 s, mas decodificacion autoregresiva |
| Huella en Quest | Modelo `es` pequeno (~40 MB), solo CPU; deja libre el presupuesto Sentis/GPU que M2 ya usa con MiniLM | Segundo modelo sobre el mismo runtime Sentis, compitiendo con el render a 72-90 Hz |
| Exactitud es-CO | Modelo pequeno entrenado mayoritariamente en es europeo; sin puntuacion ni mayusculas | Whisper multilingue es mas fuerte, pero en Quest solo caben tiny/base, y tiny alucina sobre silencio y ruido |
| Confianza | `conf` por palabra (lattice Kaldi) mapea directo a `Utterance.Confidence` | Sin confianza calibrada; habria que derivarla de logprobs |
| VAD | Endpointing por silencio incluido en el motor | Habria que construirlo aparte |
| Esfuerzo / riesgo | Primer plugin nativo del repo: `.so` arm64-v8a + `.dll` x64 y P/Invoke | Preprocesado mel/STFT + tokenizer + bucle de decodificacion; sin precedente de inferencia de audio en el repo |

**Razon de la decision.** El consumidor de `Utterance.Text` es un clasificador de intencion (M2),
no un dictado literal: tolera WER moderado, pero **no** tolera latencia de turno ni frases
alucinadas sobre silencio en modo VAD. Eso inclina la balanza a Vosk pese al costo del plugin
nativo. El riesgo de esa apuesta se acota con la costura `IRecognitionEngine`: migrar al respaldo
Sentis/Whisper seria una clase adaptadora nueva mas un cambio de dato, no una reescritura del modulo.

**El contrato queda intacto.** `StartListening`/`StopListening` delimitan la *ventana de escucha*,
no la frase; la estrategia decide como se segmenta dentro de esa ventana. En `PulsarParaHablar` la
ventana es la frase (boton abre y cierra); en `ActividadDeVoz` la ventana sigue abierta y cada corte
del endpointer produce un `OnUtterance`. Ninguna de las dos requiere que `Start`/`Stop` bloqueen.
Contra la emision tardia: cada arranque incrementa un contador de generacion, el callback del motor
captura la generacion vigente y su resultado se descarta si no coincide o si `IsListening == false`
al momento de invocar (misma invariante que el `if (!IsListening) return false;` del doble). M1
calcula y acota `Confidence` a [0,1] y `DurationSeconds` a >= 0 antes de construir `Utterance`
(G14 es contrato del productor). **Confirmado con la exploracion: no hay brecha en el contrato
congelado; `Runtime/Core/` no se toca.**

**Costura de prueba.** El motor se inyecta; sin motor la clase sigue construible y pasa el contrato.
Un metodo `internal` empuja un resultado al mismo camino de emision, despues de las guardas, expuesto
con `InternalsVisibleTo("NpcAi.Speech.Tests")`; `EmitTestUtterance` lo invoca, igual que `Emit()` en
el doble. Cero microfono, cero modelo, cero hardware en EditMode.

**Configuracion como dato** (`Data/Speech/`, regla dura 7): estrategia de disparo, umbrales VAD
(energia, ms minimos de voz, ms de silencio para cortar, segundos maximos por frase), referencia y
ruta del modelo, tasa de muestreo y etiqueta de idioma. Cambiar de VAD a pulsar-para-hablar edita el
asset, no una clase. **Sin campo de confianza minima**: la Decision del usuario 3 (mas abajo) prohibe
filtrar por confianza, asi que ese campo no existe en el asset — corrige una mencion de una version
anterior de esta seccion, anterior a esa decision.

## Affected Areas

| Area | Impacto | Descripcion |
|---|---|---|
| `Runtime/Speech/` | Nuevo | Adaptador real, captura, estrategias de disparo, costura de motor. |
| `Runtime/Speech/Plugins/` | Nuevo | Binarios nativos del motor + sus `.meta`, dentro del modulo para no romper la frontera. |
| `Runtime/Speech/Fakes/ScriptedSpeechToText.cs` | Sin cambio | Confirmado valido; sigue pasando el contrato. |
| `Runtime/Speech/NpcAi.Speech.asmdef` | Modificado | Referencias siguen siendo `NpcAi.Core` + `NpcAi.Core.Channels`; se agrega `InternalsVisibleTo`. |
| `Data/Speech/` | Nuevo | ScriptableObject de configuracion STT. |
| `Tests/EditMode/Speech/` | Nuevo | Clase gemela del contrato + pruebas de guardas, clamps y segmentacion. |
| `package.json` | Modificado | Declarar la dependencia del motor. |
| `Runtime/Core/`, resto de modulos | Sin cambio | Frontera dura: un cambio, un modulo. |

## Risks

| Riesgo | Prob. | Mitigacion |
|---|---|---|
| Licencias del motor y del modelo `es` | Resuelto | **Verificado 2026-09-01**: `vosk-api` (libreria/binarios nativos) esta bajo Apache License 2.0, confirmado en `COPYING` del repo `alphacep/vosk-api`. El modelo `vosk-model-small-es-0.42` (39 MB, el candidato para Quest) tambien esta bajo Apache 2.0, segun la tabla oficial `alphacep/vosk-space/models.md`. Apache 2.0 es permisiva: permite vendorizar binarios y modelo en el paquete UPM del trabajo de grado sin restriccion copyleft; unica obligacion es acompanar el binario/modelo con el texto de la licencia (y el `NOTICE` si el upstream trae uno) dentro de `Runtime/Speech/Plugins/`. Ya no es bloqueante. |
| Primer plugin nativo del repo (empaquetado UPM, `.meta` de importador, arm64) | Alta | Costura `IRecognitionEngine`; si el empaquetado falla, se cae al respaldo Sentis sin tocar el adaptador. |
| Entrega del modelo (~40 MB) desde un paquete UPM al APK de Quest | Alta | `sdd-design` decide el mecanismo (StreamingAssets del proyecto anfitrion, Addressables o `TextAsset`); no se resuelve aqui. |
| Exactitud es-CO insuficiente para M2 | Media | Validacion manual contra `Data/Corpus/`; sin filtro de confianza minima (Decision del usuario 3), M2 absorbe la variacion; respaldo a modelo `es` grande o a Whisper. |
| Sin CI ni hardware en fases de agente | Alta | El contrato se prueba en EditMode via costura; la validacion en Quest es compuerta humana. |
| VAD dispara frases espurias con ruido de sala | Media | Umbrales de VAD (energia, ms minimos, ms de silencio) como dato, y `PulsarParaHablar` como respaldo operativo por configuracion. No se usa confianza minima como mitigacion: la Decision del usuario 3 prohibe filtrar por confianza. |
| Emision tardia tras `StopListening` rompe el contrato | Media | Contador de generacion + revalidacion de `IsListening`, con prueba dedicada. |
| Presupuesto de revision de 800 lineas | Media | Candidato natural a PRs encadenados si igual se excede: (1) adaptador + contrato + doble, (2) motor nativo, (3) datos y wiring. `sdd-tasks` decide. |

## Rollback Plan

Todo `Runtime/Speech/` salvo el doble es codigo nuevo. Revertir los commits del cambio deja el modulo
con solo `ScriptedSpeechToText` y el sistema queda exactamente como hoy: ningun consumidor fuera del
modulo depende de la clase nueva, porque el acoplamiento es via `ISpeechToText` y `UtteranceChannel`,
ambos del contrato congelado. No hay migracion de datos, no sube `Contract.Version` y `Data/Speech/`
se elimina sin efectos en M3/M5. Si solo falla el motor nativo, se revierte el commit del plugin y el
adaptador queda operando con el doble o con el respaldo Sentis.

## Dependencies

- Licencias verificadas (ver `Risks`): Apache 2.0 en `vosk-api` y `vosk-model-small-es-0.42`. Ya no bloquea.
- Decision de entrega del binario del modelo desde el paquete UPM (fase `sdd-design`).
- Quest fisico + Unity 6 para la validacion manual; EditMode no la cubre.
- Al vendorizar el binario/modelo en `Runtime/Speech/Plugins/`, incluir el texto de la licencia Apache 2.0 (y `NOTICE` si el upstream trae uno) junto a los archivos, por requisito de atribucion de la licencia.

## Success Criteria

- [x] `SpeechToTextContract` pasa con la implementacion real en EditMode, sin microfono ni modelo cargado. Confirmado: `OfflineSpeechToTextTests : SpeechToTextContract` (constructor sin parametros, sin motor ni microfono), 10 pruebas heredadas, CONFIRMADO EN VERDE por el usuario 2026-09-01.
- [x] No se emite ninguna `OnUtterance` tras `StopListening`, probado con una emision tardia simulada. Confirmado: `OfflineSpeechToTextGuardTests.Resultado_que_resuelve_despues_de_StopListening_no_se_emite`, CONFIRMADO EN VERDE 2026-09-01.
- [x] Pasar de `PulsarParaHablar` a `ActividadDeVoz` no requiere recompilar: solo editar el asset de `Data/Speech/`. Confirmado en la practica: el usuario cambio `Boardroom.asset.Estrategia` de `PulsarParaHablar` a `ActividadDeVoz` sin tocar codigo ni recompilar, para la verificacion en Quest (tarea 4.7).
- [x] Un build Android para Quest transcribe es-CO sin red y produce `Utterance` con `Confidence` en [0,1] y `DurationSeconds >= 0`. **CONFIRMADO 2026-09-03 en un Meta Quest 3 fisico**, completamente offline: `texto="prueba de sonido uno" confidence=1,00 duration=2,88s`, mas dos capturas adicionales con `Confidence` 1,00 y 0,99. Ver Engram `sdd/m1-speech-to-text/apply-progress`.
- [x] El diff no toca rutas fuera de `Runtime/Speech/`, `Data/Speech/`, `Tests/EditMode/Speech/` y `package.json`. Verificado por `sdd-verify` via `git status`: cero cambios en `Runtime/Core/`/`Runtime/CoreChannels/`, `package.json` intacto (Vosk quedo vendorizado, no como dependencia UPM — ver Approach). Unica excepcion, ya documentada: `.gitattributes` en la raiz del repo, necesaria para declarar el tracking de Git LFS de los binarios de `Runtime/Speech/Plugins/` antes del primer commit.
- [x] Licencias verificadas y registradas en el cambio (Apache 2.0, ver `Risks`).

## Decisiones del usuario (confirmadas 2026-09-01)

1. **Motor:** se acepta Vosk como motor primario. Licencia verificada 2026-09-01: `vosk-api` y
   `vosk-model-small-es-0.42` estan bajo Apache 2.0 (ver `Risks`), sin restriccion para vendorizar en
   el paquete UPM. Ya no es condicional; sigue disponible el respaldo Sentis/Whisper documentado en
   `Approach` si en `sdd-design` o `sdd-apply` aparece un impedimento tecnico, gracias a la costura
   `IRecognitionEngine`.
2. **Alcance de la configuracion:** la estrategia de disparo (VAD / PulsarParaHablar) se define **por
   escenario/sala**, no global — cada escenario (ej. Boardroom) tiene su propio asset en `Data/Speech/`
   segun el ambiente acustico donde se vaya a usar.
3. **Confianza baja:** M1 **emite igual** el `Utterance` con su `Confidence` baja, sin filtrar ni
   descartar. El clasificador de intencion (M2) decide si responde `Desconocida`. M1 no bifurca logica
   segun el contenido ni la confianza del texto — coherente con que el struct no valida ni recorta
   rangos.
