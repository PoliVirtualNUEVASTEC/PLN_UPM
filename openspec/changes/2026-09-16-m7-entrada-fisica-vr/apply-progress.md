# Apply Progress: M7 — Entrada física VR (PR1: Núcleo C# puro)

## Change
`2026-09-16-m7-entrada-fisica-vr`

## Scope of this batch
Phase 1 only (tasks 1.1-1.9), PR1 per el `feature-branch-chain` decidido en tasks.md. Rama:
`feat/m7-01-nucleo` (ramificada de la rama tracker `feat/m7-entrada-fisica-vr`). Fases 2-5 NO
tocadas en este batch.

## Mode
Strict TDD (`openspec/config.yaml` → `strict_tdd: true`).

## TDD Cycle Evidence

| Task | Behavior | RED (test written first) | GREEN (implementation) | REFACTOR |
|---|---|---|---|---|
| 1.1 | Conformidad con `PhysicalActionSourceContract` (4 pruebas heredadas + vía de `GestoCalma`) | `SpatialPhysicalActionSourceTests.cs` escrito primero, hereda la base sin modificarla | `SpatialPhysicalActionSource()` + `EmitirParaPrueba()` (tarea 1.7) | `Levantar()` extraído como única vía de emisión (AD4) |
| 1.2 | Latch de permanencia de mirada + liberación con histéresis | `DeteccionDeMiradaTests.cs` escrito primero (4 pruebas) | `DetectarMirada()` en `SpatialPhysicalActionSource.cs` | `MuestraConAngulo` factorizado como helper |
| 1.3 | Banda de histéresis de distancia | `HisteresisDeDistanciaTests.cs` escrito primero (5 pruebas) | `DetectarDistancia()` | `frenteDeCabeza` fijado perpendicular al objetivo para aislar del Detector 1 (fix de contaminación cruzada encontrado por inspección) |
| 1.4 | Enfriamiento de contacto | `DeteccionDeContactoTests.cs` escrito primero (4 pruebas) | `DetectarContacto()` | N/A |

Nota de proceso: los 4 archivos RED referencian tipos de los archivos GREEN (`SpatialSample`,
`VrInputSettings`, `SpatialPhysicalActionSource`, etc.), así que ningún RED individual pudo
compilar/fallar de forma aislada antes de que existieran TODOS los archivos GREEN a la vez — esto
es una restricción del compilador de C#/Unity (unidad de compilación por ensamblado), no un atajo
del proceso: los 4 archivos RED se razonaron y escribieron completos, contra el pseudocódigo exacto
de `design.md`, ANTES de escribir una sola línea de `SpatialPhysicalActionSource.cs`. `tasks.md`
1.1 ya anticipa esto explícitamente ("No compila hasta 1.5/1.7").

## Work Unit Evidence

| Evidence | Value |
|---|---|
| Focused test command and exact result | Unity Editor Test Runner > EditMode > Run All, filtrado a `SpatialPhysicalActionSourceTests` + `DeteccionDeMiradaTests` + `HisteresisDeDistanciaTests` + `DeteccionDeContactoTests` (17 métodos de prueba). NO ejecutado por el agente — sin acceso a CLI/headless Unity en este entorno. Pendiente compuerta humana (ver abajo). |
| Runtime harness command/scenario and exact result | N/A — C# puro, sin escena, sin headset, sin sesión XR (tarea 1.9), tal como fija `design.md` (Threat Matrix: "sin... integración de procesos") y la tabla de Work Units de `tasks.md` ("Runtime harness: N/A — C# puro"). |
| Rollback boundary | Borrar `Runtime/VrInput/{SpatialSample,ISpatialSampler,SpatialPhysicalActionSource}.cs`, `Runtime/VrInput/Fakes/ScriptedSpatialSampler.cs`, `Runtime/VrInput/Properties/AssemblyInfo.cs` y los 4 archivos nuevos de `Tests/EditMode/VrInput/`. M7 vuelve a "solo doble" (`ScriptedPhysicalActionSource` no se toca). Ningún otro módulo se ve afectado. |

## COMPUERTA HUMANA — resultado (2026-09-16)

- **Quién la corrió**: el usuario, en Unity Editor Test Runner (pestaña EditMode), rama
  `feat/m7-01-nucleo`.
- **Resultado**: las 17 pruebas nuevas (`SpatialPhysicalActionSourceTests`,
  `DeteccionDeMiradaTests`, `HisteresisDeDistanciaTests`, `DeteccionDeContactoTests`) pasan en
  verde, junto con el resto de la suite completa del proyecto.
- **Qué confirma esto**: la lógica de los 3 detectores (mirada con permanencia/liberación,
  histéresis de distancia sembrada en el punto medio, contacto con enfriamiento saturado) y la vía
  guardada de emisión (`Levantar`/`EmitirParaPrueba`) funcionan como diseñado, no solo por
  inspección de código — incluida la conformidad heredada de `PhysicalActionSourceContract` sin
  modificar la clase base. Fase 1 (PR1) queda confirmada de punta a punta.

## Files Changed

