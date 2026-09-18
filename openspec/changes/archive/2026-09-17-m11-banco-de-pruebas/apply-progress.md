# Apply Progress: M11 — Banco de pruebas, PR1 (`SessionDirector`)

## Change

`2026-09-17-m11-banco-de-pruebas`

## Scope of this batch

Fases 0-4 y 6 (tareas 0.1-0.4, 1.1-1.3, 2.1-2.4, 3.1-3.2, 4.1-4.2, 6.1), en un solo commit.
`size:exception` en un solo PR, confirmado por el usuario 2026-09-18 y registrado en el Review
Workload Forecast de `tasks.md`: no hay `feature-branch-chain` ni ramas por unidad. Fase 5
(compuerta humana, tarea 5.1) es explícitamente del usuario, no de este batch. Tareas 6.2 y 6.3
quedan bloqueadas: 6.2 depende de que el usuario corra 5.1; 6.3 depende de que el usuario abra
Unity Editor para regenerar `.meta`.

## Mode

Strict TDD (`Strict TDD Mode: enabled`, instrucción explícita del batch). A diferencia de M9
(que razonó el ciclo RED→GREEN por inspección, sin ejecutar nada, por falta de CLI headless de
Unity), este batch SÍ ejecutó un ciclo RED→GREEN real: sin Unity Editor disponible en este
entorno, se creó un proyecto NUnit de .NET SDK (`dotnet test`, fuera del repositorio, en el
scratchpad de la sesión — nunca en el diff) que compila y corre los **mismos archivos `.cs` reales**
que viven en `Runtime/Harness/` y `Tests/EditMode/Harness/` (via `<Compile Include>` apuntando a
las rutas reales del repo, no copias), junto con los archivos puros de M0/M15/M8 de los que
depende (`Ports.cs`, `Dtos.cs`, `Enums.cs`, `ClinicalCaseId.cs`, `PersonalityId.cs`,
`ClinicalCase.cs`, `ClinicalFactMatcher.cs`, `ScriptedClinicalResponder.cs`,
`RecordingNpcPresenter.cs` — todos verificados como C# puro, sin `UnityEngine`, antes de incluirlos).
NUnit se fijó en la línea 3.x (3.14.0) a propósito, porque Unity's `com.unity.test-framework`
empaqueta NUnit ~3.5, donde `Assert.AreEqual`/`Throws`/`DoesNotThrow`/etc. viven directo en
`Assert` (modelo "clásico"); NUnit 4.x los movió a `NUnit.Framework.Legacy.ClassicAssert`, lo que
habría hecho que este arnés de prueba usara una API distinta a la que Unity realmente ejecuta.

**Esto NO sustituye la compuerta humana de la tarea 5.1.** Es evidencia real y honesta de que la
lógica de producción se comporta como exige la spec quando corre bajo NUnit — pero corrió bajo
.NET SDK puro, no bajo el motor de Unity ni el EditMode Test Runner real, y ninguna prueba tocó
`UnityEngine.TestRunner`/`UnityEditor.TestRunner` (esos ensamblados no existen fuera de Unity). El
Test Runner de Unity sigue siendo la única confirmación que cierra la Fase 5.

## TDD Cycle Evidence

| Task | Behavior | RED (test written first) | GREEN (implementation) | REFACTOR |
|---|---|---|---|---|
| 1.3 | Los 5 espías implementan sus puertos y graban lo que exige la tabla "Testing Strategy" | N/A — infraestructura de prueba, no comportamiento bajo prueba | `EspiasDeArnes.cs`: `EspiaClasificador`, `EspiaReceptividad`, `EspiaDialogo`, `EspiaObjetivo`, `EspiaClinico` | Se corrigió una colisión real de nombres (`ClinicalResponse` namespace vs. tipo `NpcAi.Core.ClinicalResponse`) descubierta por el compilador real, no por inspección — ver Issues Found |
| 2.1-2.3 | Constructor (AD3, 11 casos), `IniciarSesion` (3 efectos, mismo caso M9/M15), selección por semilla (mismo par en 20 sesiones, no-repetición, borde de 1 caso), `Progreso` sin caché | `SessionDirectorSesionTests.cs` escrito primero (16 métodos). `dotnet build` → `CS0246: SessionDirector no encontrado` (RED real, por compilador) | `SessionDirector.cs`: constructor + validación, `IniciarSesion()`/`IniciarSesion(caso, p)`, `SiguienteCaso()`, `Arrancar()`, `Progreso` — SIN `ProcesarTurno`/`ProcesarAccion`/`DeclararTriaje` todavía (orden exacto de design.md: "primero el arranque y la selección") | `dotnet test` → **16/16 en verde** (GREEN real) |
| 3.1-3.2 | Centinelas (`Ninguna`/`Unknown`), silencio de la acción sola, enrutado clínico/social | `SessionDirectorTurnoTests.cs` escrito primero (6 métodos). `dotnet build` → `CS1061: SessionDirector no contiene ProcesarTurno/ProcesarAccion` (RED real) | `ProcesarTurno`/`ProcesarAccion` agregados a `SessionDirector.cs` | `dotnet test` → **22/22 en verde** (16 previas + 6 nuevas, GREEN real, cero regresión) |
| 4.1-4.2 | Nunca `INpcPresenter` en el constructor (reflexión), nunca `RegisterRedFlag`, instanciación/operación sin escena, `DeclararTriaje` pass-through | `SessionDirectorFronterasTests.cs` escrito primero (4 métodos). `dotnet build` → `CS1061: SessionDirector no contiene DeclararTriaje` (RED real) | `DeclararTriaje(string)` agregado a `SessionDirector.cs` | `dotnet test` → **26/26 en verde** (suite completa, GREEN real, cero regresión) |

Nota de proceso: `EspiasDeArnes.cs` (1.3) se escribió y compiló ANTES que cualquier archivo de
prueba de `SessionDirector`, porque los 5 espías no dependen de `SessionDirector` (solo
implementan interfaces de `NpcAi.Core`) — a diferencia de M9, aquí SÍ fue posible aislar esta
pieza y confirmar su compilación de forma independiente antes de escribir la primera prueba RED.

## Work Unit Evidence

| Evidence | Value |
|---|---|
| Focused test command and exact result | `dotnet test` sobre un proyecto NUnit 3.14.0 del .NET SDK (10.0.400) que compila, vía `<Compile Include>`, los archivos reales de `Runtime/Harness/*.cs` + `Tests/EditMode/Harness/*.cs` (sin copiarlos) junto a los archivos puros de M0/M15/M8 de los que dependen. Resultado final, tras las Fases 2-4: **26/26 pruebas en verde, 0 fallidas, 0 omitidas, ~51 ms**. Progresión real registrada arriba (16 → 22 → 26). Esto complementa — **no sustituye** — la compuerta humana de Unity Test Runner (tarea 5.1), que sigue pendiente: `UnityEngine.TestRunner`/`UnityEditor.TestRunner` no se ejercitaron, y el compilador real de Unity (con sus propios `.asmdef`, GUIDs y ensamblados) no corrió. |
| Runtime harness command/scenario and exact result | N/A — C# puro, sin escena, sin VR, sin audio, sin `MonoBehaviour` (tal como fija la tabla de Work Units de `tasks.md`: "N/A — C# puro"). |
| Rollback boundary | Borrar `Runtime/Harness/` (2 archivos) y `Tests/EditMode/Harness/` (6 archivos) completos. Revertir los 2 bloques de `tasks.md` marcados en este commit. M11 vuelve al estado "solo README" que ya está en `main`. Ningún otro módulo referencia `NpcAi.Harness` (M11 es hoja del grafo de dependencias); ningún `.asmdef` ajeno fue tocado. |

## Files Changed

| File | Action | Lines | What Was Done |
|---|---|---|---|
| `Runtime/Harness/NpcAi.Harness.asmdef` | Created | 16 | `references: ["NpcAi.Core"]`, `noEngineReferences: true` (AD12); espejo literal de `Runtime/Receptivity/NpcAi.Receptivity.asmdef` |
| `Runtime/Harness/SessionDirector.cs` | Created | 166 | Único tipo público del módulo: constructor validado (AD2/AD3), selección con semilla (AD6/AD7/AD8), arranque (`Arrancar`, AD5), turno (`ProcesarTurno`/`ProcesarAccion`, AD9/AD10), `DeclararTriaje`, `Progreso` |
| `Tests/EditMode/Harness/NpcAi.Harness.Tests.asmdef` | Created | 27 | `references`: `NpcAi.Core`, `NpcAi.Harness`, `NpcAi.ClinicalResponse`, `NpcAi.Presentation`, `UnityEngine.TestRunner`, `UnityEditor.TestRunner`; `nunit.framework.dll`; `includePlatforms: ["Editor"]` |
| `Tests/EditMode/Harness/EspiasDeArnes.cs` | Created | 156 | 5 espías `internal sealed` (AD11): `EspiaClasificador`, `EspiaReceptividad`, `EspiaDialogo`, `EspiaObjetivo`, `EspiaClinico` |
| `Tests/EditMode/Harness/SessionDirectorSesionTests.cs` | Created | 228 | 16 métodos: 11 de validación AD3, escenarios 7-9 de la spec (secuencia de inicio, semilla, no-repetición + borde de 1 caso), `Progreso` sin caché |
| `Tests/EditMode/Harness/SessionDirectorTurnoTests.cs` | Created | 127 | 6 métodos: los 6 escenarios de "Centinelas", "Silencio de la acción" y "Enrutado clínico/social" |
| `Tests/EditMode/Harness/SessionDirectorFronterasTests.cs` | Created | 101 | 4 métodos: los 2 escenarios de "Fronteras" (uno con aserción por reflexión), "SessionDirector es C# puro", y `DeclararTriaje` (In Scope sin requisito formal, design.md Open Questions) |
| `openspec/changes/2026-09-17-m11-banco-de-pruebas/tasks.md` | Modified | — | Marcadas `[x]` 0.1-0.4, 1.1-1.3, 2.1-2.4, 3.1-3.2, 4.1-4.2, 6.1; 5.1/6.2/6.3 explícitamente NO marcadas, con nota de por qué |
| `openspec/changes/2026-09-17-m11-banco-de-pruebas/apply-progress.md` | Created | — | Este archivo |

Total nuevo en `Runtime/`+`Tests/`: **821 líneas** en 7 archivos (2 `.asmdef` + 5 `.cs`), dentro del
rango estimado (~750-900) del Review Workload Forecast de `tasks.md`.

## Traceability check (spec.md, 12 escenarios)

Los 12 nombres de prueba de la tabla "Trazabilidad" de `spec.md` se usaron literalmente, sin
parafrasear, en los 3 archivos de prueba nuevos:

| # | Nombre exacto de spec.md | Archivo | Estado |
|---|---|---|---|
| 1 | `SoloUtterance_evalua_con_PhysicalAction_Ninguna` | `SessionDirectorTurnoTests.cs` | Verde |
| 2 | `SoloPhysicalAction_evalua_con_IntentResult_Unknown` | `SessionDirectorTurnoTests.cs` | Verde |
| 3 | `AccionFisica_mueve_receptividad` | `SessionDirectorTurnoTests.cs` | Verde |
| 4 | `AccionFisica_no_produce_respuesta_hablada` | `SessionDirectorTurnoTests.cs` | Verde |
| 5 | `Turno_clinico_manejado_no_llama_Generate` | `SessionDirectorTurnoTests.cs` | Verde |
| 6 | `Turno_social_llama_Generate_con_receptividad_actual` | `SessionDirectorTurnoTests.cs` | Verde |
| 7 | `IniciarSesion_dispara_Reset_AssignCase_M9_AssignCase_M15_con_mismo_caso` | `SessionDirectorSesionTests.cs` | Verde |
| 8 | `MismaSemillaYCatalogo_elige_mismo_par` | `SessionDirectorSesionTests.cs` | Verde |
| 9 | `SesionNueva_no_repite_caso_anterior` | `SessionDirectorSesionTests.cs` | Verde |
| 10 | `ConstructorPublico_no_declara_ningun_parametro_INpcPresenter` | `SessionDirectorFronterasTests.cs` | Verde |
| 11 | `SesionCompleta_nunca_incrementa_el_contador_de_bandera_roja_del_espia_local` | `SessionDirectorFronterasTests.cs` | Verde |
| 12 | `Construible_y_operativo_sin_escena_Unity` | `SessionDirectorFronterasTests.cs` | Verde |

Más 14 pruebas de diseño no nombradas por la spec (11 de validación AD3 + `SesionNueva_con_catalogo_de_un_caso_no_lanza_y_repite` + `Progreso_refleja_Progress01_del_objetivo_sin_cachear` + `DeclararTriaje_hace_pass_through_crudo_al_delegado`), declaradas explícitamente en design.md ("Pruebas de diseño que la spec no nombra, se agregan y se declaran"). Total: 26.

## Deviations from Design

1. **`EspiaClinico` necesitó "Core." explícito para su tipo de retorno**, exactamente el mismo
   problema que `ScriptedClinicalResponder.cs` ya documenta en un comentario: el namespace
   `NpcAi.ClinicalResponse` colisiona con el tipo `NpcAi.Core.ClinicalResponse` para CUALQUIER
   código anidado bajo `NpcAi` (incluido `NpcAi.Harness.Tests`), no solo para código dentro del
   propio namespace `NpcAi.ClinicalResponse`. `design.md` no lo menciona para `EspiaClinico`
   porque no llegó a escribir su firma completa; se corrigió con la misma convención ya establecida
   en el repositorio (`Core.ClinicalResponse`), y se descubrió con el compilador real del arnés
   de prueba, no por inspección — ver Issues Found.
2. **Sin archivos `.meta` para los 9 elementos nuevos** (2 carpetas: `Runtime/Harness/`,
   `Tests/EditMode/Harness/`; 7 archivos: 2 `.asmdef` + 5 `.cs`). Mismo patrón ya documentado por
   M9 y M7: la frontera de este batch no incluye `*.meta`, y escribirlos a mano en vez de dejar
   que Unity Editor los autogenere fue explícitamente descartado por instrucción del usuario
   (tarea 0.4/6.3). Pendiente de un commit de seguimiento cuando el usuario abra el Editor.
3. **Se usó un arnés `dotnet test` fuera del repositorio para obtener evidencia RED→GREEN real**,
   en vez de razonar el ciclo por inspección (patrón que usó M9 por falta de acceso a CLI/headless
   de Unity). Esto no está en `design.md` porque `design.md` no anticipa el entorno de ejecución
   del agente; se documenta aquí como método, no como desviación de comportamiento. El arnés vive
   enteramente en el scratchpad de la sesión (`D:\cctmp\claude\...\scratchpad\m11-tdd\`), nunca en
   el repositorio, y no aparece en `git status`.

## Issues Found

1. **Colisión de nombres real, no hipotética**: al compilar `EspiaClinico : IClinicalResponder`
   por primera vez, el compilador de C# resolvió el identificador `ClinicalResponse` como el
   *namespace* `NpcAi.ClinicalResponse` (porque `NpcAi.Harness.Tests` está anidado bajo `NpcAi`,
   igual que `NpcAi.ClinicalResponse`) en vez del *tipo* `NpcAi.Core.ClinicalResponse` traído por
   `using NpcAi.Core;` — produciendo `CS0118` y luego `CS0738` (la firma de `Respond` no
   coincidía con la del puerto). Se corrigió calificando `Core.ClinicalResponse` explícitamente,
   la misma convención que ya usa `ScriptedClinicalResponder.cs`. Verificado por lectura completa
   de `Runtime/Harness/SessionDirector.cs` y `Tests/EditMode/Harness/EspiasDeArnes.cs` que ningún
   otro tipo del contrato tiene un nombre que colisione con un namespace de módulo existente.
2. Se verificó, leyendo los 2 archivos reales completos (no solo confiando en `design.md`), que
   `Runtime/Harness/SessionDirector.cs` no contiene ningún `using UnityEngine`, `Debug.Log` ni
   herencia de `MonoBehaviour` — y adicionalmente, `grep -rn "UnityEngine\|Debug.Log\|MonoBehaviour"`
   sobre `Runtime/Harness/` y `Tests/EditMode/Harness/` completos solo encuentra menciones dentro
   de comentarios XML-doc y la referencia esperada a `UnityEngine.TestRunner` en el `.asmdef` de
   pruebas — ningún `using` ni llamada real.
3. Ninguno más de diseño: los 12 escenarios de `spec.md` se satisficieron con el pseudocódigo
   exacto de `design.md`, sin necesidad de desviar ninguna decisión de comportamiento (AD1-AD13).

## COMPUERTA HUMANA — pendiente

- **Quién debe correrla**: el usuario, en Unity Editor Test Runner (pestaña EditMode), rama
  `feat/m11-01-session-director`.
- **Qué debe confirmar**: las 3 clases de prueba nuevas (`SessionDirectorSesionTests`,
  `SessionDirectorTurnoTests`, `SessionDirectorFronterasTests`, 26 métodos en total) en verde,
  junto con el resto de la suite del proyecto (para descartar cualquier regresión en otros
  módulos, cosa que este arnés de `dotnet test` no puede verificar: no compiló el resto del
  paquete).
- **Por qué no la sustituye la evidencia de este batch**: el arnés `dotnet test` corrió sobre
  .NET SDK puro, con NUnit 3.14.0 standalone — nunca sobre el compilador de Unity, sus `.asmdef`
  reales (con sus propias reglas de `defineConstraints`/`versionDefines`), ni
  `UnityEngine.TestRunner`/`UnityEditor.TestRunner`. Es evidencia fuerte de que la LÓGICA es
  correcta; no es evidencia de que el PAQUETE compile dentro de Unity.
- **Nota para 0.4/6.3**: al abrir el Editor, Unity generará los 9 `.meta` faltantes
  automáticamente; commitearlos en un commit de seguimiento separado, igual que hizo M7.

## Remaining Tasks

- [ ] 5.1 (Fase 5): compuerta humana, Unity Test Runner > EditMode > Run All — NO corrida por el
  agente (fuera de su alcance por instrucción explícita del batch).
- [ ] 6.2 (Fase 6): checklist "Antes de mergear" — bloqueado en su primer punto (depende de 5.1).
- [ ] 6.3 (Fase 6): commit de seguimiento para los `.meta` — requiere que el usuario abra Unity Editor.

## Estado final al cierre (2026-09-18)

19/19 tareas completadas. La compuerta humana de la tarea 5.1 fue confirmada en verde por el usuario el 2026-09-18; el usuario confirmó después que el total fue de 26 pruebas (W1 cerrado). Los `.meta` fueron generados en el commit de seguimiento `a4118f8`. El PR #44 fue mergeado en `5b78ce7` el 2026-09-18T16:12:38Z. Este archivo es un snapshot de la sesión anterior (17/19); el apartado anterior es histórico y este cierre prevalece sobre esas cifras.

## Workload / PR Boundary

- Mode: `size:exception` en un solo PR (confirmado por el usuario 2026-09-18, registrado en el
  Review Workload Forecast de `tasks.md`) — sin `feature-branch-chain`, sin ramas por unidad.
- Current work unit: Fases 0-4 y 6.1 de un total de 6 fases del cambio (todo lo automatizable de
  PR1 salvo la compuerta humana de la Fase 5 y las tareas de cierre que dependen de ella).
- Boundary: arranca desde M11 "solo README" (estado actual en `main`) y termina con
  `SessionDirector` completo (constructor, sesión, turno, fronteras) + su suite de 26 pruebas,
  autónomo y reversible sin tocar ningún otro módulo.
- Estimated review budget impact: `tasks.md` estimaba ~750-900 líneas; el diff real de
  `Runtime/`+`Tests/` es de 821 líneas en 7 archivos nuevos (ver `git diff --cached --stat` en el
  mensaje de retorno de este batch al orquestador). Dentro del rango estimado.

## Status

17/19 tareas de este cambio completas (todas las de Fases 0-4 más 6.1). Fase 5 (compuerta humana)
y las 2 tareas restantes de Fase 6 (bloqueadas por 5.1 y por la apertura del Editor) quedan
explícitamente pendientes, fuera del alcance de este batch. `sdd-apply` no continúa: el siguiente
paso es que el usuario corra la compuerta humana de la tarea 5.1.
