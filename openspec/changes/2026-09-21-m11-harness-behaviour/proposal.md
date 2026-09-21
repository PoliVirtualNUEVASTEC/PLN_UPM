# Propuesta: M11 PR2 — cáscara de escena (`HarnessBehaviour`), README y documentación

## Intent

PR1 (`2026-09-17-m11-banco-de-pruebas`, archivado) entregó `Runtime/Harness/SessionDirector.cs`
y su spec promovida (`openspec/specs/banco-de-pruebas-m11/spec.md`, 7 requisitos, 12 escenarios,
26 pruebas EditMode). Verificado contra el código de hoy: **ese director no tiene un solo
llamador en runtime**. Ningún módulo del pipeline consume `UtteranceChannel` ni `PhysicalActionChannel`: el único suscriptor de `UtteranceChannel` es la bitácora de M13 y `PhysicalActionChannel` no tiene ninguno
(los únicos `[SerializeField]` de canal del repositorio son los de M1, M7, M8 y M13), así que el
sistema continúa completo por módulo e inerte como sistema.

PR2 cierra M11: la cáscara `MonoBehaviour` que conecta los canales con el director, sus pruebas
de cableado en EditMode, el README paso a paso para quien construye la escena, y la sección M11
de `Docs/MODULES.md`. Resultado esperado: **una persona puede correr el pipeline completo en una
escena de Unity, sin VR**, y ese mismo cableado es el que después se lleva al Quest 3.

## Scope

### In Scope

- **`Runtime/Harness/Unity/HarnessBehaviour.cs` + `NpcAi.Harness.Unity.asmdef`** (decisión 1):
  `references: ["NpcAi.Core", "NpcAi.Core.Channels", "NpcAi.Harness"]`, namespace
  `NpcAi.Harness.Unity`. Sin `Debug.Log`.
  - `[SerializeField]` de los **3 canales** (`UtteranceChannel`, `PhysicalActionChannel`,
    `NpcReplyChannel`) y de la configuración de sesión (etiqueta de bitácora).
  - Simetría **Subscribe/Unsubscribe en `OnEnable`/`OnDisable` sobre los dos canales de entrada**.
  - Reenvío al director y **un solo `Raise`** del `NpcReply` en `NpcReplyChannel`; una acción
    física sola no publica nada (`ProcesarAccion` devuelve `null`).
  - Costura de inyección del `SessionDirector` ya construido (ver Approach) y costura de prueba
    `CablearParaPrueba`/`DescablearParaPrueba`, patrón de `VrInputBehaviour` y
    `NpcPresenterBehaviour`.
  - **Inicio de sesión como una sola operación coherente**: los 3 efectos del director más el
    cuarto efecto de M13 (`SessionLogBehaviour.IniciarSesion(etiqueta)`, AD13 de PR1), y el
    cierre simétrico de esa sesión de bitácora.
  - `DeclararTriaje(string)` público, para que un control de escena pueda cerrar el caso.
- **`Samples~/Harness/` — raíz de composición del anfitrión**: el script que construye las
  implementaciones concretas (M2, M4, M6, M9, M15) y las inyecta en la cáscara. Es código de
  aplicación anfitriona, **no compilado por Unity dentro del paquete** y por lo tanto **no
  probable en EditMode**; se mantiene lo más delgado posible.
- **`Tests/EditMode/Harness/HarnessBehaviourWiringTests.cs`** (+ las referencias que falten en
  `NpcAi.Harness.Tests.asmdef`), reutilizando los espías locales de `EspiasDeArnes.cs`.
- **`Samples~/Harness/README.md`** reescrito: pasos humanos de construcción de escena, con el
  **cableado de canales compartidos como primer punto**.
- **`Docs/MODULES.md`, sección M11**: hoy todavía dice "solo existe `Samples~/Harness/README.md`"
  y "Specs formales: no existe". Se actualiza y se nombra lo diferido, más la nota de la regla
  dura 4 (decisión 3).
- **Adenda de spec sobre `banco-de-pruebas-m11`**: la spec promovida ya anuncia que el cableado
  de escena "tendrá su propio addendum tras el merge de PR1".

### Out of Scope

- **El archivo `.unity`**: tarea humana documentada; `rules.apply` prohíbe fusionar YAML de Unity
  a mano.
- **M10 y M16**, ni siquiera contra dobles (decisión 2). `Runtime/RequirementResponse/` ya existe
  en `main` por trabajo de otro compañero; sigue fuera de M11.
- **La brecha M9↔M15 de índices de bandera roja** (decisión 2): M11 no llama `RegisterRedFlag`.
- **Las 3 `PhysicalAction` diferidas de M7** (`EntregarObjeto`, `SenalarPantalla`, `GestoCalma`):
  ninguna fuente las produce.
