# Propuesta: M11 — Banco de pruebas (raíz de composición del escenario de emergencia)

## Intent

M11 hoy es **solo `Samples~/Harness/README.md`**. `package.json` ya registra el sample
(`samples[0] = { displayName: "Banco de pruebas sin VR (M11)", path: "Samples~/Harness" }`), pero
detrás de esa entrada no hay ni un `.cs` ni una escena.

La desviación concreta que este cambio cierra es que **el sistema está completo por módulo y
totalmente inerte como sistema**. Verificado contra el código real de hoy:

- `UtteranceChannel` tiene un emisor (`SpeechToTextBehaviour.cs:138`) y **cero suscriptores**
  que hagan algo con el texto.
- `PhysicalActionChannel` tiene un emisor (`VrInputBehaviour.cs:58`) y **cero suscriptores**.
- `IIntentClassifier.Classify` (M2, `BertIntentClassifier`) **no lo llama nadie** en runtime.
- El enrutador que el propio contrato describe — `IClinicalResponder`, `Ports.cs:51-52`: *"si el
  turno no es clinico devuelve `ClinicalResponse.NoAplica` y el llamador enruta a
  `IDialogueGenerator` (M6)"* — **no tiene llamador**. Ese "llamador" es este cambio.
- La superficie aditiva de M9 (`AssignCase`, `DeclareTriage`) y la de M15 (`AssignCase`) las
  ejercitan hoy **solo las pruebas**. M9 cerró su propuesta nombrando esto como trabajo de M11.

Este cambio entrega esa raíz de composición: un `SessionDirector` (C# puro, en `Runtime/Harness/`,
probable en EditMode) más el arnés de escena (`Samples~/Harness/`) que lo ejecuta.

### Supersesión explícita del cambio de 2026-09-09

`openspec/changes/2026-09-09-m11-armado-sesion/` (proposal, design, tasks; sin `specs/`) **existe,
nunca se implementó y nunca se archivó**: no hay `Runtime/Harness/`, ni `Samples~/Harness/*.cs`,
ni `Tests/EditMode/Harness/`, ni `Data/Npcs/` en el repositorio. Su pseudocódigo quedó **stale de
forma concreta y verificable** contra el trabajo real posterior de M2, M7, M8, M13 y M9:

| Lo que asumía el cambio stale | La API real de hoy |
|---|---|
| `CerrarConTriaje(Triage categoria)` | El enum `Triage` **nunca se agregó a M0**. M9 expone `DeclareTriage(string category)`, normalizado contra `clave.triajeEsperado` |
| `m9.RegistrarHechoObtenido(campoDe(clin))` | M9 expone `RegisterRedFlag(int index)`. `Core.ClinicalResponse` **no dice cuál `Hecho` hizo match**, así que `campoDe` no tiene implementación limpia |
| `m8.Play(reply)` directo | `NpcPresenterBehaviour` **se auto-suscribe** a `NpcReplyChannel` en `OnEnable`, con comentario propio: *"M11 (que publica en el canal) se cablean en la escena anfitriona"* |
| `ISessionRecorder m13` inyectado | `SessionLogBehaviour` **se auto-suscribe** a `UtteranceChannel` + `NpcReplyChannel` por `[SerializeField]`; no expone costura inyectable |

**Esta propuesta lo reemplaza, no lo reanuda.** Recomendación explícita: el folder de 2026-09-09
se **borra**, no se archiva — archivarlo afirmaría que entregó algo, y no entregó una línea. Se
conservan de él las partes que siguen siendo válidas y se re-justifican abajo: el reparto
`Runtime/` + `Samples~/` (su AD1), la inyección por constructor (AD2), la rama de enrutado (AD3),
la reproducibilidad por semilla (AD4) y el corte en dos PR apilados.

## Scope

### In Scope

