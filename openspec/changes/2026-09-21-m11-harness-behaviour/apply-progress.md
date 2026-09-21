# Apply Progress: M11 PR2 — cáscara de escena (`HarnessBehaviour`), unidad 1 (PR 2a)

## Change

`2026-09-21-m11-harness-behaviour`

## Scope of this batch

Unidad 1 (PR 2a, `stacked-to-main`, decisión del usuario 2026-09-21): Fases 0-10 de `tasks.md`,
ejecutadas por el agente. Quedan fuera de este batch: Fase 11 (Compuerta humana 1), Fases 12-14
(unidad 2: `Samples~/`, README, `Docs/`, comentario de `SessionDirector.cs:25`, Compuertas 2-3) y
la Fase 15 (git, del orquestador). Rama `feat/m11-02-harness-behaviour`. Ningún comando git de
escritura fue ejecutado por el agente; no se creó ningún `.meta`.

Estado por fase. La columna "Compilación" es el resultado de `dotnet build` del proyecto de
descarte de 1.6 al cerrar la fase: compila, NO ejecuta pruebas.

| Fase | Estado | Compilación (1.6) |
|---|---|---|
| 0 Guardrails | Hecha | N/A |
| 1 Foundation | Hecha (1.6 opcional hecha) | Correcta, 0 advertencias, 0 errores (esqueleto + andamio, 0 pruebas) |
| 2 A. Simetría de suscripción | Hecha: 4 pruebas escritas (3 R, 1 F) + GREEN 2.5 | Correcta, 0 advertencias, 0 errores |
| 3 B. Salida y conteo de `Raise` | Hecha: 4 pruebas escritas (2 R, 2 F) + GREEN 3.5 + refuerzo 3.6 | Correcta, 0 advertencias, 0 errores |
| 4 C. Frontera de inyección | Hecha: 3 pruebas escritas (3 R; una es EXTRA) + GREEN 4.4 | Correcta, 0 advertencias, 0 errores |
| 5 D. Inicio de sesión | Hecha: 6 pruebas escritas (5 R, 1 F) + GREEN 5.7 | Correcta, 0 advertencias, 0 errores |
| 6 E. Cierre | Hecha: 4 pruebas escritas (4 R) + GREEN 6.5 | Correcta, 0 advertencias, 0 errores |
| 7 F. Punto de entrada y arranque | Hecha: 3 pruebas escritas (1 R, 2 F) + GREEN 7.4 | Correcta, 0 advertencias, 0 errores |
| 8 G. `DeclararTriaje` | Hecha: 3 pruebas escritas (2 R, 1 F EXTRA) + GREEN 8.4 | No compilada por separado: la primera compilación posterior incluyó las pruebas de la Fase 9 (ver fila 9) |
| 9 H. Estructural | Hecha: 2 pruebas escritas (1 F, 1 R EXTRA) + GREEN 9.3 | Primer intento: 1 error CS0246 (`DefaultExecutionOrderAttribute` no existe en `UnityEngine`; ver "Issues Found" 1). Corregido el nombre en la prueba y agregado 9.3: correcta, 0 advertencias, 0 errores |
| 10 Verificación estática y trazabilidad | Hecha: 10.1, 10.2 y 10.3 cumplidas (ver secciones abajo) | Compilación final (`--no-incremental`), primero tras el último cambio de comentarios y otra vez tras la pasada correctiva del validador (ver "Pasada correctiva"): correcta, 0 advertencias, 0 errores en ambas |

## Mode

Strict TDD (`strict_tdd: true`; instrucción explícita del batch). **Restricción de evidencia, que
condiciona todo lo que sigue**: las 29 pruebas de este cambio usan `ScriptableObject.CreateInstance`,
`new GameObject` y `AddComponent`, que necesitan el motor nativo de Unity. No hay CLI/batchmode del
Test Runner y el arnés `dotnet test` de PR1 no puede correrlas. Por eso:

- El agente **no ejecutó ninguna** de las 29 pruebas. "RED" y "GREEN" en este documento significan
  **escrita, no ejecutada por el agente**: el RED de cada prueba R se razonó por inspección contra
  el esqueleto o el GREEN previo (la aserción o excepción que fallaría se nombra en la tabla); el
  GREEN se razonó por inspección contra el código de producción, prueba por prueba, y se comprobó
  que compila.
- Las pruebas F (fijación: 2.4, 3.3, 3.4, 5.6, 7.1, 7.2, 8.3, 9.1) ya pasan al escribirlas; su RED
  efectiva es por mutación (quitar la guarda que fijan) y **no se observó**. Ninguna se presenta
  como RED: en `tasks.md` esas 8 tareas se rotulan `FIJACION (F)` y no `RED`. Total: 21 R + 8 F = 29.
