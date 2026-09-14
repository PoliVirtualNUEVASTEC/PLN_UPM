# Tasks: M8 — Presentador de NPC (voz sintetizada + animación)

## Review Workload Forecast

| Field | Value |
|---|---|
| Estimated changed lines | PR1 ~230 (núcleo + costuras + dobles + bomba), PR2 ~280 (envoltura + config + `AnimatorDriver` + pruebas de cableado), PR3 ~320 C# + binarios LFS, PR4 ~140 (spec + docs) |
| 400-line budget risk | Medio en PR3 (el P/Invoke + provisioner pueden pasar 400 sin contar binarios) |
| Chained PRs recommended | Sí — PR1 → PR2 → PR3 → PR4, stacked-to-main |
| Alternativa | Un solo PR con `size:exception` como M4/M5/M6, a decidir con el pronóstico real tras PR1 |
| Chain strategy | stacked-to-main |

### Suggested Work Units

| Unit | Meta | PR | Prueba enfocada | Frontera de rollback |
|---|---|---|---|---|
| 1 | Núcleo `NpcPresenter` + costuras `ISpeechSynthesizer`/`IAnimationDriver` + dobles + bomba | PR1 | EditMode: `NpcPresenterTests : NpcPresenterContract`, `AnimationDriverTests` | Borrar `Runtime/Presentation/*.cs` nuevos; `RecordingNpcPresenter` sigue siendo el único presentador |
| 2 | Envoltura `NpcPresenterBehaviour` + config SO + `AnimatorDriver` + `Data/Presentation/` | PR2 | EditMode: `NpcPresenterBehaviourWiringTests`, `PresentationSettingsAssetTests` | Borrar la envoltura y la config; el núcleo queda como pieza suelta usable en pruebas |
| 3 | Motor Piper real detrás de `ISpeechSynthesizer` + aprovisionamiento de voz + binarios | PR3 | EditMode + prueba manual: audio audible en español en el Editor | Borrar `Runtime/Presentation/{Piper,Model,Plugins}/`; vuelve el doble silencioso |
| 4 | Spec formal + documentación | PR4 | Manual: la spec traza a las pruebas EditMode en verde | Revertir el commit de docs/spec; no afecta código |

## Phase 0: Guardrails (leer antes de escribir)

- [x] 0.1 Este cambio toca **un solo módulo**: `Runtime/Presentation/` + su data (`Data/Presentation/`)
  + sus pruebas (`Tests/EditMode/Presentation/`) + `Docs/` + `openspec/` + `.gitattributes` (regla LFS).
  Nada en otro `Runtime/`.
- [x] 0.2 **Cero cambio en `Runtime/Core/` y `Runtime/CoreChannels/`.** `INpcPresenter`, `NpcReply`
  y `NpcReplyChannel` no cambian una firma. Si algo parece requerir tocarlos (p. ej. promover
  `IMainThreadPump` a `NpcAi.Core`), **parar y avisar**: es un cambio de contrato con co-revisión,
  fuera de lo que este cambio autoriza.
- [x] 0.3 **No referenciar `NpcAi.Speech` ni ningún otro módulo** desde `NpcAi.Presentation`. El
  patrón de bomba al hilo principal se **copia** (tipo propio en `Runtime/Presentation/Threading/`),
  no se importa de M1 (regla 3).
- [x] 0.4 `Tests/EditMode/Core/NpcPresenterContract.cs` **no se modifica** en ningún PR:
  `NpcPresenterTests` hereda, no edita, la base.
- [x] 0.5 `Runtime/Presentation/Fakes/RecordingNpcPresenter.cs` **no se toca** (Out of Scope —
  queda como doble de referencia para M11).
- [x] 0.6 M8 **no es** módulo de IA: no aplica la restricción de `/sdd-ff` de M1/M2/M4/M6, pero
  igual se entrega por el flujo completo propuesta → diseño → tareas → revisión humana.
