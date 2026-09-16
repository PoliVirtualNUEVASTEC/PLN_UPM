# Design: M7 — Entrada física VR (fuente real de `IPhysicalActionSource`)

## Technical Approach

Un adaptador nuevo detrás del puerto `IPhysicalActionSource`, ya congelado. Tres capas, espejo
exacto de M1 (`OfflineSpeechToText` / `IAudioCapture` / `SpeechToTextBehaviour`):

| Capa | Tipo | `UnityEngine` |
|---|---|---|
| Núcleo | `SpatialPhysicalActionSource : IPhysicalActionSource` | No |
| Costura de hardware | `ISpatialSampler` + `SpatialSample` (+ `Vec3`) | No |
| Implementación real de la costura | `UnitySpatialSampler` | Sí |
| Envoltura | `VrInputBehaviour`, `TouchZoneRelay` | Sí |

El núcleo recibe `SpatialSample` ya digeridas (posiciones, frente de cabeza, pulso de contacto,
delta de tiempo), corre tres detectores con histéresis y antirrebote, y levanta `OnAction`. No
conoce `Camera`, `Transform`, `Collider` ni `Time`.

### Unidad de módulo y grafo de referencias

    NpcAi.VrInput -> [NpcAi.Core, NpcAi.Core.Channels]   (sin cambio)

`Runtime/VrInput/NpcAi.VrInput.asmdef` **no se modifica**: ya referencia exactamente esos dos
ensamblados y `noEngineReferences: false` (lo necesitan las envolturas). `InternalsVisibleTo` es
atributo de C#, no campo de asmdef: va en `Runtime/VrInput/Properties/AssemblyInfo.cs`
(patrón de M1 y M8). `Tests/EditMode/VrInput/NpcAi.VrInput.Tests.asmdef` tampoco cambia: ya
referencia `NpcAi.Core`, `NpcAi.Core.Tests` y `NpcAi.VrInput`.

`Data/VrInput/*.asset` vive fuera de todo `.asmdef`: son datos, no código (regla dura 7).

## Architecture Decisions

