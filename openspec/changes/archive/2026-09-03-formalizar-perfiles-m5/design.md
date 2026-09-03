# Design: Formalizar M5 — Perfiles de personalidad

## Technical Approach

"Documentar y fijar lo que existe". M5 no se redisena: se le agrega su especificacion versionada.
Este cambio mueve el alcance de M5 y las garantias de sus datos desde el codigo, el `README.md`
de la carpeta y las memorias de Engram hacia una spec OpenSpec con escenarios ejecutables
mapeados a pruebas ya en verde. **Cero lineas de runtime.** Las 8 pruebas de
`PersonalityProfilesDataTests` y los 4 `.asset` ya existen (implementacion directa); este cambio
solo escribe los 4 artefactos Markdown.

### Unidad de modulo y grafo de referencias

M5 es **datos, no un ensamblado**: `Data/Personalities/*.asset`, sin `.asmdef` propio. El
esquema que instancian lo posee M4:

    ReceptivityProfileAsset        en  NpcAi.Receptivity.Unity   -> [NpcAi.Core, NpcAi.Receptivity]
    ReceptivityProfile / Catalog   en  NpcAi.Receptivity          -> [NpcAi.Core]   (noEngineReferences: true)

La prueba de aceptacion de M5 vive en `Tests/EditMode/Receptivity/PersonalityProfilesDataTests.cs`
y **reusa** el ensamblado de prueba de M4 (`NpcAi.Receptivity.Tests`), que ya referencia
`NpcAi.Core`, `NpcAi.Receptivity`, `NpcAi.Receptivity.Unity`, `UnityEngine.TestRunner` y
`UnityEditor.TestRunner`. Ningun ensamblado gana o pierde referencias. La regla dura "un cambio,
un modulo de `Runtime/`" no aplica: no se toca ninguna carpeta de `Runtime/`.

## Architecture Decisions

| # | Decision | Eleccion | Alternativas rechazadas | Razon |
|---|---|---|---|---|
| AD1 | Forma de M5 | 4 archivos `.asset` en `Data/Personalities/`, uno por personalidad; cero clases nuevas | `enum PersonalityId`; una clase o `ScriptableObject` con logica por personalidad | CLAUDE.md regla 7 ("lo variable es dato") y regla 3 (M5 no puede tener un asmdef que M4 referencie). `PersonalityId` es `readonly struct` justo para que pasar de 4 a 8 no toque `NpcAi.Core` |
| AD2 | Dueno del esquema | La clase `ReceptivityProfileAsset` (y `ReceptivityProfile`, `ReceptivityProfileCatalog`) vive en M4; M5 solo aporta instancias | Poner el esquema en M5 | Hereda de M4 AD4 / [[m4-decision-esquema-perfil]]. M4 no puede referenciar el (inexistente) ensamblado de M5, y M5 no escribe clases |
| AD3 | Origen de los numeros | 1:1 desde `ReceptivityProfileCatalog.Standard()` — `PerfilGrosero()`, `PerfilHisterico()`, `PerfilIntrovertido()`, `PerfilEmpatico()`. Verificado campo a campo contra `Runtime/Receptivity/ReceptivityProfileCatalog.cs` | Tunear de cero ahora con datos inventados | `Standard()` ya paso por la revision de M4 y esta cubierto por sus pruebas. El tuning fino necesita datos de uso que todavia no hay. Reproducirlo 1:1 prueba la costura con datos reales sin abrir una discusion de balance prematura |
| AD4 | Ubicacion de la prueba de carga | `Tests/EditMode/Receptivity/PersonalityProfilesDataTests.cs`, reusando el asmdef `NpcAi.Receptivity.Tests` | Nuevo ensamblado `Tests/EditMode/Personalities/` con su `.asmdef` | La prueba ejerce `ReceptivityProfileAsset.BuildCatalog` (adaptador de M4). Un asmdef nuevo solo para 8 pruebas duplicaria las mismas 3 referencias sin ganancia. Es `Tests/`, no `Runtime/`: no viola "un cambio, un modulo" |
| AD5 | Como carga la prueba los `.asset` | `AssetDatabase.FindAssets("t:ReceptivityProfileAsset")` + filtro por ruta que contenga `Data/Personalities/` | Mover los `.asset` a `Resources/` y usar `Resources.LoadAll`; un fixture `ScriptableObject` con 4 refs `[SerializeField]` cableadas en el Inspector | `FindAssets` descubre los 4 sin cableado manual, sin mover los datos de su carpeta y sin ruta absoluta al paquete (resiliente a que el UPM cambie de path). El asmdef es `includePlatforms: [Editor]`, asi que `using UnityEditor` compila sin `#if UNITY_EDITOR` |
| AD6 | Guarda de datos en la prueba | El test valida signos (`agresion <= 0`, `empatia >= 0`) y coherencia de umbrales, aunque el motor ya blinde la monotonia | Confiar solo en `Blindar()` de M4 | `Blindar()` hace **imposible** violar la monotonia en runtime, pero un `.asset` con un signo cambiado seguiria siendo un dato mentiroso que enganaria a quien lea la tabla. El test lo atrapa en milisegundos, en EditMode, antes de cualquier escena |
| AD7 | Estado de entrega de M5 | Dato inerte: `ReceptivityEngine()` sigue usando `Standard()`; los `.asset` no se inyectan al motor todavia | Cablear ya los 4 `.asset` al motor por defecto | Cablear el catalogo real es de un bootstrap / escena (M8, M11): necesita un punto de composicion que M5 no tiene. M5 entrega los datos y su costura (`BuildCatalog`) probada; el consumo en produccion es de otro modulo |
| AD8 | Gotcha de NUnit | La prueba usa comparacion con `==`, no `Is.AnyOf` | `Assert.That(x, Is.AnyOf(...))` | Unity Test Framework trae NUnit 3.5; `Is.AnyOf` se agrego en 3.6. Usarlo no compila y tumba todo el ensamblado de pruebas. Ver [[gotcha-unity-nunit-3-5]] |
| AD9 | Validacion de aceptacion | Verde EditMode verificado por humano en el Test Runner de Unity 6 | Compuerta de CI | No hay CI en el repo; el repo ES el paquete. Ningun agente ejecuta Unity: cada verde es compuerta humana. Igual que M4 AD11 |