- [x] 0.7 El rig, el `AnimatorController` y la escena son del **proyecto anfitrión**. M8 se prueba
  con un `Animator` de mentira (o sin `Animator`); no se agrega rig ni controller al paquete.

## Phase 1: Núcleo + costuras (PR1)

- [x] 1.1 `Runtime/Presentation/ISpeechSynthesizer.cs` — `EstaListo` +
  `float[] Sintetizar(string texto, string vozId, int tasaDeMuestreo)` (mono, `[-1,1]`, vacío si no puede).
- [x] 1.2 `Runtime/Presentation/IAnimationDriver.cs` — `void Aplicar(string emotionTag, string animationCue)`
  (tag/cue desconocido = no-op sin lanzar).
- [x] 1.3 `Runtime/Presentation/Threading/` — `IMainThreadPump` + `ImmediateMainThreadPump` (default,
  síncrona) + `QueuedMainThreadPump` con `Drenar()` observable. Copia mínima del patrón de M1, tipo
  propio de M8. `Post(Action)` genérico (no tipado a un payload como M1).
- [x] 1.4 `Runtime/Presentation/Fakes/SilentSpeechSynthesizer.cs` (devuelve `float[0]`, cuenta
  invocaciones) y `RecordingAnimationDriver.cs` (registra los `(emotionTag, animationCue)` recibidos).
  Más `Runtime/Presentation/Properties/AssemblyInfo.cs` con `InternalsVisibleTo` (patrón de M1).
- [x] 1.5 `Tests/EditMode/Presentation/NpcPresenterTests.cs : NpcPresenterContract` — `CreateSubject()`
  arma `NpcPresenter` con los dobles + `ImmediateMainThreadPump` + despacho de síntesis en línea.
  Pruebas propias: `Play(default)`/texto vacío/espacios/`null` NO incrementan el contador ni lanzan;
  reply válido sintetiza 1 vez y dispara la animación; 10 encadenados = 10 síntesis; con
  `QueuedMainThreadPump` no hay animación hasta `Drenar`.
- [~] 1.6 `AnimationDriverTests` — el mapa cue→param y el "cue desconocido = no-op" viven en
  `AnimatorDriver`, que es del PR2. En PR1 va `RecordingAnimationDriverTests` (registra en orden,
  tolera `null`). La cobertura del mapa se traslada a PR2 (task 2.5).
- [x] 1.7 GREEN: `Runtime/Presentation/NpcPresenter.cs` (`internal sealed : INpcPresenter`) —
  guardas de `reply.IsEmpty`; `Play` despacha la síntesis a un trabajador (`Task.Run` por defecto,
  inyectable en línea para pruebas) y hace `Post` del resultado en la bomba; la entrega en el hilo
  principal dispara `IAnimationDriver.Aplicar` (sin `AudioSource` todavía). Pasa `NpcPresenterContract`
  + las pruebas propias.
- [x] 1.8 `NpcPresenter` es construible y probable **sin `UnityEngine` de escena** (sin `AudioSource`,
  sin `Animator`): confirmado — 18 pruebas EditMode de `NpcAi.Presentation.Tests` en verde
  (14 nuevas + 4 de `RecordingNpcPresenterTests`), atestiguado por el usuario el 2026-09-10.

> PR1 no necesita audio ni escena: se valida en EditMode con dobles.

## Phase 2: Envoltura + config + canal (PR2)

- [x] 2.1 `Runtime/Presentation/Config/PresentationSettings.cs` (snapshot POCO + `TipoDeParametroDeAnimacion`,
  `EntradaDeVoz`, `EntradaDeCue`, `ParametroDeAnimacion`) + `PresentationSettingsAsset.cs : ScriptableObject`
  con `[CreateAssetMenu]` — `TasaDeMuestreo` (22050), `Velocidad`, `VozPorDefecto`, `Voces[]`, `Cues[]`,
  `VozEmpaquetada`/`IdDeVoz` (PR3). `internal OnValidate()` acota `TasaDeMuestreo` (8000–48000) y
  `Velocidad` (0.5–2). `internal ToSettings()` arma los diccionarios.