| File | Action | What Was Done |
|---|---|---|
| `Runtime/VrInput/SpatialSample.cs` | Created | `Vec3` (AD1) + `SpatialSample` (AD2/AD3), sin `UnityEngine` |
| `Runtime/VrInput/ISpatialSampler.cs` | Created | Costura interna de hardware (AD2/AD3) |
| `Runtime/VrInput/SpatialPhysicalActionSource.cs` | Created | Núcleo `IPhysicalActionSource`: `EstadoDeDistancia`, `VrInputSettings` (ver Deviations), 3 detectores, `Levantar` (AD4), `EmitirParaPrueba` (AD5), `Bombear` |
| `Runtime/VrInput/Fakes/ScriptedSpatialSampler.cs` | Created | Doble determinista interno de `ISpatialSampler` (cola FIFO) |
| `Runtime/VrInput/Properties/AssemblyInfo.cs` | Created | `InternalsVisibleTo("NpcAi.VrInput.Tests")` |
| `Tests/EditMode/VrInput/SpatialPhysicalActionSourceTests.cs` | Created | Gemela de contrato (4 pruebas heredadas) |
| `Tests/EditMode/VrInput/DeteccionDeMiradaTests.cs` | Created | 4 pruebas: mirada sostenida emite una vez, vistazo fugaz no emite, no repite hasta liberar, perder objetivo reinicia |
| `Tests/EditMode/VrInput/HisteresisDeDistanciaTests.cs` | Created | 5 pruebas: siembra sin emitir (AD8), umbral de entrada, umbral de salida, oscilación sin ráfaga, perder objetivo no es alejarse |
| `Tests/EditMode/VrInput/DeteccionDeContactoTests.cs` | Created | 4 pruebas: primer pulso emite, solape continuo no inunda, reemite tras enfriamiento, sin pulso no emite |
| `openspec/changes/2026-09-16-m7-entrada-fisica-vr/tasks.md` | Modified | Marcadas `[x]` 1.1-1.9 con comentarios de evidencia |

## Deviations from Design

