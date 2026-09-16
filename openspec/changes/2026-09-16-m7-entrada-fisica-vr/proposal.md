# Propuesta: M7 — Entrada física VR (fuente real de `IPhysicalActionSource`)

## Intent

`Docs/MODULES.md` documenta hoy a M7 como **"solo doble"**: `Runtime/VrInput/` contiene
únicamente `Fakes/ScriptedPhysicalActionSource.cs`, un `Emit(PhysicalAction)` manual para que una
prueba o el banco de pruebas dispare acciones a mano. No lee el headset, no lee los controles, no
mide distancias. `Specs formales: no existe`.

M7 es la **otra entrada** del pipeline: M1 aporta lo que el usuario *dice*, M7 aporta lo que el
usuario *hace con el cuerpo*. M4 (`IReceptivityEngine`) ya consume `PhysicalAction` para mover la
receptividad del NPC, pero hoy esa señal solo llega si alguien la escribe a mano. Sin M7 real, el
lado no verbal de la simulación de triaje no existe en el headset.

Este cambio entrega la primera fuente real de `IPhysicalActionSource` para **4 de las 7** acciones
del vocabulario de M0: `ContactoVisual`, `Acercarse`, `Alejarse` y `TocarPaciente` — las que se
derivan de la pose del HMD y de un collider, sin ningún paquete de interacción XR. Las otras 3
(`EntregarObjeto`, `SenalarPantalla`, `GestoCalma`) quedan explícitamente fuera y se rastrean como
cambio SDD posterior. **Esta propuesta no cierra M7 por completo y no debe leerse así**: cierra el
estado "solo doble" para la mitad medible del vocabulario.

Decisiones ya fijadas antes de este cambio (confirmadas con el usuario el 2026-09-16): ver
"Decisiones del usuario" al final.

## Scope

### In Scope

- `Runtime/VrInput/SpatialPhysicalActionSource.cs` — núcleo `IPhysicalActionSource` en C# puro,
  construible y probable **sin sesión XR, sin escena y sin headset** (misma propiedad que
  `OfflineSpeechToText` en M1): recibe muestras espaciales, aplica mirada/distancia/contacto con
  histéresis y antirrebote, y levanta `OnAction`.
- `Runtime/VrInput/ISpatialSampler.cs` + `SpatialSample` (POCO) — costura interna de hardware de
  M7, espejo de `IAudioCapture` en M1. **No va a `NpcAi.Core`.** Transporta pose de cabeza,
  pose del NPC objetivo y el pulso de contacto, sin tipos de `UnityEngine` en el núcleo.
- `Runtime/VrInput/Fakes/ScriptedSpatialSampler.cs` — doble determinista de esa costura interna
  (espejo de `Fakes/SilentSpeechSynthesizer.cs` en M8), para guionar trayectorias en EditMode.
- `Runtime/VrInput/VrInputBehaviour.cs` — `MonoBehaviour` raíz de composición: lee
  `Camera`/`Transform` reales de la escena anfitriona, bombea el núcleo en `Update` y publica por
  `PhysicalActionChannel` (ya existe en `Runtime/CoreChannels/`). Espejo de
  `SpeechToTextBehaviour` y `NpcPresenterBehaviour`.
- `Runtime/VrInput/TouchZoneRelay.cs` — `MonoBehaviour` sobre el collider del paciente que
  convierte `OnTriggerEnter` en el pulso de contacto del muestreador.
- `Runtime/VrInput/Config/VrInputSettings.cs` (snapshot POCO) +
  `VrInputSettingsAsset : ScriptableObject` con `[CreateAssetMenu]` y `OnValidate` que acota
  rangos — copia exacta de la forma de `SpeechSettingsAsset`.
- `Data/VrInput/<escenario>.asset` + `Data/VrInput/README.md` — ángulo del cono de mirada,
  permanencia y liberación de la mirada, umbrales de acercarse/alejarse, banda de histéresis y
  enfriamiento de re-emisión. **Todo dato, ninguna constante en código** (regla dura 7).
- `Tests/EditMode/VrInput/SpatialPhysicalActionSourceTests.cs : PhysicalActionSourceContract`
  (hereda la misma base que ya pasa `ScriptedPhysicalActionSource`, sin modificarla) más pruebas
  propias de mirada, histéresis, contacto y cableado del behaviour.
- `openspec/specs/entrada-fisica-vr-m7/spec.md` — primera spec formal de M7.
- `Docs/MODULES.md` (sección M7), cerrando "solo doble" **para las 4 acciones en alcance** y
  dejando escritas las 3 pendientes.

### Out of Scope

- **`PhysicalAction` e `IPhysicalActionSource` no cambian.** Contrato v1 congelado. Este cambio es
  puro motor/adaptador, exactamente como M2 fue a `IIntentClassifier` y M8 a `INpcPresenter`. **No
  es cambio de M0.**
