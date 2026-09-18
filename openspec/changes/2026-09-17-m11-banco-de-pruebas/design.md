# Design: M11 — Banco de pruebas, PR1 (`SessionDirector`)

## Technical Approach

Una sola clase C# pura, `NpcAi.Harness.SessionDirector`, que **elige, secuencia, enruta y delega**.
No clasifica, no genera, no evalúa, no puntúa: las cinco piezas de comportamiento entran por
interfaz de `NpcAi.Core` y salen como valores. No es `MonoBehaviour`, no toca canales, no toca
disco, no imprime.

| Responsabilidad | Cómo se cumple |
|---|---|
| Elegir `(caso, personalidad)` | `System.Random` propio, semilla inyectada, catálogo inyectado |
| Secuenciar el arranque | Un único método privado `Arrancar` dispara los 3 efectos del director |
| Enrutar el turno | `clin.Handled ? clin.Reply : m6.Generate(...)` — una rama, una línea |
| Delegar | Cinco puertos + dos delegados hacia la superficie aditiva de M9 |

Alcance: **solo PR1**. `Samples~/Harness/HarnessBehaviour.cs`, la escena, el README y
`Docs/MODULES.md` son PR2 y tendrán su propio addendum. Este diseño solo confirma que la frontera
de PR1 queda limpia: el director recibe `Utterance`/`PhysicalAction` como **argumentos de método
ya deserializados** y devuelve `NpcReply`/`NpcReply?` como **valores de retorno**. Quién los saca
de un canal y quién los publica es trabajo de la cáscara.

### Unidad de módulo y grafo de referencias

    NpcAi.Harness       -> [NpcAi.Core]                                  (noEngineReferences: true)
    NpcAi.Harness.Tests -> [NpcAi.Core, NpcAi.Harness,
                            NpcAi.ClinicalResponse, NpcAi.Presentation]  (Editor)

`Runtime/Harness/NpcAi.Harness.asmdef` copia literalmente la forma de
`Runtime/Receptivity/NpcAi.Receptivity.asmdef`: una sola referencia (`NpcAi.Core`) y
`noEngineReferences: true`. **No** referencia `NpcAi.Core.Channels`: los canales los toca la
cáscara, no el director (regla dura 3 intacta; M11 es consumidor, no par).

`NpcAi.Harness.Tests` es el **primer ensamblado de pruebas del repositorio que referencia módulos
ajenos** (`NpcAi.ClinicalResponse`, `NpcAi.Presentation`). Verificado: ningún otro
`Tests/EditMode/*/*.asmdef` referencia un `Runtime/` que no sea el propio. Se justifica porque la
spec **nombra** `ScriptedClinicalResponder` y `RecordingNpcPresenter` en sus cláusulas `Dado`, y
porque los dobles publicados existen precisamente para esto: el comentario de
`ScriptedClinicalResponder` dice *"para que otros modulos (M4, M8, M11) integren ... sin depender
de M14"* y el de `ScriptedIntentClassifier` nombra a M11 igual. No se referencian `NpcAi.Nlu`
(arrastraría Sentis al cierre de compilación EditMode), `NpcAi.Dialogue` ni `NpcAi.Receptivity`
(ver AD11).

## Architecture Decisions

