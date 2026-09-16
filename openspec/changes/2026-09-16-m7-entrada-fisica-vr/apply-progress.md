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
