# M11 -- Banco de pruebas (escena de escritorio, sin VR)

Escena que ejercita el pipeline completo (M1 -> M2 -> M4 -> M6/M15 -> M8, mas M9 y M13) contra
implementaciones reales, sin headset. `CompositorDeArnes.cs` (este mismo directorio) construye
los cinco concretos (M2, M4, M6, M9, M15) desde assets del Inspector y los inyecta en
`HarnessBehaviour` (`Runtime/Harness/Unity/`, M11). El archivo `.unity` no se versiona: esta guia
lo hace reconstruible de forma mecanica.

## 1. Los 8 slots sobre los 3 assets compartidos

Este es el primer punto a proposito: es el riesgo mas alto del cableado y ninguna prueba EditMode
lo detecta. Todo puede compilar y pasar las 63 pruebas de `NpcAi.Harness.Tests` mientras la sesion
no hace nada, si un slot apunta al asset equivocado.

Crear (menu `Assets > Create > NPC AI > Canales`) exactamente **3 assets**, uno por canal, y
asignar cada uno de los **8 slots** siguientes al asset compartido correspondiente -- uno por uno,
no por bloque:

| Asset compartido | Slot | Componente (modulo) |
|---|---|---|
| `UtteranceChannel` | `_canalDeUtterance` | `SpeechToTextBehaviour` (M1) |
| `UtteranceChannel` | `_canalDeUtterance` | `SessionLogBehaviour` (M13) |
| `UtteranceChannel` | `_canalDeUtterance` | `HarnessBehaviour` (M11) |
| `PhysicalActionChannel` | `_canal` | `VrInputBehaviour` (M7) |
| `PhysicalActionChannel` | `_canalDeAccion` | `HarnessBehaviour` (M11) |
| `NpcReplyChannel` | `_canal` | `NpcPresenterBehaviour` (M8) |
| `NpcReplyChannel` | `_canalDeRespuesta` | `SessionLogBehaviour` (M13) |
| `NpcReplyChannel` | `_canalDeRespuesta` | `HarnessBehaviour` (M11) |

Los tres primeros slots comparten el mismo asset `UtteranceChannel`; los dos siguientes el mismo
`PhysicalActionChannel`; los tres ultimos el mismo `NpcReplyChannel`. Un slot vacio o apuntando a
un asset distinto del que le corresponde es un no-op silencioso: nada lanza, nada se ve en la
consola, y ese tramo del pipeline queda mudo.

## 2. El microfono de M1

`SpeechToTextBehaviour` **solo captura audio entre `StartListening()` y `StopListening()`**
(`Runtime/Speech/SpeechToTextBehaviour.cs:53-66`); el componente no arranca por existir en la
escena. Sin un control cableado a esos dos metodos, la Compuerta 2 no puede hablar y todo el
pipeline parece muerto sin que nada falle.

## 3. Los controles de la cascara (`HarnessBehaviour`)

Cablear un control de escena (boton, tecla, evento de UI) a cada uno de estos tres metodos
publicos, sin parametros salvo el ultimo:

- `IniciarSesion()` -- una operacion, cuatro efectos: elige y arranca el caso/personalidad en M4,
  M9 y M15, y abre la sesion de bitacora de M13 con la etiqueta
  `{_prefijoDeEtiqueta}-{caso}-{yyyyMMdd-HHmmss}`. El campo serializado `_prefijoDeEtiqueta` vale
  `"banco"` por defecto.
- `FinalizarSesion()` -- cierra la sesion de bitacora que la cascara abrio. **Obligatorio, no
  opcional**: `OnApplicationQuit` **con frecuencia no dispara en Quest/Android** (comportamiento
  de Unity que este diseno no verifico), asi que sin este control cableado
  `SessionRecorder.ReanudarUltimaSesion()` retoma la sesion sin cerrar en el siguiente arranque y
  concatena los turnos de la proxima sesion con los de la anterior.
- `DeclararTriaje(string categoria)` -- pass-through crudo hacia el triaje declarado de M9. Sin un
  control de escena cableado a este metodo, el componente de triaje del progreso de M9 se queda
  siempre en 0.

El campo serializado `_arrancarSolo` (por defecto `false`) permite que la cascara llame
`IniciarSesion()` sola al arrancar la escena, sin esperar al control manual -- util para pruebas
repetidas, no recomendado para la Compuerta 2 con un usuario real.

### Diagnostico opcional en la consola de muestra (calibracion de M2/M4/M6/M15)

