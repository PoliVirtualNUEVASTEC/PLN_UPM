# Design: Formalizar M4 — Motor de receptividad

## Technical Approach

"Documentar y fijar lo que existe". El motor de M4 no se redisena: se le agrega su
especificacion versionada. Este cambio mueve el comportamiento de M4 desde el codigo y las
memorias de Engram hacia una spec OpenSpec con escenarios ejecutables mapeados a pruebas ya en
verde. **Cero lineas de runtime, cero pruebas nuevas.**

### Unidad de modulo y grafo de referencias

Un solo modulo: **M4 = `NpcAi.Receptivity`**, con su sub-ensamblado Unity
`NpcAi.Receptivity.Unity` (adaptador ScriptableObject) y su ensamblado de prueba
`NpcAi.Receptivity.Tests`. El grafo se confirma sin cambios:

    NpcAi.Receptivity        -> [NpcAi.Core]                          (noEngineReferences: true)
    NpcAi.Receptivity.Unity  -> [NpcAi.Core, NpcAi.Receptivity]      (noEngineReferences: false)
    NpcAi.Receptivity.Tests  -> [NpcAi.Core, NpcAi.Core.Tests, NpcAi.Receptivity,
                                 NpcAi.Receptivity.Unity, UnityEngine.TestRunner,
                                 UnityEditor.TestRunner] + nunit.framework.dll

`NpcAi.Receptivity` referencia unicamente `NpcAi.Core` (regla 3 de CLAUDE.md). No usa
`NpcAi.Core.Channels`: M4 no se cablea por Inspector, publica su `ReceptivityChange` por retorno
y el canal `ReceptivityChangeChannel` vive en M0. Ningun otro modulo gana o pierde referencias.

## Architecture Decisions

| # | Decision | Eleccion | Alternativas rechazadas | Razon |
|---|---|---|---|---|
| AD1 | Fuente de verdad del estado | El puntaje acumulado es lo unico que se guarda; `Current` siempre es `EstadoPara(puntaje)` contra los umbrales del perfil | Guardar `Current` (o un `EstadoInicial`) como campo independiente del puntaje | Un estado derivado no puede desincronizarse del puntaje. Se elimino `ReceptivityProfile.EstadoInicial` por redundante: el ctor paso de 8 a 7 parametros. El doble ya funcionaba asi |
| AD2 | Garantia de monotonia del contrato | `Blindar(intent, delta)`: con `SolicitudAgresiva` un delta `> 0` se vuelve `0`; con `Empatia` un delta `< 0` se vuelve `0`. Se aplica siempre, despues del calculo del perfil | Confiar en que los numeros de cada perfil respeten los signos; validar los perfiles al construirlos | El contrato de M0 exige que la agresion sostenida no suba y la empatia sostenida no baje. `Blindar` lo hace **estructuralmente imposible** de violar por un perfil mal tuneado. El tuning de M5 no puede romper el contrato |
| AD3 | Pureza del motor | `ReceptivityEngine`, `ReceptivityProfile` y `ReceptivityProfileCatalog` son C# puro; `NpcAi.Receptivity.asmdef` tiene `noEngineReferences: true` | Motor o perfil como `ScriptableObject` en el mismo ensamblado | CLAUDE.md regla 6: la logica de M4 no usa `UnityEngine`, se prueba en milisegundos y demuestra su tabla de transiciones sin abrir Unity. `noEngineReferences: true` lo vuelve infalsificable |
| AD4 | Dueno del esquema de perfil | La clase de esquema (`ReceptivityProfile`) vive en la carpeta de M4; M5 solo aporta 4 `.asset` | Poner el esquema en M5 | M4 no puede referenciar el ensamblado de M5 (regla 3) y M5 no escribe clases nuevas ("lo variable es dato"). Ver [[m4-decision-esquema-perfil]] |
| AD5 | Costura para M5 | Catalogo inyectable por constructor + fabrica `Standard()` con 4 personalidades placeholder (`grosero`, `histerico`, `introvertido`, `empatico`) | Personalidades fijas dentro del motor | Cuando M5 publique sus datos reales, un adaptador arma el mismo `ReceptivityProfileCatalog` desde los `.asset` sin tocar el motor. Probado por `El_motor_toma_el_catalogo_que_se_le_inyecta` |
| AD6 | `PersonalityId` desconocido | `PerfilDe` cae en `ReceptivityProfile.Default`, sin lanzar | Lanzar; devolver `null` | El motor nunca opera sin perfil. Coherente con la normalizacion de `PersonalityId` que fija `contrato-nucleo-m0` |
| AD7 | Acotamiento del puntaje | `Saturar`: el puntaje se mantiene en `[-LimitePuntaje, +LimitePuntaje]` (valor absoluto). `LimitePuntaje` se guarda tal cual llega | Sin cota; cota global fija | Un puntaje acotado hace la tabla de transiciones predecible y demostrable. El limite es por perfil (contrato del productor, no invariante del contenedor) |
| AD8 | Tratamiento del `Tone` | El motor real suma `DeltaPorTono(intent.Tone)` al delta bruto. El doble lo ignora | Que el doble tambien pondere el tono | Asimetria deliberada: el doble es un stand-in minimo y determinista; el contrato de M0 no exige sensibilidad al tono. Se documenta para que ningun consumidor asuma paridad de `Score` |
| AD9 | Vocabulario de `ReasonCode` | `ReceptivityEngine.Razon` y `ScriptedReceptivityEngine.RazonDe` emiten los mismos 15 codigos, en el mismo orden (Intent -> PhysicalAction -> `delta == 0 ? SIN_CAMBIO_RELEVANTE : AJUSTE_MENOR`) | Doble con un subconjunto de codigos | Un consumidor (M6/M8) no debe depender de que implementacion hay detras del puerto. El `Score` puede diferir; la etiqueta de diagnostico no. `RazonParityTests` (56 casos) lo fija (paso 6) |
| AD10 | Colision de nombres | Calificar `Core.Receptivity` en todo el codigo de M4 y sus pruebas | `using` alias; renombrar el enum | El enum `NpcAi.Core.Receptivity` colisiona con el namespace `NpcAi.Receptivity`. Renombrar el enum seria ruptura de contrato de M0 |
| AD11 | Validacion de aceptacion | Verde EditMode verificado por humano en el Test Runner de Unity 6 | Compuerta de CI | No hay CI en el repo; el repo ES el paquete. Ningun agente ejecuta Unity: cada verde es compuerta humana |