- **`Runtime/Harness/SessionDirector.cs` + `NpcAi.Harness.asmdef`** (`references: ["NpcAi.Core"]`,
  sin `NpcAi.Core.Channels`). Clase C# **no-`MonoBehaviour`**, puertos por constructor:
  `IIntentClassifier`, `IClinicalResponder`, `IDialogueGenerator`, `IReceptivityEngine`,
  `IScenarioObjective`. Expone `IniciarSesion(ClinicalCaseId, PersonalityId)`,
  `ProcesarTurno(Utterance) → NpcReply`, `ProcesarAccion(PhysicalAction) → NpcReply?`,
  `DeclararTriaje(string)` y lecturas de estado (`CasoActual`, `PersonalidadActual`, `Progreso`).
- **Selección reproducible con semilla** (decisión de 2026-09-09 que se mantiene): recibe
  `IReadOnlyList<ClinicalCaseId>` (los tres `Data/Cases/caso-0N.json`) y
  `IReadOnlyList<PersonalityId>` más una semilla; elige el par `(caso, personalidad)` con un
  `System.Random` propio y **no repite el último `ClinicalCaseId`**. Nada de `UnityEngine.Random`.
- **`Samples~/Harness/HarnessBehaviour.cs`** — cáscara delgada: campos `[SerializeField]` para los
  canales y los assets de config, construye el `SessionDirector`, se **suscribe** a
  `UtteranceChannel` y `PhysicalActionChannel` en `OnEnable`, se **desuscribe** en `OnDisable`,
  reenvía al director y **`Raise`** el `NpcReply` resultante en `NpcReplyChannel`. Costura
  `CablearParaPrueba`/`DescablearParaPrueba` igual que `VrInputBehaviour` y `NpcPresenterBehaviour`.
- **`Samples~/Harness/README.md`** (modificado) — instrucciones **paso a paso para el humano** que
  construye la escena: qué componente va en qué objeto y, sobre todo, que los ~6 slots de canal
  apunten al **mismo asset compartido** (el error de cableado más probable y el único que ninguna
  prueba EditMode puede detectar).
- **`Tests/EditMode/Harness/`** — `SessionDirectorTests` + `HarnessBehaviourWiringTests`, contra
  los dobles ya publicados (nombres verificados): `ScriptedIntentClassifier` (M2),
  `ScriptedClinicalResponder` (M15), `ScriptedDialogueGenerator` (M6),
  `ScriptedReceptivityEngine` (M4), `ScriptedScenarioObjective` (M9), `RecordingNpcPresenter` (M8).
- **`openspec/specs/banco-de-pruebas-m11/spec.md`** — primera spec formal de M11.
- **`Docs/MODULES.md`** (sección M11), cerrando "solo README".

### Out of Scope

- **Ningún cambio de contrato.** M11 es **puro consumidor** del M0 congelado: no agrega ni modifica
  un puerto, un DTO, un enum ni un canal. No aplica la gobernanza de la regla dura 2.
- **El archivo `.unity` en sí.** Ninguna escena existe hoy en el repositorio (verificado por glob
  completo) y la guía de `apply` prohíbe fusionar YAML de Unity a mano: *"on conflict discard and
  redo"*. Construir la sala de triaje, meter el modelo del NPC y cablear los Inspectors es una
  **tarea humana documentada**, no un artefacto diffeable que entregue este cambio.
- **M10 (Sala de Juntas) y M16 (`IRequirementResponder`)** — decisión 1 del usuario. No se cablean
  **ni contra dobles**. M16 tiene cero código en `Runtime/`: solo aterrizó la forma de su contrato
  en M0 v3. Improvisar un doble para un módulo que nunca se propuso no sirve al Objetivo 4.