- La evidencia real son las Compuertas humanas 1, 2 y 3. Ninguna fue corrida.

## Evidencia de compilación (tarea 1.6, opcional; NO es evidencia RED/GREEN)

Se armó un proyecto `dotnet` de descarte **fuera del repo** (scratchpad de la sesión, nunca en el
diff) con 5 ensamblados que espejan lo que Unity genera en `NpcAi.Harness.Tests.csproj`
(`netstandard2.1`, C# 9.0, `NoStdLib`, referencias de `UnityReferenceAssemblies/unity-4.8-api` y
`UnityEngine.CoreModule.dll` del Editor 6000.5.4f1, `nunit.framework.dll` del `PackageCache` del
proyecto anfitrión): `NpcAi.Core` (`Runtime/Core/**`), `NpcAi.Core.Channels`
(`Runtime/CoreChannels/**`), `NpcAi.Harness` (`SessionDirector.cs`), `NpcAi.Harness.Unity`
(`Runtime/Harness/Unity/**`) y `NpcAi.Harness.Tests` (`EspiasDeArnes.cs` y
`HarnessBehaviourWiringTests.cs`). Sirve para detectar errores de firma y de uso antes de la
Compuerta 1: **compila, no ejecuta**. Como los nombres de ensamblado son los reales, también
confirma que el `InternalsVisibleTo("NpcAi.Harness.Tests")` alcanza los campos y costuras
`internal`. No cubre `Samples~/` (Sentis), ni las otras 3 clases de prueba del ensamblado (dependen
de `NpcAi.ClinicalResponse`/`NpcAi.Presentation`), ni la generación de `.meta`, ni el compilador
de Unity propiamente dicho (mismo lenguaje y referencias, pero otro host).

Comando (repetible tras cada GREEN): `dotnet build tests/HarnessTests.csproj` desde el proyecto de
descarte. Se comprobó que el resultado no es vacuo: la DLL de pruebas generada contiene los nombres
de los métodos escritos hasta ese momento, y el primer intento de la Fase 9 falló de verdad (ver
"Issues Found" 1).

Comprobación estática complementaria (metadatos de la DLL compilada de `NpcAi.Harness.Unity`, NO
es la prueba 9.1 ni se ejecutó NUnit): los ensamblados referenciados son `mscorlib`,
`UnityEngine.CoreModule`, `NpcAi.Core.Channels`, `NpcAi.Harness` y `NpcAi.Core`. Los tres `NpcAi.*`
pertenecen a la lista blanca de 9.1 y `NpcAi.Core` está presente (guarda anti-vacuidad). Hecho
contra la DLL del proyecto de descarte, no contra la que compilará Unity.

## TDD Cycle Evidence

Ninguna columna dice "observado": ver "Mode". "Escrita" = escrita y revisada por inspección, no
ejecutada por el agente. Safety Net: el único archivo existente modificado es
`NpcAi.Harness.Tests.asmdef` (configuración, sin lógica); `SessionDirector.cs` y las 34 pruebas
previas del ensamblado no se tocaron, así que no hay línea base ejecutable que proteger.