1. **`VrInputSettings` vive en `SpatialPhysicalActionSource.cs`, no en `Config/VrInputSettings.cs`.**
   `design.md` (Interfaces/Contracts) muestra el constructor interno consumiendo `VrInputSettings`
   como un tipo ya existente, pero `tasks.md` asigna crear `Runtime/VrInput/Config/VrInputSettings.cs`
   a la tarea 2.2 (Fase 2/PR2, fuera de alcance explícito de este PR). La tarea 2.4 ("confirmar que
   el constructor interno... consume `VrInputSettings`, y que el constructor público sin parámetros
   usa `new VrInputSettings()`") se lee como una confirmación de un hecho que ya debe existir al
   cerrar la Fase 1 — así que la Fase 1 necesitaba alguna definición del tipo para compilar contra
   la firma exacta que fija `design.md`. Ponerla en este mismo archivo (sin crear una carpeta
   `Config/` nueva) mantiene el diff dentro de la lista de archivos autorizada para este PR. La
   Fase 2 debe trasladar esta clase intacta a `Config/VrInputSettings.cs` y borrarla de aquí; los
   nombres de campo, tipos y defaults ya coinciden 1:1 con la tabla "Configuración" de `design.md`
   para que ese traslado sea copiar-pegar, no reescribir.
2. `VrInputSettings` usa propiedades `{ get; set; }`, no campos públicos planos, siguiendo la
   convención ya establecida en `Runtime/Speech/Config/SpeechSettings.cs` (otro "Snapshot POCO"
   `internal sealed class`).
3. `ISpatialSampler` y `ScriptedSpatialSampler` quedan `internal` (no `public`), a diferencia de
   `ScriptedPhysicalActionSource` (`public`, porque implementa el puerto público
   `NpcAi.Core.IPhysicalActionSource` que consumen otros módulos como M11). `ISpatialSampler` es
   una costura interna de hardware según `design.md`; nada fuera de `NpcAi.VrInput` debería
   referenciarla.
4. Se agregó una prueba extra por detector más allá de los nombres literales de la tabla de
   Trazabilidad de `spec.md` (`Perder_el_objetivo_reinicia_la_permanencia`,
   `Perder_el_objetivo_no_cuenta_como_Alejarse`, `Sin_pulso_no_emite_TocarPaciente`) para fijar
   comportamiento que `design.md` declara explícitamente ("perder el objetivo no es alejarse") y
   que el conjunto mínimo de pruebas nombradas no cubre directamente.
5. Sin prueba dedicada para `Bombear()` en este PR: es una delegación trivial de una línea (jalar
   una muestra, correr `ProcesarMuestra`) cuya corrección queda cubierta extremo a extremo cuando
   la Fase 3 cablee un `ISpatialSampler` real (`VrInputBehaviourWiringTests`, tarea 3.4). Coincide
   con `tasks.md`, que no asigna ninguna tarea RED para `Bombear()` en la Fase 1.

## Issues Found

Ninguno de diseño. Un riesgo de contaminación cruzada entre detectores se encontró y corrigió por
inspección antes de considerar el trabajo terminado: `ProcesarMuestra` corre los 3 detectores en
cada llamada (por diseño), así que las pruebas de un detector deben neutralizar explícitamente a
los otros dos (objetivo perdido para aislar distancia/mirada del contacto; ángulo de mirada fijo en
90° para aislar distancia de mirada). Documentado en los comentarios de cada archivo de prueba.

## Remaining Tasks

- [ ] Fase 2 (PR2): `VrInputSettings`/`VrInputSettingsAsset` como dato — NO iniciada, fuera de
  alcance de este PR.
- [ ] Fase 3 (PR3): `VrInputBehaviour`/`TouchZoneRelay`/`UnitySpatialSampler` — NO iniciada.
- [ ] Fase 4 (PR4): datos por escenario + docs + archivo de spec — NO iniciada.
- [ ] Fase 5: cierre (este batch hace `git add` + commit de PR1; abrir/mergear el PR es acción del
  orquestador/usuario).

## Workload / PR Boundary

- Mode: `feature-branch-chain` (decidido 2026-09-16, registrado en el Review Workload Forecast de
  `tasks.md`)
- Current work unit: Unit 1 — Núcleo C# puro (PR1)
- Boundary: arranca desde el VrInput existente (solo `ScriptedPhysicalActionSource`) y termina con
  `SpatialPhysicalActionSource` + sus 4 archivos de prueba EditMode, autónomo y reversible sin
  tocar ningún otro módulo ni fase.
- Estimated review budget impact: `tasks.md` estimaba ~570 líneas para esta unidad; el stat real
  de `git diff --cached --stat` se reporta en el mensaje de retorno de este batch al orquestador.

## Status

9/9 tareas de la Fase 1 completas y **confirmadas en Test Runner real** (ver COMPUERTA
HUMANA — resultado arriba). PR1 (`feat/m7-01-nucleo` → tracker `feat/m7-entrada-fisica-vr`,
PR #34) queda listo de punta a punta. `sdd-apply` continúa con la Fase 2 (PR2, rama
`feat/m7-02-config`).

---

# Apply Progress: M7 — Entrada física VR (PR2: Configuración como dato)

## Scope of this batch

Phase 2 solamente (tareas 2.1-2.4), PR2 del `feature-branch-chain` decidido en `tasks.md`. Rama:
`feat/m7-02-config` (ramificada de `feat/m7-01-nucleo`, que ya trae la Fase 1 mergeada en su
historia). Fases 1 (ya cerrada arriba), 3, 4 y 5 NO tocadas en este batch.

## Mode

Strict TDD (`openspec/config.yaml` → `strict_tdd: true`).

## TDD Cycle Evidence

| Task | Behavior | RED (test written first) | GREEN (implementation) | REFACTOR |
|---|---|---|---|---|
| 2.1 | `OnValidate` acota cada campo de la tabla "Configuración", incluidos los 2 `Max` cruzados | `VrInputSettingsAssetTests.cs` escrito primero (9 pruebas); no compila hasta que `VrInputSettingsAsset` existe | `VrInputSettingsAsset.OnValidate()` (tarea 2.3) | N/A — molde ya fijado por `SpeechSettingsAsset`, sin necesidad de reestructurar |
| 2.2 | `VrInputSettings` disponible como snapshot POCO en su ubicación final | N/A — es un traslado de código ya verde (PR1), no un comportamiento nuevo que necesite RED propio | Movida verbatim a `Runtime/VrInput/Config/VrInputSettings.cs`; eliminada de `SpatialPhysicalActionSource.cs` | N/A |
| 2.3 | `ToSettings()` copia los 6 campos del asset al snapshot | Cubierto por `ToSettings_copia_los_campos_del_asset_al_snapshot` en el mismo archivo RED de 2.1 | `VrInputSettingsAsset.ToSettings()` | N/A |
| 2.4 | El núcleo sigue consumiendo `VrInputSettings` sin cambios tras el traslado | N/A — tarea de verificación explícita, no de comportamiento nuevo (`tasks.md` no le asigna RED) | N/A — sin código nuevo; confirmado por inspección de `SpatialPhysicalActionSource.cs` | N/A |

Nota de proceso: 2.2 y 2.4 son, por diseño de `tasks.md`, tareas de traslado/verificación sin RED
propio (el comportamiento que cubren ya quedó verde en PR1 bajo otro archivo/ubicación). El único
RED de este batch es 2.1, que cubre tanto el clamp (2.1) como `ToSettings` (2.3) en el mismo
archivo de prueba.

## Work Unit Evidence

| Evidence | Value |
|---|---|
| Focused test command and exact result | Unity Editor Test Runner > EditMode > Run All, filtrado a `VrInputSettingsAssetTests` (9 métodos de prueba). NO ejecutado por el agente — sin acceso a CLI/headless Unity en este entorno. Pendiente compuerta humana (tarea 3.5, junto con Fase 3). |
| Runtime harness command/scenario and exact result | N/A — `ScriptableObject.CreateInstance`/`OnValidate` llamado directo desde la prueba, sin escena ni Inspector, tal como fija la tabla de Work Units de `tasks.md` ("Runtime harness: N/A — `ScriptableObject.CreateInstance`, sin escena"). |
| Rollback boundary | Borrar `Runtime/VrInput/Config/` completo (`VrInputSettings.cs`, `VrInputSettingsAsset.cs`) y `Tests/EditMode/VrInput/VrInputSettingsAssetTests.cs`; restaurar la definición de `VrInputSettings` dentro de `SpatialPhysicalActionSource.cs` (o dejar que el núcleo caiga a `new VrInputSettings()` con una definición mínima). Ningún otro módulo se ve afectado; PR1 queda intacto. |

## COMPUERTA HUMANA — resultado (2026-09-16)

- **Quién la corrió**: el usuario, en Unity Editor Test Runner (pestaña EditMode), rama
  `feat/m7-02-config`.
- **Resultado**: las 9 pruebas nuevas de `VrInputSettingsAssetTests` pasan en verde, junto con el
  resto de la suite completa del proyecto.
- **Qué confirma esto**: `OnValidate()` acota correctamente los 6 campos, incluidos los 2 rangos
  cruzados (liberación de mirada ≥ cono de mirada; alejarse ≥ acercarse + margen mínimo), y
  `ToSettings()` copia los 6 campos sin pérdida. Fase 2 (PR2) queda confirmada de punta a punta.

## Files Changed

| File | Action | What Was Done |
|---|---|---|
| `Runtime/VrInput/Config/VrInputSettings.cs` | Created | Snapshot POCO trasladado verbatim desde `SpatialPhysicalActionSource.cs` (mismos campos/tipos/defaults) |
| `Runtime/VrInput/Config/VrInputSettingsAsset.cs` | Created | `ScriptableObject` con `[CreateAssetMenu]`, `OnValidate` (clamps simples + 2 `Max` cruzados), `ToSettings()` |
| `Runtime/VrInput/SpatialPhysicalActionSource.cs` | Modified | Eliminada la definición de `VrInputSettings` (ahora vive en `Config/VrInputSettings.cs`); el resto del archivo (enum `EstadoDeDistancia`, los 3 detectores, `Levantar`, ambos constructores) sin cambios |
| `Tests/EditMode/VrInput/VrInputSettingsAssetTests.cs` | Created | 9 pruebas: 4 clamps simples de un solo límite, 2 clamps con su propio rango simple, 2 pruebas dedicadas a los `Max` cruzados, 1 prueba de `ToSettings` |
| `openspec/changes/2026-09-16-m7-entrada-fisica-vr/tasks.md` | Modified | Marcadas `[x]` 2.1-2.4 con comentarios de evidencia |

## Deviations from Design

1. **`VrInputSettings` y `VrInputSettingsAsset` quedan en el namespace `NpcAi.VrInput`, no
   `NpcAi.VrInput.Config`.** `design.md` no fija explícitamente el namespace de los archivos de
   `Config/` (solo su ruta de archivo en el "File Inventory" y su forma en la sección
   "Configuración"). El precedente de M1 (`NpcAi.Speech.Config`) usa un namespace anidado por
   carpeta, pero mantener `NpcAi.VrInput` evita que `SpatialPhysicalActionSource.cs` (Fase 1,
   ya cerrada) y `VrInputBehaviour.cs` (Fase 3, próximo PR) necesiten un `using` adicional solo
   por el traslado de archivo — el nombre de tipo ya es único dentro del módulo y ningún otro
   módulo referencia estos tipos (regla dura 3: son internos/de módulo). Carpeta física
   (`Runtime/VrInput/Config/`) y namespace de C# no tienen que coincidir.
2. Sin cambios de comportamiento en `SpatialPhysicalActionSource.cs` más allá de eliminar la
   clase ya trasladada: los 3 detectores, `Levantar`, `Bombear`, `ProcesarMuestra` y ambos
   constructores quedan byte-por-byte iguales a como los dejó PR1, tal como exige la frontera de
   este PR (tarea 2.4 es solo confirmación).

## Issues Found

Ninguno. El traslado fue mecánico (copiar-pegar, como anticipaba la Desviación 1 de PR1): los
nombres de campo, tipos y defaults de `VrInputSettings` ya coincidían 1:1 con la tabla
"Configuración" de `design.md` antes de este PR, así que no hubo reconciliación de valores que
hacer.

## Remaining Tasks

- [ ] Fase 3 (PR3): `VrInputBehaviour`/`TouchZoneRelay`/`UnitySpatialSampler` — NO iniciada.
- [ ] Fase 4 (PR4): datos por escenario + docs + archivo de spec — NO iniciada.
- [ ] Fase 5: cierre (este batch hace `git add` + commit de PR2; abrir/mergear el PR es acción del
  orquestador/usuario).
- [ ] Tarea 3.5 (compuerta humana): Test Runner EditMode > Run All cubriendo también las 9
  pruebas de `VrInputSettingsAssetTests` de este PR2, agrupada con el cierre de la Fase 3.

## Workload / PR Boundary

- Mode: `feature-branch-chain` (decidido 2026-09-16, registrado en el Review Workload Forecast de
  `tasks.md`)
- Current work unit: Unit 2 — Configuración como dato (PR2)
- Boundary: arranca desde `SpatialPhysicalActionSource` con `VrInputSettings` embebida (fin de
  PR1) y termina con `VrInputSettings`/`VrInputSettingsAsset` trasladados a `Config/`, autónomo y
  reversible sin tocar los detectores del núcleo ni ningún otro módulo.
- Estimated review budget impact: `tasks.md` estimaba ~190 líneas para esta unidad; el stat real
  de `git diff --cached --stat` se reporta en el mensaje de retorno de este batch al orquestador.

## Status

4/4 tareas de la Fase 2 completas y **confirmadas en Test Runner real** (ver COMPUERTA HUMANA —
resultado arriba). PR2 (`feat/m7-02-config` → PR1 `feat/m7-01-nucleo`, PR #35) queda listo de
punta a punta. `sdd-apply` continúa con la Fase 3 (PR3, rama `feat/m7-03-envoltura`).

---

# Apply Progress: M7 — Entrada física VR (PR3: Envoltura MonoBehaviour)

## Scope of this batch

Solo tareas 3.1-3.4 de la Fase 3 (PR3 del `feature-branch-chain` decidido en `tasks.md`). Rama:
`feat/m7-03-envoltura` (ramificada de `feat/m7-02-config`, que ya trae las Fases 1-2 mergeadas en
su historia). Las tareas 3.5 y 3.6 son compuertas humanas — MANUAL, no las ejecuta el agente — y
quedan explícitamente sin marcar (ver notas de `tasks.md`). Fase 4 (PR4: datos por escenario, docs,
archivo de spec) NO tocada en este batch.

## Mode

Strict TDD (`openspec/config.yaml` → `strict_tdd: true`). Entorno sin acceso a CLI/headless Unity
(solo Unity Editor GUI, según instrucción del proyecto): el agente escribió pruebas y producción
razonando el ciclo RED→GREEN por inspección, sin ejecutar el Test Runner. La confirmación real en
verde queda como compuerta humana (tarea 3.5), igual que en PR1/PR2, pero sin la sección "COMPUERTA
HUMANA — resultado" que sí tienen PR1/PR2, porque a la fecha de este batch el usuario aún no ha
corrido el Test Runner para estas dos nuevas pruebas.

## TDD Cycle Evidence

| Task | Behavior | RED (test written first) | GREEN (implementation) | REFACTOR |
|---|---|---|---|---|
| 3.1 | Latch de contacto por `LayerMask`, lectura-y-limpieza en una sola llamada (AD7) | N/A — sin prueba EditMode dedicada: `OnTriggerEnter` exige física de escena real que `design.md` no lista en su tabla "Testing Strategy"; se ejercita en la compuerta humana 3.6 | `TouchZoneRelay.OnTriggerEnter`/`ConsumirPulso()` | N/A |
| 3.2 | `LeerMuestra` arma un `SpatialSample` real o devuelve `false` en silencio sin cámara/objetivo (AD10) | Cubierto indirectamente por `Sin_camara_u_objetivo_asignados_no_lanza` (tarea 3.4): esa prueba ejercita `UnitySpatialSampler` real (no un doble) a través de `VrInputBehaviour.CablearParaPrueba()` sin argumento | `UnitySpatialSampler.LeerMuestra`/`AVec3` | N/A |
| 3.3 | Ciclo de vida `Awake`/`OnEnable`/`Update`/`OnDisable` cablea el núcleo al canal sin suscribirse al canal mismo (AD9) | `VrInputBehaviourWiringTests.cs` escrito primero (2 pruebas); no compila hasta que `VrInputBehaviour` exista | `VrInputBehaviour.Awake/OnEnable/OnDisable/Update/PublicarEnCanal` | N/A — molde ya fijado por `NpcPresenterBehaviour` (M8), sin necesidad de reestructurar |
| 3.4 | `OnEnable` suscribe/`OnDisable` desuscribe el núcleo interno sin fuga; no-op silencioso sin cámara/objetivo | `VrInputBehaviourWiringTests.cs` escrito primero (2 pruebas), nombres tomados literal de la Trazabilidad de `spec.md`: `OnEnable_suscribe_y_OnDisable_desuscribe_sin_fugas`, `Sin_camara_u_objetivo_asignados_no_lanza` | Cubierto por 3.1-3.3 ya construidas al mismo tiempo (mismo problema de unidad de compilación por ensamblado que PR1) | N/A |

Nota de proceso (igual que en PR1): `VrInputBehaviourWiringTests.cs` referencia `VrInputBehaviour`,
`UnitySpatialSampler` y `TouchZoneRelay`, así que no pudo compilar/fallar de forma aislada antes de
que existieran los 3 archivos GREEN a la vez — restricción del compilador de C#/Unity (unidad de
compilación por ensamblado), no un atajo del proceso. El archivo de prueba se razonó y escribió
completo, contra el pseudocódigo exacto de `design.md` (sección "ciclo de vida" y AD7/AD9/AD10),
ANTES de escribir una sola línea de los 3 archivos de producción.

Triangulación explícita para 3.4: la prueba `OnEnable_suscribe_y_OnDisable_desuscribe_sin_fugas`
encola una SEGUNDA muestra con `deltaSegundos = 2f` (fuera del enfriamiento de contacto por
defecto, 1.0 s) para el bombeo posterior a `DescablearParaPrueba()`. Esto fuerza que, si `OnDisable`
NO hubiera desuscrito correctamente, el núcleo SÍ levantaría un segundo `TocarPaciente` y el conteo
del canal subiría a 2 — es decir, la prueba puede fallar de una forma real y no solo "nunca hizo
nada" (ver Assertion Quality Rules de `strict-tdd.md`, regla de asercion de coleccion vacia: la
ausencia de una segunda emisión está explícitamente forzada por el setup, no es un accidente del
enfriamiento).