| # | Decisión | Elección | Alternativas rechazadas | Razón |
|---|---|---|---|---|
| AD1 | Tipos vectoriales del núcleo | `Vec3` propio del módulo (`readonly struct`, `float X,Y,Z`) dentro de `SpatialSample.cs` | `UnityEngine.Vector3`; `System.Numerics.Vector3` | `Vector3` arrastraría `UnityEngine` al núcleo y mataría la prueba sin escena; `System.Numerics` agrega una dependencia de framework para tres floats y dos operaciones. El núcleo usa `System.Math`, nunca `Mathf` |
| AD2 | Forma de la costura | `bool LeerMuestra(out SpatialSample)` — una muestra por tick | `int LeerDisponibles(SpatialSample[] destino)` (copia literal de `IAudioCapture`) | No hay productor en segundo plano ni búfer que drenar: un `Update` produce exactamente una pose. Un arreglo de destino sería ceremonia sin lote que llenar |
| AD3 | Base de tiempo | El `SpatialSample` transporta `DeltaSegundos`; `Bombear()` no recibe parámetros | `Bombear(float delta)`; que el núcleo lea `Time.deltaTime` | La muestra queda autodescriptiva: `ScriptedSpatialSampler` guiona trayectoria **y** tiempo en un solo objeto, y el núcleo no puede tocar `UnityEngine.Time` ni por accidente |
| AD4 | Vía de levantamiento | Un único `private bool Levantar(PhysicalAction)` que solo rechaza `Ninguna` e invoca; los enfriamientos viven en cada detector | Poner el enfriamiento genérico dentro de `Levantar` | Si `Levantar` estrangulara, `EmitirParaPrueba` heredaría el estrangulamiento y `PhysicalActionSourceContract` dependería del estado del reloj. Mantenerla tonta es lo que deja el punto de extensión limpio para el cambio posterior de mandos |
| AD5 | Costura de prueba | `internal bool EmitirParaPrueba(PhysicalAction)` delega en `Levantar` | Hacer `Levantar` público; modificar `PhysicalActionSourceContract` | Espejo literal de `OfflineSpeechToText.EmitirParaPrueba`. La base emite `GestoCalma`, que ningún detector de esta entrega produce: la gemela pasa el contrato **sin tocar la clase base** |
| AD6 | Sin bomba de hilo principal | Ninguna. Todo corre síncrono dentro de `Update()` | `QueuedMainThreadPump` (M1/M8) | M1 la necesita porque Vosk finaliza en un hilo trabajador y M8 porque sintetiza en `Task.Run`. M7 no tiene ni un hilo de fondo: muestreo, detección y `Raise` ocurren en el mismo frame y en el hilo principal. Copiarla agregaría un frame de latencia y código muerto |
| AD7 | Pulso de contacto | `TouchZoneRelay` latchea en `OnTriggerEnter` y el muestreador **jala** con `ConsumirPulso()` (lee y limpia) | Que el relay empuje al `VrInputBehaviour` por referencia inversa; `UnityEvent` en el Inspector | El resto de la costura es *pull*; jalar mantiene una sola dirección de dependencia (relay no conoce al behaviour) y garantiza a lo sumo un pulso por tick aunque entren varios colliders |
| AD8 | Primera clasificación de distancia | La primera muestra con objetivo **siembra** el estado (por el punto medio de la banda) y **no** emite | Sembrar en `Lejos` y emitir `Acercarse` al primer cruce real; emitir en la primera clasificación | Aparecer ya cerca del paciente no es acercarse. Sembrar por punto medio evita además el caso en que la primera muestra cae dentro de la banda y el estado quedaría indeterminado para siempre |
| AD9 | Suscripción en `OnEnable`/`OnDisable` | Lo que se suscribe y desuscribe es `SpatialPhysicalActionSource.OnAction`; el `PhysicalActionChannel` es destino de `Raise`, no de `Subscribe` | Suscribir el behaviour al canal | M7 **publica**; suscribirse a su propio canal sería un lazo. La fuga real que el criterio de la propuesta previene es el delegado del núcleo sobreviviendo al cambio de escena, y eso es exactamente lo que cubren `OnEnable`/`OnDisable` |
| AD10 | Degradación sin cámara u objetivo | `UnitySpatialSampler.LeerMuestra` devuelve `false`; `Bombear()` es no-op y ningún temporizador avanza | Lanzar en `Awake` (como hace M1 con su config faltante) | La propuesta lo fija como riesgo alto: no hay rig XR en ninguna escena todavía. No-op silencioso, sin `Debug.Log` (regla dura de `config.yaml`) |

## Interfaces / Contracts

Sin cambios en `NpcAi.Core`. Superficie nueva, toda interna a `NpcAi.VrInput`:

```csharp
// Runtime/VrInput/SpatialSample.cs
internal readonly struct Vec3
{
    public readonly float X, Y, Z;
    public Vec3(float x, float y, float z);
    public static Vec3 operator -(Vec3 a, Vec3 b);
    public float Longitud { get; }                       // System.Math.Sqrt
    public static float Distancia(Vec3 a, Vec3 b);
    /// <summary>Angulo en grados entre dos direcciones. -1 si alguna es degenerada.</summary>
    public static float AnguloEnGrados(Vec3 a, Vec3 b);  // Acos(clamp(dot/(|a||b|), -1, 1)) * 180/PI
}

internal readonly struct SpatialSample
{
    public readonly Vec3  PosicionDeCabeza;
    public readonly Vec3  FrenteDeCabeza;      // no se exige normalizado: el angulo normaliza
    public readonly Vec3  PosicionDelObjetivo;
    public readonly bool  HayObjetivo;         // false = no se resolvio NPC este tick
    public readonly bool  PulsoDeContacto;     // true solo en el tick del OnTriggerEnter
    public readonly float DeltaSegundos;       // AD3

    public SpatialSample(Vec3 posicionDeCabeza, Vec3 frenteDeCabeza, Vec3 posicionDelObjetivo,
                         bool hayObjetivo, bool pulsoDeContacto, float deltaSegundos);
}

// Runtime/VrInput/ISpatialSampler.cs — espejo de IAudioCapture (M1)
internal interface ISpatialSampler
{
    bool EstaActivo { get; }
    void Iniciar();
    /// <summary>Una muestra por tick. false = nada que muestrear (sin camara, sin objetivo).</summary>
    bool LeerMuestra(out SpatialSample muestra);
    void Detener();
}

// Runtime/VrInput/SpatialPhysicalActionSource.cs
public sealed class SpatialPhysicalActionSource : IPhysicalActionSource
{
    public event Action<PhysicalAction> OnAction;

    /// <summary>Sujeto sin cablear: solo la via guardada. Lo usa la gemela de contrato.</summary>
    public SpatialPhysicalActionSource();

    /// <summary>internal: expone tipos internos del modulo (regla dura 3), como en M1.</summary>
    internal SpatialPhysicalActionSource(VrInputSettings config, ISpatialSampler muestreador);

    internal void Bombear();                               // jala una muestra y corre detectores
    internal void ProcesarMuestra(in SpatialSample m);     // detectores; costura directa de prueba
    internal bool EmitirParaPrueba(PhysicalAction accion); // AD5 -> Levantar
    internal EstadoDeDistancia DistanciaActual { get; }    // observabilidad de prueba
    internal bool MirandoAlObjetivo { get; }               // observabilidad de prueba

    private bool Levantar(PhysicalAction accion)           // AD4: unica via de emision
    {
        if (accion == PhysicalAction.Ninguna) return false;
        OnAction?.Invoke(accion);
        return true;
    }
}

internal enum EstadoDeDistancia { Indeterminado = 0, Cerca = 1, Lejos = 2 }
```

