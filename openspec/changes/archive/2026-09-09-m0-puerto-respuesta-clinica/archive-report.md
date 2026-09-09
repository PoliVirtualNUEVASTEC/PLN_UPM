# Reporte de archivo: 2026-09-09-m0-puerto-respuesta-clinica

**Cambio**: `2026-09-09-m0-puerto-respuesta-clinica`
**Tipo**: Extensión de contrato (v1 → v2)
**Fecha de archivo**: 2026-09-09
**Modo**: SDD hybrid (OpenSpec + Engram)

## Resumen

Este cambio formaliza el puerto `IClinicalResponder` como superficie de contrato nueva en
`NpcAi.Core` (contrato v1 → v2), para que M15 (módulo respondedor clínico) exista como
módulo de primera clase. El cambio es puramente aditivo:

- **3 tipos nuevos**: `ClinicalCaseId` (struct, espejo de `PersonalityId`), `ClinicalResponse`
  (struct con la señal de enrutado), `IClinicalResponder` (puerto con `IsReady`, `AssignCase`,
  `Respond`).
- **Ningún tipo de v1 tocado**: no se renombra, reordena ni cambia la forma de ningún enum,
  DTO ni puerto del contrato v1.
- **Versión de contrato**: sube de 1 a 2, con la sección `## v2` correspondiente en
  `Docs/CONTRACT-CHANGELOG.md`.
- **Spec promovida**: la capacidad nueva `respuesta-clinica-m0` pasa de delta a specs
  principales en `openspec/specs/respuesta-clinica-m0/spec.md`.

**Trazabilidad**: 8 requisitos ADDED / 16 escenarios. 8 escenarios están verificados por
runtime ahora (verde EditMode humano del 2026-09-09); 8 están trazados hacia adelante a M15
(base abstracta `ClinicalResponderContract` con 7 métodos `[Test]`, sin ejecución de runtime
todavía).

---

## Autoridad de verificación

### Reporte de verify

- **Veredicto**: PASA CON OBSERVACIONES — apto para archivo tras la co-revisión de M0
- **Bloqueos**: 0
- **Hallazgos CRITICAL**: 0
- **Requisitos**: 8/8 ✓
- **Escenarios**: 16/16 (8 ahora, 8 hacia adelante)
- **ID de observación en Engram**: #48 (`sdd/2026-09-09-m0-puerto-respuesta-clinica/verify-report`)

### Datos de estado final (por el handoff; prevalecen sobre los snapshots intermedios)

1. **Evidencia de runtime**: Test Runner EditMode → Run All 100% VERDE, 2026-09-09
   (autor luisk, tarea 3.1 de `tasks.md`).
   - Cubre: `ContractTypeTests` (congelamiento de enums de v1 + 4 casos nuevos de v2),
     `ContractVersionChangelogTests`, `CoreAssemblyPurityTests`, las 7 bases `*Contract`
     previas.
   - NO ejecuta `ClinicalResponderContract` (abstracta, sin subclase de M15 todavía); sus
     7 `[Test]` están trazados hacia adelante.

2. **Completitud de tareas**: 18/18 tareas marcadas `[x]` en `tasks.md` (autoritativo;
   `apply-progress.md` quedó desactualizado, ver WARNING 2 del reporte de verify).
   - Fases 0, 1, 2: código y pruebas escritos y commiteados.
   - Fase 3: compuertas manuales (EditMode, inspección del `.asmdef`) y auditoría de la
     frontera de `git` completadas.

3. **Co-revisión de M0 (regla 3 del repo)**: APROBADA por Luis (autor y dueño compartido
   de M0) el 2026-09-09.
   - Compuerta de pre-merge satisfecha; sin bloqueo para el archivo.

4. **SUG 1 (RESUELTA en el commit `5b7c8af`)**:
   - Problema: `Docs/CONTRACT-CHANGELOG.md` línea 76 (sección congelada `## v1`) todavía
     decía "(ventana del lunes, co-aprobacion)" — contradecía la línea 4 ya corregida.
   - Resolución: se quitó la frase obsoleta; ahora apunta a la regla del encabezado.

5. **Commits en la rama `feat/m0-puerto-respuesta-clinica`**:
   - `ba329bb` — feat: superficie de contrato v2 + tipos + puerto + base + pruebas nuevas
   - `05808bd` — chore: checkpoint de tareas
   - `d4dfbbc` — chore: checkpoint de tareas
   - `ffc803b` — docs: `verify-report.md` (sdd-verify)
   - `5b7c8af` — docs: corrección de `CONTRACT-CHANGELOG.md` línea 76 (SUG 1)

---

## Observaciones y disposición

| # | Categoría | Problema | Disposición | ¿Bloquea el archivo? |
|---|-----------|----------|-------------|----------------------|
| W1 | INFO | Discrepancia en el conteo de escenarios del metadato de Engram | `spec.md` tiene 16 escenarios (8 req); Engram #46 dice 18; el archivo usa el conteo autoritativo de `spec.md` (16) | No |
| W2 | INFO | `apply-progress.md` desactualizado | Muestra 3.1/3.2/4.x como `[ ]`; `tasks.md` (autoritativo) está 18/18 `[x]` | No |
| W3 | HACIA ADELANTE | 8/16 escenarios trazados hacia adelante a M15 | `ClinicalResponderContract` abstracta; la ejecución de comportamiento se difiere al verify de M15 | No (por diseño; la spec declara la trazabilidad hacia adelante) |
| W4 | FAST-FOLLOW | `Respond_no_lanza_en_ningun_estado` solo ejercita el camino no-listo | No existe ningún sujeto listo en M0 (el stub nunca lo es, M15 ausente); el hueco se cierra en el apply de M15 con 4 líneas: `AssignCase` + `Assume.That` | No (responsabilidad de M15) |