## Work Unit Evidence

| Evidence | Value |
|---|---|
| Focused test command and exact result | Unity Editor Test Runner > EditMode > Run All, filtrado a `VrInputBehaviourWiringTests` (2 métodos de prueba). NO ejecutado por el agente — sin acceso a CLI/headless Unity en este entorno (solo Unity Editor GUI). Escrito y creído correcto por inspección; confirmación real pendiente como compuerta humana (tarea 3.5). |
| Runtime harness command/scenario and exact result | Automatizable vía `CablearParaPrueba`/`DescablearParaPrueba`/`BombearParaPrueba` sin escena real, tal como fija la tabla de Work Units de `tasks.md` ("Automatizable... sin escena real"). La confirmación en vivo con headset (Quest 3) queda diferida a la compuerta humana 3.6, que además necesita los `.asset` de datos de la Fase 4 (PR4) y el cableado de escena de M11 — ninguno de los dos existe todavía. |
| Rollback boundary | Borrar `Runtime/VrInput/VrInputBehaviour.cs`, `Runtime/VrInput/TouchZoneRelay.cs`, `Runtime/VrInput/UnitySpatialSampler.cs` y `Tests/EditMode/VrInput/VrInputBehaviourWiringTests.cs`. M7 vuelve a exponer solo el núcleo (`SpatialPhysicalActionSource`) más su config (Fases 1-2, ya cerradas e intactas). Ningún otro módulo se ve afectado. |

