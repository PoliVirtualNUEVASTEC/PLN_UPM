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

> **Nota de alcance temporal**: este documento describe el estado verificado el 2026-09-03.
> Los módulos evolucionan; si pasa tiempo desde esa fecha, re-verificar contra el repo antes de
> confiar en el detalle fino de "Estado actual".

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
- **Estado actual**: **implementado y verificado en hardware físico** (Meta Quest 3, transcripción
  offline en español confirmada, 2026-09-03), pero **no está en `origin/main` todavía**. Vive
  completo en 4 ramas encadenadas sin mergear: `feat/m1-speech-pr1-adapter-core` →
  `feat/m1-speech-pr2-capture-segmentation` → `feat/m1-speech-pr3-config-wiring` →
  `feat/m1-speech-pr4-vosk-engine`. En `origin/main`, `Runtime/Speech/` hoy solo contiene el
  doble `ScriptedSpeechToText.cs`.
- **Specs formales**: `openspec/specs/reconocimiento-voz-m1/spec.md` existe y está completo, pero
  solo en la rama `feat/m1-speech-pr4-vosk-engine` (no llegó a `main` porque el PR no está
  mergeado). El cambio SDD fue archivado en Engram/OpenSpec local de ese trabajo.

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
- **Estado actual**: real e implementado, y mergeado en `origin/main`. **Desviación de diseño sin
  documentar**: la propuesta archivada de M2 (`proposal.md`, sección "Approach") comprometió
  explícitamente un motor de inferencia por modelo entrenado — *"Motor de Inferencia:
  Procesamiento local mediante Sentis / ONNX..."* con MiniLM como candidato — para clasificar
  semánticamente. Lo que existe hoy en `NluIntentClassifier.cs` y `SemanticMatcher.cs` es un
  clasificador **basado en reglas de palabras clave**: un arreglo estático de ~45 tuplas
  `(patrón, Intent, confianza_fija)` en español, resuelto por `string.Contains` sobre el texto
  normalizado — sin modelo, sin embeddings, sin entrenamiento, y sin ninguna conexión al corpus
  de M3. Cumple el contrato `IIntentClassifier` al pie de la letra, pero no es lo que la
  propuesta prometió, y esa desviación no quedó registrada en ningún documento de diseño ni en
  el changelog del contrato.
- **Specs formales**: `openspec/specs/clasificador-intenciones-m2/spec.md` (7 requisitos: nunca
  lanza, determinismo, `Confidence`∈[0,1], resiliencia a entradas atípicas, comportamiento
  cuando `IsReady` es falso, conformidad con `IntentClassifierContract`). La spec formaliza el
  comportamiento observable del puerto, no el enfoque de implementación — por eso la desviación
  de Sentis/ONNX no aparece ahí ni la contradice formalmente, pero sí contradice la propuesta.

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
- **Estado actual**: los datos existen y fueron depurados (`emergencia.json` con 181 entradas,
  `juntas.json` con 180 entradas — duplicados y errores ortográficos corregidos el 2026-09-01),
  pero **están sin commitear**: `git status` los marca como `??` (untracked) en el working tree
  local, y no existen en ninguna rama de `origin`. Solo `Data/Corpus/README.md` está en
  `origin/main`. Es decir: el trabajo de M3 está hecho pero no entregado.
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
- **Estado actual**: **solo doble**. `Runtime/Dialogue/` en `origin/main` únicamente contiene
  `Fakes/ScriptedDialogueGenerator.cs` — un `switch` fijo sobre `Receptivity` con 3 plantillas de
  texto codificadas (`"Claro, digame en que le ayudo"`, `"No tengo nada mas que hablar con
  usted"`, `"Lo escucho"`). No hay implementación real.
- **Specs formales**: no existe `openspec/specs/dialogo-*` ni carpeta de cambio archivada para
  M6.

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
- **Estado actual**: **solo doble**. `Runtime/Presentation/` en `origin/main` únicamente contiene
  `Fakes/RecordingNpcPresenter.cs` — no sintetiza voz ni anima nada, solo registra en una lista
  cada `NpcReply` que le pasaron (`Played`, `HasPlayed`, `Last`) para que una prueba pueda
  verificar sobre eso.
- **Specs formales**: no existe.

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

- **Carpeta**: `Runtime/SessionLog` (planeado — todavía no existe en el repo).
- **Dueño**: asignado (nombre no registrado en este documento).
- **Qué hace (planeado)**: al terminar una sesión de entrenamiento, guarda un registro
  persistente de la conversación completa (lo que dijo el usuario y lo que respondió el NPC) en
  una base de datos local del Quest, para exportar y revisar después. Se suscribe a los canales
  ya existentes — `UtteranceChannel` (M1, voz del usuario) y el canal de respuesta del NPC (M6)
  — sin necesidad de tocar el contrato de `NpcAi.Core`. El archivo de texto que también se pide
  como salida es una exportación generada a partir de lo que ya está en la base, no una escritura
  paralela.
- **Contrato que expone**: ninguno todavía — no está en `Runtime/Core/Ports.cs`. Al ser un
  suscriptor puro de canales existentes, no necesariamente hace falta un puerto nuevo en M0.
- **Estado actual**: **decidido, no iniciado.** Confirmado con el usuario (2026-09-03): módulo
  nuevo M13, no se cuelga de ningún módulo existente. Decisión de diseño ya fijada antes de
  arrancar el SDD: escritura **turno por turno** a SQLite (no acumular en memoria y volcar recién
  al cerrar la sesión), para no perder la conversación si la app crashea a mitad de una sesión de
  entrenamiento. Almacenamiento propuesto: SQLite embebido (p. ej. `sqlite-net-pcl`), con el
  binario nativo vendorizado por plataforma siguiendo el mismo patrón que M1 resolvió para Vosk.
- **Specs formales**: no existe `openspec/specs/bitacora-sesion-m13` — ningún cambio SDD
  arrancó todavía para este módulo.
