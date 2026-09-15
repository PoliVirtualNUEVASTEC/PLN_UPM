# Design: M7 — Entrada física VR

## Modulo y frontera

Un solo módulo lógico, dos ensamblados (mismo patrón que M1 y M13):

    Runtime/VrInput/PhysicalActionSource.cs       -> NpcAi.VrInput          (references: [NpcAi.Core, NpcAi.Core.Channels])
    Runtime/VrInput/Unity/VrInputBehaviour.cs     -> NpcAi.VrInput.Unity    (references: [NpcAi.Core, NpcAi.Core.Channels, NpcAi.VrInput,
                                                                              Unity.XR.Interaction.Toolkit, Unity.XR.CoreUtils])

`NpcAi.VrInput.asmdef` ya existe con `references: ["NpcAi.Core", "NpcAi.Core.Channels"]` — no
cambia. El sub-ensamblado `NpcAi.VrInput.Unity` es nuevo, y es el único punto del módulo que
referencia XR Interaction Toolkit: el núcleo (`PhysicalActionSource`) no sabe que existe un
headset.

## Technical Approach

Dos capas, igual espíritu que M1 (captura vs. decisión) pero sin necesidad de un hilo
trabajador: XR Interaction Toolkit ya entrega sus eventos en el hilo principal, y no hay
trabajo pesado de CPU que justifique moverlo.

| Capa | Tipo | Responsabilidad | Hilo |
|---|---|---|---|
| Señal cruda | `VrInputBehaviour : MonoBehaviour` | lee el rig XR cada `Update()`/evento de XRI (mirada, zonas, grab, rayo, botón) | Principal |
| Decisión | `PhysicalActionSource : IPhysicalActionSource` | anti-rebote, umbrales de tiempo, transforma señal cruda en `PhysicalAction` | Principal (llamado sincrónicamente desde `VrInputBehaviour`) |

## Mapeo de señal física → `PhysicalAction`

Decisión del usuario 2026-09-15: Quest 2/3 con controles físicos únicamente, gestos simples con
XR Interaction Toolkit estándar (no hand-tracking, no eye-tracking, no reconocimiento de
posturas libres).

| `PhysicalAction` | Señal física real | Mecanismo XRI | Disparo |
|---|---|---|---|
| `ContactoVisual` | Proxy de mirada: el forward de la cámara del XR Origin apunta hacia el collider de la cabeza del NPC, dentro de un ángulo de cono configurable | Raycast/`Vector3.Angle` manual en `VrInputBehaviour` (no requiere un componente XRI específico) | `ReportarMirada(true, dt)` acumula tiempo; al cruzar `MinSegundosDeMirada` dispara una vez, con cooldown antes de poder volver a disparar |
| `Acercarse` | El jugador entra a una `ZonaCercana` (esfera/cápsula de trigger) alrededor del NPC | `Collider` en modo Trigger + `OnTriggerEnter` sobre el XR Origin | `ReportarZonaCercana(true)` dispara una vez en la transición falso→verdadero |
| `Alejarse` | El jugador sale de la misma `ZonaCercana` | `OnTriggerExit` | `ReportarZonaCercana(false)` dispara una vez en la transición verdadero→falso |
| `TocarPaciente` | El collider de la mano/controlador entra en una `ZonaDeContacto` sobre el cuerpo del NPC | `XRDirectInteractor`/collider del controlador + `OnTriggerEnter` sobre esa zona | `ReportarContactoPaciente(true)` dispara una vez por entrada (histéresis: no vuelve a disparar hasta salir y volver a entrar) |
| `EntregarObjeto` | El jugador suelta un objeto agarrado dentro de un socket cerca de las manos del NPC | `XRGrabInteractable` + `XRSocketInteractor` (evento `selectEntered` del socket) | `ReportarEntrega()` — evento discreto, sin estado, dispara siempre que ocurre |
| `SenalarPantalla` | El rayo del controlador apunta a un objeto marcado (tag/layer `Pantalla`) y se presiona el gatillo | `XRRayInteractor` + su evento `selectEntered`/`activated` sobre un `Interactable` en esa capa | `ReportarSenalado()` — evento discreto en el gatillo, no continuo mientras el rayo permanece encima |
| `GestoCalma` | Botón primario del controlador presionado mientras el controlador está orientado hacia el NPC (palma/frente del control apuntando al NPC) | Input System (acción del botón) + `Vector3.Angle` entre el forward del controlador y la dirección al NPC | `ReportarGestoCalma()` — evento discreto en el flanco de subida del botón, solo si el ángulo está dentro del umbral en ese instante |

