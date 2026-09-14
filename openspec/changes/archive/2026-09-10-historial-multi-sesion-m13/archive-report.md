# Archive Report: historial-multi-sesion-m13

**Change**: 2026-09-10-historial-multi-sesion-m13
**Archived**: 2026-09-14
**Status**: PASS - ARCHIVED
**Artifact Store Mode**: filesystem OpenSpec (sin acceso de este agente a Engram/`gentle-ai`)

## Executive Summary

El cambio `2026-09-10-historial-multi-sesion-m13` queda archivado. Modifica la capacidad
`bitacora-sesion-m13` (ya archivada, PR1-PR3) en dos frentes, entregados en un solo PR:

1. **Historial multi-sesión**: `IniciarSesion(string etiqueta)` reemplaza a `IniciarSesion()`.
   Cada etiqueta persiste indefinidamente (ya no hay `DeleteAll` al iniciar una sesión nueva);
   `ListarSesiones()` y `ObtenerTurnosDeSesion(etiqueta)` permiten consultar el historial
   completo sin tocar SQL a mano.
2. **Reanudación automática tras un cierre no limpio** (ampliación decidida en vivo, motivada
   por una prueba real del usuario): `SessionLogBehaviour.OnEnable()` llama
   `SessionRecorder.ReanudarUltimaSesion()`, que retoma la última etiqueta **solo si quedó
   interrumpida** (nunca se llamó `FinalizarSesion()`), gracias a un flag `Cerrada` nuevo por
   sesión. Se corrigió de paso un bug: la numeración `Secuencia` ya no se reiniciaba a 0 al
   retomar/reusar una etiqueta con turnos existentes.

El usuario confirmó Test Runner EditMode en verde antes del PR, y validó manualmente con un
arnés de prueba (`Assets/_ArnesM13/`, fuera del paquete) los tres escenarios: multi-sesión sin
mezclar turnos, reanudación automática tras un "crash" simulado, y que una sesión cerrada
limpiamente **no** se retoma sola. `openspec/specs/bitacora-sesion-m13/spec.md` se actualizó
in-place (Modified Capability, no capacidad nueva): reemplaza el requisito "reinicia limpio" y
agrega 3 requisitos nuevos. Verificación: **PASS** (0 CRITICO, 0 blockers). **No es cambio de
contrato**: `Runtime/Core/` intacto.

## Final-State Authority Ranking

1. **Native review authority** — Sin review gate propio; diff acotado revisado antes de cada
   commit (regla 7 del README).
2. **Persisted tasks artifact** — `tasks.md`: `[x]` todas las fases (0-4, incluida la 3b de la
   ampliación). Sin residuales abiertos propios de este cambio.
3. **Explicit final-state facts** — Verde de Test Runner EditMode atestiguado por el usuario
   antes del PR (2026-09-14); validación manual de los 3 escenarios con el arnés de prueba.
4. **Intermediate snapshots** — Ninguna (implementación directa, sin `sdd-apply`).

## Artifact Inventory

### Merged Specs

| Domain | File Path | Status | Details |
|--------|-----------|--------|---------|
| bitacora-sesion-m13 | `openspec/specs/bitacora-sesion-m13/spec.md` | Modified | Reemplaza 1 requisito, agrega 3 nuevos (historial, consulta, reanudación automática) |

**Merge Strategy**: Capacidad existente modificada (Strategy A). Edición in-place del `spec.md`
canónico — no se generó un `spec.md` delta separado dentro de la carpeta del cambio, dado que
este agente no tiene acceso a la herramienta de merge formal (`gentle-ai`) y la edición directa
es equivalente en efecto.

### Archived Change Folder

**Location**: `openspec/changes/archive/2026-09-10-historial-multi-sesion-m13/`

**Contents**:
- `proposal.md` — intent (por qué se reemplaza "reinicia limpio"), scope in/out, decisiones del
  usuario (etiqueta manual, alcance con listado/consulta), ampliación de reanudación automática,
  riesgos.
- `design.md` — AD1-AD8 (forma de la etiqueta, normalización, reuso acumula, esquema SQLite,
  orden de listado, `ObtenerTurnos()` como azúcar sintáctico, reanudación automática, numeración
  de secuencia al retomar).
- `tasks.md` — Fases 0-4 más la 3b (ampliación), todas `[x]`.
- `archive-report.md` — este documento.

**Verificación de movimiento**: carpeta origen
`openspec/changes/2026-09-10-historial-multi-sesion-m13/` confirmada inexistente tras el `mv`.

## Verification Outcome (Final State)

**Verdict**: PASS

| Metric | Count |
|--------|-------|
| Total Requirements (nuevos/modificados en este cambio) | 4 |
| Covered Requirements | 4/4 |
| Critical Findings | 0 |
| Blockers | 0 |

### Test Execution

**Runtime GREEN** (compuerta humana, autoridad de estado final):
- Usuario corrió EditMode > Run All en Unity 6 antes de abrir el PR (2026-09-14), confirmado en
  verde.