- **`ScriptedPhysicalActionSource` (el doble) no cambia.** Sigue existiendo, sigue pasando
  `PhysicalActionSourceContract`, y M11 lo sigue usando donde prefiera no depender del headset —
  igual que `NluIntentClassifier` quedó documentado como respaldo en M2.
- **`EntregarObjeto`, `SenalarPantalla` y `GestoCalma`.** Las tres exigen agarrar, apuntar o
  gesticular con los mandos, y eso obliga a elegir antes un **paquete de interacción** (lo más
  probable, XR Interaction Toolkit con interactores de rayo/agarre ligados a los botones del
  control). Esa elección arrastra dependencias nuevas en el proyecto anfitrión, prefabs de
  interactor y un modelo de eventos propio: es una decisión de arquitectura completa, no un
  detector más. Meterla aquí duplicaría el tamaño del cambio y mezclaría dos decisiones
  independientes. Va como **cambio SDD aparte**, con esta entrega como base ya probada.
- **Seguimiento de manos (`com.unity.xr.hands`), reconocimiento de gestos y Meta Interaction
  SDK.** Descartados por decisión del usuario (Decisión 1).
- **Eye tracking real.** El Quest 3 confirmado no tiene sensor de mirada; `ContactoVisual` se
  aproxima con la cabeza (Decisión 2).
- **Rig de cámara XR, escena, prefabs y navegación**: son del **proyecto VR anfitrión**, y su
  cableado real es de M11, igual que M1, M2 y M8 difirieron ahí su composición en escena.
- **Cualquier otro módulo.** Un cambio toca un módulo (regla dura 1).

## Capabilities

### New Capabilities

- `entrada-fisica-vr-m7` (nuevo `openspec/specs/entrada-fisica-vr-m7/spec.md`): primera spec
  formal de M7. Formaliza lo que `PhysicalActionSourceContract.cs` ya fija (entrega al suscrito,
  nada tras desuscribirse, `Ninguna` no es evento, emitir sin suscriptores no lanza) más las
  garantías nuevas de esta implementación: permanencia y liberación de la mirada, histéresis de
  distancia sin oscilación en el umbral, contacto sin repetición por frame, y suscripción/
  desuscripción limpia del `PhysicalActionChannel`. Mismo patrón que `presentador-npc-m8`.

### Modified Capabilities

- Ninguna. `IPhysicalActionSource` (`contrato-nucleo-m0`) y `PhysicalActionChannel`
  (`canales-evento-nucleo-m0`) no cambian.

## Approach

**El seam ya existe y ya está probado**: `IPhysicalActionSource.OnAction` es la única superficie
que ve el resto del sistema, y `PhysicalActionChannel` ya está en `Runtime/CoreChannels/`.
`SpatialPhysicalActionSource` es un adaptador nuevo detrás de esa interfaz. Ningún otro módulo
cambia una línea.

**Capas (espejo de M1):**

| Capa | Tipo | Responsabilidad | Depende de UnityEngine |
|---|---|---|---|
| Núcleo | `SpatialPhysicalActionSource : IPhysicalActionSource` | detectores, histéresis, antirrebote, `OnAction` | No |
| Costura de hardware | `ISpatialSampler` / `ScriptedSpatialSampler` | entrega `SpatialSample` por tick | No (el doble) |
| Envoltura | `VrInputBehaviour`, `TouchZoneRelay` | `Camera`/`Transform`/`Collider` reales, bombeo, canal | Sí |

**Por qué el núcleo no toca `UnityEngine`**: es lo que permite probar las 4 acciones con
trayectorias sintéticas en milisegundos en EditMode, sin headset y sin escena — la misma razón por
la que M4 es C# puro. El vector y la distancia se representan con tipos propios del módulo en
`SpatialSample`, no con `Vector3`.

**Por qué las 3 detecciones y no otras**: `ContactoVisual` = ángulo entre el *forward* de la
cabeza y la dirección al NPC, por debajo de un cono configurable, sostenido una permanencia
mínima. `Acercarse`/`Alejarse` = distancia HMD↔NPC cruzando umbrales separados por una banda de
histéresis, para que caminar sobre el umbral no dispare una ráfaga de eventos. `TocarPaciente` =
entrada en un collider, con enfriamiento. Las tres salen de datos que la escena ya tiene; ninguna
necesita un paquete nuevo.

**Punto de extensión para las 3 acciones diferidas**: el núcleo expone una vía única y guardada de
levantamiento de acción (que rechaza `Ninguna`) que los detectores invocan. Esa misma vía es la
que enchufará el cambio posterior de interacción con mandos, y es la que permite que
`SpatialPhysicalActionSourceTests` satisfaga `PhysicalActionSourceContract` **sin modificar la
clase base** — la base emite `GestoCalma`, que ningún detector de esta entrega produce.

