# Archive Report: respondedor-clinico-m15

**Change**: 2026-09-09-m15-respondedor-clinico
**Archived**: 2026-09-15
**Status**: PASS - ARCHIVED
**Artifact Store Mode**: filesystem OpenSpec (sin acceso de este agente a Engram/`gentle-ai`)

## Executive Summary

El cambio `2026-09-09-m15-respondedor-clinico` queda archivado. Entrega el modulo M15
(reasignado de Luis Miguel Cañaveral Restrepo a Nataly Álvarez el 2026-09-14) en 2 PR
encadenados, ambos dependientes de `2026-09-09-m0-puerto-respuesta-clinica` (mergeado antes)
y `2026-09-09-m14-catalogo-casos-clinicos` (mergeado el 2026-09-15, justo antes de arrancar
este cambio):

- **PR1** — nucleo, cargador, emparejador y doble: `Runtime/ClinicalResponse/{ClinicalCase,
  ClinicalCaseLoader,ClinicalFactMatcher}.cs` + doble `Fakes/ScriptedClinicalResponder.cs`.
  11 pruebas EditMode nuevas (`ClinicalFactMatcherTests`, `ClinicalCasesDataTests`,
  `ScriptedClinicalResponderTests`).
- **PR2** — adaptador real: `Runtime/ClinicalResponse/ClinicalResponder.cs`, implementacion
  real de `IClinicalResponder` con matiz de personalidad y `EmotionTag`/`AnimationCue` de
  tabla fija. 12 pruebas EditMode nuevas (`ClinicalResponderTests`, hereda
  `ClinicalResponderContract` completo mas 5 pruebas propias).

Total: **23 pruebas EditMode nuevas** en `NpcAi.ClinicalResponse.Tests`, confirmadas en verde
por el usuario despues de cada PR (419/419 en el Test Runner completo del proyecto al cierre
de PR2, 2026-09-15). La spec `respondedor-clinico-m15` (6 requisitos, 8 escenarios) se crea en
`openspec/specs/respondedor-clinico-m15/spec.md` — capacidad nueva que satisface
`respuesta-clinica-m0` sin modificarla. La carpeta del cambio se mueve a
`openspec/changes/archive/2026-09-09-m15-respondedor-clinico/`. Verificacion: **PASS**
(0 CRITICO, 0 blockers, 1 item residual diferido explicitamente: cableado en la escena real
de M11, que todavia no existe). **No es cambio de contrato**: `Runtime/Core/` y
`Runtime/CoreChannels/` quedan intactos en los 2 PR.

Una desviacion encontrada y corregida en el camino: durante el spike de deserializacion de
PR1 se detecto que `JsonUtility` no soporta `null` en un campo numerico, lo que rompia
`Data/Cases/caso-02.json` (`temperaturaC: 36.4` conviviendo con `null` en los otros 2 casos).
Se resolvio con un ajuste de datos de M14 (no de M15), en un PR aparte y minimo
(`fix/m14-temperaturac-string`, mergeado antes de PR1): `temperaturaC` pasa de `number|null`
a `string|null`, igual tratamiento que `taMmHg`/`glasgow`.

## Final-State Authority Ranking

1. **Native review authority** — Sin review gate propio; cada PR paso por revision de diff
   acotado (`git diff --cached --stat`) antes de commitear, siguiendo la regla del `README.md`.
2. **Persisted tasks artifact** — `tasks.md`: `[x]` 0.1-0.7, 1.1-1.10, 2.1-2.5. Fase 3
   (este cierre) se completa en este documento.
3. **Explicit final-state facts** — verde EditMode humano atestiguado por el usuario tras
   cada uno de los 2 PR (capturas de Test Runner: 23/23 en `NpcAi.ClinicalResponse.Tests.dll`,
   419/419 en el proyecto completo, 2026-09-15).
4. **Intermediate snapshots** — Ninguna (implementacion directa, sin `sdd-apply`/`apply-progress`).

## Artifact Inventory

### Merged Specs

| Domain | File Path | Status | Details |
|--------|-----------|--------|---------|
| respondedor-clinico-m15 | `openspec/specs/respondedor-clinico-m15/spec.md` | Created | 6 requisitos, 8 escenarios; capacidad nueva que satisface `respuesta-clinica-m0` sin modificarla |

**Merge Strategy**: Capacidad nueva (Strategy B). Convive con `contrato-nucleo-m0`,
`canales-evento-nucleo-m0` y `respuesta-clinica-m0`; NO redefine `IClinicalResponder`,
`ClinicalResponse`, `ClinicalCaseId` ni `ClinicalResponderContract`.

