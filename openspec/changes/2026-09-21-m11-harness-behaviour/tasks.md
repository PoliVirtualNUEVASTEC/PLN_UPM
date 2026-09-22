# Tasks: M11 PR2 — cáscara de escena (`HarnessBehaviour`), raíz de composición y documentación

## Review Workload Forecast

| Field | Value |
|---|---|
| Estimated changed lines | ~940-1,220, sin `openspec/` ni `.meta`: `HarnessBehaviour.cs` ~150-200, `HarnessBehaviourWiringTests.cs` ~450-560 (29 métodos + andamio), 2 `.asmdef` + `AssemblyInfo.cs` ~30, `CompositorDeArnes.cs` ~90-120, `README.md` ~170-230, `MODULES.md` sección M11 ~50-75, `SessionDirector.cs:25` ~3 |
| 400-line budget risk | High (supera 400 y también el presupuesto de sesión de 800) |
| Chained PRs recommended | Yes |
| Suggested split | **Resuelto (usuario, 2026-09-21): opción A, dos unidades apiladas** (U1 = Fases 0-11; U2 = Fases 12-14); B y C descartadas |
| Delivery strategy | ask-on-risk (review_budget_lines: 800) |
| Chain strategy | **stacked-to-main**: PR 2a contra `main`; PR 2b sale de `main` cuando 2a esté mergeado |

Decision needed before apply: Resolved — dos PR apilados (usuario, 2026-09-21)
Chained PRs recommended: Yes
Chain strategy: stacked-to-main
400-line budget risk: High

El diff también incluye `openspec/changes/2026-09-21-m11-harness-behaviour/` (~900 líneas de proposal, spec y design ya escritas, más este archivo y `apply-progress.md`). No se cuentan arriba, igual que en el pronóstico de PR1; si el presupuesto debe contarlas, la división es inevitable.

Decisión (1) cerrada por el usuario el 2026-09-21: división A, dos PR apilados. Decisión (2), por defecto del orquestador y vetable: se CONSERVAN las 3 pruebas EXTRA (4.3, 8.3, 9.2), que no están en las 26 de la trazabilidad del spec (total esperado en `NpcAi.Harness.Tests`: 34 + 26 + 3 = 63).

### Suggested Work Units

| Unit | Goal | Likely PR | Focused test command | Runtime harness | Rollback boundary |
|---|---|---|---|---|---|
| 1 | Cáscara `HarnessBehaviour` + `.asmdef` + 29 pruebas de cableado (Fases 0-11) | PR 2a (base por definir según la cadena) | Unity Test Runner > EditMode, filtro `HarnessBehaviourWiringTests` (no hay CLI) | N/A — EditMode sin escena; el cableado real se prueba en la Compuerta 2 (unidad 2). 1.6 solo compila | Borrar `Runtime/Harness/Unity/` y `HarnessBehaviourWiringTests.cs`; revertir `NpcAi.Harness.Tests.asmdef`. `main` queda en el estado de PR1 |
| 2 | Compositor + README + `MODULES.md` + comentario de `SessionDirector.cs:25` (Fases 12-14) | PR 2b (depende de la unidad 1) | Sin prueba EditMode propia por diseño; regresión: Run All (total en 11.1) | Compuerta 2 (escena de escritorio) y Compuerta 3 (Quest 3), ambas humanas | Borrar `Samples~/Harness/CompositorDeArnes.cs`; restaurar README, `MODULES.md` y el comentario. La unidad 1 sigue en verde |

Límites de división candidatos (esta fase no elige):

- **A, 2 unidades**: U1 = Fases 0-11 (~630-790); U2 = Fases 12-14 (~310-430). U1 supera 400 pero cabe en 800.
- **B, 3 unidades**: U1a = Fases 0-4 (fundación, cableado, salida, frontera de inyección; ~340-410); U1b = Fases 5-11 (sesión, cierre, arranque, triaje, estructural; ~290-390); U2 como en A. Cada unidad cerca de 400 o menos.
- **C, `size:exception`**: un solo PR de ~940-1,220 líneas (una clase, su suite y prosa; mismo argumento que PR1). Requiere aprobación de mantenedor.
- Común: U2 depende de U1 (README y compositor nombran la cáscara); las Compuertas 2-3 exigen U2 aplicada. Si se divide, la Fase 15 se repite por unidad con las rutas de esa unidad.

## Restricción de evidencia (leer antes de aplicar)