- [x] 2.2 `Runtime/Presentation/AnimatorDriver.cs : IAnimationDriver` — `Animator` inyectado + el mapa
  de la config; `SetTrigger`/`SetBool` según el tipo; cue fuera del mapa, `Animator` nulo o sin
  `runtimeAnimatorController` = no-op sin lanzar. Aplica tanto `EmotionTag` como `AnimationCue`.
- [x] 2.3 `Runtime/Presentation/NpcPresenterBehaviour.cs : MonoBehaviour` — `[SerializeField]`
  `NpcReplyChannel`, `PresentationSettingsAsset` (`internal`, para cablear en pruebas), `AudioSource`,
  `Animator`, `_vozId`. `Awake` arma el `NpcPresenter` (síntesis silenciosa en PR2, `Task.Run` como
  despachador, `ReproducirEnAudioSource` como callback de audio). `OnEnable` suscribe `OnReply` al
  canal; `OnDisable` desuscribe. `Update()` drena la bomba. `NpcPresenter.Entregar` gana el callback
  `reproducirAudio` que el behaviour cablea a `AudioClip.Create` + `AudioSource.Play`.
- [~] 2.4 `Data/Presentation/README.md` escrito (esquema, campos, relación con `Data/Npcs/` de M11 y con
  el rig del anfitrión). **`Emergency.asset` lo crea el usuario en Unity** (`Assets > Create > NpcAi >
  Presentation > Presentation Settings`) durante el gate — igual que los `.asset` de M5/M1, porque el
  `guid` del script lo genera el importador.
- [x] 2.5 `Tests/EditMode/Presentation/NpcPresenterBehaviourTests.cs` — `OnEnable` suscribe y
  `OnDisable` desuscribe del `NpcReplyChannel` sin fugas (`RepliesRecibidos` no crece tras
  desactivar); cablear sin canal no lanza. **Gotcha real (2026-09-11):** `GameObject.SetActive(true)`
  sobre un objeto recién creado NO dispara `Awake`/`OnEnable` de forma confiable dentro de un método
  de prueba EditMode síncrono (queda pendiente hasta el próximo tick del Editor) — el primer intento
  con esa técnica dio `Expected: 1, But was: 0`. Fix: `NpcPresenterBehaviour` expone
  `internal CablearParaPrueba()`/`DescablearParaPrueba()` que llaman `Awake`/`OnEnable`/`OnDisable`
  directamente, sin pasar por `SetActive`. La entrega del PCM al `AudioSource` (con `samples > 0`) es
  prueba manual de escena (task 2.7), no EditMode: `AudioClip.Create` no se ejercita porque el
  sintetizador de PR2 es silencioso.
- [x] 2.6 `Tests/EditMode/Presentation/PresentationSettingsAssetTests.cs` — `OnValidate` acota
  `TasaDeMuestreo`/`Velocidad`; `ToSettings` copia escalares y arma los mapas de voces y cues,
  ignorando entradas incompletas; `ResolverIdDeVoz` cae en `VozPorDefecto`. + `AnimatorDriverTests.cs`
  (task 1.6 movida acá): sin `Animator`, mapa nulo, cue fuera del mapa, `Animator` sin controller →
  no-op sin lanzar. Carga del `.asset` de disco: se cubre cuando el usuario cree `Emergency.asset`.
- [x] 2.7 Prueba manual (autor, en Unity): escena de escritorio con `NpcPresenterBehaviour`, un
  `AudioSource`, un `Animator` y el `NpcReplyChannel`; `Raise` a mano y confirmar que no lanza y que
  el flujo llega (audio silencioso en PR2, audible en PR3). **Confirmado por el usuario el
  2026-09-14:** sin errores en consola con un `NpcReplyRaiser` de prueba (script descartable fuera
  del paquete) disparando `Raise` a mano en Play Mode.

> PR2 depende de PR1. Todavía sin Piper: `SilentSpeechSynthesizer` sigue siendo la costura.

## Phase 3: Motor Piper real (PR3)