### Archived Change Folder

**Location**: `openspec/changes/archive/2026-09-09-m15-respondedor-clinico/`

**Contents**:
- `proposal.md` — intent, scope in/out, capacidad nueva, dependencias (M0 + M14), decisiones
  del usuario 2026-09-09 (enrutador, determinista, dueño Luis — luego reasignado a Nataly).
- `design.md` — unidad de modulo y grafo de referencias, decisiones AD1-AD8 (tecnica de
  respuesta, estilo de la respuesta, de donde salen los bytes, `ClinicalCase` sin `clave`,
  garantia sobre el dato, tags fijos, `Respond` sin `Receptivity`, empate del emparejador),
  Open Questions resueltas (JsonUtility elegido, umbral de cobertura de palabras).
- `tasks.md` — 5 fases; 0-2 marcadas completas, 3-4 (documentacion y cierre) esta entrega.
- `archive-report.md` — este documento.
- `specs/respondedor-clinico-m15/spec.md` — 6 requisitos ADDED + tabla de trazabilidad
  (copia identica de la promovida a `openspec/specs/`).

**Verificacion de movimiento**: carpeta origen `openspec/changes/2026-09-09-m15-respondedor-clinico/`
confirmada inexistente tras el `mv`.

## Verification Outcome (Final State)

**Verdict**: PASS

| Metric | Count |
|--------|-------|
| Total Requirements | 6 |
| Covered Requirements | 6/6 |
| Total Scenarios | 8 |
| Passing Scenarios | 8/8 (7 con prueba EditMode; 1 por inspeccion de codigo/tipo) |
| Critical Findings | 0 |
| Blockers | 0 |

### Test Execution

**Runtime GREEN** (compuerta humana, autoridad de estado final):
- Usuario corrio EditMode > Run All en Unity 6 tras cada uno de los 2 PR.
- Cierre de PR2 (2026-09-15): **419/419** en el Test Runner completo del proyecto,
  `NpcAi.ClinicalResponse.Tests.dll` con **23 pruebas** (7 `ClinicalFactMatcherTests` +
  6 `ClinicalCasesDataTests` + 9 `ScriptedClinicalResponderTests` [7 heredadas + 2 propias] +
  12 `ClinicalResponderTests` [7 heredadas + 5 propias]).

**Agent-side** (por PR, verificado por inspeccion manual):
- `git diff --cached --stat`: cada PR toca solo su propia frontera
  (`Runtime/ClinicalResponse/`, `Tests/EditMode/ClinicalResponse/`,
  `openspec/changes/2026-09-09-m15-respondedor-clinico/`). Cero `Runtime/Core/`, cero
  `Runtime/CoreChannels/`, cero `Data/Cases/*.json` (guardrail 0.7).
- GUID de `.meta`: cada archivo nuevo (`.cs`, `.asmdef`, carpetas) recibio un GUID generado
  a mano, sin reusar ninguno existente en el repo.
- Choque de nombres `ClinicalResponse` (namespace del modulo) vs. `NpcAi.Core.ClinicalResponse`
  (DTO): detectado por el compilador de Unity (`CS0118`/`CS0738`) al abrir el proyecto tras
  PR1, corregido calificando como `Core.ClinicalResponse` en todo el modulo — mismo patron
  que `Core.Receptivity` en M4. Aplicado desde el inicio en PR2.

### Findings

**CRITICAL**: Ninguno. **WARNING**: Ninguno.

**SUGGESTION** (no bloqueante):
1. Cablear `ClinicalResponder` en la escena real de M11 cuando esa escena exista (sigue sin
   existir al 2026-09-15) — de donde salen los bytes de `Data/Cases/` en el Quest
   (`Resources/` vs. ruta de paquete) y quien decide "turno clinico vs. social" son decisiones
   de M11, explicitamente fuera de alcance de M15.
2. El matiz de personalidad para `histerico` ("¡Ay, doctora! ") es una decision tomada durante
   esta entrega, sin discusion previa en `proposal.md` (que solo cubria grosero/empatico/
   introvertido). Documentado en `tasks.md` y `Docs/MODULES.md`; ajustarlo es un cambio de
   una linea si no encaja con el tono que el equipo quiere para esa personalidad.

## Runtime Changes