- Las pruebas de cableado usan `ScriptableObject.CreateInstance`, `new GameObject` y `AddComponent`: necesitan el motor nativo de Unity. El arnés `dotnet test` fuera del repo que ejecutó `SessionDirector` en PR1 **no puede correrlas**. El agente no ejecuta ninguna de las 29 pruebas de este cambio.
- "RED" y "GREEN" de la cáscara significan escrito y revisado por inspección (más, opcional, la compilación de 1.6). `apply-progress.md` NO debe afirmar RED/GREEN observados; solo "escrita, no ejecutada por el agente". La evidencia real son las Compuertas 1, 2 y 3, que corren y registran las personas.
- Leyenda de las tareas RED: **R** = fallaría por aserción o excepción contra el esqueleto o el GREEN previo. **F** = fijación: ya pasa al escribirla (esqueleto vacío o GREEN previo); su RED efectiva es por mutación (quitar la guarda que fija) y no se observa. Una F nunca se presenta como RED (8 F, 21 R).
- Los nombres de las 26 pruebas son los de la tabla de trazabilidad del spec (autoritativos). Los 3 EXTRA son propuestas de nombre derivadas de filas de "Testing Strategy" del design (AD2, AD4) sin escenario en el spec.

## Phase 0: Guardrails

- [x] 0.1 Confirmar la frontera del diff antes de escribir: solo `Runtime/Harness/`, `Samples~/Harness/`, `Tests/EditMode/Harness/`, `Docs/` y `openspec/changes/2026-09-21-m11-harness-behaviour/`. Nada en `Runtime/Core/`, `Runtime/CoreChannels/`, otro `Runtime/<Modulo>/`, `Data/` ni `package.json` (la entrada del sample ya existe).
- [x] 0.2 Confirmar que no hay cambio de contrato (`Ports.cs`/`Dtos.cs` sin tocar) y que NO se crea `Runtime/Harness/Fakes/` (excepción a la regla dura 4, decisión 3; se documenta en 13.5).
- [x] 0.3 Línea base: `NpcAi.Harness.Tests` tiene hoy 34 pruebas (conteo estático de `[Test]`: 13 + 16 + 5). El usuario registra, si puede, el total global previo de Run All para la aritmética de 11.1.
- [x] 0.4 Nota (sin acción): los `.meta` de archivos y carpetas nuevos bajo `Runtime/Harness/Unity/` y `Tests/EditMode/Harness/` no existirán hasta que el Editor los genere (el agente no fabrica GUIDs): commit de seguimiento en 15.4. Nada bajo `Samples~/` lleva `.meta`. El árbol ya tiene `.meta` ajenos sin trackear (`Training/`, `Registro_Modelo_Etiquetado/`, `openspec/changes/archive/**`): nunca `git add -A` ni `git add .`.

## Phase 1: Foundation (asmdef, esqueleto, andamio)