| Task | Test File | Layer | Safety Net | RED | GREEN | TRIANGULATE | REFACTOR |
|------|-----------|-------|------------|-----|-------|-------------|----------|
| 1.4 | `HarnessBehaviour.cs` (esqueleto) | N/A | N/A (nuevo) | N/A — esqueleto sin comportamiento | Compila (1.6) | N/A | N/A |
| 1.5 | `HarnessBehaviourWiringTests.cs` (andamio) | N/A | N/A (nuevo) | N/A — infraestructura de prueba, 0 `[Test]` | Compila (1.6) | N/A | N/A |
| 2.1-2.4 | `HarnessBehaviourWiringTests.cs` | Unit (EditMode, cableado sobre director real y espías) | N/A (nuevo) | Escrita, no ejecutada. 2.1 R: contra el esqueleto (`OnEnable` vacío) falla `AreEqual("hola doctora", M2.UltimoTexto)` (null). 2.2 R: falla el control positivo (`M2.Invocaciones` 0, esperado 1). 2.3 R: `M2.Invocaciones` 0, esperado 1. 2.4 **F** (pasa contra el esqueleto; su RED efectiva es por mutación y no se observó) | 2.5 escrito, no ejecutado: `OnEnable`/`OnDisable` con `Subscribe`/`Unsubscribe` sobre los dos canales y handlers que solo reenvían; compila (1.6) | 2.2: control positivo + los dos canales por separado (una fuga en uno solo mueve un contador distinto); 2.3: también el canal de acción (2 evaluaciones, no 3); 2.4: dos configuraciones (sin utterance, sin acción); 2.1 fija además AD9 (un `Raise` en el canal de respuesta deja en 0 a M2 y M4: la cáscara no se suscribe a su canal de salida; pasada correctiva) | Ninguno necesario |
| 3.1-3.4, 3.6 | `HarnessBehaviourWiringTests.cs` | Unit (EditMode, cableado) | N/A (nuevo) | Escrita, no ejecutada. 3.1 R y 3.2 R: contra el GREEN 2.5 (handlers sin `Raise`) falla `AreEqual(1, Replies.Count)` (0). 3.3 **F** y 3.4 **F**: pasan contra el GREEN 2.5 (RED por mutación, no observada). 3.6 refuerza 2.3 (`Replies.Count == 1`; un oyente fugado daría 2) | 3.5 escrito, no ejecutado: `AlRecibirUtterance` hace un solo `Raise` con guarda `!= null`; `AlRecibirAccion` publica solo si `reply.HasValue`; compila (1.6) | 3.1 social y 3.2 clínico cubren las dos ramas del director con respuestas distinguibles (texto, emoción y cue por separado); 3.3 con control positivo (la acción sí llegó a M4) | Ninguno necesario |
| 4.1-4.3 | `HarnessBehaviourWiringTests.cs` | Unit (EditMode, cableado) | N/A (nuevo) | Escrita, no ejecutada. 4.1 R: contra el GREEN 3.5 el handler desreferencia `_director` nulo (NRE en `Raise`, falla `DoesNotThrow`). 4.2 R: `Inyectar` vacío deja la primera cáscara sin director (NRE). 4.3 R (EXTRA): `Inyectar` vacío no lanza (falla `Throws`) | 4.4 escrito, no ejecutado: guarda `if (_director == null) return;` en ambos handlers e `Inyectar` con `?? throw new ArgumentNullException(nameof(director))`; compila (1.6) | 4.1 con control positivo al final (`Inyectar` de un director que funciona y un `Raise` publican exactamente 1 `NpcReply`: la cáscara sí estaba suscrita); 4.2 compara los dos órdenes con montajes independientes y, tras inyectar lambdas que graban, abre y cierra la sesión de la primera cáscara (una apertura con `caso-01` y un cierre: solo esta ruta ejercita las asignaciones de las costuras dentro de `Inyectar`; pasada correctiva); 4.3 afirma también `ParamName == "director"` | Ninguno necesario |
| 5.1-5.6 | `HarnessBehaviourWiringTests.cs` | Unit (EditMode, cableado) | N/A (nuevo) | Escrita, no ejecutada. 5.1-5.5 R: contra el GREEN 4.4 (`IniciarSesion` vacío) fallan por aserción (`M4.Resets.Count` 0, esperado 1; o `etiquetas.Count` 0, esperado 1 o 2). 5.6 **F**: pasa contra el esqueleto (RED por mutación: quitar la guarda del director, no observada) | 5.7 escrito, no ejecutado: guarda de director, `_director.IniciarSesion()`, etiqueta `prefijo-caso-yyyyMMdd-HHmmss` con `InvariantCulture` y `_abrirBitacora` con guarda `!= null`; compila (1.6) | 5.3 cubre dos configuraciones (prefijo `sim` + `caso-02`; y forma exacta con reloj fijo); 5.1 verifica el mismo caso en M9, M15 y la etiqueta; 5.2 la ruta sin oyente | Ninguno necesario |
| 6.1-6.4 | `HarnessBehaviourWiringTests.cs` | Unit (EditMode, cableado) | N/A (nuevo) | Escrita, no ejecutada. 6.1 R: contra el GREEN 5.7 (`FinalizarSesion` vacío) `cierres` es 0, esperado 1. 6.2 R: tras `Iniciar`, `Finalizar`, `Finalizar` la costura cuenta 0, esperado 1 (la primera aserción, 0 sin sesión, ya pasa). 6.3 R: `SalirParaPrueba` sin efecto, `cierres` 0, esperado 1. 6.4 R: la secuencia es `inicio, inicio` y no `inicio, cierre, inicio` | 6.5 escrito, no ejecutado: `FinalizarSesion` (guarda por etiqueta vacía, costura, etiqueta a `""`), `OnApplicationQuit` la llama, `IniciarSesion` la llama tras la guarda del director; compila (1.6) | 6.2 cubre sin sesión, cierre y segundo cierre; 6.3 agrega que `OnDisable` no cierra (AD6; pasa contra el esqueleto, no cuenta como RED); 6.1 con seis contadores de línea base en M4, M9 y M15 | Ninguno necesario |
| 7.1-7.3 | `HarnessBehaviourWiringTests.cs` | Unit (EditMode, cableado y reflexión) | N/A (nuevo) | Escrita, no ejecutada. 7.1 **F** (el esqueleto ya declara `IniciarSesion()` pública y `void`; RED por mutación, no observada) y 7.2 **F** (`Start` vacío: pasa contra el esqueleto). 7.3 R: contra el GREEN 6.5 `Start` vacío deja `M4.Resets.Count` en 0, esperado 1 | 7.4 escrito, no ejecutado: `Start()` = `if (_arrancarSolo) IniciarSesion();`; compila (1.6) | 7.3 comprueba primero que habilitar NO arranca (AD3) y luego que `Start` arranca una vez; 7.2 fija el valor por defecto de `_arrancarSolo` | Ninguno necesario |
| 8.1-8.3 | `HarnessBehaviourWiringTests.cs` | Unit (EditMode, cableado) | N/A (nuevo) | Escrita, no ejecutada. 8.1 R: contra el esqueleto (`DeclararTriaje` vacío) `Triajes.Count` es 0, esperado 2. 8.2 R por control positivo: `Triajes.Count` 0, esperado 1. 8.3 **F** (EXTRA): pasa contra el esqueleto (RED por mutación: quitar la guarda del director, no observada) | 8.4 escrito, no ejecutado: `if (_director == null) return; _director.DeclararTriaje(categoria);`; compila (con la Fase 9, ver tabla de estado) | 8.1 con dos categorías distintas (`"  Rojo "` y `"AMARILLO"`, en orden) para descartar un valor fijo o una normalización; 8.2 abre primero una sesión (línea base de 1 apertura y 0 cierres) para que un cierre o una apertura indebidos sí se vean (pasada correctiva) | Ninguno necesario |
| 9.1-9.2 | `HarnessBehaviourWiringTests.cs` | Unit (EditMode, reflexión sobre el ensamblado compilado) | N/A (nuevo) | Escrita, no ejecutada. 9.1 **F** (lista blanca `{NpcAi.Core, NpcAi.Core.Channels, NpcAi.Harness, NpcAi.Harness.Unity}` + guarda anti-vacuidad: la enumeración contiene `NpcAi.Core`; ya pasa; RED por mutación: agregar una referencia a un par, no observada). 9.2 R (EXTRA): contra el GREEN 8.4 (sin atributo) `GetCustomAttribute` devuelve null y falla `IsNotNull` | 9.3 escrito, no ejecutado: `[DefaultExecutionOrder(100)]` sobre `HarnessBehaviour` (AD2), más el párrafo de AD2 en el comentario de la clase; compila (1.6) | 9.1 incluye la guarda anti-vacuidad y un mensaje que nombra las referencias ajenas; 9.2 afirma que existe y que es > 0 (el efecto no es observable en EditMode) | Ninguno necesario |

