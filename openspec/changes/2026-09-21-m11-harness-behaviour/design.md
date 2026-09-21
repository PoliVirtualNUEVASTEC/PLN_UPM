# Design: M11 PR2 — cáscara de escena (`HarnessBehaviour`), raíz de composición y documentación

## Technical Approach

PR1 dejó `SessionDirector` como C# puro, agnóstico del transporte y **sin un solo llamador en
runtime**. PR2 agrega la pieza que falta y nada más: un `MonoBehaviour` que traduce **canal →
argumento de método** y **valor de retorno → canal**, más la raíz de composición del anfitrión que
arma los concretos. La opción A de la propuesta se mantiene tal cual.

| Responsabilidad | Dónde vive | Cómo se cumple |
|---|---|---|
| Sacar `Utterance`/`PhysicalAction` de un canal | `HarnessBehaviour` | `Subscribe`/`Unsubscribe` simétricos en `OnEnable`/`OnDisable` |
| Publicar el `NpcReply` | `HarnessBehaviour` | Un solo `Raise`, solo si el director devolvió valor |
| Los 4 efectos del inicio de sesión | `HarnessBehaviour` | Un método: 3 del director + la costura de M13 |
| Construir M2, M4, M6, M9, M15 | `Samples~/Harness/CompositorDeArnes.cs` | Assets del Inspector; inyecta y se aparta |
| Elegir, secuenciar, enrutar, delegar | `SessionDirector` (PR1) | Sin cambio de comportamiento; solo se corrige un comentario obsoleto (`SessionDirector.cs:25`) |

### Unidad de módulo y grafo de referencias

    NpcAi.Harness.Unity -> [NpcAi.Core, NpcAi.Core.Channels, NpcAi.Harness]   (noEngineReferences: false)
    NpcAi.Harness.Tests -> [... de PR1] + [NpcAi.Core.Channels, NpcAi.Harness.Unity]
    Samples~/Harness/   -> sin .asmdef: compila en el Assembly-CSharp del anfitrión (AD10)

Copia literal de la forma de `Runtime/SessionLog/Unity/NpcAi.SessionLog.Unity.asmdef`, que es el
precedente exacto de "envoltura Unity del propio módulo". Regla dura 1 y 3 intactas: un solo
módulo tocado (`Runtime/Harness/`), y ninguna referencia a un par.

### Hechos verificados en el código, de los que depende el resto del diseño

1. **`EventChannel<T>` (`Runtime/CoreChannels/EventChannel.cs:11-29`)**: `_listeners` es un
   `Action<T>` multicast y `Raise` es `_listeners?.Invoke(payload)`. Por lo tanto el despacho es
   **síncrono**, **en orden de suscripción** (`+=` agrega al final), sobre una **lista de
   invocación inmutable** — suscribirse o desuscribirse dentro de un handler no afecta el
   despacho en curso. **No deduplica**: suscribir dos veces invoca dos veces (de ahí que la
   simetría `OnEnable`/`OnDisable` sea un requisito y no una cortesía). Y **una excepción en un
   listener aborta la cadena**: los listeners posteriores de ese `Raise` no corren.
2. **M13 no empareja el turno (`Runtime/SessionLog/SessionRecorder.cs:68-86`)**: no existe ningún
   objeto "turno = utterance + reply". `RegistrarUtterance` y `RegistrarRespuesta` escriben **dos
   filas independientes** con un único contador monótono `_siguienteSecuencia`. El emparejamiento
   es **solo el orden de secuencia**.
3. **M13 construye su `SessionRecorder` en su propio `OnEnable`**
   (`SessionLogBehaviour.cs:51-65`), y `IniciarSesion(etiqueta)` es `_recorder?.IniciarSesion(...)`:
   antes de ese `OnEnable` la costura es un **no-op silencioso**. Y con `SesionActiva == false`,
   `RegistrarUtterance`/`RegistrarRespuesta` también son no-op.

De (1)+(2) sale el peligro de orden (AD2); de (3), el de arranque (AD3). Ninguno de los dos es
observable en EditMode, y los dos son silenciosos: por eso ambos llevan mitigación estructural
**y** un criterio explícito de compuerta humana.

## Architecture Decisions