`ProcesarMuestra` corre los tres detectores en orden fijo: mirada, distancia, contacto. Un tick
puede levantar más de una acción (encarar y cruzar el umbral a la vez es legítimo).

### Detector 1 — `ContactoVisual` (mirada de cabeza con permanencia y liberación)

```
_mirando        : bool  = false     // latch: ya se emitio para esta mirada
_segundosEnCono : float = 0

if (!m.HayObjetivo) { _segundosEnCono = 0; _mirando = false; return; }

angulo = Vec3.AnguloEnGrados(m.FrenteDeCabeza, m.PosicionDelObjetivo - m.PosicionDeCabeza)
if (angulo < 0) return                       // direccion degenerada: no toca el estado

if (!_mirando)
{
    if (angulo <= cfg.GradosDelConoDeMirada)
    {
        _segundosEnCono += m.DeltaSegundos
        if (_segundosEnCono >= cfg.SegundosDePermanenciaDeMirada)
        {
            _mirando = true
            Levantar(PhysicalAction.ContactoVisual)      // una sola vez por mirada
        }
    }
    else _segundosEnCono = 0                 // salir del cono reinicia la permanencia
}
else if (angulo > cfg.GradosDeLiberacionDeMirada)   // cono ANCHO de salida (histeresis)
{
    _mirando = false
    _segundosEnCono = 0
}
```

Acumular **antes** de comparar hace que `SegundosDePermanenciaDeMirada = 0` emita en el primer
tick dentro del cono. Con `>=`, la igualdad exacta emite. No hace falta enfriamiento de
re-emisión: reentrar exige salir del cono ancho **y** volver a cumplir la permanencia completa.

### Detector 2 — `Acercarse` / `Alejarse` (banda de histéresis)

```
_distancia : EstadoDeDistancia = Indeterminado

if (!m.HayObjetivo) return                   // conserva el estado: perder el objetivo no es alejarse

d = Vec3.Distancia(m.PosicionDeCabeza, m.PosicionDelObjetivo)

if (_distancia == Indeterminado)             // AD8: sembrar sin emitir
{
    _distancia = d <= (cfg.MetrosParaAcercarse + cfg.MetrosParaAlejarse) * 0.5f ? Cerca : Lejos
    return
}

nuevo = _distancia
if      (d <= cfg.MetrosParaAcercarse) nuevo = Cerca
else if (d >= cfg.MetrosParaAlejarse)  nuevo = Lejos
// dentro de la banda: nuevo == _distancia -> no hay transicion

if (nuevo == _distancia) return
_distancia = nuevo
Levantar(nuevo == Cerca ? PhysicalAction.Acercarse : PhysicalAction.Alejarse)
```

