# Apply Progress: M9 — Escenario Sala de Emergencia (Fases 0-3: lector de `clave` + objetivo real)

## Change

`2026-09-17-m9-escenario-emergencia`

## Scope of this batch

Fases 0-3 solamente (tareas 0.1-0.4, 1.1-1.3, 2.1, 3.1-3.5). Este cambio es `size:exception` en
un solo PR (decidido con el usuario 2026-09-17, registrado en el Review Workload Forecast de
`tasks.md`): no hay `feature-branch-chain` ni ramas por unidad — todo el trabajo automatizable
llega en una sola rama (`feat/m9-escenario-emergencia`, ya activa). Fase 4 (compuerta humana,
tarea 4.1) es explícitamente del usuario, no de este batch. Fases 5 (docs) y 6 (cierre) NO
tocadas: quedan para después de que el usuario confirme el Test Runner en verde.

## Mode

Strict TDD (`Strict TDD Mode: enabled`, instrucción explícita del batch). Entorno sin acceso a
CLI/headless Unity (solo Unity Editor GUI, per instrucción del proyecto): el agente escribió
pruebas y producción razonando el ciclo RED→GREEN por inspección, sin ejecutar el Test Runner. La
confirmación real en verde queda como compuerta humana (tarea 4.1), igual que en M1/M2/M7.

## TDD Cycle Evidence

| Task | Behavior | RED (test written first) | GREEN (implementation) | REFACTOR |
|---|---|---|---|---|
| 1.1/1.2/1.3 | `TriageKeyLoader.TryParse` valida el bloque `clave` sin exigir `id`/`paciente`/mínimo de `hechos` (AD3) | `TriageKeyLoaderTests.cs` escrito primero (8 métodos, 10 ejecuciones) | `TriageKey.cs` (POCO) + `TriageKeyLoader.cs` (`TryParse` + `Raw*` privados) | N/A — molde ya fijado por `ClinicalCaseLoader`, sin necesidad de reestructurar |
| 3.1 | Conformidad con `ScenarioObjectiveContract` (7 pruebas heredadas) vía renormalización sin caso | `TriageScenarioObjectiveTests.cs` escrito primero, hereda la base sin modificarla | `TriageScenarioObjective()` (constructor sin parámetros) | N/A |
| 3.2 | Mezcla receptividad/clínica con caso asignado: techos exactos 0.3/0.7/1.0, crédito proporcional, racha saturada, reversibilidad | `TriageProgresoTests.cs` escrito primero (10 métodos) | `Progress01`/`IsComplete`/`Notify` en `TriageScenarioObjective.cs` | N/A |
| 3.3 | Superficie aditiva: `AssignCase`/`RegisterRedFlag`/`DeclareTriage`/`Reset`, todos los bordes | `TriageSuperficieAditivaTests.cs` escrito primero (16 métodos, 17 ejecuciones) | `AssignCase`/`RegisterRedFlag`/`DeclareTriage`/`Reset` en `TriageScenarioObjective.cs` | N/A |

Nota de proceso (mismo patrón que M7 PR1/PR3): `TriageKeyLoaderTests.cs` referencia `TriageKey`/
`TriageKeyLoader`, y `TriageScenarioObjectiveTests.cs`/`TriageProgresoTests.cs`/
`TriageSuperficieAditivaTests.cs` referencian `TriageScenarioObjective`/`TriageObjectiveSettings`
— ninguno de los 4 archivos RED pudo compilar/fallar de forma aislada antes de que existieran
TODOS los archivos GREEN correspondientes, restricción del compilador de C#/Unity (unidad de
compilación por ensamblado), no un atajo del proceso. Los 4 archivos RED se razonaron y
escribieron completos, contra el pseudocódigo exacto de `design.md` (incluida la tabla
"Aritmética" para los techos 0.3/0.7/1.0), ANTES de escribir una sola línea de
`TriageKey.cs`/`TriageKeyLoader.cs`/`TriageObjectiveSettings.cs`/`TriageScenarioObjective.cs`.

## Work Unit Evidence

