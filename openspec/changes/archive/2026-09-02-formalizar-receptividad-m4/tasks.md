# Tasks: Formalizar M4 — Motor de receptividad

## Review Workload Forecast

| Field | Value |
|-------|-------|
| Estimated changed lines | ~4 archivos Markdown bajo `openspec/`; 0 de runtime; 0 de pruebas |
| 400-line budget risk | None |
| Chained PRs recommended | No |
| Suggested split | UN PR de documentacion |
| Delivery strategy | ask-on-risk (sin riesgo -> un PR) |
| Chain strategy | N/A |

Decision needed before apply: No
Chained PRs recommended: No
400-line budget risk: None

Entrega: un unico PR de documentacion. No hay codigo, no hay decision de encadenamiento.

## Phase 0: Guardrails (leer antes de escribir)

- [x] 0.1 Frontera de escritura: SOLO `openspec/changes/2026-09-02-formalizar-receptividad-m4/`
  y, al archivar, `openspec/specs/receptividad-m4/`. Cualquier edicion en `Runtime/` o
  `Tests/` esta fuera de alcance: parar y avisar.
- [x] 0.2 Cero cambio de codigo y cero pruebas nuevas. M4 ya esta implementado y en verde
  (pasos 1-6 de la implementacion directa). Este cambio solo documenta.
- [x] 0.3 `receptividad-m4` NO redefine el puerto `IReceptivityEngine`: eso lo posee
  `contrato-nucleo-m0`. La spec de M4 solo documenta la realizacion (perfil, blindaje,
  saturacion, tono, vocabulario, adaptador, paridad del doble).
- [x] 0.4 NO es cambio de contrato (no toca `Runtime/Core/`): sin ventana fija ni
  co-aprobacion de todos los duenos. Cambio propio del dueno de M4, auto-merge con checklist.

## Phase 1: Artefactos del cambio

- [x] 1.1 `proposal.md`: intent (cerrar item 1 del DoD), scope in/out, capacidad nueva
  `receptividad-m4`, approach "documentar y fijar", riesgos, criterios de exito, decisiones
  del usuario 2026-09-02.
- [x] 1.2 `design.md`: enfoque, unidad de modulo y grafo de referencias confirmado
  (`NpcAi.Receptivity` -> `[NpcAi.Core]`, `noEngineReferences: true`), decisiones AD1-AD11,
  flujo de datos runtime + costura de M5, inventario de archivos existentes, superficie
  publica, estrategia de pruebas (las ~105 ya en verde), threat matrix N/A, rollback.
- [x] 1.3 `specs/receptividad-m4/spec.md`: `## ADDED Requirements` con 9 requisitos
  (`DEBE/NO DEBE/DEBERIA/PUEDE`), escenarios `Dado/Cuando/Entonces`, tabla del vocabulario de
  `ReasonCode` y tabla de trazabilidad requisito -> prueba.
- [x] 1.4 `tasks.md`: este archivo.

## Phase 2: Trazabilidad (sin codigo)

- [x] 2.1 Cada uno de los 9 requisitos de `receptividad-m4/spec.md` apunta a al menos una
  prueba EditMode concreta ya en verde. Sin pruebas nuevas. (Tabla al final de la spec.)
- [x] 2.2 Confirmar que ningun requisito duplica un invariante que ya fija
  `contrato-nucleo-m0` (Requirement "Puerto IReceptivityEngine"): los escenarios de M4 que
  tocan Neutral-hasta-Reset y monotonia se citan como **heredados** del contrato, no como
  requisitos nuevos de M4; los requisitos nuevos son los especificos de la implementacion.

## Phase 3: Verificacion ligera (compuerta humana)

- [x] 3.1 El usuario confirma verde EditMode del ensamblado `NpcAi.Receptivity.Tests` en
  Unity 6 (Test Runner > EditMode > Run All). Confirmado por el usuario tras el paso 6
  (~105 pruebas).
- [x] 3.2 `sdd-verify` en modo ligero: `verify-report.md` escrito, verdict **PASS**. 16/16
  escenarios y 9/9 requisitos trazan a prueba en verde. Una desviacion de firma
  (`BuildCatalog` estatico + recibe la coleccion) se detecto y corrigio en design/spec antes
  de archivar. Sin ciclo CRITICO/remediacion.
- [x] 3.3 Validacion estructural OpenSpec: no hay CLI `openspec` ni `gentle-ai openspec` en
  este entorno. `gentle-ai sdd-status 2026-09-02-formalizar-receptividad-m4 --json` devuelve
  `proposal/specs/design/tasks = done` (parseo limpio).

## Phase 4: Cierre del DoD del modulo (acciones del autor, fuera de este cambio)

- [ ] 4.1 `git add openspec/changes/2026-09-02-formalizar-receptividad-m4/` y
  `git diff --cached` antes de nada; confirmar que el diff no toca ninguna ruta fuera de
  `openspec/`.
- [ ] 4.2 Item 4 del DoD (receipt `--projection staged`): decidir explicitamente entre
  encender RDD (`gentle-ai review mode enable`) y generar el receipt, o entregar bajo politica
  de repo normal con RDD apagado. Este cambio no lo activa.
- [ ] 4.3 Checklist "Antes de mergear" del README completa (pruebas propias en verde, diff
  solo en las carpetas del autor, spec/design/tasks archivados, doble en paridad, rama al dia
  con `main`, decisiones registradas).
- [ ] 4.4 `sdd-archive`: fusiona `specs/receptividad-m4/spec.md` en
  `openspec/specs/receptividad-m4/spec.md`, mueve la carpeta a
  `openspec/changes/archive/2026-09-02-formalizar-receptividad-m4/` y escribe
  `archive-report.md`.
- [ ] 4.5 PR abierto y mergeado a `main` por el autor (Jefferson Estiven Aristizabal Quiceno),
  regla 8.
- [ ] 4.6 Decisiones relevantes registradas en Engram (ya: memoria "M4 paso 7 / formalizacion")
  y en el `archive-report.md` del cambio, que hace las veces de documento de contexto para
  esta formalizacion (igual que hizo M0).

## Notas

- Ningun agente ejecuta Unity: cada verde es compuerta humana.
- Este cambio no tiene ciclo TDD: no hay codigo. Las fases 1 y 2 quedan marcadas hechas al
  crear los artefactos; 3 y 4 dependen del usuario.