| # | Decisión | Elección | Alternativas rechazadas | Razón |
|---|---|---|---|---|
| AD1 | Ensamblado de la cáscara | `Runtime/Harness/Unity/NpcAi.Harness.Unity.asmdef`, `references: ["NpcAi.Core", "NpcAi.Core.Channels", "NpcAi.Harness"]`, `rootNamespace` y namespace `NpcAi.Harness.Unity`, `autoReferenced: true`, `noEngineReferences: false` | Referenciar M2/M4/M6/M9/M15/M13 (opción B); devolver la cáscara a `Samples~/` (opción C); fábricas `ScriptableObject` en M0 (opción D) | Decisión 1 del usuario, ya cerrada en la propuesta. `noEngineReferences` **no** puede ser `true` aquí (a diferencia de `NpcAi.Harness`): la clase es un `MonoBehaviour`. Se pierde el error de compilación que en PR1 hacía imposible un `Debug.Log`, así que el criterio "cero `Debug.Log` bajo `Runtime/Harness/`" vuelve a ser algo que alguien debe verificar leyendo el diff. `autoReferenced: true` como todos los runtime del paquete: es lo que permite que `Samples~/` compile sin `.asmdef` propio (AD10) |
| AD2 | **Orden de escucha y re-entrada del canal** | Tres capas: (a) `[DefaultExecutionOrder(100)]` sobre `HarnessBehaviour`, para que su `OnEnable` corra después de los componentes de orden por defecto y su listener quede **último** en la cadena de `UtteranceChannel`; (b) los handlers de canal **nunca lanzan** (AD4); (c) criterio explícito de compuerta humana sobre el transcript exportado | Diferir el `Raise` al siguiente `Update`; mover el listener a la cola re-suscribiéndose en `Start()`; documentar el orden en el README sin mecanismo; tocar `SessionLogBehaviour` | **El peligro, concreto**: la cáscara hace `Raise` de `NpcReplyChannel` **dentro** de su handler de `UtteranceChannel`, y M13 escucha los dos canales. Con la cáscara antes que M13 en `UtteranceChannel`, la fila `Npc` se escribe con secuencia N y la fila `Usuario` con N+1: **todos los turnos quedan invertidos** en la bitácora que es el insumo del Objetivo 4. Nada se pierde (las dos filas se escriben, `Hablante` se conserva): se invierte el intercalado. `[DefaultExecutionOrder]` es la única palanca que Unity ofrece para fijar ese orden relativo sin tocar M13 ni el `ProjectSettings` del anfitrión, y además pone a la cáscara al final de la cadena, donde una excepción suya no puede abortar a los listeners de un asset **compartido**. Diferir el `Raise` sí vuelve la corrección independiente del orden, pero paga un frame de latencia en la respuesta hablada, agrega cola + `BombearParaPrueba`, y degrada la aserción de la spec de "exactamente un `Raise` por turno hablado" a "un `Raise` por bombeo": desproporcionado para un daño acotado al orden de filas. Re-suscribirse en `Start()` (`Unsubscribe` + `Subscribe` mueve el listener al final) funciona, pero es invisible en el Inspector, sorprende a quien lea `OnEnable` y se rompe si un par se habilita más tarde. **Consecuencia declarada**: el efecto no es observable en EditMode; solo se afirma por reflexión que el atributo existe y es positivo. Y el atributo **solo manda cuando los dos componentes se habilitan en el mismo pase de carga**: escenas aditivas, prefabs instanciados y componentes activados a mano quedan fuera, y `Project Settings > Script Execution Order` lo sobrescribe si alguien lo toca. Esas condiciones son de escena, no de código, así que van al README como caveats (ver "Plan de documentación"). Si la compuerta humana muestra la inversión, la salida ya está escrita: diferir el `Raise` |
| AD3 | Momento del arranque de sesión | El auto-arranque opcional corre en **`Start()`**, nunca en `OnEnable` ni en `Awake` | Auto-arrancar en `OnEnable`; auto-arrancar en `Awake`; no ofrecer auto-arranque | Hecho verificado 3: si la cáscara llama la costura de M13 antes del `OnEnable` de `SessionLogBehaviour`, `_recorder` todavía es `null`, `IniciarSesion` es no-op, y **la bitácora queda vacía entera** — un fallo peor que el de AD2 y exactamente igual de silencioso. Unity garantiza que `Start` corre después de **todos** los `Awake`/`OnEnable` del pase de carga, así que esta elección elimina el peligro **sin depender de AD2**: es estructural, no de configuración. El mismo argumento hace que la costura se guarde como *method group* late-bound (AD5): se resuelve `_recorder` en el momento de la llamada, no en el del cableado |
| AD4 | Frontera de inyección | `public void Inyectar(SessionDirector director, Action<string> abrirBitacora, Action cerrarBitacora)`. **Ruidoso al inyectar** (`ArgumentNullException` si `director` es nulo), **silencioso en los handlers** (sin director, el handler de utterance retorna sin publicar). La suscripción a los canales **no** depende de la inyección | Inyección por Inspector (imposible: `SessionDirector` no es `UnityEngine.Object`); constructor (no aplica a `MonoBehaviour`); lanzar desde el handler cuando falta el director; no validar nada | La invariante que exige la propuesta — *"sin director no publica nada en `NpcReplyChannel`"* — se cumple por construcción con una guarda de una línea, y deja que `OnEnable` corra antes de la inyección sin ningún orden que garantizar (la suscripción solo necesita los assets de canal). El reparto ruidoso/silencioso es el AD3 de PR1 llevado a la cáscara y tiene aquí un argumento **más fuerte**: por el hecho verificado 1, una excepción lanzada desde un handler **aborta el resto de la cadena de ese canal compartido**, así que una falla ruidosa en la cáscara rompería en silencio a M13 y a M8. Ruidoso donde es barato y aislado (la inyección, en la raíz de composición, en el Editor); silencioso donde el ruido daña a terceros. `public` y no `internal` por el AD4 de PR1: el llamador real vive en el ensamblado del anfitrión, cuyo nombre no existe al escribir el paquete, así que un `InternalsVisibleTo` para ese destino es inescribible. **Alcance exacto de "los handlers nunca lanzan"**: cubre el código propio de la cáscara — director no inyectado y slots de canal sin asignar salen por `return` o por guarda `!= null`, nunca por excepción. **No** es una promesa sobre el director: `ProcesarTurno`/`ProcesarAccion` no se envuelven en `try/catch`, así que una excepción suya propaga fuera del handler y aborta el resto de ese despacho. Es deliberado. Atraparla exigiría o tragársela en silencio (un turno perdido sin rastro, porque bajo `Runtime/Harness/` no hay bitácora de diagnóstico) o un `Debug.LogException`, que la regla "los puertos no imprimen" prohíbe; propagarla la deja visible en la consola de Unity por el manejador del propio motor, que no es logging nuestro. Además, por el AD3 de PR1 los métodos del director **nunca lanzan** por contrato: una excepción ahí es un defecto que debe verse, no enmascararse |
| AD5 | Costura de M13 | Dos delegados, `Action<string> abrirBitacora` y `Action cerrarBitacora`, entregados en la **misma** llamada `Inyectar` que el director | `UnityEvent<string>` serializado y cableado en el Inspector contra `SessionLogBehaviour.IniciarSesion` | La propuesta exige que los cuatro efectos sean **una sola operación**. Con el `UnityEvent`, el arranque tendría dos dueños y dos modos de fallo independientes (cableado en Inspector vs. cableado en código); con un solo punto de inyección hay un modo de fallo, y es ruidoso. Tres razones más, en orden de peso: (1) el **cierre simétrico** no tiene forma natural de `UnityEvent` — harían falta dos eventos y dos cableados más en el Inspector (10 slots en vez de 8), cada uno olvidable en silencio, justo la mitigación del riesgo de `ReanudarUltimaSesion`; (2) Unity no serializa un `UnityEvent<T>` genérico sin declarar una subclase concreta `[Serializable]`, o sea un tipo público extra en un módulo cuya superficie debe quedarse mínima; (3) un `Action<string>` en EditMode es una lambda que graba, mientras que un `UnityEvent` con listeners persistentes exige `UnityEditor.Events.UnityEventTools`. **Costo declarado**: el cableado de M13 deja de ser visible en el Inspector y vive en el script de composición — el README lo nombra y la compuerta humana verifica que la bitácora no quede vacía |
| AD6 | Cuándo cierra la bitácora | Tres caminos, uno solo obligatorio: (a) `public void FinalizarSesion()` cableado a un control de escena — **el camino normal**; (b) `OnApplicationQuit()` de la cáscara cierra la sesión abierta, con costura `SalirParaPrueba`; (c) `IniciarSesion()` llama `FinalizarSesion()` **primero**, justo después de la guarda del director. **Nunca** desde `OnDisable` | Cerrar en `OnDisable`; cerrar solo con el método explícito; confiar solo en `OnApplicationQuit`; no cerrar (defecto de la pregunta abierta 1, ya resuelto en contra) | Contra `OnDisable`, con precisión: `SessionLogBehaviour.OnDisable` hace `_store?.Dispose(); _recorder = null;`, y Unity **no ordena los `OnDisable` entre componentes**, así que un cierre disparado desde ahí puede llegar cuando el recorder ya es `null` — otro no-op silencioso, y vuelve el riesgo de concatenación de `ReanudarUltimaSesion`. En el apagado de la aplicación la situación es distinta y por eso (b) sí sirve: `OnApplicationQuit` corre **antes** de los `OnDisable`, así que el recorder de M13 sigue vivo y el cierre llega a destino. Segundo motivo contra `OnDisable`: en el Editor dispara en cada recarga de dominio y cerraría sesiones por recompilar. (c) existe porque un reinicio debe ser **"inicio, cierre, inicio"**: sin él, arrancar una segunda sesión dejaría la primera abierta y el próximo `ReanudarUltimaSesion` la retomaría concatenando turnos ajenos; es no-op cuando no hay sesión abierta, así que no cambia el arranque normal. **Límite declarado**: `OnApplicationQuit` **con frecuencia no dispara en Quest/Android** (comportamiento de Unity que este diseño no verificó), y tampoco dispara si el proceso muere por crash o kill del SO. Por eso (a) es el camino normal y no un respaldo: el README lo marca como paso obligatorio de la compuerta humana |
| AD7 | Etiqueta de sesión y reloj | `{prefijo}-{caso}-{yyyyMMdd-HHmmss}` (p. ej. `banco-caso-01-20260921-143512`), con `[SerializeField] internal string _prefijoDeEtiqueta = "banco"`, el caso leído de `_director.CasoActual` **después** del arranque, y `DateTime.Now` con `CultureInfo.InvariantCulture` detrás de un `Func<DateTime>` sustituible solo por la costura de prueba | Precisión de minuto (`HHmm`); pedir la etiqueta completa por Inspector; usar `UtcNow`; llamar `DateTime.Now` en línea; agregar el reloj a la firma pública de `Inyectar` | El caso solo se conoce **después** de `_director.IniciarSesion()`, así que el orden dentro de la operación es fijo: los 3 efectos del director, leer `CasoActual`, construir la etiqueta, disparar el cuarto. **Por qué segundos y no minutos**: en `SqliteSessionStore` la etiqueta es la **clave primaria** de la sesión, y `SessionRecorder.IniciarSesion` sobre una etiqueta ya conocida **reabre esa fila y continúa la numeración** en vez de empezar de cero. Dos sesiones del mismo caso dentro del mismo minuto — repetir un caso corto es lo normal en una compuerta humana — se fundirían en silencio en una sola. Los segundos hacen falsificable esa colisión sin agregar ningún estado. Lo mismo descarta una etiqueta escrita a mano en el Inspector, que se repite siempre. `InvariantCulture` para que la forma de la etiqueta no dependa del *locale* del Quest. El reloj queda fuera de `Inyectar` porque no es asunto de la raíz de composición: es el mismo patrón que `VrInputBehaviour.CablearParaPrueba(ISpatialSampler muestreador = null)`, un parámetro opcional de la costura `internal`, y con él la prueba afirma la **cadena exacta**. La normalización de la etiqueta es de M13 (`EtiquetaSesion.Normalizar`): la cáscara hace pass-through crudo, igual que `DeclararTriaje` en PR1 |
| AD8 | Superficie de control de escena | Exactamente tres métodos públicos sin argumentos o con uno: `IniciarSesion()`, `FinalizarSesion()` y `DeclararTriaje(string)`, más `[SerializeField] internal bool _arrancarSolo` (por defecto **falso**) | Una sobrecarga `IniciarSesion(ClinicalCaseId, PersonalityId)` que espejara la del director; UI propia en este cambio; auto-arranque por defecto activado; no exponer `DeclararTriaje` | Cierra las preguntas abiertas 2 y 4 con sus valores por defecto: métodos públicos que el humano cablea a un control de escena (patrón exacto de `SpeechToTextBehaviour.StartListening`/`StopListening`), sin construir UI. **La sobrecarga explícita se descarta** por tres razones que se acumulan: no está en el In Scope de la propuesta, ningún escenario de la spec la exige, y un `UnityEvent` del Inspector **no puede cablear** un método con argumentos que no sean un tipo serializable — `ClinicalCaseId` y `PersonalityId` son structs de M0 con campo `readonly`, que Unity no serializa, así que la sobrecarga sería superficie pública inalcanzable desde la escena. Cuarta razón, mecánica: con dos sobrecargas, `GetMethod("IniciarSesion")` de la prueba estructural queda ambiguo y lanza. Quien necesite forzar un caso concreto lo hace donde corresponde, en la raíz de composición, llamando al director. `_arrancarSolo` por defecto falso: el comportamiento por defecto de la escena debe ser explícito, y quien quiera una escena que arranque sola marca una casilla. **Límite declarado**: sin control de UI para `DeclararTriaje`, el componente de triaje del progreso de M9 se queda en 0 — se documenta en el README y en `Docs/MODULES.md` |
| AD9 | Qué canales se tocan y cómo | `Subscribe`/`Unsubscribe` **solo** sobre los dos canales de entrada; `NpcReplyChannel` **solo** recibe `Raise` | Suscribirse también al canal de salida para trazar | Es el AD9 de M7 palabra por palabra: suscribirse al propio canal de salida es un lazo. Además, por el hecho verificado 1, la cáscara se invocaría a sí misma dentro de su propio `Raise`. Los 3 canales se guardan con guarda `!= null` en cada uso (patrón de M1/M7/M8/M13): un slot vacío es un no-op silencioso, no una excepción que rompa la cadena compartida. `IntentResultChannel` y `ReceptivityChangeChannel` siguen sin cablear a propósito: el director ya tiene ambos valores en mano dentro del mismo turno |
| AD10 | Raíz de composición del anfitrión | Un solo archivo, `Samples~/Harness/CompositorDeArnes.cs`, namespace `NpcAi.Samples.Harness`, **sin `.asmdef` propio**. Dos listas serializadas producen los cuatro catálogos | Publicar un `.asmdef` con el sample; varios archivos por módulo; listas separadas para casos/personalidades | Verificado: todos los runtime `.asmdef` del paquete son `autoReferenced: true`, así que un script de `Samples~/` sin `.asmdef` compila dentro del `Assembly-CSharp` del anfitrión y ve los namespaces de todos los módulos sin declarar nada. Un `.asmdef` en el sample tendría que enumerar las 10 referencias a mano, se copia a `Assets/` al importar y pasa a ser un ensamblado más que el anfitrión debe mantener; además nunca sería `autoReferenced` hacia el código propio del anfitrión. **Costo declarado**: un proyecto anfitrión cuyos scripts vivan bajo su propio `.asmdef` sí debe agregar las referencias a mano — el README las lista. Sobre las listas: un `TextAsset[] _casos` da a la vez el `Func<ClinicalCaseId,string>` y el catálogo `IReadOnlyList<ClinicalCaseId>`; una lista de corpus por personalidad da a la vez el diccionario de M6 y el catálogo de personalidades. Derivarlos de la misma fuente hace **imposible** que diverjan, y agregar un cuarto caso o una quinta personalidad es una entrada de Inspector y cero líneas de código (`rules.design`). **Dos precisiones de mecánica de Unity, o el Inspector queda vacío**: (1) Unity **no serializa tuplas**, así que el par personalidad/corpus necesita una clase anidada `[Serializable]` con dos campos (`public string Personalidad; public TextAsset Corpus;`), no un `(string, TextAsset)[]`; (2) la búsqueda del caso compara **`new ClinicalCaseId(asset.name) == id`**, no `asset.name == id.Value`: el constructor de `ClinicalCaseId` recorta y pasa a minúsculas su entrada (`ClinicalCaseId.cs:21`), así que comparar la cadena cruda del asset contra el `Value` ya normalizado fallaría con cualquier archivo nombrado `Caso-01`. El compositor también hace `(m2 as IDisposable)?.Dispose()` en `OnDestroy`: `BertIntentClassifier` implementa `IDisposable` y nadie más lo libera |
| AD11 | Visibilidad y costuras de prueba | `Inyectar` y los 3 métodos de sesión **públicos**; los 5 `[SerializeField]` **`internal`**; cuatro costuras `internal` — `CablearParaPrueba`, `ArrancarParaPrueba`, `DescablearParaPrueba`, `SalirParaPrueba` — vía `Runtime/Harness/Unity/Properties/AssemblyInfo.cs` con `[assembly: InternalsVisibleTo("NpcAi.Harness.Tests")]` | Todo público; reflexión desde la prueba; `GameObject.SetActive(true)` en vez de las costuras | Precedente literal de M1/M7/M8: `[SerializeField] internal` mantiene el campo visible en el Inspector y asignable desde el ensamblado amigo sin ampliar la superficie pública, y las costuras existen porque activar un `GameObject` recién creado **no dispara `Awake`/`OnEnable` de forma confiable** dentro de un método de prueba EditMode síncrono (comentario textual de `VrInputBehaviour`). Cada una corresponde a un momento distinto del ciclo de vida que la prueba debe poder disparar por separado: `ArrancarParaPrueba` es `Start()`, y está separada de `CablearParaPrueba` porque modela lo que ocurre de verdad en la escena — `Start` corre **después** de la inyección (AD3), así que es el único punto donde se puede observar `_arrancarSolo`; `SalirParaPrueba` es `OnApplicationQuit()`, el cierre de AD6. `CablearParaPrueba` asigna los campos directamente y **no** pasa por `Inyectar`, porque debe poder dejar el director nulo para probar el no-op de AD4 |
| AD12 | Ensamblado de pruebas | `NpcAi.Harness.Tests.asmdef` += `NpcAi.Core.Channels`, `NpcAi.Harness.Unity`. Nada más | Agregar `NpcAi.Nlu`/`NpcAi.SessionLog.Unity` para probar contra implementaciones reales | Confirmado leyendo el grafo: `NpcAi.Harness.Unity` solo referencia `NpcAi.Core`, `NpcAi.Core.Channels` y `NpcAi.Harness`, y ninguno de los tres referencia Sentis (`com.unity.ai.inference` entra únicamente por `NpcAi.Nlu`, que nadie de esta cadena referencia). El cierre de compilación de EditMode no crece. Las cinco piezas de comportamiento siguen entrando por los espías locales de `EspiasDeArnes.cs` (AD11 de PR1), que ya graban todo lo que hace falta y además exponen `AssignCase`/`RegisterRedFlag` propios para cablearse como delegados |
| AD13 | `.meta`, `Samples~` y frontera de reversión | `.meta` obligatorio **en el mismo commit** para todo archivo nuevo bajo `Runtime/Harness/Unity/` y `Tests/EditMode/Harness/`, **y también para las dos carpetas nuevas** (`Runtime/Harness/Unity` y `Runtime/Harness/Unity/Properties`). **Nada bajo `Samples~/` lleva `.meta`** | Dejar los `.meta` para un commit posterior (lo que pasó en M7 y en PR1); commitear solo los `.meta` de archivo | Unity genera los `.meta` con GUID al abrir el Editor; el agente no puede fabricar GUIDs válidos, así que `tasks.md` debe incluir un punto de control explícito donde el humano abre el Editor una vez y el agente commitea los `.meta` generados. **Las carpetas también llevan `.meta`** y es el olvido más fácil: una carpeta sin `.meta` versionado le cambia el GUID a cada quien clone el repositorio. La carpeta `Samples~` termina en `~`, así que Unity la ignora por completo y **no genera `.meta` dentro** — pedirlos ahí sería un error. `package.json` ya declara la entrada del sample (`"path": "Samples~/Harness"`): no se toca |

