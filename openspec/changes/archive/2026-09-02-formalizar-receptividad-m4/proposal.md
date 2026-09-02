# Propuesta: Formalizar M4 — Motor de receptividad (Runtime/Receptivity)

## Intent

El motor real de M4 ya existe, compila y esta en verde: `ReceptivityEngine : IReceptivityEngine`,
el contenedor de datos `ReceptivityProfile`, el catalogo `ReceptivityProfileCatalog`, el
adaptador `ReceptivityProfileAsset` en el sub-ensamblado `NpcAi.Receptivity.Unity`, y el doble
`ScriptedReceptivityEngine`. Alrededor de 105 pruebas EditMode cubren la tabla de transiciones,
los perfiles, el catalogo, el adaptador, la paridad de vocabulario del doble y el contrato de
puerto heredado de M0.

Lo que falta es su especificacion versionada en OpenSpec (spec, design, tasks), a la par de lo
que M0 ya tiene en `openspec/specs/contrato-nucleo-m0/`. Mientras eso no exista, el
comportamiento de M4 solo vive en el codigo y en las memorias de Engram: cualquier consumidor
(M6, M8) o el tuning de M5 puede apoyarse en supuestos que ninguna spec fija. Esta propuesta
**no reescribe** M4: lo documenta y lo congela. Cierra el item 1 del *Definition of Done* del
modulo (README): "`spec.md`, `design.md` y `tasks.md` archivados en OpenSpec y versionados en git".

## Scope

### In Scope

- Especificacion OpenSpec versionada de M4: nueva capacidad `receptividad-m4` (spec delta
  `ADDED`), design y tasks de este cambio.
- **Frontera dura de escritura (una sola ruta):** `openspec/changes/2026-09-02-formalizar-receptividad-m4/`
  y, al archivar, `openspec/specs/receptividad-m4/`.
- Tabla de trazabilidad: cada requisito de la spec apunta a la prueba EditMode ya en verde que
  lo demuestra.

### Out of Scope

- Cualquier cambio de codigo en `Runtime/Receptivity/` o `Tests/EditMode/Receptivity/`. M4 ya
  esta implementado; este cambio agrega cero lineas de runtime y cero pruebas nuevas.
- Cualquier otro modulo.
- Los `.asset` reales de personalidad: son dato de M5 (`Data/Personalities/`), cambio aparte.
  `ReceptivityProfileCatalog.Standard()` sigue con sus 4 personalidades placeholder.
- El contrato del puerto `IReceptivityEngine`: lo posee `contrato-nucleo-m0` y NO se toca. Esta
  spec documenta como M4 lo **realiza y extiende**, no lo redefine.
- El receipt de revision con `--projection staged` (item 4 del DoD): RDD esta apagado por
  defecto; encenderlo o aceptar politica de repo normal es decision del usuario, fuera de este
  cambio.
- El merge del PR (item 5 del DoD): lo hace el autor (regla 8).

## Capabilities

### New Capabilities

- `receptividad-m4`: invariantes del motor real de M4 — puntaje como unica fuente de verdad,
  blindaje de monotonia, saturacion simetrica, modulacion por tono, perfil como contenedor de
  datos, catalogo inyectable como costura para M5, vocabulario compartido de `ReasonCode`,
  aislamiento del adaptador ScriptableObject y paridad del doble.

### Modified Capabilities

- Ninguna. `contrato-nucleo-m0` ya fija el puerto `IReceptivityEngine` y NO se modifica;
  `receptividad-m4` lo complementa.

## Approach

"Documentar y fijar lo que existe", igual que la formalizacion de M0 (`enfoque A`), pero sin
ningun cambio de runtime ni de pruebas:

- **Spec:** captura los invariantes que hoy ya exigen `ReceptivityEngineTests` (10),
  `ReceptivityProfileTests` (4), `ReceptivityProfileCatalogTests` (8),
  `ReceptivityProfileAssetTests` (7), `RazonParityTests` (56) y el contrato heredado
  `ReceptivityEngineContract` (9). Escenarios en `Dado/Cuando/Entonces`, niveles `DEBE/NO DEBE/
  DEBERIA/PUEDE`.
