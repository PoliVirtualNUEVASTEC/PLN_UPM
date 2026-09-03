# Tasks: Formalizar M5 — Perfiles de personalidad

## Review Workload Forecast

| Field | Value |
|-------|-------|
| Estimated changed lines | ~4 archivos Markdown bajo `openspec/`; 4 `.asset` (~1 KB c/u) y 1 prueba (~170 lineas) ya escritos |
| 400-line budget risk | None |
| Chained PRs recommended | No |
| Suggested split | UN PR |
| Delivery strategy | ask-on-risk (sin riesgo -> un PR) |
| Chain strategy | N/A |

Decision needed before apply: No
Chained PRs recommended: No
400-line budget risk: None

Entrega: un unico PR. El codigo (los `.asset`) y la prueba ya existen y estan en verde; este
cambio agrega solo los 4 artefactos OpenSpec.

## Phase 0: Guardrails (leer antes de escribir)

- [x] 0.1 Frontera de escritura: SOLO `openspec/changes/2026-09-03-formalizar-perfiles-m5/` y, al
  archivar, `openspec/specs/perfiles-personalidad-m5/`. Cualquier edicion en `Runtime/` esta
  fuera de alcance: parar y avisar.
- [x] 0.2 Cero cambio de codigo. El esquema (`ReceptivityProfileAsset`, `ReceptivityProfile`,
  `ReceptivityProfileCatalog`) lo posee `receptividad-m4` y NO se toca. M5 no escribe clases.
- [x] 0.3 `perfiles-personalidad-m5` NO redefine el esquema del perfil ni los invariantes del
  motor: eso es de `receptividad-m4`. Documenta los **datos** que ese esquema consume.
- [x] 0.4 NO es cambio de contrato (no toca `Runtime/Core/` ni `Runtime/CoreChannels/`): sin
  ventana fija ni co-aprobacion de todos los duenos. Cambio propio del dueno de M5, auto-merge
  con checklist.
- [x] 0.5 Una sola unidad de trabajo: el diff toca `Data/Personalities/` (M5), la prueba de
  aceptacion en `Tests/EditMode/Receptivity/` (reusa el asmdef de M4, no crea un segundo modulo
  de `Runtime/`) y `openspec/`. No hay dos carpetas de `Runtime/`.

## Phase 1: Artefactos del cambio

- [x] 1.1 `proposal.md`: intent (formalizar M5 y cerrar la Open Question de `receptividad-m4`),
  scope in/out, capacidad nueva `perfiles-personalidad-m5`, approach "documentar y fijar",
  riesgos, criterios de exito, decisiones del usuario 2026-09-03.
- [x] 1.2 `design.md`: enfoque, unidad de modulo (M5 = datos, sin asmdef) y grafo de referencias,
  decisiones AD1-AD9, flujo de datos de la costura + runtime actual con `Standard()`, tablas
  "Tuning de record" de las 4 personalidades, inventario de archivos, superficie publica
  consumida, estrategia de pruebas (las 8 nuevas + las de M4 en verde), threat matrix N/A,
  rollback.
- [x] 1.3 `specs/perfiles-personalidad-m5/spec.md`: `## ADDED Requirements` con 7 requisitos
  (`DEBE/NO DEBE/DEBERIA/PUEDE`), escenarios `Dado/Cuando/Entonces` y tabla de trazabilidad
  requisito -> prueba.
- [x] 1.4 `tasks.md`: este archivo.

## Phase 2: Trazabilidad (sin codigo)

- [x] 2.1 Cada uno de los 7 requisitos de `perfiles-personalidad-m5/spec.md` apunta a al menos
  una prueba EditMode concreta ya en verde (`PersonalityProfilesDataTests`, 8 pruebas, o una
  prueba de M4 ya en verde). Sin pruebas nuevas en este cambio. (Tabla al final de la spec.)