Oscilar sobre `MetrosParaAcercarse` no puede emitir dos veces: para volver a emitir `Acercarse`
hay que cruzar antes `MetrosParaAlejarse` completo. `OnValidate` garantiza banda no degenerada.

### Detector 3 — `TocarPaciente` (contacto con antirrebote)

```
_segundosDesdeContacto : float = float.MaxValue   // el primer contacto siempre emite

if (_segundosDesdeContacto < cfg.SegundosDeEnfriamientoDeContacto)
    _segundosDesdeContacto += m.DeltaSegundos     // acumulacion saturada, sin desbordar

if (!m.PulsoDeContacto) return
if (_segundosDesdeContacto < cfg.SegundosDeEnfriamientoDeContacto) return  // rebote

_segundosDesdeContacto = 0
Levantar(PhysicalAction.TocarPaciente)
```

`TouchZoneRelay.OnTriggerEnter(Collider otro)` filtra por `LayerMask` y levanta `_pendiente`;
`ConsumirPulso()` devuelve y limpia (AD7). Varios colliders entrando en el mismo frame producen
un solo pulso.

## Configuración (`Data/VrInput/*.asset`)

`VrInputSettingsAsset : ScriptableObject` con `[CreateAssetMenu]`, campos públicos planos e
`internal void OnValidate()` con `Mathf.Clamp` — copia de la forma de `SpeechSettingsAsset`.
`internal VrInputSettings ToSettings()` produce el snapshot POCO sin `UnityEngine`.

| Campo | Tipo | Defecto | Rango en `OnValidate` |
|---|---|---|---|
| `GradosDelConoDeMirada` | `float` | `20f` | `Clamp(1f, 90f)` |
| `GradosDeLiberacionDeMirada` | `float` | `30f` | `Clamp(1f, 120f)`, luego `Max(GradosDelConoDeMirada)` |
| `SegundosDePermanenciaDeMirada` | `float` | `0.6f` | `Clamp(0f, 5f)` |
| `MetrosParaAcercarse` | `float` | `1.2f` | `Clamp(0.1f, 10f)` |
| `MetrosParaAlejarse` | `float` | `2.0f` | `Clamp(0.2f, 20f)`, luego `Max(MetrosParaAcercarse + 0.1f)` |
| `SegundosDeEnfriamientoDeContacto` | `float` | `1.0f` | `Clamp(0f, 10f)` |

Los dos `Max` cruzados son lo que impide una banda invertida o de ancho cero, que degeneraría la
histéresis en una ráfaga. Ninguno de estos números aparece en código de detección: el núcleo solo
lee `cfg`.