| # | Decisión | Elección | Alternativas rechazadas | Razón |
|---|---|---|---|---|
| AD1 | **La brecha estructural de M9** | Dos delegados inyectados: `Action<ClinicalCaseId> asignarCasoAlObjetivo` y `Action<string> declararTriajeEnObjetivo` | Referenciar `NpcAi.Scenarios.Emergency`; agregar `AssignCase` a `IScenarioObjective`; declarar una interfaz propia que M9 implemente; dejar `m9.AssignCase` a la cáscara | `IScenarioObjective` (`Ports.cs:181-199`) expone **solo** `Progress01`, `IsComplete` y `Notify`. `AssignCase(ClinicalCaseId)` y `DeclareTriage(string)` son la superficie **aditiva de la clase concreta** `TriageScenarioObjective`, en otro ensamblado. Con `references: ["NpcAi.Core"]` el director **no puede compilar** esas llamadas. Referenciar M9 rompe la regla dura 3; tocar M0 es un cambio de contrato fuera de alcance; una interfaz propia invierte la dependencia (M9 tendría que referenciar M11); dejarlo a la cáscara haría **inobservable en EditMode** la invariante que la spec exige ("el mismo `ClinicalCaseId` en M9 y M15"). El delegado es el precedente exacto del repositorio: `ClinicalResponder` recibe `Func<ClinicalCaseId,string> cargarJson` (AD3 de M15) y `TriageScenarioObjective` el mismo, justo para no referenciar a quien produce el dato |
| AD2 | Forma del constructor | **Uno solo, público, 10 parámetros, validación estricta** | Constructor pobre + rico (patrón M9/M4/M15); POCO `ConfiguracionDeSesion` que agrupe catálogo y semilla; delegados opcionales `= null` | El constructor pobre de M9/M4/M15 existe por una razón concreta: la **gemela de contrato** necesita `CreateSubject()` sin argumentos. `SessionDirector` **no implementa ningún puerto**, así que no tiene gemela de contrato y esa razón desaparece. Un POCO agregaría un segundo tipo público a un módulo cuyo único argumento contra el riesgo "módulo dios" es tener exactamente un tipo público. Diez parámetros son un olor en una clase normal y **la firma esperable de una raíz de composición**: cada parámetro es un colaborador o una entrada obligatoria |
| AD3 | Validación en construcción | Los 5 puertos y los 2 delegados nulos → `ArgumentNullException`; catálogos nulos o vacíos → `ArgumentException`. **Los métodos nunca lanzan; solo el constructor** | Tolerar nulos como no-op (precedente `cargarJson` PUEDE ser null de M9/M15) | Desviación deliberada de ese precedente, con su razón: en M9/M15 el nulo modela un **dato ausente**, que es un estado legítimo en ejecución. Aquí modelaría un **cable ausente en la raíz de composición**, que nunca es legítimo. La propuesta nombra "todo compila, todas las pruebas pasan y la sesión no hace nada" como el fallo más probable y más caro de M11: la construcción es el único momento en que ese fallo puede ser ruidoso, y ocurre en EditMode. Los contratos "NO DEBE lanzar" de M0 no obligan a esta clase porque no implementa ningún puerto; aun así `IniciarSesion`, `ProcesarTurno`, `ProcesarAccion` y `DeclararTriaje` no lanzan nunca |
| AD4 | Visibilidad | Todo `public` | `internal` + `AssemblyInfo.cs` con `InternalsVisibleTo` (precedente M1/M7/M8) | El AD2 de M9 dice `public` porque el llamador real (M11) vive en otro ensamblado. Aquí el argumento es **más fuerte, no igual**: el llamador real es `HarnessBehaviour` en `Samples~/Harness/`, que al importarse desde Package Manager compila dentro del ensamblado **del consumidor**, cuyo nombre no existe al momento de escribir el paquete. Un `InternalsVisibleTo` para ese destino es literalmente inescribible |
| AD5 | Arranque de sesión | **Dos sobrecargas** `IniciarSesion()` y `IniciarSesion(ClinicalCaseId, PersonalityId)`, ambas delegando en un único `Arrancar(caso, personalidad)` privado | Un solo método con parámetros opcionales/`default`; un `bool usarSeleccion`; que el catálogo sea opcional y la sobrecarga sin argumentos sea no-op sin él | La spec separa dos preguntas: **cómo se elige el par** (requisito "Selección reproducible por semilla") y **qué efectos dispara el arranque** (requisito "Secuencia de inicio de sesión"). Dos sobrecargas sobre un `Arrancar` común hacen **estructuralmente imposible** que los dos caminos difieran en los efectos, que es justo lo que la spec prohíbe ("Divergir ese caso entre M9 y M15 está prohibido"): existe un único `var caso` local que se entrega a los dos sumideros. `default(ClinicalCaseId)` como centinela de "elige tú" fue rechazado: `ClinicalCaseId.None` ya significa "ningún caso", y darle un segundo significado rompe el patrón centinela de v1 |
| AD6 | Fuente de azar | `int semilla` en el constructor, `new System.Random(semilla)` privado | Recibir un `System.Random` ya construido; `UnityEngine.Random`; `Guid`/reloj | La spec dice "dos `SessionDirector` con el mismo catálogo y **la misma semilla**": recibir un `int` hace esa frase directamente expresable y deja la reproducibilidad como propiedad del director, no del llamador. Un `System.Random` compartido y mutado desde fuera la rompería. `UnityEngine.Random` es global, no sembrable por instancia y exige `UnityEngine`, que `noEngineReferences: true` prohíbe. Precedente: `MarkovDialogueGenerator` ya guarda un `System.Random` privado (sin semilla, porque M6 no necesita reproducibilidad) |
| AD7 | Regla "no repetir el caso anterior" | **Lista filtrada**: se construye la lista de candidatos distintos de `_ultimoCaso` y se saca **un** índice de ella; si queda vacía se usa el catálogo completo | Re-tirada hasta que el caso salga distinto; índice desplazado (`(anterior + 1 + rnd) % n`) | La re-tirada **no termina** con un catálogo de un solo caso, y el catálogo real de M14 hoy tiene tres: dos borrados de archivo lo dejarían en uno. La lista filtrada resuelve ese borde sin rama especial (el respaldo cubre además el catálogo con ids duplicados). Segundo motivo, decisivo para la reproducibilidad: la lista filtrada consume **exactamente dos tiradas por selección** (caso y personalidad, en ese orden), así que dos directores con la misma semilla recorren la misma secuencia de `Random`; con re-tirada el número de tiradas depende del estado y la igualdad solo se sostendría por casualidad |
| AD8 | Memoria del último caso | `_ultimoCaso` es estado **de instancia**, y lo actualizan **ambas** sobrecargas | Actualizarlo solo en la sobrecarga con selección; persistirlo (M13, `PlayerPrefs`) | Actualizarlo en las dos deja el escenario "cuya sesión anterior eligió `caso-01`" verificable de forma determinista: la prueba fija el estado con `IniciarSesion(caso-01, p)` y luego llama `IniciarSesion()`, sin depender de qué eligió el azar primero. No se persiste: reconstruir el director olvida el último caso, y una sesión nueva de proceso nuevo puede repetir. Persistirlo exigiría a M11 escribir estado, fuera de alcance |
| AD9 | Chequeos defensivos de `IsReady` | **Ninguno**. El director nunca lee `m2.IsReady` ni `m15.IsReady` | Guardas `if (!m2.IsReady) return ...` antes de cada llamada | Los contratos de M0 ya definen el camino seguro y `IntentClassifierContract` lo prueba: `Classify` "NO DEBE lanzar en ningún estado" y con `IsReady == false` devuelve `Unknown` (`Un_clasificador_no_listo_devuelve_Unknown`); `Respond` con `IsReady == false` "DEBE devolver `NoAplica`", que el enrutador ya convierte en turno social; `Generate` nunca devuelve texto vacío ni con `PersonalityId.None`. Una guarda duplicaría esa lógica en el llamador y podría **divergir** de ella: exactamente el riesgo "módulo dios" |
| AD10 | Posición de `m9.Notify` | Antes de la bifurcación clínico/social, en **un solo sitio de llamada** | Notificar dentro de cada rama; notificar al final del turno | La receptividad se mueve en todo turno, gane quien gane la bifurcación. Un solo sitio de llamada antes de la rama lo hace incondicional por construcción; duplicarlo en dos ramas crea la posibilidad de olvidarlo en una. Coincide con el orden de la tabla de composición de la propuesta (Objetivo antes de Respuesta clínica) |
| AD11 | Dobles de prueba | Cinco espías **locales** en `Tests/EditMode/Harness/`, más `ScriptedClinicalResponder` y `RecordingNpcPresenter` compartidos donde la spec los nombra | Usar los seis `Scripted*`/`Recording*` publicados que lista la propuesta | Verificado leyendo cada `Fakes/`: los dobles compartidos **no pueden observar** lo que la spec exige observar. `ScriptedReceptivityEngine` no registra los argumentos de `Evaluate` (2 escenarios) y su delta para `Acercarse` es **0**, así que el escenario "`ProcesarAccion(PhysicalAction.Acercarse)` ⇒ `m4.Current` cambia" es **insatisfacible** con él tal como está escrito. `ScriptedIntentClassifier` no cuenta invocaciones ("`m2.Classify` no se invoca"). `ScriptedDialogueGenerator` no registra sus argumentos. `ScriptedScenarioObjective` es `sealed`, no tiene `AssignCase` y **no tiene `RegisterRedFlag`**, así que "un `ScriptedScenarioObjective` espía que cuenta llamadas a `RegisterRedFlag`" no existe. `ScriptedClinicalResponder` no expone el `ClinicalCaseId` que recibió. Precedente exacto del repositorio para este caso: `IntentClassifierContract.ClasificadorNoListo`, *"stub minimo ... definido DENTRO de Tests/EditMode/Core/ (decision D3): no es un doble de modulo"*, creado porque el doble compartido no alcanza el estado bajo prueba. Los espías locales **no duplican comportamiento**: graban |
| AD12 | `noEngineReferences: true` | Se declara en `NpcAi.Harness.asmdef` | Dejarlo en `false` como el resto de los módulos con canales | Consecuencia directa y gratuita: sin `UnityEngine.dll` en el ensamblado, `Debug.Log` **no compila**. El criterio de éxito "ningún `Debug.Log` en `Runtime/Harness/`" pasa de ser algo que alguien debe notar leyendo el diff a un error de compilación. Es la misma razón por la que la regla dura 6 lo exige en M4 |
| AD13 | Fronteras que el director no cruza | Sin `FinalizarSesion`, sin `INpcPresenter`, sin `RegisterRedFlag`, sin canales, sin `Data/` | Que el director también arranque y cierre M13; que publique el `NpcReply` él mismo | `SessionLogBehaviour` (M13) se auto-suscribe a los canales y no expone costura inyectable, así que `m13.IniciarSesion`/`FinalizarSesion` son de la cáscara: darle al director un método que no puede cumplir sería mentir en la firma. El único `Raise` del turno lo hace la cáscara, y de ahí llegan M8 y M13 con una sola publicación. `RegisterRedFlag` queda fuera por la decisión 2 del usuario y, además, es estructuralmente inalcanzable (AD1) |

