# Propuesta: M8 — Presentador de NPC (voz sintetizada + animación)

## Intent

`Docs/MODULES.md` documenta hoy a M8 como **"solo doble"**: `Runtime/Presentation/` en
`origin/main` únicamente contiene `Fakes/RecordingNpcPresenter.cs` — un sumidero que guarda cada
`NpcReply` en una lista (`Played`, `HasPlayed`, `Last`) para que una prueba pueda afirmar sobre
ello. No sintetiza voz ni anima nada. No existe `openspec/specs/presentador-*` ni carpeta de
cambio: M8 nunca se formalizó.

M8 es la **última milla** del pipeline. Todo lo de arriba
(`M1 ISpeechToText → M2 IIntentClassifier → (M15 | M6) → M4 IReceptivityEngine`) produce un
`NpcReply` = `{ Text, EmotionTag, AnimationCue }` que nadie oye ni ve. M8 lo convierte en voz
sintetizada del `Text` (español) y en animación del NPC a partir de `EmotionTag` y
`AnimationCue`. Es el espejo de M1: M1 es voz-que-entra (STT), M8 es voz-que-sale (TTS) más
animación.

Este cambio implementa `NpcPresenter` (implementación real de `INpcPresenter`) con la misma
arquitectura en capas que M1: un núcleo C# construible sin escena ni motor, dos costuras internas
(síntesis y animación), una envoltura `MonoBehaviour` que se cablea a la escena anfitriona, y
—en su propio PR— el motor de TTS real: **Piper** on-device y offline, con binarios nativos por
plataforma y la voz pre-entrenada empaquetada como asset, calcado del PR de Vosk en M1.

Decisiones ya fijadas antes de este cambio (confirmadas con el usuario el 2026-09-10):

1. **Motor de TTS**: **Piper** (clase VITS/ONNX), on-device y 100% offline en inferencia, voces
   en español, licencia MIT. Espejo de la decisión de Vosk para M1: coherente con "ligero,
   económico y funcional sin dependencia de servicios externos".
2. **Arquitectura**: en capas como M1 — núcleo puro probable en EditMode + costura `ISpeechSynthesizer`
   con doble silencioso + motor Piper detrás en un PR aparte.
3. **Costo real**: inferencia on-device gratuita y offline. Descargar la voz Piper pre-entrenada
   requiere internet **una sola vez** (igual que el encoder de M2); tras empaquetarla, el paquete
   corre sin conexión.

## Scope

### In Scope

- `Runtime/Presentation/NpcPresenter.cs` — núcleo `INpcPresenter`: guardas, despacho a las dos
  costuras, construible sin `Animator` ni motor.
- `Runtime/Presentation/ISpeechSynthesizer.cs` + `IAnimationDriver.cs` — puertos internos de M8
  (no van a `NpcAi.Core`).
- `Runtime/Presentation/Fakes/SilentSpeechSynthesizer.cs` + `RecordingAnimationDriver.cs` —
  dobles deterministas de las dos costuras.
- `Runtime/Presentation/NpcPresenterBehaviour.cs` — `MonoBehaviour`: se suscribe al
  `NpcReplyChannel` (ya existe en `Runtime/CoreChannels/`), posee un `AudioSource`, cablea un
  `Animator` inyectado por la escena, bombea la reproducción en el hilo principal.
- `Runtime/Presentation/Config/` — `PresentationSettings` (snapshot puro) +
  `PresentationSettingsAsset : ScriptableObject` con `OnValidate` (espejo de `SpeechSettingsAsset`).
- `Runtime/Presentation/Piper/` — `PiperSpeechSynthesizer : ISpeechSynthesizer` + `PiperInterop`
  (P/Invoke), y `Runtime/Presentation/Model/VoiceProvisioner.cs` (extrae la voz a
  `Application.persistentDataPath` en el primer arranque).
- `Runtime/Presentation/Plugins/` — binarios nativos de Piper por plataforma
  (`Windows/x86_64`, `Android/arm64-v8a`) y la voz `es_*` empaquetada como `.bytes` + su `.json`.
  Git LFS, siguiendo el precedente de `.gitattributes` que dejó M1.