- [x] 1.1 Crear `Runtime/Harness/Unity/NpcAi.Harness.Unity.asmdef` (AD1), con la forma de `Runtime/SessionLog/Unity/NpcAi.SessionLog.Unity.asmdef`: `references` exactamente `NpcAi.Core`, `NpcAi.Core.Channels`, `NpcAi.Harness`; `autoReferenced: true`; `noEngineReferences: false`.
- [x] 1.2 Crear `Runtime/Harness/Unity/Properties/AssemblyInfo.cs` con `[assembly: InternalsVisibleTo("NpcAi.Harness.Tests")]` y comentario en español (AD11; precedente `Runtime/VrInput/Properties/AssemblyInfo.cs`).
- [x] 1.3 Modificar `Tests/EditMode/Harness/NpcAi.Harness.Tests.asmdef` (AD12): agregar SOLO `NpcAi.Core.Channels` y `NpcAi.Harness.Unity` a `references` (ni `NpcAi.Nlu` ni `NpcAi.SessionLog.Unity`).
- [x] 1.4 Crear el esqueleto `Runtime/Harness/Unity/HarnessBehaviour.cs` (namespace `NpcAi.Harness.Unity`, `sealed`, SIN `[DefaultExecutionOrder]` todavía): los 5 campos `[SerializeField] internal` (`_canalDeUtterance`, `_canalDeAccion`, `_canalDeRespuesta`, `_prefijoDeEtiqueta = "banco"`, `_arrancarSolo`); firmas de `Inyectar`, `IniciarSesion`, `FinalizarSesion`, `DeclararTriaje`; `EtiquetaActiva` (`""`); `OnEnable`/`OnDisable`/`Start`/`OnApplicationQuit` privados VACÍOS; y las 4 costuras `internal` ya delegando (`CablearParaPrueba` asigna campos, con `_reloj` = `DateTime.Now` por defecto, y llama `OnEnable()` sin pasar por `Inyectar`). Sin lógica de comportamiento: cada RED falla por aserción, no por compilación.
- [x] 1.5 Andamio en `Tests/EditMode/Harness/HarnessBehaviourWiringTests.cs` (namespace `NpcAi.Harness.Tests`, sin `[Test]` todavía): helper que arma los 3 canales con `ScriptableObject.CreateInstance<T>()`, los 5 espías de `EspiasDeArnes.cs` (sin modificarlo), un `SessionDirector` real (1 caso, 1 personalidad; `EspiaObjetivo.AssignCase` como `asignarCasoAlObjetivo`; delegado de triaje que graba), un `new GameObject` con `AddComponent<HarnessBehaviour>()`, un contador suscrito a `NpcReplyChannel` y `[TearDown]` con `Object.DestroyImmediate` (patrón `VrInputBehaviourWiringTests`).
- [x] 1.6 (Opcional) Red de seguridad SOLO de compilación: proyecto `dotnet` de descarte FUERA del repo que compile con `dotnet build` `Runtime/Core/`, `Runtime/CoreChannels/`, `Runtime/Harness/**` y `Tests/EditMode/Harness/*.cs` contra los DLL administrados del Editor instalado (`UnityEngine.CoreModule.dll`, ruta no verificada) y `nunit.framework.dll`; si no existen, omitir. Detecta errores de firma y de uso antes de la Compuerta 1. **No ejecuta pruebas y no se cita como evidencia RED/GREEN.** No cubre `Samples~/` (Sentis) y en un solo ensamblado no valida `InternalsVisibleTo`. Repetible tras cada GREEN.

## Phase 2: TDD — A. Simetría de suscripción

- [x] 2.1 RED `OnEnable_suscribe_los_dos_canales_de_entrada` (R): director inyectado con `CablearParaPrueba`; `Raise` de una `Utterance` y de una `PhysicalAction` ⇒ `EspiaClasificador.UltimoTexto` es el texto y `EspiaReceptividad.Evaluaciones` registra esa acción.
- [x] 2.2 RED `OnDisable_desuscribe_los_dos_canales_sin_fuga` (R): control positivo (antes de `DescablearParaPrueba` un `Raise` sí llega a los espías); luego `Raise` en ambos ⇒ contadores congelados y 0 `NpcReply` en el contador de `NpcReplyChannel`.
- [x] 2.3 RED `Volver_a_habilitar_no_duplica_la_suscripcion` (R): `CablearParaPrueba`, `DescablearParaPrueba`, `CablearParaPrueba` y un `Raise` de `Utterance` ⇒ `EspiaClasificador.Invocaciones == 1` (un oyente fugado daría 2). La aserción de 1 `NpcReply` se agrega en 3.6, cuando existe el `Raise` de salida.
- [x] 2.4 FIJACION (F) `Canal_de_entrada_sin_asignar_no_lanza`: con `_canalDeUtterance` o `_canalDeAccion` nulo, `Assert.DoesNotThrow` sobre `CablearParaPrueba` y `DescablearParaPrueba`.
- [x] 2.5 GREEN en `HarnessBehaviour.cs`: `OnEnable` suscribe `AlRecibirUtterance` y `AlRecibirAccion` a los DOS canales de entrada; `OnDisable` los desuscribe (simetría estricta: el canal no deduplica). Guardas `!= null`, nunca `?.` sobre `UnityEngine.Object`. Nunca se suscribe a `_canalDeRespuesta` (AD9) ni se cierra la bitácora aquí (AD6). Los handlers solo reenvían a `_director.ProcesarTurno`/`ProcesarAccion`, sin `Raise` todavía.

## Phase 3: TDD — B. Salida y conteo de `Raise`