## Interfaces / Contracts

Sin cambios en `NpcAi.Core`. La superficie pública nueva del módulo es **un solo tipo**.

```csharp
// Runtime/Harness/SessionDirector.cs
namespace NpcAi.Harness
{
    /// <summary>
    /// M11 — raiz de composicion del escenario de emergencia. Elige, secuencia, enruta y
    /// delega. C# puro: no es MonoBehaviour, no toca canales, no lee disco, no imprime.
    /// </summary>
    public sealed class SessionDirector
    {
        /// <param name="asignarCasoAlObjetivo">
        /// Puente hacia la superficie aditiva de M9 (AD1): IScenarioObjective NO declara
        /// AssignCase, y este ensamblado solo referencia NpcAi.Core. Lo cablea el compositor.
        /// </param>
        /// <param name="semilla">Reproducibilidad: misma semilla + mismo catalogo, mismo par.</param>
        public SessionDirector(
            IIntentClassifier             m2,
            IReceptivityEngine            m4,
            IDialogueGenerator            m6,
            IScenarioObjective            m9,
            IClinicalResponder            m15,
            Action<ClinicalCaseId>        asignarCasoAlObjetivo,
            Action<string>                declararTriajeEnObjetivo,
            IReadOnlyList<ClinicalCaseId> casos,
            IReadOnlyList<PersonalityId>  personalidades,
            int                           semilla);

        // --- Lecturas de estado ---
        public ClinicalCaseId CasoActual         { get; }   // None antes del primer arranque
        public PersonalityId  PersonalidadActual { get; }   // None antes del primer arranque
        public float          Progreso           { get; }   // => _m9.Progress01, sin cachear

        // --- Sesion ---
        public void IniciarSesion();                                              // AD5: elige y arranca
        public void IniciarSesion(ClinicalCaseId caso, PersonalityId personalidad); // AD5: explicito

        // --- Turno ---
        public NpcReply  ProcesarTurno(Utterance utterance);   // nunca null: M15/M6 lo garantizan
        public NpcReply? ProcesarAccion(PhysicalAction accion); // siempre null en esta entrega

        // --- Cierre clinico (superficie aditiva de M9, via delegado) ---
        public void DeclararTriaje(string categoria);
    }
}
```

