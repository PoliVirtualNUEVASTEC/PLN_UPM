# Design: M8 — Presentador de NPC (Runtime/Presentation)

## Módulo y frontera

Módulo único: `Runtime/Presentation/`, ensamblado `NpcAi.Presentation`. Su grafo de referencias
sigue siendo exactamente `NpcAi.Core` + `NpcAi.Core.Channels` (esta última porque el módulo se
suscribe a `NpcReplyChannel` por Inspector). **`NpcAi.Presentation.asmdef` no cambia su lista de
referencias.** `Runtime/Core/` y `Runtime/CoreChannels/` quedan intactos y `Contract.Version`
sigue igual. El `Animator` del NPC y el botón/flujo que dispara la conversación los cablea la
escena anfitriona; M8 **no** referencia `NpcAi.VrInput` ni `NpcAi.Scenarios`.

`Data/Presentation/` vive fuera de cualquier `.asmdef`: son assets `ScriptableObject` de config,
un archivo por escenario (mismo patrón que `Data/Speech/` para M1 y `Data/Personalities/` para M5).

## Technical Approach

Cinco capas, con el hilo en que corre cada una. El núcleo (`NpcPresenter`) es construible sin
`Animator`, sin motor de TTS y sin `AudioSource` — esa es la propiedad que hace pasable el
contrato en EditMode.

