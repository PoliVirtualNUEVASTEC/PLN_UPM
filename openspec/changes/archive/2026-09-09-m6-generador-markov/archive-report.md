# Archive Report: m6-generador-markov

**Change**: 2026-09-09-m6-generador-markov
**Archived**: 2026-09-10
**Status**: PASS - ARCHIVED
**Artifact Store Mode**: hybrid (OpenSpec filesystem + Engram)

## Executive Summary

El cambio `2026-09-09-m6-generador-markov` queda archivado. Entrega el generador de dialogo real
de M6 por cadenas de Markov, implementado de forma directa sobre el `proposal.md`/`design.md` ya
planificados:

- `Runtime/Dialogue/MarkovChainBuilder.cs` — cadena de bigramas (orden 2), C# puro.
- `Runtime/Dialogue/MarkovDialogueGenerator.cs` — `IDialogueGenerator` real: indexa el corpus
  semilla por `(PersonalityId, Receptivity)`, paseo aleatorio con respaldo escalonado (reintento
  acotado -> frase semilla verbatim -> frase fija) para nunca devolver texto vacio.
- `Data/Dialogue/` — corpus semilla nuevo: README + 4 `.json` (`grosero`, `histerico`,
  `introvertido`, `empatico`), 3 listas por estado de `Receptivity`, 6 frases por bloque.
- `Tests/EditMode/Dialogue/` — 14 pruebas EditMode nuevas (`MarkovChainBuilderTests` 6,
  `MarkovDialogueGeneratorTests : DialogueGeneratorContract` 8).

El usuario confirmo **19 pruebas en verde** en `NpcAi.Dialogue.Tests` (14 nuevas + 5 del contrato
heredado que ya pasaba `ScriptedDialogueGenerator`). La spec delta `generador-dialogo-m6` (7
requisitos, 14 escenarios) se copio byte-identica a `openspec/specs/generador-dialogo-m6/spec.md`
— capacidad nueva, sin spec previa que reconciliar. La carpeta del cambio se movio a
`openspec/changes/archive/2026-09-09-m6-generador-markov/`. Verificacion: **PASS** (0 CRITICO, 0
blockers). **No es cambio de contrato**: `IDialogueGenerator`, `NpcReply` y
`ScriptedDialogueGenerator` quedan intactos.

## Final-State Authority Ranking

1. **Native review authority** — Sin review gate; RDD apagado por defecto (Opcion B, sin receipt).
2. **Persisted tasks artifact** — `tasks.md`: `[x]` 0.1-0.6, 1.1-1.3, 2.1-2.4, 2.6, 3.1-3.3.
   `[~]` 2.5 (cubierto por la prueba de volumen NUnit). Abiertos: 1.4 (revision cruzada del
   corpus, accion del equipo), 4.1-4.2 y 4.4 (acciones del autor, pre-merge no pre-archive).
3. **Explicit final-state facts** — verify-report verdict PASS; verde EditMode humano de las 19
   pruebas de `NpcAi.Dialogue.Tests`, atestiguado por el usuario el 2026-09-10.
4. **Intermediate snapshots** — verify-report.md (este cambio no tiene apply-progress:
   implementacion directa sin ciclo `sdd-apply`).

## Artifact Inventory

### Merged Specs

| Domain | File Path | Status | Details |
|--------|-----------|--------|---------|
| generador-dialogo-m6 | `openspec/specs/generador-dialogo-m6/spec.md` | Created | 7 requisitos, 14 escenarios; capacidad nueva (sin spec previa que reconciliar) |

**Merge Strategy**: Capacidad nueva (Strategy B). `cp` mecanico + `diff` vacio (byte-identico).
Convive con `contrato-nucleo-m0`, `receptividad-m4`, `perfiles-personalidad-m5` y el resto; NO
redefine `IDialogueGenerator` ni `NpcReply` (de `contrato-nucleo-m0`).

**Verificacion de copia**: `diff` (origen vs destino) sin diferencias.

### Archived Change Folder

**Location**: `openspec/changes/archive/2026-09-09-m6-generador-markov/`

**Contents**:
- `proposal.md` — intent (cerrar el estado "solo doble" de M6), scope in/out, capacidad nueva,
  approach Markov vs modelo entrenado, riesgos, decisiones del usuario 2026-09-08/09.