- `Data/Presentation/<escenario>.asset` (config por escenario) + `Data/Presentation/README.md`.
- `Tests/EditMode/Presentation/` — `NpcPresenterTests : NpcPresenterContract` +
  `AnimationDriverTests`, `PresentationSettingsAssetTests`, `NpcPresenterBehaviourWiringTests`.
- `openspec/specs/presentador-npc-m8/spec.md` — primera spec formal de M8.
- `Docs/MODULES.md` (sección M8), cerrando el estado "solo doble".

### Out of Scope

- **`INpcPresenter` no cambia.** Ni una firma nueva: este cambio implementa el puerto, no lo
  modifica. No es cambio de M0.
- **`RecordingNpcPresenter` (el doble) no cambia.** Sigue existiendo, sigue pasando
  `NpcPresenterContract`, y M11 lo usa donde prefiera evitar audio real.
- **Rig, avatar, prefabs, escena y navegación del NPC**: son del **proyecto VR anfitrión** (lo
  dice el proposal de M11). M8 aporta el motor de TTS y un driver que traduce los cues a
  parámetros de un `Animator` que la escena le inyecta; no trae rig ni controller propios.
- **`Data/Npcs/*.json` con `vozId`**: lo crea **M11** (su proposal ya lo define). M8 solo define
  cómo se lee un `vozId` y lo mapea a una voz Piper vía la config.
- **Lip-sync / visemas**: sincronía labial a partir del audio o del texto. Mejora de seguimiento
  si el rig del anfitrión la soporta; no entra en esta primera entrega.
- **M10 y otros escenarios**: M8 es agnóstico de escenario; agregar una config de escenario es un
  `.asset` nuevo, no toca `Runtime/Presentation/`.
- **Segunda voz (mayor / joven)**: la primera entrega trae **una** voz por defecto. Agregar otra
  es un `.bytes` más + una entrada en el mapa de la config, sin tocar código.

## Capabilities

### New Capabilities

- `presentador-npc-m8` (nuevo `openspec/specs/presentador-npc-m8/spec.md`): primera spec formal
  de M8. Formaliza el comportamiento que `NpcPresenterContract.cs` ya fija (Play normal no lanza,
  `Play(default)` no lanza, 10 reproducciones encadenadas no lanzan) más las garantías nuevas de
  esta implementación (texto vacío no invoca al sintetizador, cue desconocido no lanza, la
  envoltura se suscribe y se desuscribe del canal sin fugas, síntesis fuera del hilo principal),
  siguiendo el mismo patrón que `reconocimiento-voz-m1` hizo sobre `ISpeechToText`.

### Modified Capabilities

- Ninguna: `INpcPresenter` (parte de `contrato-nucleo-m0`) y `NpcReplyChannel` (parte de
  `canales-evento-nucleo-m0`) no cambian.

## Approach

**El seam ya existe y ya está probado**: `INpcPresenter.Play(NpcReply)` es la única superficie
que ve el resto del sistema, y el `NpcReplyChannel` (`EventChannel<NpcReply>`) ya está en
`Runtime/CoreChannels/Channels.cs`. `NpcPresenter` es un adaptador nuevo detrás de esa interfaz —
mismo patrón que `VoskRecognitionEngine`/`OfflineSpeechToText` para M1.

**Capas (espejo de M1):**

