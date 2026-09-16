# Especificación de módulos — com.poli.npc-ai

Este documento describe, módulo por módulo, qué hace cada uno, qué contrato expone y en qué
estado real está hoy. No reemplaza a las otras tres fuentes de verdad del proyecto — las
complementa:

- **`README.md`** — tabla de módulos, carpetas, dueños y reglas de convivencia.
- **`Runtime/Core/Ports.cs`** — el contrato en código, fuente única de verdad para las firmas.
- **`openspec/specs/`** — especificaciones formales versionadas (requisitos + escenarios) para
  los módulos que ya pasaron por el ciclo SDD completo.

Cada sección abajo fue verificada leyendo el archivo real correspondiente (código fuente o
`spec.md`) en `origin/main`, no a partir de lo que dice el `README.md` o de memoria de trabajo
previo. Donde local `main` y `origin/main` difieren, se documenta el estado de `origin/main`
por ser la rama compartida real del equipo.

> **Nota de alcance temporal**: este documento describe el estado verificado el 2026-09-09,
> con una pasada de actualización el 2026-09-16 sobre las secciones M1, M2, M3, M6, M9 y M13-M15
> (verificado leyendo `origin/main` directo, no de memoria de trabajo previo). Los módulos
> evolucionan; si pasa tiempo desde esa fecha, re-verificar contra el repo antes de confiar en el
> detalle fino de "Estado actual".

---

## M0 — Núcleo de contratos

- **Carpeta**: `Runtime/Core`, `Runtime/CoreChannels`
- **Dueño**: Compartido (Luis + Jefferson)
- **Qué hace**: define el vocabulario común que usan todos los demás módulos — los 4 enums
  congelados (`Intent`, `Tone`, `PhysicalAction`, `Receptivity`), los 5 DTO (`Utterance`,
  `IntentResult`, `ReceptivityChange`, `NpcReply`, `PersonalityId`) y los 7 puertos
  (interfaces). No contiene lógica de negocio: es el punto de acoplamiento único entre módulos.
  `Runtime/CoreChannels` añade el mecanismo de conexión por Inspector: un
  `EventChannel<T> : ScriptableObject` por cada DTO relevante (`UtteranceChannel`,
  `IntentResultChannel`, `PhysicalActionChannel`, `ReceptivityChangeChannel`,
  `NpcReplyChannel`), con `Subscribe`/`Unsubscribe`/`Raise`.
- **Contrato que expone**: los 7 puertos (`ISpeechToText`, `IPhysicalActionSource`,
  `IIntentClassifier`, `IReceptivityEngine`, `IDialogueGenerator`, `INpcPresenter`,
  `IScenarioObjective`) y los 5 DTO. `NpcAi.Core` tiene `noEngineReferences: true`: no puede
  referenciar `UnityEngine` bajo ninguna circunstancia. Los canales garantizan que `Raise` sin
  suscriptores no lanza y que `OnDisable` limpia todas las suscripciones.
- **Estado actual**: real e implementado. `Contract.Version` congelado en `1`.
- **Specs formales**: `openspec/specs/contrato-nucleo-m0/spec.md` (DTOs, enums, los 7 puertos
  con reglas DEBE/NO DEBE y escenarios Given/When/Then) y
  `openspec/specs/canales-evento-nucleo-m0/spec.md` (semántica de los 5 `EventChannel<T>`).

---

## M1 — Percepción de voz

- **Carpeta**: `Runtime/Speech`
- **Dueño**: Luis Miguel Cañaveral Restrepo
- **Qué hace**: convierte audio del micrófono en texto en español, offline, usando Vosk/Kaldi.
  Segmenta el audio por estrategia configurable según el escenario (pulsar-para-hablar o
  actividad de voz), aprovisiona el modelo de reconocimiento en el dispositivo y publica cada
  frase reconocida como un `Utterance` en el `UtteranceChannel`.
- **Contrato que expone**: `ISpeechToText` — evento `OnUtterance`, propiedad `IsListening`,
  `StartListening()`/`StopListening()`. Regla dura verificada en el diseño: `StopListening()`
  cierra el segmento abierto y emite el `Utterance` pendiente **antes** de bajar `IsListening`,
  para no violar a la vez "la ventana es la frase" y "no emitir después de `StopListening`".
- **Estado actual**: **real e implementado, mergeado en `origin/main`** (corrige el estado
  reportado en la verificación de 2026-09-09, que todavía marcaba las 4 ramas encadenadas como
  sin mergear). Implementado y verificado en hardware físico (Meta Quest, transcripción offline
  en español confirmada), incluyendo `SpeechToTextBehaviour` (envoltura `MonoBehaviour` real: pide
  permiso de micrófono, aprovisiona el modelo Vosk vendorizado a `persistentDataPath`, y publica
  cada `Utterance` en el `UtteranceChannel`) — es el punto de composición que M11 (y cualquier
  escena anfitriona) usa para conectar voz real, no solo el doble.