`ProcesarTurno` puede devolver `NpcReply` no anulable **por contrato**, no por optimismo: con
`Handled == true`, `Ports.cs:76-77` garantiza `Reply.Text` no vacío y tags no nulos; con
`Handled == false`, `IDialogueGenerator` garantiza texto no vacío para cualquier `Receptivity` y
para `PersonalityId.None`. `ProcesarAccion` devuelve `NpcReply?` porque la spec lo exige
("DEBE devolver `null`"); el tipo anulable, y no `void`, deja la pregunta 2 de la propuesta
("¿un gesto también habla?") como un cambio de una línea en el cuerpo, sin tocar la firma ni la
cáscara.

### Estado y arranque (pseudocódigo)

```
_m2 .. _m15          : puertos, nunca null (AD3)
_asignarCaso         : Action<ClinicalCaseId>, nunca null (AD3)
_declararTriaje      : Action<string>,         nunca null (AD3)
_casos               : ClinicalCaseId[]   copia propia, longitud >= 1
_personalidades      : PersonalityId[]    copia propia, longitud >= 1
_rng                 : System.Random = new System.Random(semilla)
_ultimoCaso          : ClinicalCaseId = ClinicalCaseId.None

ctor:
    cada puerto y cada delegado null            -> throw ArgumentNullException
    casos/personalidades null o vacios          -> throw ArgumentException
    _casos = casos.ToArray(); _personalidades = personalidades.ToArray()
    // copia propia: si el llamador muta su List<T> despues, la reproducibilidad
    // exigida por la spec seguiria valiendo

IniciarSesion():                                  // AD5, AD6, AD7
    var caso         = SiguienteCaso()            // tirada 1 de 2, siempre
    var personalidad = _personalidades[_rng.Next(_personalidades.Length)]   // tirada 2 de 2
    Arrancar(caso, personalidad)

IniciarSesion(caso, personalidad):                // AD5: no toca _rng en absoluto
    Arrancar(caso, personalidad)

SiguienteCaso():                                  // AD7: lista filtrada, nunca re-tirada
    if (_ultimoCaso.IsNone)
        return _casos[_rng.Next(_casos.Length)]   // primera sesion: catalogo completo

    candidatos = [c in _casos where c != _ultimoCaso]     // ClinicalCaseId.operator!=
    if (candidatos.Count == 0)
        candidatos = _casos                       // catalogo de 1 caso, o todo duplicado
    return candidatos[_rng.Next(candidatos.Count)]

Arrancar(caso, personalidad):                     // UNICO sitio con los 3 efectos
    CasoActual         = caso                     // un solo valor local para los dos sumideros
    PersonalidadActual = personalidad
    _ultimoCaso        = caso                     // AD8: tambien en el camino explicito

    _m4.Reset(personalidad)                       // M4 — estado inicial del perfil
    _asignarCaso(caso)                            // M9 — AssignCase(caso), via delegado (AD1)
    _m15.AssignCase(caso, personalidad)           // M15 — mismo ClinicalCaseId que M9
    // El cuarto efecto, m13.IniciarSesion(etiqueta), es de la cascara (PR2, AD13)
```