**Sugerencias** (no bloquean):

- SUGERENCIA 1: agregar una aserción directa opcional de parseo del `.asmdef`
  (`noEngineReferences == true`, largo de `references` == 0).
- SUGERENCIA 2: documentar en el metadato de Engram por qué se reconcilió el conteo 18→16
  (nota opcional).

---

## Métricas

| Métrica | Valor |
|---------|-------|
| Requisitos ADDED | 8 |
| Escenarios totales | 16 |
| Diff de código + pruebas | +343 / −4 líneas (~347 neto) |
| Presupuesto de revisión (800 líneas) | **Riesgo bajo** (43% usado) |
| Tareas totales | 18 |
| Tareas completas `[x]` | 18 |
| Violaciones de frontera de escritura | 0 (rutas de M0: `Runtime/Core/`, `Docs/CONTRACT-CHANGELOG.md`, `Tests/EditMode/Core/`, directorio del cambio) |
| Firmas de enum/DTO/puerto de v1 alteradas | 0 (puramente aditivo) |
| Hallazgos CRITICAL | 0 |
| Bloqueos | 0 |
| Veredicto de verify | PASA CON OBSERVACIONES |

---

## Promoción de spec y contenido del archivo

### Spec promovida

- **Origen**: `openspec/changes/2026-09-09-m0-puerto-respuesta-clinica/specs/respuesta-clinica-m0/spec.md`
- **Destino**: `openspec/specs/respuesta-clinica-m0/spec.md`
- **Verificación de copia mecánica**: `diff -r` vacío ✓

### Directorio de archivo

Ruta: `openspec/changes/archive/2026-09-09-m0-puerto-respuesta-clinica/`

Contenido (movido con `git mv`):

- `proposal.md` — alcance, enfoque, rollback
- `design.md` — decisiones de arquitectura, firmas del puerto, estrategia de pruebas
- `tasks.md` — 18 tareas de implementación (18/18 completas ✓)
- `apply-progress.md` — snapshot de la fase apply (desactualizado; se incluye para el rastro
  de auditoría)
- `verify-report.md` — reporte de verificación (versión canónica en Engram #48)
- `specs/respuesta-clinica-m0/spec.md` — delta de spec (ahora también en specs principales)
- `archive-report.md` (este archivo) — rastro de auditoría final

**Verificación del movimiento mecánico**: `diff -r` entre el snapshot pre-movimiento y el
contenido archivado no muestra diferencias ✓

---

## IDs de observación en Engram (trazabilidad de artefactos SDD)

| Fase | Artefacto | ID Engram | Topic key |
|------|-----------|-----------|-----------|
| spec | Especificación | #46 | `sdd/2026-09-09-m0-puerto-respuesta-clinica/spec` |
| verify | Reporte de verificación | #48 | `sdd/2026-09-09-m0-puerto-respuesta-clinica/verify-report` |
| archive | Este reporte de archivo | #49 | `sdd/2026-09-09-m0-puerto-respuesta-clinica/archive-report` |

---

## Qué queda para el pipeline

1. **Compuerta de co-revisión de M0**: YA SATISFECHA (Luis aprobó el 2026-09-09).
2. **Merge del PR**: el autor (luisk) mergea el PR de `feat/m0-puerto-respuesta-clinica`
   tras cerrarse la compuerta de co-revisión.
3. **Responsabilidad de M15** (SDD `2026-09-09-m15-respondedor-clinico`):
   - Heredar `ClinicalResponderContract` en el doble y la implementación real de M15.
   - Ejecutar los 8 escenarios trazados hacia adelante (disposición de W3, W4).
   - Agregar cobertura del camino listo para `Respond_no_lanza_en_ningun_estado`
     (fast-follow de 4 líneas, opcional).
4. **Documento de contexto del proyecto**: registrar la decisión "Contrato v2 — puerto
   `IClinicalResponder` para M15" (regla 10 del `CLAUDE.md`).

---

## Compuertas de cumplimiento

| Compuerta | Estado | Evidencia |
|-----------|--------|-----------|
| **Completitud de tareas** | ✓ PASA | 18/18 `[x]` en `tasks.md` |
| **Receipt de revisión nativa** | ✓ POLÍTICA ORDINARIA | Sin compuerta formal de revisión; co-revisión aprobada por Luis (regla 3) |
| **Hallazgos CRITICAL** | ✓ NINGUNO | verify-report: 0 `critical_findings` |
| **Bloqueos** | ✓ NINGUNO | verify-report: 0 `blockers` |
| **Promoción de spec** | ✓ PASA | Copia mecánica a `openspec/specs/respuesta-clinica-m0/spec.md`, diff verificado |
| **Movimiento de archivo** | ✓ PASA | `git mv` a `openspec/changes/archive/`, origen eliminado, diff verificado |
| **Frontera de escritura** | ✓ PASA | Solo rutas del contrato de M0; sin contaminación entre módulos |

---

## Siguiente paso

**Estado**: archivo completo y cerrado.

El ciclo SDD del cambio `2026-09-09-m0-puerto-respuesta-clinica` está CERRADO. La spec, el
diseño, las tareas y los artefactos de verificación quedan en el rastro de auditoría. El PR
mergeado es el vehículo de entrega de runtime. M15 heredará la base de contrato y ejecutará
los escenarios trazados hacia adelante.

No se necesitan más fases SDD para M0.
