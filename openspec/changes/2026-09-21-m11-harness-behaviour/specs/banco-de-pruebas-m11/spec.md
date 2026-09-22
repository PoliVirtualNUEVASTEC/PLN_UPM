# Delta for banco-de-pruebas-m11

Adenda de PR2 (`2026-09-21-m11-harness-behaviour`) sobre la capacidad `banco-de-pruebas-m11`.
Especifica el comportamiento observable de `HarnessBehaviour` (`Runtime/Harness/Unity/`,
ensamblado `NpcAi.Harness.Unity`): la cascara `MonoBehaviour` que conecta los canales de evento con
`SessionDirector`. Los 7 requisitos de PR1 no cambian; este delta solo AGREGA.

Terminos. "La cascara" es `HarnessBehaviour`. "Canales de entrada" son `UtteranceChannel` y
`PhysicalActionChannel`. "Costura de M13" es el punto por el que la cascara le pide a la bitacora
abrir o cerrar su sesion, sin referenciar `NpcAi.SessionLog.Unity`. Lo que la propuesta deja a
`sdd-design` (como se inyecta el director, que tipo tiene la costura de M13, de donde sale la hora)
se describe aqui solo como comportamiento observable.

## ADDED Requirements

### Requirement: Simetria de suscripcion sobre los dos canales de entrada

La cascara DEBE suscribirse en `OnEnable` a `UtteranceChannel` y a `PhysicalActionChannel`, los
dos, y DEBE desuscribirse de ambos en `OnDisable` sin dejar ningun oyente vivo (fuga). Un canal de
entrada sin asignar NO DEBE hacer que `OnEnable` ni `OnDisable` lancen.

#### Scenario: OnEnable suscribe los dos canales de entrada
- Dado una cascara con un director inyectado (espias de M2 y M4) y ambos canales de entrada
  asignados
- Cuando se ejecuta `OnEnable` y se hace `Raise` de una `Utterance` en `UtteranceChannel` y de una
  `PhysicalAction` en `PhysicalActionChannel`
- Entonces el espia de M2 registra el texto de la `Utterance` y el espia de M4 registra una
  evaluacion con esa `PhysicalAction`

#### Scenario: OnDisable desuscribe los dos canales sin fuga
- Dado la cascara del escenario anterior, habilitada, con un oyente contador en `NpcReplyChannel`
- Cuando se ejecuta `OnDisable` y se hace `Raise` en ambos canales de entrada
- Entonces ningun espia registra llamadas nuevas y el oyente no recibe ningun `NpcReply`

#### Scenario: Volver a habilitar no duplica la suscripcion
- Dado una cascara habilitada, deshabilitada y habilitada de nuevo, con un oyente contador en
  `NpcReplyChannel`
- Cuando se hace `Raise` de una `Utterance` en `UtteranceChannel`
- Entonces el oyente recibe exactamente un `NpcReply` (un oyente fugado en el primer ciclo daria
  dos)

#### Scenario: Un canal de entrada sin asignar no lanza
- Dado una cascara con `UtteranceChannel` o `PhysicalActionChannel` sin asignar
- Cuando se ejecutan `OnEnable` y `OnDisable`
- Entonces ninguno de los dos lanza

### Requirement: Un solo `Raise` por turno hablado y ninguno por accion fisica sola

Ante una `Utterance` recibida por `UtteranceChannel`, la cascara DEBE reenviarla al director sin
alterarla y DEBE publicar exactamente un `Raise` en `NpcReplyChannel` con el `NpcReply` que devolvio
`ProcesarTurno`, sin alterarlo, sea por la via clinica o por la social. Ante una `PhysicalAction`
recibida por `PhysicalActionChannel`, DEBE reenviarla a `ProcesarAccion` y NO DEBE publicar nada en
`NpcReplyChannel` (`ProcesarAccion` devuelve `null`). Un `NpcReplyChannel` sin asignar NO DEBE hacer
que un turno lance.