**Por qué eventos discretos para unos y transición para otros.** `Acercarse`/`Alejarse`/
`TocarPaciente` son estados de posición: tiene sentido que disparen en la transición (entrar/
salir), no cada frame que el jugador se queda quieto adentro — eso evitaría inundar
`ReceptivityEngine` de eventos idénticos. `EntregarObjeto`/`SenalarPantalla`/`GestoCalma` son
gestos intencionales de un instante: no tienen "estado sostenido" natural, así que son eventos
discretos con su propio anti-rebote (un cooldown mínimo entre dos disparos del mismo tipo, para
que un jugador nervioso presionando el gatillo repetido no dispare 10 `SenalarPantalla` por
segundo).

## Architecture Decisions

| # | Decisión | Elección | Alternativas rechazadas | Razón |
|---|---|---|---|---|
| AD1 | Dónde vive la decisión de "esto es un evento" | Núcleo puro (`PhysicalActionSource`), recibe señales ya interpretadas | Decidir todo dentro de `VrInputBehaviour` | Mismo argumento que M1 AD2: la lógica de anti-rebote/umbrales se prueba en milisegundos sin headset si no depende de `UnityEngine`. Migrar de controles físicos a hand-tracking después solo cambia quién llama a `Reportar*`, no la lógica de disparo |
| AD2 | `ContactoVisual` sin eye-tracking | Proxy: forward de la cámara + ángulo hacia la cabeza del NPC, sostenido un mínimo de tiempo | Esperar a tener Quest Pro; omitir `ContactoVisual` de esta entrega | Decisión del usuario: el hardware disponible es Quest 2/3. El proxy es la aproximación estándar en VR sin eye-tracking (mirar-con-la-cabeza) y dispara la misma semántica de "el paciente le prestó atención" que necesita `ReceptivityEngine` |
| AD3 | Gestos por controles + colisiones, no manos libres | XRI estándar: triggers, grab+socket, rayo, botón+ángulo | Hand-tracking con reconocimiento de posturas | Decisión del usuario 2026-09-15: confiabilidad y tiempo de implementación sobre realismo. Es una ampliación documentada, no bloqueante |
| AD4 | Transición vs. evento discreto por tipo de acción | Ver tabla de mapeo arriba | Todo como evento discreto por frame | Evita inundar a M4/M6 de eventos idénticos mientras el jugador permanece quieto en una zona; coincide con cómo M4 espera recibir acciones (una por ocurrencia, no por frame) |
| AD5 | Umbral de mirada y cooldowns como dato, no constantes de código | `VrInputSettings` (ScriptableObject, `Data/VrInput/`), snapshot POCO al núcleo — mismo patrón que `SpeechSettingsAsset` de M1 | Constantes `const` en `PhysicalActionSource` | Regla dura 7 del repo ("lo variable es dato, no código"): ajustar el ángulo de mirada o el cooldown de gestos tras probar en el Quest no debe tocar una clase |
| AD6 | Sin hilo trabajador | Todo corre en el hilo principal, llamado sincrónicamente desde `Update()`/callbacks de XRI | Topología de hilos como M1 | No hay trabajo de CPU pesado (ni audio, ni motor de reconocimiento): raycasts y comparaciones de ángulo por frame son triviales para el presupuesto de Quest. Una topología de hilos aquí sería complejidad sin beneficio |
| AD7 | Paquetes XR como `dependencies` reales | Agregar `com.unity.xr.interaction.toolkit` y `com.unity.xr.openxr` a `package.json` | Dejarlos como *keyword* nada más (como le pasó a Sentis en M2 antes de corregirse) | El código de `VrInputBehaviour` los referencia directamente; declarados como dependencia, cualquiera que clone el paquete UPM los resuelve automáticamente en vez de encontrar un error de compilación críptico |