- **`IntentResultChannel` y `ReceptivityChangeChannel`**: verificado hoy, siguen con cero
  emisores y cero suscriptores en todo el repositorio. Se dejan sin cablear a propósito.
- **Cualquier cambio de contrato** y cualquier otro `Runtime/<Modulo>/` o `Data/`.
- **Métricas del Objetivo 4** y cualquier HUD de depuración.

## Capabilities

### New Capabilities

- Ninguna.

### Modified Capabilities

- `banco-de-pruebas-m11`: adenda con los requisitos de la cáscara (simetría Subscribe/Unsubscribe
  sobre ambos canales de entrada, un solo `Raise` por turno hablado y cero por acción física
  sola, inicio de sesión como una operación con los cuatro efectos, y la frontera de inyección).

## Approach

### El problema de la regla dura 3, y cómo se reconcilia

La regla dura 3 permite a un módulo referenciar `NpcAi.Core` y, si se conecta por Inspector,
`NpcAi.Core.Channels`. Referenciar el ensamblado del **propio** módulo no es una referencia entre
módulos: `NpcAi.SessionLog.Unity` → `NpcAi.SessionLog` y `NpcAi.Receptivity.Unity` →
`NpcAi.Receptivity` son el precedente exacto. Por lo tanto `NpcAi.Harness.Unity` puede ver
`NpcAi.Harness`, pero **no puede ver a M2, M4, M6, M9, M15 ni M13**.

Eso importa porque los cinco concretos se construyen con insumos del anfitrión:
`BertIntentClassifier(ModelAsset, TextAsset)`, `MarkovDialogueGenerator(IReadOnlyDictionary<string,
TextAsset>)`, `ClinicalResponder(Func<ClinicalCaseId,string>)`,
`TriageScenarioObjective(TriageObjectiveSettings, Func<...>)` y `ReceptivityEngine(catálogo)`.
Una cáscara dentro del paquete **no puede compilar ninguna de esas líneas**.

| Opción | Qué implica | Por qué se acepta o se rechaza |
|---|---|---|
| **A. Cáscara = puente de canales con director inyectado; composición en `Samples~/`** (recomendada) | La cáscara vive en el paquete, se prueba en EditMode y recibe el `SessionDirector` ya armado; el script de `Samples~/Harness/` construye los concretos y lo inyecta | Único camino que respeta la decisión 1 y la regla dura 3 a la vez. Mismo rol que el proyecto VR anfitrión: consumidor, no par. **Costo declarado**: el código de composición no es probable en EditMode |
| B. Agregar referencias a M2/M4/M6/M9/M15 en `NpcAi.Harness.Unity` | La cáscara construye todo | Rompe la regla dura 3. Sería un cambio de contrato (regla dura 2), fuera de alcance |
| C. Devolver la cáscara a `Samples~/` (plan original de PR1) | Sin cambios de regla | Ya descartado por el usuario (decisión 1): `Samples~/` no lo compila Unity, y `HarnessBehaviourWiringTests` no existiría |
| D. Fábricas `ScriptableObject` declaradas en `NpcAi.Core.Channels` | Inyección por Inspector | Cambio de contrato sobre M0 congelado. Rechazada |

**Consecuencia que se declara en voz alta**: con la opción A, la raíz de composición del
anfitrión queda **sin ninguna prueba automatizada**. Por eso se mantiene mínima (construir e
inyectar, nada más): todo comportamiento vive en `Runtime/Harness/`, y esa raíz se cubre solo con
las dos compuertas humanas.

### El cuarto efecto (M13) sin referencia entre pares

La cáscara tampoco puede referenciar `NpcAi.SessionLog.Unity`. La salida es la misma forma que
usó PR1 en su AD1 para la superficie aditiva de M9: **una costura inyectada**, ya sea un
`UnityEvent` serializado que el humano cablea en el Inspector contra
`SessionLogBehaviour.IniciarSesion(string)` — cero referencias, visible en la escena, probable con
`AddListener` — o un `Action<string>` que fija la raíz de composición. `sdd-design` elige; esta
propuesta solo exige que los cuatro efectos sigan siendo **una sola operación** de la cáscara.

### Pruebas de cableado

Lo que las pruebas deben observar es de nivel de canal (suscripción, desuscripción sin fuga,
conteo de `Raise`), así que se arma un `SessionDirector` real con los **espías locales ya
existentes** en `Tests/EditMode/Harness/EspiasDeArnes.cs`. No se agregan referencias a módulos
ajenos en el ensamblado de pruebas: el AD11 de PR1 documentó que los dobles publicados
(`Scripted*`) no observan lo que hace falta, y referenciar `NpcAi.Nlu` arrastraría Sentis al
cierre de compilación de EditMode.