## Test Summary

- **Total tests written**: 29 (`[Test]`; 0 `[TestCase]`), en `HarnessBehaviourWiringTests`.
- **Total tests passing**: no ejecutadas por el agente (ver "Mode"). La suma esperada del ensamblado
  `NpcAi.Harness.Tests` es 34 previas + 29 = 63 (60 sin las 3 EXTRA).
- **R / F**: 21 R + 8 F.
- **Layers used**: Unit/EditMode (29); integración y E2E, ninguna (`config.yaml`: sin capa).
- **Approval tests** (refactoring): None — ninguna tarea de refactor.
- **Pure functions created**: 0 (la cáscara es un `MonoBehaviour`; la fórmula de la etiqueta vive
  en línea dentro de `IniciarSesion`).

## Work Unit Evidence

| Evidence | Value |
|---|---|
| Focused test command and exact result | Unity Test Runner > EditMode, filtro `HarnessBehaviourWiringTests` (no hay CLI): **NO ejecutado por el agente**. Evidencia disponible, distinta de una corrida de pruebas: compilación de descarte 1.6 (0 advertencias, 0 errores) y las comprobaciones estáticas 10.1-10.3 |
| Runtime harness command/scenario and exact result | N/A — EditMode sin escena; el cableado real (8 slots sobre 3 assets, orden AD2, bitácora AD3/AD5, cierre AD6) se prueba en las Compuertas 2-3 de la unidad 2 |
| Rollback boundary | Borrar `Runtime/Harness/Unity/` (asmdef, `HarnessBehaviour.cs`, `Properties/AssemblyInfo.cs`) y `Tests/EditMode/Harness/HarnessBehaviourWiringTests.cs`; revertir las 2 líneas de `Tests/EditMode/Harness/NpcAi.Harness.Tests.asmdef`. `main` queda en el estado de PR1 |