- [x] 3.1 RED `Turno_social_produce_exactamente_un_Raise_con_la_respuesta_del_director` (R): `EspiaClinico` en `NoAplica` y `EspiaDialogo.Respuesta` = R ⇒ el contador de `NpcReplyChannel` recibe 1 `NpcReply` igual a R en `Text`, `EmotionTag` y `AnimationCue`; el espía de M2 recibió el texto sin alterar.
- [x] 3.2 RED `Turno_clinico_manejado_produce_exactamente_un_Raise` (R): `EspiaClinico.Respuesta = new Core.ClinicalResponse(true, C)` (prefijo `Core.`, como en `EspiasDeArnes.cs`) ⇒ 1 `NpcReply` igual a C y `EspiaDialogo.Invocaciones == 0`.
- [x] 3.3 FIJACION (F) `Accion_fisica_sola_no_publica_nada_pero_llega_al_director`: `Raise(PhysicalAction.Acercarse)` ⇒ 0 `NpcReply`, `EspiaReceptividad.Evaluaciones` con `Acercarse`, y `EspiaClasificador`, `EspiaClinico` y `EspiaDialogo` sin invocaciones.
- [x] 3.4 FIJACION (F) `Canal_de_respuesta_sin_asignar_no_lanza`: `_canalDeRespuesta` nulo ⇒ `Assert.DoesNotThrow` al `Raise` de una `Utterance`, y `EspiaClasificador` registró el texto.
- [x] 3.5 GREEN: `AlRecibirUtterance` hace UN solo `_canalDeRespuesta.Raise(reply)` con lo que devuelve `ProcesarTurno`, con guarda `!= null` (AD9); `AlRecibirAccion` publica solo si `reply.HasValue` (siempre `null` en esta entrega: la guarda es diseño y no es observable).
- [x] 3.6 Reforzar la prueba de 2.3 (mismo método, sin crear otro): agregar la aserción exacta del spec (el contador de `NpcReplyChannel` recibe exactamente 1 `NpcReply`).

## Phase 4: TDD — C. Frontera de inyección y rutas sin director

- [x] 4.1 RED `Sin_director_los_canales_de_entrada_no_publican` (R): `CablearParaPrueba()` sin argumentos, 3 canales asignados; `Raise` de `Utterance` y de `PhysicalAction` ⇒ `Assert.DoesNotThrow` y 0 `NpcReply`.
- [x] 4.2 RED `El_orden_entre_inyeccion_y_habilitacion_no_cambia_el_resultado` (R): cáscara 1 = `CablearParaPrueba()` y luego `Inyectar(...)`; cáscara 2 = `CablearParaPrueba(director)`; `Raise` en cada una ⇒ exactamente 1 `NpcReply` por cáscara (canales y contadores propios).
- [x] 4.3 RED EXTRA (fuera de las 26; nombre propuesto) `Inyectar_con_director_nulo_lanza_ArgumentNullException` (R): `Assert.Throws<ArgumentNullException>(() => b.Inyectar(null, null, null))` (AD4).
- [x] 4.4 GREEN: `if (_director == null) return;` al inicio de ambos handlers (AD4: silencioso en handlers); `Inyectar` asigna `_director = director ?? throw new ArgumentNullException(nameof(director))`, `_abrirBitacora` y `_cerrarBitacora` (AD4/AD5). La suscripción no depende de la inyección.

## Phase 5: TDD — D. Inicio de sesión (cuatro efectos y etiqueta)

- [x] 5.1 RED `IniciarSesion_dispara_los_cuatro_efectos_juntos_con_el_mismo_caso` (R): `IniciarSesion()` una vez ⇒ `EspiaReceptividad.Resets` con 1 entrada = `PersonalidadActual`; `EspiaObjetivo` y `EspiaClinico` con 1 asignación cada uno y el mismo caso = `CasoActual`; la lambda `abrirBitacora` con 1 llamada cuya etiqueta contiene ese caso.
- [x] 5.2 RED `IniciarSesion_sin_oyente_en_la_costura_de_M13_dispara_igual_los_tres_efectos_del_director` (R): `abrirBitacora` y `cerrarBitacora` nulos ⇒ `Assert.DoesNotThrow` y los 3 efectos del director ocurren.
- [x] 5.3 RED `Etiqueta_lleva_el_prefijo_y_el_caso_elegido` (R): prefijo `"sim"` y catálogo de un solo caso `caso-02` ⇒ empieza por `"sim"` y contiene `"caso-02"`; con reloj fijo (`CablearParaPrueba(..., reloj)`), prefijo por defecto y `caso-01`, igualdad exacta `banco-caso-01-20260921-143512` (AD7).
- [x] 5.4 RED `Instantes_distintos_dan_etiquetas_distintas` (R): catálogo de 1 caso, mismo prefijo, reloj de prueba con dos instantes separados por 1 s ⇒ las dos etiquetas grabadas difieren.
- [x] 5.5 RED `Prefijo_vacio_no_produce_etiqueta_vacia` (R): `_prefijoDeEtiqueta = ""` ⇒ etiqueta ni vacía ni en blanco y contiene el caso.
- [x] 5.6 FIJACION (F) `Sin_director_IniciarSesion_no_abre_la_sesion_de_M13`: `CablearParaPrueba(abrirBitacora: ...)` sin director ⇒ `IniciarSesion()` deja la costura en 0 llamadas y no lanza.
- [x] 5.7 GREEN: `IniciarSesion()` con guarda `_director == null` ⇒ `return`; `_director.IniciarSesion()` (efectos 1-3); `EtiquetaActiva = _prefijoDeEtiqueta + "-" + _director.CasoActual.Value + "-" + _reloj().ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture)`; `if (_abrirBitacora != null) _abrirBitacora(EtiquetaActiva)` (efecto 4). Orden fijo: el caso solo se conoce después del arranque (AD7).

