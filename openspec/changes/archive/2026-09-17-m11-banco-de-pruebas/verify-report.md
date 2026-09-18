```yaml
schema: gentle-ai.verify-result/v1
evidence_revision: sha256:1666771f21f1afc099dda1b6c55d71987499b9787f492c15e1d7bb0547913b80
verdict: pass_with_warnings
blockers: 0
critical_findings: 0
requirements: 7/7
scenarios: 12/12
test_command: rg -c --sort path "^\s*\[Test\]" Tests/EditMode/Harness --glob "SessionDirector*Tests.cs"
test_exit_code: 0
test_output_hash: sha256:7e641bb5ba9a47af2d43233b19f495e75c8591c6f4a2ff9bb28c72dd5d156b5e
build_command: git diff --no-renames --numstat 5b78ce7^1 5b78ce7 -- Runtime Tests Data Docs package.json 'Samples~'
build_exit_code: 0
build_output_hash: sha256:d03ad2a197534241baeb058bb1c9ef0202a03e50c21a4260bcf04b91ef984982
```

## Verification Report

**Change**: 2026-09-17-m11-banco-de-pruebas
**Version**: N/A (spec nueva `banco-de-pruebas-m11`, solo PR1: `NpcAi.Harness.SessionDirector`)
**Mode**: Strict TDD (instrucción del orquestador). Runner del proyecto: Unity Test Runner (GUI), sin CLI ni batchmode cableado (Engram `sdd/pln_upm/testing-capabilities`, #20).
**Fecha**: 2026-09-18
**Código verificado**: HEAD `e59f1b7`, árbol idéntico a `origin/main` (PR #44 fusionado, merge `5b78ce7`).

### Alcance y base de evidencia (leer primero)

Esta verificación es ESTÁTICA y basada en evidencia. **No ejecutó ninguna prueba ni ninguna compilación**: no existe CLI de Unity en el repositorio, `openspec/config.yaml` (`rules.verify.test_command`) es una plantilla no verificada y la instrucción prohibía lanzar Unity o armar un arnés dotnet. Ninguna afirmación de "pruebas en verde" de este informe proviene de una ejecución propia.

Evidencia de ejecución existente (la única) y sus límites:

| Fuente | Qué afirma | Límite |
|---|---|---|
| (a) Compuerta humana, `tasks.md` 5.1 | "Confirmado por el usuario el 2026-09-18: todo en verde" en Unity Test Runner > EditMode > Run All | Atestación cualitativa: no registra el total de pruebas ni una salida; no fue reobservada por esta verificación (ver W1). |
| (b) `apply-progress.md`, arnés `dotnet test` (NUnit 3.14.0) de la sesión de apply | Progresión RED -> GREEN real 16 -> 22 -> 26 sobre los mismos `.cs` del repositorio | El arnés vive fuera del repositorio (scratchpad de una sesión anterior); no localicé un log de resultados persistido y no lo ejecuté. No sustituye a Unity (otro compilador, sin `.asmdef` reales, sin `UnityEngine.TestRunner`). |

Cadena de custodia verificada con git (solo lectura):

- `0aa37d3` (2026-09-18 10:43:07 -0500) es el único commit que toca `.cs`/`.asmdef` de `Runtime/Harness/` y `Tests/EditMode/Harness/`.
- `a4118f8` (10:56:12) añade únicamente los 9 `.meta` (generados por el Editor al correr el Test Runner, según `tasks.md` 6.3). `b714c15` (10:56:43) cierra `tasks.md` con la compuerta confirmada. `e59f1b7` (10:57:33) es el handoff.
- `git diff --stat origin/main HEAD` y `git diff --stat 5b78ce7 HEAD -- Runtime Tests` están vacíos.
- Conclusión: los bytes de código verificados son los mismos sobre los que se corrió la compuerta humana; ningún `.cs`/`.asmdef` cambió después de `0aa37d3`.

Sobre los campos `test_*` y `build_*` del envelope: **no son una ejecución de pruebas ni una compilación**. Siguiendo la convención de los verify-report previos del proyecto (p. ej. `sdd/2026-09-09-m0-puerto-respuesta-clinica/verify-report`), registran dos sondas estáticas que sí se ejecutaron aquí, con código de salida y hash reales:

- Sonda T (inventario de pruebas, no ejecución). Salida exacta (el hash se calcula sobre esta salida más un salto de línea final):
  ```text
  Tests/EditMode/Harness\SessionDirectorFronterasTests.cs:4
  Tests/EditMode/Harness\SessionDirectorSesionTests.cs:16
  Tests/EditMode/Harness\SessionDirectorTurnoTests.cs:6
  ```
- Sonda B (alcance del diff, no compilación): `git diff --no-renames --numstat 5b78ce7^1 5b78ce7 -- Runtime Tests Data Docs package.json 'Samples~'` lista solo 16 archivos, todos bajo `Runtime/Harness*` y `Tests/EditMode/Harness*` (7 de código = 821 líneas añadidas, 0 eliminadas; 9 `.meta` = 40 líneas). Nada en `Runtime/Core`, `Runtime/CoreChannels`, `Data`, `Docs`, `Samples~` ni `package.json`.
- `evidence_revision` = sha256 del listado ordenado de `sha256sum` de los 7 archivos de código (bytes del árbol de trabajo, finales de línea CRLF):
  ```text
  55d0f016902eb4969553e12238864c5b3eae2f71846de1b838308a29bb198236 *Runtime/Harness/NpcAi.Harness.asmdef
  d79de3070f029ef35c85b0664762f932991c0e0d2342de746e56d0a1aad82b65 *Runtime/Harness/SessionDirector.cs
  fb6b2d19b5c914541058063fecc14b84ce9160c9a5a7e6c085799a823c806f98 *Tests/EditMode/Harness/EspiasDeArnes.cs
  deb51a62cdadeae7b88d991743e8f852aa9b730fa1fab91f8ca4940bfa99e3f5 *Tests/EditMode/Harness/NpcAi.Harness.Tests.asmdef
  e4b3160ebea6716502702fd9cc4f93b6ee83d979a641d3f1e9645d8381a4e4ee *Tests/EditMode/Harness/SessionDirectorFronterasTests.cs
  7022823df9cdd06aa6e20c91b615d5129017a934da663b57a938b447685e0806 *Tests/EditMode/Harness/SessionDirectorSesionTests.cs
  3cdc309c34ab0ed7d8c2bde6a3593ba9d29be9affe00aa05bbea5245c19dae38 *Tests/EditMode/Harness/SessionDirectorTurnoTests.cs
  ```

En la matriz, `COMPLIANT*` significa: existe prueba cubriente y la ejecución en verde está atestada por la compuerta humana, pero **no fue reobservada** por esta verificación.

### Completeness

| Metric | Value |
|--------|-------|
| Tasks total | 19 |
| Tasks complete | 19 |
| Tasks incomplete | 0 |

Conteo propio sobre `tasks.md`: 19 casillas marcadas, 0 sin marcar. La tarea 6.2 conserva el texto "rama al día con main: pendiente de verificar" (nit de documentación, ver S6; el orquestador la verificó al abrir el PR: 0 commits detrás de main, 7 por delante). Conteo propio sobre `spec.md`: **7 requisitos y 12 escenarios** (no 13; el desajuste que anticipa `tasks.md` "Notas" es del recuento de la orquestación, no de la spec).

### Build & Tests Execution

**Build**: no ejecutado por esta verificación. La compilación de Unity (Roslyn) es implícita en el Editor; su evidencia indirecta es la compuerta humana y la generación de los `.meta` por el Editor.

**Tests**: no ejecutados por esta verificación. Inventario estático: 26 métodos `[Test]` (16 + 6 + 4), de los cuales 12 corresponden a los escenarios de la spec y 14 son pruebas de diseño declaradas en `design.md` (11 de validación del constructor, `SesionNueva_con_catalogo_de_un_caso_no_lanza_y_repite`, `Progreso_refleja_Progress01_del_objetivo_sin_cachear`, `DeclararTriaje_hace_pass_through_crudo_al_delegado`). Sin `[Ignore]`, `[Explicit]`, `Assert.Ignore`, `Assert.Inconclusive`, `[Category]`, `[Retry]`, `[Repeat]`, `[Timeout]` ni `[UnityTest]` (búsqueda con rg sobre `Tests/EditMode/Harness`, sin coincidencias).

**Coverage**: no disponible (sin herramienta de cobertura, Engram #20).

### Spec Compliance Matrix (escenario -> prueba -> código)

Rutas relativas a la raíz del paquete. `SD` = `Runtime/Harness/SessionDirector.cs`; `TT` = `Tests/EditMode/Harness/SessionDirectorTurnoTests.cs`; `ST` = `...SessionDirectorSesionTests.cs`; `FT` = `...SessionDirectorFronterasTests.cs`; `EA` = `...EspiasDeArnes.cs`.

| # | Requisito | Escenario | Prueba (archivo:líneas) | Comportamiento en código | Resultado |
|---|---|---|---|---|---|
| 1 | Centinelas para señales parciales | Solo llega Utterance | `TT:43-52` `SoloUtterance_evalua_con_PhysicalAction_Ninguna` | `SD:136` `_m4.Evaluate(intent, PhysicalAction.Ninguna)` | COMPLIANT* (reserva W4) |
| 2 | Centinelas para señales parciales | Solo llega PhysicalAction | `TT:55-67` `SoloPhysicalAction_evalua_con_IntentResult_Unknown` | `SD:152` `Evaluate(IntentResult.Unknown(), accion)`; `ProcesarAccion` no llama `Classify` | COMPLIANT* |
| 3 | Silencio de la acción física sola | La acción mueve receptividad | `TT:72-80` `AccionFisica_mueve_receptividad` (doble local `EA:56-65`) | `SD:152` | COMPLIANT* |
| 4 | Silencio de la acción física sola | La acción no produce habla | `TT:83-94` `AccionFisica_no_produce_respuesta_hablada` | `SD:154` `return null`; el método no referencia `_m15` ni `_m6` | COMPLIANT* (S4) |
| 5 | Enrutado clínico/social | Turno clínico manejado | `TT:99-109` `Turno_clinico_manejado_no_llama_Generate` (`ScriptedClinicalResponder` real) | `SD:139-141` devuelve `clin.Reply` sin alterar | COMPLIANT* (S1) |
| 6 | Enrutado clínico/social | Turno social (no clínico) | `TT:112-125` `Turno_social_llama_Generate_con_receptividad_actual` | `SD:142` `_m6.Generate(PersonalidadActual, _m4.Current, intent)` | COMPLIANT* (reservas W4, W5) |
| 7 | Secuencia de inicio de sesión | Los tres efectos con el mismo caso | `ST:146-167` `IniciarSesion_dispara_Reset_AssignCase_M9_AssignCase_M15_con_mismo_caso` | `SD:97-100` -> `Arrancar` `SD:118-127`: un único `caso` a `_asignarCaso` y `_m15.AssignCase` | COMPLIANT* (S2) |
| 8 | Selección reproducible por semilla | Misma semilla y catálogo repiten el par | `ST:172-185` `MismaSemillaYCatalogo_elige_mismo_par` (20 sesiones) | ctor `SD:85` `new Random(semilla)`; `SD:89-94`, `SD:105-115` | COMPLIANT* |
| 9 | Selección reproducible por semilla | No se repite el caso anterior | `ST:188-199` `SesionNueva_no_repite_caso_anterior` (20 semillas) + borde `ST:202-211` | lista filtrada `SD:107-114`; `_ultimoCaso` `SD:122` | COMPLIANT* |
| 10 | Fronteras que SessionDirector no cruza | Nunca reproduce directamente | `FT:41-57` `ConstructorPublico_no_declara_ningun_parametro_INpcPresenter` (reflexión) | ctor `SD:55-65` sin parámetro `INpcPresenter`; ninguna referencia a `INpcPresenter`/`Play` en `Runtime/Harness/` | COMPLIANT* (reserva W6) |
| 11 | Fronteras que SessionDirector no cruza | Nunca registra banderas rojas | `FT:60-71` `SesionCompleta_nunca_incrementa_el_contador_de_bandera_roja_del_espia_local` | `RegisterRedFlag` ausente de `Runtime/Harness/`; sin costura en el ctor. La propiedad es cierta por construcción | COMPLIANT* (reserva W6: prueba vacía) |
| 12 | SessionDirector es C# puro | Instanciación sin escena | `FT:76-86` `Construible_y_operativo_sin_escena_Unity` | `SD:14` `sealed class` sin base; asmdef `noEngineReferences: true`, `references: ["NpcAi.Core"]` | COMPLIANT* |

**Compliance summary**: 12/12 escenarios con prueba cubriente y atestada en verde; 0 UNTESTED, 0 FAILING, 0 PARTIAL. Nombres de prueba idénticos a la tabla "Trazabilidad" de `spec.md` en los 12 casos.

### Correctness (Static Evidence)

| Requisito | Estado | Notas |
|---|---|---|
| Centinelas para señales parciales | Implementado | `SD:135-136` (clasifica con `m2` y evalúa con `Ninguna`), `SD:152` (`Unknown`, sin `Classify`). La cláusula "DEBE clasificar con `m2`" no tiene aserción (W4). |
| Silencio de la acción física sola | Implementado | `SD:150-155`: mueve receptividad, notifica a M9 y devuelve `null`. |
| Enrutado clínico/social | Implementado | `SD:139-142`: `m15.Respond` primero; `Handled` -> `clin.Reply`; si no, `Generate`. |
| Secuencia de inicio de sesión | Implementado | `SD:118-127`: `m4.Reset`, `_asignarCaso(caso)`, `_m15.AssignCase(caso, personalidad)` con el mismo valor local. |
| Selección reproducible por semilla | Implementado | `System.Random` (`using System;`, sin `UnityEngine`; el asmdef excluye el motor). No repite `_ultimoCaso` con lista filtrada y respaldo al catálogo completo. |
| Fronteras que SessionDirector no cruza | Implementado (por construcción) | rg sobre `Runtime/Harness/`: ni `INpcPresenter`, `.Play(`, `RegisterRedFlag` ni `Channel` en código (solo `MonoBehaviour`/`UnityEngine`/`IsReady` dentro de comentarios). |
| SessionDirector es C# puro | Implementado | Un solo tipo público (`SD:14`), un solo constructor público (`SD:55`), sin herencia; asmdef con una única referencia. |

### Coherence (Design)

| Decisión | ¿Seguida? | Notas |
|---|---|---|
| AD1 Dos delegados hacia M9 | Sí | `SD:27,29,61-62`. Premisa verificada: `IScenarioObjective` (`Ports.cs:181-198`) solo declara `Progress01`, `IsComplete`, `Notify`. `Ports.cs` y `Dtos.cs` sin cambios en el diff. |
| AD2 Un constructor público de 10 parámetros | Sí | `SD:55-65`; `FT:43-45` usa `.Single()` sobre los constructores públicos. |
| AD3 Validación y copia defensiva | Sí (copia sin prueba: S3) | `SD:67-79` (5 puertos + 2 delegados -> `ArgumentNullException`; catálogos nulos/vacíos -> `ArgumentException`), `SD:83-84` `.ToArray()`. 11 pruebas cubren las excepciones (`ST:55-141`). Los métodos no contienen `throw`. |
| AD4 Todo `public`, sin `InternalsVisibleTo` | Sí | Sin `AssemblyInfo` en `Runtime/Harness/`. |
| AD5 Dos sobrecargas sobre `Arrancar` | Sí (sin prueba: S3) | `SD:89-100`; la explícita no toca `_rng`. |
| AD6 `int semilla` -> `new Random(semilla)` | Sí | `SD:85`. |
| AD7 Lista filtrada, no re-tirada | Sí | `SD:105-115`; dos tiradas por selección (`SD:91-92`). Borde de un caso cubierto por `ST:202-211`. |
| AD8 `_ultimoCaso` en ambos caminos | Sí | `SD:122`, dentro de `Arrancar`. |
| AD9 Sin chequeos de `IsReady` | Sí | Única aparición de `IsReady` en `Runtime/Harness/`: comentario `SD:135`. |
| AD10 `m9.Notify` antes de la bifurcación | Sí en código; **sin prueba** (W3) | `SD:137` (antes de `SD:139-142`) y `SD:153`. |
| AD11 Cinco espías locales + dobles compartidos donde la spec los nombra | Sí (campos grabados sin aserción: W3, W4, S2) | `EA:14,33,68,88,125`, `internal sealed`; `ScriptedClinicalResponder` en `TT`, `RecordingNpcPresenter` en `FT`. |
| AD12 `noEngineReferences: true` | Sí | `NpcAi.Harness.asmdef:15`. Es un espejo literal de `NpcAi.Receptivity.asmdef` (difieren solo `name` y `rootNamespace`). |
| AD13 Fronteras (sin `FinalizarSesion`, presentador, banderas, canales, `Data/`) | Sí | Confirmado por rg y por la lectura completa de `SessionDirector.cs`; ningún id de caso ni personalidad hardcodeado (`caso-0*`, `grosero`, `amable` sin apariciones). |

Contrato público (`design.md` "Interfaces / Contracts") coincide con el código: constructor, `CasoActual`, `PersonalidadActual`, `Progreso`, `IniciarSesion` x2, `ProcesarTurno` -> `NpcReply`, `ProcesarAccion` -> `NpcReply?`, `DeclararTriaje`. Inventario de archivos: los 7 archivos previstos existen; `openspec/specs/banco-de-pruebas-m11/spec.md` se crea al archivar (pendiente, corresponde a `sdd-archive`).

### Reglas duras del proyecto (CLAUDE.md del repositorio)

| Regla | Resultado | Evidencia |
|---|---|---|
| 1. Un cambio toca un solo módulo | Cumple | Diff limitado a `Runtime/Harness/`, `Tests/EditMode/Harness/` y `openspec/`. Además 6 archivos eliminados del cambio superado `2026-09-09-m11-armado-sesion/` (borrado aprobado por el usuario). Totales del diff contra el `main` previo: 22 archivos añadidos, 6 eliminados. |
| 2. `Runtime/Core/` y `Runtime/CoreChannels/` de solo lectura | Cumple | `git diff --name-only 5b78ce7^1 5b78ce7 -- Runtime/Core Runtime/CoreChannels` vacío; `Ports.cs`/`Dtos.cs` sin cambios (sin cambio de contrato). |
| 3. El módulo referencia solo `NpcAi.Core` | Cumple en runtime | `NpcAi.Harness.asmdef` referencia únicamente `NpcAi.Core`. Ensamblado de pruebas: ver S7. |
| 4. Todo módulo publica su doble en `Runtime/<Modulo>/Fakes/` | **Sin justificación explícita** | Ver W7. `Runtime/Harness/` no tiene `Fakes/`. |
| 5. `NpcAi.Core` sin `UnityEngine` | Cumple | `Core` no fue tocado. |
| 6. Receptividad sin `UnityEngine` | No aplica | Módulo no tocado. |
| 7. Lo variable es dato | Cumple | Catálogos y semilla inyectados; sin ids ni personalidades en código. |
| Sin `UnityEngine` / `Debug.Log` en `Runtime/Harness/` | Cumple | Solo menciones en comentarios XML-doc; el asmdef sin motor lo convierte en error de compilación. |
| Namespaces `NpcAi.Harness` y `NpcAi.Harness.Tests`; pruebas en `Tests/EditMode/Harness/` | Cumple | rg sobre las declaraciones `namespace`. |
| No usar `/sdd-ff` en módulos de IA; no abrir SDD para 1-3 archivos | No aplica | M11 no es módulo de IA; el cambio es de 7 archivos de código. |

### Higiene

| Comprobación | Resultado |
|---|---|
| `.meta` de cada `.cs`/`.asmdef` y de las dos carpetas | 9/9 presentes en disco y rastreados (`Runtime/Harness.meta`, `Tests/EditMode/Harness.meta` y los 7 de archivo), todos añadidos por el commit de seguimiento `a4118f8` (9 archivos, 40 inserciones). |
| GUIDs de los 9 `.meta` | Únicos. |
| Archivos ajenos en las dos carpetas | Ninguno (solo `.cs`, `.asmdef`, `.meta`). |
| Determinismo de las pruebas | Semillas fijas (1234, 0..19, 7, 1); sin `DateTime`, `Guid`, `UnityEngine.Random`, `new Random`, `File.`, `Directory.`, `Path.`, hilos, red ni `Resources`/`AssetDatabase` (rg sin coincidencias). `ScriptedClinicalResponder` usa una tabla embebida, sin disco. |
| Pruebas omitidas o ignoradas | Ninguna. |
| Bucles con aserciones | `ST:177-184` y `ST:190-198` iteran sobre rangos enteros fijos de 20: no son "ghost loops". |
| Ruido en el árbol de trabajo | Hay `.meta` sin rastrear de `openspec/` y `Training/` (preexistentes, ajenos al PR1; ver S10). El estado git de `Runtime/Harness*` y `Tests/EditMode/Harness*` está limpio. |

### TDD Compliance (Strict TDD)

| Check | Resultado | Detalles |
|-------|-----------|----------|
| TDD Evidence reported | Sí, con formato reducido | `apply-progress.md` incluye la tabla "TDD Cycle Evidence" con columnas Task/Behavior/RED/GREEN/REFACTOR, sin TRIANGULATE ni SAFETY NET (S9). |
| All tasks have tests | 3/3 grupos de comportamiento | 2.1-2.3 (`ST`), 3.1 (`TT`), 4.1 (`FT`); 1.3 es infraestructura de prueba (RED no aplica). |
| RED confirmed (tests exist) | 4/4 archivos verificados | Los tres `SessionDirector*Tests.cs` y `EspiasDeArnes.cs` existen. Los errores de compilación citados como RED (`CS0246`, `CS1061`) no son reproducibles por esta verificación. |
| GREEN confirmed (tests pass) | **No reejecutado** | Solo atestado (a) y reportado (b); ver W1. |
| Triangulation adequate | Parcial | Buena en constructor (11 casos), selección (20 semillas, 20 sesiones), borde de un caso. Débil en W4 y W5. |
| Safety Net for modified files | N/A | Los 7 archivos de código son nuevos (estado `A` en git); no se modificó código preexistente. |

**TDD Compliance**: 3/6 sin reserva (cobertura de tareas, RED, safety net); 3/6 con reserva (evidencia en formato reducido S9, GREEN no reejecutado W1, triangulación parcial W4/W5).

### Test Layer Distribution

| Layer | Tests | Files | Tools |
|-------|-------|-------|-------|
| Unit | 26 | 3 (+1 de espías) | Unity Test Framework EditMode + NUnit 3 |
| Integration | 0 | 0 | no instalado (Engram #20) |
| E2E | 0 | 0 | no instalado |
| **Total** | **26** | **3** | |

Los escenarios 5, 6 y 10 usan dobles compartidos de otros módulos (`ScriptedClinicalResponder`, `RecordingNpcPresenter`); siguen siendo pruebas unitarias sin I/O.

### Changed File Coverage

Análisis omitido: no hay herramienta de cobertura detectada. No es un fallo.

### Quality Metrics

**Linter**: no disponible (sin `.editorconfig` ni analizadores). **Type Checker**: compilador de Unity (Roslyn) implícito; sin salida propia en esta verificación (evidencia indirecta: compuerta humana).

### Assertion Quality

| Archivo | Línea | Aserción | Problema | Severidad |
|---------|-------|----------|----------|-----------|
| `FT` | 55-56 | `presentador = new RecordingNpcPresenter()` seguido de `Assert.IsFalse(presentador.HasPlayed)` | Tautología decorativa: un doble recién construido y nunca conectado a producción no puede haber reproducido. La aserción de reflexión de la línea 50 sí es real. | WARNING (W6) |
| `FT` | 70 | `Assert.AreEqual(0, objetivo.BanderasRegistradas)` | Contador inalcanzable por construcción (ver W6). | WARNING (W6) |
| `TT` | 122 | `Assert.AreEqual(m4.Current, m6.Llamadas[0].Receptivity)` | `Current` es siempre `Neutral` (igual a `default(Receptivity)`): no discrimina contra una constante (W5). | WARNING (W5) |
| `TT` | 107 | `Assert.AreEqual("Desde hace un par de dias, doctora.", respuesta.Text)` | Compara solo `Text`; la spec dice "exactamente `clin.Reply`" (S1). | SUGGESTION |
| `TT` | 48-51 | `SoloUtterance_evalua_con_PhysicalAction_Ninguna` | Solo verifica `Action`; nada verifica que `Classify` se invoque ni que su resultado fluya (W4). | WARNING (W4) |
| (sin aserción) | n/a | `EspiaObjetivo.Notificaciones` | Grabado pero nunca asertado (W3). | WARNING (W3) |

**Assertion quality**: 0 CRITICAL, 5 WARNING (W3-W6), 1 SUGGESTION. No hay tautologías que sean la única aserción de una prueba, ni pruebas cuyo único contenido no llame a código de producción, ni bucles fantasma, ni pruebas de humo, ni acoplamiento a detalle de implementación, ni razón mocks/aserciones alta (no hay framework de mocks).

Análisis de mutantes **por inspección** (no se ejecutó ninguna herramienta de mutación):

| Mutante | Resultado esperado |
|---|---|
| Omitir `_m4.Reset`, `_asignarCaso` o `_m15.AssignCase`; usar casos distintos para M9 y M15 | Eliminado (`ST:146-167`; además `TT:99-109` falla si M15 no recibe caso) |
| No actualizar `_ultimoCaso` en el camino explícito | Eliminado (`ST:188-199`; con 20 semillas fijas y una probabilidad de colisión de 1/3 por semilla, el falso verde queda cerca de 0,03 %) |
| `ProcesarAccion` devuelve no-`null`, o llama `Respond`, `Generate` o `Classify` | Eliminado (`TT:55-67`, `TT:83-94`) |
| Cambiar `Ninguna` por otra acción en `ProcesarTurno` | Eliminado (`TT:43-52`) |
| Rama clínica siempre llama `Generate`; rama social no llama `Generate` | Eliminado (`TT:99-125`) |
| Normalizar la categoría en `DeclararTriaje` | Eliminado (`FT:91-99`, con la cadena `Infeccioso` y espacio final) |
| Quitar cualquier validación del constructor | Eliminado (`ST:55-141`) |
| **Quitar `_m2.Classify` y usar `IntentResult.Unknown()`** | **Sobrevive** (W4) |
| **Quitar `_m9.Notify(cambio)` en ambos métodos** | **Sobrevive** (W3) |
| **Pasar `Receptivity.Neutral` fijo a `Generate`** | **Sobrevive** (W5) |
| Pasar `PersonalityId.None` a `_m15.AssignCase` | Sobrevive (S2) |
| Que la sobrecarga explícita consuma una tirada de `_rng` (AD5) | Sobrevive (S3) |
| Quitar la copia defensiva `.ToArray()` (AD3) | Sobrevive (S3) |
| Alterar `EmotionTag` o `AnimationCue` de la respuesta clínica | Sobrevive (S1) |

### Issues Found

**CRITICAL**: Ninguno.

**WARNING**:

- **W1. Evidencia de ejecución solo atestada, sin total registrado.** `tasks.md` 5.1 dice "todo en verde" sin cifra, mientras `proposal.md` (Success Criteria PR1) y `design.md` (Testing Strategy, "Compuerta humana") exigen "el total registrado en `apply-progress.md`". Esta verificación no puede reobservar la ejecución, así que la conformidad de los 12 escenarios descansa en una atestación cualitativa y no puedo cotejar que los 26 `[Test]` de este cambio estuvieran entre los ejecutados. Mitigación verificada: los bytes de código no cambiaron tras `0aa37d3`. Acción sugerida (humano, coste mínimo): en Test Runner > EditMode confirmar `NpcAi.Harness.Tests` = 26/26 y anotar ese total y el de la suite completa en `apply-progress.md`.
- **W2. `apply-progress.md` (y Engram #95) desactualizados.** Siguen indicando 17/19, listan 5.1/6.2/6.3 como pendientes y conservan la sección "COMPUERTA HUMANA — pendiente"; `tasks.md` (autoritativo) está en 19/19 con la compuerta confirmada. Mismo patrón que el WARNING 2 del verify de M0. Refrescar al archivar.
- **W3. AD10 (`m9.Notify`) sin prueba cubriente.** `EspiaObjetivo.Notificaciones` (`EA:91`) se graba pero ninguna prueba lo asierta. Quitar `_m9.Notify(cambio)` (`SD:137` y `SD:153`) deja 26/26 en verde, y en producción el componente de receptividad de `Progress01` dejaría de actualizarse sin ninguna señal. No es un escenario de la spec (por eso no es CRITICAL), pero es comportamiento implementado sin prueba que lo haya puesto en RED (Strict TDD). Sugerencia: pruebas de `ProcesarTurno` y de `ProcesarAccion` que afirmen que `EspiaObjetivo.Notificaciones` recibe el `ReceptivityChange` una vez por llamada.
- **W4. El flujo `Classify` -> `IntentResult` no está afirmado.** El requisito "Centinelas" dice que `ProcesarTurno` "DEBE clasificar con `m2`", pero ninguna cláusula `Entonces` lo asierta: `EspiaClasificador.Invocaciones` solo se comprueba igual a 0 en el escenario 2; `UltimoTexto` y `Resultado` no se usan (siempre `Unknown`); y `ScriptedClinicalResponder` ignora `intent`. Quitar `_m2.Classify` sobrevive a las 26 pruebas. Sugerencia: programar `EspiaClasificador.Resultado` con un `IntentResult` distinguible y afirmar `Invocaciones` igual a 1, `UltimoTexto` igual a `utterance.Text` y que ese mismo resultado llega a `Evaluate`, `Respond` y `Generate`.

- **W5. `Turno_social_llama_Generate_con_receptividad_actual` no discrimina `m4.Current` de una constante.** `EspiaReceptividad.Current` permanece en `Neutral` durante toda la prueba (`ProcesarTurno` evalúa con `Ninguna`), y `Neutral` es `default(Receptivity)`. Un `Generate` con `Receptivity.Neutral` fijo pasaría. Sugerencia: mover primero la receptividad (`ProcesarAccion(PhysicalAction.Acercarse)` deja `Receptivo`) y luego afirmar `Receptivo` en la llamada a `Generate`.
- **W6. Pruebas de fronteras vacías o decorativas.** (i) `SesionCompleta_nunca_incrementa_el_contador_de_bandera_roja_del_espia_local` pasa `EspiaObjetivo` solo como `IScenarioObjective` (interfaz sin `RegisterRedFlag`) y ambos delegados del constructor son funciones vacías: ningún camino de `SessionDirector` puede alcanzar `EspiaObjetivo.RegisterRedFlag`, así que el contador vale 0 para cualquier implementación que conserve el constructor de 10 parámetros. El requisito **sí se cumple** (garantía estructural, verificada: `RegisterRedFlag` no aparece en `Runtime/Harness/` y `design.md` AD1/AD13 lo declara inalcanzable), pero la prueba no protege contra regresiones. Sugerencia: una aserción por reflexión sobre la lista exacta de parámetros del constructor (5 puertos, un delegado de `ClinicalCaseId`, un delegado de `string`, dos listas y un `int`) para que agregar cualquier costura nueva ponga la prueba en rojo. (ii) `FT:55-56` (`Assert.IsFalse(presentador.HasPlayed)`) es una tautología decorativa; conviene eliminarla y dejar solo la aserción de reflexión, que es la que puede fallar de verdad.
- **W7. Regla dura 4 (`Fakes/`) sin justificación explícita en ningún artefacto.** El `CLAUDE.md` del repositorio la declara sin excepción y `openspec/config.yaml` (`rules.tasks`, línea 41) dice "Every module change ships its deterministic fake in Runtime/<Module>/Fakes/". `Runtime/Harness/` no tiene `Fakes/`, y una búsqueda dirigida sobre los artefactos del cambio (`proposal.md`, `spec.md`, `design.md`, `tasks.md`, `apply-progress.md`, `PENDIENTES-SIGUIENTE-CHAT.md`) no encuentra ninguna frase que lo justifique: `design.md:257` habla solo de que los cinco espías de prueba no viven en `Fakes/`; AD2 dice que `SessionDirector` "no implementa ningún puerto, así que no tiene gemela de contrato", que cubre únicamente la mitad "hereda de la clase de prueba de contrato"; y "hoja del grafo" aparece solo en contexto de rollback. Valoración: la excepción parece sustantivamente sólida (M0 define 7 puertos y `SessionDirector` no implementa ninguno; ningún ensamblado referencia `NpcAi.Harness`; un doble no tendría clase de contrato que heredar; `Core` y `CoreChannels` tampoco publican `Fakes/`), pero la regla no prevé excepciones, por lo que debe quedar registrada. No es CRITICAL: no rompe ningún escenario y la razón es derivable. Decidir si registrarla y dónde (adenda de diseño del PR2, sección M11 de `Docs/MODULES.md` que el PR2 ya modifica, o una aclaración en `config.yaml`) corresponde a los dueños del módulo.
- **W8. Engram: spec y proposal bajo clave no canónica, y la spec está obsoleta.** `proposal` (#90), `spec` (#91) y `explore` (#88) están guardados como `sdd/m11-banco-de-pruebas/*`, mientras que `design` (#92), `tasks` (#94) y `apply-progress` (#95) usan `sdd/2026-09-17-m11-banco-de-pruebas/*`. Una fase que busque `sdd/2026-09-17-m11-banco-de-pruebas/spec` (p. ej. `sdd-archive`) no la encontrará. Además #91 es una versión condensada previa a la corrección de la spec (commit `4fe4709`): conserva los `Dado` no satisfacibles (`ScriptedScenarioObjective` espía, `RecordingNpcPresenter` espía) y los nombres `NuncaLlama_Play` y `NuncaLlama_RegisterRedFlag`. Los archivos de `openspec/` son autoritativos (modo híbrido), pero conviene volver a guardar `spec.md` y `proposal.md` bajo las claves canónicas antes de archivar.

**SUGGESTION**:

- **S1.** Escenario 5: afirmar igualdad de la `NpcReply` completa (texto y etiquetas), no solo `Text` (`TT:107`).
- **S2.** `EspiaClinico.UltimaPersonalidad` se graba pero no se asierta; añadir la comparación con `personalidad` en `ST:146-167`.
- **S3.** Sin prueba para AD5 (la sobrecarga explícita no consume el RNG: intercalar arranques explícitos y aleatorios en dos directores con la misma semilla) ni para la copia defensiva de AD3 (mutar la lista del llamador tras construir y comprobar la misma secuencia).
- **S4.** El escenario 4 dice "cualquier acción distinta de `Ninguna`" y la prueba usa solo `Acercarse`; triangular con `ContactoVisual`.
- **S5.** El criterio de éxito de la propuesta dice "juntos y en orden" para los efectos del arranque; la spec solo exige "juntos" y el código respeta el orden `Reset`, M9, M15, pero ninguna prueba fija el orden. Opcional.
- **S6.** Notas de documentación: `tasks.md` 6.2 conserva "rama al día con main: pendiente de verificar" (ya verificado por el orquestador: 0 detrás, 7 delante); `apply-progress.md` "Rollback boundary" dice 6 archivos en `Tests/EditMode/Harness/` cuando son 5 de código (4 `.cs` y 1 `.asmdef`).
- **S7.** `NpcAi.Harness.Tests` es el primer ensamblado de pruebas que referencia módulos pares (`NpcAi.ClinicalResponse`, `NpcAi.Presentation`); verificado sobre los 12 `.asmdef` de `Tests/EditMode`. Está documentado y justificado en `design.md` ("Unidad de módulo y grafo de referencias") y el ensamblado de runtime cumple la regla 3; como el texto literal de la regla habla de "módulos", conviene registrar el precedente (p. ej. en `Docs/MODULES.md`).
- **S8.** Al archivar, la tabla "Trazabilidad" de `spec.md` conserva la etiqueta "(propuesto)" en pruebas que ya existen, y `design.md` "Open Questions" conserva ítems ya resueltos (lo reconoce `tasks.md` 0.3). Ordenar al promover la spec a `openspec/specs/`.
- **S9.** La tabla "TDD Cycle Evidence" de `apply-progress.md` no incluye columnas TRIANGULATE ni SAFETY NET.
- **S10.** Hay `.meta` sin rastrear en el árbol de trabajo para carpetas de `openspec/` y `Training/` (preexistentes, fuera del PR1); considerar una política uniforme de `.meta` para `openspec/`.

### Verdict

**PASS WITH WARNINGS**

0 CRITICAL y 8 WARNING. Los 7 requisitos y los 12 escenarios de la spec tienen prueba cubriente y el código en disco los implementa; el diff respeta las reglas duras 1-3 y 5-7 (la 4 queda sin justificación explícita: W7); la higiene de `.meta` está completa. La evidencia de ejecución es únicamente la atestación de la compuerta humana (más el arnés del apply fuera del repositorio): esta verificación ejecutó cero pruebas, y el cambio es archivable bajo esa condición. Se recomienda cerrar antes o durante el archivo W1, W2 y W8 (registros), y abrir un seguimiento pequeño para W3-W6 (huecos de cobertura, sin defecto de producto conocido) y W7 (decisión de los dueños del módulo).

### Siguiente paso

`sdd-archive` (sin CRITICAL). Antes de archivar: registrar el total de la compuerta humana (W1), refrescar `apply-progress.md` (W2) y volver a guardar `spec` y `proposal` en Engram bajo las claves canónicas (W8).