- `design.md` — tecnica Markov orden 2, decisiones AD1-AD7, data flow, file inventory, interfaces,
  testing strategy, open questions.
- `tasks.md` — 4 fases; 0-3 marcadas, 4 (entrega) parcialmente.
- `verify-report.md` — verdict PASS; 7/7 requisitos, 14/14 escenarios; 0 CRITICO, 0 WARNING; 0
  hallazgos corregidos.
- `archive-report.md` — este documento.
- `specs/generador-dialogo-m6/spec.md` — 7 requisitos ADDED + tabla de trazabilidad.

**Verificacion de movimiento**: carpeta origen `openspec/changes/2026-09-09-m6-generador-markov/`
confirmada inexistente tras el `mv`.

## Verification Outcome (Final State)

**Verdict**: PASS

| Metric | Count |
|--------|-------|
| Total Requirements | 7 |
| Covered Requirements | 7/7 |
| Total Scenarios | 14 |
| Passing Scenarios | 14/14 |
| Critical Findings | 0 |
| Blockers | 0 |

3 escenarios combinan una prueba EditMode en verde con verificacion por inspeccion (tabla de
etiquetas por receptividad; esquema y minimo del corpus semilla). El resto traza a una prueba
automatica.

### Test Execution

**Runtime GREEN** (compuerta humana, autoridad de estado final):
- El usuario corrio EditMode en Unity 6 el 2026-09-10 ("muestra 19", todas en verde).
- Ensamblado `NpcAi.Dialogue.Tests`: 6 `MarkovChainBuilderTests` + 8 `MarkovDialogueGeneratorTests`
  (5 heredadas de `DialogueGeneratorContract` + 3 propias) + 5 `ScriptedDialogueGeneratorTests`
  preexistentes = 19.
- Implementacion directa, RDD off: no hay `sdd-attempt` que consumir.

**Agent-side**:
- `git diff --cached --stat`: el diff solo toca `Data/Dialogue/`, `Runtime/Dialogue/Markov*.cs`,
  `Tests/EditMode/Dialogue/Markov*.cs`, `Docs/MODULES.md` y
  `openspec/changes/2026-09-09-m6-generador-markov/`. Cero `Runtime/Core/`.
- `codegraph_explore`: `IDialogueGenerator`/`NpcReply` sin cambio de firma;
  `ScriptedDialogueGenerator` sin tocar.
- `python json.load` sobre los 4 `.json`: validos, 6 frases por bloque, 12 bloques.

### Findings

**CRITICAL**: Ninguno. **WARNING**: Ninguno.

**SUGGESTION** (no bloqueante, del verify-report):
1. `design.md` "Unidad de modulo": corregir `noEngineReferences: true` -> `false` (el asmdef real
   permite `UnityEngine`; el generador usa `TextAsset`/`JsonUtility`).
2. Corpus semilla chico: ampliar `Data/Dialogue/` es iteracion de datos, no reabre M6.
3. Condicionar la cadena por `Intent` es mejora de seguimiento (proposal -> Out of Scope).
4. Script manual de 1000 llamadas (task 2.5): opcional; cubierto en espiritu por la prueba de
   volumen NUnit de 3000 llamadas.

## Runtime Changes

**Nuevos** (modulo M6, `Runtime/Dialogue/`):
- `MarkovChainBuilder` — helper interno, C# puro, sin `UnityEngine`.
- `MarkovDialogueGenerator : IDialogueGenerator` — usa `UnityEngine` (`TextAsset`, `JsonUtility`);
  no toca `AssetDatabase` (el llamador carga los assets).

**Sin cambio**: `IDialogueGenerator`, `NpcReply` (`Runtime/Core/`, capacidad
`contrato-nucleo-m0`); `Runtime/Dialogue/Fakes/ScriptedDialogueGenerator.cs` (doble de
referencia). **No es cambio de contrato**: sin gate de gobernanza previo.

