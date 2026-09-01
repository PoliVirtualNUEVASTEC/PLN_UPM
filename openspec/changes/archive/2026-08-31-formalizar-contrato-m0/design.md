# Design: Formalizar el contrato v1 del Nucleo M0

## Technical Approach

Enfoque A, "documentar y fijar lo que existe". El contrato v1 no se redisena: se le agrega
evidencia ejecutable. La estrategia es mover 15 afirmaciones (G1-G15) desde comentarios y
documentacion hacia tres soportes verificables: pruebas de reflexion sobre el ensamblado,
metodos `[Test]` **concretos** en las clases base de contrato, y la seccion v1 del changelog.
El runtime cambia ~6 lineas (dos guardas nulas). `Contract.Version` queda congelado en 1.

### Unidad de modulo y grafo de referencias

Un solo modulo: **M0 = `NpcAi.Core` + `NpcAi.Core.Channels`**, tratados como una unica unidad de
contrato (regla 2 de CLAUDE.md los nombra juntos), mas su ensamblado de prueba `NpcAi.Core.Tests`.
El grafo se confirma sin cambios estructurales:

    NpcAi.Core            -> []                      (noEngineReferences: true)
    NpcAi.Core.Channels   -> [NpcAi.Core]            (Unity permitido: ScriptableObject)
    NpcAi.Core.Tests      -> [NpcAi.Core, NpcAi.Core.Channels*, UnityEngine.TestRunner,
                              UnityEditor.TestRunner] + nunit.framework.dll
    (*) unica arista nueva del cambio (G3)

Ningun modulo M1-M10 gana o pierde referencias. Ningun doble bajo `Runtime/*/Fakes/` se toca.

## Architecture Decisions