## Phase 6: TDD — E. Cierre (`FinalizarSesion`, reinicio, `OnApplicationQuit`)

- [x] 6.1 RED `FinalizarSesion_dispara_la_costura_de_cierre_una_vez_sin_tocar_el_director` (R): `IniciarSesion()` y `FinalizarSesion()` ⇒ `cerrarBitacora` con 1 llamada y sin llamadas nuevas en `EspiaReceptividad`, `EspiaObjetivo` ni `EspiaClinico`.
- [x] 6.2 RED `FinalizarSesion_sin_sesion_abierta_es_no_op` (R): `FinalizarSesion()` sin sesión ⇒ 0 llamadas; `IniciarSesion()`, `FinalizarSesion()`, `FinalizarSesion()` ⇒ 1 llamada en total.
- [x] 6.3 RED `OnApplicationQuit_con_sesion_abierta_cierra_la_sesion_de_M13` (R): `IniciarSesion()` y `SalirParaPrueba()` ⇒ `cerrarBitacora` con 1 llamada.
- [x] 6.4 RED `IniciarSesion_con_otra_abierta_cierra_primero_la_anterior` (R): dos `IniciarSesion()` seguidos con una lista compartida por ambas lambdas y reloj de dos instantes distintos ⇒ secuencia `inicio, cierre, inicio` y etiquetas distintas.
- [x] 6.5 GREEN: `FinalizarSesion()` = `if (string.IsNullOrEmpty(EtiquetaActiva)) return;`, luego `if (_cerrarBitacora != null) _cerrarBitacora();` y `EtiquetaActiva = ""`. `OnApplicationQuit()` llama `FinalizarSesion()` (AD6b; NUNCA desde `OnDisable`). `IniciarSesion()` llama `FinalizarSesion()` justo después de la guarda del director (AD6c).

## Phase 7: TDD — F. Punto de entrada y arranque automático

- [x] 7.1 FIJACION (F) `IniciarSesion_es_publico_sin_parametros_y_sin_retorno`: `typeof(HarnessBehaviour).GetMethod("IniciarSesion", Type.EmptyTypes)` no nulo, `IsPublic` y `ReturnType == typeof(void)` (no hay sobrecarga: AD8).
- [x] 7.2 FIJACION (F) `Con_el_flag_por_defecto_arrancar_no_inicia_sesion`: componente recién agregado, `CablearParaPrueba(director, ...)` y `ArrancarParaPrueba()` ⇒ ninguno de los 4 efectos.
- [x] 7.3 RED `Con_el_flag_activo_arrancar_inicia_exactamente_una_sesion` (R): `_arrancarSolo = true`, `CablearParaPrueba(director, ...)` y `ArrancarParaPrueba()` ⇒ cada uno de los 4 efectos exactamente una vez.
- [x] 7.4 GREEN: `Start()` = `if (_arrancarSolo) IniciarSesion();` (AD3: nunca en `Awake`/`OnEnable`, donde `SessionLogBehaviour` aún no armó su recorder y la bitácora quedaría vacía); `_arrancarSolo` por defecto falso.

## Phase 8: TDD — G. `DeclararTriaje`