## Data Flow

### Costura de M5 (construccion del catalogo desde los datos reales)

    Data/Personalities/grosero.asset       |
    Data/Personalities/histerico.asset     |  AssetDatabase.FindAssets("t:ReceptivityProfileAsset")
    Data/Personalities/introvertido.asset  |    (SOLO en la prueba de carga; no en runtime)
    Data/Personalities/empatico.asset      |
                                           v
                          ReceptivityProfileAsset[]   (SO, NpcAi.Receptivity.Unity)
                                           |  ReceptivityProfileAsset.BuildCatalog(assets)   (estatico, de M4)
                                           |     usa ToProfile() de cada asset
                                           v
                                 ReceptivityProfileCatalog  -->  new ReceptivityEngine(catalogo)
                                                                  (lo hara un bootstrap de M8/M11)

### Runtime hoy (sin M5 cableado)

    new ReceptivityEngine()  -->  ReceptivityProfileCatalog.Standard()   (numeros en codigo, identicos a los .asset)

M5 entrega los 4 `.asset` y deja probada la costura. El cambio de `Standard()` a
`BuildCatalog(assets)` como fuente del motor es una decision de composicion de un modulo
posterior.

## Tuning de record (numeros vigentes al 2026-09-03)

Fuente: `Data/Personalities/*.asset`, identicos 1:1 a `ReceptivityProfileCatalog.Standard()`.
Lo no listado en una tabla de delta suma `0`. El ajuste fino de estos numeros es **dato** y
**no es cambio de contrato** de M4.

### grosero — extremo hostil

| Umbral | Valor |  | Intencion | delta |  | Tono | delta |  | Accion fisica | delta |
|---|---|---|---|---|---|---|---|---|---|---|
| umbralReceptivo | 4 |  | SolicitudRespetuosa | +1 |  | Respetuoso | +1 |  | ContactoVisual | +1 |
| umbralNoReceptivo | -2 |  | SolicitudAgresiva | -3 |  | Agresivo | -2 |  | EntregarObjeto | +1 |
| limitePuntaje | 5 |  | Empatia | +2 |  | Empatico | +1 |  | GestoCalma | +1 |
| puntajeInicial | -2 |  | AportaInformacion | +1 |  | Ansioso | -1 |  | Alejarse | -1 |
| | |  | PreguntaFueraDeTema | -2 |  | | |  | TocarPaciente | -2 |
| | |  | Interrupcion | -2 |  | | |  | | |