**Los umbrales son dato**: cono, permanencia, distancias, histéresis y enfriamiento viven en
`Data/VrInput/*.asset`. Ajustar un escenario es editar un asset, no recompilar (regla dura 7).

## Affected Areas

| Área | Impacto | Descripción |
|---|---|---|
| `Runtime/VrInput/SpatialPhysicalActionSource.cs` | Nuevo | Núcleo `IPhysicalActionSource`, C# puro |
| `Runtime/VrInput/ISpatialSampler.cs`, `SpatialSample.cs` | Nuevo | Costura interna de hardware de M7 |
| `Runtime/VrInput/Fakes/ScriptedSpatialSampler.cs` | Nuevo | Doble determinista de la costura interna |
| `Runtime/VrInput/VrInputBehaviour.cs`, `TouchZoneRelay.cs` | Nuevo | Envolturas MonoBehaviour + canal |
| `Runtime/VrInput/Config/` | Nuevo | `VrInputSettings` + `VrInputSettingsAsset` |
| `Data/VrInput/` | Nuevo | Config por escenario + README |
| `Tests/EditMode/VrInput/` | Modificado | Contrato heredado + pruebas de detección y cableado |
| `openspec/specs/entrada-fisica-vr-m7/spec.md` | Nuevo | Primera spec formal de M7 |
| `Docs/MODULES.md` | Modificado | Cerrar "solo doble" en la sección M7 (4 de 7) |
| `Runtime/VrInput/Fakes/ScriptedPhysicalActionSource.cs` | Sin cambio | Sigue siendo el doble del puerto |
| `Runtime/Core/`, `Runtime/CoreChannels/` | Sin cambio | Contrato v1 congelado |

## Risks

| Riesgo | Prob. | Mitigación |
|---|---|---|
| **No existe rig de cámara XR en ninguna escena**: `package.json` solo declara `com.unity.ai.inference`, y el proyecto anfitrión no declara paquetes XR | Alta | El núcleo y la costura no dependen de un rig: `VrInputBehaviour` recibe la cámara y el objetivo por Inspector y, si faltan, es no-op silencioso (sin `Debug.Log`). "¿Existe un rig en una escena real?" se difiere a M11/Harness, igual que M1, M2 y M8 difirieron su composición en escena |
| La aproximación cabeza≠ojos produce falsos `ContactoVisual` (el usuario mira de reojo, o encara al NPC sin atenderlo) | Media | El cono y la permanencia son dato ajustable por escenario; la compuerta humana en Quest físico es justamente donde se calibran. Queda documentado en la spec que `ContactoVisual` significa *encarar*, no *fijar la vista* |
| Ráfaga de eventos al caminar sobre el umbral de distancia | Media | Banda de histéresis explícita (umbral de entrada ≠ umbral de salida) más enfriamiento, y una prueba EditMode que camina la trayectoria sobre el umbral y afirma una sola emisión |
| `VrInputBehaviour` no se desuscribe del núcleo interno (`SpatialPhysicalActionSource.OnAction`) y fuga entre escenas | Media | Suscripción en `OnEnable` / desuscripción en `OnDisable` (nunca al `PhysicalActionChannel`, que solo recibe `Raise`), verificado por prueba de cableado (mismo patrón que `Tests/EditMode/Presentation/NpcPresenterBehaviourTests.cs`) |
| Entregar 4 de 7 acciones se lee como M7 terminado | Media | La propuesta, la spec y la sección de `Docs/MODULES.md` nombran las 3 pendientes de forma explícita y las atan al cambio SDD posterior de interacción con mandos |
| Sin CI ni runner headless, la confirmación en hardware depende de una persona | Alta | Criterios de éxito partidos en dos: lo automatizable en EditMode con transformaciones sintéticas, y una **compuerta humana** en Quest físico que ejecuta y registra el usuario en `apply-progress.md`, siguiendo el patrón ya establecido por M1 y M2 |

## Rollback Plan

Todo lo de M7 es aditivo sobre un puerto que ya existe; `ScriptedPhysicalActionSource` no se toca.
Revertir los commits de este cambio deja M7 en el estado "solo doble" que ya está en `main` — el
pipeline sigue funcionando (M11 usa el doble), porque ningún otro módulo depende de
`SpatialPhysicalActionSource` por nombre, solo de `IPhysicalActionSource`. No hay migración de
datos, no hay dependencia nueva en `package.json` y no hay cambio de `Contract.Version`. Los
assets de `Data/VrInput/` quedan huérfanos y se borran sin efecto sobre otro módulo.

## Dependencies

- `contrato-nucleo-m0`, archivado y estable: define `IPhysicalActionSource` y `PhysicalAction`. No
  se modifica.