- Validación manual adicional con el arnés de prueba: (a) dos etiquetas distintas no mezclan
  turnos y ambas aparecen en `ListarSesiones()`; (b) cerrar con "Finalizar Sesion" y reabrir deja
  el campo de etiqueta habilitado (no se retoma nada); (c) cerrar sin "Finalizar Sesion" (Stop
  directo) y reabrir retoma la misma etiqueta sola, mostrando `EtiquetaActiva` sin que el usuario
  la reescriba.

**Agent-side** (por inspección manual, sin `codegraph_explore`/Engram):
- `git diff --cached --stat`: el diff toca solo `Runtime/SessionLog/`,
  `Tests/EditMode/SessionLog/`, `openspec/specs/bitacora-sesion-m13/spec.md` y
  `openspec/changes/2026-09-10-historial-multi-sesion-m13/`. Cero `Runtime/Core/`.
- Grep de `IniciarSesion()` (sin argumento) sobre todo el repo tras el cambio: sin coincidencias
  en código `.cs` real (un caso se corrigió en `SessionExportTests.cs` tras un error de
  compilación reportado por el usuario, otro tras revisión propia).
- Lectura completa de los 6 archivos de producción modificados/nuevos y sus pruebas
  correspondientes, confirmando consistencia de la firma nueva en todos los call sites.

### Findings

**CRITICAL**: Ninguno. **WARNING**: Ninguno.

**SUGGESTION** (no bloqueante):
1. La base de datos ya usada durante las pruebas manuales acumuló sesiones de rondas anteriores
   sin el flag `Cerrada` (columna agregada en este cambio); las filas viejas migran a
   `Cerrada = false` por defecto de SQLite, lo que pudo causar una reanudación inesperada en la
   primera prueba tras el cambio. No es un defecto del código: es dato de prueba heredado. Se le
   indicó al usuario borrar `session-log.db` para validar limpio.
2. Sigue sin existir una UI real dentro de Unity para navegar el historial — la consulta hoy es
   por consola (arnés) o SQL directo (DB Browser). Fuera de alcance de este cambio (ver
   `proposal.md` -> Out of Scope).

## Runtime Changes

**Modificados** (módulo M13, `Runtime/SessionLog/`):
- `ISessionStore`, `SessionRecorder`, `SessionExport`, `InMemorySessionStore`,
  `SqliteSessionStore`, `SessionLogBehaviour`.

**Nuevos**: `SesionInfo.cs`, `EtiquetaSesion.cs`.

**Sin cambio**: `SessionTurn` (el DTO de turno no cambió de forma), `Runtime/Core/`,
`Runtime/CoreChannels/`. **No es cambio de contrato**: sin gate de gobernanza previo.

**Write boundary**: `Runtime/SessionLog/` + `Tests/EditMode/SessionLog/` +
`openspec/specs/bitacora-sesion-m13/spec.md` + `openspec/changes/2026-09-10-historial-multi-sesion-m13/`.

## Task Completion Gate

**Status**: PASS — todas las fases de `tasks.md` (0-4, incluida 3b) quedaron `[x]`.

## Archiving Decisions

- **Merge Strategy A** (capacidad existente modificada): edición in-place del `spec.md`
  canónico, sin `spec.md` delta separado (limitación de herramienta, no de proceso).
- **Nombre de carpeta**: se conserva `2026-09-10-historial-multi-sesion-m13` (fecha de inicio).

## Relación con otros cambios

- **Modifica**: `bitacora-sesion-m13` (archivado 2026-09-10, PR1-PR3). No lo reabre — es un
  cambio SDD independiente, tal como estaba planeado desde su `proposal.md`.
- **Depende de**: la infraestructura ya construida en el cambio original (SQLite vendorizado,
  `SessionRecorder`, `SessionLogBehaviour`). No agrega dependencias externas nuevas.
- **Habilita**: una futura UI de revisión de historial (M11 u otro consumidor) puede construirse
  sobre `ListarSesiones()`/`ObtenerTurnosDeSesion()` sin tocar `Runtime/SessionLog/` de nuevo.

## Next Steps

### Residual (no bloqueante para este cierre)

1. Validar en dispositivo/build IL2CPP real el binario nativo Android (heredado del M13
   original, tasks.md 2.5, sigue sin hacerse).
2. Cablear `SessionLogBehaviour` en la escena real de M11 cuando exista (M11 sigue sin escena
   `.unity` commiteada al 2026-09-14).
3. Considerar (fuera de alcance, sugerencia a futuro): una UI real de historial, y
   exportación automática a archivo al cerrar sesión.

## Conclusion

**El cambio 2026-09-10-historial-multi-sesion-m13 está archivado y el ciclo SDD está
completo.** M13 pasa de retener solo la última sesión a un historial completo por etiqueta, con
reanudación automática ante cierres no limpios — la necesidad real que motivó la ampliación.
`openspec/specs/bitacora-sesion-m13/spec.md` queda actualizado como fuente de verdad vigente.