| Capa | Tipo | Responsabilidad | Hilo |
|---|---|---|---|
| Núcleo | `NpcPresenter : INpcPresenter` | guardas (`default`/texto vacío → no-op), despacho a las dos costuras | Cualquiera (C# puro) |
| Síntesis | `ISpeechSynthesizer` / `PiperSpeechSynthesizer` | `texto` → PCM mono `float[]` en `[-1,1]` | Trabajador |
| Animación | `IAnimationDriver` / `AnimatorDriver` | `(EmotionTag, AnimationCue)` → triggers/params de un `Animator` | Principal |
| Aprovisionamiento | `VoiceProvisioner` | descomprime la voz Piper a `persistentDataPath` en el primer arranque | Principal (I/O de disco al iniciar) |
| Envoltura | `NpcPresenterBehaviour : MonoBehaviour` | suscripción a `NpcReplyChannel`, `AudioSource`, bomba en `Update`, `Animator` inyectado | Principal |

## Architecture Decisions

### Decisión 1: la síntesis es una costura interna (`ISpeechSynthesizer`), no Piper directo

**Elección.** `NpcPresenter` habla con `ISpeechSynthesizer` — `EstaListo` +
`Sintetizar(string texto, string vozId, int tasaDeMuestreo) -> float[]` (mono, `[-1,1]`, `null`
o vacío si no puede). El doble `SilentSpeechSynthesizer` devuelve `float[0]`; `PiperSpeechSynthesizer`
llega en el PR3.

**Alternativa descartada.** Que `NpcPresenter` cargue y llame a Piper directamente.

**Razón.** Es lo que hace pasable el contrato en EditMode sin nativo ni voz, y lo que permite
meter Piper en su propio PR sin tocar el núcleo — exactamente `IRecognitionEngine` de M1. También
deja la puerta abierta a un `SentisPiperSynthesizer` u otra voz sin reescribir M8.

### Decisión 2: entrega de la voz Piper como `.bytes` empaquetado + extracción en runtime

**Elección.** La voz (`<voz>.onnx` + `<voz>.onnx.json`) viaja comprimida como un único
`Runtime/Presentation/Plugins/Voices/<idDeVoz>.bytes` (zip renombrado, importado como `TextAsset`),
referenciada por el asset de config. En el primer arranque, `VoiceProvisioner` la descomprime a
`Application.persistentDataPath/NpcAi/Presentation/<idDeVoz>/` y deja un centinela `.listo`; las
siguientes ejecuciones no hacen nada.

**Alternativa descartada.** `StreamingAssets` del proyecto anfitrión o Addressables.

**Razón.** Es la Decisión 1 de M1 aplicada tal cual: en Android nada dentro del APK es una ruta
de archivo real, y onnxruntime abre el modelo por ruta, así que la extracción a
`persistentDataPath` es obligatoria de todos modos. Un único asset comprimido es lo que el
pipeline de assets de Unity transporta intacto, y mantiene el paquete UPM autocontenido (`git
clone` y funciona), que es la propiedad que más vale en un trabajo de grado que se evalúa.
Implementación: `ZipArchive` sobre `MemoryStream` (`System.IO.Compression`, presente en .NET
Standard 2.0), con validación de traversal por entrada; `Resources.UnloadAsset` al terminar.

### Decisión 3: topología de hilos — síntesis en un trabajador, reproducción en el principal

**Elección.** `Play(reply)` valida y encola `reply` en una cola concurrente. Un hilo trabajador
la consume, llama `ISpeechSynthesizer.Sintetizar`, y al terminar hace `Post` del PCM (más
`reply.EmotionTag`/`reply.AnimationCue`) en un `IMainThreadPump`. `Update()` (o `Drenar()` en
hosting C#) drena la bomba: crea el `AudioClip`, lo pasa al `AudioSource`, y recién ahí llama al
`IAnimationDriver`.

**Alternativa descartada.** Sintetizar dentro de `Play`, en el hilo que lo llamó.

**Razón.** Piper es CPU-bound (decenas a cientos de ms por frase); hacerlo en el hilo principal
rompería el presupuesto de 72–90 Hz del Quest. `AudioClip.Create` y `AudioSource.Play` son API de
Unity y exigen hilo principal. Es la Decisión 3 de M1, y se puede **reusar `IMainThreadPump` /
`ImmediateMainThreadPump`** de `NpcAi.Speech`... salvo que eso cruzaría una referencia entre
módulos (M8 → M1), prohibida por la regla 3. Por eso M8 define su propio `IMainThreadPump` mínimo
en `Runtime/Presentation/Threading/` (misma forma, copia deliberada — o se promueve el tipo a
`NpcAi.Core` en un cambio de contrato aparte; ver Open Questions).

### Decisión 4: la animación es un mapa de datos, no código

**Elección.** `IAnimationDriver.Aplicar(string emotionTag, string animationCue)`. La
implementación real `AnimatorDriver` tiene un `Animator` inyectado y un mapa
`cue → (nombreDeParam, tipo)` que viene de `PresentationSettings`. Un `emotionTag`/`animationCue`
que no está en el mapa se ignora (no-op), no lanza. Sin `Animator`, el driver entero es no-op.

**Alternativa descartada.** `switch` fijo sobre los cues conocidos (`"asentir"`, `"cruzar_brazos"`,
`"idle"`, ...).

**Razón.** El rig y su `AnimatorController` son del proyecto anfitrión; los nombres de sus
parámetros no los conoce M8. Dejarlos como dato (regla dura 7) permite que cada rig del anfitrión
se conecte editando un `.asset`, sin tocar `Runtime/Presentation/`. El doble
`RecordingAnimationDriver` registra los `(emotionTag, animationCue)` recibidos para que una
prueba afirme sin `Animator`.

### Decisión 5: `Play(default)` y texto vacío son no-op total, sin lanzar

**Elección.** Si `reply.IsEmpty` (texto `null`/vacío/espacios), `Play` no encola nada: ni
síntesis ni animación. Si el texto es válido pero los tags no están en el mapa, se sintetiza el
texto y la animación es no-op.

**Razón.** El contrato exige que `Play(default)` no lance. M6 nunca devuelve `Text` vacío (tiene
respaldo escalonado), pero M15 puede devolver `ClinicalResponse.NoAplica` y un cableado
descuidado podría pasar un `default`; hay que blindarlo igual. Cubierto por
`NpcPresenterContract.Sobrevive_a_una_respuesta_vacia` + una prueba propia que afirma que el
sintetizador no fue invocado.

### Decisión 6: `vozId` como parámetro de la síntesis, mapeado en la config

**Elección.** `ISpeechSynthesizer.Sintetizar(texto, vozId, tasaDeMuestreo)`. `NpcPresenter`
recibe el `vozId` de su construcción (estado de sesión, no de turno: el NPC no cambia de voz
entre frases). El mapa `vozId → idDeVoz Piper` está en `PresentationSettings`; `vozId` vacío o
desconocido → voz por defecto de la config.

**Razón.** `Data/Npcs/*.json` (de M11) trae `vozId` por NPC para distinguir "señora mayor" de
"joven". M8 define el contrato de lectura; M11 crea los archivos y elige el NPC. Agregar una voz
es un `.bytes` + una fila en el mapa.

### Decisión 7: primera entrega con una sola voz

**Elección.** Una voz Piper en español por defecto (p. ej. `es_ES-*-medium` o `es_MX-*`), elegida
en el spike de Fase 3 por tamaño y naturalidad. La segunda voz (contraste etario) se agrega
después sin tocar código.

**Razón.** No inflar el build ni el spike antes de validar que el stack de Piper funciona
on-device. El mapa de voces ya soporta N voces desde el día uno.

### Decisión 8: config `Data/Presentation/<escenario>.asset`, acotada en `OnValidate`

**Elección.** `PresentationSettingsAsset : ScriptableObject` con: mapa de voces
(`vozId → idDeVoz`), voz por defecto, `TasaDeMuestreo` (default 22050 — Piper suele emitir a
22.05 kHz), `Velocidad` (multiplicador), `TextAsset` de la voz empaquetada, `IdDeVoz` (sello de
aprovisionamiento), y el mapa `cue → parámetro del Animator`. `OnValidate()` acota los numéricos.
`PresentationSettings` es el snapshot puro que consume el núcleo. Espejo de `SpeechSettingsAsset`.

**Razón.** Regla dura 7: lo variable es dato. Un escenario nuevo (o un rig nuevo) es un `.asset`
nuevo, sin tocar `Runtime/Presentation/`.

### Decisión 9: gate humano EditMode + prueba manual de audio

**Elección.** Cada verde de Test Runner es compuerta humana (como M1/M2/M4/M5/M6). PR1 y PR2 se
validan en EditMode sin audio real. PR3 agrega una prueba manual: en el Editor de escritorio,
`Play` de una frase produce audio audible en español.

## Data Flow

    M11: NpcReplyChannel.Raise(reply)
              │
              ▼
    NpcPresenterBehaviour.OnReply(reply)      (hilo principal, callback del canal)
              │
              ▼
    NpcPresenter.Play(reply)
              │  reply.IsEmpty ? -> return (no-op)
              ▼
    cola concurrente  ──►  hilo trabajador:
                              ISpeechSynthesizer.Sintetizar(reply.Text, vozId, tasa) -> float[] pcm
                              IMainThreadPump.Post( (pcm, reply.EmotionTag, reply.AnimationCue) )
              │
              ▼  Update() / Drenar()  (hilo principal)
    ┌─────────────────────────────────────────────┐
    │ AudioClip.Create(pcm) -> AudioSource.clip     │
    │ AudioSource.Play()                            │
    │ IAnimationDriver.Aplicar(emotionTag, cue)     │
    └─────────────────────────────────────────────┘

## File Inventory

| Archivo | Rol | PR |
|---|---|---|
| `Runtime/Presentation/NpcPresenter.cs` | núcleo `INpcPresenter` | PR1 |
| `Runtime/Presentation/ISpeechSynthesizer.cs` | puerto interno TTS | PR1 |
| `Runtime/Presentation/IAnimationDriver.cs` | puerto interno animación | PR1 |
| `Runtime/Presentation/Threading/IMainThreadPump.cs` + `ImmediateMainThreadPump.cs` + `QueuedMainThreadPump.cs` | bomba al hilo principal (copia mínima del patrón de M1) | PR1 |
| `Runtime/Presentation/Fakes/SilentSpeechSynthesizer.cs` | doble: `float[0]` | PR1 |
| `Runtime/Presentation/Fakes/RecordingAnimationDriver.cs` | doble: registra `(tag, cue)` | PR1 |
| `Runtime/Presentation/Config/PresentationSettings.cs` | snapshot puro | PR2 |
| `Runtime/Presentation/Config/PresentationSettingsAsset.cs` | SO + `OnValidate` | PR2 |
| `Runtime/Presentation/AnimatorDriver.cs` | `IAnimationDriver` real sobre un `Animator` | PR2 |
| `Runtime/Presentation/NpcPresenterBehaviour.cs` | envoltura: canal + `AudioSource` + `Animator` + bomba | PR2 |
| `Data/Presentation/Emergency.asset` + `Data/Presentation/README.md` | config del escenario de triaje | PR2 |
| `Runtime/Presentation/Piper/PiperInterop.cs` | P/Invoke a libpiper / onnxruntime | PR3 |
| `Runtime/Presentation/Piper/PiperSpeechSynthesizer.cs` | `ISpeechSynthesizer` real | PR3 |
| `Runtime/Presentation/Model/VoiceProvisioner.cs` | extracción de la voz a `persistentDataPath` | PR3 |
| `Runtime/Presentation/Plugins/Windows/x86_64/*`, `Plugins/Android/arm64-v8a/*` | binarios nativos (LFS) | PR3 |
| `Runtime/Presentation/Plugins/Voices/<idDeVoz>.bytes` | voz empaquetada (LFS) | PR3 |
| `.gitattributes` | regla LFS `Runtime/Presentation/Plugins/**` | PR3 |
| `Tests/EditMode/Presentation/NpcPresenterTests.cs` | hereda `NpcPresenterContract` + guardas | PR1 |
| `Tests/EditMode/Presentation/AnimationDriverTests.cs` | mapa cue→param, cue desconocido no lanza | PR1/PR2 |
| `Tests/EditMode/Presentation/PresentationSettingsAssetTests.cs` | `OnValidate` acota; el `.asset` de disco carga | PR2 |
| `Tests/EditMode/Presentation/NpcPresenterBehaviourWiringTests.cs` | suscripción/desuscripción al canal; el `AudioSource` recibe clip no vacío | PR2 |
| `openspec/specs/presentador-npc-m8/spec.md` | primera spec formal | PR4 |
| `Docs/MODULES.md` (M8) | cerrar "solo doble" | PR4 |

## Interfaces / Contracts

Sin cambios en `NpcAi.Core`. Superficie nueva, interna a `NpcAi.Presentation`:

```csharp
// Runtime/Presentation/ISpeechSynthesizer.cs
public interface ISpeechSynthesizer
{
    bool EstaListo { get; }

    /// <summary>
    /// Sintetiza <paramref name="texto"/> a PCM mono en [-1,1] a <paramref name="tasaDeMuestreo"/> Hz.
    /// Devuelve un arreglo vacio (no null) si no puede: el llamador no reproduce nada, no lanza.
    /// </summary>
    float[] Sintetizar(string texto, string vozId, int tasaDeMuestreo);
}

// Runtime/Presentation/IAnimationDriver.cs
public interface IAnimationDriver
{
    /// <summary>Un tag/cue fuera del mapa se ignora sin lanzar. Sin Animator, es no-op.</summary>
    void Aplicar(string emotionTag, string animationCue);
}

// Runtime/Presentation/NpcPresenter.cs
public sealed class NpcPresenter : INpcPresenter
{
    public NpcPresenter(
        ISpeechSynthesizer sintetizador,
        IAnimationDriver animacion,
        IMainThreadPump bomba,
        PresentationSettings config,
        string vozId = null);

    public void Play(NpcReply reply);   // encola; nunca lanza; default/vacio = no-op
}
```

## Testing Strategy

| Archivo | Qué fija |
|---|---|
| `NpcPresenterContract` (sin cambios) | `Play(normal)` no lanza, `Play(default)` no lanza, 10 encadenados no lanzan |
| `NpcPresenterTests : NpcPresenterContract` (nuevo) | `CreateSubject()` arma `NpcPresenter` con los dobles + `ImmediateMainThreadPump`; texto vacío/`default` NO invoca al `SilentSpeechSynthesizer` (contador); `Play` con PCM del doble encola y drena sin lanzar |
| `AnimationDriverTests` (nuevo) | el mapa cue→param traduce lo conocido; un cue desconocido es no-op y no lanza; sin `Animator` no lanza |
| `PresentationSettingsAssetTests` (nuevo) | `OnValidate` acota `TasaDeMuestreo`/`Velocidad`; el `.asset` de `Data/Presentation/` carga de disco (patrón `AssetDatabase.FindAssets` de M5) |
| `NpcPresenterBehaviourWiringTests` (nuevo) | `OnEnable` suscribe y `OnDisable` desuscribe del `NpcReplyChannel` (sin fugas); tras `Raise`, el `AudioSource` recibe un `AudioClip` con `samples > 0` cuando la costura devuelve PCM |
| Prueba manual (PR3) | En el Editor de escritorio, `Play("Buenas, ¿en qué le ayudo?")` produce audio audible en español |

Ejecución EditMode: Test Runner de Unity 6, igual que el resto del repo. NUnit 3.5 (sin `Is.AnyOf`).

## Migration / Rollout

Sin migración de datos ni de contrato. `NpcPresenter` es una implementación nueva del mismo
puerto: se integra reemplazando el punto donde M11 hoy usaría `RecordingNpcPresenter` por
`NpcPresenterBehaviour` en la escena real, sin tocar `INpcPresenter`. Rollback: revertir los
commits deja `RecordingNpcPresenter` como única implementación, igual que hoy.

## Open Questions

- [ ] **Mecanismo exacto de embebido de Piper**: `libpiper` como lib nativa, onnxruntime directo
      + `piper-phonemize`, o el binario `piper` como proceso (solo escritorio, para desbloquear
      PR3 antes que Android). Se decide en el spike de Fase 3, como el de Sentis en M1.
- [ ] **`IMainThreadPump` duplicado vs promovido a `NpcAi.Core`**: hoy M1 tiene su propio
      `IMainThreadPump`. Copiarlo en M8 es lo que la regla 3 permite sin cambio de contrato;
      promoverlo a `NpcAi.Core` sería un cambio de M0 propio. Empezar copiando; consolidar si
      aparece un tercer consumidor.
- [ ] **Una o dos voces** en la primera entrega — depende del tamaño de la voz Piper "medium" y
      del presupuesto de build. Se mira en el spike.
- [ ] **`Data/Npcs/*.json` (con `vozId`)**: ¿lo entrega M11 o es un micro-módulo aparte? Ya está
      en Open Questions del proposal de M11; M8 no lo bloquea (usa voz por defecto si falta).
- [ ] **Lip-sync / visemas**: fuera de alcance; posible mejora si el rig del anfitrión expone
      blendshapes de visema.