| # | Decision | Eleccion | Alternativas rechazadas | Razon |
|---|---|---|---|---|
| D1 | Enfoque global | A: documentar y fijar | B: solo docs. C: `IEquatable` + ganchos "no listo" en el contrato | B deja las afirmaciones sin exigir: el doble puede divergir y ninguna prueba falla. C obliga a miembros `abstract` nuevos en las bases compartidas, lo que obliga a cambiar los 7 dobles y rompe "un cambio = un modulo" |
| D2 | Forma de la cobertura nueva | Metodos `[Test]` **no abstractos** en las bases existentes | Nuevos miembros `abstract` (`CreateNotReadySubject()`, etc.) | Un metodo concreto heredado hace que cada modulo corra mas pruebas con **cero** cambios en su doble y en su `.asmdef`. Un miembro `abstract` rompe la compilacion de los 7 ensamblados de prueba de modulo |
| D3 | Camino `IsReady == false` (G9) | Stub minimo no-listo definido **dentro** de `Tests/EditMode/Core/` | Reutilizar un doble de modulo; agregar un modo no-listo a `ScriptedIntentClassifier` | `ScriptedIntentClassifier.IsReady` es `true` por diseno (no carga modelos): el camino no-listo es inalcanzable desde los dobles. Un stub local mantiene la escritura dentro de las 4 rutas autorizadas |
| D4 | Sincronia version-changelog (G1) | Leer `Path.GetFullPath("Packages/com.poli.npc-ai/Docs/CONTRACT-CHANGELOG.md")`, parsear el mayor `## v<N>` y `Assert.AreEqual(Contract.Version, N)`; `Assert.Ignore` si el archivo no existe | Recurso embebido; atributo de ensamblado con la version | Es la unica forma de atar el numero al documento sin duplicarlo. Fragilidad documentada: renombrar el paquete rompe la ruta; por eso `Assert.Ignore` en vez de `Assert.Fail` |
| D5 | Pureza del ensamblado (G2) | `typeof(NpcAi.Core.Contract).Assembly.GetReferencedAssemblies()`: cero nombres que empiecen por `UnityEngine`/`UnityEditor`, cero `NpcAi.*` distinto de si mismo. Opcional: parsear el `.asmdef` y afirmar `noEngineReferences == true` y `references.length == 0` | Confiar solo en el `.asmdef`; revision manual | La reflexion prueba el binario compilado, no la intencion declarada. El chequeo del `.asmdef` es complementario y detecta la regresion antes de que el compilador la materialice |
| D6 | Pruebas de canales (G3) | Agregar `"NpcAi.Core.Channels"` a `NpcAi.Core.Tests.asmdef` | Crear `NpcAi.Core.Channels.Tests.asmdef` aparte | El ensamblado de prueba **ya** depende de Unity via `UnityEngine.TestRunner` y `noEngineReferences: false`; no se pierde ninguna pureza. Un ensamblado extra agrega un `.asmdef` y un `.meta` de mantenimiento sin ganancia. Instanciacion via `ScriptableObject.CreateInstance<T>()` para los 5 canales; para `OnDisable`, `DestroyImmediate` y luego afirmar que un `Raise` posterior no invoca al handler viejo |
| D7 | Guardas nulas en DTO (G5, G6) | `Text = text ?? string.Empty` y `ReasonCode = reasonCode ?? string.Empty` en los constructores | Lanzar `ArgumentNullException`; dejarlo como esta | No es ruptura: ningun consumidor correcto podia depender de recibir `null`, e `IsEmpty` ya trataba `null` como vacio via `IsNullOrWhiteSpace`. Lanzar si seria ruptura. Por eso `Contract.Version` se queda en **1**. Unico cambio de runtime del PR |
| D8 | Completitud reversible (G11) | `IsComplete` es funcion pura de `Progress01`: verdadero si y solo si `Progress01 == 1` (tolerancia `1e-4`); ambos PUEDEN bajar de nuevo | Completitud pegajosa (una vez completo, siempre completo) | Decision del usuario (2026-08-31). La prueba de contrato **no** debe afirmar pegajosidad: afirma la relacion si-y-solo-si y que `Notify(default)` es no-op. `default` no es `Improved` ni `Worsened`, asi que el no-op ya se cumple en los dos dobles de escenario |
| D9 | Congelamiento de enums (G4) | Una prueba por enum comparando `Enum.GetNames`/`GetValues` contra una lista ordenada esperada de (nombre, valor) mas el conteo; `-1` y `1` explicitos en `Receptivity` | Afirmar solo el centinela `0`; afirmar solo el conteo | Fija nombre, valor, orden y cardinalidad. Consecuencia deliberada y documentada: cualquier rename futuro deja de ser refactor y pasa a ser ruptura de contrato con ventana del lunes |
| D10 | Colision de nombres | Calificar `Core.Receptivity` en todo archivo de prueba nuevo | `using` alias; renombrar el enum | El enum `NpcAi.Core.Receptivity` colisiona con el namespace `NpcAi.Receptivity` (M4). El codigo existente ya usa `Core.Receptivity`; se sigue el patron establecido. Renombrar el enum es ruptura de contrato |
| D11 | Entrega | **Un solo PR** marcado `size:exception`, organizado internamente en 4 grupos de commit revertibles | PRs encadenados A -> B -> C -> D | Decision del usuario (2026-08-31): revision pesada asumida. Encadenar 4 PRs sobre un cambio de contrato multiplica por 4 la ventana del lunes y la co-aprobacion de todos los duenos |
| D12 | Archivos de prueba nuevos | 3 archivos nuevos (`EventChannelTests.cs`, `ContractVersionChangelogTests.cs`, `CoreAssemblyPurityTests.cs`) mas adiciones a `ContractTypeTests.cs` y a las bases `*Contract.cs` | Un unico archivo `M0ContractTests.cs` | Un archivo por preocupacion mantiene el mapeo grupo-commit-archivo limpio y hace el revert por grupo trivial. Ver *File Changes* |
| D13 | Gobernanza y version | Cambio de contrato: ventana del lunes y co-aprobacion de todos los duenos de modulo. `Contract.Version` congelado en 1. **No** se cambia `const` a `static readonly` en esta pasada | Subir a v2; cambiar a `static readonly` ahora | La v2 obligaria a migracion en 10 ensamblados por dos guardas nulas. `const` se documenta como riesgo conocido (se hornea en los consumidores); cambiarlo aqui mezclaria una preocupacion de empaquetado con la formalizacion |
| D14 | Validacion de aceptacion | Verde EditMode verificado por humano en el Test Runner de Unity 6, o via CLI `Unity -runTests -batchmode -projectPath <host-unity-project> -testPlatform EditMode -testResults results.xml` | Compuerta de CI | No hay CI en el repositorio y el repositorio ES el paquete; correr pruebas exige el proyecto Unity anfitrion. La compuerta es humana y explicita, no automatizada |

## Data Flow