## Interfaces / Contracts

Sin cambios en `NpcAi.Core`, y ningún cambio de comportamiento en
`Runtime/Harness/SessionDirector.cs` (solo el comentario obsoleto de la línea 25). La superficie
nueva del módulo es **un tipo público más**.

```csharp
// Runtime/Harness/Unity/HarnessBehaviour.cs
namespace NpcAi.Harness.Unity
{
    /// <summary>
    /// M11 — cascara de escena: traduce canal -> argumento de metodo y valor de retorno ->
    /// canal. No construye nada (regla dura 3): recibe el SessionDirector ya armado y la
    /// costura de M13 desde la raiz de composicion del anfitrion.
    /// </summary>
    [DefaultExecutionOrder(100)]     // AD2: suscribe ultimo; su listener corre ultimo
    public sealed class HarnessBehaviour : MonoBehaviour
    {
        [SerializeField] internal UtteranceChannel      _canalDeUtterance;   // entrada
        [SerializeField] internal PhysicalActionChannel _canalDeAccion;      // entrada
        [SerializeField] internal NpcReplyChannel       _canalDeRespuesta;   // salida: solo Raise
        [SerializeField] internal string               _prefijoDeEtiqueta = "banco";
        [SerializeField] internal bool                 _arrancarSolo;        // AD8: por defecto falso

        /// <exception cref="ArgumentNullException"><paramref name="director"/> es null (AD4).</exception>
        public void Inyectar(SessionDirector director,
                             Action<string> abrirBitacora,      // AD5: puede ser null (sin M13)
                             Action         cerrarBitacora);    // AD5: puede ser null (sin M13)

        // AD8: tres metodos, todos cableables desde un UnityEvent del Inspector.
        public void IniciarSesion();                    // AD6+AD7: cierra la abierta, arranca, etiqueta
        public void FinalizarSesion();                  // AD6: camino normal de cierre
        public void DeclararTriaje(string categoria);   // AD8: pass-through

        public string EtiquetaActiva { get; }   // "" antes del primer IniciarSesion

        // --- Costuras de prueba (AD11), solo visibles para NpcAi.Harness.Tests ---
        internal void CablearParaPrueba(SessionDirector director      = null,
                                        Action<string>  abrirBitacora = null,
                                        Action          cerrarBitacora = null,
                                        Func<DateTime>  reloj          = null);
        internal void ArrancarParaPrueba();     // => Start();            AD3: despues de inyectar
        internal void DescablearParaPrueba();   // => OnDisable();
        internal void SalirParaPrueba();        // => OnApplicationQuit(); AD6
    }
}
```