- **Specs formales**: `openspec/specs/reconocimiento-voz-m1/spec.md`, ya en `origin/main`.

---

## M2 — Comprensión de lenguaje

- **Carpeta**: `Runtime/Nlu`
- **Dueño**: Luis Miguel Cañaveral Restrepo
- **Qué hace**: clasifica una frase en texto libre en una `Intent` y un `Tone`, con una
  confianza y una latencia. Normaliza el texto (`TextPreprocessor`), busca coincidencias de
  frase contra una tabla fija de patrones (`SemanticMatcher`) y analiza el tono por separado
  (`ToneAnalyzer`).
- **Contrato que expone**: `IIntentClassifier` — `IsReady` (nunca lanza), `Classify(string text)`
  (nunca lanza ante `null`/vacío/símbolos/números/cadenas largas; determinista en `Intent` y
  `Tone` para la misma entrada; `Confidence` y `LatencyMs` no están obligados a serlo).
- **Estado actual**: **motor real reemplazado — `BertIntentClassifier` (Unity Sentis)**. El
  cambio `openspec/changes/2026-09-09-m2-clasificador-bert-reducido/` (propuesto 2026-09-09) ya
  se implementó, se verificó en hardware real y se cierra con este mismo update de documentación
  (PR #30, `feat/m2-pr2-bert-sentis` → `main`). El motor anterior de reglas de palabras clave
  (`NluIntentClassifier.cs`/`SemanticMatcher.cs`) **no se borró**: sigue en el repo como respaldo
  determinista documentado, pero ya no es el motor de referencia del módulo.
  - **Encoder**: `distilbert-base-multilingual-cased` (~135M parámetros, 542MB fp32),
    **congelado**, con una cabeza de clasificación (`Intent` + `Tone`) entrenada por transfer
    learning sobre el corpus de M3 (`Training/Nlu/train.py`), exportada a ONNX y ejecutada
    on-device vía Unity Sentis (`com.unity.ai.inference` 2.6.1, ya como dependencia real de
    `package.json`, no solo `keywords`).
  - **Constructor portable a build real**: `BertIntentClassifier(ModelAsset modelo, TextAsset
    tokenizadorJson)` — corregido durante PR2 respecto del diseño original
    (`string modelPath` + `AssetDatabase`/`File.ReadAllText`, que solo funcionaba dentro del
    Editor). Sin esa corrección, ningún wiring de M11 podría haber cargado el modelo en un build
    de jugador real.
  - **Métricas del entrenamiento que produjo el `.onnx` commiteado** (sobre el corpus de M3
    vigente en ese momento, ~30 ejemplos/intención — anterior a la ampliación de M3 del
    2026-09-14, ver abajo): Intent accuracy 0.792, Intent macro-F1 0.679, Tone accuracy 0.729,
    Tone macro-F1 0.720. `Tone.Empatico` explícitamente marcado como no confiable con ese corpus
    en el momento del entrenamiento (ver limitación conocida más abajo).
  - **Confirmado en hardware real**: Unity Editor Test Runner (368 pruebas en verde, luego 386
    tras M14/M15) y **spike físico en Meta Quest** (2026-09-15/16) — carga del modelo ~1.0s,
    latencia de inferencia 30-80ms por frase, pipeline completo micrófono real (M1) →
    `BertIntentClassifier` confirmado end-to-end. Ver `apply-progress.md` de este cambio para el
    detalle y las transcripciones reales.
  - **Limitación conocida — modelo entrenado con un corpus ya superado**: el `.onnx` commiteado
    se entrenó *antes* de que M3 ampliara el corpus de ~30 a 100 ejemplos/intención y resolviera
    el desbalance de `Tone.Empatico` (2026-09-14, ver sección M3). Reentrenar contra el corpus
    nuevo es un cambio pequeño y ya prácticamente listo (el pipeline de `Training/Nlu/` corre tal
    cual sobre el corpus ampliado) — no es una reapertura de este cambio, según sus propias notas
    de cierre.
- **Specs formales**: `openspec/specs/clasificador-intenciones-m2/spec.md` (7 requisitos: nunca
  lanza, determinismo, `Confidence`∈[0,1], resiliencia a entradas atípicas, comportamiento
  cuando `IsReady` es falso, conformidad con `IntentClassifierContract`). La spec formaliza el
  comportamiento observable del puerto, no el enfoque de implementación — sigue vigente sin
  cambios con el nuevo motor, porque el comportamiento observable no cambia, solo el motor que
  lo satisface.

---

## M3 — Corpus y etiquetado

- **Carpeta**: `Data/Corpus`
- **Dueño**: Luis Miguel Cañaveral Restrepo
- **Qué hace**: provee las frases de entrenamiento/etiquetado por escenario, en JSON, con el
  esquema `{text, intent, tone, scenario, labeler}`. Es dato puro, sin código: "arranca el día 1,
  no bloquea a nadie" según su propio README.
- **Contrato que expone**: no es un puerto de código — es un esquema de datos. Cada `intent` y
  `tone` debe coincidir exactamente con un miembro de los enums `Intent`/`Tone` de `NpcAi.Core`.
  Regla de calidad: 10% de las frases doble-etiquetadas para medir acuerdo entre etiquetadores.
- **Estado actual**: **corpus ampliado y rebalanceado, mergeado en `origin/main`** (PR "feat(m3):
  corpus en voz del usuario, 100/intencion, dos escenarios completos", mergeado 2026-09-14) —
  corrige el estado de la verificación anterior de este documento (30 ejemplos/intención, ~180
  frases por archivo). Estado actual verificado directo sobre `origin/main`:
  - `emergencia.json` y `juntas.json`, **600 frases cada uno** (1200 en total), exactamente 100
    ejemplos por cada una de las 6 categorías de `Intent`, en ambos escenarios — cumple la meta
    de "60-100 frases por categoría de intención, por escenario" del propio
    `Data/Corpus/README.md` (en el borde superior).
  - **Desbalance de `Tone.Empatico` resuelto**: pasó de 1 ejemplo (de 180) a 95 ejemplos (de
    600) en ambos archivos. Distribución de `Tone` actual, igual en los dos escenarios: Neutral
    150, Respetuoso 125, Agresivo 125, Ansioso 105, Empatico 95 — ya no hay ninguna categoría con
    un solo dígito de ejemplos.
  - **Sigue sin resolver**: acuerdo entre etiquetadores. El campo `labeler` sigue siendo
    `"Luis"` en el 100% de las 1200 entradas de ambos archivos — la regla de calidad del propio
    README ("10% de las frases doble-etiquetadas") sigue sin aplicarse.
  - **Consecuencia para M2**: el `.onnx` commiteado en `2026-09-09-m2-clasificador-bert-reducido`
    se entrenó *antes* de esta ampliación (con el corpus viejo de ~30/intención) — reentrenar
    contra este corpus nuevo es el siguiente paso natural para mejorar la precisión de `Tone`, no
    una reapertura de ese cambio.
  Ver `Data/Corpus/PENDIENTE-AMPLIACION.md` para el detalle línea por línea de esta ampliación
  (marcada `Completado` para ambos archivos).
- **Specs formales**: no existe `openspec/specs/corpus-*`. La única especificación es
  `Data/Corpus/README.md`, que no pasó por el ciclo SDD (documento de diseño directo, no delta
  formal).

---

## M4 — Motor de receptividad

- **Carpeta**: `Runtime/Receptivity`
- **Dueño**: Jefferson Estiven Aristizábal Quiceno
- **Qué hace**: mantiene una máquina de estados (`Receptivo` / `Neutral` / `NoReceptivo`) por
  NPC, a partir de un puntaje interno saturado que sube o baja según la intención, el tono y la
  acción física recibidas, filtrado por el perfil de personalidad activo. Aplica un "blindaje"
  estructural: una intención agresiva nunca suma puntos y una intención de empatía nunca resta,
  sin importar los números del perfil.
- **Contrato que expone**: `IReceptivityEngine` — `Current` (debe ser `Neutral` hasta el primer
  `Reset`), `Reset(PersonalityId)` (determinista e idempotente), `Evaluate(IntentResult,
  PhysicalAction)` → `ReceptivityChange`.
- **Estado actual**: **real e implementado**, mergeado en `origin/main`
  (`ReceptivityEngine.cs`, `ReceptivityProfile.cs`, `ReceptivityProfileCatalog.cs`, y
  `Unity/ReceptivityProfileAsset.cs` en un sub-ensamblado separado `NpcAi.Receptivity.Unity` que
  es el único punto del módulo que referencia `UnityEngine`). Tiene doble en paridad
  (`Fakes/ScriptedReceptivityEngine.cs`) y una prueba de paridad de 56 casos entre ambos.
- **Specs formales**: `openspec/specs/receptividad-m4/spec.md` — 9 requisitos con tabla de
  trazabilidad completa a pruebas concretas (`ReceptivityEngineContract`,
  `ReceptivityEngineTests`, `ReceptivityProfileTests`, `ReceptivityProfileCatalogTests`,
  `RazonParityTests`).

---

## M5 — Perfiles de personalidad

- **Carpeta**: `Data/Personalities`
- **Dueño**: Jefferson Estiven Aristizábal Quiceno
- **Qué hace**: aporta los datos de tuning fino (umbrales y 3 tablas de delta por intención,
  tono y acción) para 4 personalidades — `grosero`, `histerico`, `introvertido`, `empatico` —
  como instancias de `ReceptivityProfileAsset` (el tipo lo define M4, M5 solo pone datos). Es
  dato puro: agregar o quitar una personalidad es crear o borrar un `.asset`, cero clases.
- **Contrato que expone**: no define un puerto propio — extiende el esquema de M4
  (`ReceptivityProfileAsset` → `ReceptivityProfile` vía `ToProfile()`). El contrato formal exige
  que los 4 `.asset` respeten los signos del motor (agresión/interrupción ≤ 0, respeto/empatía/
  calma ≥ 0), que sus umbrales sean coherentes (`umbralNoReceptivo < umbralReceptivo`,
  `limitePuntaje > 0`) y que los extremos (`grosero` vs. `empatico`) arranquen en estados
  distinguibles.
- **Estado actual**: real e implementado, mergeado en `origin/main` (4 archivos `.asset` en
  `Data/Personalities/`). Al momento de su creación reproducen 1:1
  `ReceptivityProfileCatalog.Standard()` de M4.
- **Specs formales**: `openspec/specs/perfiles-personalidad-m5/spec.md` — 7 requisitos con
  trazabilidad a `Tests/EditMode/Receptivity/PersonalityProfilesDataTests.cs` (8 pruebas) más
  pruebas de M4 sobre el adaptador y el catálogo.

---

## M6 — Generador de diálogo

- **Carpeta**: `Runtime/Dialogue`
- **Dueño**: Jefferson Estiven Aristizábal Quiceno
- **Qué hace** (según el contrato — ver Estado actual): a partir de la personalidad, el estado
  de receptividad y la intención detectada, redacta la respuesta que dirá el NPC.
- **Contrato que expone**: `IDialogueGenerator` — `Generate(PersonalityId, Receptivity,
  IntentResult)` → `NpcReply`. Nunca devuelve `Text` vacío; `EmotionTag`/`AnimationCue` nunca son
  `null`; funciona con `PersonalityId.None` sin lanzar; `Receptivo` y `NoReceptivo` deben
  producir texto distinto. A diferencia de `IIntentClassifier`, **no se exige determinismo**: es
  una asimetría deliberada del contrato de M0.
- **Estado actual**: **real e implementado, mergeado en `origin/main`** (corrige el estado
  reportado en la verificación de 2026-09-09, que todavía lo marcaba sin mergear; cambio
  `openspec/changes/2026-09-09-m6-generador-markov/`). `Runtime/Dialogue/` tiene
  `MarkovDialogueGenerator : IDialogueGenerator` (motor real) y `MarkovChainBuilder` (cadena de
  Markov de palabras, bigramas / orden 2), más el doble `Fakes/ScriptedDialogueGenerator.cs` que
  se mantiene **sin cambios** como implementación determinista de referencia. `Generate` elige el
  bloque por `(PersonalityId, Receptivity)`, hace un paseo aleatorio sobre la cadena de ese
  bloque y, si el paseo degenera, cae en un respaldo escalonado (reintento acotado → frase
  semilla verbatim → frase fija) para no devolver texto vacío nunca. `EmotionTag`/`AnimationCue`
  salen de una tabla fija por receptividad, no de la cadena. `IntentResult` se acepta en la firma
  pero no condiciona el texto en esta primera entrega (ver el cambio → Out of Scope).
- **Corpus semilla**: `Data/Dialogue/<personalidad>.json` — 4 archivos (`grosero`, `histerico`,
  `introvertido`, `empatico`, los mismos 4 ids de M5) más un `README.md`; cada `.json` tiene 3
  listas por estado de `Receptivity`, 6 frases por bloque (12 bloques, 72 frases). Es lo que dice
  el *NPC*, distinto de `Data/Corpus/` de M3 (lo que dice el *usuario*). Ampliar el corpus es una
  edición de datos: no toca ninguna clase. El comentario ya existente en
  `Tests/EditMode/Core/DialogueGeneratorContract.cs` ("el generador real usa Markov") anticipaba
  este enfoque.
- **Pruebas**: 14 pruebas EditMode nuevas en verde — `MarkovChainBuilderTests` (6) y
  `MarkovDialogueGeneratorTests` (8 = 5 del contrato `DialogueGeneratorContract` heredado + 3
  propias) —, más las del doble, que siguen en verde. Ningún punto del código instancia
  `MarkovDialogueGenerator` todavía: el cableado en escena real es de M11 (Harness).
- **Specs formales**: `openspec/specs/generador-dialogo-m6/spec.md`, creada por el cambio
  `2026-09-09-m6-generador-markov` (vive en `changes/.../specs/generador-dialogo-m6/spec.md`
  hasta el archivo).

---

## M7 — Entrada física VR

- **Carpeta**: `Runtime/VrInput`
- **Dueño**: Luis Miguel Cañaveral Restrepo
- **Qué hace** (según el contrato — ver Estado actual): traduce los controles físicos del
  headset VR (gestos, botones) a un `PhysicalAction` del vocabulario de M0.
- **Contrato que expone**: `IPhysicalActionSource` — solo el evento `OnAction`. El doble deja
  explícito que `PhysicalAction.Ninguna` nunca se emite como evento (no representa una acción).
- **Estado actual**: **solo doble**. `Runtime/VrInput/` en `origin/main` únicamente contiene
  `Fakes/ScriptedPhysicalActionSource.cs` — un `Emit(PhysicalAction)` manual pensado para que
  pruebas o el banco de pruebas disparen acciones a mano, sin headset ni controles reales.
- **Specs formales**: no existe.

---

## M8 — Presentador de NPC

- **Carpeta**: `Runtime/Presentation`
- **Dueño**: Jefferson Estiven Aristizábal Quiceno
- **Qué hace** (según el contrato — ver Estado actual): reproduce la respuesta del NPC como voz
  sintetizada y animación.
- **Contrato que expone**: `INpcPresenter` — solo `Play(NpcReply)`.
- **Estado actual**: **real e implementado, mergeado en `origin/main`** (4 PRs encadenados,
  mergeados 2026-09-15: PR #21 `c6cbab0` nucleó, PR #25 `562986b` envoltura, PR #23 `894f3d6`
  motor Piper, PR #24 `b116a7b` spec/docs). Presentador por capas:
  `NpcPresenter : INpcPresenter` (núcleo, sin `UnityEngine` de escena) despacha la síntesis fuera
  del hilo principal y entrega el resultado por una bomba al hilo principal, donde
  `NpcPresenterBehaviour : MonoBehaviour` reproduce el PCM en un `AudioSource` y dispara
  `IAnimationDriver` sobre un `Animator` de escena. La síntesis de voz es TTS **Piper on-device**
  (`PiperSpeechSynthesizer` vía P/Invoke a `libpiper`, motor GPL-3.0 aceptado explícitamente).
  Sin voz configurada, degrada de forma segura a `Fakes/SilentSpeechSynthesizer.cs` (que se
  mantiene como doble determinista de referencia, igual que `Fakes/RecordingNpcPresenter.cs`).
  La configuración (voces, cues de animación, tasa de muestreo, velocidad) es dato por escenario
  (`PresentationSettingsAsset`, `Data/Presentation/`), no código. Dos voces vendorizadas:
  `es_AR-daniela-high` (femenina) y `es_MX-ald-medium` (masculina). 32 pruebas EditMode en verde,
  prueba manual de audio confirmada por el usuario (20 repeticiones sin fugas, 2026-09-14).
  Integración con la escena real (instanciar `NpcPresenterBehaviour`, cablear el rig del anfitrión)
  queda pendiente de M11.
- **Specs formales**: `openspec/specs/presentador-npc-m8/spec.md` (promovida desde
  `openspec/changes/2026-09-10-m8-presentador-npc/specs/` al archivar el cambio).

---

## M9 — Escenario Sala de Emergencia

- **Carpeta**: `Runtime/Scenarios/Emergency`
- **Dueño**: Luis Miguel Cañaveral Restrepo
- **Qué hace** (según el contrato — ver Estado actual): define y mide el progreso del objetivo
  del escenario de triaje/emergencia.
- **Contrato que expone**: `IScenarioObjective` — `Progress01` (siempre en `[0,1]`),
  `IsComplete` (verdadero si y solo si `Progress01 == 1`, con tolerancia `1e-4`; es
  reversible/no-pegajoso), `Notify(ReceptivityChange)` (`Notify` con `default` debe ser no-op).
- **Estado actual**: **solo doble**. `Runtime/Scenarios/Emergency/` en `origin/main` únicamente
  contiene `Fakes/ScriptedScenarioObjective.cs` — avanza o retrocede un paso fijo de 4 según si
  el cambio de receptividad mejoró o empeoró, saturado en `[0, 4]`. No implementa condiciones
  propias del escenario de emergencia (eso queda para la versión real).
  **Nota (2026-09-16, sin cambio de estado)**: ya hay un modelo 3D de la sala de triaje y un
  modelo de NPC disponibles para el proyecto anfitrión — insumo directo para cuando arranque la
  implementación real de este módulo (y de la escena de M11), pero no implican todavía ningún
  cambio de código ni de estado en M9.
- **Specs formales**: no existe.

---

## M10 — Escenario Sala de Juntas

- **Carpeta**: `Runtime/Scenarios/Boardroom`
- **Dueño**: Jefferson Estiven Aristizábal Quiceno
- **Qué hace** (según el contrato — ver Estado actual): define y mide el progreso del objetivo
  del escenario de levantamiento de requerimientos en sala de juntas.
- **Contrato que expone**: `IScenarioObjective` — idéntico al de M9 (mismo puerto, dos
  implementaciones distintas por escenario).
- **Estado actual**: **solo doble**. `Runtime/Scenarios/Boardroom/` en `origin/main` únicamente
  contiene `Fakes/ScriptedScenarioObjective.cs`, con la misma lógica de paso fijo ±1 que la de
  M9 (mismo patrón, distinto escenario). No implementa condiciones propias de sala de juntas.
- **Specs formales**: no existe.

---

## M11 — Banco de pruebas

- **Carpeta**: `Samples~/Harness`
- **Dueño**: Jefferson Estiven Aristizábal Quiceno
- **Qué hace**: escena de escritorio, sin VR, donde se escribe una frase por teclado para
  ejercitar el pipeline completo. Se arma contra los dobles de cada módulo y se van
  reemplazando por implementaciones reales de a uno, sin tocar el resto. En Sprint 13-14 se
  convierte en el instrumento de medición del Objetivo 4 del proyecto.
- **Contrato que expone**: no es un puerto — es una herramienta de integración/demostración que
  consume los 7 puertos de M0.
- **Estado actual**: solo existe `Samples~/Harness/README.md` en `origin/main` (la descripción
  del banco). No hay escena `.unity` ni script de arnés commiteados todavía — el diseño está
  escrito pero la construcción de la escena no se ha entregado.
- **Specs formales**: no existe `openspec/specs/banco-pruebas-*`.

---

## M12 — Documentación

- **Carpeta**: `Docs/`
- **Dueño**: Compartido (Luis + Jefferson)
- **Qué hace**: concentra los manuales, el informe del trabajo de grado y el registro de
  decisiones de contrato que no caben en el código ni en `openspec/specs/`.
- **Contrato que expone**: no aplica — es documentación, no código.
- **Estado actual**: contiene `CONTRACT-CHANGELOG.md` (registro versionado del contrato v1: las
  4 tablas de enums, las reglas DEBE/NO DEBE por puerto, la política de campos de diagnóstico
  `Score`/`ReasonCode`, la asimetría de determinismo, y la lista de pruebas que hacen cumplir el
  contrato). Este mismo archivo (`MODULES.md`) es la segunda pieza de M12.
- **Specs formales**: no aplica en el sentido de OpenSpec (M12 es el destino de la
  documentación, no un módulo con comportamiento verificable por escenarios).

---

## M13 — Bitácora de sesión

- **Carpeta**: `Runtime/SessionLog`.
- **Dueño**: asignado (nombre no registrado en este documento).
- **Qué hace**: al terminar una sesión de entrenamiento, guarda un registro persistente de la
  conversación completa (lo que dijo el usuario y lo que respondió el NPC) en SQLite local del
  dispositivo, turno por turno, para exportar y revisar después. Se suscribe a los canales ya
  existentes — `UtteranceChannel` (M1, voz del usuario) y `NpcReplyChannel` (M6, respuesta del
  NPC) — sin tocar el contrato de `NpcAi.Core`. Cada sesión se identifica por una etiqueta manual
  (`IniciarSesion(string etiqueta)`) y **todas conviven indefinidamente** en la base — no hay
  borrado al iniciar una sesión nueva —, consultables por separado
  (`ListarSesiones()`/`ObtenerTurnosDeSesion(etiqueta)`). Si la app se cierra por un error a
  mitad de una sesión, al reabrir **la retoma sola** (sin perder de vista dónde se quedó ni
  pedirle a nadie que reescriba la etiqueta), siempre que esa sesión no se haya cerrado ya con
  `FinalizarSesion()`.
- **Contrato que expone**: ninguno en `Runtime/Core/Ports.cs` — `ISessionStore` es un seam
  interno de `NpcAi.SessionLog`, no un puerto compartido de M0 (al ser un suscriptor puro de
  canales existentes, no hizo falta un puerto nuevo).
- **Estado actual**: **real e implementado, mergeado en `origin/main`** en 3 PR encadenados
  (`feat/m13-session-log-pr1` PR #8, `pr2` PR #11, `pr3` PR #12; 2026-09-08 a 2026-09-10).
  Núcleo puro (`SessionTurn.cs`, `ISessionStore.cs`, `SessionRecorder.cs`, `SessionExport.cs`) y
  doble en memoria (`Fakes/InMemorySessionStore.cs`). Adaptador real
  `Sqlite/SqliteSessionStore.cs` sobre `sqlite-net-pcl`, escribiendo cada turno confirmado en
  disco de inmediato (sin transacción abierta), para no perder la sesión si la app se cierra a
  mitad de un entrenamiento. Sub-ensamblado `NpcAi.SessionLog.Unity` con
  `SessionLogBehaviour.cs`, que se suscribe por Inspector a los dos canales y expone
  `IniciarSesion()`/`FinalizarSesion()`/`ExportarTextoPlano()` a la escena anfitriona — mismo
  patrón núcleo-puro/adaptador que ya usa M4. **Desviación de diseño corregida en el camino**:
  la propuesta original asumía que `sqlite-net-pcl` se declaraba como dependencia UPM en
  `package.json`; no era cierto (es un paquete NuGet, no UPM), así que se vendorizó a mano —
  DLL administrados y binarios nativos Windows x86_64 / Android arm64-v8a — mismo patrón que M1
  usó con Vosk. De paso se detectó que el binario nativo por defecto de `sqlite-net-pcl` 1.9.172
  trae una vulnerabilidad conocida de severidad alta (CVE-2025-6965); los binarios vendorizados
  se forzaron a una versión parchada, verificada binariamente antes de vendorizar. 22 pruebas
  EditMode nuevas en `NpcAi.SessionLog.Tests`, en verde. **Ampliación 2026-09-14** (cambio SDD
  separado, `historial-multi-sesion-m13`, motivada por una prueba real del usuario): historial
  multi-sesión por etiqueta (antes cada `IniciarSesion` borraba todo) y reanudación automática
  tras un cierre no limpio (`SessionRecorder.ReanudarUltimaSesion()`, gobernada por un flag
  `Cerrada` por sesión para no retomar una que sí se cerró bien). **Pendiente (no bloqueante)**:
  validar el binario nativo en un dispositivo/build IL2CPP real (hoy solo verificado
  binariamente), y cablear `SessionLogBehaviour` en la escena real de M11 cuando esa escena
  exista (sigue sin existir al 2026-09-14).
- **Specs formales**: `openspec/specs/bitacora-sesion-m13/spec.md` — 9 requisitos con
  trazabilidad completa a pruebas concretas (`SessionRecorderTests`, `SessionStoreContract`
  heredada por `InMemorySessionStoreTests`/`SqliteSessionStoreTests`, `SessionExportTests`).

---

## M14 — Catálogo de casos clínicos

- **Carpeta**: `Data/Cases`
- **Dueño**: Nataly Álvarez
- **Qué hace**: provee el caso clínico que un NPC-paciente "adquiere" al iniciar una sesión de
  triaje — síntomas, antecedentes, alergias, signos vitales y motivo de consulta —, en JSON, un
  archivo por caso (`id` == nombre de archivo, mismo patrón que M5). Es dato puro, sin código: el
  tipo `ClinicalCase` que lo deserializa lo define M15. Cada archivo separa dos bloques: `paciente`
  (lo que el NPC sabe y dice, incluida una tabla `hechos` de `{campo, ejemplosDePregunta[],
  respuesta}` que M15 empareja contra lo que pregunta la enfermera) y `clave` (`triajeEsperado`,
  `banderasRojas`, `cierreEsperado`) — dato de evaluación exclusivo de M9, que M15 tiene
  contractualmente prohibido leer.
- **Contrato que expone**: no es un puerto de código — es un esquema de datos, documentado en
  `Data/Cases/README.md`. `clave.triajeEsperado` es una cadena romana (`"I"`–`"V"`), no un enum:
  si M9 crea un enum `Triage`, mapea a esta cadena en su frontera.
- **Estado actual**: 3 casos transcritos desde `Data/Cases/Casos_Medicos.md` (fuente narrativa,
  movida desde la raíz del paquete), con 9 `hechos` cada uno (mínimo fijado: 8):
  `caso-01` (Mariana, 38, cefalea de 2 meses, `triajeEsperado: "II"`), `caso-02` (María Rosa, 53,
  odinofagia de 5 días, `triajeEsperado: "IV"`), `caso-03` (Sofía, 34, TEC leve,
  `triajeEsperado: "II"`). El original en `Casos_Medicos.md` tenía ruido de OCR (números y una
  palabra perdidos en la sección de banderas rojas del caso 3, umbral de fiebre no dado en el
  caso 2); las asunciones tomadas al transcribir quedan documentadas en `Data/Cases/README.md` →
  "Notas de transcripción", pendientes de revisión cruzada por el asesor o un segundo integrante
  antes de darlas por definitivas. Ampliar el catálogo más allá de 3 casos es edición de datos
  posterior, no reapertura de este cambio.
- **Specs formales**: `openspec/specs/catalogo-casos-clinicos-m14/spec.md` se crea al archivar
  este cambio SDD (`openspec/changes/2026-09-09-m14-catalogo-casos-clinicos/`).

---

## M15 — Respondedor clínico

- **Carpeta**: `Runtime/ClinicalResponse/`
- **Dueño**: Nataly Álvarez (reasignado; la propuesta original asignaba a Luis Miguel
  Cañaveral Restrepo).
- **Qué hace**: implementa el puerto `IClinicalResponder` para que el NPC responda como el
  paciente del caso clínico asignado, usando la tabla de `hechos` de M14. Cuando la enfermera
  dice algo que empareja con una entrada de esa tabla, devuelve la `respuesta` en primera
  persona envuelta en un `NpcReply`, con un matiz de personalidad; cuando nada empareja,
  devuelve `ClinicalResponse.NoAplica` (`Handled == false`) y el turno lo toma M6. Es un
  módulo enrutador, no una envoltura de M6: M6 no se tocó.
- **Puerto que consume**: `IClinicalResponder` — congelado en `Runtime/Core/Ports.cs` desde
  el cambio de contrato v2 `2026-09-09-m0-puerto-respuesta-clinica`, con su propia base de
  pruebas de contrato (`ClinicalResponderContract`). M15 no modifica ese contrato: lo
  implementa por primera vez de verdad, igual que `BertIntentClassifier` hace con
  `IIntentClassifier` en M2.
- **Estado actual — real e implementado, mergeado en `origin/main`** en 2 PR (PR1: núcleo +
  doble; PR2: adaptador real, 2026-09-15):
  - `ClinicalCase.cs`: POCO (`Paciente`, `SignosVitales`, `Hecho`) — **sin** `Clave`, así que
    es estructuralmente imposible que `triajeEsperado`/`banderasRojas` lleguen a filtrarse a
    una respuesta.
  - `ClinicalCaseLoader.cs`: JSON → `ClinicalCase` con `JsonUtility`. Motivó un ajuste chico
    de datos en M14 (`temperaturaC` de número a texto, PR aparte): `JsonUtility` no soporta
    `null` en un campo numérico.
  - `ClinicalFactMatcher.cs`: normaliza el texto de la enfermera (minúsculas, sin tildes,
    sin signos) y busca el primer `campo` cuyos `ejemplosDePregunta` quedan totalmente
    cubiertos por las palabras de la pregunta; el empate lo resuelve el orden de la lista
    (menor índice gana, por construcción del recorrido).
  - `ClinicalResponder.cs`: adaptador real. Constructor recibe
    `Func<ClinicalCaseId,string> cargarJson` (de dónde salen los bytes de `Data/Cases/` en
    cada plataforma lo decide quien lo inyecte — M11 — no este tipo). Matiz de personalidad
    por prefijo fijo (`grosero` → "Ya le dije, "; `empatico` → "Claro, doctora. "; `histerico`
    → "¡Ay, doctora! ", decisión tomada en esta entrega, no estaba en la propuesta original;
    `introvertido`/`None`/desconocida → sin matiz); la `respuesta` del caso siempre sobrevive
    intacta como subcadena. `EmotionTag`/`AnimationCue` de tabla fija por `campo`.
  - `Fakes/ScriptedClinicalResponder.cs`: doble determinista con 3 hechos embebidos, sin leer
    `Data/Cases/`; reconoce los mismos 3 ids del catálogo real (`caso-01`/`02`/`03`) como
    "casos existentes" — cualquier otro id (incluido `"no-existe"`) deja `IsReady` en `false`,
    para que la batería heredada de `ClinicalResponderContract` pase igual contra el doble.
  - **Choque de nombres resuelto**: el namespace del módulo (`NpcAi.ClinicalResponse`) choca
    con el nombre del propio DTO (`NpcAi.Core.ClinicalResponse`) — mismo problema que
    `Core.Receptivity` en M4, misma solución: calificar como `Core.ClinicalResponse` dentro
    del módulo.
  - 23 pruebas EditMode nuevas en `NpcAi.ClinicalResponse.Tests`, en verde (419/419 en el
    proyecto completo): `ClinicalFactMatcherTests`, `ClinicalCasesDataTests` (valida los 3
    `Data/Cases/caso-*.json` reales de M14 contra el esquema), `ScriptedClinicalResponderTests`
    y `ClinicalResponderTests` (ambas heredan `ClinicalResponderContract` completo), más
    subcadena intacta, saludo no manejado, sin matiz para `None`/`introvertido`, y
    determinismo en 1000 llamadas para las 4 personalidades.
  - **Pendiente (no bloqueante)**: cablear `ClinicalResponder` en la escena real de M11
    cuando esa escena exista (sigue sin existir al 2026-09-15) — quién decide "clínico vs.
    social" y de dónde salen los bytes de `Data/Cases/` en el Quest es decisión de M11, no
    de M15.
- **Specs formales**: `openspec/specs/respondedor-clinico-m15/spec.md` se crea al archivar
  este cambio SDD.