- **Design:** registra retroactivamente las decisiones de arquitectura de M4 (AD1-AD11), el
  grafo de referencias confirmado y el flujo de datos runtime + la costura de M5.
- **Tasks:** fases de artefacto + trazabilidad + cierre del DoD. Sin ciclo RED/GREEN: no hay
  codigo.

## Affected Areas

| Area | Impacto | Descripcion |
|---|---|---|
| `openspec/changes/2026-09-02-formalizar-receptividad-m4/` | Nuevo | proposal, design, tasks, spec delta |
| `openspec/specs/receptividad-m4/` | Nuevo (al archivar) | spec estandar tras `sdd-archive` |
| `Runtime/Receptivity/` | Sin cambio | Solo queda documentado |
| `Tests/EditMode/Receptivity/` | Sin cambio | Solo queda referenciado por la trazabilidad |

## Gobernanza

**No es un cambio de contrato** (CLAUDE.md regla 2): no toca `Runtime/Core/` ni
`Runtime/CoreChannels/`. No aplica ventana fija ni co-aprobacion de todos los duenos. Se integra
como cambio propio con auto-merge y checklist (commit `57090d6`), dueno de M4:
Jefferson Estiven Aristizabal Quiceno.

## Risks

| Riesgo | Prob. | Mitigacion |
|---|---|---|
| La spec se desincroniza si M4 cambia despues sin actualizarla | Media | La enforcement son las pruebas, no la prosa: la tabla de trazabilidad ata cada requisito a un `[Test]` concreto |
| Unity no corre en fases de agente; el verde es compuerta humana | Alta | Aceptacion por Test Runner; el usuario ya confirmo verde tras el paso 6 |
| Duplicar en `receptividad-m4` invariantes que ya posee `contrato-nucleo-m0` | Media | La spec de M4 solo documenta lo especifico de la implementacion real (perfil, blindaje, saturacion, tono, vocabulario, adaptador); el puerto se referencia, no se copia |

## Presupuesto de revision

Solo documentacion: ~4 archivos Markdown, cero runtime, cero pruebas. Muy por debajo del
presupuesto de 400 lineas. **Un unico PR, sin encadenar** (`delivery_strategy: ask-on-risk`, el
pronostico no detecta riesgo).

## Rollback Plan

`git revert` del commit de este cambio elimina los cuatro artefactos. Como no hay cambio de
runtime ni de pruebas, ningun modulo consumidor necesita accion. Si el cambio ya fue archivado,
revertir tambien saca `openspec/specs/receptividad-m4/` y devuelve el arbol de specs a su estado
previo.

## Dependencies

- Unity 6 disponible para correr EditMode (validacion humana; ya cubierta por los pasos 1-6 de
  la implementacion directa de M4).
- `contrato-nucleo-m0` archivado y estable (lo esta: commits `cc4da1e` / `02e8973`).

## Success Criteria

- [ ] Existen y estan versionados en git los 4 artefactos del cambio.
- [ ] Cada requisito de `receptividad-m4/spec.md` traza a al menos una prueba EditMode en verde.
- [ ] El spec delta se fusiona limpio en `openspec/specs/receptividad-m4/spec.md` al archivar.
- [ ] El diff no toca ninguna ruta fuera de `openspec/`.
- [ ] `receptividad-m4` no redefine ningun invariante que ya posea `contrato-nucleo-m0`.

## Decisiones del usuario (2026-09-02)

1. **Ruta: cambio retroactivo, sin loop de verify.** Se crea la carpeta de cambio con los 4
   artefactos que documentan el M4 ya construido y en verde. `sdd-verify` solo chequea que la
   spec case con el codigo existente; sin ciclo CRITICO/remediacion porque no hay codigo nuevo.
2. **RDD sigue apagado.** El item 4 del DoD (receipt `--projection staged`) se maneja aparte:
   el usuario decide si enciende `gentle-ai review mode enable` o entrega bajo politica de repo
   normal. Este cambio no lo activa.
3. **Frontera de capacidades.** `receptividad-m4` documenta la realizacion de M4;
   `contrato-nucleo-m0` sigue siendo el dueno de los invariantes del puerto `IReceptivityEngine`.