`CablearParaPrueba` **asigna los campos y fuerza `OnEnable()`**, sin pasar por `Inyectar` (que
lanzaría con director nulo). Esta clase no necesita `Awake`, así que la costura solo cubre el
`OnEnable` — por lo demás es el patrón textual de `VrInputBehaviour.CablearParaPrueba`, y existe
porque `OnEnable` no es invocable desde la prueba: Unity lo llama por nombre, pero el método es
privado. Una prueba que necesite el orden real "habilitado **antes** de inyectar" llama
`CablearParaPrueba()` sin argumentos y llama `Inyectar(...)` después; `ArrancarParaPrueba` queda
aparte para poder disparar `Start` cuando ya hay director (AD3).

### Ciclo de vida y turno (pseudocódigo)

`!= null` y no `?.` sobre todo campo `UnityEngine.Object`: el `==` sobrecargado de Unity reporta
como nulo un objeto ya destruido que la referencia gestionada todavía apunta ("fake null"), y `?.`
se salta esa sobrecarga. Es el patrón textual de M1, M7, M8 y M13.

```
OnEnable:                                   // AD4: no depende de la inyeccion
    if (_canalDeUtterance != null) _canalDeUtterance.Subscribe(AlRecibirUtterance)
    if (_canalDeAccion    != null) _canalDeAccion.Subscribe(AlRecibirAccion)

OnDisable:                                  // simetria estricta: el canal NO deduplica (hecho 1)
    if (_canalDeUtterance != null) _canalDeUtterance.Unsubscribe(AlRecibirUtterance)
    if (_canalDeAccion    != null) _canalDeAccion.Unsubscribe(AlRecibirAccion)
    // AD6: aqui NO se cierra la bitacora -- el _recorder de M13 puede estar ya en null

Start:                                      // AD3: despues de TODOS los OnEnable del pase de carga
    if (_arrancarSolo) IniciarSesion()

OnApplicationQuit: FinalizarSesion()        // AD6: corre ANTES de los OnDisable del apagado

Inyectar(director, abrir, cerrar):          // AD4
    _director = director ?? throw new ArgumentNullException(nameof(director))
    _abrirBitacora = abrir; _cerrarBitacora = cerrar     // AD5: late-bound method groups

AlRecibirUtterance(u):                      // AD4: la cascara no lanza; el director puede
    if (_director == null) return           // invariante: sin director, cero Raise
    var reply = _director.ProcesarTurno(u)
    if (_canalDeRespuesta != null) _canalDeRespuesta.Raise(reply)
                                            // UN solo Raise; AD2: dentro del despacho de utterance

AlRecibirAccion(a):                         // mismo alcance de no-lanzar
    if (_director == null) return
    var reply = _director.ProcesarAccion(a) // siempre null en esta entrega
    if (reply.HasValue && _canalDeRespuesta != null)
        _canalDeRespuesta.Raise(reply.Value)            // cero Raise por accion sola

IniciarSesion():                            // UNA operacion, CUATRO efectos
    if (_director == null) return
    FinalizarSesion()                       // AD6: no-op si no hay sesion; "inicio, cierre, inicio"
    _director.IniciarSesion()               // efectos 1-3: m4.Reset, m9.AssignCase, m15.AssignCase
    EtiquetaActiva = _prefijoDeEtiqueta + "-" + _director.CasoActual.Value
                   + "-" + _reloj().ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture)
                                            // AD7: el caso solo se conoce DESPUES del arranque
    if (_abrirBitacora != null) _abrirBitacora(EtiquetaActiva)   // efecto 4 (M13), sin ver al par

FinalizarSesion():                          // AD6: camino normal de cierre
    if (string.IsNullOrEmpty(EtiquetaActiva)) return
    if (_cerrarBitacora != null) _cerrarBitacora()
    EtiquetaActiva = ""
```