- **La brecha M9↔M15 de índices de bandera roja** — decisión 2 del usuario. M11 **no llama
  `RegisterRedFlag(int)` en ninguna parte de esta entrega**; el componente clínico de `Progress01`
  se queda en 0 y la renormalización sin clave que M9 ya implementó lo absorbe sin romperse. El
  arreglo correcto a largo plazo es un **cambio SDD aparte sobre M15**: un método **aditivo** sobre
  la clase concreta `ClinicalResponder` que exponga cuál `Hecho`/`Campo` hizo match (mismo
  precedente que M2 con `EmitirParaPrueba`, M7 con `CablearParaPrueba` y M9 con
  `RegisterRedFlag`) — **no** un cambio de contrato de M0, y **no** algo que M11 deba intentar
  solo, porque la única alternativa sería duplicar la lógica de matching de M15: frágil, y
  rechazada explícitamente.
- **Las 3 `PhysicalAction` diferidas de M7** (`EntregarObjeto`, `SenalarPantalla`, `GestoCalma`):
  ninguna fuente las produce hoy. M11 no las trata como caso especial; esas ramas simplemente
  quedan **inalcanzables** hasta que aterrice el cambio de seguimiento de M7. Nada se rompe y nada
  de lo que entrega esta propuesta se reescribe entonces.
- **`IntentResultChannel` y `ReceptivityChangeChannel`: se dejan sin cablear, a propósito.** Ambos
  están **completamente huérfanos hoy** — cero emisores, cero suscriptores en todo el repositorio
  (grep exhaustivo). El `SessionDirector` ya tiene el `IntentResult` y el `ReceptivityChange` en
  mano dentro del mismo turno; publicarlos en un canal solo para volver a leerlos agregaría dos
  assets compartidos más al cableado de escena (ver la fila de riesgo correspondiente) sin ningún
  consumidor. Quedan disponibles para un HUD futuro. **Esto es una elección, no un olvido.**
- **`Data/Npcs/` e identidad del cuerpo del NPC.** El cambio stale la proponía (su AD6), pero M8
  ya lleva `_vozId` como `[SerializeField]` propio de `NpcPresenterBehaviour`: el cuerpo y la voz
  son cableado de escena, no dato que el director deba elegir. Crear una carpeta `Data/` nueva sin
  consumidor en runtime sería inventar precedente. Diferido.
- **Métricas del Objetivo 4.** M11 se convierte en el instrumento de medición más adelante; este
  cambio solo arma la sesión.

## Capabilities

### New Capabilities

- `banco-de-pruebas-m11` (nuevo `openspec/specs/banco-de-pruebas-m11/spec.md`): primera spec formal
  de M11. Formaliza el orden del turno, la regla del centinela en `Evaluate`, el enrutado
  clínico/social, la secuencia de inicio de sesión, la simetría Subscribe/Unsubscribe de la cáscara
  y la selección reproducible por semilla.

### Modified Capabilities

- Ninguna. `contrato-nucleo-m0` y `canales-evento-nucleo-m0` no cambian: M11 solo consume.

## Approach

### La cadena de composición (verificada puerto por puerto contra el código real)