- [x] 2.2 Confirmar que ningun requisito duplica un invariante que ya fija `receptividad-m4`: los
  escenarios de M5 que tocan `ToProfile()`, `BuildCatalog`, `Default` y la monotonia se citan
  como **heredados** de `receptividad-m4`; los requisitos nuevos son sobre los datos (alcance,
  signos, coherencia de umbrales, extremos distinguibles, tuning como dato).
- [x] 2.3 Confirmar que la tabla "Tuning de record" del design coincide 1:1 con los 4 `.asset` y
  con `ReceptivityProfileCatalog.Standard()` (verificado campo a campo el 2026-09-03).

## Phase 3: Verificacion ligera (compuerta humana)

- [x] 3.1 El usuario confirma verde EditMode del ensamblado `NpcAi.Receptivity.Tests` en Unity 6
  (Test Runner > EditMode > Run All), con las 8 pruebas de `PersonalityProfilesDataTests`
  incluidas. Confirmado por el usuario el 2026-09-03 ("ya corrieron y todos los test estan en
  verde").
- [x] 3.2 `sdd-verify` en modo ligero: `verify-report.md` escrito, verdict **PASS**. 7/7
  requisitos y 10/10 escenarios trazan a prueba en verde (el escenario 7 combina 2 pruebas
  verdes + inspeccion de la equivalencia numerica). Sin ciclo CRITICO/remediacion: no hay codigo
  nuevo. 0 CRITICAL, 0 WARNING.
- [x] 3.3 Validacion estructural OpenSpec: `gentle-ai sdd-status 2026-09-03-formalizar-perfiles-m5
  --json` devuelve `proposal/specs/design/tasks = done`, `nextRecommended = apply`,
  `blockedReasons = []` (parseo limpio; el `apply` recomendado se salta porque el cambio es
  retroactivo, sin codigo — mismo criterio que M4).

## Phase 4: Cierre (acciones del autor)

- [ ] 4.1 `git add openspec/changes/2026-09-03-formalizar-perfiles-m5/ Data/Personalities/
  Tests/EditMode/Receptivity/PersonalityProfilesDataTests.cs` (y sus `.meta`) y `git diff --cached`
  antes de nada; confirmar que el diff no toca ninguna ruta de `Runtime/`.
- [ ] 4.2 Item del receipt `--projection staged`: decidir explicitamente entre encender RDD
  (`gentle-ai review mode enable`) y generar el receipt, o entregar bajo politica de repo normal
  con RDD apagado. Este cambio no lo activa.
- [ ] 4.3 Checklist "Antes de mergear" del README (pruebas propias en verde, diff solo en las
  carpetas del autor, spec/design/tasks archivados, rama al dia con `main`, decisiones
  registradas).
- [x] 4.4 `sdd-archive`: `specs/perfiles-personalidad-m5/spec.md` copiado byte-identico a
  `openspec/specs/perfiles-personalidad-m5/spec.md` (capacidad nueva, `diff` vacio), carpeta
  movida a `openspec/changes/archive/2026-09-03-formalizar-perfiles-m5/`, `archive-report.md`
  escrito. Hecho por el orquestador inline (mismo criterio que M4: contexto cargado, solo docs).
- [ ] 4.5 PR abierto y mergeado a `main` por el autor (Jefferson Estiven Aristizabal Quiceno),
  regla 8.
- [ ] 4.6 Decisiones relevantes registradas en Engram y en el `archive-report.md` del cambio.

## Notas

- Ningun agente ejecuta Unity: cada verde es compuerta humana.
- Este cambio no tiene ciclo TDD: el codigo (los `.asset`) y las 8 pruebas ya existen. Las fases
  1 y 2 quedan hechas al escribir los artefactos; 3.2-3.3 son del agente; la fase 4 es del autor.
- Gotcha registrado en el design (AD8): Unity trae NUnit 3.5; `Is.AnyOf` (3.6+) no compila.