## Data Flow

    Escena anfitriona (M11)                    NpcAi.VrInput
    ───────────────────────                    ─────────────
    Camera (HMD) ──┐
    Transform NPC ─┼──► UnitySpatialSampler ──► SpatialSample ──┐
    Collider ──► TouchZoneRelay ──┘  (ConsumirPulso)            │
                                                                ▼
                                          SpatialPhysicalActionSource (C# puro)
                                          ┌──────────────────────────────────┐
                                          │ 1. mirada  (cono + permanencia)  │
                                          │ 2. distancia (banda de histeresis)│
                                          │ 3. contacto (pulso + enfriamiento)│
                                          │         └─► Levantar(...)         │
                                          └──────────────────────────────────┘
                                                                │ OnAction
                                                                ▼
                                          VrInputBehaviour.PublicarEnCanal
                                                                │ Raise
                                                                ▼
                                          PhysicalActionChannel (asset, M0)
                                                                │ Subscribe (Inspector)
                                                                ▼
                                          M11 ──► IReceptivityEngine.Evaluate(intent, action)  (M4)

M7 nunca llama a M4: el canal es la única conexión, cableada por Inspector en la escena
anfitriona (regla dura 3).

### `VrInputBehaviour` — ciclo de vida

```csharp
[SerializeField] internal VrInputSettingsAsset  _configuracion;
[SerializeField] internal PhysicalActionChannel _canal;
[SerializeField] private  Camera                _camaraDelHmd;
[SerializeField] private  Transform             _objetivo;
[SerializeField] internal TouchZoneRelay        _zonaDeContacto;

private void Awake()
{
    var cfg = _configuracion != null ? _configuracion.ToSettings() : new VrInputSettings();
    _muestreador = _muestreadorDePrueba
                   ?? new UnitySpatialSampler(_camaraDelHmd, _objetivo, _zonaDeContacto);
    _sujeto = new SpatialPhysicalActionSource(cfg, _muestreador);
}
private void OnEnable()  { _sujeto.OnAction += PublicarEnCanal; _muestreador.Iniciar(); }  // AD9
private void OnDisable() { _sujeto.OnAction -= PublicarEnCanal; _muestreador.Detener(); }
private void Update()    => _sujeto?.Bombear();                                            // AD6
private void PublicarEnCanal(PhysicalAction a) { if (_canal != null) _canal.Raise(a); }

internal void CablearParaPrueba(ISpatialSampler muestreador = null)   // patron de M8
{ _muestreadorDePrueba = muestreador; Awake(); OnEnable(); }
internal void DescablearParaPrueba() => OnDisable();
internal void BombearParaPrueba()    => Update();
```

`Update()` no lee nada de Unity por sí mismo: `UnitySpatialSampler` es quien lee
`_camaraDelHmd.transform.position/forward`, `_objetivo.position`, `Time.deltaTime` y
`_zonaDeContacto.ConsumirPulso()`, y los empaqueta en un `SpatialSample`. Sin cámara u objetivo
asignados devuelve `false` y el frame es un no-op (AD10).

## File Inventory

| Archivo | Rol |
|---|---|
| `Runtime/VrInput/Properties/AssemblyInfo.cs` | `InternalsVisibleTo("NpcAi.VrInput.Tests")` |
| `Runtime/VrInput/SpatialSample.cs` | `Vec3` + `SpatialSample`, sin `UnityEngine` |
| `Runtime/VrInput/ISpatialSampler.cs` | Costura interna de hardware de M7 |
| `Runtime/VrInput/SpatialPhysicalActionSource.cs` | Núcleo: los 3 detectores y `Levantar` |
| `Runtime/VrInput/UnitySpatialSampler.cs` | Implementación real de la costura (espejo de `MicrophoneAudioCapture`) |
| `Runtime/VrInput/VrInputBehaviour.cs` | Raíz de composición: `Awake`/`OnEnable`/`Update`/`OnDisable` + canal |
| `Runtime/VrInput/TouchZoneRelay.cs` | `OnTriggerEnter` → pulso latcheado, `ConsumirPulso()` |
| `Runtime/VrInput/Config/VrInputSettings.cs` | Snapshot POCO que consume el núcleo |
| `Runtime/VrInput/Config/VrInputSettingsAsset.cs` | `ScriptableObject` + `OnValidate` |
| `Runtime/VrInput/Fakes/ScriptedSpatialSampler.cs` | Doble determinista de la costura: cola de `SpatialSample` |
| `Runtime/VrInput/Fakes/ScriptedPhysicalActionSource.cs` | **Sin cambio**: sigue siendo el doble del puerto |
| `Runtime/VrInput/NpcAi.VrInput.asmdef` | **Sin cambio**: referencias ya correctas |
| `Data/VrInput/README.md` | Qué significa cada umbral y cómo se calibra en Quest |
| `Data/VrInput/<escenario>.asset` | Umbrales por escenario |
| `Tests/EditMode/VrInput/SpatialPhysicalActionSourceTests.cs` | Gemela de `PhysicalActionSourceContract` |
| `Tests/EditMode/VrInput/DeteccionDeMiradaTests.cs` | Permanencia, latch, liberación |
| `Tests/EditMode/VrInput/HisteresisDeDistanciaTests.cs` | Banda, siembra, una emisión por cruce |
| `Tests/EditMode/VrInput/DeteccionDeContactoTests.cs` | Pulso y enfriamiento |
| `Tests/EditMode/VrInput/VrInputBehaviourWiringTests.cs` | Ciclo de vida, canal, no-op sin cámara |
| `Tests/EditMode/VrInput/VrInputSettingsAssetTests.cs` | Clamps de `OnValidate`, incluidos los cruzados |
| `openspec/specs/entrada-fisica-vr-m7/spec.md` | Primera spec formal de M7 |
| `Docs/MODULES.md` | Sección M7: cierra "solo doble" para 4 de 7, nombra las 3 pendientes |

## Testing Strategy

Todo EditMode, sin escena, sin cámara, sin headset. `ScriptedSpatialSampler` expone
`Encolar(SpatialSample)` y `LeerMuestra` desencola en orden; la prueba construye la trayectoria
como una secuencia de muestras sintéticas y decide el delta de cada una (AD3).

| Archivo | Qué fija | Cómo |
|---|---|---|
| `SpatialPhysicalActionSourceTests` | Los 4 casos del puerto, incluido `GestoCalma` | `CreateSubject()` = constructor sin parámetros; `EmitTestAction` = `((SpatialPhysicalActionSource)subject).EmitirParaPrueba(action)`. **La clase base no se toca** |
| `DeteccionDeMiradaTests` | Mirar menos que la permanencia no emite; sostenerla emite **una** vez; salir del cono ancho y volver emite de nuevo; perder el objetivo reinicia | `ProcesarMuestra` directo con ángulos calculados |
| `HisteresisDeDistanciaTests` | La primera muestra no emite (AD8); oscilar sobre el umbral cercano emite **una** sola vez; cruzar la banda completa emite `Alejarse` | Trayectoria de N muestras sobre el umbral, contar emisiones |
| `DeteccionDeContactoTests` | Primer pulso emite; segundo pulso dentro del enfriamiento no; pasado el enfriamiento sí | Pulsos separados por deltas controlados |
| `VrInputBehaviourWiringTests` | `CablearParaPrueba(scripted)` → `BombearParaPrueba()` publica en el canal; `DescablearParaPrueba()` deja de publicar (sin fuga); sin cámara ni objetivo no lanza y no publica | Espejo de `NpcPresenterBehaviourTests` (M8, no `...WiringTests` como dice la propuesta) y de `OfflineSpeechToTextWiringTests` (M1): forzar `Awake`/`OnEnable` sin escena viva |
| `VrInputSettingsAssetTests` | `OnValidate` acota cada campo y corrige banda invertida y cono de liberación menor que el de entrada | Llamada directa a `OnValidate` (ensamblado amigo) |

Separación deliberada, igual que M1: pruebas de lógica del núcleo (`ProcesarMuestra`, sin
`MonoBehaviour`) frente a pruebas de cableado (fuerzan los callbacks del ciclo de vida).

**Compuerta humana**: la confirmación en Quest 3 físico de las 4 acciones la ejecuta y la registra
el usuario en `apply-progress.md`, no el agente. Es el único criterio que EditMode no cubre.

## Threat Matrix

N/A — no hay enrutamiento, shell, subproceso, automatización de VCS/PR, clasificación de archivos
ejecutables ni integración de procesos. El cambio es lógica de detección en proceso más lectura de
`Transform` de la escena.

## Migration / Rollout

Sin migración. Todo es aditivo sobre un puerto existente; `ScriptedPhysicalActionSource` no se
toca y sigue disponible para M11. Sin dependencias nuevas en `package.json` y sin cambio de
`Contract.Version`. Revertir los commits deja M7 en el estado "solo doble" actual; los assets de
`Data/VrInput/` quedan huérfanos y se borran sin efecto sobre otro módulo.

## Open Questions

- [ ] Valores por defecto reales de cono, permanencia y umbrales de distancia. Los de la tabla son
      puntos de partida razonables, no medidos: se calibran en la compuerta humana en Quest 3 con
      un paciente a escala real, y el resultado se escribe en `Data/VrInput/<escenario>.asset`, no
      en código. **No bloquea el diseño ni las pruebas** (las pruebas construyen su propia config).
- [ ] ¿`TouchZoneRelay` debe distinguir mano izquierda de derecha, o basta con "algo del usuario
      tocó al paciente"? Esta entrega asume lo segundo (`PhysicalAction` no tiene lateralidad).
      Si el cambio posterior de mandos necesita distinguirlas, es una decisión de ese cambio.
- [ ] ¿El `LayerMask` por defecto de `TouchZoneRelay` debe ser restrictivo o permisivo? Depende de
      las capas que defina el proyecto anfitrión, que hoy no existen; se resuelve en M11.