## Data Flow

### Flujo runtime (una evaluacion)

    M2  IIntentClassifier  --IntentResult-->  |
                                              |  ReceptivityEngine.Evaluate
    M7  IPhysicalActionSource --PhysicalAction->|
                                              v
        DeltaBruto (DeltaPorIntencion + DeltaPorTono + DeltaPorAccion, del perfil)
                                              v
        Blindar  (agresion nunca suma, empatia nunca resta)   <-- AD2
                                              v
        Saturar  ([-LimitePuntaje, +LimitePuntaje])            <-- AD7
                                              v
        EstadoPara (puntaje vs UmbralReceptivo / UmbralNoReceptivo)  <-- AD1
                                              v
        ReceptivityChange(From, To, Score, Razon(...))  -->  M6 IDialogueGenerator / M8 INpcPresenter
                                                             (via ReceptivityChangeChannel de M0)

### Costura de M5 (construccion del catalogo)

    Data/Personalities/*.asset (M5, 4 archivos)  -->  ReceptivityProfileAsset[]  (SO, NpcAi.Receptivity.Unity)
                                          |  ReceptivityProfileAsset.BuildCatalog(assets)   (estatico)
                                          |     usa ToProfile() de cada asset
                                          v
                                     ReceptivityProfileCatalog  -->  new ReceptivityEngine(catalogo)

El motor no conoce `ReceptivityProfileAsset`: recibe un `ReceptivityProfileCatalog` ya armado.

## File Inventory (existente, sin cambios en este cambio)

| Archivo | Rol |
|---|---|
| `Runtime/Receptivity/ReceptivityEngine.cs` | Motor real; `Blindar()` es la garantia estructural del contrato |
| `Runtime/Receptivity/ReceptivityProfile.cs` | Contenedor de datos del perfil (ctor de 7 params tras quitar `EstadoInicial`) |
| `Runtime/Receptivity/ReceptivityProfileCatalog.cs` | Catalogo inyectable + `Standard()` con las 4 personalidades placeholder |
| `Runtime/Receptivity/Unity/ReceptivityProfileAsset.cs` | Unico archivo de M4 que toca `UnityEngine`; `ToProfile()` (instancia) y `BuildCatalog(assets)` (estatico) |
| `Runtime/Receptivity/Fakes/ScriptedReceptivityEngine.cs` | El doble; determinista, ignora el tono, mismo vocabulario de `ReasonCode` |
| `Runtime/Receptivity/NpcAi.Receptivity.asmdef` | `noEngineReferences: true` |
| `Runtime/Receptivity/Unity/NpcAi.Receptivity.Unity.asmdef` | `noEngineReferences: false`; `[NpcAi.Core, NpcAi.Receptivity]` |
| `Tests/EditMode/Receptivity/*` | 5 archivos de prueba; ~105 pruebas EditMode |

## Interfaces / Contracts

Sin firmas nuevas en `NpcAi.Core`. `ReceptivityEngine` implementa `IReceptivityEngine` tal como
lo fija `contrato-nucleo-m0`. Superficie publica de M4 (ya existente):

```csharp
// NpcAi.Receptivity
public sealed class ReceptivityEngine : IReceptivityEngine
{
    public ReceptivityEngine();                                  // usa Standard()
    public ReceptivityEngine(ReceptivityProfileCatalog catalogo); // costura de M5
    // Current / Reset / Evaluate vienen del puerto
}

public sealed class ReceptivityProfile
{
    public ReceptivityProfile(int umbralReceptivo, int umbralNoReceptivo, int limitePuntaje,
        int puntajeInicial, IReadOnlyDictionary<Intent,int> porIntencion,
        IReadOnlyDictionary<Tone,int> porTono, IReadOnlyDictionary<PhysicalAction,int> porAccion);
    public int DeltaPorIntencion(Intent intent);  // 0 si no puntua
    public int DeltaPorTono(Tone tone);           // 0 si no puntua
    public int DeltaPorAccion(PhysicalAction a);  // 0 si no puntua
    public static ReceptivityProfile Default { get; }
}

public sealed class ReceptivityProfileCatalog
{
    public ReceptivityProfileCatalog(IReadOnlyDictionary<PersonalityId,ReceptivityProfile> perfiles);
    public IEnumerable<PersonalityId> Personalidades { get; }
    public ReceptivityProfile PerfilDe(PersonalityId personality);  // Default si no existe
    public static ReceptivityProfileCatalog Standard();
}

// NpcAi.Receptivity.Unity
// Campos editables en Inspector: personalityId, umbrales, puntajeInicial,
// porIntencion / porTono / porAccion (arreglos de structs [Serializable]).
public sealed class ReceptivityProfileAsset : ScriptableObject
{
    public PersonalityId PersonalityId { get; }                       // id normalizado
    public ReceptivityProfile ToProfile();                            // proyeccion 1:1, sin logica
    public static ReceptivityProfileCatalog BuildCatalog(             // costura de M5
        IEnumerable<ReceptivityProfileAsset> assets);                 //   ignora null; ultimo gana si repiten id
}
```

## Testing Strategy

Este cambio agrega **cero** pruebas. La spec se apoya en lo que ya corre:

| Archivo | Pruebas | Que fija |
|---|---|---|
| `Tests/EditMode/Core/ReceptivityEngineContract.cs` | 9 | Contrato de puerto (heredado): Neutral hasta Reset, Reset determinista/idempotente, `Evaluate` reporta From/To/razon, monotonia, entrada neutra no mueve |
| `Tests/EditMode/Receptivity/ReceptivityEngineTests.cs` | 10 | Tabla de transiciones por personalidad, estado inicial por perfil, tono decisivo, saturacion, catalogo inyectado, `ReasonCode` |
| `Tests/EditMode/Receptivity/ReceptivityProfileTests.cs` | 4 | Contenedor de datos: accesores devuelven 0 sin tabla, `Default` |
| `Tests/EditMode/Receptivity/ReceptivityProfileCatalogTests.cs` | 8 | `PersonalityId` -> perfil, fallback a `Default`, `Standard()` |
| `Tests/EditMode/Receptivity/ReceptivityProfileAssetTests.cs` | 7 | `ToProfile()` / `BuildCatalog()` del adaptador SO |
| `Tests/EditMode/Receptivity/RazonParityTests.cs` | 56 | Paridad de `ReasonCode` doble vs motor real, `Intent` x `PhysicalAction` |

Ejecucion: Test Runner de Unity 6 (EditMode > Run All). Sin escena, sin VR, sin ningun otro
modulo real.

## Threat Matrix

N/A — no hay frontera de enrutamiento, shell, subproceso, automatizacion VCS/PR ni clasificacion
de archivos ejecutables. M4 es una maquina de estados C# pura mas una proyeccion de solo lectura
desde un `ScriptableObject`. Este cambio ademas solo escribe Markdown bajo `openspec/`.

## Migration / Rollout

No se requiere migracion: no hay cambio de codigo y `IReceptivityEngine` no cambia. **No es
cambio de contrato**, no aplica ventana del lunes. Integracion como cambio propio con auto-merge
y checklist "Antes de mergear".

Rollback: `git revert` del commit del cambio elimina los 4 artefactos; si ya se archivo, tambien
retira `openspec/specs/receptividad-m4/`. Ningun modulo consumidor necesita accion.

## Open Questions

- [ ] Cuando M5 publique sus 4 `.asset` reales, el tuning fino de los perfiles entra como cambio
      propio de M5. Definir si ese cambio agrega escenarios a `receptividad-m4` (numeros
      concretos por personalidad) o si `receptividad-m4` se queda solo con los invariantes
      estructurales y M5 documenta sus datos aparte. Preferencia actual: lo segundo.
- [ ] `ReceptivityProfileAsset` es el unico tipo de M4 con `UnityEngine`. Se deja plegado en
      `receptividad-m4` (Requirement "Adaptador ScriptableObject aislado"), no como capacidad
      propia: es una sola clase de proyeccion sin logica.