#### Scenario: Turno social produce exactamente un Raise
- Dado una cascara habilitada con un director cuyo M15 devuelve `Handled == false` y cuyo M6
  devuelve una `NpcReply` R, y un oyente contador en `NpcReplyChannel`
- Cuando se hace `Raise` de una `Utterance` en `UtteranceChannel`
- Entonces el oyente recibe exactamente un `NpcReply`, igual a R en `Text`, `EmotionTag` y
  `AnimationCue`, y el espia de M2 recibio el texto sin alterar

#### Scenario: Turno clinico manejado produce exactamente un Raise
- Dado el mismo montaje, con M15 devolviendo `Handled == true` y una `NpcReply` C
- Cuando se hace `Raise` de una `Utterance` en `UtteranceChannel`
- Entonces el oyente recibe exactamente un `NpcReply`, igual a C, y M6 no se invoca

#### Scenario: Una accion fisica sola no publica nada
- Dado el mismo montaje
- Cuando se hace `Raise` de `PhysicalAction.Acercarse` en `PhysicalActionChannel`
- Entonces el oyente recibe cero `NpcReply`, el espia de M4 registra la evaluacion con `Acercarse`
  y ni `Classify`, ni `Respond`, ni `Generate` se invocan

#### Scenario: Un canal de respuesta sin asignar no lanza
- Dado una cascara habilitada con un director inyectado y `NpcReplyChannel` sin asignar
- Cuando se hace `Raise` de una `Utterance` en `UtteranceChannel`
- Entonces no lanza y el espia de M2 registra el texto (el director si proceso el turno)

### Requirement: Inicio de sesion como una sola operacion con cuatro efectos