- [x] 8.1 RED `DeclararTriaje_reenvia_la_categoria_sin_alterarla` (R): delegado de triaje del director que graba ⇒ `DeclararTriaje("  Rojo ")` entrega exactamente `"  Rojo "`.
- [x] 8.2 RED `DeclararTriaje_no_publica_ni_toca_la_sesion_de_M13` (R, por control positivo): `DeclararTriaje("rojo")` con contador en `NpcReplyChannel` y lambdas en ambas costuras ⇒ el delegado recibió `"rojo"`, 0 `NpcReply` y 0 llamadas en las dos costuras.
- [x] 8.3 FIJACION (F) EXTRA (fuera de las 26; nombre propuesto) `DeclararTriaje_sin_director_es_no_op`: sin director, `Assert.DoesNotThrow(() => b.DeclararTriaje("rojo"))`.
- [x] 8.4 GREEN: `DeclararTriaje(string categoria)` = `if (_director == null) return; _director.DeclararTriaje(categoria);`. Pass-through crudo: sin recortar, sin normalizar, sin `Raise`, sin tocar la bitácora.

## Phase 9: TDD — H. Estructural

- [x] 9.1 FIJACION (F) `Ensamblado_compilado_no_referencia_otros_modulos_NpcAi`: de `typeof(HarnessBehaviour).Assembly.GetReferencedAssemblies()`, todo nombre que empiece por `NpcAi.` pertenece a la lista blanca `{NpcAi.Core, NpcAi.Core.Channels, NpcAi.Harness, NpcAi.Harness.Unity}` (blanca, no negra); guarda anti-vacuidad: la enumeración contiene `NpcAi.Core`.
- [x] 9.2 RED EXTRA (fuera de las 26; nombre propuesto) `DefaultExecutionOrder_de_la_cascara_es_positivo` (R): `GetCustomAttribute<DefaultExecutionOrder>(typeof(HarnessBehaviour))` no nulo y `order > 0` (AD2: solo se afirma que el atributo existe; el efecto no es observable en EditMode).
- [x] 9.3 GREEN: agregar `[DefaultExecutionOrder(100)]` a `HarnessBehaviour` (AD2).

## Phase 10: Verificación estática y trazabilidad

- [x] 10.1 Criterio de `sdd-verify`, NO prueba EditMode: `Runtime/Harness/Unity/NpcAi.Harness.Unity.asmdef` declara en `references` exactamente `NpcAi.Core`, `NpcAi.Core.Channels` y `NpcAi.Harness` (lectura con `rg`/Read). `GetReferencedAssemblies()` no lo ve: una referencia declarada y no usada no aparece.
- [x] 10.2 Criterio de `sdd-verify`, NO prueba EditMode: `rg "Debug\."` sobre `Runtime/Harness/` ⇒ 0 coincidencias (hoy 0). `noEngineReferences: false` devolvió la posibilidad de compilarlo: no escribir esa cadena ni en comentarios ni en XML docs.
- [x] 10.3 Chequeo de trazabilidad (Grep `-c` por nombre): `HarnessBehaviourWiringTests.cs` tiene exactamente 29 `[Test]`; cada uno de los 26 nombres de la tabla del spec aparece exactamente una vez (tareas 2.1-9.1) y los 3 EXTRA (4.3, 8.3, 9.2) una vez cada uno; ningún nombre renombrado.

## Phase 11: Compuerta humana 1

- [x] 11.1 MANUAL (Unity Editor; la ejecuta y registra el usuario, no el agente): Test Runner > EditMode > Run All en verde, con el total registrado. **Resultado (usuario, 2026-09-21): todos los tests en verde; el total no se indicó (esperado: 63).** Aritmética del ensamblado `NpcAi.Harness.Tests`: 34 existentes + 26 de la trazabilidad + 3 EXTRA = **63** (60 si se descartan los EXTRA); Run All global = línea base de 0.3 + 29. 0 fallidas, 0 omitidas. Es la primera compilación real de `Runtime/Harness/Unity/` y genera los `.meta` de 15.4.

## Phase 12: Raíz de composición del anfitrión (sin pruebas por diseño)