| Evidence | Value |
|---|---|
| Focused test command and exact result | Unity Editor Test Runner > EditMode > Run All, filtrado a `TriageKeyLoaderTests` + `TriageScenarioObjectiveTests` + `TriageProgresoTests` + `TriageSuperficieAditivaTests`: 34 métodos de prueba propios (8 + 0 + 10 + 16; `TriageScenarioObjectiveTests` no declara métodos propios, hereda 7 de `ScenarioObjectiveContract`), 44 ejecuciones totales contando los 7 heredados y los `TestCase` parametrizados (10 + 7 + 10 + 17). NO ejecutado por el agente — sin acceso a CLI/headless Unity en este entorno. Pendiente compuerta humana (tarea 4.1, ver abajo). |
| Runtime harness command/scenario and exact result | N/A — C# puro, JSON en memoria, sin escena, sin VR, sin audio, sin `MonoBehaviour` (tal como fija la tabla de Work Units de `tasks.md`: "N/A — C# puro"). |
| Rollback boundary | Borrar `Runtime/Scenarios/Emergency/{TriageKey,TriageKeyLoader,TriageScenarioObjective}.cs`, `Runtime/Scenarios/Emergency/Config/TriageObjectiveSettings.cs` y los 4 archivos nuevos de `Tests/EditMode/Scenarios/Emergency/`. M9 vuelve a "solo doble" (`ScriptedScenarioObjective` no se toca). Ningún otro módulo se ve afectado. |

## Verificación aritmética por inspección (sustituto de ejecución real)

Con los pesos por defecto (`PesoDeReceptividad=0.3f`, `PesoDeBanderasRojas=0.7f`,
`RachaParaReceptividadPlena=5`), recalculado a mano contra el pseudocódigo exacto de
`design.md`:

| Estado | `r` | `f` | `t` | `Progress01` esperado | Coincide con "Aritmética" de `design.md` |
|---|---|---|---|---|---|
| Sin clave, sin racha | 0 | — | — | `0f` | Sí (exacto, sin tolerancia) |
| Con clave, racha plena, sin banderas/triaje | 1 | 0 | 0 | `0.3f` | Sí |
| Con clave, sin racha, todas las banderas + triaje correcto | 0 | 1 | 1 | `0.7f` | Sí |
| Con clave, racha plena, todas las banderas + triaje correcto | 1 | 1 | 1 | `1.0f` (`IsComplete`) | Sí |
| Con clave, sin racha, 2 de 4 banderas | 0 | 0.5 | 0 | `0.245f` | Sí (0.7·0.5·0.7) |

Los 4 techos exactos son aritmética `float` exacta (AD4: `0.3f + 0.7f == 1.0f` en IEEE-754
binario32), no dependen de la tolerancia `1e-4` que sí usan las pruebas por consistencia con el
resto de la suite.

## Files Changed

| File | Action | What Was Done |
|---|---|---|
| `Runtime/Scenarios/Emergency/TriageKey.cs` | Created | POCO del bloque `clave` (AD1); nulos → `string.Empty`/`Array.Empty<string>()`, espejo de `Hecho`/`ClinicalCase` |
| `Runtime/Scenarios/Emergency/TriageKeyLoader.cs` | Created | `TryParse` + `RawCase`/`RawClave` `[Serializable]` privados anidados que declaran únicamente `clave` (AD1); valida `triajeEsperado` contra `{"I".."V"}` con `Array.IndexOf`; único archivo del módulo que referencia `UnityEngine` (`JsonUtility`) |
| `Runtime/Scenarios/Emergency/Config/TriageObjectiveSettings.cs` | Created | Dos fracciones complementarias (`PesoDeReceptividad`/`PesoDeBanderasRojas`, AD4) + `PesoClinico`/`PesoDeTriaje` calculadas; `RachaParaReceptividadPlena` (AD7); `Clamp01`/`Math.Max(1, ...)` en el constructor; namespace `NpcAi.Scenarios.Emergency` (no `.Config`, ver Deviations) |
| `Runtime/Scenarios/Emergency/TriageScenarioObjective.cs` | Created | `IScenarioObjective` real + superficie aditiva: 2 constructores (AD2, ambos `public`), `Notify`/`AssignCase`/`RegisterRedFlag`/`DeclareTriage`/`Reset` (AD6/AD8/AD9/AD10/AD11), `Progress01`/`IsComplete` derivados en lectura (AD5), `HasKey`/`RedFlagsFound`/`RedFlagCount`/`ExpectedClosure` |
| `Tests/EditMode/Scenarios/Emergency/TriageKeyLoaderTests.cs` | Created | 8 métodos (10 ejecuciones): caso feliz con los 4 campos poblados, `clave` ausente, JSON malformado, JSON nulo/vacío/blanco, `triajeEsperado` fuera de rango (`TestCase` × 3: "VI", "", "2"), `banderasRojas` ausente y `[]` explícito, `hechos` insuficientes (AD3) |
| `Tests/EditMode/Scenarios/Emergency/TriageScenarioObjectiveTests.cs` | Created | Gemela de contrato: `CreateSubject() => new TriageScenarioObjective()`, hereda `ScenarioObjectiveContract` sin modificarla |
| `Tests/EditMode/Scenarios/Emergency/TriageProgresoTests.cs` | Created | 10 métodos: techos exactos 0.3/0.7/1.0, crédito proporcional 2/4 (0.245), racha monótona hasta su techo, `Worsened` reinicia a 0, `Notify(default)` y `From==To` no-op, reversión por `Worsened` y por triaje incorrecto tras acertar |
| `Tests/EditMode/Scenarios/Emergency/TriageSuperficieAditivaTests.cs` | Created | 16 métodos (17 ejecuciones): `RegisterRedFlag` idempotente/fuera de rango/antes de `AssignCase`, `RedFlagCount` con y sin clave, `DeclareTriage` correcto/normalizado (`TestCase` `"ii"`/`" II "`)/sobrescrito/antes de `AssignCase`, `cargarJson` nulo/que lanza/caso sin `clave`, `AssignCase(ClinicalCaseId.None)`, reasignación descarta progreso previo, `Reset()` recién-construido e idempotente |
| `openspec/changes/2026-09-17-m9-escenario-emergencia/tasks.md` | Modified | Marcadas `[x]` 0.1-0.4, 1.1-1.3, 2.1, 3.1-3.5 con comentarios de evidencia |