`ConsolaDeTranscripcion.cs` (este mismo directorio) construye su propia UI en `Awake()` -- Canvas,
panel oscuro, encabezado y un historial con scroll --, igual que `ConsolaDeVoz.cs`: no hay que armar
nada a mano en el editor ni asignar un `Text`/`TMP_Text` de la escena. Solo hace falta arrastrar el
componente a un GameObject de la escena y cablear sus tres slots (`_canalDeUtterance`,
`_canalDeRespuesta`, `_arnes` apuntando al `HarnessBehaviour`). Si la escena todavia tiene el panel
de texto que se le asignaba a mano en una version anterior de este script, se puede borrar: ya no lo
usa.

**Encabezado fijo** (arriba del panel, nunca se desplaza con el scroll): el **caso clinico**, la
**personalidad** y la **receptividad** actuales del NPC (`CasoActual`, `PersonalidadActual`,
`ReceptividadActual`). Se re-pinta solo cuando alguno cambia, incluso por una accion fisica que no
produce respuesta. Antes de iniciar sesion muestra `(sin sesion)`.

**Historial con scroll**, debajo del encabezado, separado por una linea divisoria: cada turno agrega
CUATRO lineas, en este orden --

1. `Enfermero: <texto>`.
2. **Clasificacion de M2 sobre ESE texto** (pegada a la linea anterior, no a la del paciente): el
   `Intent`, el `Tono` y la `Confianza-M2` que el clasificador le asigno, mas la
   `Confianza-transcripcion(M1)` del mismo turno (`Utterance.Confidence` -- con
   `DebugForzarUtterance` siempre 1.00, porque no hay audio; con M1 real, la confianza de la
   transcripcion). Son dos numeros distintos que el contrato llama "Confianza" a ambos: el de M1
   mide la transcripcion, el de M2 mide que tan segura esta la clasificacion de intencion --
   puede ser bajo con una transcripcion perfecta si la frase es ambigua para el modelo.
3. `Paciente: <texto>`.
4. **Origen y estado tras la respuesta**: si el turno lo respondio M15 (anclado a un hecho del caso
   clinico asignado) o si cayo al generador generico de M6 (respaldo Markov, sin anclaje clinico),
   mas la receptividad y la emocion resultantes.

M15 solo responde si el texto del enfermero cubre TODAS las palabras de alguno de los
`ejemplosDePregunta` del caso activo (`Data/Cases/<caso>.json`, campo `hechos[].ejemplosDePregunta`
-- no `Casos_Medicos.md`, que es solo la narrativa clinica para el estudiante, sin esas frases
disparadoras). Es un emparejamiento por palabras, no semantico: sin ver ahi las frases exactas del
caso que el encabezado muestra como activo, es facil que toda la sesion caiga en M6 por no acertar
la redaccion.

La scrollbar vertical es permanente (siempre visible, no aparece y desaparece). La consola sigue el
final del historial (ultimos mensajes) mientras el usuario no toque el scroll; en cuanto se sube a
leer turnos anteriores, un turno nuevo NO lo empuja de vuelta abajo -- para volver al final basta con
arrastrar la barra hasta el fondo. `_maximoDeLineas` (500 por defecto) es un limite de seguridad
contra crecimiento sin fin en una sesion muy larga, no una ventana chica: con el scroll se puede leer
desde el primer turno.

Esto es una ayuda para afinar el corpus de M3 y el clasificador de M2 durante el desarrollo --
para ver, por ejemplo, si una frase que deberia caer en M15 esta cayendo en el generico por error
de clasificacion. No es parte de la experiencia final entregada al usuario: ninguna logica de
enrutado de `SessionDirector` ni de `HarnessBehaviour` lee estos datos, solo los calculan y los
exponen.

### Probar desde el PC sin voz

En vez del microfono (M1), el anfitrion puede publicar `Utterance` en el mismo `UtteranceChannel`
que escucha `HarnessBehaviour`. `DebugForzarUtterance` (en `Assets/` del proyecto anfitrion, fuera
del paquete) muestra un campo de texto en pantalla: escribir la frase del enfermero y pulsar Enter
(o "Enviar") con la vista Game enfocada en Play Mode. Su menu contextual sigue enviando la frase
fija del Inspector.

## 4. Orden de escucha y limites de la garantia (AD2)