Estado inicial en el motor: `puntajeInicial -2 == umbralNoReceptivo -2` -> **NoReceptivo**.

### histerico — alta reactividad emocional

| Umbral | Valor |  | Intencion | delta |  | Tono | delta |  | Accion fisica | delta |
|---|---|---|---|---|---|---|---|---|---|---|
| umbralReceptivo | 3 |  | SolicitudRespetuosa | +1 |  | Respetuoso | +1 |  | ContactoVisual | +1 |
| umbralNoReceptivo | -2 |  | SolicitudAgresiva | -3 |  | Agresivo | -3 |  | GestoCalma | +3 |
| limitePuntaje | 5 |  | Empatia | +2 |  | Empatico | +2 |  | Acercarse | -2 |
| puntajeInicial | 0 |  | AportaInformacion | +1 |  | Ansioso | -2 |  | Alejarse | -1 |
| | |  | PreguntaFueraDeTema | -1 |  | | |  | TocarPaciente | -1 |
| | |  | Interrupcion | -3 |  | | |  | | |

Estado inicial: `0` entre umbrales -> **Neutral**.

### introvertido — baja iniciativa

| Umbral | Valor |  | Intencion | delta |  | Tono | delta |  | Accion fisica | delta |
|---|---|---|---|---|---|---|---|---|---|---|
| umbralReceptivo | 3 |  | SolicitudRespetuosa | +1 |  | Respetuoso | +1 |  | ContactoVisual | +1 |
| umbralNoReceptivo | -3 |  | SolicitudAgresiva | -2 |  | Agresivo | -2 |  | GestoCalma | +1 |
| limitePuntaje | 4 |  | Empatia | +1 |  | Empatico | +1 |  | Acercarse | -1 |
| puntajeInicial | -1 |  | AportaInformacion | +2 |  | Ansioso | -1 |  | Alejarse | -1 |
| | |  | PreguntaFueraDeTema | -1 |  | | |  | | |
| | |  | Interrupcion | -2 |  | | |  | | |

Estado inicial: `-1` entre umbrales -> **Neutral**.

### empatico — extremo cooperativo (linea base)

| Umbral | Valor |  | Intencion | delta |  | Tono | delta |  | Accion fisica | delta |
|---|---|---|---|---|---|---|---|---|---|---|
| umbralReceptivo | 2 |  | SolicitudRespetuosa | +2 |  | Respetuoso | +1 |  | ContactoVisual | +1 |
| umbralNoReceptivo | -3 |  | SolicitudAgresiva | -1 |  | Agresivo | -1 |  | Acercarse | +1 |
| limitePuntaje | 4 |  | Empatia | +2 |  | Empatico | +2 |  | EntregarObjeto | +1 |
| puntajeInicial | 1 |  | AportaInformacion | +2 |  | | |  | GestoCalma | +1 |
| | |  | Interrupcion | -1 |  | | |  | Alejarse | -1 |

`PreguntaFueraDeTema` y `Ansioso` no se puntuan (quedan en `0`). Estado inicial: `1` entre
umbrales -> **Neutral**.

## File Inventory

| Archivo | Rol | Estado en este cambio |
|---|---|---|
| `Data/Personalities/grosero.asset` (+ `.meta`) | Perfil, instancia de `ReceptivityProfileAsset` | Ya escrito (usuario) |
| `Data/Personalities/histerico.asset` (+ `.meta`) | Perfil | Ya escrito (usuario) |
| `Data/Personalities/introvertido.asset` (+ `.meta`) | Perfil | Ya escrito (usuario) |
| `Data/Personalities/empatico.asset` (+ `.meta`) | Perfil | Ya escrito (usuario) |
| `Data/Personalities/README.md` | Alcance de M5 (4 personalidades, ampliacion futura) | Existente, sin cambios |
| `Tests/EditMode/Receptivity/PersonalityProfilesDataTests.cs` (+ `.meta`) | Prueba de carga: 8 pruebas EditMode | Ya escrito (agente) |
| `Runtime/Receptivity/**` | Esquema y motor | Sin cambios; solo referenciado |