No hay flujo de datos nuevo en tiempo de ejecucion. El flujo relevante es el de **verificacion**,
y es lo unico que este cambio agrega:

    Docs/CONTRACT-CHANGELOG.md --lee--> ContractVersionChangelogTests --afirma--> Contract.Version
    NpcAi.Core.dll --reflexion--> CoreAssemblyPurityTests --afirma--> cero Unity, cero NpcAi.* ajeno
    Runtime/Core/Enums.cs --reflexion--> ContractTypeTests --afirma--> (nombre, valor, conteo)
    Runtime/CoreChannels/ --CreateInstance--> EventChannelTests --afirma--> Raise/Subscribe/OnDisable

    *Contract.cs (bases) --[Test] concretos heredados--> 7 dobles de modulo (sin tocarlos)

La ultima flecha es la palanca del diseno: agregar un metodo concreto a una base hace que los 7
ensamblados de prueba de modulo ejecuten mas verificacion sin una sola linea modificada en ellos.

## File Changes

| Archivo | Accion | Descripcion | Grupo |
|---|---|---|---|
| `Docs/CONTRACT-CHANGELOG.md` | Modificar | Reescritura de la seccion v1: invariantes por puerto, `Score`/`ReasonCode` como diagnostico no tipado, asimetria de determinismo, centinelas, igualdad de DTO, nota sobre `const`, puntero de enforcement (G15) | A |
| `Tests/EditMode/Core/ContractVersionChangelogTests.cs` | Crear | G1: version atada al mayor `## v<N>`; `Assert.Ignore` si falta el archivo | A |
| `Tests/EditMode/Core/CoreAssemblyPurityTests.cs` | Crear | G2: reflexion sobre referencias del ensamblado (+ chequeo opcional del `.asmdef`) | A |
| `Tests/EditMode/Core/NpcAi.Core.Tests.asmdef` | Modificar | G3: agregar `"NpcAi.Core.Channels"` a `references` | B |
| `Tests/EditMode/Core/EventChannelTests.cs` | Crear | G3: camino feliz, guardas nulas en `Subscribe`/`Unsubscribe`, desuscripcion, `OnDisable` limpia listeners | B |
| `Runtime/Core/Dtos.cs` | Modificar | G5, G6: `Text ?? string.Empty` y `ReasonCode ?? string.Empty` (~6 lineas) | C |
| `Tests/EditMode/Core/ContractTypeTests.cs` | Modificar | G4 (4 pruebas de congelamiento de enum), G5/G6 (aserciones de guarda nula), G13 (`Unknown(latencyMs)` preserva latencia) | C |
| `Runtime/Core/Ports.cs` | Modificar | Solo XML-doc: G7, G8, G9, G11 (sin cambio de comportamiento) | D |
| `Tests/EditMode/Core/ReceptivityEngineContract.cs` | Modificar | G7: `Current` es `Neutral` hasta `Reset` | D |
| `Tests/EditMode/Core/DialogueGeneratorContract.cs` | Modificar | G8: `Generate` NO exige determinismo (Markov), a diferencia de `Classify` | D |
| `Tests/EditMode/Core/IntentClassifierContract.cs` | Modificar | G9: `Classify` NO DEBE lanzar en ningun estado; stub no-listo local para el camino `IsReady == false` | D |
| `Tests/EditMode/Core/SpeechToTextContract.cs` | Modificar | G10: nada antes del primer `StartListening`, `StartListening` doble, Start/Stop/Start reanuda, fan-out multi-suscriptor, emitir sin suscriptores no lanza | D |
| `Tests/EditMode/Core/ScenarioObjectiveContract.cs` | Modificar | G11: `IsComplete` si y solo si `Progress01 == 1` (tol `1e-4`), reversible, `Notify(default)` no-op | D |

Sin archivos borrados. G12 (`IEquatable`) y G14 (clamp de floats) **no** producen codigo: solo
quedan registrados en el changelog como diferidos a v2.

## Interfaces / Contracts

No hay firmas nuevas en `NpcAi.Core`. Los dos unicos cambios de comportamiento:

```csharp
// Runtime/Core/Dtos.cs — NpcReply (G5)
Text = text ?? string.Empty;            // antes: Text = text;

// Runtime/Core/Dtos.cs — ReceptivityChange (G6)
ReasonCode = reasonCode ?? string.Empty; // antes: ReasonCode = reasonCode;
```

