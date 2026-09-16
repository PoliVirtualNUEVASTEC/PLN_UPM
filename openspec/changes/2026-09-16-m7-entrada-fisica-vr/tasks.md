# Tasks: M7 — Entrada física VR (fuente real de `IPhysicalActionSource`)

## Review Workload Forecast

| Field | Value |
|---|---|
| Estimated changed lines | ~1200-1400 total: núcleo+pruebas ~570, config+pruebas ~190, envoltura+fakes+pruebas ~370, datos+docs ~110-150 |
| 400-line budget risk | Alto |
| Chained PRs recommended | Sí |
| Suggested split | PR1 (núcleo) → PR2 (config) → PR3 (envoltura) → PR4 (datos+docs+cierre) |
| Delivery strategy | ask-on-risk |
| Chain strategy | pending — el usuario debe elegir |

Decision needed before apply: Yes
Chained PRs recommended: Yes
Chain strategy: feature-branch-chain (decidido con el usuario 2026-09-16 -- PR1 apunta a una
rama tracker, los PRs hijos apuntan al PR anterior, solo la rama tracker se mergea a main al
final; elegido explicitamente para evitar el mismo problema que se encontro y corrigio hoy en
M2 con stacked-to-main: un PR hijo mergeado hacia la rama de un padre que ya habia mergeado a
main, sin que nadie abriera el PR de cierre)
400-line budget risk: High

**Nota sobre el corte sugerido por el pedido inicial** ("núcleo+config" como PR1, "envoltura+docs+cierre"
como PR2): con las estimaciones de arriba, PR1 quedaría en ~760 líneas y PR2 en ~550 — ambos superan
igual el presupuesto de 400. Se sugiere en su lugar el corte de 4 unidades de abajo, cada una bajo o
cerca del presupuesto por sí sola. Esta es una decisión de equipo, no una que este documento resuelva
solo.

### Suggested Work Units

| Unit | Goal | PR | Focused test command | Runtime harness | Rollback boundary |
|---|---|---|---|---|---|
| 1 | Núcleo C# puro: `SpatialSample`, `ISpatialSampler`, `ScriptedSpatialSampler`, `SpatialPhysicalActionSource` | PR1 | EditMode: `SpatialPhysicalActionSourceTests` + `DeteccionDeMiradaTests` + `HisteresisDeDistanciaTests` + `DeteccionDeContactoTests` | N/A — C# puro, sin escena ni headset | Borrar `Runtime/VrInput/{SpatialSample,ISpatialSampler,SpatialPhysicalActionSource}.cs`, `Fakes/ScriptedSpatialSampler.cs`, `Properties/AssemblyInfo.cs` + sus pruebas; M7 vuelve a "solo doble" |
| 2 | Config como dato: `VrInputSettings` + `VrInputSettingsAsset` | PR2 | EditMode: `VrInputSettingsAssetTests` | N/A — `ScriptableObject.CreateInstance`, sin escena | Borrar `Runtime/VrInput/Config/` + su prueba; el núcleo cae a `new VrInputSettings()` por defecto |
| 3 | Envoltura MonoBehaviour: `VrInputBehaviour`, `TouchZoneRelay`, `UnitySpatialSampler` | PR3 | EditMode: `VrInputBehaviourWiringTests` | Automatizable vía `CablearParaPrueba`/`BombearParaPrueba` (sin escena real); confirmación en vivo diferida a la compuerta humana 3.6 (Quest 3) | Borrar `VrInputBehaviour.cs`, `TouchZoneRelay.cs`, `UnitySpatialSampler.cs` + su prueba; M7 vuelve a exponer solo el núcleo |
| 4 | Datos por escenario, docs y cierre | PR4 | Manual: revisión de que `Docs/MODULES.md` y `Data/VrInput/README.md` quedan consistentes con las 4 acciones entregadas | Compuerta humana Quest 3 físico (3.6), referenciada aquí para fijar los `.asset` finales | Revertir el commit de datos/docs; no toca código de `Runtime/` |