## Data Flow

    ┌─ ESCENA: 8 slots de Inspector sobre 3 assets de canal (primer punto del README) ────────┐
    │                                                                                         │
    │  SpeechToTextBehaviour (M1) ──Raise──┐                                                  │
    │                                      ▼                                                  │
    │                       [asset 1: UtteranceChannel]                                       │
    │                                      │  despacho SINCRONO, en orden de suscripcion      │
    │                     ┌────────────────┴─────────────────┐                                │
    │                     ▼ (1º, orden por defecto)           ▼ (2º, AD2: orden 100)          │
    │            SessionLogBehaviour (M13)            HarnessBehaviour (M11)                  │
    │             RegistrarUtterance                   AlRecibirUtterance                     │
    │               fila seq N, Usuario                       │                               │
    │                                                         ▼  SessionDirector (PR1)        │
    │                                                   ProcesarTurno(u) -> NpcReply          │
    │                                                         │                               │
    │  VrInputBehaviour (M7) ──Raise──► [asset 2: PhysicalActionChannel]                      │
    │                                      └──► HarnessBehaviour: ProcesarAccion -> null      │
    │                                           (cero Raise: un gesto no habla)               │
    │                                                         │                               │
    │                                    [asset 3: NpcReplyChannel] ◄── UN solo Raise ────────┤
    │                     ┌────────────────┴─────────────────┐                                │
    │                     ▼                                   ▼                               │
    │            NpcPresenterBehaviour (M8)          SessionLogBehaviour (M13)                │
    │              Play (bomba, Update)               RegistrarRespuesta                      │
    │                                                   fila seq N+1, Npc                     │
    └─────────────────────────────────────────────────────────────────────────────────────────┘

    ┌─ Samples~/Harness/CompositorDeArnes.cs — Awake, una vez, sin pruebas (AD10) ────────────┐
    │  _casos: TextAsset[] ──┬──► Func<ClinicalCaseId,string> CargarCaso  ──► M9, M15         │
    │    (new ClinicalCaseId │     (compara new ClinicalCaseId(asset.name) == id, AD10)       │
    │     (asset.name))      └──► IReadOnlyList<ClinicalCaseId>           ──► SessionDirector │
    │  _corpus: CorpusDePersonalidad[]  [Serializable] {string Personalidad; TextAsset Corpus;}│
    │    (AD10: Unity no    ─┬──► IReadOnlyDictionary<string,TextAsset>   ──► M6              │
    │     serializa tuplas)  └──► IReadOnlyList<PersonalityId>            ──► SessionDirector │
    │  new BertIntentClassifier(_modelo, _tokenizador)                    (M2, IDisposable)   │
    │  new ReceptivityEngine()                                            (M4, catalogo Standard)
    │  new MarkovDialogueGenerator(corpus)                                (M6)                │
    │  var m9 = new TriageScenarioObjective(new TriageObjectiveSettings(), CargarCaso)        │
    │  new ClinicalResponder(CargarCaso)                                  (M15)               │
    │  new SessionDirector(m2, m4, m6, m9, m15, m9.AssignCase, m9.DeclareTriage,              │
    │                      casos, personalidades, _semilla)               (AD1 de PR1)        │
    │  _arnes.Inyectar(director, _bitacora.IniciarSesion, _bitacora.FinalizarSesion)          │
    └─────────────────────────────────────────────────────────────────────────────────────────┘