El stub de G9 vive solo en pruebas y nunca sale de `Tests/EditMode/Core/`:

```csharp
// Tests/EditMode/Core/IntentClassifierContract.cs (privado, no un doble de modulo)
private sealed class ClasificadorNoListo : IIntentClassifier
{
    public bool IsReady => false;
    public IntentResult Classify(string text) => IntentResult.Unknown();
}
```

## Testing Strategy

| Capa | Que se prueba | Enfoque |
|---|---|---|
| Meta (reflexion) | Version vs changelog, pureza del ensamblado, congelamiento de enums | `Assembly.GetReferencedAssemblies`, `Enum.GetNames`/`GetValues`, lectura de archivo con `Assert.Ignore` defensivo |
| Unidad (DTO) | Guardas nulas, `IntentResult.Unknown(latencyMs)`, direccion de `ReceptivityChange` | Adiciones a `ContractTypeTests` |
| Unidad (canales) | `Raise`/`Subscribe`/`Unsubscribe`/`OnDisable` de los 5 canales | `ScriptableObject.CreateInstance<T>()`; `DestroyImmediate` y `Raise` posterior para `OnDisable` |
| Contrato (heredada) | Semantica de los 7 puertos | `[Test]` concretos en las bases `*Contract.cs`; se ejecutan automaticamente en los 7 ensamblados de prueba de modulo |
| Integracion / E2E | N/A | No existe capa automatizada; M11 Samples~/Harness es cableado manual |

Ejecucion: Test Runner de Unity 6 (EditMode > Run All) o el comando CLI de D14.

## Threat Matrix

N/A — no hay frontera de enrutamiento, shell, subproceso, automatizacion VCS/PR, clasificacion de
archivos ejecutables ni integracion de procesos. El unico acceso a I/O es una lectura de archivo de
solo lectura, con ruta fija, dentro de una prueba (D4).

## Migration / Rollout

No se requiere migracion: `Contract.Version` sigue en 1 y ningun consumidor cambia. Integracion en
la ventana del lunes con co-aprobacion de todos los duenos de modulo.

Rollback por grupo dentro del PR unico:

| Grupo | Contenido | Historia de reversion |
|---|---|---|
| A | G1, G2, G15 | `git revert` devuelve el changelog a su v1 corta y elimina 2 archivos de prueba de meta. Cero runtime, cero modulos afectados |
| B | G3 | Elimina `EventChannelTests.cs` y la arista `NpcAi.Core.Channels` del `.asmdef` de prueba, que vuelve a 3 referencias. Cero runtime |
| C | G4, G5, G6, G12, G13, G14 | Unico grupo con runtime: devuelve `Dtos.cs` al estado de `a7a0590` y retira las aserciones de enum/DTO. Como la version no subio, ningun modulo necesita accion tras el revert |
| D | G7-G11 | Retira los `[Test]` concretos de las bases y las notas XML-doc de `Ports.cs`. Los ensamblados de prueba de modulo simplemente ejecutan menos pruebas; ninguno deja de compilar |

Revertir cualquier grupo aislado deja el arbol compilable: ningun grupo depende de otro en tiempo de
compilacion (B agrega una referencia que solo B usa; C es el unico que toca runtime).

## Open Questions

- [ ] **Riesgo estructural del D2:** un `[Test]` concreto nuevo en una base se ejecuta contra los 7
      dobles existentes. Si un doble viola la invariante recien codificada, su ensamblado de prueba de
      modulo se pone rojo y arreglarlo exigiria tocar otro modulo, lo cual esta prohibido. Regla de
      operacion: si un doble falla, **no** se arregla en este cambio; se registra como cambio SDD del
      modulo dueno y la asercion se ajusta o se difiere. Verificacion previa hecha sobre
      `ScriptedScenarioObjective` (M9/M10, cumple el si-y-solo-si) y `ScriptedIntentClassifier`
      (M2, `IsReady` siempre `true`, de ahi D3).
- [ ] Confirmar en Unity 6 que `DestroyImmediate` sobre un `ScriptableObject` creado con
      `CreateInstance` dispara `OnDisable` de forma sincrona antes de la asercion (D6).
- [ ] Definir si el chequeo opcional del `.asmdef` de D5 se incluye en este PR o se difiere; duplica
      la fragilidad de ruta ya aceptada en D4.