## Phase 0: Guardrails (leer antes de escribir)

- [ ] 0.1 Confirmar frontera del diff: solo `Runtime/VrInput/`, `Data/VrInput/`, `Tests/EditMode/VrInput/`,
  `Docs/` y este directorio de `openspec/`. Nada en `Runtime/Core/` ni `Runtime/CoreChannels/`.
- [ ] 0.2 Confirmar que esto NO es cambio de contrato: `IPhysicalActionSource`, `PhysicalAction` y
  `PhysicalActionChannel` no cambian firma. Si algo exige tocarlos, parar y avisar — eso es un cambio
  SDD de M0 aparte, co-revisado por el otro dueño de M0 o el asesor.
- [ ] 0.3 Confirmar que `Tests/EditMode/Core/PhysicalActionSourceContract.cs` no se modifica:
  `SpatialPhysicalActionSourceTests` solo hereda de ella.
- [ ] 0.4 Confirmar que `Runtime/VrInput/Fakes/ScriptedPhysicalActionSource.cs` y
  `Runtime/VrInput/NpcAi.VrInput.asmdef` quedan intactos (ya referencian correctamente `NpcAi.Core` y
  `NpcAi.Core.Channels`).

## Phase 1: Núcleo C# puro, TDD (PR1)

- [x] 1.1 RED: `Tests/EditMode/VrInput/SpatialPhysicalActionSourceTests.cs : PhysicalActionSourceContract`
  — `CreateSubject()` = constructor sin parámetros; `EmitTestAction` vía `EmitirParaPrueba` (cast
  interno). No compila hasta 1.5/1.7.
  <!-- apply PR1 (2026-09-16): creado, hereda PhysicalActionSourceContract sin modificarla, espejo
  literal de OfflineSpeechToTextTests. Escrito y creido correcto por inspeccion; confirmacion real
  en el Test Runner de Unity queda pendiente como compuerta humana (tarea 3.5/notas del archivo). -->
- [x] 1.2 RED: `DeteccionDeMiradaTests.cs` — mirada sostenida emite `ContactoVisual` una vez, vistazo
  fugaz no emite, mirada continua no repite hasta liberar el cono ancho.
  <!-- apply PR1 (2026-09-16): creado con 4 pruebas — las 3 pedidas por la traza de spec.md
  (Mirada_sostenida_en_el_cono_levanta_ContactoVisual_una_vez,
  Vistazo_fugaz_no_levanta_ContactoVisual,
  Mirada_continua_no_repite_ContactoVisual_hasta_liberar_el_cono, esta ultima cubre tambien la
  liberacion por cono ancho y la re-emision tras volver a entrar) mas una adicional
  (Perder_el_objetivo_reinicia_la_permanencia, de AD del detector 1). Corre ProcesarMuestra
  directo con angulos calculados, sin muestreador, como indica la tabla "Testing Strategy" de
  design.md. Frente de cabeza fijo en +Z y objetivo siempre a radio 5 evita que el Detector 2
  (distancia) contamine estas pruebas (distancia constante = nunca cruza umbral). Escrito y
  creido correcto por inspeccion; confirmacion real en Test Runner pendiente (compuerta humana). -->
- [x] 1.3 RED: `HisteresisDeDistanciaTests.cs` — primera muestra siembra sin emitir (AD8), cruzar el
  umbral de entrada levanta `Acercarse`, oscilar dentro de la banda no produce ráfaga.
  <!-- apply PR1 (2026-09-16): creado con 5 pruebas — Primera_muestra_siembra_sin_emitir (AD8),
  Cruzar_el_umbral_de_entrada_levanta_Acercarse_una_vez, Cruzar_el_umbral_de_salida_levanta_Alejarse
  y el caso critico Oscilar_sobre_el_umbral_de_entrada_no_produce_una_rafaga_de_eventos (nombres
  siguiendo la Trazabilidad de spec.md), mas Perder_el_objetivo_no_cuenta_como_Alejarse. El frente
  de cabeza de cada muestra mira perpendicular al objetivo (angulo de mirada fijo en 90 grados)
  para que el Detector 1 (mirada) no contamine estas pruebas de distancia. Escrito y creido
  correcto por inspeccion; confirmacion real en Test Runner pendiente (compuerta humana). -->