**Nuevos** (modulo M15):
- `Runtime/ClinicalResponse/` — `ClinicalCase`, `ClinicalCaseLoader`, `ClinicalFactMatcher`,
  `ClinicalResponder`; `Fakes/ScriptedClinicalResponder`.
- `Tests/EditMode/ClinicalResponse/` — 23 pruebas.

**Sin cambio**: `Runtime/Core/`, `Runtime/CoreChannels/` (ningun puerto ni DTO nuevo);
`IClinicalResponder`/`ClinicalResponse`/`ClinicalCaseId` (M15 es consumidor puro). **No es
cambio de contrato**: sin gate de gobernanza previo (regla 3 del README no aplica).

**Write boundary por PR**:
- PR1: `Runtime/ClinicalResponse/{ClinicalCase,ClinicalCaseLoader,ClinicalFactMatcher}.cs` +
  `Fakes/` + `Tests/EditMode/ClinicalResponse/` (3 archivos) + `design.md`/`tasks.md`.
- PR2: agrega `Runtime/ClinicalResponse/ClinicalResponder.cs` +
  `Tests/EditMode/ClinicalResponse/ClinicalResponderTests.cs` + `tasks.md`.

## Task Completion Gate

**Status**: PASS

| Fase | Checkboxes | Estado |
|------|-----------|--------|
| 0. Guardrails | 0.1-0.7 | `[x]` frontera confirmada; no es cambio de contrato |
| 1. Nucleo + doble (PR1) | 1.1-1.10 | `[x]` |
| 2. `ClinicalResponder` real (PR2) | 2.1-2.5 | `[x]` |
| 3. Documentacion | 3.1-3.2 | `[x]` `Docs/MODULES.md` actualizado con el estado real; diff revisado contra `Success Criteria` |
| 4. Cierre | 4.1, 4.2, 4.4 | `[x]` cumplidos en cada uno de los 2 PR (diff acotado, checklist, merge por el autor) |
| 4. Cierre | 4.3 | `[x]` este documento |

## Archiving Decisions

- **Merge Strategy B** (capacidad nueva): satisface `respuesta-clinica-m0` sin reconciliar
  contenido — ese contrato no cambia.
- **Nombre de carpeta**: se conserva `2026-09-09-m15-respondedor-clinico` (fecha de la
  propuesta original de Luis, consistente con el resto del historial de
  `openspec/changes/archive/`), aunque la implementacion la hizo Nataly tras la reasignacion
  del 2026-09-14.

## Relacion con otros cambios

- **Cierra**: el estado "diseño completo, cero implementacion" de M15 en `Docs/MODULES.md`.
- **Depende de**: `respuesta-clinica-m0` (puerto, DTO, contrato de pruebas) y
  `catalogo-casos-clinicos-m14` (esquema y los 3 `Data/Cases/caso-*.json`). No modifica
  ninguna de las dos — el unico ajuste necesario en M14 (`temperaturaC` a texto) se hizo como
  cambio de datos aparte, no desde este cambio.
- **Habilita**: M9 (`2026-09-09-m9-decision-triaje`) puede avanzar en paralelo (ya tenia M14
  como dependencia, no M15). M11 (`2026-09-09-m11-armado-sesion`) puede cablear
  `ClinicalResponder` en su escena real cuando exista, inyectandole el proveedor de bytes de
  `Data/Cases/` que decida para cada plataforma.

## Next Steps

### Residual (no bloqueante para este cierre)

1. Cablear `ClinicalResponder` en la escena real de M11 cuando esa escena exista.
2. Si el equipo revisa el matiz de "histerico" y no encaja, es un cambio de una linea en
   `ClinicalResponder.ConMatiz`.

### Cierre del DoD del modulo M15

Ya cumplido: los 2 PR se abrieron, revisaron (checklist "Antes de mergear") y mergearon a
`main` por la autora, uno por uno, el 2026-09-15.

## Conclusion

**El cambio 2026-09-09-m15-respondedor-clinico esta archivado y el ciclo SDD esta completo.**
M15 pasa de "diseño completo, cero implementacion" a un modulo real: el NPC responde con los
hechos del caso asignado, de forma determinista, sin poder filtrar nunca la clave de
evaluacion, y con un doble que deja a M4/M8/M11 integrar sin depender de `Data/Cases/`.
`openspec/specs/respondedor-clinico-m15/spec.md` es ahora la fuente de verdad del
comportamiento observable de M15. Queda 1 item residual explicitamente diferido (cableado en
la escena de M11), no bloqueante para este cierre.