## Files Changed

| File | Action | What Was Done |
|---|---|---|
| `Runtime/VrInput/TouchZoneRelay.cs` | Created | `MonoBehaviour` público: `OnTriggerEnter` filtra por `LayerMask`, latch `_pendiente`, `ConsumirPulso()` internal (lee y limpia, AD7) |
| `Runtime/VrInput/UnitySpatialSampler.cs` | Created | `internal sealed class` (espejo de `MicrophoneAudioCapture`, M1): implementación real de `ISpatialSampler`; `LeerMuestra` arma `SpatialSample` desde `Camera`/`Transform`/`Time.deltaTime`/`TouchZoneRelay.ConsumirPulso()`, o devuelve `false` en silencio sin cámara/objetivo (AD10) |
| `Runtime/VrInput/VrInputBehaviour.cs` | Created | Raíz de composición `MonoBehaviour` público: `Awake` arma el núcleo con `UnitySpatialSampler` (o el muestreador de prueba inyectado), `OnEnable`/`OnDisable` (des)suscriben `_sujeto.OnAction` (AD9, nunca el canal), `Update` bombea (AD6), `PublicarEnCanal` solo llama `_canal?.Raise(a)`; costuras `CablearParaPrueba`/`DescablearParaPrueba`/`BombearParaPrueba` (patrón M8) |
| `Tests/EditMode/VrInput/VrInputBehaviourWiringTests.cs` | Created | 2 pruebas: `OnEnable_suscribe_y_OnDisable_desuscribe_sin_fugas` (con `ScriptedSpatialSampler`) y `Sin_camara_u_objetivo_asignados_no_lanza` (con `UnitySpatialSampler` real sin cámara/objetivo) |
| `openspec/changes/2026-09-16-m7-entrada-fisica-vr/tasks.md` | Modified | Marcadas `[x]` 3.1-3.4 con comentarios de evidencia; 3.5/3.6 quedan sin marcar con notas explicando que son compuertas humanas pendientes |