- [x] 1.4 RED: `DeteccionDeContactoTests.cs` — primer pulso levanta `TocarPaciente`, pulso repetido
  dentro del enfriamiento no repite.
  <!-- apply PR1 (2026-09-16): creado con 4 pruebas — Primer_pulso_levanta_TocarPaciente,
  Contacto_continuo_dentro_del_enfriamiento_no_repite_el_evento (el caso "solape continuo no
  inunda"), Pulso_pasado_el_enfriamiento_vuelve_a_levantar_TocarPaciente y
  Sin_pulso_no_emite_TocarPaciente. `hayObjetivo=false` en todas las muestras aisla el Detector 3
  del 1 y el 2. Escrito y creido correcto por inspeccion; confirmacion real en Test Runner
  pendiente (compuerta humana). -->
- [x] 1.5 GREEN: `Runtime/VrInput/SpatialSample.cs` (`Vec3` + `SpatialSample`, AD1) y
  `Runtime/VrInput/ISpatialSampler.cs` (AD2/AD3) — sin `UnityEngine`.
  <!-- apply PR1 (2026-09-16): creados exactamente como en la seccion "Interfaces / Contracts" de
  design.md. Vec3 usa System.Math (Sqrt/Acos), nunca Mathf; AnguloEnGrados devuelve -1 para
  direccion degenerada (longitud cero) como pide el pseudocodigo del Detector 1. -->
- [x] 1.6 GREEN: `Runtime/VrInput/Fakes/ScriptedSpatialSampler.cs` — `Encolar(SpatialSample)`/
  `LeerMuestra` desencola en orden (lo necesitan 1.1-1.4).
  <!-- apply PR1 (2026-09-16): creado como cola FIFO (System.Collections.Generic.Queue). Marcado
  `internal` (no `public` como ScriptedPhysicalActionSource): ISpatialSampler es una costura
  interna del modulo, no un puerto de NpcAi.Core, y no la usan directamente las 4 clases de
  prueba de esta fase (todas corren ProcesarMuestra a mano), asi que queda lista para cuando
  Fase 3 la necesite. -->
- [x] 1.7 GREEN: `Runtime/VrInput/SpatialPhysicalActionSource.cs` — los 3 detectores (pseudocódigo de
  `design.md`) + `Levantar` único (AD4), hasta verde en 1.1-1.4.
  <!-- apply PR1 (2026-09-16): creado con los 3 detectores como pseudocodigo literal de design.md,
  Levantar unico que solo rechaza Ninguna (AD4), EmitirParaPrueba delegando en Levantar sin
  estrangular (AD5), DistanciaActual/MirandoAlObjetivo como observabilidad interna de prueba, y
  Bombear() para cuando Fase 3 cablee un ISpatialSampler real. DESVIACION registrada: el
  constructor interno consume un `VrInputSettings` (snapshot POCO con los defaults de la tabla
  "Configuracion" de design.md) definido EN ESTE MISMO ARCHIVO, no en
  `Runtime/VrInput/Config/VrInputSettings.cs` — ese archivo/carpeta es entrega explicita de la
  tarea 2.2 (Fase 2, fuera de alcance de este PR) y la frontera de este PR no incluye crear
  `Runtime/VrInput/Config/`. La tarea 2.4 ya anticipa este traslado ("confirmar que el constructor
  interno... consume VrInputSettings, y que el constructor publico sin parametros usa `new
  VrInputSettings()`"): Fase 2 debe mover esta clase intacta a `Config/VrInputSettings.cs` y
  borrarla de este archivo. Ver tambien Deviations en apply-progress.md. -->
- [x] 1.8 GREEN: `Runtime/VrInput/Properties/AssemblyInfo.cs` con
  `InternalsVisibleTo("NpcAi.VrInput.Tests")`.
  <!-- apply PR1 (2026-09-16): creado, espejo literal de Runtime/Speech/Properties/AssemblyInfo.cs.
  Necesario para que las 4 clases de prueba de esta fase (en el ensamblado NpcAi.VrInput.Tests)
  accedan a los miembros internal del nucleo (EmitirParaPrueba, ProcesarMuestra, VrInputSettings,
  DistanciaActual, etc.); sin este archivo el PR no compila. -->
- [x] 1.9 Confirmar que 1.5-1.7 no referencian `UnityEngine`/`Mathf`/`Time` (AD1/AD6). Fase 100%
  automatizable: sin escena, sin headset, sin sesión XR.
  <!-- apply PR1 (2026-09-16): confirmado con busqueda de texto (`UnityEngine|Mathf|Debug\.Log`)
  sobre Runtime/VrInput/: las 2 unicas coincidencias son comentarios de documentacion que
  EXPLICAN por que esos tipos estan ausentes (p.ej. "nunca Mathf"), no un `using` ni una llamada
  real. Cero referencias en codigo ejecutable. -->

## Phase 2: Configuración como dato (PR2)

- [x] 2.1 RED: `Tests/EditMode/VrInput/VrInputSettingsAssetTests.cs` (precedente
  `SpeechSettingsAssetTests`) — clamp de cada campo de la tabla de `design.md`, incluidos los 2 `Max`
  cruzados (liberación ≥ cono, alejarse ≥ acercarse + 0.1).
  <!-- apply PR2 (2026-09-16): creado, espejo literal de SpeechSettingsAssetTests (SetUp/TearDown
  con ScriptableObject.CreateInstance/DestroyImmediate). 9 pruebas: una por campo simple
  (GradosDelConoDeMirada, SegundosDePermanenciaDeMirada, MetrosParaAcercarse,
  SegundosDeEnfriamientoDeContacto), una para GradosDeLiberacionDeMirada y MetrosParaAlejarse
  cada uno con su propio Clamp simple, mas una prueba dedicada por cada uno de los 2 Max
  cruzados (GradosDeLiberacionDeMirada nunca queda por debajo del cono; MetrosParaAlejarse nunca
  queda por debajo de acercarse + 0.1) y ToSettings_copia_los_campos_del_asset_al_snapshot. No
  compila hasta 2.3 (VrInputSettingsAsset no existe todavia). Escrito y creido correcto por
  inspeccion; confirmacion real en Test Runner pendiente (compuerta humana, tarea 3.5). -->
- [x] 2.2 GREEN: `Runtime/VrInput/Config/VrInputSettings.cs` — snapshot POCO sin `UnityEngine`.
  <!-- apply PR2 (2026-09-16): movida tal cual desde SpatialPhysicalActionSource.cs (mismos
  nombres de campo, tipos y defaults de la tabla "Configuracion" de design.md -- ningun valor
  cambio, copiar-pegar como preveia el DESVIACION 1 de apply-progress.md de PR1). Se elimino la
  definicion duplicada de SpatialPhysicalActionSource.cs (deja solo el enum EstadoDeDistancia,
  que design.md ubica en ese mismo archivo). Namespace NpcAi.VrInput (no NpcAi.VrInput.Config):
  ver Deviations abajo. -->
- [x] 2.3 GREEN: `Runtime/VrInput/Config/VrInputSettingsAsset.cs` — `ScriptableObject`,
  `[CreateAssetMenu]`, `OnValidate`/`Mathf.Clamp`, `ToSettings()`, hasta verde en 2.1.
  <!-- apply PR2 (2026-09-16): creado, mismo molde que SpeechSettingsAsset (campos publicos
  planos en el ScriptableObject, OnValidate internal con Mathf.Clamp, ToSettings internal
  devolviendo el snapshot POCO con propiedades {get;set;}). OnValidate acota
  GradosDelConoDeMirada y MetrosParaAcercarse ANTES que sus pares cruzados
  (GradosDeLiberacionDeMirada, MetrosParaAlejarse) porque los 2 Max dependen del valor ya
  acotado, tal como exige la tabla "Configuracion" de design.md. -->
- [x] 2.4 Confirmar que el constructor interno de `SpatialPhysicalActionSource` (1.7) consume
  `VrInputSettings`, y que el constructor público sin parámetros usa `new VrInputSettings()`.
  <!-- apply PR2 (2026-09-16): confirmado por inspeccion -- verificacion, no codigo nuevo. Ambos
  constructores de Runtime/VrInput/SpatialPhysicalActionSource.cs (internal con `VrInputSettings
  config`, publico sin parametros con `: this(new VrInputSettings(), null)`) quedan sin tocar:
  compilan igual que antes porque VrInputSettings sigue en el namespace NpcAi.VrInput, solo en
  un archivo distinto (Runtime/VrInput/Config/VrInputSettings.cs). Ningun `using` nuevo hizo
  falta en SpatialPhysicalActionSource.cs. -->

## Phase 3: Envoltura MonoBehaviour (PR3)

- [x] 3.1 GREEN: `Runtime/VrInput/TouchZoneRelay.cs` — `OnTriggerEnter` + `LayerMask`, latch
  `_pendiente`, `ConsumirPulso()` (AD7).
  <!-- apply PR3 (2026-09-16): creado, `OnTriggerEnter(Collider otro)` filtra por
  `[SerializeField] LayerMask _capasDeContacto` con `(1 << otro.gameObject.layer) &
  _capasDeContacto.value`, levanta el latch `_pendiente`. `ConsumirPulso()` internal: lee y
  limpia en una sola operacion (pull, AD7), a lo sumo un pulso por lectura aunque entren
  varios colliders en el mismo frame. `public sealed class : MonoBehaviour` para poder
  agregarse desde el proyecto anfitrion (M11), igual que `VrInputBehaviour`/
  `NpcPresenterBehaviour`. Sin prueba EditMode dedicada: `OnTriggerEnter` necesita fisica de
  escena real que design.md no exige automatizar (Testing Strategy no la lista); se ejercita
  end-to-end en la compuerta humana 3.6. -->
- [x] 3.2 GREEN: `Runtime/VrInput/UnitySpatialSampler.cs` — lee `Camera`/`Transform`/`Time.deltaTime`/
  `ConsumirPulso()`; `LeerMuestra` devuelve `false` sin cámara u objetivo (AD10).
  <!-- apply PR3 (2026-09-16): creado, `internal sealed class` (espejo de
  `MicrophoneAudioCapture` de M1, tambien `internal`). Constructor recibe `Camera`,
  `Transform`, `TouchZoneRelay` (los 3 nullable). `LeerMuestra` devuelve `false` con
  `muestra = default` si `_camaraDelHmd == null || _objetivo == null` (AD10, sin
  `Debug.Log`, sin lanzar); si no, arma el `SpatialSample` con `Vec3` propios (conversion
  `AVec3(Vector3)` privada, nunca `UnityEngine.Vector3` cruza al nucleo),
  `pulsoDeContacto = _zonaDeContacto?.ConsumirPulso() ?? false` y `deltaSegundos =
  Time.deltaTime`. Verificado end-to-end (sin escena) por
  `Sin_camara_u_objetivo_asignados_no_lanza` en 3.4, que ejercita esta clase real (no un
  doble) a traves de `VrInputBehaviour.CablearParaPrueba()` sin argumento. -->
- [x] 3.3 GREEN: `Runtime/VrInput/VrInputBehaviour.cs` — `Awake`/`OnEnable`/`Update`/`OnDisable`/
  `PublicarEnCanal` (AD9) + costuras `CablearParaPrueba`/`DescablearParaPrueba`/`BombearParaPrueba`
  (patrón M8).
  <!-- apply PR3 (2026-09-16): creado, molde exacto de `NpcPresenterBehaviour` (M8).
  `Awake` arma `_sujeto = new SpatialPhysicalActionSource(cfg, _muestreador)` con
  `_muestreador = _muestreadorDePrueba ?? new UnitySpatialSampler(_camaraDelHmd, _objetivo,
  _zonaDeContacto)`. `OnEnable` suscribe `_sujeto.OnAction += PublicarEnCanal` (AD9: el
  nucleo interno) y llama `_muestreador.Iniciar()`; `OnDisable` desuscribe y llama
  `_muestreador.Detener()`. `Update` solo bombea (`_sujeto?.Bombear()`, AD6, sin bomba de
  hilo). `PublicarEnCanal` solo llama `_canal?.Raise(a)` — CONFIRMADO por inspeccion: cero
  llamadas a `_canal.Subscribe` en todo el archivo, el bug que el orquestador ya encontro y
  corrigio en proposal.md/spec.md no se reintrodujo. Costuras de prueba
  `CablearParaPrueba(ISpatialSampler muestreador = null)` / `DescablearParaPrueba` /
  `BombearParaPrueba` identicas en forma a las de M8. -->
- [x] 3.4 RED→GREEN: `Tests/EditMode/VrInput/VrInputBehaviourWiringTests.cs` (precedente
  `NpcPresenterBehaviourTests` de M8 y `OfflineSpeechToTextWiringTests` de M1) — `OnEnable` suscribe
  `_sujeto.OnAction` (el núcleo interno, NUNCA `PhysicalActionChannel`, que solo recibe `Raise`) y
  `OnDisable` desuscribe sin fuga; sin `Camera` ni `Transform` objetivo asignados no lanza y no publica.
  <!-- apply PR3 (2026-09-16): creado con 2 pruebas, nombres tomados literal de la
  Trazabilidad de spec.md. `OnEnable_suscribe_y_OnDisable_desuscribe_sin_fugas`: un
  `ScriptedSpatialSampler` (doble de PR1) guiona 2 muestras de `TocarPaciente`
  (`hayObjetivo: false` para aislar del detector de mirada/distancia); tras
  `CablearParaPrueba(muestreador)` + `BombearParaPrueba()` el canal recibe 1 `TocarPaciente`,
  y tras `DescablearParaPrueba()` + `BombearParaPrueba()` (2da muestra con delta=2s, fuera
  del enfriamiento por defecto, para forzar que el nucleo SI levantaria de nuevo si algo
  siguiera escuchando) el conteo del canal se queda en 1 -- prueba real de que
  `OnDisable` desuscribe, no solo que el canal nunca recibio nada.
  `Sin_camara_u_objetivo_asignados_no_lanza`: `CablearParaPrueba()` SIN argumento (arma el
  `UnitySpatialSampler` real de la tarea 3.2 con `_camaraDelHmd`/`_objetivo` nunca asignados
  en el `GameObject` de prueba) + `BombearParaPrueba()` -- `Assert.DoesNotThrow` en ambas
  llamadas y 0 publicaciones en el canal (AD10). Escrito y creido correcto por inspeccion;
  confirmacion real en el Test Runner de Unity queda pendiente como compuerta humana (tarea
  3.5, ver nota abajo). -->
- [x] 3.5 MANUAL (Unity Editor — la ejecuta y la registra el usuario, no el agente): Test Runner >
  EditMode > Run All con las Fases 1-3 completas; registrar el total de pruebas en
  `apply-progress.md`.
  <!-- (2026-09-16): confirmado por el usuario en Unity Editor Test Runner, rama
  feat/m7-03-envoltura, tras corregir NpcAi.VrInput.Tests.asmdef (le faltaba la referencia a
  NpcAi.Core.Channels) y commitear los .meta faltantes de las 3 PRs. Todo en verde. -->
  <!-- apply PR3 (2026-09-16): PENDIENTE. Sin acceso a CLI/headless Unity en este entorno
  (regla del proyecto: solo Unity Editor GUI). El agente no reclama pruebas en verde por su
  cuenta -- esta es una compuerta humana que el usuario debe correr y registrar, igual que en
  PR1 y PR2. -->
- [ ] 3.6 MANUAL / compuerta humana (Quest 3 físico — la ejecuta y la registra el usuario, no el
  agente): confirmar en vivo que encarar al NPC emite `ContactoVisual`, que acercarse/alejarse emiten
  una vez por cruce, y que tocar al paciente emite `TocarPaciente`. Los defaults de
  `Data/VrInput/*.asset` (cono, permanencia, distancias, enfriamiento) son puntos de partida
  razonables, no medidos (Open Questions de `design.md`): esta compuerta puede exigir ajustar esos
  valores, no solo confirmar pase/falla. Registrar resultado y cualquier ajuste en
  `apply-progress.md`, atribuido al usuario.
  <!-- apply PR3 (2026-09-16): PENDIENTE, y probablemente lo siga estando por un tiempo:
  ademas de la compuerta humana en si, esta tarea necesita los `.asset` de datos por
  escenario de la Fase 4 (PR4, fuera de alcance de este PR) y, en la practica, el cableado de
  escena de M11 (Camera del HMD, Transform del NPC, collider + `TouchZoneRelay` sobre el
  paciente) que hoy no existe en ninguna escena del proyecto. No se marca `[x]` hasta que el
  usuario la corra y registre el resultado en Quest 3 fisico. -->

## Phase 4: Datos y documentación (PR4)

- [x] 4.1 `Data/VrInput/README.md` — qué significa cada umbral y cómo se calibra en Quest (referencia
  a la compuerta 3.6).
  <!-- apply PR4 (2026-09-16): creado, mismo molde que Data/Speech/README.md y
  Data/Presentation/README.md. Explica los 6 campos de VrInputSettingsAsset en lenguaje llano
  (qué controla cada uno, cómo se misbehave muy bajo/muy alto), con una tabla que incluye tipo,
  default y rango de OnValidate junto a la explicación. Sección dedicada "Calibración en Quest 3
  (compuerta humana 3.6)" deja explícito que los defaults son puntos de partida razonables, no
  medidos (Open Questions de design.md), y referencia la tarea 3.6 de este mismo tasks.md como el
  lugar donde se ajustan y registran. -->
- [x] 4.2 `Data/VrInput/<escenario>.asset` con los defaults de la tabla de `design.md`, ajustables tras
  3.6.
  <!-- apply PR4 (2026-09-16): creado Data/VrInput/Emergency.asset (+ .meta) y Data/VrInput.meta
  (folder). Nombre "Emergency" (no "Emergencia"): Data/Speech/README.md y Data/Presentation/README.md
  ya establecen este patron para el escenario de triaje/emergencia en assets de tipo *SettingsAsset
  por escenario (Boardroom.asset para juntas, Emergency.asset citado explicitamente como precedente
  en ambos READMEs) -- distinto del patron lowercase-espanol que usan los archivos de contenido de
  datos (Data/Corpus/emergencia.json, Data/Personalities/*.asset), que no son settings assets por
  escenario. m_Script referencia el guid real de VrInputSettingsAsset.cs.meta
  (05ecf875ba7cef842a22c3396a5824ee), formato YAML copiado de Data/Speech/Boardroom.asset. Los 6
  campos quedan exactamente en los defaults de la tabla "Configuracion" de design.md (mismos
  numeros ya hardcodeados como defaults de C# en VrInputSettingsAsset.cs) -- este .asset los hace
  explicitos como dato (regla dura 7), no cambia ningun valor. Guids nuevos verificados contra los
  ~124 guids existentes del repo antes de escribir (sin colision). -->
- [x] 4.3 `openspec/specs/entrada-fisica-vr-m7/spec.md` — capacidad nueva, sin spec previa que
  reconciliar: copia directa del spec de este cambio al cerrarlo.
  <!-- apply PR4 (2026-09-16): copia byte-identica confirmada con `diff` (sin salida) desde
  openspec/changes/2026-09-16-m7-entrada-fisica-vr/specs/entrada-fisica-vr-m7/spec.md. Creados
  tambien openspec/specs/entrada-fisica-vr-m7.meta (folder) y spec.md.meta, mismo molde que
  openspec/specs/generador-dialogo-m6/ (unico precedente ya promovido con .meta en este repo). El
  borrador original dentro de la carpeta del cambio queda intacto como registro historico, sin
  modificar. -->
- [x] 4.4 Sección M7 de `Docs/MODULES.md` — cerrar "solo doble" para las 4 acciones en alcance
  (`ContactoVisual`, `Acercarse`, `Alejarse`, `TocarPaciente`); nombrar explícitamente
  `EntregarObjeto`, `SenalarPantalla` y `GestoCalma` como pendientes, con su cambio SDD de seguimiento
  (interacción basada en mandos).
  <!-- apply PR4 (2026-09-16): reescrita "Estado actual" de la seccion M7: nombra las 4 acciones con
  implementacion real entregada, los 3 PR de la cadena (#34 nucleo, #35 config, #36 envoltura) como
  mergeables una vez cierre la rama tracker (PR #33, verificado con `gh pr list`), y nombra
  EntregarObjeto/SenalarPantalla/GestoCalma como fuera de alcance con su propio cambio SDD futuro
  (mandos, probablemente XR Interaction Toolkit; seguimiento de manos descartado explicitamente por
  el usuario, per proposal.md). Documenta la compuerta humana 3.6 pendiente y sus dos bloqueos
  reales (los .asset de esta misma Fase 4, ya resueltos por 4.2, y el cableado de escena de M11, que
  todavia no existe). Agregada linea "Specs formales" apuntando al spec recien promovido (4.3). -->
- [x] 4.5 Confirmar la frontera de diff completa (PR1-PR4): nada fuera de `Runtime/VrInput/`,
  `Data/VrInput/`, `Tests/EditMode/VrInput/`, `Docs/` y `openspec/`.
  <!-- apply PR4 (2026-09-16): confirmado con `git diff main...HEAD --stat` (cadena PR1-PR3 ya
  commiteada, 40 archivos) + revision de las adiciones sin commitear de esta Fase 4: el 100% de los
  archivos caen dentro de Runtime/VrInput/, Tests/EditMode/VrInput/, Data/VrInput/, Docs/MODULES.md
  y openspec/ (la carpeta de este cambio + el spec recien promovido en openspec/specs/). El arbol de
  trabajo tiene ademas numerosos archivos sin trackear ajenos a este cambio (Training/Nlu/ de M2 PR2,
  .meta faltantes de cambios ya archivados de M4/M5/M6/M8/M1/M0, Registro_Modelo_Etiquetado/) que NO
  se tocan ni se agregan al commit de este PR4 -- confirmado explicitamente en el reporte de retorno
  al orquestador. -->

## Phase 5: Cierre (acciones del autor, cada PR)

- [ ] 5.1 `git add` solo de las carpetas del PR correspondiente; `git diff --cached` antes de
  cualquier commit.
- [ ] 5.2 Checklist "Antes de mergear" del `README.md` por PR: pruebas propias en verde, diff acotado,
  spec/design/tasks archivados, `ScriptedPhysicalActionSource` sigue pasando el contrato sin cambios,
  rama al día con `main`, decisiones registradas en Engram y en el documento de contexto (regla 10).
- [ ] 5.3 Al cerrar el último PR: mover este directorio a `openspec/changes/archive/` y escribir
  `archive-report.md`.
- [ ] 5.4 PR final mergeado a `main` por el autor (regla 8, self-merge).

## Notas

- Ningún agente ejecuta Unity ni el headset de forma autónoma: cada verde de Test Runner (3.5) y cada
  confirmación en Quest 3 (3.6) es una compuerta humana, igual que en M1, M2 y M8.
- Las Fases 1 y 2 son 100% automatizables en EditMode, sin escena ni headset — buena unidad para
  empezar en paralelo mientras se decide la estrategia de cadena de PRs.
- `EntregarObjeto`, `SenalarPantalla` y `GestoCalma` (mandos, XR Interaction Toolkit) quedan
  explícitamente fuera: su propio cambio SDD posterior, según `proposal.md`.
