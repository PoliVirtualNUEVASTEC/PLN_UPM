# Propuesta: M0 — Puerto de embeddings semánticos de oraciones (`ISentenceEmbedder`)

## Intent

Hoy no existe ninguna capacidad semántica real en el repo. `ClinicalFactMatcher` (M15) y
`RequirementMatcher` (M16) emparejan por subconjunto de palabras, y `SemanticMatcher` (M2) es,
pese al nombre, una tabla de ~45 patrones con `.Contains`. M9 difirió `clave.cierreEsperado`
porque "calificarlo exige comparación semántica". La necesidad es recurrente.

`NpcAi.Core` no puede expresar un vector de embedding: `IIntentClassifier`/`IntentResult` solo
llevan `Intent`, `Tone`, `Confidence`, `LatencyMs`. Este cambio agrega esa **capacidad de
contrato** (v3 → v4), nada más.

**Aclaración honesta:** el `.onnx` commiteado usa `distilbert-base-multilingual-cased`, un
encoder de propósito general (masked-LM), **no** entrenado para similitud/paráfrasis. Este
cambio no garantiza que sus embeddings sirvan para emparejar hechos; eso es una pregunta
empírica de los cambios consumidores futuros (M15/M16).

## Scope

### In Scope
- Puerto nuevo en `Runtime/Core/Ports.cs` (nombre de trabajo `ISentenceEmbedder`; forma final en `sdd-design`).
- Tipo(s) nuevo(s) de embedding en C# puro (`Runtime/Core/`), sin `UnityEngine`.
- `Contract.Version` `3 → 4` + `## v4` en `Docs/CONTRACT-CHANGELOG.md` (molde de v1/v2/v3), mismo commit.
- Base abstracta `Tests/EditMode/Core/SentenceEmbedderContract.cs` con stubs locales desechables y una subclase concreta que pruebe que el contrato es satisfacible.
- Casos aditivos en `ContractTypeTests.cs` + renombre del pin `Version_del_contrato_es_tres` → `..._es_cuatro`.

### Out of Scope
- **Implementación real de M2** (`BertIntentClassifier` leyendo una salida ONNX nueva; `export_onnx` de `train.py` exportándola; re-export del `.onnx` LFS). Cambio SDD futuro, solo M2.
- **Consumo en M15 (y luego M16)**: reemplazar `ClinicalFactMatcher`/`RequirementMatcher` por similitud. Un cambio SDD futuro por módulo.
- `Runtime/Nlu/Fakes/` y cualquier `Runtime/<Módulo>/Fakes/`: los stubs de este cambio viven en `Tests/EditMode/Core/`.
- `IIntentClassifier`, `IntentResult` y todo tipo de v1–v3: byte-idénticos.
- `Docs/MODULES.md` (mismo criterio que v3) y `Runtime/CoreChannels/`.

## Capabilities

### New Capabilities
- `embeddings-semanticos-m0`: puerto de embedding, tipo(s) de vector, invariantes DEBE/NO DEBE, trazabilidad a `SentenceEmbedderContract`.

### Modified Capabilities
- `contrato-nucleo-m0`: +1 puerto, +tipo(s), `Contract.Version` `3 → 4`. Puramente aditivo.

## Approach

Opción 2 de la exploración: puerto **nuevo y separado**, aditivo puro, espejo del precedente
`2026-09-16-m0-puerto-requerimientos-juntas`. Deja intactos a los cuatro consumidores de
`IntentResult` (M4, M6, M15, M16) y a los dobles de M2.

Decisiones abiertas para `sdd-design` (se nombran, no se resuelven aquí):

| AD | Pregunta | Tensión |
|---|---|---|
| AD1 | ¿Dimensión fija en el contrato o definida por la implementación? | `hidden_size` depende de `--encoder` (sin default): 768 hoy, 384 con MiniLM |
| AD2 | ¿Cláusula de determinismo propia (bit-exacta, con tolerancia o ninguna)? | `IIntentClassifier` solo garantiza `Intent`/`Tone`; `IClinicalResponder` exige determinismo total |

## Affected Areas

| Área | Impacto | Descripción |
|---|---|---|
| `Runtime/Core/Ports.cs` | Modificado (aditivo) | `+ ISentenceEmbedder` |
| `Runtime/Core/` (tipo de embedding) | Nuevo/aditivo | DTO en C# puro |
| `Runtime/Core/Contract.cs` | Modificado | `Version` `3 → 4` |
| `Docs/CONTRACT-CHANGELOG.md` | Modificado | `+ ## v4` |
| `Tests/EditMode/Core/SentenceEmbedderContract.cs` | Nuevo | Base de contrato + stubs locales |
| `Tests/EditMode/Core/ContractTypeTests.cs` | Modificado (aditivo) | Casos nuevos + pin de versión |

## Risks

| Riesgo | Prob. | Mitigación |
|---|---|---|
| Embeddings de DistilBERT genérico inútiles para similitud | Media-Alta | Advertencia en changelog; validación empírica en M15/M16 |
| Doble `Schedule()` por turno si M2 implementa ambos puertos | Media | Decisión del cambio de M2 (cachear o aceptar ~60-160 ms por turno) |
| Bump sin `## v4` rompe `ContractVersionChangelogTests` | Alta si se separa | Mismo commit |
| Tentación de tocar `Runtime/Nlu/` "de paso" | Media | Criterio de éxito que acota el diff |

## Rollback Plan

Revertir los commits: `Version` vuelve a `3`, se quita `## v4`, se borran puerto, tipo(s) y
base de contrato. Ningún módulo depende aún del puerto; sin migración de datos.

## Dependencies

- `contrato-nucleo-m0` estable (v3 en `main`).
- Co-revisión de M0 antes del merge (regla 2): Luis Miguel Cañaveral Restrepo o, en su defecto, el asesor.

## Success Criteria

- [ ] `Contract.Version == 4`, `## v4` presente, `ContractVersionChangelogTests` en verde.
- [ ] `CoreAssemblyPurityTests` en verde (`noEngineReferences: true` intacto).
- [ ] `SentenceEmbedderContract` pasa contra un stub local; lista para heredarse en M2.
- [ ] AD1 y AD2 resueltas y registradas en el changelog.
- [ ] Diff limitado a `Runtime/Core/`, `Tests/EditMode/Core/`, `Docs/CONTRACT-CHANGELOG.md`, `openspec/`.
- [ ] Revisado por el otro dueño de M0 o el asesor.

## Proposal question round

Pendiente de revisión del usuario (el ejecutor no pudo preguntar directamente):

1. ¿Qué consumidor justifica hacerlo ahora: M15, M16 o la evaluación de `cierreEsperado` de M9?
2. AD1: ¿aceptan que la dimensión cambie entre reentrenamientos, o prefieren fijarla y obligar a reentrenar con un encoder compatible?
3. AD2: ¿un mismo texto debe producir exactamente el mismo vector en cada turno, o basta una tolerancia?
4. ¿Un embedder "no listo" debe devolver un vector vacío o señalarlo con `IsReady`, como `IClinicalResponder`?

Supuestos vigentes: puerto separado (Opción 2); solo M0; M2 y M15/M16 en cambios posteriores.
