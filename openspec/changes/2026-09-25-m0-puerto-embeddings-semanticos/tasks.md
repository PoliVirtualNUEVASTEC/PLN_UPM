# Tasks: M0 — Puerto de embeddings semánticos de oraciones (`ISentenceEmbedder`)

## Review Workload Forecast

| Field | Value |
|-------|-------|
| Estimated changed lines | U1 ~140-190 · U2 ~380-430 · Total ~460-620 (mismo PR) |
| 400-line budget risk | Medium (presupuesto real de esta sesión: 800 líneas, no el default de 400) |
| Chained PRs recommended | No |
| Suggested split | PR único: commit U1 (tipo) → commit U2 (puerto+bump atómico) |
| Delivery strategy | ask-on-risk |
| Chain strategy | pending |

Decision needed before apply: Yes
Chained PRs recommended: No
Chain strategy: pending
400-line budget risk: Medium

Recalculado contra el precedente real (`2026-09-16-m0-puerto-requerimientos-juntas`: PR1 tipos
203L, PR2 puerto+bump 145L, PR2b clase de contrato 262L; total ≈610L bajo un presupuesto de
400L). Este cambio tiene forma comparable pero una superficie algo menor: 1 tipo nuevo (no 2) y
mismo tamaño de clase de contrato (10 `[Test]`, 2 stubs, subclase). Estimado: `SentenceEmbedding.cs`
~90-110L, `+6` casos en `ContractTypeTests` ~55-70L (U1); `Ports.cs` ~15-25L,
`SentenceEmbedderContract.cs` ~230-260L, `Contract.cs` ~2L, `## v4` ~70-100L, renombre de pin ~8L
(U2). Total ≈460-620L cabe con margen bajo el presupuesto de 800L de esta sesión (57-77% de uso),
por lo que **no** se recomienda encadenar PRs de entrada — a diferencia del precedente, cuyo total
comparable (≈610L) excedía su presupuesto (400L). `Decision needed before apply: Yes` por
`ask-on-risk`: el tamaño es real (por encima del default clásico de 400L) y el precedente demostró
que la clase de contrato puede medir más de lo estimado; se pide confirmar el plan de PR único con
la contingencia de la Fase 4 antes de aplicar.

### Suggested Work Units

| Unit | Goal | Likely PR | Focused test command | Runtime harness | Rollback boundary |
|---|---|---|---|---|---|
| U1 | DTO `SentenceEmbedding` + 6 casos en `ContractTypeTests` | PR único, commit 1 | Test Runner, filtro `ContractTypeTests` | N/A — C# puro, sin escena/VR | Borra `SentenceEmbedding.cs`+`.meta`, revierte los 6 casos; `Contract.Version` sigue en `3`, sin `## v4` |
| U2 | `ISentenceEmbedder` + `SentenceEmbedderContract` + bump `Version` `3→4` + `## v4` + pin | PR único, commit 2 (atómico) | Test Runner, filtro `SentenceEmbedderContract\|ContractVersionChangelogTests` | N/A — C# puro, sin escena/VR | Revierte `Version` a `3`, borra `## v4`, el puerto y la clase de contrato; ningún módulo lo referencia aún |

## Phase 1: DTO `SentenceEmbedding` (U1)

- [x] 1.1 RED — en `Tests/EditMode/Core/ContractTypeTests.cs`, sección `// --- v4: embeddings de oraciones ---`: 6 casos nuevos (`SentenceEmbedding_Empty_es_el_valor_por_defecto_y_hashea_a_cero`, `_con_null_o_arreglo_vacio_es_Empty`, `_copia_el_arreglo_recibido`, `_ToArray_devuelve_una_copia`, `_compara_bit_a_bit` incl. `0f` vs `-0f`, `_iguales_hashean_igual`); no compila hasta 1.2.
- [x] 1.2 GREEN — crear `Runtime/Core/SentenceEmbedding.cs` (+`.meta`) per el esqueleto de design.md (AD6/AD7): campo privado `float[] _values` (clonado en el constructor), `Empty = default`, `Length`, `IsEmpty`, indexador de solo lectura, `ToArray()` (copia defensiva), `IEquatable<SentenceEmbedding>` bit a bit con `BitConverter.SingleToInt32Bits`, `GetHashCode`, `==`/`!=`. Ningún campo público expone el arreglo crudo.
- [x] 1.3 Verify — código escrito y revisado por lectura contra el precedente compilado (`ClinicalCaseId.cs`/`RequirementId.cs` para el estilo de archivo, `ContractTypeTests.cs` para el patrón de casos); los 6 casos nuevos referencian solo API real de `SentenceEmbedding`, los casos v1-v3 quedan intactos (sin edición), `Contract.Version` sigue en `3` y `Docs/CONTRACT-CHANGELOG.md` no tiene `## v4` en este punto de la sesión (límite de U1 del design). **Verde en Unity Test Runner EditMode aún NO confirmado por un humano — ningún agente corre Unity en esta sesión** (mismo patrón que el precedente `2026-09-16-m0-puerto-requerimientos-juntas`).

## Phase 2: Puerto `ISentenceEmbedder` y base de contrato (U2, depende de Fase 1)