- [x] 12.1 Crear `Samples~/Harness/CompositorDeArnes.cs` (AD10): `MonoBehaviour` en `NpcAi.Samples.Harness`, sin `.asmdef` y SIN `.meta`, sin la cadena `Debug.`; mínimo: construir e inyectar. Campos serializados: `TextAsset[] _casos`, `CorpusDePersonalidad[] _corpus` (clase anidada `[Serializable]` con `public string Personalidad; public TextAsset Corpus;`; Unity no serializa tuplas), `_modelo`, `_tokenizador`, `_semilla`, `_arnes` (`HarnessBehaviour`), `_bitacora` (`SessionLogBehaviour`).
- [x] 12.2 En su `Awake`: `CargarCaso` comparando `new ClinicalCaseId(asset.name) == id` (no `asset.name == id.Value`); catálogos derivados de las mismas dos fuentes; `new BertIntentClassifier(_modelo, _tokenizador)`, `new ReceptivityEngine()`, `new MarkovDialogueGenerator(corpus)`, `new TriageScenarioObjective(new TriageObjectiveSettings(), CargarCaso)`, `new ClinicalResponder(CargarCaso)`; `new SessionDirector(m2, m4, m6, m9, m15, m9.AssignCase, m9.DeclareTriage, casos, personalidades, _semilla)`; `_arnes.Inyectar(director, _bitacora.IniciarSesion, _bitacora.FinalizarSesion)`. `OnDestroy`: `(m2 as IDisposable)?.Dispose()`.
- [x] 12.3 Revisión por lectura (el agente no puede compilar `Samples~/`: Unity no la compila dentro del paquete y depende de Sentis): contrastar cada constructor y `m9.AssignCase`/`m9.DeclareTriage` con sus firmas reales y anotar en `apply-progress.md` "revisado por lectura, no compilado". La primera compilación real es 14.1.

## Phase 13: Documentación

- [x] 13.1 Reescribir `Samples~/Harness/README.md`, sección 1 (PRIMER punto): los 8 slots sobre los 3 assets, uno por uno. `UtteranceChannel`: `SpeechToTextBehaviour._canalDeUtterance` (M1), `SessionLogBehaviour._canalDeUtterance` (M13), `HarnessBehaviour._canalDeUtterance`. `PhysicalActionChannel`: `VrInputBehaviour._canal` (M7), `HarnessBehaviour._canalDeAccion`. `NpcReplyChannel`: `NpcPresenterBehaviour._canal` (M8), `SessionLogBehaviour._canalDeRespuesta` (M13), `HarnessBehaviour._canalDeRespuesta`. Retirar las promesas fuera de alcance del README actual (teclado, HUD, latencias).
- [x] 13.2 README secciones 2-3: `SpeechToTextBehaviour` solo captura entre `StartListening` y `StopListening` (`SpeechToTextBehaviour.cs:53-66`), así que cablear un control de escena a ambos; controles de la cáscara `IniciarSesion`, `FinalizarSesion`, `DeclararTriaje(string)`, `_arrancarSolo` (falso) y `_prefijoDeEtiqueta` (`banco`); `OnApplicationQuit` con frecuencia NO dispara en Quest/Android, por lo que el control de `FinalizarSesion` es obligatorio (sin él `ReanudarUltimaSesion` concatena la sesión siguiente); sin control para `DeclararTriaje` el componente de triaje de M9 queda en 0.
- [x] 13.3 README sección 4 (AD2): GameObjects de M13 y de la cáscara activos al cargar la escena y sin alternarlos; no sobrescribir `[DefaultExecutionOrder(100)]` con `Project Settings > Script Execution Order`; escena aditiva o prefab instanciado quedan fuera de la garantía; fiabilidad ~85% según la revisión independiente del diseño, solo en el mismo pase de carga; el transcript de `ExportarTextoPlano()` (`Usuario` antes que `Npc`) es la única red.
- [x] 13.4 README sección 5: referencias que necesita un anfitrión con `.asmdef` propio (nombres exactos leídos de los `.asmdef` de `Runtime/`: `NpcAi.Core`, `NpcAi.Harness`, `NpcAi.Harness.Unity` y los de M2, M4, M6, M9, M13, M15), importación del sample desde Package Manager, campos de Inspector del compositor y pasos de construcción de la escena (el `.unity` no se versiona).
- [x] 13.5 `Docs/MODULES.md`, sección M11 (líneas ~368-381): reemplazar "Estado actual" y "Specs formales" obsoletos por el estado real: `Runtime/Harness/` (director, PR1), `Runtime/Harness/Unity/` (cáscara), `Samples~/Harness/` (compositor y README), spec `openspec/specs/banco-de-pruebas-m11/spec.md`. Nombrar lo diferido: M10/M16, brecha M9↔M15 de banderas rojas, las 3 acciones de M7 (`EntregarObjeto`, `SenalarPantalla`, `GestoCalma`), `IntentResultChannel` y `ReceptivityChangeChannel` sin cablear, sin control de UI para `DeclararTriaje`, escena `.unity` fuera del repo. Nota W7 (decisión 3, vetable por el usuario): `Runtime/Harness/` no publica `Fakes/` porque consume puertos y no implementa ninguno, y `Core`/`CoreChannels` tampoco los publican. No afirmar resultados de compuertas: remitir a `apply-progress.md`.
- [x] 13.6 `Runtime/Harness/SessionDirector.cs` línea 25, solo comentario: `HarnessBehaviour</c>, PR2` pasa a `CompositorDeArnes</c>, PR2`. Cero cambio de comportamiento; releer la línea para confirmar.

