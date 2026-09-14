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

> **Nota de alcance temporal**: este documento describe el estado verificado el 2026-09-09.
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
- **Estado actual**: real e implementado, y mergeado en `origin/main`, pero **motor actual
  distinto del que promete la propuesta de trabajo de grado**: lo que existe hoy en
  `NluIntentClassifier.cs` y `SemanticMatcher.cs` es un clasificador **basado en reglas de
  palabras clave**: un arreglo estático de ~45 tuplas `(patrón, Intent, confianza_fija)` en
  español, resuelto por `string.Contains` sobre el texto normalizado — sin modelo, sin
  embeddings, sin entrenamiento, y sin ninguna conexión al corpus de M3. Cumple el contrato
  `IIntentClassifier` al pie de la letra, pero no es el motor final.
  **Esta desviación ya está cerrada con un plan concreto**, no solo señalada: el cambio
  `openspec/changes/2026-09-09-m2-clasificador-bert-reducido/` (propuesto 2026-09-09, decisiones
  confirmadas con el usuario) especifica el reemplazo — un encoder BERT reducido (clase
  MiniLM/DistilBERT, ~20-60M de parámetros) **congelado**, con una **cabeza de clasificación
  entrenada por transfer learning** sobre el corpus de M3, exportado a ONNX y ejecutado
  on-device vía Unity Sentis (`com.unity.ai.inference`, hoy solo en `keywords` de
  `package.json`, no como dependencia real — el cambio lo corrige). Motivo de la elección:
  con el volumen de corpus disponible (30 frases/intención/escenario hoy, meta documentada
  60-100), afinar el transformer completo sobreajustaría; congelar el encoder y entrenar solo la
  cabeza es transfer learning estándar para datasets chicos y corre en una GPU de consumo (GTX
  1660, 6 GB — verificado suficiente, sin necesidad de Colab). Hasta que ese cambio se mergee,
  `NluIntentClassifier.cs`/`SemanticMatcher.cs`/`ToneAnalyzer.cs` siguen siendo el motor real; no
  se borran ni con el cambio nuevo, quedan como respaldo determinista documentado.
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
- **Estado actual**: los datos existen, fueron depurados (duplicados y errores ortográficos
  corregidos el 2026-09-01) y **ya están commiteados en `origin/main`**
  (PR "docs/corpus-m3-y-modulos-m13", mergeado 2026-09-04) — `emergencia.json` con 181 entradas,
  `juntas.json` con 180 entradas. Esto corrige el estado reportado en una verificación anterior
  de este documento, que los marcaba como `untracked`.
  **Volumen por debajo de la meta documentada**: ambos archivos tienen exactamente 30 ejemplos
  por cada una de las 6 categorías de `Intent` por escenario (180/181 frases en total), frente a
  la meta de "60-100 frases por categoría de intención, por escenario" que el propio
  `Data/Corpus/README.md` fija — es decir, entre 2× y 3.3× por debajo de la meta en cada
  categoría, no solo en el total.
  **Desbalance de `Tone` no cubierto por la meta de volumen**: `Tone.Empatico` tiene apenas 1
  ejemplo en `emergencia.json` y 0 en `juntas.json` (de 180-181 frases); `Tone.Ansioso` tiene
  solo 16 de 180 en `juntas.json`. Ampliar el volumen total sin corregir explícitamente este
  desbalance no resuelve el problema — un modelo entrenado sobre este corpus no puede aprender a
  reconocer `Empatico` con 1 ejemplo. Ver `Data/Corpus/PENDIENTE-AMPLIACION.md` (nuevo) para el
  detalle línea por línea.
  **Acuerdo entre etiquetadores no medido todavía**: el campo `labeler` es `"Luis"` en el 100% de
  las entradas de ambos archivos — la regla de calidad del propio README ("10% de las frases
  doble-etiquetadas para medir acuerdo entre etiquetadores") no se ha aplicado aún; no hay una
  segunda persona etiquetando ni una sola frase para poder medir el acuerdo.
  Este corpus es la entrada de `Training/Nlu/` en el cambio
  `2026-09-09-m2-clasificador-bert-reducido`: el pipeline de entrenamiento puede correr sobre el
  corpus actual como prueba de humo del pipeline, pero la calidad del modelo resultante (sobre
  todo en `Empatico`) depende de que esta ampliación avance.
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
- **Estado actual**: **implementado en local, sin mergear todavía** (cambio
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

- **Carpeta**: `Data/Cases/` (planeado; no existe todavía en el repo).
- **Dueño**: no asignado en la propuesta (M15, su consumidor principal, es de Nataly).
- **Qué hace (según la propuesta — ver Estado actual)**: define el caso clínico que cada NPC
  "adquiere" en la simulación de triaje — síntomas, antecedentes, alergias, signos vitales y
  motivo de consulta — como dato puro en `Data/Cases/`, un archivo JSON por caso. Sigue el
  patrón de M3 (`Data/Corpus`) y M5 (`Data/Personalities`): "lo variable es dato, no código".
  Cada archivo tendría dos bloques: `paciente` (lo que el NPC sabe y dice) y `hechos` (tabla
  de recuperación pregunta→respuesta que consumiría M15), más un bloque `clave`
  (`triajeEsperado`, `banderasRojas`, `cierreEsperado`) reservado para M9 y explícitamente
  prohibido para M15. El nombre de archivo sería el `ClinicalCaseId`. Alimentaría a M15
  (respuestas del paciente) y a M9 (progreso y veredicto del objetivo de triaje).
- **Contrato/esquema que definiría**: no es un puerto de código — sería, como M3 y M5, un
  esquema de datos versionado en `Data/Cases/README.md`. El tipo C# que deserializa ese
  esquema (`ClinicalCase`) no lo define M14: lo definiría M15
  (`2026-09-09-m15-respondedor-clinico`), igual que las pruebas de datos de M5 viven en la
  carpeta de M4. El `readonly struct ClinicalCaseId` que M14 seguiría como convención de
  nombres de archivo ya existe, pero pertenece a M0: lo agregó el cambio de contrato
  `2026-09-09-m0-puerto-respuesta-clinica` (v2), no M14.
- **Diseño / enfoque (de `design.md`, sin implementar todavía)**: un archivo JSON por caso,
  con `id` == nombre de archivo (misma regla que M5, `grosero.asset` ↔
  `personalityId: grosero`), para que M15 resuelva un `ClinicalCaseId` a un archivo sin
  índice aparte. Los signos vitales serían un objeto de campos nombrados (`fcLpm`, `taMmHg`,
  `frRpm`, `satO2Pct`, `glasgow`, `temperaturaC`), no texto libre, para que M9 pueda razonar
  sobre ellos (p. ej. detectar una crisis hipertensiva) y M15 los pueda leer al responder. La
  propuesta transcribe los 3 casos hoy narrados en `Casos_Medicos.md` (raíz del paquete) al
  nuevo esquema y mueve ese archivo a `Data/Cases/Casos_Medicos.md` como fuente de
  referencia. Fuera de alcance explícito: el cargador JSON→objeto, el enum `Triage` como
  tipo de código, y ampliar el catálogo más allá de esos 3 casos.
- **Estado actual — diseño completo, cero implementación**: `openspec/changes/2026-09-09-m14-catalogo-casos-clinicos/`
  contiene únicamente `proposal.md`, `design.md` y `tasks.md`. **No existe `spec.md`, no
  existe `apply-progress.md`, y no hay ningún archivo bajo `Data/Cases/`** — ni el
  `README.md` del catálogo, ni `caso-01.json`/`caso-02.json`/`caso-03.json`, ni el
  `Casos_Medicos.md` movido. `Casos_Medicos.md` sigue en la raíz del paquete, sin mover. La
  única pieza que ya existe en el repo relacionada con este trabajo es el contrato de M0
  (`ClinicalCaseId`, `IClinicalResponder`, `ClinicalResponse` en `Runtime/Core/`), que es un
  cambio de contrato **separado y ya cerrado**, no una entrega de M14: M14 en sí no ha
  producido ni dato ni código.
- **Specs formales**: no existe `openspec/specs/catalogo-casos-clinicos-m14/spec.md`. Según
  la propuesta, se crearía recién al archivar el cambio.

---

## M15 — Respondedor clínico

- **Carpeta**: `Runtime/ClinicalResponse/` (planeado; no existe todavía en el repo).
- **Dueño**: Nataly (reasignado; la propuesta original asignaba a Luis Miguel Cañaveral
  Restrepo).
- **Qué hace (según la propuesta — ver Estado actual)**: implementaría el puerto
  `IClinicalResponder` para que el NPC responda como el paciente del caso clínico asignado,
  usando la tabla de `hechos` de M14. Cuando la enfermera dice algo que empareja con una
  entrada de esa tabla, devolvería la `respuesta` en primera persona envuelta en un
  `NpcReply`, con un matiz de personalidad; cuando nada empareja, devolvería
  `ClinicalResponse.NoAplica` (`Handled == false`) y el turno lo tomaría M6. Es un módulo
  enrutador, no una envoltura de M6: M6 no se toca.
- **Puerto que implementaría**: `IClinicalResponder` — ya congelado en `Runtime/Core/Ports.cs`
  (junto con el DTO `ClinicalResponse` y `ClinicalCaseId`) desde el cambio de contrato v2
  `2026-09-09-m0-puerto-respuesta-clinica`, con su propia base de pruebas de contrato
  (`ClinicalResponderContract`). M15 **no modifica ese contrato**: lo consumiría e
  implementaría por primera vez de verdad, igual que `BertIntentClassifier` hace con
  `IIntentClassifier` en M2.
- **Diseño / enfoque (de `design.md`, sin implementar todavía)**: un ensamblado nuevo
  `NpcAi.ClinicalResponse` que referenciaría solo `NpcAi.Core`. Recuperación por tabla de
  hechos, no por modelo: `ClinicalFactMatcher` normalizaría el texto de la enfermera
  (minúsculas, sin tildes/signos) y buscaría el `campo` cuyos `ejemplosDePregunta` mejor
  cubran la pregunta; en empate, gana el de menor índice en la lista (determinismo). El dato
  debería sobrevivir textual: `Reply.Text` contendría siempre la `respuesta` del caso como
  subcadena intacta, con a lo sumo un prefijo/sufijo fijo por personalidad (p. ej. `grosero`
  antepone "Ya le dije, "). `ClinicalCase` (el POCO que carga el JSON de M14) no incluiría el
  bloque `clave` en absoluto — lo hace estructuralmente imposible de filtrar, en vez de
  confiar en que el código simplemente no lo lea. La obtención de los bytes de
  `Data/Cases/` quedaría inyectada por constructor (`Func<ClinicalCaseId,string>`), decidida
  por M11, para mantener el núcleo probable sin Unity. Fuera de alcance explícito: cambiar el
  puerto, redefinir el esquema de M14, el enrutado clínico/social (decisión de M11), y usar
  `Receptivity` en la respuesta.
- **Estado actual — diseño completo, cero implementación**: `openspec/changes/2026-09-09-m15-respondedor-clinico/`
  contiene únicamente `proposal.md`, `design.md` y `tasks.md`. **No existe `spec.md`, no
  existe `apply-progress.md`, y no hay ningún archivo bajo `Runtime/ClinicalResponse/`
  ni bajo `Tests/EditMode/ClinicalResponse/`** — ni el `.asmdef`, ni `ClinicalCase.cs`,
  `ClinicalCaseLoader.cs`, `ClinicalFactMatcher.cs`, `ClinicalResponder.cs`, ni el doble
  `Fakes/ScriptedClinicalResponder.cs`. `IClinicalResponder` sigue sin ninguna implementación
  real en el repo — únicamente su base de pruebas de contrato existe (heredada de M0), sin
  una clase concreta que la extienda todavía. M15 depende explícitamente de que M14 se
  mergee primero (necesita el esquema y los 3 `Data/Cases/caso-*.json` reales para
  `ClinicalCasesDataTests`), y M14 tampoco existe aún — ver M14 arriba.
- **Specs formales**: no existe `openspec/specs/respondedor-clinico-m15/spec.md`. Según la
  propuesta, se archivaría al cerrar el cambio.