## Data Flow

    [XR Origin del jugador, en la escena de M11]
              │
              ▼
    VrInputBehaviour.Update() / callbacks de XRI
       │ mirada (raycast+angulo)   │ zona cercana (trigger)   │ contacto (trigger)   │ socket   │ rayo+gatillo   │ boton+angulo
       ▼                           ▼                          ▼                      ▼          ▼                ▼
    ReportarMirada(bool,dt)   ReportarZonaCercana(bool)  ReportarContactoPaciente(bool)  ReportarEntrega()  ReportarSenalado()  ReportarGestoCalma()
                                          │
                                          ▼
                            PhysicalActionSource (nucleo puro)
                            anti-rebote + umbrales de VrInputSettings
                                          │
                                          ▼
                            OnAction(PhysicalAction) ──> PhysicalActionChannel.Raise (via VrInputBehaviour)

## Interfaces / Contracts

```csharp
// Runtime/VrInput/PhysicalActionSource.cs — nucleo puro, sin UnityEngine
public sealed class PhysicalActionSource : IPhysicalActionSource
{
    public PhysicalActionSource(VrInputSettings ajustes);

    public event Action<PhysicalAction> OnAction;

    // Metodos "Reportar*": los llama VrInputBehaviour con la senal ya interpretada.
    public void ReportarMirada(bool mirandoAlNpc, float deltaSegundos);
    public void ReportarZonaCercana(bool dentro);
    public void ReportarContactoPaciente(bool dentro);
    public void ReportarEntrega();
    public void ReportarSenalado();
    public void ReportarGestoCalma();
}

// Runtime/VrInput/VrInputSettings.cs — snapshot inmutable, sin UnityEngine (mismo patron
// que SpeechSettings de M1)
public readonly struct VrInputSettings
{
    public readonly float MinSegundosDeMirada;      // default 1.0
    public readonly float CooldownSegundos;          // minimo entre dos disparos del mismo tipo, default 0.5
}
```

Costuras `internal` para pruebas de anti-rebote fino no son necesarias aquí: a diferencia de
M1, no hay generación/hilo que revalidar — los `Reportar*` públicos ya son la superficie
completa que hace falta probar.

## `Data/VrInput/` — forma del ScriptableObject

`Runtime/VrInput/Config/VrInputSettingsAsset.cs` (clase; `Data/` no compila código). El asset
vive en `Data/VrInput/Default.asset`. `[CreateAssetMenu(menuName = "NPC AI/Entrada VR/Configuracion")]`.
`ToSettings()` devuelve el snapshot POCO.

| Campo | Tipo | Valor por defecto | Rango |
|---|---|---|---|
| `MinSegundosDeMirada` | `float` | `1.0` | 0.1–5 |
| `CooldownSegundos` | `float` | `0.5` | 0–3 |
| `AnguloDeMiradaGrados` | `float` (usado en `VrInputBehaviour`, no en el núcleo) | `20` | 5–60 |
| `AnguloDeGestoCalmaGrados` | `float` (usado en `VrInputBehaviour`) | `30` | 5–60 |

## Empaquetado de dependencias XR — primero en el repo

```json
"dependencies": {
  "com.unity.xr.interaction.toolkit": "3.0.7",
  "com.unity.xr.openxr": "1.12.1"
}
```

- Versiones tentativas compatibles con Unity 6000.x; **confirmar contra la versión exacta del
  Editor de la usuaria al instalar** (Package Manager las ajusta si hace falta).
- Instalar y habilitar el proveedor OpenXR (Project Settings → XR Plug-in Management →
  Android/Windows → OpenXR) es un paso manual de la usuaria, no algo que este agente ejecute.
- Es el primer módulo del repo con dependencia de paquetes XR: no hay precedente que copiar.
  Si el Editor de la usuaria no tiene el módulo de Android instalado, el build a Quest fallará
  por separado de este cambio — fuera de alcance de M7.

## File Inventory