- [x] 3.1 **Spike técnico de Piper** (equivalente a "1.2 Spike de Sentis" de M1): elegir el
  mecanismo de embebido (`libpiper` / onnxruntime + `piper-phonemize` / proceso `piper` de
  escritorio) y una voz `es_*` candidata; medir tamaño, latencia y naturalidad. Registrar la
  decisión en `design.md` (Open Questions) y en Engram. **Resuelto 2026-09-14:** `libpiper` +
  P/Invoke (GPL-3.0, aceptado explícitamente por el usuario — repo MIT original `rhasspy/piper`
  archivado desde oct. 2025; el activo `OHF-Voice/piper1-gpl` es GPL-3.0 por embeber `espeak-ng`).
  Voz candidata `es_MX-ald-medium` (63 MB), a confirmar de oído antes de empaquetar. Android
  arm64 (Quest) queda **fuera** de este spike — sin binarios oficiales de `libpiper` ni camino sin
  fricción para compilar `espeak-ng` en ARM; se trata como spike aparte de mayor riesgo, sin
  bloquear el camino de escritorio. Ver Decisión 10 en `design.md`.
- [x] 3.2 `.gitattributes`: regla LFS para `Runtime/Presentation/Plugins/**` antes de commitear
  cualquier binario. **Hecho 2026-09-14:** reglas `*.so`/`*.dll`/`*.bytes` bajo
  `Runtime/Presentation/Plugins/**`, mismo patrón que M1 (Vosk). `git-lfs` ya estaba inicializado
  en el repo (confirmado con `git lfs env` + `git lfs ls-files` mostrando los binarios de M1).