**Write boundary**: `Data/Dialogue/` + `Runtime/Dialogue/Markov{ChainBuilder,DialogueGenerator}.cs`
+ `Tests/EditMode/Dialogue/Markov*Tests.cs` + `Docs/MODULES.md` +
`openspec/changes/2026-09-09-m6-generador-markov/`. Cero `Runtime/Core/`.

## Task Completion Gate

**Status**: PASS

| Fase | Checkboxes | Estado |
|------|-----------|--------|
| 0. Guardrails | 0.1-0.6 | `[x]` frontera confirmada; no es cambio de contrato |
| 1. Corpus (PR1) | 1.1-1.3 | `[x]` — 1.4 (revision cruzada) abierta, accion del equipo |
| 2. Codigo + tests (PR2) | 2.1-2.4, 2.6 | `[x]`; 2.5 `[~]` cubierto por la prueba de volumen |
| 3. Spec + docs (PR3) | 3.1-3.3 | `[x]` spec delta + MODULES.md + trazabilidad |
| 4. Cierre | 4.3 | `[x]` `sdd-archive` inline (merge + move + report) |
| 4. Cierre | 4.1-4.2, 4.4 | `[ ]` acciones del autor: `git add`/`git diff --cached`, checklist "Antes de mergear", PR + self-merge |

## Archiving Decisions

- **Merge Strategy B** (capacidad nueva): `cp` mecanico + `diff` vacio. No reemplaza nada.
- **Nombre de carpeta**: formato ISO `2026-09-09-m6-generador-markov`.
- **Integridad**: copia de spec `diff` vacio -> PASS; movimiento de carpeta, origen inexistente -> PASS.

## Relacion con otros cambios

- **Cierra**: el estado "solo doble" de M6 documentado en `Docs/MODULES.md`. `Runtime/Dialogue/`
  pasa de solo `Fakes/ScriptedDialogueGenerator.cs` a tener un motor real.
- **Depende de**: `contrato-nucleo-m0` (`IDialogueGenerator`, `NpcReply`, `PersonalityId`,
  `Receptivity`) y `perfiles-personalidad-m5` (los 4 ids `grosero`/`histerico`/`introvertido`/
  `empatico` que indexan el corpus). No modifica ninguna de las dos.
- **Habilita**: M11 (Harness) puede instanciar `MarkovDialogueGenerator` en una escena de
  composicion real cuando exista. Hoy nada lo instancia automaticamente (task 2.6).

## Next Steps

### Cierre del DoD del modulo M6 (acciones del autor — Jefferson Estiven Aristizabal Quiceno)

1. `git add` de las rutas del cambio (+ `.meta`) y `git diff --cached`; confirmar que el diff no
   sale de `Data/Dialogue/`, `Runtime/Dialogue/`, `Tests/EditMode/Dialogue/`, `Docs/`, `openspec/`.
2. Entrega bajo politica de repo normal con RDD apagado (Opcion B, sin receipt).
3. Checklist "Antes de mergear" del README.
4. PR abierto y mergeado a `main` por el autor (regla 8). Rama: `feat/m6-generador-markov`.
   El PR pasa de presupuesto de 400 lineas -> `size:exception` aceptado explicitamente por el
   autor (entrega en un solo PR, como M4 y M5).
5. Decisiones registradas en Engram (hecho en esta sesion) y en este `archive-report.md`.

### Residual

- **1.4**: un segundo integrante del equipo revisa el tono del corpus semilla (que cada bloque
  sea reconociblemente distinto entre personalidades y estados).
- Ampliar `Data/Dialogue/` si el texto generado se siente repetitivo: edicion de datos, cambio
  propio de M6, sin reabrir este.
- `design.md` "Unidad de modulo": corregir la frase `noEngineReferences: true`.
- Condicionar la cadena por `Intent`: mejora de seguimiento.

## Conclusion

**El cambio 2026-09-09-m6-generador-markov esta archivado y el ciclo SDD esta completo.**
M6 queda con un generador de dialogo real por cadenas de Markov, trazado a 14 pruebas EditMode
nuevas en verde. `openspec/specs/generador-dialogo-m6/spec.md` es ahora la fuente de verdad del
comportamiento observable de M6. Queda pendiente el cierre manual del DoD del modulo (git add,
checklist, PR + self-merge) por parte del autor, mas la revision cruzada del corpus (1.4).