## Files Changed

| File | Action | Lines | What Was Done |
|------|--------|-------|---------------|
| `Runtime/Harness/Unity/NpcAi.Harness.Unity.asmdef` | Created | 18 | AD1: `references` exactamente `NpcAi.Core`, `NpcAi.Core.Channels`, `NpcAi.Harness`; `autoReferenced: true`; `noEngineReferences: false`. Espejo de `NpcAi.SessionLog.Unity.asmdef` |
| `Runtime/Harness/Unity/Properties/AssemblyInfo.cs` | Created | 8 | AD11: `[assembly: InternalsVisibleTo("NpcAi.Harness.Tests")]` |
| `Runtime/Harness/Unity/HarnessBehaviour.cs` | Created | 229 | Único tipo nuevo: `[DefaultExecutionOrder(100)]`, 5 campos `[SerializeField] internal`, `Inyectar`, `IniciarSesion`, `FinalizarSesion`, `DeclararTriaje`, `EtiquetaActiva`, ciclo de vida (`OnEnable`, `OnDisable`, `Start`, `OnApplicationQuit`), 4 costuras `internal` |
| `Tests/EditMode/Harness/HarnessBehaviourWiringTests.cs` | Created | 681 | 29 `[Test]` + andamio (`Montaje`, `Armar`, `Registrar`, `Dicho`, `[TearDown]`); 651 líneas antes de la pasada correctiva |
| `Tests/EditMode/Harness/NpcAi.Harness.Tests.asmdef` | Modified | +2 | AD12: + `NpcAi.Core.Channels`, + `NpcAi.Harness.Unity` (el resto intacto) |
| `openspec/changes/2026-09-21-m11-harness-behaviour/tasks.md` | Modified | — | Marcadas `[x]` las tareas 0.1-10.3; Fase 11 y siguientes sin marcar. Pasada correctiva: las 8 tareas F rotuladas `FIJACION (F)` y el tipo `DefaultExecutionOrder` corregido en 9.2 |
| `openspec/changes/2026-09-21-m11-harness-behaviour/apply-progress.md` | Created | — | Este archivo |

Total nuevo en `Runtime/` + `Tests/`: **938 líneas** en 5 archivos (229 + 681 + 18 + 8 + 2), de las
cuales 577 son código, 197 comentarios y 164 líneas en blanco (antes de la pasada correctiva eran
908: 561, 188 y 159; la pasada agregó 30 líneas, todas en el archivo de pruebas). **Supera el
pronóstico de la unidad 1 (~630-790) y el presupuesto de sesión de 800 en unas 138 líneas**: el
pronóstico de `HarnessBehaviourWiringTests.cs` era 450-560 (real 681) y el de `HarnessBehaviour.cs`
150-200 (real 229); el exceso es sobre todo espaciado de las 29 pruebas y comentarios XML-doc. Sin
`openspec/` ni `.meta`. Decisión de si se recorta o se acepta: del orquestador/usuario.

## Traceability check (tarea 10.3)

`rg`/Grep sobre `HarnessBehaviourWiringTests.cs`: exactamente **29 `[Test]`** y 0 `[TestCase]`; cada
uno de los 26 nombres de la tabla de trazabilidad del spec aparece exactamente una vez como
definición (`public void <Nombre>(`) y una vez en todo el archivo, y lo mismo los 3 EXTRA. El único
otro método público del archivo es el `[TearDown]` `Limpiar`. Ningún nombre renombrado.