## Phase 14: Compuertas humanas 2 y 3

- [ ] 14.1 MANUAL (Editor; el usuario): importar el sample desde Package Manager; la Consola no muestra errores de `CompositorDeArnes.cs`.
- [ ] 14.2 MANUAL: crear los 3 assets de canal y asignar los 8 slots de 13.1 uno por uno, cada uno al asset compartido correcto; cablear el compositor y los controles (`StartListening`/`StopListening`, `IniciarSesion`, `FinalizarSesion`).
- [ ] 14.3 MANUAL: jugar 2 o más turnos y verificar que `ExportarTextoPlano()` muestra `Usuario` antes que `Npc` en cada turno (AD2); que tras `IniciarSesion` `ListarSesiones()` muestra la etiqueta `{prefijo}-{caso}-{yyyyMMdd-HHmmss}` y la sesión NO está vacía (AD3/AD5); y que tras `FinalizarSesion` la sesión figura cerrada y una nueva empieza su numeración en cero (AD6). Riesgo residual AD2: si el transcript sale invertido, NO parchear aquí; abrir un cambio propio y pequeño que difiera el `Raise` al siguiente `Update` con cola y `BombearParaPrueba` (patrón M1/M7/M8).
- [ ] 14.4 MANUAL (Quest 3 físico, patrón M1/M2/M7): sesión completa (se habla con `StartListening`, el NPC responde, cambia la receptividad, se mueve el progreso de M9, M13 registra los turnos); cerrar con el control de `FinalizarSesion` y registrar si `OnApplicationQuit` dispara en el dispositivo.

## Phase 15: Cierre

- [ ] 15.1 `git add` acotado, por rutas explícitas: `Runtime/Harness/`, `Samples~/Harness/`, `Tests/EditMode/Harness/`, `Docs/`, `openspec/changes/2026-09-21-m11-harness-behaviour/`. Sin `.meta` en este commit (van en 15.4); nunca `git add -A` ni `git add .`.
- [ ] 15.2 `git diff --cached --stat` antes de cualquier commit: cero líneas fuera de esas 5 rutas; nada en `Runtime/Core/`, `Runtime/CoreChannels/`, otro `Runtime/<Modulo>/`, `Data/` ni `package.json`; ningún `.meta` bajo `Samples~/`; único `.asmdef` modificado: `Tests/EditMode/Harness/NpcAi.Harness.Tests.asmdef`.
- [ ] 15.3 Checklist "Antes de mergear": Compuerta 1 en verde con el total registrado (11.1); Compuertas 2 y 3 registradas en `apply-progress.md` o diferidas por decisión explícita del usuario; 10.1-10.3 cumplidas; rama al día con `main`.
- [ ] 15.4 Commit de seguimiento con los `.meta` que el Editor generó en 11.1: `Runtime/Harness/Unity.meta` y `Runtime/Harness/Unity/Properties.meta` (carpetas) más los de `NpcAi.Harness.Unity.asmdef`, `HarnessBehaviour.cs`, `Properties/AssemblyInfo.cs` y `Tests/EditMode/Harness/HarnessBehaviourWiringTests.cs`. Ninguno bajo `Samples~/`; los `.meta` que Unity genere para `openspec/` y `Docs/` quedan sin trackear (misma convención que PR1).

## Notas

- AD13 pide `.meta` en el mismo commit; como el agente no fabrica GUIDs, se cumple a nivel de PR (15.4 inmediatamente después), igual que el commit `a4118f8` de PR1.
- La adenda de spec ya está escrita en `specs/banco-de-pruebas-m11/spec.md`. Su promoción a `openspec/specs/banco-de-pruebas-m11/spec.md` y la actualización de su `Purpose` ocurren en `sdd-archive`, no aquí.
- Las verificaciones estáticas 10.1 y 10.2 son criterios de `sdd-verify`; el agente de apply puede pre-correrlas y `sdd-verify` las repite de forma independiente.
- Tareas F (fijación): 2.4, 3.3, 3.4, 5.6, 7.1, 7.2, 8.3, 9.1. Su RED efectiva es por mutación; no declarar RED observado en `apply-progress.md`.