`HarnessBehaviour` lleva `[DefaultExecutionOrder(100)]` para que su suscripcion a
`UtteranceChannel` quede **despues** de la de `SessionLogBehaviour`, y asi cada turno se grabe en
la bitacora con la fila `Usuario` antes que la fila `Npc`. Ese atributo **solo** ordena
componentes que se habilitan en el **mismo pase de carga** de la escena:

- Los GameObjects de `SessionLogBehaviour` (M13) y de `HarnessBehaviour` (M11) deben estar
  **activos al cargar la escena** y no alternarse (`SetActive`) despues.
- No sobrescribir el orden con `Project Settings > Script Execution Order`.
- Una escena **aditiva** o un prefab **instanciado en tiempo de ejecucion** cargan en otro pase y
  quedan **fuera de esta garantia**.

La revision independiente de este diseno califico el atributo como fiable en torno al **85%**, y
solo en el caso del mismo pase de carga. La unica red de verificacion es el transcript: tras una
sesion con al menos dos turnos, `SessionLogBehaviour.ExportarTextoPlano()` debe mostrar `Usuario`
antes que `Npc` en cada turno (criterio de la Compuerta 1). Si aparece invertido, este diseno ya
tiene la salida escrita (diferir el `Raise` de la cascara al siguiente `Update`) como un cambio
propio y pequeno, no un parche sobre este archivo.

## 5. Construir la escena

### Referencias si el codigo del anfitrion vive en su propio `.asmdef`

`CompositorDeArnes.cs`, tal cual vive en este directorio (`Samples~/Harness/`), **no necesita
ningun `.asmdef` propio**: Unity no compila `Samples~/` dentro del paquete, y todos los
ensamblados runtime del paquete son `autoReferenced: true`, asi que un script del proyecto
anfitrion sin `.asmdef` ve sus namespaces sin declarar nada. Si en cambio el codigo del anfitrion
(por ejemplo, una version extendida del compositor) vive bajo un `.asmdef` propio del proyecto,
ese ensamblado debe referenciar, por nombre exacto:

`NpcAi.Core`, `NpcAi.Harness`, `NpcAi.Harness.Unity`, `NpcAi.Nlu` (M2), `NpcAi.Receptivity` (M4),
`NpcAi.Dialogue` (M6), `NpcAi.Scenarios.Emergency` (M9), `NpcAi.SessionLog` y
`NpcAi.SessionLog.Unity` (M13), `NpcAi.ClinicalResponse` (M15).

### Pasos

1. Importar este sample desde **Package Manager > NPC AI... > Samples > Banco de pruebas sin VR
   (M11)**. Unity copia `Samples~/Harness/` a `Assets/Samples/...` del proyecto anfitrion.
2. Crear los 3 assets de canal y asignar los 8 slots de la seccion 1, uno por uno.
3. En la escena, agregar y cablear (segun aplique a cada modulo entregado):
   `SpeechToTextBehaviour` (M1), `VrInputBehaviour` + `TouchZoneRelay` (M7),
   `NpcPresenterBehaviour` (M8), `SessionLogBehaviour` (M13), `HarnessBehaviour` (M11) y el sample
   `CompositorDeArnes` (este script, en el mismo GameObject que `HarnessBehaviour` o en uno
   propio).
4. En `CompositorDeArnes`: asignar `_modelo` (el `ModelAsset` del clasificador BERT de M2),
   `_tokenizador` (el `TextAsset` de `tokenizer.json`), `_casos` (los `TextAsset` de
   `Data/Cases/*.json`), `_corpus` (un elemento por personalidad, cada uno con su id y su
   `TextAsset` de `Data/Dialogue/<personalidad>.json`), `_semilla`, `_arnes` (el
   `HarnessBehaviour` de la escena) y `_bitacora` (el `SessionLogBehaviour` de la escena).
5. Cablear un control de escena a `StartListening()`/`StopListening()` de `SpeechToTextBehaviour`
   (seccion 2) y a `IniciarSesion()`/`FinalizarSesion()`/`DeclararTriaje(string)` de
   `HarnessBehaviour` (seccion 3).
6. Confirmar los caveats de orden de la seccion 4 antes de dar la escena por lista.

## Fuera de alcance de esta entrega

M10 y M16 (sala de juntas), la brecha M9<->M15 de indices de bandera roja, las tres acciones de M7
sin productor (`EntregarObjeto`, `SenalarPantalla`, `GestoCalma`), y cualquier UI/HUD de metricas.
Ver `Docs/MODULES.md`, seccion M11, para el estado completo y lo diferido.