| Archivo | Rol |
|---|---|
| `Runtime/VrInput/PhysicalActionSource.cs` | Núcleo puro, implementación real del puerto |
| `Runtime/VrInput/VrInputSettings.cs` | Snapshot POCO |
| `Runtime/VrInput/Config/VrInputSettingsAsset.cs` | ScriptableObject |
| `Runtime/VrInput/Unity/NpcAi.VrInput.Unity.asmdef` | Sub-ensamblado nuevo, referencia XRI |
| `Runtime/VrInput/Unity/VrInputBehaviour.cs` | Envoltura real: lee el rig, llama `Reportar*`, publica en `PhysicalActionChannel` |
| `Data/VrInput/README.md`, `Data/VrInput/Default.asset` | Dato (regla dura 7) |
| `Tests/EditMode/VrInput/PhysicalActionSourceTests.cs` | Hereda `PhysicalActionSourceContract` + anti-rebote propio |
| `package.json` | Agrega `dependencies` XRI + OpenXR |

## Testing Strategy

| Capa | Qué se prueba | Cómo |
|---|---|---|
| Contrato | `PhysicalActionSourceTests : PhysicalActionSourceContract` | `CreateSubject() => new PhysicalActionSource(VrInputSettings.Default)`; `EmitTestAction` llama el `Reportar*` que corresponda a cada `PhysicalAction` de prueba. Las 4 pruebas heredadas deben pasar sin cambios |
| Anti-rebote | `ContactoVisual` no dispara antes de `MinSegundosDeMirada` | `ReportarMirada(true, 0.3f)` tres veces (0.9s acumulados) → 0 eventos; una cuarta llamada que cruza 1.0s → 1 evento |
| Anti-rebote | `ContactoVisual` no se repite mientras se sigue mirando | tras el primer disparo, seguir llamando `ReportarMirada(true, dt)` → sin nuevos eventos hasta que se reporte `false` y se vuelva a acumular |
| Transición | `Acercarse`/`Alejarse` disparan una vez por cruce | `ReportarZonaCercana(true)` → 1 `Acercarse`; repetir `true` → 0 eventos nuevos; `ReportarZonaCercana(false)` → 1 `Alejarse` |
| Transición | `TocarPaciente` con histéresis | `ReportarContactoPaciente(true)` → 1 evento; `true` de nuevo sin pasar por `false` → 0 eventos nuevos |
| Discreto | `EntregarObjeto`/`SenalarPantalla`/`GestoCalma` respetan el cooldown | dos `ReportarEntrega()` seguidos dentro de `CooldownSegundos` → 1 evento; tras esperar el cooldown → 2do evento sí se emite |
| Dato | `VrInputSettingsAsset.OnValidate` acota los rangos | `ScriptableObject.CreateInstance<VrInputSettingsAsset>()` en EditMode |
| Dispositivo | El mapeo controles→acción funciona en un Quest real | **Compuerta humana.** Sin CI ni hardware en fases de agente; validación manual de la usuaria, registrada como tal, no como prueba automatizada |

## Migration / Rollout

Sin migración. `Contract.Version` no sube. Despliegue sugerido en 2 PR (`sdd-tasks` confirma):
(1) núcleo puro + doble sin cambios + pruebas — compilable y verde sin ningún paquete XR nuevo
instalado; (2) `NpcAi.VrInput.Unity` + dependencias XR + `Data/VrInput/`. El corte 1 deja el
módulo funcional para cualquiera que integre contra el doble mientras la usuaria instala y
configura OpenXR para el corte 2.

## Open Questions

- [ ] Versión exacta de `com.unity.xr.interaction.toolkit`/`com.unity.xr.openxr` a fijar:
      depende de la versión del Editor instalada; se confirma al instalar en el corte 2.
- [ ] ¿La "zona cercana" y la "zona de contacto" del NPC son colliders fijos en el prefab del
      NPC (decisión de M11) o M7 expone un componente reutilizable
      (`NpcProximityZones.cs`) para que M11 lo agregue? Tentativo: componente reutilizable,
      para no acoplar M7 a un prefab específico que todavía no existe. Se confirma en el corte 2.
- [ ] ¿Qué tag/layer identifica "Pantalla" para `SenalarPantalla`? Se decide con M11 cuando
      arme la escena; M7 solo necesita el nombre acordado.