| # | Nombre (spec) | Tarea | Tipo |
|---|---|---|---|
| 1 | `OnEnable_suscribe_los_dos_canales_de_entrada` | 2.1 | R |
| 2 | `OnDisable_desuscribe_los_dos_canales_sin_fuga` | 2.2 | R |
| 3 | `Volver_a_habilitar_no_duplica_la_suscripcion` | 2.3 (+3.6) | R |
| 4 | `Canal_de_entrada_sin_asignar_no_lanza` | 2.4 | F |
| 5 | `Turno_social_produce_exactamente_un_Raise_con_la_respuesta_del_director` | 3.1 | R |
| 6 | `Turno_clinico_manejado_produce_exactamente_un_Raise` | 3.2 | R |
| 7 | `Accion_fisica_sola_no_publica_nada_pero_llega_al_director` | 3.3 | F |
| 8 | `Canal_de_respuesta_sin_asignar_no_lanza` | 3.4 | F |
| 9 | `IniciarSesion_dispara_los_cuatro_efectos_juntos_con_el_mismo_caso` | 5.1 | R |
| 10 | `IniciarSesion_sin_oyente_en_la_costura_de_M13_dispara_igual_los_tres_efectos_del_director` | 5.2 | R |
| 11 | `IniciarSesion_es_publico_sin_parametros_y_sin_retorno` | 7.1 | F |
| 12 | `Con_el_flag_por_defecto_arrancar_no_inicia_sesion` | 7.2 | F |
| 13 | `Con_el_flag_activo_arrancar_inicia_exactamente_una_sesion` | 7.3 | R |
| 14 | `Etiqueta_lleva_el_prefijo_y_el_caso_elegido` | 5.3 | R |
| 15 | `Instantes_distintos_dan_etiquetas_distintas` | 5.4 | R |
| 16 | `Prefijo_vacio_no_produce_etiqueta_vacia` | 5.5 | R |
| 17 | `FinalizarSesion_dispara_la_costura_de_cierre_una_vez_sin_tocar_el_director` | 6.1 | R |
| 18 | `FinalizarSesion_sin_sesion_abierta_es_no_op` | 6.2 | R |
| 19 | `OnApplicationQuit_con_sesion_abierta_cierra_la_sesion_de_M13` | 6.3 | R |
| 20 | `IniciarSesion_con_otra_abierta_cierra_primero_la_anterior` | 6.4 | R |
| 21 | `Sin_director_los_canales_de_entrada_no_publican` | 4.1 | R |
| 22 | `Sin_director_IniciarSesion_no_abre_la_sesion_de_M13` | 5.6 | F |
| 23 | `El_orden_entre_inyeccion_y_habilitacion_no_cambia_el_resultado` | 4.2 | R |
| 24 | `Ensamblado_compilado_no_referencia_otros_modulos_NpcAi` | 9.1 | F |
| 25 | `DeclararTriaje_reenvia_la_categoria_sin_alterarla` | 8.1 | R |
| 26 | `DeclararTriaje_no_publica_ni_toca_la_sesion_de_M13` | 8.2 | R |
| E1 | `Inyectar_con_director_nulo_lanza_ArgumentNullException` | 4.3 | R |
| E2 | `DeclararTriaje_sin_director_es_no_op` | 8.3 | F |
| E3 | `DefaultExecutionOrder_de_la_cascara_es_positivo` | 9.2 | R |

## Verificación estática (tareas 10.1 y 10.2; las repite `sdd-verify`)

- **10.1**: `Runtime/Harness/Unity/NpcAi.Harness.Unity.asmdef` declara en `references` exactamente
  `NpcAi.Core`, `NpcAi.Core.Channels` y `NpcAi.Harness` (líneas 5-7); `autoReferenced: true`,
  `noEngineReferences: false`.
- **10.2**: `Debug\.` sobre `Runtime/Harness/` ⇒ 0 coincidencias (también 0 de `Debug` a secas bajo
  `Runtime/Harness/Unity/`). Sin `?.` en `Runtime/Harness/Unity/` (el comentario que explica la
  regla se redactó sin la cadena literal). Solo se referencian `NpcAi.Core`, `NpcAi.Core.Channels` y
  `NpcAi.Harness` (los `using` de la carpeta son `System`, `System.Globalization`, `NpcAi.Core`,
  `NpcAi.Core.Channels`, `UnityEngine` y `System.Runtime.CompilerServices`).

## Deviations from Design

1. **`DefaultExecutionOrderAttribute` no existe: el tipo de Unity es `UnityEngine.DefaultExecutionOrder`.**
   Solo `design.md` (Testing Strategy, línea 296) y la tarea 9.2 de `tasks.md` nombraban
   `DefaultExecutionOrderAttribute` (el spec no lo nombra), pero `UnityEngine.CoreModule` declara la
   clase sin sufijo (`T:UnityEngine.DefaultExecutionOrder`, propiedad `order`). La prueba 9.2 usa
   `GetCustomAttribute<DefaultExecutionOrder>()`; el comportamiento afirmado (el atributo existe y
   `order > 0`) es el del diseño. El nombre ya está corregido en la tarea 9.2 (pasada correctiva);
   la línea 296 de `design.md` la corrige el orquestador.
2. Sin más desviaciones de comportamiento: AD1-AD12 se cumplieron con el pseudocódigo del diseño.
   AD13 (`.meta`) queda para 15.4 por decisión ya registrada en `tasks.md`.

## Issues Found

1. **Nombre de tipo inexistente en el diseño** (ver Deviations 1). Lo detectó la compilación de
   descarte de 1.6 (CS0246) antes de la Compuerta 1; sin ella, habría roto la compilación de todo
   el ensamblado de pruebas en el Editor.