## Deviations from Design

1. **`hayObjetivo` en `UnitySpatialSampler.LeerMuestra` se calcula explícitamente como
   `_camaraDelHmd != null && _objetivo != null` dentro de la rama que ya solo se alcanza cuando
   ambos son no-nulos** (por el `return false` anterior). Es una expresión que, en este punto del
   método, siempre evalúa a `true` — se dejó así, en vez de simplemente `hayObjetivo: true`, porque
   el texto de la tarea de origen la especifica literalmente de esa forma y documenta la invariante
   de forma explícita en el sitio de construcción del `SpatialSample`, sin cambiar ningún
   comportamiento observable.
2. **`TouchZoneRelay` se declaró `public sealed class` (no `internal`).** `design.md` no fija su
   accesibilidad explícitamente. Se decidió `public` por paridad con `VrInputBehaviour` y
   `NpcPresenterBehaviour` (M8): es un componente que el proyecto anfitrión (M11) agrega al
   `GameObject` del paciente en una escena real, igual que las demás envolturas `MonoBehaviour` ya
   públicas del proyecto.
3. **Sin prueba EditMode dedicada para `TouchZoneRelay.OnTriggerEnter`.** La tabla "Testing
   Strategy" de `design.md` no lo lista como archivo de prueba (a diferencia de
   `VrInputBehaviourWiringTests`, que sí aparece explícitamente); `OnTriggerEnter` depende de
   colisiones físicas reales de Unity, que EditMode sin escena no puede disparar de forma confiable.
   Su corrección por inspección (filtro de `LayerMask`, latch de lectura única) queda documentada en
   el comentario del archivo y se confirma end-to-end en la compuerta humana 3.6 (Quest 3).

## Issues Found

Ninguno de diseño. Se confirmó por inspección, antes de dar la Fase 3 por terminada, que
`VrInputBehaviour.PublicarEnCanal` no contiene ninguna llamada a `_canal.Subscribe` en ningún punto
del archivo — el bug real que el orquestador ya había encontrado y corregido en `proposal.md`/
`spec.md` el mismo día (AD9) no se reintrodujo en el código de esta Fase.

## COMPUERTA HUMANA 3.5 — resultado (2026-09-16)

- **Quién la corrió**: el usuario, en Unity Editor Test Runner (pestaña EditMode), rama
  `feat/m7-03-envoltura`.
- **Bloqueo real encontrado y corregido en el camino**: `NpcAi.VrInput.Tests.asmdef` (creado en
  PR1) le faltaba la referencia a `NpcAi.Core.Channels` — `VrInputBehaviourWiringTests.cs` usa
  `PhysicalActionChannel` y no compilaba (`CS0234`). Mismo tipo de gap que el fix de asmdef de
  M2 (`Unity.InferenceEngine` faltante) — solo un compilador real lo detecta. Corregido en las 3
  ramas de la cadena (cada fix en el PR que corresponde). De paso se commitearon los `.meta`
  faltantes de los archivos de las 3 PRs (los agentes los escriben sin Unity abierto).
- **Resultado**: tras el fix, las 2 pruebas de `VrInputBehaviourWiringTests` pasan en verde, junto
  con el resto de la suite completa (Fases 1-3).
- **Qué confirma esto**: el ciclo de vida de `VrInputBehaviour` (suscripción/desuscripción al
  núcleo interno, nunca al canal — AD9) y el no-op silencioso sin cámara/objetivo (AD10) funcionan
  como diseñado. Fase 3 (PR3) queda confirmada de punta a punta para las tareas 3.1-3.5.

## Remaining Tasks

- [ ] Tarea 3.6 (compuerta humana, Quest 3 físico): confirmación en vivo de las 4 acciones. Depende
  además de los `.asset` de la Fase 4 (PR4) y del cableado de escena de M11 (Camera, Transform,
  collider + `TouchZoneRelay`), ninguno de los dos existente todavía — puede quedar abierta por un
  tiempo, como se advierte explícitamente en `tasks.md`.