| Capa | Tipo | Responsabilidad | Hilo |
|---|---|---|---|
| Núcleo | `NpcPresenter : INpcPresenter` | guardas, despacho a las dos costuras | Cualquiera (C# puro) |
| Costura TTS | `ISpeechSynthesizer` / `PiperSpeechSynthesizer` | texto → PCM | Trabajador |
| Costura animación | `IAnimationDriver` | `EmotionTag`/`AnimationCue` → params del `Animator` | Principal |
| Envoltura | `NpcPresenterBehaviour : MonoBehaviour` | suscripción al canal, `AudioSource`, bomba, `Animator` de la escena | Principal |

**Por qué Piper y no una API de nube**: la propuesta del proyecto fija TTS on-device (igual que
STT con Vosk y NLU con Sentis). Piper corre en CPU a partir de un `.onnx` de voz — sin
entrenamiento, sin GPU, sin costo recurrente ni internet en inferencia. La única dependencia de
red es descargar la voz pre-entrenada **una vez**, como el encoder de M2.

**Entrega de la voz**: la voz Piper (`.onnx` + `.json` de config) viaja empaquetada como
`.bytes` y se descomprime a `Application.persistentDataPath/NpcAi/Presentation/<idDeVoz>/` en el
primer arranque, con un centinela `.listo`. Es la Decisión 1 de M1 aplicada tal cual: en Android
nada dentro del APK es una ruta de archivo real, así que la extracción es obligatoria de todos
modos y un único asset comprimido es lo que el pipeline de assets transporta intacto.

**Topología de hilos**: `Play` encola el texto; un trabajador sintetiza con Piper (CPU-bound) y,
al terminar, hace `Post` del PCM en un `IMainThreadPump`; `Update`/la bomba lo pasa al
`AudioSource`. Sintetizar en el hilo principal rompería el presupuesto de frame del Quest;
`AudioSource.Play` es API de Unity, hilo principal. Espejo de la Decisión 3 de M1.

**La animación es dato**: `IAnimationDriver` traduce `(EmotionTag, AnimationCue)` a triggers/params
de un `Animator` usando un mapa que viene de la config (`Data/Presentation/*.asset`), no
hardcodeado. El `Animator` concreto lo inyecta la escena; sin `Animator`, el driver es no-op.

## Affected Areas

| Área | Impacto | Descripción |
|---|---|---|
| `Runtime/Presentation/NpcPresenter.cs` | Nuevo | Núcleo `INpcPresenter` |
| `Runtime/Presentation/ISpeechSynthesizer.cs`, `IAnimationDriver.cs` | Nuevo | Puertos internos de M8 |
| `Runtime/Presentation/Fakes/` | Nuevo | Dobles de las dos costuras |
| `Runtime/Presentation/NpcPresenterBehaviour.cs` | Nuevo | Envoltura MonoBehaviour + canal |
| `Runtime/Presentation/Config/` | Nuevo | `PresentationSettings` + asset SO |
| `Runtime/Presentation/Piper/`, `Model/` | Nuevo | Motor Piper + P/Invoke + aprovisionamiento de voz |
| `Runtime/Presentation/Plugins/` | Nuevo (binarios, LFS) | libpiper + runtime + voz `es_*` |
| `Data/Presentation/` | Nuevo | Config por escenario + README |
| `Tests/EditMode/Presentation/` | Nuevo | Contrato heredado + pruebas de costuras y cableado |
| `.gitattributes` | Modificado | Regla LFS para `Runtime/Presentation/Plugins/**` |
| `openspec/specs/presentador-npc-m8/spec.md` | Nuevo | Primera spec formal de M8 |
| `Docs/MODULES.md` | Modificado | Cerrar "solo doble" en la sección M8 |
| `Runtime/Presentation/Fakes/RecordingNpcPresenter.cs` | Sin cambio | Sigue siendo el doble |
| `Runtime/Core/`, `Runtime/CoreChannels/` | Sin cambio | `INpcPresenter` y `NpcReplyChannel` no cambian |

## Risks

| Riesgo | Prob. | Mitigación |
|---|---|---|
| Embeber Piper en Android/Quest (onnxruntime nativo + datos de espeak-ng para fonemización) es más complejo que Vosk | Alta | **Spike técnico de Fase 3** con la voz candidata real antes de comprometer el stack, igual que "1.2 Spike de Sentis" en M1. Alternativas: `libpiper`, onnxruntime directo + `piper-phonemize`, o proceso `piper` en escritorio para PR3 y nativo después |
| Tamaño de la voz `.onnx` + runtime infla el build | Media | Una sola voz por defecto en la primera entrega; `.gitattributes` con LFS antes del primer binario; voz "low"/"medium" de Piper, no "high" |
| Latencia de síntesis perceptible antes de que el NPC hable | Media | Topología de hilos (síntesis en trabajador); si molesta, cachear frases frecuentes es iteración posterior. La calidad/latencia se mide en el spike |
| El doble silencioso (`SilentSpeechSynthesizer`) oculta bugs de audio hasta el PR3 | Media | `NpcPresenterBehaviourWiringTests` afirma que el `AudioSource` recibe un `AudioClip` no vacío cuando la costura devuelve PCM; el PR3 agrega una prueba manual con audio real |
| Nombres de params/triggers del `Animator` dependen del rig del anfitrión, que no existe aún | Media | El mapa cue→trigger es **dato** (`Data/Presentation/*.asset`); M8 se prueba con un `Animator` de mentira que registra las llamadas. El rig real lo cablea M11/el anfitrión |
| `NpcPresenterBehaviour` no se desuscribe del canal y fuga entre escenas | Media | `EventChannel.OnDisable` limpia listeners, y el behaviour se suscribe en `OnEnable` / desuscribe en `OnDisable`; `NpcPresenterBehaviourWiringTests` lo verifica |

## Rollback Plan

`NpcPresenter` y todo lo de M8 son aditivos sobre un puerto que ya existe; `RecordingNpcPresenter`
no se toca. Revertir los commits de este cambio deja M8 en el estado "solo doble" que ya está en
`main` — el pipeline sigue funcionando (M11 usa el doble), porque ningún otro módulo depende de
`NpcPresenter` por nombre, solo de `INpcPresenter`. No hay migración de datos ni cambio de
`Contract.Version`.

## Dependencies

- `contrato-nucleo-m0`, archivado y estable: define `INpcPresenter`, `NpcReply`. No se modifica.
- `canales-evento-nucleo-m0`: `NpcReplyChannel` (`EventChannel<NpcReply>`) ya existe. No se
  modifica.
- Un `Animator` del **proyecto anfitrión**, inyectado a `NpcPresenterBehaviour` por Inspector.
- `com.unity.nuget.newtonsoft-json` o `JsonUtility` para leer el `.json` de la voz Piper (se
  decide en el spike).
- Dependencia de red **puntual y de una sola vez**: descargar la voz Piper pre-entrenada. Tras
  empaquetarla, cero conexión.
- Entorno de build con Git LFS (ya en uso por M1).

## Success Criteria

- [x] `NpcPresenter` pasa exactamente la misma batería `NpcPresenterContract` que
      `RecordingNpcPresenter`, sin modificar la clase base.
- [x] `Play(default)` y `Play` con `Text` vacío o solo espacios NO invocan al `ISpeechSynthesizer`
      y NO lanzan.
- [x] `Play` con un `EmotionTag`/`AnimationCue` fuera del mapa de la config NO lanza (no-op de
      animación) y el `Text` igual se sintetiza.
- [x] `NpcPresenterBehaviour` se suscribe al `NpcReplyChannel` en `OnEnable` y se desuscribe en
      `OnDisable`; una prueba lo verifica sin fugas.
- [x] La síntesis ocurre fuera del hilo principal; la entrega al `AudioSource` ocurre en el hilo
      principal a través de la bomba.
- [x] Con Piper real: `Play(new NpcReply("Buenas, ¿en qué le ayudo?", ...))` produce audio
      audible en español en el Editor de escritorio. Confirmado por el usuario el 2026-09-14
      (20 repeticiones sin fugas ni bloqueo del hilo principal).
- [x] `openspec/specs/presentador-npc-m8/spec.md` existe y traza cada requisito a una prueba
      EditMode en verde. (Ruta real de este cambio:
      `openspec/changes/2026-09-10-m8-presentador-npc/specs/presentador-npc-m8/spec.md`,
      pendiente de promover a `openspec/specs/` al archivar — tasks.md 5.3.)
- [ ] `Docs/MODULES.md` (sección M8) deja de decir "solo doble".
- [ ] El diff no toca `Runtime/Core/`, `Runtime/CoreChannels/`, ni ninguna carpeta fuera de
      `Runtime/Presentation/`, `Data/Presentation/`, `Tests/EditMode/Presentation/`, `Docs/`,
      `openspec/` y `.gitattributes`.

## Decisiones del usuario (confirmadas 2026-09-10)

1. **Motor de TTS**: Piper nativo on-device y offline, espejo de la arquitectura de Vosk en M1 —
   no una API de nube, no un modelo entrenado por el equipo.
2. **Costo/dependencias**: inferencia 100% offline y sin costo recurrente; el empaquetado de la
   voz tiene una dependencia de internet puntual (descarga única de la voz pre-entrenada) que no
   se repite después.