| Paso | Llamada | Vía |
|---|---|---|
| Entrada de voz | `UtteranceChannel.Subscribe(...)` | **Canal** (M1 lo emite; nadie lo consumía) |
| Entrada física | `PhysicalActionChannel.Subscribe(...)` | **Canal** (M7 lo emite; nadie lo consumía) |
| Comprensión | `m2.Classify(utterance.Text)` | **Llamada directa** (M2 no usa canales) |
| Decisión | `m4.Evaluate(intent, action)` | **Llamada directa** (M4 es C# puro, sin `MonoBehaviour`) |
| Objetivo | `m9.Notify(change)` | **Llamada directa, en proceso** |
| Respuesta clínica | `m15.Respond(utterance, intent)` | **Llamada directa** |
| Respuesta social | `m6.Generate(personality, m4.Current, intent)` | **Llamada directa, solo si `!Handled`** |
| Salida | `canalDeRespuesta.Raise(reply)` | **Canal**: alimenta a M8 **y** a M13 con un solo `Raise` |

**El enrutado clínico/social es el corazón de este cambio** y no existe en ninguna otra parte del
código fuera de las pruebas: `var reply = clin.Handled ? clin.Reply : m6.Generate(...)`. Una rama
de una línea, exactamente como el comentario de `IClinicalResponder` la describe.

**Nunca `INpcPresenter.Play` directo.** El único `Raise` en `NpcReplyChannel` llega a
`NpcPresenterBehaviour` (presentación) y a `SessionLogBehaviour` (bitácora) porque **ambos se
auto-suscriben**. Por eso M11 **no llama nada de M13 por turno**: solo `IniciarSesion(etiqueta)` y
`FinalizarSesion()` en los bordes de la sesión.

### La brecha no definida: `Evaluate` recibe dos señales asíncronas

`Evaluate(IntentResult, PhysicalAction)` exige **ambos** argumentos, pero el `Utterance` y la
`PhysicalAction` llegan por canales **independientes y asíncronos**. Decisión de este cambio:
cada rama usa el **centinela `0`** de la otra dimensión.

- Llega solo `Utterance` → `Evaluate(intent, PhysicalAction.Ninguna)`.
- Llega solo `PhysicalAction` → `Evaluate(IntentResult.Unknown, action)`.

No es una invención: `Enums.cs:9` documenta `Desconocida = 0` con la razón exacta — *"un valor sin
inicializar nunca significa algo fuerte"* — y `PhysicalAction.Ninguna = 0` sigue la misma regla.
Todo el contrato v1 ya trata esos ceros como "sin señal" seguro. Consecuencia que la spec debe
fijar: una acción física sola **sí** mueve la receptividad (el perfil de M4 tiene
`DeltaPorAccion`), pero **no** produce respuesta hablada — `ProcesarAccion` devuelve `null` y no
publica en `NpcReplyChannel`. Hablar es una respuesta a lo que se dijo, no a un gesto.

### El reparto `Runtime/Harness/` + `Samples~/Harness/`

`Samples~/` (con tilde) es convención UPM real: Unity **nunca lo compila** como parte del paquete;
solo entra a un proyecto consumidor cuando alguien importa el sample desde Package Manager, y ahí
se comporta como código de la aplicación anfitriona. Consecuencia dura: **lógica en `Samples~/` es
lógica sin pruebas EditMode**. De ahí el reparto, que re-confirma el AD1 del cambio stale:

- `Runtime/Harness/` → `SessionDirector`, compilado en el paquete, probable en EditMode, con
  `.asmdef` que referencia **solo `NpcAi.Core`** (recibe todo por interfaz; ni siquiera necesita
  `NpcAi.Core.Channels`, porque los canales los toca la cáscara).
- `Samples~/Harness/` → `HarnessBehaviour` (namespace **`NpcAi.Harness.Sample`**) y la escena. El
  sufijo `.Sample` es deliberado: al importarse, este archivo compila dentro del ensamblado del
  consumidor, y un namespace distinto evita colisionar con la propia raíz de composición del
  anfitrión y deja claro en cualquier stack trace si el frame vino del paquete o del sample.

La regla dura 3 ("ninguna otra referencia entre módulos") gobierna las referencias **entre
ensamblados pares dentro de `Runtime/`**. No restringe a una raíz de composición en `Samples~/`,
porque ese contenido es estructuralmente el mismo rol que jugará el proyecto VR anfitrión: un
**consumidor**, no un par. La regla dura 1 se satisface tratando `Runtime/Harness/` +
`Samples~/Harness/` como la **única** frontera del módulo M11.

**`Debug.Log`, con una distinción explícita**: cero en `Runtime/Harness/`, sin excepción. En
`Samples~/Harness/HarnessBehaviour.cs` **sí se permite**, acotado a la traza de turno y de sesión.
Se declara la desviación en voz alta porque contradice la guía general del repositorio, y se
justifica: un puerto que imprime contamina una librería embarcada, pero un **sample** cuya única
retroalimentación fuera un breakpoint no enseña nada. El sample educa por ser legible y
observable; nunca se compila dentro del paquete, así que no puede contaminar a ningún consumidor
que no lo importe a propósito.

### Inicio de sesión: una sola operación coherente

`IniciarSesion(caso, personalidad)` dispara los cuatro juntos, en este orden, y es el único lugar
del sistema donde ese orden queda fijado:

```
m4.Reset(personalidad)                        // M4 — estado inicial del perfil
m9.AssignCase(caso)                           // M9 — carga clave; sin esto HasKey queda falso
m15.AssignCase(caso, personalidad)            // M15 — sin esto IsReady queda falso
m13.IniciarSesion(etiqueta)                   // M13 — vía SessionLogBehaviour, desde la cáscara
```

Los tres primeros los hace el `SessionDirector` (son puertos que ya tiene). El cuarto lo hace la
cáscara, porque `SessionLogBehaviour` es un `MonoBehaviour` que M11 recibe por Inspector y del que
M13 no expone costura inyectable. `AssignCase` del mismo caso en M9 y M15 con el **mismo
`ClinicalCaseId`** es la invariante que la spec debe exigir: divergir ahí significa que el paciente
responde un caso y el evaluador puntúa otro.

### Entrega en dos PR apilados (recomendación a nivel de propuesta)

El pronóstico exacto de líneas es trabajo de `sdd-tasks`, no de esta fase, pero el corte se
recomienda aquí porque **PR2 no se puede estimar con la misma vara que PR1**:

- **PR1 — `SessionDirector` + pruebas EditMode.** 100% construible por agente, 100% verificable por
  el Test Runner. Compuerta: solo EditMode.
- **PR2 — cáscara `HarnessBehaviour` + escena + README + `Docs/MODULES.md`.** Su costo dominante no
  son líneas de diff: es **trabajo manual en el GUI del Editor** de duración no acotada, más una
  ejecución física en Quest 3.

Meterlos en un solo PR pondría una revisión de código normal detrás de una tarea humana de GUI sin
fecha, y dejaría el budget de 400 líneas mezclado con un artefacto que no se revisa leyéndolo.

## Affected Areas

| Área | Impacto | Descripción |
|---|---|---|
| `Runtime/Harness/NpcAi.Harness.asmdef` | Nuevo | `references: ["NpcAi.Core"]`, nada más |
| `Runtime/Harness/SessionDirector.cs` | Nuevo | Selección con semilla, inicio de sesión, turno, enrutado, cierre |
| `Samples~/Harness/HarnessBehaviour.cs` | Nuevo | Cáscara: Inspector → constructor, Subscribe/Unsubscribe, `Raise` |
| `Samples~/Harness/README.md` | Modificado | Pasos humanos de construcción de escena y de cableado de canales compartidos |
| `Tests/EditMode/Harness/` | Nuevo | `SessionDirectorTests`, `HarnessBehaviourWiringTests`, `.asmdef` |
| `openspec/specs/banco-de-pruebas-m11/spec.md` | Nuevo | Primera spec formal de M11 |
| `Docs/MODULES.md` (sección M11) | Modificado | Cerrar "solo README"; nombrar lo diferido |
| `openspec/changes/2026-09-09-m11-armado-sesion/` | A borrar | Decisión del usuario/orquestador, **no de esta fase** |
| `Runtime/Core/`, `Runtime/CoreChannels/` | Sin cambio | Contrato congelado; M11 solo consume |
| Todo otro `Runtime/<Modulo>/` | Sin cambio | Solo se consumen sus puertos y constructores ya públicos |
| `Data/` | Sin cambio | Ninguna carpeta ni asset nuevo |

## Risks

| Riesgo | Prob. | Mitigación |
|---|---|---|
| Que alguien **reanude el cambio stale de 2026-09-09** y escriba código contra `Triage`, `RegistrarHechoObtenido` o `m8.Play` — APIs que no existen | Alta | Esta propuesta nombra las cuatro divergencias en una tabla del Intent y recomienda **borrar** el folder viejo, no archivarlo. La spec fija las firmas reales |
| **La escena es humana y no diffeable**: nadie puede revisarla leyendo el PR, y un conflicto se descarta y se rehace | Alta | Se separa en PR2 con compuerta humana propia; el README documenta la construcción paso a paso para que rehacerla sea mecánico y no arqueología |
| **Los ~6 slots de canal apuntando a assets distintos**: todo compila, todas las pruebas pasan, y la sesión no hace nada. El fallo más probable y el que ninguna prueba EditMode ve | Alta | Se deja escrito como el primer punto del README y como criterio explícito de la compuerta física; se minimiza el número de canales cableados (de ahí dejar los dos huérfanos sin cablear) |
| Las **3 `PhysicalAction` de M7 que ningún emisor produce** dejan ramas silenciosamente inalcanzables | Media | Declaradas fuera de alcance; M11 no las trata como caso especial, así que el cambio de seguimiento de M7 no reescribe nada de aquí |
| Que **dejar `IntentResultChannel`/`ReceptivityChangeChannel` sin cablear se lea como un olvido** | Media | Declarado como elección en Scope Out con la evidencia (huérfanos, cero emisores y cero suscriptores) y con el costo que evita (dos assets compartidos más) |
| El componente clínico de `Progress01` **se queda en 0** en toda esta entrega: una demo mostrará progreso parcial | Media | Decisión 2 del usuario, consciente. La renormalización sin clave de M9 ya lo absorbe. El seguimiento sobre M15 queda nombrado y es **no bloqueante** |
| `SessionDirector` se convierte en un **"módulo dios"** que reimplementa lógica ajena | Media | Solo elige, secuencia, enruta y delega. No clasifica, no genera, no evalúa, no puntúa: llama a puertos. Su `.asmdef` referencia únicamente `NpcAi.Core`, lo que lo hace estructuralmente incapaz de reimplementar a otro módulo |
| Sin CI ni runner headless, el verde depende de una persona — y aquí además **de una persona con un Quest 3 y una escena construida** | Alta | **Dos** compuertas humanas explícitas, no una (ver Success Criteria). Es el módulo con más trabajo humano de todos los entregados hasta hoy |

## Rollback Plan

Todo es aditivo y nadie depende de M11: es la **hoja** del grafo de dependencias, no un nodo
intermedio. Ningún módulo referencia `NpcAi.Harness` ni por nombre ni por interfaz. Revertir los
commits borra `Runtime/Harness/`, `Samples~/Harness/*.cs`, `Tests/EditMode/Harness/` y la spec, y
M11 vuelve al estado "solo README" que ya está en `main`. No hay migración de datos, no se crea
ningún asset ni carpeta en `Data/`, no hay dependencia nueva en `package.json` (la entrada del
sample ya existía), no hay cambio de `Contract.Version`, y ningún `.asmdef` ajeno se modifica. Si
solo falla PR2, revertirlo deja PR1 en verde y M11 sin escena: un estado coherente, no roto.

## Dependencies

Implementaciones reales que este cambio **consume** (todas ya en `main`), sin modificar ninguna:

- **M1** `SpeechToTextBehaviour` → emite en `UtteranceChannel`.
- **M2** `BertIntentClassifier` → `IIntentClassifier.Classify`.
- **M4** `ReceptivityEngine` → `Reset`, `Evaluate`, `Current`.
- **M6** `MarkovDialogueGenerator` → `IDialogueGenerator.Generate`.
- **M8** `NpcPresenterBehaviour` → se auto-suscribe a `NpcReplyChannel`.
- **M9** `TriageScenarioObjective` → `Notify`, `AssignCase`, `DeclareTriage`, `Progress01`.
- **M13** `SessionLogBehaviour` → auto-suscrito a los canales; `IniciarSesion`/`FinalizarSesion`.
- **M14** `Data/Cases/caso-01..03.json` → solo la lista de `ClinicalCaseId`, en solo lectura.
- **M15** `ClinicalResponder` → `AssignCase`, `Respond`.
- **M0** `contrato-nucleo-m0` y `canales-evento-nucleo-m0`, archivados y **congelados**.

**Explícitamente NO son dependencias**: **M10** (Sala de Juntas, sigue siendo el doble genérico) ni
**M16** (`IRequirementResponder`; solo aterrizó la forma del contrato en M0 v3, cero código en
`Runtime/`). Decisión 1 del usuario.

Dobles usados por las pruebas (nombres verificados en `Runtime/*/Fakes/`):
`ScriptedIntentClassifier`, `ScriptedClinicalResponder`, `ScriptedDialogueGenerator`,
`ScriptedReceptivityEngine`, `ScriptedScenarioObjective`, `RecordingNpcPresenter`.
Ningún paquete nuevo.

## Success Criteria

**PR1 — lógica**

- [ ] `SessionDirector` vive en `Runtime/Harness/` (**fuera** de `Samples~/`), **no es un
      `MonoBehaviour`**, y su `.asmdef` referencia **solo `NpcAi.Core`**.
- [ ] El enrutado se prueba en las dos direcciones: con un `ScriptedClinicalResponder` que devuelve
      `Handled == true`, `ProcesarTurno` **no** llama al `IDialogueGenerator`; con
      `Handled == false`, **sí** lo llama, y con `m4.Current` como parámetro de receptividad.
- [ ] Los centinelas se prueban: un `Utterance` solo evalúa con `PhysicalAction.Ninguna`; una
      `PhysicalAction` sola evalúa con `IntentResult.Unknown`, mueve la receptividad y **devuelve
      `null` sin publicar respuesta**.
- [ ] `IniciarSesion` dispara los cuatro efectos **juntos y en orden**: `m4.Reset`,
      `m9.AssignCase`, `m15.AssignCase`, y la cáscara `m13.IniciarSesion` — con el **mismo**
      `ClinicalCaseId` en M9 y M15, afirmado en prueba.
- [ ] Reproducibilidad: dos directores con la misma semilla y el mismo catálogo eligen el mismo par
      `(caso, personalidad)`; una sesión nueva no repite el `ClinicalCaseId` anterior.
- [ ] Ningún `Debug.Log` en `Runtime/Harness/`.
- [ ] `SessionDirector` **nunca** llama `INpcPresenter.Play` ni `RegisterRedFlag`, afirmado con
      dobles espía.
- [ ] **Compuerta humana (la ejecuta y la registra el usuario, no el agente)**: Unity Test Runner >
      EditMode > Run All en verde, con el total registrado en `apply-progress.md`.

**PR2 — escena y cáscara**

- [ ] `HarnessBehaviourWiringTests` afirma simetría Subscribe/Unsubscribe en `OnEnable`/`OnDisable`
      sobre **ambos** canales de entrada y el conteo de `Raise` en `NpcReplyChannel`, con el mismo
      estilo de `VrInputBehaviourWiringTests` y `NpcPresenterBehaviourTests`.
- [ ] `Samples~/Harness/README.md` lista los pasos humanos de construcción de escena, con el
      cableado de **canales compartidos** como primer punto.
- [ ] **Compuerta humana de construcción de escena**: la escena existe, importa desde Package
      Manager, y los ~6 slots de canal apuntan a los mismos assets. Esta compuerta **exige que la
      escena exista primero**; no la puede ejecutar ningún agente.
- [ ] **Compuerta física en Quest 3 real** (patrón de M1/M2/M7, **no** el de M9): el usuario juega
      una sesión completa — habla, el NPC responde, la receptividad cambia, el progreso de M9 se
      mueve, la bitácora de M13 registra los turnos — y lo registra. **Ninguna prueba EditMode
      sustituye esta compuerta**: es el único punto donde se verifica que los assets compartidos de
      la escena forman un bucle coherente.
- [ ] `Docs/MODULES.md` (sección M11) deja de decir "solo README" y **nombra lo diferido**: M10/M16,
      la brecha M9↔M15, las 3 acciones de M7 y los dos canales huérfanos.
- [ ] El diff no toca `Runtime/Core/`, `Runtime/CoreChannels/`, ningún otro `Runtime/<Modulo>/`, ni
      `Data/`; se limita a `Runtime/Harness/`, `Samples~/Harness/`, `Tests/EditMode/Harness/`,
      `Docs/` y `openspec/`.

## Decisiones del usuario (confirmadas 2026-09-17)

1. **Alcance: solo el escenario de emergencia para esta primera entrega de M11.** Sala de Juntas
   (M10, que sigue siendo un doble genérico) y M16 (el puerto `IRequirementResponder` de M0 v3
   recién descubierto, para la lógica análoga a la clínica de Sala de Juntas, con cero código en
   `Runtime/` — solo aterrizó la forma del contrato, al parecer por un compañero trabajando en
   paralelo) quedan explícitamente **fuera**. No se cablea M10/M16 en absoluto, **ni contra
   dobles**, en este cambio.
2. **La brecha M9↔M15 del índice de bandera roja: se omite en este primer corte.** M11 **no llama
   `TriageScenarioObjective.RegisterRedFlag(int)` en ninguna parte** de esta entrega — el
   componente clínico de `Progress01` de M9 simplemente se queda en 0 por ahora (la fórmula de M9,
   con su renormalización, ya lo maneja con elegancia; solo no alcanzará el crédito clínico
   completo). El arreglo correcto a largo plazo es un cambio SDD **aparte** y pequeño sobre M15 que
   exponga cuál `Hecho`/`Campo` hizo match mediante un método **aditivo** sobre la clase concreta
   `ClinicalResponder` (mismo precedente de API aditiva más allá del puerto que ya usaron M2, M7 y
   M9) — **no** un cambio de contrato de M0, y **no** algo que M11 deba intentar por su cuenta:
   eso significaría duplicar la lógica de matching de M15, frágil y rechazado explícitamente.

## Proposal question round

No hay canal directo con el usuario en esta fase. Estos supuestos se tomaron por defecto y quedan
para revisión antes de `sdd-spec`/`sdd-design`; ninguno bloquea el arranque de PR1.

1. **La selección con semilla se conserva.** La decisión de 2026-09-09 ("el director elige cuerpo,
   caso y personalidad al azar, con semilla reproducible") **no** se re-confirmó el 2026-09-17. Se
   mantiene la parte de `(caso, personalidad)` porque es lógica pura trivial de probar y da demos
   reproducibles ante el jurado, y se **descarta** la parte del cuerpo del NPC junto con
   `Data/Npcs/` (M8 ya lleva `_vozId` en el Inspector). ¿Se conserva así, o el director debe
   recibir el par `(caso, personalidad)` ya elegido desde fuera?
2. **Una acción física sola no produce habla** (`ProcesarAccion` devuelve `null`). Alternativa
   plausible: que un gesto también dispare una réplica del NPC. ¿Se confirma el silencio?
3. **`Debug.Log` permitido en `Samples~/Harness/` y prohibido en `Runtime/Harness/`.** ¿Se acepta
   esa asimetría, o el sample también debe quedar silencioso?
4. **Namespace `NpcAi.Harness.Sample` para la cáscara** (distinto de `NpcAi.Harness`), para evitar
   colisiones cuando el sample compile dentro del ensamblado del consumidor. ¿Se acepta, o se
   prefiere `NpcAi.Harness` en ambos lados?
5. **El folder stale `openspec/changes/2026-09-09-m11-armado-sesion/` se borra, no se archiva.**
   Esta fase no lo toca; ejecutar el borrado es decisión del orquestador o del usuario.