## Affected Areas

| Área | Impacto | Descripción |
|---|---|---|
| `Runtime/Harness/Unity/NpcAi.Harness.Unity.asmdef` | Nuevo | `NpcAi.Core`, `NpcAi.Core.Channels`, `NpcAi.Harness` |
| `Runtime/Harness/Unity/HarnessBehaviour.cs` | Nuevo | Puente de canales, inicio/cierre de sesión, costuras de inyección y de prueba |
| `Tests/EditMode/Harness/HarnessBehaviourWiringTests.cs` | Nuevo | Simetría en ambos canales y conteo de `Raise` |
| `Tests/EditMode/Harness/NpcAi.Harness.Tests.asmdef` | Modificado | + `NpcAi.Harness.Unity`, + `NpcAi.Core.Channels` |
| `Samples~/Harness/` (script de composición) | Nuevo | Construye los concretos y los inyecta; sin pruebas EditMode |
| `Samples~/Harness/README.md` | Modificado | Pasos humanos; canales compartidos como primer punto |
| `Docs/MODULES.md` (sección M11) | Modificado | Estado real, diferidos y nota de la regla dura 4 |
| `openspec/changes/2026-09-21-m11-harness-behaviour/` | Nuevo | Artefactos SDD, incluida la adenda de spec |
| `Runtime/Core/`, `Runtime/CoreChannels/`, otros `Runtime/<Modulo>/`, `Data/` | Sin cambio | M11 solo consume; sin cambio de contrato |

## Risks

| Riesgo | Prob. | Mitigación |
|---|---|---|
| **Los slots de canal apuntando a assets distintos**: todo compila, todo pasa, y la sesión no hace nada. Ninguna prueba EditMode lo ve. Conteo verificado: **8 slots de Inspector** (M1 ×1, M7 ×1, M8 ×1, M13 ×2, cáscara ×3) sobre **3 assets compartidos** | Alta | Primer punto del README, con los 8 slots nombrados uno por uno, y criterio explícito de la compuerta de escena |
| **La raíz de composición del anfitrión no tiene pruebas** y es donde se arman los cinco concretos | Alta | Se mantiene mínima por diseño (construir e inyectar); todo comportamiento vive en `Runtime/Harness/`, cubierto en EditMode |
| Una cáscara sin director inyectado queda **inerte y silenciosa** | Media | Guarda explícita en la cáscara y requisito de la adenda de spec; `sdd-design` decide si es no-op silencioso o falla ruidosa |
| La escena `.unity` **no es diffeable**: nadie la revisa leyendo el PR | Alta | Fuera de alcance como artefacto; el README la vuelve reconstruible de forma mecánica |
| El **cierre de sesión de M13** se olvida: `SessionRecorder.ReanudarUltimaSesion()` retoma una sesión no cerrada y concatena turnos nuevos con los viejos | Media | El cierre simétrico entra en el alcance de la cáscara y en la adenda de spec |
| Sin CI, el verde depende de una persona **con Quest 3 y una escena construida** | Alta | Dos compuertas humanas explícitas (escena y hardware), ninguna ejecutable por el agente |

## Rollback

Todo es aditivo y M11 sigue siendo la hoja del grafo: ningún ensamblado referencia
`NpcAi.Harness` ni `NpcAi.Harness.Unity`. Revertir los commits borra `Runtime/Harness/Unity/`,
`Tests/EditMode/Harness/HarnessBehaviourWiringTests.cs` y el script de `Samples~/Harness/`, y
deja `main` en el estado de PR1: director en verde, sin escena. Estado coherente, no roto. Sin
migración de datos, sin asset nuevo en `Data/`, sin dependencia nueva en `package.json` (la
entrada del sample ya existe), sin cambio de `Contract.Version`, sin `.asmdef` ajeno modificado.

## Success Criteria

- [ ] `NpcAi.Harness.Unity.asmdef` referencia exactamente `NpcAi.Core`, `NpcAi.Core.Channels` y
      `NpcAi.Harness`, y ningún otro módulo.
- [ ] Cero `Debug.Log` y cero `UnityEngine.Debug` bajo `Runtime/Harness/`.
- [ ] `HarnessBehaviourWiringTests` afirma: `OnEnable` suscribe **los dos** canales de entrada;
      `OnDisable` desuscribe **los dos** sin fuga; un turno hablado produce **exactamente un**
      `Raise` en `NpcReplyChannel`; una acción física sola produce **cero** `Raise`.
