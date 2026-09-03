# Propuesta: Formalizar M5 — Perfiles de personalidad (Data/Personalities)

## Intent

Los 4 perfiles de personalidad de M5 ya existen como datos y estan validados: `grosero.asset`,
`histerico.asset`, `introvertido.asset` y `empatico.asset` en `Data/Personalities/`, cada uno una
instancia de `ReceptivityProfileAsset` (el esquema lo posee M4, sub-ensamblado
`NpcAi.Receptivity.Unity`). Una prueba de carga EditMode nueva
(`Tests/EditMode/Receptivity/PersonalityProfilesDataTests.cs`, 8 pruebas) levanta los 4 `.asset`
reales, arma el catalogo con `ReceptivityProfileAsset.BuildCatalog(assets)` y fija que respetan
el contrato de receptividad.

Lo que falta es su especificacion versionada en OpenSpec, a la par de lo que M0, M2 y M4 ya
tienen en `openspec/specs/`. Mientras eso no exista, el alcance de M5 (cuantas personalidades,
que garantiza cada `.asset`, donde vive el tuning) solo esta en el codigo, en el `README.md` de
la carpeta y en las memorias de Engram. Esta propuesta **no reescribe** M5: documenta y congela
los 4 datos y su prueba, y cierra la *Open Question* que dejo abierta el design de
`receptividad-m4` — donde se documentan los numeros concretos por personalidad.

## Scope

### In Scope

- Especificacion OpenSpec versionada de M5: nueva capacidad `perfiles-personalidad-m5` (spec
  delta `ADDED`), design y tasks de este cambio.
- **Frontera de escritura:** `openspec/changes/2026-09-03-formalizar-perfiles-m5/` y, al
  archivar, `openspec/specs/perfiles-personalidad-m5/`.
- Los 4 `.asset` de `Data/Personalities/` y la prueba de carga
  `PersonalityProfilesDataTests.cs`: ya escritos como implementacion directa; este cambio los
  **documenta y traza**, no agrega ni una linea mas.
- Tabla "Tuning de record" en el design: los umbrales y las 3 tablas de delta de las 4
  personalidades, con la nota de que el ajuste fino es dato y no es cambio de contrato.

### Out of Scope

- Cualquier cambio de codigo en `Runtime/`. M4 ya define el esquema (`ReceptivityProfileAsset`,
  `ReceptivityProfile`, `ReceptivityProfileCatalog`); M5 no escribe clases.
- El cableado de los 4 `.asset` al motor en runtime. `ReceptivityEngine()` sigue usando
  `ReceptivityProfileCatalog.Standard()` (numeros en codigo). Inyectar el catalogo armado desde
  los `.asset` es de un bootstrap / escena (M8, M11), no de M5. M5 entrega **dato inerte** con su
  costura probada.
- El esquema del perfil y los invariantes del motor: los posee `receptividad-m4` y NO se tocan.
  `perfiles-personalidad-m5` documenta los **datos** que ese esquema consume.
- El puerto `IReceptivityEngine` y los tipos de `NpcAi.Core` (`PersonalityId`, enums): los posee
  `contrato-nucleo-m0`.
- Ampliar el reparto a 8 personalidades (`parlanchin`, `extrovertido`, `tranquilo`, `pesimista`):
  es crear mas `.asset`, cambio aparte.
- El receipt de revision con `--projection staged`: RDD esta apagado por defecto; encenderlo o
  entregar bajo politica de repo normal es decision del autor, fuera de este cambio.
- El merge del PR: lo hace el autor (regla 8).

## Capabilities

### New Capabilities

- `perfiles-personalidad-m5`: el alcance y las garantias de los datos de personalidad de M5 —
  exactamente 4 `.asset` en `Data/Personalities/`, cero clases nuevas; el esquema es de M4;
  cada perfil respeta los signos del contrato de receptividad y tiene umbrales coherentes;
  los 4 `.asset` arman un `ReceptivityProfileCatalog` via `BuildCatalog` sin tocar el motor;
  un id ausente cae en `ReceptivityProfile.Default`; los extremos hostil y cooperativo son
  distinguibles; el tuning fino es dato y su fuente de verdad esta en este cambio.

### Modified Capabilities

- Ninguna. `receptividad-m4` documenta el esquema y el motor y NO se modifica: sus escenarios
  siguen apoyandose en `Standard()`. `perfiles-personalidad-m5` documenta los datos que ese
  esquema consume y cierra su *Open Question* sin editar su spec.

## Approach

"Documentar y fijar lo que existe", igual que la formalizacion de M0 y de M4:

- **Spec:** captura los invariantes que hoy ya exigen las 8 pruebas de
  `PersonalityProfilesDataTests` y las de M4 que tocan el adaptador y el catalogo
  (`ReceptivityProfileAssetTests`, `ReceptivityProfileCatalogTests`, `ReceptivityEngineTests`).
  Escenarios en `Dado/Cuando/Entonces`, niveles `DEBE/NO DEBE/DEBERIA/PUEDE`.
- **Design:** registra las decisiones (datos y no codigo; esquema en M4; numeros 1:1 desde
  `Standard()`; el test reusa el asmdef de M4; carga por `AssetDatabase.FindAssets` filtrado por
  ruta; dato inerte hasta el bootstrap), el flujo de datos de la costura y las 4 tablas de
  tuning de record.
- **Tasks:** fases de artefacto + trazabilidad + cierre. Sin ciclo RED/GREEN de este cambio: el
  codigo y las pruebas ya existen y estan en verde.

## Affected Areas