2. Aclaración del conteo de la línea base (0.3): `SessionDirectorTurnoTests` tiene 11 `[Test]` y un
   método con 2 `[TestCase]`, o sea 13 casos; con 16 + 5 son los 34 casos de la aritmética. Los 29
   nuevos son todos `[Test]`, así que 34 + 29 = 63 casos.
3. Aviso para 15.4 (predicción DESMENTIDA por la compuerta): se anticipó que Unity escribiría
   `executionOrder: 100` en `HarnessBehaviour.cs.meta` por el `[DefaultExecutionOrder(100)]`. No fue
   así: el `.meta` generado solo trae `fileFormatVersion` y `guid`, como los demás `.meta` de scripts
   del repositorio; Unity lee el atributo del código, no del `.meta`.
4. Los `.meta` de `Runtime/Harness/Unity/` (carpeta y archivos) y de
   `HarnessBehaviourWiringTests.cs` no existen hasta que el Editor los genere (tarea 0.4, 15.4).

## Pasada correctiva (validador independiente: PASS WITH FIXES, 2026-09-21)

Único pase correctivo pedido por el orquestador tras la validación en contexto limpio: por trazado
no encontró nada que vaya a fallar en Unity; los hallazgos son brechas de fuerza de las pruebas y
de bookkeeping. Sin código bajo `Runtime/` tocado, sin métodos `[Test]` nuevos (siguen siendo
exactamente 29), sin git y sin `.meta`. Nada se ejecutó: los cambios son de escritura e inspección.

Cambios en `HarnessBehaviourWiringTests.cs`:

1. `El_orden_entre_inyeccion_y_habilitacion_no_cambia_el_resultado`: la primera cáscara recibe por
   `Inyectar` lambdas que graban (`abrirBitacora`, `cerrarBitacora`); tras los `Raise` se llama
   `IniciarSesion()` y `FinalizarSesion()` y se afirma exactamente una apertura con una etiqueta que
   contiene `caso-01` y exactamente un cierre. Motivo: toda prueba de costura pasa por
   `CablearParaPrueba`, que asigna las costuras directo; borrar las dos asignaciones dentro de
   `Inyectar` habría dejado las 29 en verde.
2. `DeclararTriaje_no_publica_ni_toca_la_sesion_de_M13`: llama `IniciarSesion()` primero, toma la
   línea base (1 apertura, 0 cierres), llama `DeclararTriaje("rojo")` y afirma ambos conteos sin
   cambio (más el control positivo del delegado y 0 `NpcReply`). Motivo: sin sesión abierta,
   `cierres == 0` no prueba nada porque `FinalizarSesion()` es no-op.
3. `Sin_director_los_canales_de_entrada_no_publican`: control positivo al final (`Inyectar` de un
   director que funciona, `Raise` de una utterance, exactamente 1 `NpcReply`). Motivo: un cero
   también valdría si `OnEnable` no hubiera suscrito nada.
4. Pin de AD9 dentro de una prueba existente, `OnEnable_suscribe_los_dos_canales_de_entrada` (sin
   `[Test]` nuevo): un `Raise` en el canal de RESPUESTA deja en 0 las invocaciones del espía de M2
   y las evaluaciones del espía de M4 (la cáscara no se suscribe a su canal de salida).

Omitidos por indicación del orquestador (nits del validador): el pin de `InvariantCulture`, el
retorno temprano con un solo canal nulo y `EtiquetaActiva`.

Cambios en `tasks.md` (nada desmarcado: siguen 51 `[x]` y 18 `[ ]`): las 8 tareas F (2.4, 3.3, 3.4,
5.6, 7.1, 7.2, 8.3, 9.1) pasan de `RED ... (F)` a `FIJACION (F)` (8.3: `FIJACION (F) EXTRA`), con
numeración y nombres intactos (quedan 21 tareas `RED`); en la tarea 9.2 el tipo de Unity es
`DefaultExecutionOrder`, no `DefaultExecutionOrderAttribute`.

Cambios en este documento: Deviations 1 (solo `design.md` línea 296 y `tasks.md` 9.2 nombraban el
tipo inexistente; el spec no), pista de la Compuerta 1 (ver abajo), conteos de líneas, filas
afectadas de la tabla TDD y esta sección.

Verificación tras la pasada (nada ejecutado):

- `dotnet build tests/HarnessTests.csproj --no-incremental` del proyecto de descarte 1.6:
  compilación correcta, **0 advertencias, 0 errores**.
- `HarnessBehaviourWiringTests.cs`: exactamente 29 `[Test]`, 0 `[TestCase]`, un `[TearDown]`; cada
  uno de los 26 + 3 nombres definido una vez (`public void <Nombre>(`) y presente una vez en el
  archivo; el único otro método público es `Limpiar`.
