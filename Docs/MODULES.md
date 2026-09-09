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
- **Estado actual**: **solo doble en `origin/main` todavía**, pero con plan concreto ya
  especificado. `Runtime/Dialogue/` en `origin/main` únicamente contiene
  `Fakes/ScriptedDialogueGenerator.cs` — un `switch` fijo sobre `Receptivity` con 3 plantillas de
  texto codificadas (`"Claro, digame en que le ayudo"`, `"No tengo nada mas que hablar con
  usted"`, `"Lo escucho"`). No hay implementación real todavía.
  El cambio `openspec/changes/2026-09-09-m6-generador-markov/` (propuesto 2026-09-09) especifica
  el reemplazo — `MarkovDialogueGenerator`, una cadena de Markov de palabras (bigramas) construida
  a partir de un corpus semilla nuevo (`Data/Dialogue/<personalidad>.json`, uno por cada una de
  las 4 personalidades de M5, con frases de ejemplo por estado de `Receptivity`), coherente con
  la propuesta de trabajo de grado ("cadenas de Markov... para darle respuesta"). Este corpus
  semilla es distinto del de M3: M3 etiqueta lo que dice el *usuario*; el de M6 son ejemplos de
  lo que dice el *NPC*, y no existe todavía en ningún lado del repo — el propio cambio de M6 lo
  crea. La primera entrega indexa el corpus por personalidad y receptividad únicamente
  (`IntentResult` se acepta en la firma pero no condiciona el texto todavía, ver el cambio →
  Out of Scope). El comentario ya existente en
  `Tests/EditMode/Core/DialogueGeneratorContract.cs` ("el generador real usa Markov") anticipaba
  exactamente este enfoque.
- **Specs formales**: no existe `openspec/specs/dialogo-*` ni carpeta de cambio archivada
  todavía — el cambio `2026-09-09-m6-generador-markov` crea la primera,
  `openspec/specs/generador-dialogo-m6/spec.md`, cuando se mergee.

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
- **Qué hace (núcleo ya implementado; SQLite todavía planeado)**: al terminar una sesión de
  entrenamiento, guarda un registro persistente de la conversación completa (lo que dijo el
  usuario y lo que respondió el NPC), para exportar y revisar después. Se suscribe a los canales
  ya existentes — `UtteranceChannel` (M1, voz del usuario) y el canal de respuesta del NPC (M6)
  — sin necesidad de tocar el contrato de `NpcAi.Core`.
- **Contrato que expone**: ninguno en `Runtime/Core/Ports.cs` — `ISessionStore` es un seam
  interno de `NpcAi.SessionLog`, no un puerto compartido de M0 (decisión ya tomada: al ser un
  suscriptor puro de canales existentes, no hace falta un puerto nuevo).
- **Estado actual**: **PR1 mergeado en `origin/main`** (PR #8,
  "feat/m13-session-log-pr1", 2026-09-09): núcleo puro (`SessionTurn.cs`, `ISessionStore.cs`,
  `SessionRecorder.cs`) y doble en memoria (`Fakes/InMemorySessionStore.cs`). Escritura
  **turno por turno** ya implementada tal como se decidió (no acumula en memoria para volcar
  recién al cerrar la sesión, evitando perder la conversación si la app crashea a mitad de una
  sesión). **Pendiente (PR2/PR3 del mismo cambio, no iniciados)**: el adaptador real de
  persistencia sobre SQLite embebido (`sqlite-net-pcl`, con el binario nativo resuelto vía UPM en
  vez de vendorizado manual como M1 hizo con Vosk) y el sub-ensamblado Unity que cablea
  `SessionRecorder` a los canales reales en una escena. Hasta que eso se mergee, M13 solo
  funciona con el doble en memoria (no persiste entre sesiones de la app).
- **Specs formales**: no existe todavía `openspec/specs/bitacora-sesion-m13` — la spec se archiva
  al cerrar el último PR del cambio `openspec/changes/2026-09-07-bitacora-sesion-m13/`
  (actualmente con PR1 completado y PR2/PR3 pendientes).