- `canales-evento-nucleo-m0`: `PhysicalActionChannel` (`EventChannel<PhysicalAction>`) ya existe.
  No se modifica.
- Una `Camera` (el HMD) y un `Transform` de NPC del **proyecto anfitrión**, inyectados a
  `VrInputBehaviour` por Inspector; un `Collider` en el paciente para `TouchZoneRelay`.
- Ningún paquete nuevo: esta entrega **no** agrega XR Interaction Toolkit, XR Hands ni Meta SDK.
- Headset Meta Quest 3 físico para la compuerta humana final (no requerido para las pruebas
  EditMode).

## Success Criteria

- [ ] `SpatialPhysicalActionSource` pasa exactamente la misma batería `PhysicalActionSourceContract`
      que `ScriptedPhysicalActionSource`, **sin modificar la clase base**.
- [ ] El núcleo se construye y se prueba sin sesión XR, sin escena y sin headset: las pruebas de
      `ContactoVisual`, `Acercarse`, `Alejarse` y `TocarPaciente` corren con muestras sintéticas.
- [ ] Una trayectoria que oscila sobre el umbral de distancia produce **una** sola emisión, no una
      ráfaga (prueba de histéresis en verde).
- [ ] Mirar al NPC menos que la permanencia mínima **no** emite `ContactoVisual`; sostenerla sí, y
      una sola vez hasta liberar el cono.
- [ ] Ningún umbral está escrito en código: cono, permanencia, distancias, histéresis y
      enfriamiento se leen de `Data/VrInput/*.asset`, y `OnValidate` acota sus rangos.
- [ ] `VrInputBehaviour` se suscribe al evento `OnAction` de su `SpatialPhysicalActionSource`
      interno en `OnEnable` y se desuscribe en `OnDisable` (nunca se suscribe a
      `PhysicalActionChannel`, que es solo destino de `Raise`); una prueba lo verifica sin fugas.
      Sin `Camera` u objetivo asignados, no lanza.
- [ ] Ningún `Debug.Log` en el código de runtime del módulo.
- [ ] **Automatizable**: Unity Test Runner > EditMode > Run All en verde, con el total de pruebas
      registrado en `apply-progress.md`.
- [ ] **Compuerta humana (la ejecuta y la registra el usuario, no el agente)**: en Quest 3 físico,
      el usuario confirma que encarar al NPC emite `ContactoVisual`, que caminar hacia él y
      alejarse emiten `Acercarse`/`Alejarse` una vez por cruce, y que tocar al paciente emite
      `TocarPaciente`; deja el resultado escrito en `apply-progress.md`, atribuido a él.
- [ ] `openspec/specs/entrada-fisica-vr-m7/spec.md` existe y traza cada requisito a una prueba
      EditMode en verde o a la compuerta humana.
- [ ] `Docs/MODULES.md` (sección M7) deja de decir "solo doble", describe el estado real y **nombra
      las 3 acciones pendientes** con su cambio SDD de seguimiento.
- [ ] El diff no toca `Runtime/Core/`, `Runtime/CoreChannels/`, ni ninguna carpeta fuera de
      `Runtime/VrInput/`, `Data/VrInput/`, `Tests/EditMode/VrInput/`, `Docs/` y `openspec/`.

## Decisiones del usuario (confirmadas 2026-09-16)

1. **Solo mandos, nada de seguimiento de manos.** No se agrega `com.unity.xr.hands`, ni
   reconocimiento de gestos, ni el Meta Interaction SDK. Cualquier interacción que esta propuesta
   o una posterior necesite se resuelve con botones, gatillos o rayos del control de Quest, no con
   poses de la mano.
2. **`ContactoVisual` es aproximación por mirada de cabeza, no eye tracking literal.** Se
   implementa como un raycast/cono hacia adelante desde la transformación del HMD (`Camera.main` o
   la cámara XR), no con datos de seguimiento ocular: el hardware Quest 3 confirmado no tiene
   sensor de mirada.
3. **Alcance MVP: 4 de las 7 acciones `PhysicalAction` distintas de `Ninguna` ahora, 3 como
   seguimiento explícito.** En alcance de **este** cambio: `ContactoVisual` (mirada de cabeza),
   `Acercarse` y `Alejarse` (distancia HMD↔NPC con histéresis) y `TocarPaciente` (disparador por
   collider). Explícitamente **fuera** de alcance, para convertirse en su propio cambio SDD
   posterior: `EntregarObjeto`, `SenalarPantalla` y `GestoCalma` — requieren una decisión sobre
   paquete de interacción basado en mandos (lo más probable, XR Interaction Toolkit con
   interactores de rayo/agarre ligados a la entrada del control, dado que el seguimiento de manos
   quedó descartado) que **no** forma parte de esta propuesta.