**Los 8 slots, uno por uno** (README, primer punto): `UtteranceChannel` →
`SpeechToTextBehaviour._canalDeUtterance` (M1), `SessionLogBehaviour._canalDeUtterance` (M13),
`HarnessBehaviour._canalDeUtterance` (M11). `PhysicalActionChannel` → `VrInputBehaviour._canal`
(M7), `HarnessBehaviour._canalDeAccion` (M11). `NpcReplyChannel` → `NpcPresenterBehaviour._canal`
(M8), `SessionLogBehaviour._canalDeRespuesta` (M13), `HarnessBehaviour._canalDeRespuesta` (M11).

## File Inventory

| Archivo | Acción | Rol |
|---|---|---|
| `Runtime/Harness/Unity.meta` | Crear | AD13: `.meta` **de carpeta**; sin él el GUID cambia en cada clon |
| `Runtime/Harness/Unity/NpcAi.Harness.Unity.asmdef` (+ `.meta`) | Crear | AD1: 3 referencias, `noEngineReferences: false` |
| `Runtime/Harness/Unity/HarnessBehaviour.cs` (+ `.meta`) | Crear | Único tipo nuevo: puente de canales, sesión, costuras |
| `Runtime/Harness/Unity/Properties.meta` | Crear | AD13: `.meta` de carpeta |
| `Runtime/Harness/Unity/Properties/AssemblyInfo.cs` (+ `.meta`) | Crear | AD11: `InternalsVisibleTo("NpcAi.Harness.Tests")` |
| `Tests/EditMode/Harness/NpcAi.Harness.Tests.asmdef` | Modificar | AD12: + `NpcAi.Core.Channels`, `NpcAi.Harness.Unity` |
| `Tests/EditMode/Harness/HarnessBehaviourWiringTests.cs` (+ `.meta`) | Crear | Cableado, sesión, no-op sin director, fronteras estructurales |
| `Samples~/Harness/CompositorDeArnes.cs` | Crear | AD10: construye los 5 concretos e inyecta; **sin `.meta`**, sin pruebas |
| `Samples~/Harness/README.md` | Reescribir | Pasos humanos; ver "Plan de documentación" |
| `Docs/MODULES.md` (sección M11) | Modificar | Estado real, diferidos, nota de la regla dura 4 (decisión 3) |
| `Runtime/Harness/SessionDirector.cs:25` | Modificar (solo comentario) | Dice `HarnessBehaviour, PR2` refiriéndose al compositor; debe decir `CompositorDeArnes`. Sin cambio de comportamiento: la cáscara `HarnessBehaviour` ya no es quien cablea el delegado |
| `Tests/EditMode/Harness/EspiasDeArnes.cs` | Sin cambio | Los 5 espías de PR1 ya graban lo necesario (AD12) |
| `Runtime/Core/`, `Runtime/CoreChannels/`, otros `Runtime/<Modulo>/`, `Data/`, `package.json` | Sin cambio | M11 solo consume; sin cambio de contrato (reglas duras 1 y 2) |

### Plan de documentación

`Samples~/Harness/README.md`, en este orden:

1. **Los 8 slots sobre los 3 assets**, uno por uno, con la lista de arriba. Primer punto porque es
   el riesgo Alto nº 1 y ninguna prueba EditMode lo ve.