### Turno (pseudocódigo)

```
ProcesarTurno(utterance) -> NpcReply:
    var intent = _m2.Classify(utterance.Text)              // AD9: sin mirar IsReady
    var cambio = _m4.Evaluate(intent, PhysicalAction.Ninguna)   // centinela de la otra dimension
    _m9.Notify(cambio)                                     // AD10: un solo sitio, antes de la rama

    var clin = _m15.Respond(utterance, intent)
    return clin.Handled
        ? clin.Reply                                       // tal cual, sin alterar
        : _m6.Generate(PersonalidadActual, _m4.Current, intent)

ProcesarAccion(accion) -> NpcReply?:
    var cambio = _m4.Evaluate(IntentResult.Unknown(0f), accion) // centinela; Classify NO se llama
    _m9.Notify(cambio)
    return null                                            // hablar responde a lo dicho, no a un gesto

DeclararTriaje(categoria):
    _declararTriaje(categoria)          // pass-through crudo: normalizar es de M9 (su AD10)

Progreso => _m9.Progress01              // derivado en lectura, nada cacheado
```

Tres precisiones sobre el turno:

- **`_m4.Current`, no `cambio.To`.** Son iguales por contrato (`Ports.cs:151`: *"`result.To`
  queda igual a `Current`"*), y la spec nombra `m4.Current`: se usa la forma que la spec nombra.
- **Una `Utterance` vacía no es caso especial.** `Classify("")` devuelve `Unknown`, el matcher de
  M15 no empareja, y el turno cae en M6, que produce una línea neutral. Filtrar entradas vacías es
  trabajo de M1, y ningún requisito lo pide aquí.
- **Las 3 `PhysicalAction` diferidas de M7 no son caso especial.** `ProcesarAccion` las trata como
  cualquier otra; no hay `switch` que reescribir cuando aterrice el seguimiento de M7.

## Data Flow

    ┌─ PR2, fuera de este diseño ─────────────────────────────────────────────┐
    │  UtteranceChannel ──► HarnessBehaviour.OnEnable/Subscribe               │
    │  PhysicalActionChannel ──►         │                                    │
    └────────────────────────────────────┼────────────────────────────────────┘
                                         │  Utterance / PhysicalAction
                                         │  (argumentos de metodo, ya deserializados)
                                         ▼
    ┌──────────────────────── SessionDirector (PR1) ──────────────────────────┐
    │  _casos[] _personalidades[] _rng(semilla) _ultimoCaso                   │
    │  CasoActual  PersonalidadActual                                         │
    │                                                                          │
    │  IniciarSesion ─► Arrancar ─┬─► _m4.Reset(personalidad)                 │
    │                             ├─► _asignarCaso(caso)        ─► M9 AssignCase
    │                             └─► _m15.AssignCase(caso, p)                │
    │                                   mismo `caso` para los dos sumideros   │
    │                                                                          │
    │  ProcesarTurno ─► _m2.Classify ─► _m4.Evaluate(i, Ninguna) ─► _m9.Notify│
    │                     └─► _m15.Respond ──Handled──► Reply                 │
    │                                      └─!Handled─► _m6.Generate ─► Reply │
    │                                                                          │
    │  ProcesarAccion ─► _m4.Evaluate(Unknown, accion) ─► _m9.Notify ─► null  │
    └──────────────────────────────────────┬──────────────────────────────────┘
                                           │  NpcReply / NpcReply?
                                           ▼
    ┌─ PR2, fuera de este diseño ─────────────────────────────────────────────┐
    │  if (reply.HasValue) NpcReplyChannel.Raise(reply.Value)                 │
    │       └─► NpcPresenterBehaviour (M8) y SessionLogBehaviour (M13),       │
    │           ambos auto-suscritos: un solo Raise alimenta a los dos        │
    └─────────────────────────────────────────────────────────────────────────┘

**Canales tocados por este diseño: cero.** `SessionDirector` es agnóstico del transporte por
construcción — su `.asmdef` no referencia `NpcAi.Core.Channels`, así que no puede suscribirse ni
publicar ni por accidente. `IntentResultChannel` y `ReceptivityChangeChannel` siguen huérfanos a
propósito: el director ya tiene el `IntentResult` y el `ReceptivityChange` en mano dentro del
mismo turno.

## File Inventory

| Archivo | Acción | Rol |
|---|---|---|
| `Runtime/Harness/NpcAi.Harness.asmdef` | Crear | `references: ["NpcAi.Core"]`, `noEngineReferences: true` (AD12) |
| `Runtime/Harness/SessionDirector.cs` | Crear | Único tipo público del módulo: selección, arranque, turno, enrutado |
| `Tests/EditMode/Harness/NpcAi.Harness.Tests.asmdef` | Crear | + `NpcAi.ClinicalResponse`, `NpcAi.Presentation`, TestRunner, `nunit.framework.dll` |
| `Tests/EditMode/Harness/EspiasDeArnes.cs` | Crear | Los cinco espías locales `internal sealed` (AD11) |
| `Tests/EditMode/Harness/SessionDirectorTurnoTests.cs` | Crear | Centinelas, silencio de la acción, enrutado (6 escenarios) |
| `Tests/EditMode/Harness/SessionDirectorSesionTests.cs` | Crear | Los 3 efectos del arranque y la selección por semilla (3 escenarios) |
| `Tests/EditMode/Harness/SessionDirectorFronterasTests.cs` | Crear | `Play`, `RegisterRedFlag`, construcción sin escena (3 escenarios) |
| `openspec/specs/banco-de-pruebas-m11/spec.md` | Crear al archivar | Promoción de la spec delta |
| `Runtime/Core/`, `Runtime/CoreChannels/`, todo otro `Runtime/<Modulo>/`, `Data/` | Sin cambio | Contrato congelado; M11 solo consume (reglas duras 1 y 2) |
| `Samples~/Harness/`, `Docs/MODULES.md` | Sin cambio en PR1 | Son PR2 |

Cada archivo nuevo necesita su `.meta` **en el mismo commit**: es el defecto que M7 tuvo que
corregir en `chore(m7): agregar .meta faltantes de los archivos de PR2`.

Un solo archivo para los cinco espías (y no cinco de ~20 líneas con sus cinco `.meta`) porque no
son dobles de módulo: no viven en `Runtime/<Modulo>/Fakes/`, no heredan ninguna gemela de
contrato y son instrumentos de esta suite. Mismo criterio que la decisión D3 de
`IntentClassifierContract`.

## Testing Strategy

Todo EditMode, sin escena, sin VR, sin audio, sin `MonoBehaviour`. `strict_tdd: true`: cada
escenario entra en RED antes de que exista la línea que lo pone en verde.

Los cinco espías locales (`internal sealed`, en `EspiasDeArnes.cs`):

| Espía | Puerto | Qué graba / cómo se comporta |
|---|---|---|
| `EspiaClasificador` | `IIntentClassifier` | Cuenta `Classify` y guarda el último texto; devuelve un `IntentResult` fijo programable |
| `EspiaReceptividad` | `IReceptivityEngine` | Graba la lista de `(IntentResult, PhysicalAction)` de cada `Evaluate` y la `PersonalityId` de cada `Reset`; mueve `Current` a `Receptivo` ante cualquier acción distinta de `Ninguna`, para que el escenario de `Acercarse` sea satisfacible tal como está escrito (AD11) |
| `EspiaDialogo` | `IDialogueGenerator` | Cuenta `Generate`, graba `(PersonalityId, Receptivity, IntentResult)` y devuelve un `NpcReply` marcado e identificable |
| `EspiaObjetivo` | `IScenarioObjective` | Graba cada `ReceptivityChange`; **además** expone `AssignCase(ClinicalCaseId)` y `RegisterRedFlag(int)` propios con sus contadores, para poder cablearlo como delegado y contar lo que la spec exige contar |
| `EspiaClinico` | `IClinicalResponder` | Graba el `(ClinicalCaseId, PersonalityId)` de `AssignCase` y devuelve una `ClinicalResponse` programable |

Trazabilidad escenario → prueba (7 requisitos, **12 escenarios**):

| # | Escenario de la spec | Prueba propuesta | Dobles |
|---|---|---|---|
| 1 | Solo llega Utterance | `SoloUtterance_evalua_con_PhysicalAction_Ninguna` | `EspiaReceptividad`: último `Evaluate` con `action == Ninguna` |
| 2 | Solo llega PhysicalAction | `SoloPhysicalAction_evalua_con_IntentResult_Unknown` | `EspiaReceptividad` (`intent.Intent == Desconocida`, `Confidence == 0`) + `EspiaClasificador.Invocaciones == 0` |
| 3 | La acción mueve receptividad | `AccionFisica_mueve_receptividad` | `EspiaReceptividad`: `Current` pasa de `Neutral` a `Receptivo` con `ProcesarAccion(Acercarse)` |
| 4 | La acción no produce habla | `AccionFisica_no_produce_respuesta_hablada` | Retorno `null`; `EspiaClinico` y `EspiaDialogo` con 0 invocaciones |
| 5 | Turno clínico manejado | `Turno_clinico_manejado_no_llama_Generate` | `ScriptedClinicalResponder` real + `IniciarSesion(new ClinicalCaseId("caso-01"), p)` y `Utterance("desde cuando le duele", ...)`: `ClinicalFactMatcher` empareja `inicio_sintoma` (subconjunto de palabras, verificado), así que `Handled == true`. Se afirma igualdad exacta con `Reply.Text` y `EspiaDialogo.Invocaciones == 0` |
| 6 | Turno social (no clínico) | `Turno_social_llama_Generate_con_receptividad_actual` | Mismo `ScriptedClinicalResponder` con `Utterance("hola futbol", ...)` (ningún hecho empareja ⇒ `NoAplica`); `EspiaDialogo` afirma `receptivity == m4.Current` y `personality == PersonalidadActual`, y que el retorno es su `NpcReply` marcado |
| 7 | Los tres efectos con el mismo caso | `IniciarSesion_dispara_Reset_AssignCase_M9_AssignCase_M15_con_mismo_caso` | `EspiaReceptividad` + `EspiaObjetivo` (como puerto **y** como delegado) + `EspiaClinico`: los tres invocados y `objetivo.UltimoCaso == clinico.UltimoCaso == caso` |
| 8 | Misma semilla y catálogo repiten el par | `MismaSemillaYCatalogo_elige_mismo_par` | Dos directores con `semilla: 1234` y el mismo catálogo de 3 casos: `CasoActual` y `PersonalidadActual` iguales tras `IniciarSesion()`. Se repite en bucle de ~20 sesiones para fijar la **secuencia** completa, no solo la primera |
| 9 | No se repite el caso anterior | `SesionNueva_no_repite_caso_anterior` | `IniciarSesion(caso-01, p)` fija `_ultimoCaso` (AD8), luego `IniciarSesion()` ⇒ `CasoActual != caso-01`; en bucle sobre varias semillas. Caso borde aparte: catálogo de **un** caso ⇒ `IniciarSesion()` no lanza ni cuelga y repite (AD7) |
| 10 | Nunca reproduce directamente | `NuncaLlama_Play` | `RecordingNpcPresenter` real: `HasPlayed == false` tras una sesión completa, **más** una aserción estructural por reflexión: ningún constructor público de `SessionDirector` declara un parámetro asignable a `INpcPresenter`. La reflexión es la parte que puede fallar de verdad (ver Open Questions) |
| 11 | Nunca registra banderas rojas | `NuncaLlama_RegisterRedFlag` | `EspiaObjetivo.BanderasRegistradas == 0` tras una sesión completa de turnos y acciones |
| 12 | Instanciación sin escena | `Construible_y_operativo_sin_escena_Unity` | Los cinco puertos como dobles deterministas; `Assert.DoesNotThrow` sobre `ProcesarTurno`/`ProcesarAccion` **antes** de cualquier `IniciarSesion`, que es el estado no inicializado que los contratos de M0 ya cubren (AD9) |

Pruebas de diseño que la spec no nombra, se agregan y se declaran: constructor con puerto nulo,
delegado nulo, catálogo nulo y catálogo vacío lanzan (AD3); `DeclararTriaje` hace pass-through
crudo al delegado; `Progreso` refleja `_m9.Progress01`.

**Orden TDD**: primero el arranque y la selección (son la precondición de todo lo demás), luego
los centinelas, luego el enrutado, y las fronteras al final.

**Compuerta humana**: Unity Test Runner > EditMode > Run All en verde, con el total registrado por
el usuario en `apply-progress.md`. **No aplica compuerta física en PR1**: sin escena, sin VR, sin
audio. Las dos compuertas humanas de escena y Quest 3 son de PR2.

## Threat Matrix

N/A — no hay enrutamiento de shell, subproceso, automatización de VCS/PR, clasificación de
archivos ejecutables ni integración de procesos. El "enrutado" de este cambio es una expresión
condicional en proceso entre dos llamadas a interfaz.

## Migration / Rollout

Sin migración. Todo es aditivo y M11 es la **hoja** del grafo: ningún ensamblado referencia
`NpcAi.Harness`. Revertir los commits de PR1 borra `Runtime/Harness/` y `Tests/EditMode/Harness/`
y deja M11 en el estado "solo README" que ya está en `main`. Sin `Data/` nuevo, sin asset nuevo,
sin dependencia nueva en `package.json` (la entrada del sample ya existía), sin cambio de
`Contract.Version`, sin `.asmdef` ajeno modificado. PR2 apila sobre PR1 y puede revertirse solo:
PR1 en verde y M11 sin escena es un estado coherente, no roto.

## Open Questions

- [ ] **El catálogo es inyectado, no hardcodeado** — confirmado y hecho explícito. `SessionDirector`
      no menciona `caso-01`, `caso-02` ni `caso-03` en ninguna línea, ni lee `Data/Cases/`. Los
      `IReadOnlyList<ClinicalCaseId>` y `IReadOnlyList<PersonalityId>` los arma el compositor: en
      PR2, `HarnessBehaviour` desde campos `[SerializeField]`; en las pruebas, literales en memoria.
      Agregar un cuarto caso es un archivo JSON más y una entrada más en el Inspector, cero líneas
      de código — que es exactamente lo que exige `rules.design` de `openspec/config.yaml` ("lo
      variable es dato, no código").
- [ ] **`DeclararTriaje(string)` no tiene ningún requisito en la spec.** Está en el In Scope de la
      propuesta y este diseño lo incluye (es un delegado y una línea), pero las 7 exigencias
      formales no lo mencionan, así que ninguna prueba suya es trazable a la spec. Mismo estatus
      que `RedFlagCount` en el diseño de M9. Decidir si se agrega un requisito en el addendum de
      PR2 o se acepta como detalle. Motivo de incluirlo ya: si se difiere, PR2 tendría que
      modificar `Runtime/Harness/SessionDirector.cs`, y PR2 está pensado como shell + escena + docs.
- [ ] **Dos escenarios de la spec no son satisfacibles con el doble que nombran** y este diseño se
      desvía con evidencia (AD11): (a) "un `ScriptedScenarioObjective` espía que cuenta llamadas a
      `RegisterRedFlag`" — esa clase es `sealed` y **no tiene** `RegisterRedFlag`; (b) "un
      `RecordingNpcPresenter` espía **en la cadena de dobles**" — el director no tiene ninguna
      costura para un `INpcPresenter`, así que nada puede inyectarlo en la cadena. Se propone
      corregir la redacción de ambas cláusulas `Dado` en la spec ("un espía local que cuenta..."
      y "un `RecordingNpcPresenter` construido pero sin costura donde inyectarlo, más una
      aserción estructural sobre el constructor"). Es edición de dos líneas y no cambia ningún
      comportamiento exigido.
- [ ] **La spec declara 7 requisitos y 12 escenarios**, no 13 (conteo verificado uno por uno). Si
      `sdd-verify` espera 13, el desajuste está en el recuento de la orquestación, no en la spec.
- [ ] **La no-repetición del caso no sobrevive a reconstruir el director** (AD8). Dos ejecuciones
      distintas del Editor pueden abrir con el mismo caso. Persistirlo exigiría que M11 escriba
      estado (¿`PlayerPrefs`? ¿M13?), fuera de alcance. Se documenta como límite conocido.
- [ ] **Constructor de 10 parámetros** (AD2). Se acepta como firma de raíz de composición, pero si
      al escribir `HarnessBehaviour` en PR2 la construcción resulta ilegible, la salida limpia es
      un POCO de configuración agregado de forma aditiva, sin tocar la lógica del director.
- [ ] **El folder stale `openspec/changes/2026-09-09-m11-armado-sesion/` ya no existe** (glob
      completo de `openspec/changes/**/*.md`): alguien lo borró, como recomendaba la propuesta. El
      riesgo "que alguien reanude el cambio stale" está cerrado a nivel de sistema de archivos y no
      necesita mitigación adicional en `tasks.md`.