- Sin caracteres no ASCII en `Runtime/Harness/Unity/` ni en el archivo de pruebas.
- `git diff --name-only` sigue mostrando solo `NpcAi.Harness.Tests.asmdef` como archivo trackeado
  modificado; `Runtime/Harness/Unity/HarnessBehaviour.cs` conserva sus 229 líneas.

## COMPUERTA HUMANA 1 — confirmada (Fase 11, tarea 11.1)

- **Resultado (2026-09-21)**: el usuario corrió Test Runner > EditMode > Run All en Unity y
  confirmó que **todos los tests están en verde**. No indicó el total: se esperaba 63 en
  `NpcAi.Harness.Tests` (34 + 29). Es atestación del usuario; el agente no re-observó la ejecución.
  Unity generó los 6 `.meta` de 15.4 (2 de carpeta y 4 de archivo) y no modificó ningún otro archivo
  trackeado: el único `M` sigue siendo `NpcAi.Harness.Tests.asmdef` (+2 líneas).
- Lo que sigue describe lo que se pidió confirmar; se conserva como registro.

- **Quién**: el usuario, en Unity Editor (Test Runner > EditMode), rama
  `feat/m11-02-harness-behaviour`. Es la primera compilación real de `Runtime/Harness/Unity/` en
  Unity y la que genera los `.meta` de 15.4.
- **Qué debe confirmar**: `HarnessBehaviourWiringTests` (29 pruebas) en verde junto con el resto de
  la suite; total del ensamblado `NpcAi.Harness.Tests` = 63 (60 si se descartan las 3 EXTRA); Run
  All global = línea base previa + 29; 0 fallidas, 0 omitidas.
- **Por qué nada de este batch la sustituye**: ninguna prueba se ejecutó, y la compilación de
  descarte no usa el compilador de Unity ni el Test Runner.
- **Si una prueba F o R falla**: no ajustar la prueba para que pase; contrastar con el pseudocódigo
  del `design.md` y con este documento. Dos causas a mirar primero, ambas suposiciones no
  observadas: (1) los valores por defecto de los campos serializados tras `AddComponent` (las
  pruebas 5.3 y 7.2 suponen `_prefijoDeEtiqueta == "banco"` y `_arrancarSolo == false`, o sea que
  Unity aplica los inicializadores de campo); (2) un `OnEnable` adicional sobre la misma cáscara
  ya con los canales asignados, aparte del de `CablearParaPrueba`, que duplique oyentes (afectaría
  a 2.3 y a los conteos de `Raise`). Un `OnEnable` automático al hacer `AddComponent` no puede
  duplicar suscripciones en este andamio: `Armar` asigna los canales DESPUÉS de `AddComponent`, así
  que ese `OnEnable` encontraría los canales nulos y no suscribiría nada.

## Remaining Tasks

- [x] 11.1 (Fase 11): Compuerta humana 1 — confirmada por el usuario el 2026-09-21 (todos en verde; total no indicado, esperado 63).
- [ ] 12.1-12.3, 13.1-13.6, 14.1-14.4 (Fases 12-14): unidad 2 (PR 2b), depende de que la unidad 1 esté
  mergeada.
- [ ] 15.1-15.4 (Fase 15): git y `.meta`, del orquestador; 15.3 exige la Compuerta 1 en verde.

## Workload / PR Boundary

- Mode: stacked PR slice (`stacked-to-main`), decisión del usuario 2026-09-21.
- Current work unit: unidad 1 (PR 2a) = Fases 0-11; este batch cubre Fases 0-10.
- Boundary: arranca del estado de PR1 en `main` (director sin llamadores) y termina con
  `HarnessBehaviour` + `NpcAi.Harness.Unity.asmdef` + 29 pruebas de cableado escritas; autónomo y
  reversible (ver "Work Unit Evidence").
- Estimated review budget impact: 938 líneas en `Runtime/` + `Tests/` (ver "Files Changed"); supera
  las 800 de la sesión y el rango de 630-790 del pronóstico.

## Status

Fases 0-10 completas (todas las tareas agent-ejecutables de la unidad 1). Fase 11 (Compuerta humana
1) confirmada por el usuario el 2026-09-21 (todos los tests en verde; total no indicado, esperado 63). La pasada correctiva del validador ya está aplicada (ver "Pasada
correctiva"): 29 `[Test]`, compilación de descarte correcta con 0 advertencias y 0 errores, nada
ejecutado. `sdd-apply` de la unidad 1 termina aquí: el siguiente paso del orquestador es preparar
el commit (Fase 15, rutas explícitas) y el del usuario, correr la Compuerta 1.