2. **El micrófono de M1**: `SpeechToTextBehaviour` **solo captura entre `StartListening` y
   `StopListening`** (`SpeechToTextBehaviour.cs:53-66`); no arranca por existir el componente. Sin
   un control de escena cableado a esos dos métodos, la compuerta 2 no puede "hablar" y el
   pipeline entero parece muerto sin que nada falle.
3. **Los controles de la cáscara**: `IniciarSesion`, `FinalizarSesion` y `DeclararTriaje`, y la
   advertencia de AD6 — `OnApplicationQuit` **con frecuencia no dispara en Quest/Android**
   (comportamiento de Unity que este diseño no verificó), así que cerrar con el control es
   obligatorio, no opcional; sin eso `ReanudarUltimaSesion` concatena la próxima sesión.
4. **Caveats de orden (AD2)**, como condiciones de escena verificables a ojo: los GameObjects de
   M13 y de la cáscara deben estar **activos al cargar la escena** y no alternarse después;
   `Project Settings > Script Execution Order` no debe sobrescribir el
   `[DefaultExecutionOrder(100)]` de la cáscara; una escena **aditiva** o un prefab instanciado
   cargan en otro pase y quedan fuera de la garantía. La revisión independiente de este diseño
   calificó el atributo como fiable en torno al **85%**, y **solo** en el caso del mismo pase de
   carga: fuera de él el orden no está garantizado, y la verificación del transcript de la
   compuerta 1 es la única red.
5. **Referencias para un anfitrión con `.asmdef` propio** (AD10) y los pasos de construcción de la
   escena.

## Testing Strategy

Todo EditMode, sin escena, sin VR, sin audio, sin SQLite. `strict_tdd: true`: cada aserción entra
en RED antes de la línea que la pone en verde. Patrón literal de `VrInputBehaviourWiringTests`:
`ScriptableObject.CreateInstance<T>()` para los 3 canales, `new GameObject` + `AddComponent`,
asignación directa de los `[SerializeField] internal`, `CablearParaPrueba` en vez de `SetActive`,
y `Object.DestroyImmediate` al final. El sujeto es un `SessionDirector` **real** sobre los cinco
espías de `EspiasDeArnes.cs`, con `EspiaObjetivo.AssignCase`/`RegisterRedFlag` como delegados. Los nombres de método autoritativos son los de la tabla de trazabilidad del spec (`specs/banco-de-pruebas-m11/spec.md`, 26 pruebas EditMode); la tabla de abajo describe la estrategia por capa y no es un inventario exhaustivo.

| Capa | Qué se afirma | Cómo |
|---|---|---|
| Cableado | `OnEnable` suscribe **los dos** canales de entrada | `Raise` en cada canal ⇒ el espía correspondiente registra la llamada |
| Cableado | `OnDisable` desuscribe **los dos**, sin fuga | Tras `DescablearParaPrueba`, `Raise` en ambos ⇒ contadores congelados |
| Cableado | Un turno hablado ⇒ **exactamente un** `Raise` en `NpcReplyChannel` | Suscriptor de prueba que cuenta; `Assert.AreEqual(1, ...)` |
| Cableado | Una acción física sola ⇒ **cero** `Raise` | Mismo contador en 0 tras `Raise` en `PhysicalActionChannel` |
| Sesión | `IniciarSesion` dispara los **cuatro** efectos juntos | `EspiaReceptividad.Resets`, `EspiaObjetivo.CasosAsignados`, `EspiaClinico.Asignaciones` y la lambda `abrirBitacora` que graba la etiqueta |
| Sesión | La etiqueta es exacta y determinista (AD7) | Reloj congelado por `CablearParaPrueba(..., reloj)`; igualdad de cadena contra `banco-caso-01-20260921-143512` |
| Sesión | `FinalizarSesion` dispara la costura una sola vez; sin sesión abierta es no-op | Contador de la lambda `cerrarBitacora` |
| Sesión | Reiniciar es **"inicio, cierre, inicio"** (AD6) | Dos `IniciarSesion()` seguidos ⇒ `cerrarBitacora` contó 1 entre ambos, y las dos etiquetas grabadas difieren |
| Sesión | `SalirParaPrueba` cierra la sesión abierta (AD6) | `IniciarSesion()` → `SalirParaPrueba()` ⇒ `cerrarBitacora` contó 1 |
| Frontera | **Sin director, un `Utterance` no publica nada** (AD4) | `CablearParaPrueba()` sin argumentos ⇒ `Raise` de utterance ⇒ 0 `Raise` de reply, `Assert.DoesNotThrow` |
| Frontera | **Sin director, una `PhysicalAction` tampoco publica nada ni lanza** (AD4) | Mismo montaje ⇒ `Raise` en `PhysicalActionChannel` ⇒ 0 `Raise` de reply, `Assert.DoesNotThrow` |
| Frontera | `OnEnable` antes de la inyección funciona | `CablearParaPrueba()` → `Inyectar(...)` → `Raise` ⇒ 1 `Raise` de reply |
| Frontera | `Inyectar(null, ...)` lanza `ArgumentNullException` | Falla ruidosa en la raíz de composición (AD4) |
| Frontera | `DeclararTriaje` es pass-through; sin director es no-op | Delegado `declararTriajeEnObjetivo` del director |
| Estructural | El ensamblado de la cáscara no usa ningún par — **lista blanca** | De `typeof(HarnessBehaviour).Assembly.GetReferencedAssemblies()`, **todo** nombre que empiece por `NpcAi.` debe pertenecer a `{NpcAi.Core, NpcAi.Core.Channels, NpcAi.Harness, NpcAi.Harness.Unity}` |
| Estructural | `[DefaultExecutionOrder]` existe y es positivo (AD2) | `GetCustomAttribute<DefaultExecutionOrder>(typeof(HarnessBehaviour))` (el tipo de Unity es `UnityEngine.DefaultExecutionOrder`, sin sufijo `Attribute`; lo detectó la compilación de prueba de la unidad 1) |

**Lista blanca, no lista negra.** Enumerar los pares prohibidos (`NpcAi.Nlu`, `NpcAi.SessionLog*`,
…) deja pasar lo que todavía no existe: `Runtime/RequirementResponse/` ya está en `main` por
trabajo de otro compañero, y su ensamblado **no aparecería** en ninguna lista negra escrita hoy.
La lista blanca falla ante cualquier par, presente o futuro, que es justo lo que la regla dura 3
exige.