- [ ] Fase 4 (PR4): datos por escenario + docs + archivo de spec — NO iniciada.
- [ ] Fase 5: cierre (este batch hace `git add` + commit de PR3; abrir/mergear el PR es acción del
  orquestador/usuario).

## Workload / PR Boundary

- Mode: `feature-branch-chain` (decidido 2026-09-16, registrado en el Review Workload Forecast de
  `tasks.md`)
- Current work unit: Unit 3 — Envoltura MonoBehaviour (PR3)
- Boundary: arranca desde el núcleo + config ya cerrados (fin de PR2) y termina con
  `VrInputBehaviour`/`TouchZoneRelay`/`UnitySpatialSampler` + su prueba de cableado, autónomo y
  reversible sin tocar el núcleo, la config ni ningún otro módulo.
- Estimated review budget impact: `tasks.md` estimaba ~370 líneas para esta unidad; el stat real de
  `git diff --cached --stat` se reporta en el mensaje de retorno de este batch al orquestador.

## Status

5/6 tareas de la Fase 3 completas y **confirmadas en Test Runner real** (3.1-3.5, ver COMPUERTA
HUMANA 3.5 arriba); solo 3.6 (Quest 3 físico) queda pendiente, bloqueada en la Fase 4 y en M11.
PR3 (`feat/m7-03-envoltura` → PR2 `feat/m7-02-config`, PR #36) queda listo de punta a punta.
`sdd-apply` continúa con la Fase 4 (PR4, datos + docs + cierre).

---

# Apply Progress: M7 — Entrada física VR (PR4: Datos, documentación y cierre)

## Scope of this batch

Solo tareas 4.1-4.5 de la Fase 4 (PR4 del `feature-branch-chain` decidido en `tasks.md`). Rama:
`feat/m7-04-datos-docs-cierre` (ramificada de `feat/m7-03-envoltura`, que ya trae las Fases 1-3
mergeadas en su historia). Esta fase es enteramente documentación y datos — sin código C#, sin
ciclo TDD aplicable. La tarea 3.6 (compuerta humana, Quest 3 físico) sigue sin marcar: esta batch
entrega uno de sus dos bloqueos (los `.asset` de datos), pero no la ejecuta ni la registra — eso
sigue siendo exclusivo del usuario, con headset real y la escena de M11 todavía por construir.

## Mode

N/A — sin código de producción ni de prueba en esta fase (documentación y datos puros, per la
tabla de Work Units de `tasks.md`, Unit 4: "Manual: revisión de que Docs/MODULES.md y
Data/VrInput/README.md quedan consistentes"). No aplica TDD Cycle Evidence.

## Work Unit Evidence

| Evidence | Value |
|---|---|
| Focused test command and exact result | N/A — sin código ejecutable en esta fase. Verificación aplicada: `diff` byte-a-byte confirmó la copia idéntica del spec promovido (tarea 4.3); revisión manual de consistencia de `Docs/MODULES.md` y `Data/VrInput/README.md` contra `design.md`/`apply-progress.md` (tarea 4.4, per Work Unit 4 de `tasks.md`). |
| Runtime harness command/scenario and exact result | N/A — datos y documentación, sin escena, sin build, sin Unity Editor. La compuerta humana real (Quest 3 físico) sigue siendo la tarea 3.6, ahora desbloqueada en uno de sus dos requisitos (`Data/VrInput/Emergency.asset`) pero todavía bloqueada en el otro (escena de M11). |
| Rollback boundary | Revertir el commit de esta batch (`Data/VrInput/`, `openspec/specs/entrada-fisica-vr-m7/`, la sección M7 de `Docs/MODULES.md`, y los checkmarks 4.1-4.5 de `tasks.md`). No toca `Runtime/`, `Tests/` ni ningún otro módulo — las Fases 1-3 quedan intactas. |

## Files Changed

| File | Action | What Was Done |
|---|---|---|
| `Data/VrInput/README.md` | Created | Explica los 6 campos de `VrInputSettingsAsset` en lenguaje llano (qué controla, cómo se misbehave en los extremos), tabla con tipo/default/rango, sección de calibración en Quest 3 que referencia la tarea 3.6 |
| `Data/VrInput/README.md.meta` | Created | `TextScriptImporter`, mismo molde que `Data/Speech/README.md.meta` |
| `Data/VrInput.meta` | Created | Meta de carpeta, `DefaultImporter`, mismo molde que `Data/Speech.meta` |
| `Data/VrInput/Emergency.asset` | Created | `VrInputSettingsAsset` con los 6 defaults exactos de la tabla "Configuración" de `design.md`; `m_Script` referencia el guid real de `VrInputSettingsAsset.cs.meta` |
| `Data/VrInput/Emergency.asset.meta` | Created | `NativeFormatImporter`, mismo molde que `Data/Speech/Boardroom.asset.meta` |
| `openspec/specs/entrada-fisica-vr-m7/spec.md` | Created | Copia byte-idéntica (confirmada con `diff`) del spec del cambio |
| `openspec/specs/entrada-fisica-vr-m7/spec.md.meta` | Created | `TextScriptImporter`, mismo molde que `openspec/specs/generador-dialogo-m6/spec.md.meta` |
| `openspec/specs/entrada-fisica-vr-m7.meta` | Created | Meta de carpeta, `DefaultImporter`, mismo molde que `openspec/specs/generador-dialogo-m6.meta` |
| `Docs/MODULES.md` | Modified | Sección M7: "Estado actual" reescrita (4/7 `PhysicalAction` reales, PRs #34-#36 mergeables tras cerrar la tracker #33, las 3 acciones de mandos nombradas explícitamente como pendientes, compuerta 3.6 y sus 2 bloqueos); agregada línea "Specs formales" |
| `openspec/changes/2026-09-16-m7-entrada-fisica-vr/tasks.md` | Modified | Marcadas `[x]` 4.1-4.5 con comentarios de evidencia |

## Deviations from Design

1. **Nombre del asset de escenario: `Emergency.asset`, no `Emergencia.asset`.** `design.md` y
   `tasks.md` solo dicen `<escenario>.asset` sin fijar el nombre exacto. `Data/Corpus/` usa
   español minúscula (`emergencia.json`) para *contenido* de datos, pero los dos precedentes
   reales de *settings asset por escenario* en este repo — `Data/Speech/README.md` (que ya
   menciona `Emergency.asset` como el nombre esperado del próximo escenario, junto al
   `Boardroom.asset` existente) y `Data/Presentation/README.md` (que nombra `Emergency.asset`
   explícitamente como la configuración del escenario de triaje/emergencia, aunque ese archivo
   todavía no existe en `Data/Presentation/`) — coinciden en inglés capitalizado. Se siguió ese
   patrón más específico y más reciente (settings asset, no dato de contenido) en vez del de
   `Data/Corpus/`.
2. **`openspec/specs/entrada-fisica-vr-m7.meta` y `spec.md.meta` con GUIDs nuevos generados en
   este batch**, verificados contra los ~124 GUIDs existentes del repo antes de escribir (sin
   colisión) — `design.md`/`tasks.md` no especifican un proceso de generación de GUID, así que se
   siguió el único precedente ya promovido con `.meta` (`openspec/specs/generador-dialogo-m6/`).

## Issues Found

Ninguno de diseño. El árbol de trabajo tiene numerosos archivos sin trackear ajenos a este PR4
(`Training/Nlu/` de M2 PR2 en curso, `.meta` faltantes de varios cambios ya archivados de M0/M1/
M4/M5/M6/M8, `Registro_Modelo_Etiquetado/`) — ninguno se tocó ni se agregó al `git add` de este
batch (ver tarea 4.5 y confirmación de frontera abajo).

## Confirmación de frontera de diff (tarea 4.5)

`git diff main...HEAD --stat` sobre la cadena ya commiteada (PR1-PR3, 40 archivos) confirma que el
100% cae dentro de `Runtime/VrInput/`, `Tests/EditMode/VrInput/` y
`openspec/changes/2026-09-16-m7-entrada-fisica-vr/`. Las adiciones sin commitear de esta Fase 4
(`Data/VrInput/`, `Docs/MODULES.md`, `openspec/specs/entrada-fisica-vr-m7/`, y los checkmarks de
`tasks.md`) caen igualmente dentro de la frontera autorizada (`Runtime/VrInput/`, `Data/VrInput/`,
`Tests/EditMode/VrInput/`, `Docs/MODULES.md`, `openspec/`). Nada fuera de esa lista se agregó al
commit de este PR4.

## Remaining Tasks

- [ ] Tarea 3.6 (compuerta humana, Quest 3 físico): confirmación en vivo de las 4 acciones. Uno de
  sus dos bloqueos queda resuelto por esta batch (`Data/VrInput/Emergency.asset`); el otro sigue
  pendiente (cableado de escena de M11 — `Camera`, `Transform`, `Collider` + `TouchZoneRelay`, que
  no existe en ninguna escena del proyecto todavía). Es la ÚNICA tarea abierta en todo el cambio
  `2026-09-16-m7-entrada-fisica-vr` tras cerrar esta Fase 4.
- [ ] Fase 5 (cierre): `git add`/commit de este PR4 (acción de este mismo batch, ver abajo); abrir
  y mergear los 4 PR (tracker #33 al final) es acción del orquestador/usuario, no del agente
  (regla dura 8 de `CLAUDE.md` del proyecto: self-merge por el autor).

## Workload / PR Boundary

- Mode: `feature-branch-chain` (decidido 2026-09-16, registrado en el Review Workload Forecast de
  `tasks.md`)
- Current work unit: Unit 4 — Datos por escenario, docs y cierre (PR4)
- Boundary: arranca desde la envoltura ya cerrada (fin de PR3) y termina con
  `Data/VrInput/Emergency.asset` + `README.md`, el spec promovido y la sección M7 de
  `Docs/MODULES.md` actualizada — autónomo y reversible sin tocar `Runtime/`, `Tests/` ni ningún
  otro módulo.
- Estimated review budget impact: `tasks.md` estimaba ~110-150 líneas para esta unidad; el stat
  real de `git diff --cached --stat` se reporta en el mensaje de retorno de este batch al
  orquestador.

## Status

5/5 tareas de la Fase 4 completas (4.1-4.5). Con esto, **todas las tareas automatizables del
cambio `2026-09-16-m7-entrada-fisica-vr` quedan completas**; solo la tarea 3.6 (compuerta humana
en Quest 3 físico) permanece abierta en todo el cambio, y depende además del cableado de escena de
M11 (fuera del alcance de este cambio). PR4 (`feat/m7-04-datos-docs-cierre` → PR3
`feat/m7-03-envoltura`) queda listo para su propio commit. `sdd-apply` entrega el control al
orquestador: los 4 PR (#34, #35, #36 y este PR4 pendiente de abrir) quedan pendientes de que la
rama tracker (#33) cierre la cadena hacia `main` — acción humana, no de `sdd-apply`.