| Area | Impacto | Descripcion |
|---|---|---|
| `openspec/changes/2026-09-03-formalizar-perfiles-m5/` | Nuevo | proposal, design, tasks, spec delta |
| `openspec/specs/perfiles-personalidad-m5/` | Nuevo (al archivar) | spec estandar tras `sdd-archive` |
| `Data/Personalities/*.asset` (4) | Ya escrito | Solo queda documentado y trazado |
| `Tests/EditMode/Receptivity/PersonalityProfilesDataTests.cs` | Ya escrito | Solo queda referenciado por la trazabilidad |
| `Runtime/` | Sin cambio | El esquema de M4 no se toca |

## Gobernanza

**No es un cambio de contrato** (CLAUDE.md regla 2): no toca `Runtime/Core/` ni
`Runtime/CoreChannels/`. No aplica ventana fija ni co-aprobacion de todos los duenos. Se integra
como cambio propio con auto-merge y checklist, dueno de M5: Jefferson Estiven Aristizabal Quiceno.

**Una sola unidad de trabajo.** El diff toca `Data/Personalities/` (el modulo M5), su prueba de
aceptacion en `Tests/EditMode/Receptivity/` (que reusa el ensamblado de prueba de M4, no crea un
segundo modulo de `Runtime/`) y `openspec/`. La regla dura de "un cambio, un modulo" apunta a no
mezclar dos carpetas de `Runtime/`; aca no hay ninguna.

## Risks

| Riesgo | Prob. | Mitigacion |
|---|---|---|
| La spec se desincroniza si los `.asset` se editan despues sin actualizar el design | Media | La enforcement son las pruebas: `PersonalityProfilesDataTests` cae si un `.asset` rompe un signo o la coherencia de umbrales. El design fija los numeros "al momento de creacion"; el tuning fino es dato declarado como no-contrato |
| Unity no corre en fases de agente; el verde es compuerta humana | Alta | Aceptacion por Test Runner; el usuario ya confirmo verde de las 8 pruebas |
| Confundir "dato de M5" con "cambio de M4" al tunear un perfil | Media | La spec fija que editar un `.asset` NO ES cambio de contrato y NO toca ninguna clase; `receptividad-m4` no recibe numeros por personalidad |
| Los `.asset` siguen sin trackear en git (`??`) | Baja | Tarea del autor: `git add Data/Personalities/ Tests/EditMode/Receptivity/PersonalityProfilesDataTests.cs` junto con los artefactos OpenSpec |

## Presupuesto de revision

El codigo y las pruebas ya estan escritos y en verde. Este cambio agrega ~4 archivos Markdown
bajo `openspec/`; los 4 `.asset` (~1 KB cada uno) y la prueba (~170 lineas) entran al PR pero ya
estaban hechos. Muy por debajo del presupuesto de 400 lineas. **Un unico PR, sin encadenar**
(`delivery_strategy: ask-on-risk`, el pronostico no detecta riesgo).

## Rollback Plan

`git revert` del commit de este cambio elimina los 4 artefactos OpenSpec y, si van en el mismo
commit, los 4 `.asset` y la prueba. Ningun modulo consumidor necesita accion: `ReceptivityEngine`
sigue usando `Standard()` (numeros en codigo), no los `.asset`. Si el cambio ya fue archivado,
revertir tambien saca `openspec/specs/perfiles-personalidad-m5/`.

## Dependencies

- `receptividad-m4` archivado y estable (lo esta: merge `1515fa4`). Define `ReceptivityProfileAsset`,
  `ToProfile()`, `BuildCatalog(...)` y `ReceptivityProfile.Default`.
- `contrato-nucleo-m0` archivado y estable: define `PersonalityId` y los enums `Intent`, `Tone`,
  `PhysicalAction`.
- Unity 6 disponible para correr EditMode (validacion humana; ya cubierta: el usuario confirmo
  las 8 pruebas en verde el 2026-09-03).

## Success Criteria

- [ ] Existen y estan versionados en git los 4 artefactos del cambio.
- [ ] Cada requisito de `perfiles-personalidad-m5/spec.md` traza a al menos una prueba EditMode en
      verde (`PersonalityProfilesDataTests` o una prueba de M4 ya en verde).
- [ ] El spec delta se fusiona limpio en `openspec/specs/perfiles-personalidad-m5/spec.md` al
      archivar (capacidad nueva, sin spec previa que reconciliar).
- [ ] El diff no toca ninguna ruta de `Runtime/` ni redefine nada de `receptividad-m4` o
      `contrato-nucleo-m0`.
- [ ] Los 4 `.asset` y la prueba quedan trackeados en git.

## Decisiones del usuario (2026-09-03)

1. **Ruta: implementacion directa + formalizacion OpenSpec retroactiva**, igual que M4. Los 4
   `.asset` los creo el usuario en Unity; el agente escribio la prueba de carga y ahora los 4
   artefactos OpenSpec. Sin ciclo CRITICO/remediacion: no hay codigo nuevo.
2. **Numeros 1:1 desde `ReceptivityProfileCatalog.Standard()`.** El tuning fino espera datos de
   uso; por ahora los `.asset` reproducen los perfiles placeholder de M4.
3. **El tuning por personalidad se documenta en M5, no en M4.** Cierra la *Open Question* del
   design de `receptividad-m4` con la preferencia que ya estaba anotada: los invariantes
   estructurales quedan en `receptividad-m4`; los numeros concretos, aca.
4. **RDD sigue apagado.** El receipt `--projection staged` se maneja aparte; este cambio no lo
   activa.