**Lo que NO es prueba EditMode, sino verificación estática en `sdd-verify`.** Dos criterios de
éxito no se pueden afirmar desde una prueba y se declaran aquí para que no se inventen como tales:

- **El `.asmdef` declara exactamente `NpcAi.Core`, `NpcAi.Core.Channels` y `NpcAi.Harness`**:
  `GetReferencedAssemblies()` devuelve lo que el compilador **emitió**, no lo que el `.asmdef`
  declara, así que una referencia declarada y no usada no aparece. Se verifica con `rg` sobre el
  JSON de `Runtime/Harness/Unity/NpcAi.Harness.Unity.asmdef`.
- **Cero llamadas de bitácora bajo `Runtime/Harness/Unity/`** (AD1: `noEngineReferences: false`
  devolvió la posibilidad de compilar `Debug.Log`): se verifica con `rg` sobre `Debug\.` en esa
  carpeta.

**Lo que EditMode no puede cubrir, y por eso es compuerta humana**:

- **Compuerta 1 (escena)**: los 8 slots apuntan a los 3 assets compartidos, uno por uno. Ninguna
  prueba EditMode ve este fallo y es el riesgo Alto nº 1 de la propuesta.
- **Compuerta 1 (orden, AD2)**: tras una sesión con al menos dos turnos, `ExportarTextoPlano()`
  debe mostrar **`Usuario` antes que `Npc` en cada turno**. Si aparece invertido,
  `[DefaultExecutionOrder(100)]` no bastó y la salida escrita es diferir el `Raise`.
- **Compuerta 1 (bitácora, AD3/AD5)**: tras `IniciarSesion`, `ListarSesiones()` muestra la
  etiqueta nueva y la sesión **no** está vacía. Una bitácora vacía significa costura de M13 sin
  cablear o arranque antes del `OnEnable` de M13.
- **Compuerta 1 (cierre, AD6)**: tras pulsar el control de `FinalizarSesion`, la sesión aparece
  **cerrada** en `ListarSesiones()`, y una sesión nueva empieza su numeración en cero en vez de
  continuar la anterior. Es la verificación que no puede delegarse a `OnApplicationQuit`, que en
  Quest/Android con frecuencia no dispara.
- **Compuerta 2 (Quest 3 físico)**: sesión completa jugada — con el control de `StartListening`
  pulsado se habla, el NPC responde, la receptividad cambia, el progreso de M9 se mueve, M13
  registra los turnos, y `FinalizarSesion` cierra al terminar.
- **La raíz de composición entera** (AD10): sin pruebas por diseño; se mantiene mínima y se cubre
  solo con las dos compuertas.

## Threat Matrix

N/A — no hay enrutamiento de shell, subproceso, automatización de VCS/PR, clasificación de
archivos ejecutables ni integración de procesos. Todo el cambio es cableado de eventos en proceso
dentro del bucle de Unity.

## Migration / Rollout

Sin migración. Todo es aditivo y M11 sigue siendo la **hoja** del grafo: ningún ensamblado
referencia `NpcAi.Harness` ni `NpcAi.Harness.Unity`. Revertir PR2 borra `Runtime/Harness/Unity/`,
`Tests/EditMode/Harness/HarnessBehaviourWiringTests.cs` y `Samples~/Harness/CompositorDeArnes.cs`,
y deja `main` en el estado de PR1 (director en verde, sin escena): coherente, no roto. Sin
`Data/` nuevo, sin asset nuevo, sin dependencia nueva en `package.json` (la entrada del sample ya
existe), sin cambio de `Contract.Version`, sin `.asmdef` ajeno modificado. La escena `.unity`
queda fuera del repositorio y del diff: el README la vuelve reconstruible de forma mecánica.

## Open Questions

- [ ] **AD2 es la única decisión con riesgo residual real.** La mitigación no es observable en
      EditMode y el fallo es silencioso. Si la compuerta humana ve el transcript invertido, el
      cambio correctivo ya está diseñado (diferir el `Raise` al siguiente `Update`, con cola y
      `BombearParaPrueba` como en M1/M7/M8) y cabe en un PR pequeño y propio.
- [ ] **`noEngineReferences: false` devuelve la posibilidad de `Debug.Log`** (AD1). En PR1 el
      criterio se hacía cumplir por error de compilación; en PR2 pasa a ser verificación estática
      en `sdd-verify` (`rg` sobre `Debug\.` bajo `Runtime/Harness/Unity/`). Se descartó una prueba
      EditMode que barriera el IL: es maquinaria nueva para un solo archivo, y una prueba que se
      salta no es más fuerte que un `rg` de la compuerta de verificación.
- [ ] **`OnApplicationQuit` en Quest/Android es comportamiento de Unity que este diseño no
      verificó** (AD6). Se reporta ampliamente que no dispara de forma fiable al cerrar una app de
      Android, así que el respaldo automático de cierre puede no existir en el dispositivo donde
      corre la compuerta 2. Por eso el cierre explícito con el control de escena es obligatorio y
      no opcional; confirmarlo en el Quest es parte de esa compuerta, no de este diseño.
- [ ] **El `PhysicalActionChannel` queda cableado a un solo productor real (M7) y a un consumidor
      que devuelve `null`.** La acción mueve la receptividad y nada más. Es lo que PR1 decidió; se
      registra aquí porque en la escena se ve como un canal que "no hace nada".
- [ ] **La costura de M13 es invisible en el Inspector** (AD5). Es el costo aceptado a cambio de
      que los cuatro efectos tengan un solo dueño. Si la compuerta humana lo encuentra confuso, la
      alternativa aditiva es exponer además un `UnityEvent` serializado que el compositor no use.
- [ ] **La decisión 3 de la propuesta (excepción a la regla dura 4, hallazgo W7)** sigue siendo un
      valor por defecto del orquestador, vetable por el usuario. Este diseño la ejecuta como se
      propuso: la nota va en `Docs/MODULES.md`, sección M11, y no se crea `Runtime/Harness/Fakes/`.
- [ ] **El nombre del script de composición (`CompositorDeArnes`) es nuevo.** PR1 lo llamaba
      `HarnessBehaviour.cs` en `Samples~/`, nombre que ahora ocupa la cáscara del paquete. Si el
      usuario prefiere otro, es un renombrado sin efecto en el resto del diseño.