## Deviations from Design

1. **`TriageObjectiveSettings` vive en el namespace `NpcAi.Scenarios.Emergency`, no
   `NpcAi.Scenarios.Emergency.Config`.** No es una desviación de `design.md` en sentido estricto
   — la propia sección "Interfaces / Contracts" del diseño lo dice explícitamente ("el `Config/`
   es carpeta, no sub-namespace — patrón de `Runtime/VrInput/Config/`") — pero se documenta aquí
   porque es la misma decisión que M7 PR2 tuvo que registrar como desviación explícita en su
   momento (`VrInputSettings`/`VrInputSettingsAsset` en `NpcAi.VrInput`, no `.Config`). Carpeta
   física y namespace de C# no tienen que coincidir; ningún otro módulo referencia estos tipos
   (regla dura 3).
2. **Sin archivos `.meta` para los 9 elementos nuevos** (5 en `Runtime/`, incluida la carpeta
   `Config/`, y 4 en `Tests/`). `design.md` (File Inventory) advierte explícitamente evitar el
   defecto que M7 tuvo que corregir en dos commits `chore` separados
   (`chore(m7): agregar .meta faltantes de los archivos de PR1/PR2`), pero la frontera de este
   batch (instrucción explícita del usuario) restringe el diff a `Runtime/Scenarios/Emergency/
   *.cs` (nuevo), `Runtime/Scenarios/Emergency/Config/*.cs` (nuevo), `Tests/EditMode/Scenarios/
   Emergency/*.cs` (nuevo) y `tasks.md` — ninguno de esos patrones incluye `*.cs.meta` ni la
   carpeta `Config/` en sí. Se generaron y verificaron 9 GUIDs candidatos sin colisión contra los
   ~530 `.meta` existentes del repositorio (por si el usuario prefiere aplicarlos manualmente),
   pero NO se escribieron los archivos: escribirlos habría cruzado la frontera de diff explícita
   de este batch. Precedente real del propio repositorio: los 2 commits `chore(m7)` citados arriba
   muestran que este proyecto ya resuelve los `.meta` faltantes en un commit posterior separado,
   normalmente después de que el usuario abre Unity Editor (que los autogenera con el formato
   completo real, más fiable que un `.meta` de 2 líneas escrito a mano). Recomendado: abrir Unity
   Editor antes o durante la compuerta humana (tarea 4.1) para que autogenere estos 9 `.meta`, y
   commitearlos junto con (o inmediatamente después de) la confirmación del Test Runner.
3. **`TriageKeyLoaderTests`, `TriageProgresoTests` y `TriageSuperficieAditivaTests` tienen más
   métodos que los nombres literales listados en la tabla de Trazabilidad de `spec.md`** (que
   marca sus nombres como "(propuesto)", no cerrados). Los métodos adicionales fijan
   comportamiento que `design.md` declara explícitamente (p. ej. `banderasRojas` ausente vs. `[]`
   explícito como dos casos separados, AD11; `RedFlagCount` con y sin clave, justificado en la
   tarea 3.4 del propio `tasks.md`) y que el conjunto mínimo de nombres no cubre por separado.
   Mismo patrón que la Desviación 4 de M7 PR1.

## Issues Found

Ninguno de diseño. Se verificó por inspección, antes de dar las Fases 0-3 por terminadas, que:
`Runtime/Scenarios/Emergency/TriageKey.cs`, `Config/TriageObjectiveSettings.cs` y
`TriageScenarioObjective.cs` no contienen ningún `using UnityEngine` ni llamada a `Debug.Log`
(confirmado por lectura completa de los 3 archivos); solo `TriageKeyLoader.cs` referencia
`UnityEngine` (vía `JsonUtility`), exactamente como exige la frontera del batch. Se verificaron
también, leyendo los archivos reales (no solo confiando en la afirmación de `design.md`), que
`Runtime/Scenarios/Emergency/NpcAi.Scenarios.Emergency.asmdef` y
`Tests/EditMode/Scenarios/Emergency/NpcAi.Scenarios.Emergency.Tests.asmdef` ya tenían todas las
referencias necesarias (`NpcAi.Core`, `NpcAi.Core.Channels` en el runtime; `NpcAi.Core`,
`NpcAi.Core.Tests`, `NpcAi.Scenarios.Emergency` en las pruebas) — a diferencia del gap real que
M7 PR3 encontró en su propio `.asmdef` de pruebas, aquí no hizo falta ningún cambio.

## COMPUERTA HUMANA PENDIENTE (tarea 4.1)

- **Quién debe correrla**: el usuario, en Unity Editor Test Runner (pestaña EditMode), rama
  `feat/m9-escenario-emergencia`.
- **Qué debe confirmar**: las 4 clases de prueba nuevas de este batch (`TriageKeyLoaderTests`,
  `TriageScenarioObjectiveTests`, `TriageProgresoTests`, `TriageSuperficieAditivaTests`; 34
  métodos propios + 7 heredados del contrato, 44 ejecuciones totales contando `TestCase`) en
  verde, junto con el resto de la suite completa del proyecto (en particular
  `ScriptedScenarioObjectiveTests`, que NO debe verse afectada: el doble no se tocó).
- **No aplica compuerta física ni de hardware.** A diferencia de M1 (micrófono), M2 (modelo
  entrenado) y M7 (Quest 3), M9 es lógica de escenario del lado del servidor: cero dependencia de
  VR, XR, audio, `MonoBehaviour` o escena. Esta es la ÚNICA confirmación humana que necesita todo
  el cambio `2026-09-17-m9-escenario-emergencia`.
- **Antes de correr el Test Runner**: se recomienda abrir Unity Editor para que autogenere los 9
  archivos `.meta` faltantes (ver Deviations #2) — sin ellos, Unity puede fallar en reconocer los
  scripts nuevos o generarlos con GUIDs distintos a los ya reservados manualmente en este batch.
  Si Unity los autogenera, no hace falta usar los GUIDs propuestos en este documento.
- **Resultado**: sin registrar todavía — el usuario debe correr el Test Runner y registrar aquí
  el total de pruebas y cualquier ajuste necesario (p. ej. un `.asmdef` con una referencia
  faltante, que solo un compilador real detecta — patrón recurrente en M2/M7 de esta sesión).

## Remaining Tasks

- [ ] Tarea 4.1 (compuerta humana): Unity Test Runner > EditMode > Run All — pendiente, ver
  sección de arriba.
- [ ] Fase 5 (`5.1`/`5.2`): `Docs/MODULES.md` (sección M9) y confirmación de frontera de diff
  completa — NO iniciada, fuera de alcance de este batch.
- [ ] Fase 6 (cierre): `git add`/commit/PR, checklist "Antes de mergear", promoción del spec a
  `openspec/specs/` y archivado — NO iniciada; depende de que la compuerta humana 4.1 confirme
  verde primero.

## Workload / PR Boundary

- Mode: `size:exception` en un solo PR (decidido 2026-09-17, registrado en el Review Workload
  Forecast de `tasks.md`) — sin `feature-branch-chain`, sin ramas por unidad.
- Current work unit: Fases 0-3 de un total de 6 fases del cambio.
- Boundary: arranca desde M9 "solo doble" (`ScriptedScenarioObjective`, sin cambios) y termina
  con `TriageKeyLoader` + `TriageScenarioObjective` + su superficie aditiva completos y sus 4
  archivos de prueba, autónomo y reversible sin tocar ningún otro módulo. Fases 4-6 (compuerta
  humana, docs, cierre) quedan explícitamente fuera de este batch por instrucción del usuario.
- Estimated review budget impact: `tasks.md` estimaba ~295 líneas (Unit 1: lector) + ~545 líneas
  (Unit 2: objetivo + superficie aditiva) ≈ 840 líneas para las Fases 1-3; el stat real de
  `git diff --cached --stat` se reporta en el mensaje de retorno de este batch al orquestador.

## Status

12/12 tareas automatizables de las Fases 0-3 completas (0.1-0.4, 1.1-1.3, 2.1, 3.1-3.5),
**razonadas correctas por inspección** contra el pseudocódigo exacto de `design.md` — ninguna
confirmada todavía en Test Runner real (compuerta humana 4.1, pendiente). `sdd-apply` entrega el
control al orquestador: no continúa con la Fase 5 hasta que el usuario registre el resultado de
la tarea 4.1 en este documento.