- [ ] El `IniciarSesion` de la cáscara dispara los cuatro efectos juntos (los 3 del director más
      la costura de M13), afirmado en EditMode.
- [ ] `Samples~/Harness/README.md` lista los pasos humanos con el cableado de canales compartidos
      como primer punto, nombrando los 8 slots y los 3 assets.
- [ ] `Docs/MODULES.md` sección M11 ya no dice "solo README", declara las carpetas y la spec
      reales, nombra lo diferido (M10/M16, brecha M9↔M15, las 3 acciones de M7, los 2 canales
      huérfanos) e incluye la nota de la regla dura 4.
- [ ] **Compuerta humana 1 (escena)**: la escena existe, importa desde Package Manager y los 8
      slots apuntan a los 3 assets compartidos. La ejecuta y la registra el usuario.
- [ ] **Compuerta humana 2 (Quest 3 físico, patrón M1/M2/M7)**: sesión completa jugada — se
      habla, el NPC responde, la receptividad cambia, el progreso de M9 se mueve, M13 registra
      los turnos. Ninguna prueba EditMode la sustituye.
- [ ] Unity Test Runner > EditMode > Run All en verde, con el total registrado por el usuario.
- [ ] El diff se limita a `Runtime/Harness/`, `Samples~/Harness/`, `Tests/EditMode/Harness/`,
      `Docs/` y `openspec/`.

## Decisions

1. **2026-09-21, confirmada por el usuario — la cáscara vive en el paquete.**
   `HarnessBehaviour` va en `Runtime/Harness/Unity/` con asmdef propio `NpcAi.Harness.Unity` y
   namespace `NpcAi.Harness.Unity` (precedentes `NpcAi.SessionLog.Unity` y
   `NpcAi.Receptivity.Unity`), para que `HarnessBehaviourWiringTests` corra en EditMode. La
   propuesta de PR1 la había puesto en `Samples~/`, que Unity no compila con el paquete, lo que
   contradecía su propio requisito de pruebas de cableado. **Consecuencia**: sin `Debug.Log`,
   como todo `Behaviour` del paquete. Cierra las preguntas abiertas 3 y 4 de PR1.
2. **2026-09-17, vigente — alcance acotado al escenario de emergencia.** M10 y M16 quedan fuera,
   ni siquiera contra dobles; la brecha M9↔M15 de índices de bandera roja se omite (M11 no llama
   `RegisterRedFlag` en ninguna parte). `Runtime/RequirementResponse/`, ya en `main` por trabajo
   de otro compañero, sigue fuera de M11.
3. **Excepción a la regla dura 4 (hallazgo W7 del verify de PR1), por defecto del orquestador,
   vetable por el usuario**: se registra en la sección M11 de `Docs/MODULES.md` por qué
   `Runtime/Harness/` no publica `Fakes/` — no implementa ningún puerto, los consume; no hay
   gemela de contrato que heredar; `Core` y `CoreChannels` tampoco publican dobles. El usuario
   puede vetarlo o redirigirlo a `openspec/config.yaml` o a la adenda de diseño.

## Open Questions

Cada una trae un valor por defecto; ninguna bloquea `sdd-spec` ni `sdd-design`.

1. **¿La cáscara también cierra la sesión de M13?** Por defecto **sí**: `SessionLogBehaviour`
   retoma automáticamente una sesión no cerrada y concatena los turnos nuevos con los anteriores,
   lo que ensucia la bitácora que es el insumo del Objetivo 4.
2. **¿Quién dispara el inicio de sesión en la escena?** Por defecto, un método público que el
   humano cablea a un control de la escena, más un flag opcional de arranque automático. No se
   construye UI en este cambio.
3. **¿De dónde sale la etiqueta de la sesión de M13?** Por defecto, un `[SerializeField]` de
   prefijo que la cáscara combina con el caso elegido y la marca de tiempo.
4. **`DeclararTriaje` no tiene superficie de entrada en la escena.** Por defecto se expone como
   método público de la cáscara y se documenta en el README; sin control de UI, el componente de
   triaje del progreso de M9 también se queda en 0. Construir ese control sería alcance nuevo.

## Review Workload

Presupuesto de revisión acordado para la sesión: **800 líneas**, estrategia `ask-on-risk`. Se
espera holgadamente por debajo: el grueso del costo de PR2 no son líneas de diff sino trabajo
humano en el GUI del Editor y una ejecución física. El pronóstico exacto (`Decision needed before
apply`, `Chained PRs recommended`, `budget risk`) es trabajo de `sdd-tasks`, no de esta fase.