- [x] 3.3 `Runtime/Presentation/Plugins/` — binarios nativos por plataforma (`Windows/x86_64`,
  `Android/arm64-v8a`) + `Runtime/Presentation/Plugins/Voices/<idDeVoz>.bytes` (voz `.onnx` + `.json`
  comprimidos) + licencias. **Hecho 2026-09-14 (Windows únicamente, Android queda fuera — ver
  Decisión 10):**
  - `Windows/x86_64/{piper.dll, onnxruntime.dll, onnxruntime_providers_shared.dll}` — bajados
    verificados del artefacto de CI de `NeverMorewd/PiperSharp` (build 2026-09-09).
  - `EspeakNgData.bytes` — los 365 archivos de `espeak-ng-data/` comprimidos en un único `.bytes`
    (zip con rutas `/`, construido a mano con `ZipArchive` para evitar el bug de
    `Compress-Archive`/`ZipFile.CreateFromDirectory` en Windows que escribe `\` literal en las
    entradas — rompería la extracción en Android). Mismo patrón que la voz (Decisión 2):
    se extrae a `persistentDataPath` en el primer arranque, no se importa archivo por archivo.
  - `Voices/es_MX-ald-medium.bytes` y `Voices/es_AR-daniela-high.bytes` — `.onnx` + `.onnx.json`
    de cada voz, bajados de HuggingFace y comprimidos igual. MD5 de `es_MX-ald-medium.onnx`
    verificado contra el manifiesto oficial (coincide).
  - `LICENSE-piper-gpl-3.0.txt`, `LICENSE-onnxruntime-mit.txt`, `LICENSE-espeak-ng-gpl-3.0.txt`
    (texto completo, mismo patrón que `LICENSE-vosk-apache-2.0.txt` de M1) +
    `Voices/NOTICE.txt` (atribución de `daniela` y `ald`).
  - Pendiente (no bloquea PR3 de escritorio): binarios Android arm64-v8a.
- [x] 3.4 `Runtime/Presentation/Model/VoiceProvisioner.cs` — descomprime `<idDeVoz>.bytes` a
  `Application.persistentDataPath/NpcAi/Presentation/<idDeVoz>/` con centinela `.listo`, validación
  de traversal por entrada, `Resources.UnloadAsset` al terminar. (`ZipArchive` sobre `MemoryStream`,
  no `ZipFile.ExtractToDirectory`.) **Hecho 2026-09-14:** espejo directo de
  `NpcAi.Speech.Model.SpeechModelProvisioner` (M1) — mismo mecanismo, sin `UnityEngine`, genérico
  (sirve tanto para una voz como para `EspeakNgData.bytes`, no hace falta una clase por tipo de
  blob). `Resources.UnloadAsset` queda para quien llame (`NpcPresenterBehaviour`, tarea 3.6), igual
  que `SpeechToTextBehaviour` en M1 — el provisioner se mantiene puro y probable en EditMode.
  Prueba `Tests/EditMode/Presentation/VoiceProvisionerTests.cs` escrita (espejo de
  `SpeechModelProvisionerTests`, 3 casos: extrae, idempotente, rechaza traversal) — pendiente
  correr en Test Runner (compuerta humana).
- [x] 3.5 `Runtime/Presentation/Piper/PiperInterop.cs` — P/Invoke al runtime elegido. `Runtime/Presentation/Piper/PiperSpeechSynthesizer.cs : ISpeechSynthesizer` — `EstaListo` tras aprovisionar; `Sintetizar` fonemiza + infiere + devuelve PCM mono `float[]` a `TasaDeMuestreo`. **Escrito 2026-09-14, sin compilar todavía (pendiente abrir Unity):**
  firmas de `PiperInterop.cs` verificadas contra el `piper.h` real de `OHF-Voice/piper1-gpl`
  (fetch directo, no de memoria) y contrastadas contra el wrapper .NET de referencia
  `NeverMorewd/PiperSharp` — mayor confianza que la nota de riesgo original de `VoskInterop`.
  `PiperSpeechSynthesizer` valida rutas ANTES de `piper_create` (si falta algo, `EstaListo`
  queda en `false`, nunca lanza) porque libpiper puede tirar una excepción nativa que cruza
  P/Invoke como `SEHException` y mata el proceso si la ruta es inválida — riesgo real
  documentado en el propio `PiperVoice.ValidatePaths` de PiperSharp. `Velocidad` de la config se
  invierte a `length_scale` de Piper (semántica opuesta). Recibe rutas ya aprovisionadas por
  constructor: no depende de `VoiceProvisioner` (3.4, todavía sin escribir). **Falta:** abrir
  Unity y confirmar que compila sin errores — ningún agente ejecuta Unity de forma autónoma
  (regla del cambio); esto no es una compuerta humana "en verde", es la primera vez que este
  código pasa por un compilador real. **Confirmado por el usuario el 2026-09-14: sin errores en
  consola.**
- [x] 3.6 Cablear `NpcPresenterBehaviour` para usar `PiperSpeechSynthesizer` cuando la config tiene
  `TextAsset` de voz, y `SilentSpeechSynthesizer` cuando no (degradación segura). **Hecho
  2026-09-14, sin compilar todavía (pendiente abrir Unity):** `Awake()` delega en
  `CrearSintetizador(velocidad)` — si `_configuracion.VozEmpaquetada`/`IdDeVoz` faltan, devuelve
  `SilentSpeechSynthesizer` (mismo camino que ya cubrían los tests existentes, sin cambios de
  comportamiento ahí); si están, aprovisiona la voz y (si está asignado) los datos de espeak-ng
  vía `VoiceProvisioner`, hace `Resources.UnloadAsset` de ambos `TextAsset` (igual que
  `SpeechToTextBehaviour` en M1) y arma `PiperSpeechSynthesizer` con las rutas ya extraídas.
  Se agregó `OnDestroy()` que libera el handle nativo (`(_sintetizador as IDisposable)?.Dispose()`)
  — gap real que no existía antes porque `SilentSpeechSynthesizer` no tiene nada que liberar.
  Se sumó el campo `PresentationSettingsAsset.DatosDeEspeak` (`TextAsset`, compartible entre
  escenarios) — no estaba en el diseño original de PR2, hacía falta para que `NpcPresenterBehaviour`
  tenga de dónde tomar `EspeakNgData.bytes`. **Falta:** abrir Unity y confirmar que compila +
  correr `NpcAi.Presentation.Tests` para confirmar que los 32 tests existentes siguen en verde
  (no debería cambiar ninguno, el camino Silent es el que ya cubrían). **Confirmado por el
  usuario el 2026-09-14: compila y los 32 tests siguen en verde.**
- [x] 3.7 Prueba manual: en el Editor de escritorio, `Play(new NpcReply("Buenas, ¿en qué le ayudo?", "neutral", "idle"))` produce audio audible en español; repetir 20 veces sin fugas ni bloqueo del hilo principal. **Confirmado por el usuario el 2026-09-14:** audio real en español con `es_MX-ald-medium` + `EspeakNgData.bytes` vía `VoiceProvisioner`, 20 repeticiones sin trabarse ni errores en consola.
- [x] 3.8 Confirmar que ningún otro punto del código instancia `NpcPresenterBehaviour` de forma
  automática: el cableado en la escena real es de M11. **Verificado 2026-09-14:** grep de
  `NpcPresenterBehaviour` en todo el repo — solo aparece en su propia definición, sus propias
  pruebas, comentarios de `NpcPresenter.cs`/`SilentSpeechSynthesizer.cs`, `Data/Presentation/README.md`
  y los documentos de `openspec/changes/2026-09-10-m8-presentador-npc/`. Ninguna escena (`.unity`)
  ni prefab del paquete lo referencia.

> PR3 depende de PR2. El spike (3.1) puede empezar en paralelo a PR1/PR2.

## Phase 4: Spec formal y documentación (PR4)

- [ ] 4.1 `openspec/changes/2026-09-10-m8-presentador-npc/specs/presentador-npc-m8/spec.md` —
  primera spec formal de M8, trazando los requisitos ya fijados por `NpcPresenterContract.cs` +
  las garantías nuevas (texto vacío no sintetiza, cue desconocido no lanza, suscripción sin fugas,
  síntesis fuera del hilo principal) a las pruebas EditMode. Mismo patrón que `reconocimiento-voz-m1`
  y `generador-dialogo-m6`.
- [ ] 4.2 `Docs/MODULES.md` sección M8: reemplazar "solo doble" por el estado real (presentador
  por capas, TTS Piper on-device, config por escenario, integración pendiente de M11).
- [ ] 4.3 Revisar que el diff acumulado respeta los `Success Criteria` de `proposal.md` (ninguna
  carpeta fuera de `Runtime/Presentation/`, `Data/Presentation/`, `Tests/EditMode/Presentation/`,
  `Docs/`, `openspec/`, `.gitattributes`).

## Phase 5: Cierre (acciones del autor, cada PR)

- [ ] 5.1 `git add` solo de las carpetas del PR; `git diff --cached` antes de cualquier commit.
- [ ] 5.2 Checklist "Antes de mergear" del `README.md` (pruebas propias en verde, diff acotado,
  rama al día con `main`, decisiones a Engram y al documento de contexto).
- [ ] 5.3 Al cerrar el último PR: `verify-report.md` + mover el cambio a `openspec/changes/archive/`
  con su `archive-report.md`; promover la spec a `openspec/specs/presentador-npc-m8/spec.md`.
- [ ] 5.4 PR(s) mergeado(s) a `main` por el autor (regla 8).

## Notas

- Ningún agente ejecuta Unity de forma autónoma: cada verde de Test Runner y cada prueba manual de
  audio es compuerta humana, igual que en M1, M2, M4, M5 y M6.
- El spike de Piper (3.1) es el mayor riesgo abierto: Piper en Android/Quest necesita onnxruntime
  nativo y datos de espeak-ng para fonemización. Si el spike se traba, PR3 puede entregar primero
  el camino de escritorio (proceso `piper`) y dejar Android como PR posterior, sin bloquear PR1/PR2
  ni a M11.
- M11 puede cablear `NpcPresenterBehaviour` real apenas PR2 esté mergeado (audio silencioso), y
  cambiar a audio real cuando PR3 cierre — sin tocar M11.