- [x] 2.1 RED — crear `Tests/EditMode/Core/SentenceEmbedderContract.cs` (+`.meta`): clase abstracta `SentenceEmbedderContract` con `protected abstract ISentenceEmbedder CreateSubject()`, 10 `[Test]` (`Reporta_si_esta_listo_sin_lanzar`, `Embed_no_lanza_en_ningun_estado`, `Sin_estar_listo_Embed_devuelve_vector_vacio`, `Texto_nulo_vacio_o_solo_espacios_devuelve_vector_vacio`, `Listo_y_con_texto_devuelve_vector_no_vacio`, `Todos_los_vectores_no_vacios_tienen_la_misma_longitud`, `Los_componentes_son_finitos`, `Es_determinista_bit_a_bit_para_el_mismo_texto`, `Mutar_la_copia_devuelta_no_altera_llamadas_posteriores`, `Textos_distintos_no_dan_el_mismo_vector`), 2 stubs `private sealed` (`EmbebedorDeOracionesNoListo`, `EmbebedorDeOracionesDePrueba` con hashing trivial de palabras a 8 cubetas), subclase concreta `SentenceEmbedderContractStubTests`; no compila (`ISentenceEmbedder` no existe).
- [x] 2.2 GREEN — `Runtime/Core/Ports.cs`, sección "Comprension", inmediatamente después de `IIntentClassifier`: `+ interface ISentenceEmbedder` (`bool IsReady { get; }`, `SentenceEmbedding Embed(string text)`), depende de 2.1.
- [x] 2.3 Verify — código escrito y revisado por lectura contra el precedente compilado (`IRequirementResponder`/`RequirementResponderContract`); los 10 `[Test]` referencian solo API real de `ISentenceEmbedder`/`SentenceEmbedding` y compilan contra `SentenceEmbedderContractStubTests` por inspección. **Verde en Unity Test Runner EditMode aún NO confirmado por un humano** (mismo patrón que 1.3 y que el precedente).

## Phase 3: Bump atómico de contrato (U2, cont.)

- [x] 3.1 Commit atómico — contenido preparado como una sola unidad atómica en el árbol de trabajo (no se dividió en commits separados): `Runtime/Core/Contract.cs` `Version` `3 → 4`; `Docs/CONTRACT-CHANGELOG.md` `+ ## v4` (molde del borrador de design.md: Tipos nuevos, invariantes de `ISentenceEmbedder`, Asimetría de determinismo, Advertencia de calidad, Decisiones y proceso con AD1/AD2 resueltas y co-revisión pendiente); `Tests/EditMode/Core/ContractTypeTests.cs` renombrado el pin `Version_del_contrato_es_tres` → `Version_del_contrato_es_cuatro` con valor `4`. **`git commit` real NO ejecutado en esta sesión de apply** — el repo CLAUDE.md global prohíbe commitear sin pedido explícito del usuario; el plan confirmado de 2 commits (U1 / U2) queda listo para ejecutarse en el paso de entrega.
- [x] 3.2 Verify — código escrito y revisado por lectura: `Version == 4` ata al mayor `## v4` recién insertado (`ContractVersionChangelogTests` no se editó, sigue leyendo el mayor encabezado); `noEngineReferences: true` intacto y sin referencias `NpcAi.*` ajenas nuevas (`SentenceEmbedding.cs`/`SentenceEmbedderContract.cs` solo usan `System.*`) — confirmado por lectura de ambos archivos, no por ejecución de `CoreAssemblyPurityTests`. **Verde en Unity Test Runner EditMode aún NO confirmado por un humano.**

## Phase 4: Cierre y contingencia de presupuesto

- [x] 4.1 Correr Test Runner EditMode completo (Window > General > Test Runner) sobre `NpcAi.Core.Tests` — compuerta humana, ningún agente corre Unity. **Confirmado por el usuario (2026-09-25): todo en verde.**
- [x] 4.2 Medir el diff real (`git diff --stat`) acotado a `Runtime/Core/`, `Tests/EditMode/Core/`, `Docs/CONTRACT-CHANGELOG.md`. **Medido**: 8 archivos, 483 inserciones + 4 eliminaciones = **487 líneas** (incluye los 2 archivos nuevos vía `git add -N` + `git diff --stat`, luego revertido a `?? untracked` sin tocar contenido). 487 < 800 (presupuesto de esta sesión): **no se activa la contingencia** — `SentenceEmbedderContract.cs` NO se parte en un PR2 encadenado; se mantiene PR único con los 2 commits confirmados.
- [x] 4.3 Solicitar co-revisión de M0 (regla 2 del repo) a Luis Miguel Cañaveral Restrepo o, en su defecto, al asesor; registrar la aprobación antes de mergear. **Confirmado por el usuario (2026-09-25): APROBADA por Luis Miguel Cañaveral Restrepo, confirmación informal** (`Docs/CONTRACT-CHANGELOG.md` `## v4` ya actualizado).
- [x] 4.4 Confirmar alcance del diff: solo `Runtime/Core/`, `Tests/EditMode/Core/`, `Docs/CONTRACT-CHANGELOG.md`, `openspec/`. Sin tocar `Runtime/Nlu/`, `Training/`, `Runtime/ClinicalResponse/`, `Runtime/RequirementResponse/`, `Docs/MODULES.md` ni ningún `Fakes/` de módulo. **Confirmado por `git status --short`**: los únicos archivos tocados por este cambio son `Runtime/Core/Contract.cs`, `Runtime/Core/Ports.cs`, `Runtime/Core/SentenceEmbedding.cs(.meta)`, `Tests/EditMode/Core/ContractTypeTests.cs`, `Tests/EditMode/Core/SentenceEmbedderContract.cs(.meta)`, `Docs/CONTRACT-CHANGELOG.md`. El resto de archivos modificados/untracked en el árbol de trabajo (M11 compositor, Training/, etc.) pertenecen a otro trabajo en curso en esta misma rama, ninguno tocado por este cambio.

Nota: `Docs/MODULES.md` NO se toca — mismo criterio que v2 y v3 (deuda preexistente, fuera de
alcance de este cambio).