`IniciarSesion` de la cascara DEBE producir, en una sola invocacion, los tres efectos de
`SessionDirector.IniciarSesion()` (`m4.Reset(personalidad)`, `m9.AssignCase(caso)` y
`m15.AssignCase(caso, personalidad)`, con el mismo `ClinicalCaseId` en M9 y M15) y un cuarto
efecto: pedir a M13, por la costura de inicio de sesion, que abra su sesion de bitacora con la
etiqueta del requisito "Etiqueta de la sesion de bitacora". Cada efecto DEBE ocurrir exactamente
una vez por invocacion. La eleccion de `(caso, personalidad)` es del director (PR1, "Seleccion
reproducible por semilla"): la cascara NO DEBE elegir por su cuenta. Sin ningun oyente en la
costura de M13, los tres efectos del director DEBEN ocurrir igual y `IniciarSesion` NO DEBE lanzar.

#### Scenario: Los cuatro efectos se disparan juntos con el mismo caso
- Dado una cascara habilitada con un director inyectado (espias de M4, M9 y M15) y un oyente en la
  costura de M13
- Cuando se llama `IniciarSesion` una vez
- Entonces M4 recibe un `Reset` con `PersonalidadActual`, M9 y M15 reciben una asignacion cada uno
  con el mismo caso, igual a `CasoActual`, y la costura de M13 recibe una llamada con una etiqueta
  que contiene ese caso

#### Scenario: Sin oyente en la costura de M13 los tres efectos del director ocurren igual
- Dado la misma cascara sin ningun oyente en la costura de M13
- Cuando se llama `IniciarSesion`
- Entonces no lanza y M4, M9 y M15 reciben cada uno su efecto

### Requirement: Punto de entrada publico y arranque automatico opcional

La cascara DEBE exponer `IniciarSesion` como metodo publico, sin parametros y sin valor de retorno,
de modo que un control de escena lo cablee desde el Inspector. DEBE ofrecer ademas un flag de
arranque automatico, configurable desde el Inspector y desactivado por defecto. Con el flag
inactivo la cascara NO DEBE iniciar ninguna sesion por su cuenta; con el flag activo y un director
inyectado DEBE iniciar exactamente una sesion (los cuatro efectos) al arrancar.

#### Scenario: IniciarSesion es publico, sin parametros y sin retorno
- Dado el tipo `HarnessBehaviour`
- Cuando se localiza por reflexion su metodo sin parametros
  (`GetMethod("IniciarSesion", Type.EmptyTypes)`)
- Entonces existe, es publico y devuelve `void`

#### Scenario: Con el flag por defecto, arrancar no inicia sesion
- Dado una cascara recien agregada a un `GameObject` (flag en su valor por defecto) con un director
  inyectado
- Cuando arranca (habilitacion y arranque de escena)
- Entonces no se dispara ninguno de los cuatro efectos

#### Scenario: Con el flag activo, arrancar inicia exactamente una sesion
- Dado una cascara con el flag de arranque automatico activo y un director inyectado
- Cuando arranca (habilitacion y arranque de escena)
- Entonces cada uno de los cuatro efectos ocurre exactamente una vez

### Requirement: Etiqueta de la sesion de bitacora

La etiqueta que la cascara entrega a M13 DEBE comenzar con el prefijo serializado (configurable
desde el Inspector) y DEBE contener el identificador del caso que el director eligio para esa
sesion (`CasoActual`) y una marca de tiempo. Dos sesiones con el mismo prefijo y el mismo caso,
iniciadas en instantes distintos, DEBEN recibir etiquetas distintas: M13 acumula los turnos bajo una
etiqueta repetida y deja la desambiguacion a quien la construye (`bitacora-sesion-m13`). La
etiqueta NO DEBE ser vacia ni en blanco, tampoco con el prefijo vacio (M13 sustituye una etiqueta en
blanco por un valor por defecto unico y fusionaria sesiones distintas).

#### Scenario: La etiqueta lleva el prefijo y el caso elegido
- Dado un prefijo "sim" y un director que elige el caso `caso-02`
- Cuando se llama `IniciarSesion`
- Entonces la etiqueta que recibe la costura de M13 comienza con "sim" y contiene "caso-02"

#### Scenario: Instantes distintos dan etiquetas distintas
- Dado un catalogo de un solo caso, el mismo prefijo y dos inicios de sesion en instantes distintos
  (a la resolucion de la marca de tiempo; como la prueba controla el reloj lo decide `sdd-design`)
- Cuando se comparan las dos etiquetas que recibe la costura de M13
- Entonces son distintas (con un solo caso, la marca de tiempo es lo unico que las distingue)

#### Scenario: Un prefijo vacio no produce una etiqueta vacia
- Dado un prefijo vacio
- Cuando se llama `IniciarSesion`
- Entonces la etiqueta no es vacia ni en blanco y contiene el caso elegido

### Requirement: Cierre simetrico de la sesion de bitacora de M13

La cascara DEBE exponer `FinalizarSesion` como metodo publico, sin parametros, que pida a M13, por
la costura de cierre de sesion, cerrar la sesion que la cascara abrio, de modo que
`SessionRecorder.ReanudarUltimaSesion()` no la retome despues y concatene turnos nuevos con los
viejos. Sin una sesion abierta por la cascara, `FinalizarSesion` NO DEBE invocar la costura de
cierre (tampoco en un segundo cierre consecutivo). El cierre NO DEBE alterar el estado de M4, M9 ni
M15: el director no tiene operacion de cierre.

#### Scenario: El cierre dispara la costura una vez sin tocar el director
- Dado una cascara con un director inyectado (espias de M4, M9 y M15) y una sesion iniciada por
  `IniciarSesion`
- Cuando se llama `FinalizarSesion`
- Entonces la costura de cierre recibe exactamente una llamada y ni M4, M9 ni M15 reciben llamadas
  nuevas

#### Scenario: Sin sesion abierta el cierre es no-op
- Dado una cascara sin sesion abierta, o cuya sesion ya se cerro
- Cuando se llama `FinalizarSesion`
- Entonces la costura de cierre no recibe ninguna llamada adicional

### Requirement: La sesion de bitacora no queda abierta por olvido

La cascara DEBERIA cerrar la sesion de M13 que ella abrio en dos casos: (a) al salir de la
aplicacion (`OnApplicationQuit`) con la sesion abierta; (b) al iniciar una sesion nueva con otra
abierta, cerrando primero la anterior. La cascara no usa `OnDisable` para (a): `SessionLogBehaviour`
anula su registrador en su propio `OnDisable` y Unity no ordena `OnDisable` entre componentes, asi
que un cierre pedido desde el `OnDisable` de la cascara podria llegar a un M13 ya deshabilitado y
perderse en silencio; `OnApplicationQuit` corre antes que las llamadas a `OnDisable`.

#### Scenario: Salir de la aplicacion con sesion abierta la cierra
- Dado una cascara con una sesion abierta por `IniciarSesion`
- Cuando se ejecuta `OnApplicationQuit`
- Entonces la costura de cierre recibe exactamente una llamada

#### Scenario: Reiniciar cierra primero la sesion anterior
- Dado una cascara con una sesion abierta por `IniciarSesion`
- Cuando se llama `IniciarSesion` otra vez sin cerrar
- Entonces la secuencia observada en las costuras de M13 es: inicio, cierre, inicio

### Requirement: Frontera de inyeccion del director

La cascara NO DEBE construir el `SessionDirector` ni ninguna implementacion concreta de M2, M4, M6,
M9 ni M15: lo recibe ya armado desde la raiz de composicion del anfitrion, fuera del paquete.
Mientras no tenga un director inyectado, la cascara NO DEBE publicar nada en `NpcReplyChannel` ni
abrir una sesion de M13, ante ningun `Raise` en los canales de entrada ni ante `IniciarSesion`. Si
esa ausencia se manifiesta como no-op silencioso o como fallo ruidoso lo decide `sdd-design`; el
invariante DEBE sostenerse con cualquiera de las dos. Con el director ya inyectado, el resultado NO
DEBE depender de si la inyeccion ocurrio antes o despues de `OnEnable`.

#### Scenario: Sin director, los canales de entrada no publican
- Dado una cascara habilitada con los tres canales asignados y sin director inyectado, con un
  oyente contador en `NpcReplyChannel`
- Cuando se hace `Raise` de una `Utterance` y de una `PhysicalAction` (una eventual excepcion de la
  cascara ante la ausencia no invalida el escenario)
- Entonces el oyente recibe cero `NpcReply`

#### Scenario: Sin director, IniciarSesion no abre la sesion de M13
- Dado una cascara habilitada sin director inyectado y un oyente en la costura de inicio de M13
- Cuando se llama `IniciarSesion` (una eventual excepcion no invalida el escenario)
- Entonces la costura de M13 recibe cero llamadas

#### Scenario: El orden entre inyeccion y habilitacion no cambia el resultado
- Dado dos cascaras con los tres canales asignados: a la primera se le inyecta un director con
  espias despues de habilitarla y a la segunda antes de habilitarla
- Cuando se hace `Raise` de una `Utterance` en `UtteranceChannel` en cada una
- Entonces cada una publica exactamente un `NpcReply` en `NpcReplyChannel`

### Requirement: Fronteras estructurales de la cascara

`NpcAi.Harness.Unity` DEBE referenciar, entre los ensamblados `NpcAi.*`, unicamente `NpcAi.Core`,
`NpcAi.Core.Channels` y `NpcAi.Harness` (mas ensamblados del motor), y NO DEBE referenciar ningun
otro modulo (regla dura 3). Referenciar `NpcAi.Harness` no cruza esa regla: es el propio modulo
(precedente `NpcAi.SessionLog.Unity` hacia `NpcAi.SessionLog`). La cascara NO DEBE usar `Debug.Log`
ni `UnityEngine.Debug`: los puertos no imprimen, devuelven. EditMode verifica solo lo compilado
(lista de permitidos); las referencias declaradas del `.asmdef` y la ausencia de `Debug` bajo
`Runtime/Harness/` se verifican de forma estatica en `sdd-verify` (`rg`).

#### Scenario: El ensamblado compilado no referencia otros modulos
- Dado el ensamblado compilado de `HarnessBehaviour`
- Cuando se enumeran por reflexion sus ensamblados referenciados cuyo nombre empieza por `NpcAi.`
- Entonces cada uno pertenece a la lista de permitidos: `NpcAi.Core`, `NpcAi.Core.Channels`,
  `NpcAi.Harness` y `NpcAi.Harness.Unity`

#### Scenario: El asmdef declara exactamente tres referencias
- Dado `Runtime/Harness/Unity/NpcAi.Harness.Unity.asmdef`
- Cuando `sdd-verify` lee su lista de referencias declaradas
- Entonces es exactamente `NpcAi.Core`, `NpcAi.Core.Channels` y `NpcAi.Harness`

#### Scenario: La cascara no imprime
- Dado el codigo bajo `Runtime/Harness/`
- Cuando `sdd-verify` busca `Debug.Log` y `UnityEngine.Debug`
- Entonces no hay ninguna coincidencia

### Requirement: `DeclararTriaje` es un pass-through publico

La cascara DEBE exponer `DeclararTriaje(string categoria)` como metodo publico y DEBE reenviar
`categoria` al `DeclararTriaje` del director sin alterarla: sin recortar, sin normalizar, sin
cambiar mayusculas (normalizar es responsabilidad de M9, no de M11). `DeclararTriaje` NO DEBE
publicar en `NpcReplyChannel` ni abrir ni cerrar la sesion de M13.

#### Scenario: La categoria llega cruda al director
- Dado una cascara con un director inyectado cuyo delegado de triaje registra su argumento
- Cuando se llama `DeclararTriaje("  Rojo ")`
- Entonces el delegado recibe exactamente `"  Rojo "`

#### Scenario: Declarar triaje no publica ni toca la sesion de M13
- Dado una cascara habilitada con un director inyectado, un oyente contador en `NpcReplyChannel` y
  oyentes en ambas costuras de M13
- Cuando se llama `DeclararTriaje("rojo")`
- Entonces el oyente recibe cero `NpcReply` y ninguna costura de M13 recibe llamadas

## Trazabilidad (requisito -> prueba)

Clase: `HarnessBehaviourWiringTests` (`Tests/EditMode/Harness/`, namespace `NpcAi.Harness.Tests`).

| Requisito | Prueba EditMode |
|---|---|
| Simetria de suscripcion sobre los dos canales de entrada | `OnEnable_suscribe_los_dos_canales_de_entrada`, `OnDisable_desuscribe_los_dos_canales_sin_fuga`, `Volver_a_habilitar_no_duplica_la_suscripcion`, `Canal_de_entrada_sin_asignar_no_lanza` (propuesto) |
| Un solo `Raise` por turno hablado y ninguno por accion fisica sola | `Turno_social_produce_exactamente_un_Raise_con_la_respuesta_del_director`, `Turno_clinico_manejado_produce_exactamente_un_Raise`, `Accion_fisica_sola_no_publica_nada_pero_llega_al_director`, `Canal_de_respuesta_sin_asignar_no_lanza` (propuesto) |
| Inicio de sesion como una sola operacion con cuatro efectos | `IniciarSesion_dispara_los_cuatro_efectos_juntos_con_el_mismo_caso`, `IniciarSesion_sin_oyente_en_la_costura_de_M13_dispara_igual_los_tres_efectos_del_director` (propuesto) |
| Punto de entrada publico y arranque automatico opcional | `IniciarSesion_es_publico_sin_parametros_y_sin_retorno`, `Con_el_flag_por_defecto_arrancar_no_inicia_sesion`, `Con_el_flag_activo_arrancar_inicia_exactamente_una_sesion` (propuesto) |
| Etiqueta de la sesion de bitacora | `Etiqueta_lleva_el_prefijo_y_el_caso_elegido`, `Instantes_distintos_dan_etiquetas_distintas`, `Prefijo_vacio_no_produce_etiqueta_vacia` (propuesto) |
| Cierre simetrico de la sesion de bitacora de M13 | `FinalizarSesion_dispara_la_costura_de_cierre_una_vez_sin_tocar_el_director`, `FinalizarSesion_sin_sesion_abierta_es_no_op` (propuesto) |
| La sesion de bitacora no queda abierta por olvido | `OnApplicationQuit_con_sesion_abierta_cierra_la_sesion_de_M13`, `IniciarSesion_con_otra_abierta_cierra_primero_la_anterior` (propuesto) |
| Frontera de inyeccion del director | `Sin_director_los_canales_de_entrada_no_publican`, `Sin_director_IniciarSesion_no_abre_la_sesion_de_M13`, `El_orden_entre_inyeccion_y_habilitacion_no_cambia_el_resultado` (propuesto) |
| Fronteras estructurales de la cascara: ensamblado compilado (lista de permitidos) | `Ensamblado_compilado_no_referencia_otros_modulos_NpcAi` (propuesto) |
| Fronteras estructurales de la cascara: `.asmdef` declara exactamente `NpcAi.Core`, `NpcAi.Core.Channels` y `NpcAi.Harness` | (verificacion estatica en sdd-verify) |
| Fronteras estructurales de la cascara: sin `Debug.Log` ni `UnityEngine.Debug` bajo `Runtime/Harness/` | (verificacion estatica en sdd-verify) |
| `DeclararTriaje` es un pass-through publico | `DeclararTriaje_reenvia_la_categoria_sin_alterarla`, `DeclararTriaje_no_publica_ni_toca_la_sesion_de_M13` (propuesto) |

## Notas no normativas

- Conteo de este delta: 10 requisitos, 28 escenarios: 26 con prueba EditMode propuesta y 2 de
  verificacion estatica en `sdd-verify` (PR1 aporta 7 y 12 en la spec promovida; la spec resultante
  tendra 17 requisitos y 40 escenarios).
- Fuera de este spec, por ser tareas y no comportamiento: `Samples~/Harness/README.md`, la seccion
  M11 de `Docs/MODULES.md` (incluida la excepcion a la regla dura 4, decision 3 de la propuesta), la
  raiz de composicion del anfitrion en `Samples~/Harness/` (no la compila Unity dentro del paquete y
  no es probable en EditMode) y las dos compuertas humanas (escena con los 8 slots de canal sobre
  los 3 assets compartidos; Quest 3). Ninguna prueba EditMode ve slots apuntando a assets
  distintos: cada prueba crea sus propias instancias de canal.
- El orden de escucha (la cascara despues de M13 en `UtteranceChannel`) no es observable en
  EditMode: lo cubren `[DefaultExecutionOrder]` en la cascara y la compuerta humana (revisar en la
  bitacora que cada turno muestre Usuario antes que Npc).
- Valores por defecto de las preguntas abiertas de la propuesta, codificados aqui y vetables: 1 (la
  cascara tambien cierra la sesion de M13) en los dos requisitos de cierre; 2 (metodo publico mas
  flag de arranque) en el punto de entrada; 3 (prefijo + caso + marca de tiempo) en la etiqueta; 4
  (`DeclararTriaje` publico) en el pass-through. Sin control de UI, el componente de triaje del
  progreso de M9 permanece en 0 hasta que un control de escena invoque `DeclararTriaje`.
- Al promover este delta, la seccion `Purpose` de `banco-de-pruebas-m11` (hoy: "Cubre solo el
  alcance de PR1 ... tendra su propio addendum") debe actualizarse para cubrir PR1 y PR2.
- Dejados a `sdd-design` (el spec fija solo lo observable) y ya decididos en `design.md`: mecanismo de inyeccion del director (AD4);
  tipo de las costuras de M13 (AD5: `Action<string>` y `Action`, no `UnityEvent`); silencio o fallo ruidoso sin
  director (AD4); formato y fuente de la marca de tiempo (AD7); instante exacto del arranque automatico (AD3).