## Interfaces / Contracts

Sin firmas nuevas. M5 consume la superficie publica que ya fija `receptividad-m4`:

```csharp
// NpcAi.Receptivity.Unity  (esquema, propiedad de M4)
public sealed class ReceptivityProfileAsset : ScriptableObject
{
    public string personalityId;                                      // campo editable; clave del catalogo
    public int umbralReceptivo, umbralNoReceptivo, limitePuntaje, puntajeInicial;
    public IntentDelta[] porIntencion;                                // structs [Serializable]
    public ToneDelta[]   porTono;
    public ActionDelta[] porAccion;
    public PersonalityId PersonalityId { get; }                       // id normalizado
    public ReceptivityProfile ToProfile();                            // proyeccion 1:1, sin logica
    public static ReceptivityProfileCatalog BuildCatalog(             // costura de M5
        IEnumerable<ReceptivityProfileAsset> assets);
}

// NpcAi.Receptivity
public sealed class ReceptivityProfile { public static ReceptivityProfile Default { get; } /* ... */ }
public sealed class ReceptivityProfileCatalog
{
    public IEnumerable<PersonalityId> Personalidades { get; }
    public ReceptivityProfile PerfilDe(PersonalityId personality);    // Default si no existe
}
```

## Testing Strategy

Este cambio agrega **cero** pruebas: `PersonalityProfilesDataTests` (8) se escribio como parte
de la implementacion directa de M5. La spec se apoya en lo que ya corre:

| Archivo | Pruebas | Que fija para M5 |
|---|---|---|
| `Tests/EditMode/Receptivity/PersonalityProfilesDataTests.cs` | 8 | Exactamente 4 `.asset`; nombre == `personalityId`; `BuildCatalog` resuelve los 4; id ausente / `None` -> `Default`; signos del contrato; umbrales coherentes; cada perfil entra a `ReceptivityEngine` sin lanzar; extremos hostil vs cooperativo distinguibles |
| `Tests/EditMode/Receptivity/ReceptivityProfileAssetTests.cs` | (de M4) | `ToProfile()` proyecta umbrales y las 3 tablas; `BuildCatalog` tolera `null` |
| `Tests/EditMode/Receptivity/ReceptivityProfileCatalogTests.cs` | (de M4) | `Standard()` trae exactamente `grosero, histerico, introvertido, empatico`; id desconocido -> `Default` |
| `Tests/EditMode/Receptivity/ReceptivityEngineTests.cs` | (de M4) | `Grosero_arranca_no_receptivo_y_empatico_arranca_neutral` |

Ejecucion: Test Runner de Unity 6 (EditMode > Run All). Sin escena, sin VR.

## Threat Matrix

N/A — M5 son 4 archivos de datos YAML de Unity y una prueba EditMode de solo lectura sobre
`AssetDatabase`. No hay frontera de enrutamiento, shell, subproceso ni automatizacion VCS/PR.
Este cambio ademas solo escribe Markdown bajo `openspec/`.

## Migration / Rollout

No se requiere migracion: no hay cambio de codigo y el motor sigue usando `Standard()`. **No es
cambio de contrato**, no aplica ventana fija. Integracion como cambio propio con auto-merge y
checklist "Antes de mergear".

Rollback: `git revert` del commit elimina los 4 artefactos OpenSpec y, si van juntos, los 4
`.asset` y la prueba. Ningun consumidor necesita accion.

## Open Questions

- [ ] Cuando un bootstrap de M8/M11 inyecte el catalogo real, decidir si `ReceptivityEngine()`
      (ctor sin args) sigue existiendo con `Standard()` o si se marca obsoleto. Es decision de
      ese modulo, no de M5.
- [ ] Si el tuning de una personalidad se ajusta con datos de uso, este design se actualiza en un
      cambio propio de M5 (editar el `.asset` + la tabla de aca). No toca `receptividad-m4`.
