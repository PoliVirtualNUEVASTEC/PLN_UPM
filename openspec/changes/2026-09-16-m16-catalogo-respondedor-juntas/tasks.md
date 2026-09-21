# Tasks: M16 — Catálogo y respondedor de requerimientos de sala de juntas

> Nota de tamaño: este artefacto excede la guía general de 530 palabras. Justificación real
> (no silenciosa): `design.md` ya estimó ~1.528 líneas autoradas (≈3,8× el presupuesto de
> revisión) repartidas en 4 PRs encadenados con TDD estricto (RED/GREEN por pieza). Recortar
> el detalle por debajo de 530 palabras dejaría tareas ambiguas (“implementar matcher”) que la
> propia guía prohíbe. Se prioriza tarea verificable y trazable a spec sobre el límite de
> palabras.

## Review Workload Forecast

| Field | Value |
|-------|-------|
| Estimated changed lines | ~1.670–1.890 autoradas + ~160–180 `.meta` (recalculado, ver abajo) |
| 400-line budget risk | High |
| Chained PRs recommended | Yes |
| Suggested split | PR1 → PR2 → PR3 → PR4 (PR4 con partición condicional PR4a/PR4b) |
| Delivery strategy | ask-on-risk |
| Chain strategy | stacked-to-main |

Decision needed before apply: Yes
Chained PRs recommended: Yes
Chain strategy: stacked-to-main (4 PRs, sin rama tracker: PR1 → `main`, PR2 → rama de PR1, PR3 → rama de PR2, PR4(a/b) → rama de PR3 — consistente con el cambio de M0)
400-line budget risk: High

**Recalculo contra código real** (no solo repite `design.md`): se leyeron los pares homólogos
de M15 (`ClinicalCasesDataTests.cs` = 139 líneas reales vs. 140 estimadas por diseño para
`RequirementCasesDataTests.cs` — coincide; `ClinicalResponderTests.cs` = 133 líneas reales vs.
110 estimadas para `RequirementResponderTests.cs` — subestimado; `Data/Cases/caso-01.json` =
88 líneas reales vs. ~65 estimadas por caso `caso-juntas-0N.json` — subestimado, mismo mínimo
de 6-7 campos por requerimiento y bloque `cliente`). Con ese ajuste, PR3 y PR4 quedan más
altos que lo que proyectó `design.md`:

| PR | Contenido | Est. `design.md` | Est. ajustada (ancla M15) |
|---|---|---|---|
| PR1 | asmdef + `RequirementCase` + `RequirementCaseLoader` + asmdef pruebas + `RequirementCaseLoaderTests` | 378 | 380–420 |
| PR2 | `RequirementMatcher` + `RequirementDisclosurePolicy` + `Fakes/ScriptedRequirementResponder` + 3 archivos de prueba | 335 | 320–360 |
| PR3 | `PersonalityStyleBank` + `matices.json` + `RequirementResponder` + sus pruebas | 390 | 420–460 |
| PR4 | 4 `caso-juntas-0N.json` + `RequirementCasesDataTests` + `Docs/MODULES.md` | 425 | 550–650 |

PR4 es el que más se aleja del presupuesto tras el recálculo. Si al escribir los 4 casos reales
el conteo confirma >400, partir en **PR4a** (`caso-juntas-01.json`, `caso-juntas-02.json`, sin
pruebas nuevas) → **PR4b** (`caso-juntas-03.json`, `caso-juntas-04.json` + `RequirementCasesDataTests.cs`
+ `Docs/MODULES.md`, que exige los 4 casos completos para validar "exactamente 4"). Es
exactamente el plan B que ya dejó `design.md`.

### Suggested Work Units

| Unit | Goal | Likely PR | Focused test command | Runtime harness | Rollback boundary |
|------|------|-----------|----------------------|-----------------|-------------------|
| 1 | Tipo de caso + cargador validando JSON en memoria | PR1 | Test Runner EditMode, filtro `RequirementCaseLoaderTests` | N/A — sin escena, sin enrutador de sala de juntas (M10/M11 no existen aún) | Borra `Runtime/RequirementResponse/RequirementCase.cs`, `RequirementCaseLoader.cs`, asmdef, sus pruebas |
| 2 | Emparejador + puerta de receptividad + doble en paridad de contrato | PR2 | Test Runner EditMode, filtro `RequirementMatcherTests\|RequirementDisclosurePolicyTests\|ScriptedRequirementResponderTests` | N/A — mismo motivo | Borra `RequirementMatcher.cs`, `RequirementDisclosurePolicy.cs`, `Fakes/ScriptedRequirementResponder.cs`, sus pruebas |
| 3 | Banco de matices + adaptador real | PR3 | Test Runner EditMode, filtro `PersonalityStyleBankTests\|RequirementResponderTests` | N/A — mismo motivo | Borra `PersonalityStyleBank.cs`, `matices.json`, `RequirementResponder.cs`, sus pruebas |
| 4 | Catálogo de datos + pruebas de datos + docs | PR4(a/b) | Test Runner EditMode, filtro `RequirementCasesDataTests` | N/A — mismo motivo | Borra `Data/Requirements/caso-juntas-0N.json`, `RequirementCasesDataTests.cs`, revierte sección M16 de `Docs/MODULES.md` |

Threat Matrix: N/A (heredado de `design.md` — sin enrutado de procesos, shell, subprocesos,
automatización VCS/PR ni clasificación de ejecutables).

## Phase 1: Tipo de caso + cargador (PR1)

- [x] 1.1 Crear `Runtime/RequirementResponse/NpcAi.RequirementResponse.asmdef`, `references: ["NpcAi.Core"]`, `noEngineReferences: false`. Verificable: compila, sin otras refs.
- [x] 1.2 Crear `Runtime/RequirementResponse/RequirementCase.cs` (`RequirementCase`, `Cliente`, `Requerimiento`, espejo del diseño). Sin prueba propia (se ejercita vía 1.5, igual que M15).
- [x] 1.3 Crear `Tests/EditMode/RequirementResponse/NpcAi.RequirementResponse.Tests.asmdef` (refs: `NpcAi.Core`, `NpcAi.Core.Tests`, `NpcAi.RequirementResponse`, TestRunner; `includePlatforms: ["Editor"]`).
- [x] 1.4 **RED** — `Tests/EditMode/RequirementResponse/RequirementCaseLoaderTests.cs`: carga válida; JSON vacío/basura ⇒ `false` sin lanzar; `receptividadMinima` por nombre mapea a los 3 valores (AD1); nombre inválido/ausente ⇒ ese requerimiento se descarta (AD2); por debajo de `MinimoRequerimientos=4` ⇒ `false`; `emotionTag`/`animationCue` ausentes ⇒ defaults `"neutral"`/`"idle"` (AD8). Debe fallar en rojo (clase no existe).
- [x] 1.5 **GREEN** — Crear `Runtime/RequirementResponse/RequirementCaseLoader.cs` (`TryParse`, clases `Raw*`, `TryMapearReceptividad`, `Where(...)` de descarte, mínimo 4) — mínimo necesario para 1.4 en verde. **Bajado de 6 a 4 el 2026-09-18** tras redactar los 4 casos de datos: el dominio real de colegio (`caso-juntas-03`) solo llega a 4 requerimientos narrados en `Data/Corpus/juntas.json` sin inventar contenido; Jefferson confirmó bajar el mínimo global en vez de forzar datos o pedir ampliación de corpus.
- [x] 1.6 **Verify PR1** — 1.4 en verde; asmdef de runtime referencia solo `NpcAi.Core`; diff no toca `Runtime/Core/`, `Runtime/CoreChannels/`, `Runtime/ClinicalResponse/`. **Confirmado por Jefferson (2026-09-18): Test Runner EditMode corrido en el Editor, todos los tests en verde.** PR abierto: [#45](https://github.com/PoliVirtualNUEVASTEC/PLN_UPM/pull/45). **Re-confirmado (mismo día)** tras bajar `MinimoRequerimientos` a 4 y reescribir los 2 tests de frontera: Test Runner corrido de nuevo, todo en verde.

## Phase 2: Emparejador + puerta + doble (PR2, depende de Fase 1)

> **Partido en PR2a/PR2b (2026-09-21)**: el lote completo (447 líneas autoradas)
> excedía el presupuesto de 400 por 47. Jefferson decidió partir por unidad de
> trabajo natural en vez de aceptar `size:exception`: **PR2a** = Matcher +
> DisclosurePolicy + sus pruebas (rama `feat/m16-matcher-politica`, commit
> `946021b`, 286 líneas con `.meta`). **PR2b** = doble + su prueba de contrato
> (rama `feat/m16-doble-scripted`, sobre PR2a, commit `17aed9e`, 197 líneas con
> `.meta`), mismo patrón ya usado para PR4a/PR4b más abajo.

- [x] 2.1 **RED** — `RequirementMatcherTests.cs`: coincidencia esperada, `-1` en turno social, normalización quita tildes/signos, empate gana menor índice. **PR2a.**
- [x] 2.2 **GREEN** — Crear `RequirementMatcher.cs` (`Normalizar`, `Match`; adaptación de `ClinicalFactMatcher`, AD11). **PR2a.**
- [x] 2.3 **RED** — `RequirementDisclosurePolicyTests.cs`: matriz 3×3 (`receptividadMinima` × `Receptivity`) — 6 `Revelado`, 3 `AunNoRevelado`; nunca `NoAplica`. **PR2a.**
- [x] 2.4 **GREEN** — Crear `RequirementDisclosurePolicy.cs` (`Decidir`, comparación aritmética `(int)actual >= (int)minima`, AD4). **PR2a.**
- [x] 2.5 Crear `Fakes/ScriptedRequirementResponder.cs`: tabla embebida de ≥6 `Requerimiento` cubriendo los 3 niveles, **incluye una entrada sintética `"presupuesto"`** (decisión #66: el doble sí puede usar datos sintéticos) con `ejemplosDePregunta` que cubran "cual es el presupuesto del proyecto"; reconoce los 4 ids `caso-juntas-01..04`; reutiliza `RequirementMatcher`/`RequirementDisclosurePolicy` (regla 4). **PR2b.**
- [x] 2.6 **RED** — `ScriptedRequirementResponderTests.cs : RequirementResponderContract` (hereda, sin tocar la base). **PR2b.**
- [x] 2.7 **GREEN** — Ajustar 2.5 hasta que los 10 `[Test]` heredados pasen sin `Assume`-skip (posible por el dato sintético). **PR2b.**
- [x] 2.8a **Verify PR2a** — 2.1 y 2.3 en verde; asmdef de runtime sigue refiriendo solo `NpcAi.Core`; diff no toca módulos ajenos. Confirmado por Jefferson (2026-09-21): Test Runner EditMode en verde.
- [x] 2.8b **Verify PR2b** — Doble en 10/10 verde; conteo total del lote (Matcher+DisclosurePolicy+doble+regresión PR1): 40/40 en verde, 0 omitidas. Confirmado por Jefferson (2026-09-21).

## Phase 3: Banco de matices + adaptador real (PR3, depende de Fase 2)

> **Partido en PR3a/PR3b (2026-09-21)**: el lote completo (527 líneas autoradas)
> excedía el presupuesto de 400 por 127, más que PR2. Jefferson volvió a elegir
> partir por unidad de trabajo natural en vez de `size:exception`: **PR3a** =
> `matices.json` + `PersonalityStyleBank` + su prueba (rama
> `feat/m16-matices-bank`, commit `607e847`, 309 líneas con `.meta`). **PR3b**
> = `RequirementResponder` + su prueba de contrato (rama
> `feat/m16-respondedor-real`, sobre PR3a, commit `4caa20b`, 241 líneas con
> `.meta`), mismo patrón de PR2a/PR2b.
>
> **Corrección al pronóstico de esta misma fase**: de los 10 `[Test]`
> heredados de `RequirementResponderContract`, en realidad corren **6 en
> verde y 4 inconclusas por `Assume`** (no 4 verdes/6 inconclusas como decía
> el párrafo original de 3.4 abajo). `Es_determinista_...` y
> `AssignCase_es_idempotente_...` no dependen de "presupuesto": solo
> verifican consistencia entre llamadas, que se cumple igual con `NoAplica`.
> Las 4 que sí quedan inconclusas: `Cuando_revela_...`,
> `Cuando_aun_no_revela_...`, `El_RequirementId_esta_poblado_...`,
> `Baja_receptividad_...`. Confirmado por Jefferson en Test Runner
> (2026-09-21): exactamente esas 4 en naranja, el resto verde.

- [x] 3.1 Crear `Data/Requirements/matices.json` (4 personalidades: `grosero`, `histerico`, `introvertido`, `empatico`, cada una con `prefijoRevelado` + `desvios[]`). **PR3a.**
- [x] 3.2 **RED** — `PersonalityStyleBankTests.cs`: carga de las 4; rotación determinista (`indice % desvios.Count`, AD7, 3 llamadas mismo resultado); índice fuera de rango no lanza; personalidad desconocida/`None` ⇒ fallback con texto no vacío (AD10). **PR3a.**
- [x] 3.3 **GREEN** — Crear `PersonalityStyleBank.cs` (`TryParse`, `PrefijoRevelado`, `Desvio`, clases `Raw*`). **PR3a.**
- [x] 3.4 **RED** — `RequirementResponderTests.cs : RequirementResponderContract`: `CreateSubject()` con fixture JSON embebido que refleja el **dominio real** de `caso-juntas-01` (torneo de fútbol) — **sin inventar `"presupuesto"`** (decisión #66). Ver corrección de conteo en la nota de partido arriba (6 verdes/4 inconclusas, no 4/6). Agregadas las 3 pruebas propias: (a) `Revelado` ⇒ `Reply.Text` contiene la `respuesta` intacta; (b) `AunNoRevelado` ⇒ `Reply.Text` no contiene la `respuesta`; (c) el matiz de personalidad cambia el prefijo, no el dato. **PR3b.**
- [x] 3.5 **GREEN** — Crear `RequirementResponder.cs` (`IRequirementResponder` real; prefijo `Core.RequirementResponse` obligatorio, AD5 ignora `intent`, AD9 `Func<RequirementCaseId,string>` + `maticesJson` inyectados, AD10 fallback si `matices.json` no valida). **PR3b.**
- [x] 3.6a **Verify PR3a** — 3.2 en verde; asmdef de runtime sigue refiriendo solo `NpcAi.Core`; diff no toca módulos ajenos. Confirmado por Jefferson (2026-09-21).
- [x] 3.6b **Verify PR3b** — 6/10 `[Test]` heredados en verde, 4 inconclusas por `Assume` documentadas (no fallo oculto); 3 pruebas propias en verde; conteo total del lote (PR1+PR2+PR3): 62 pruebas, 0 rojas. Confirmado por Jefferson (2026-09-21).

## Phase 4: Catálogo de datos + docs (PR4, depende de Fase 3; partir en 4a/4b si el conteo real de 4.2–4.5 supera 400 líneas)

> **Partido en PR4a/PR4b (2026-09-21)**: el lote completo (589 líneas autoradas,
> contando README + 4 casos + prueba + docs) excedía el presupuesto de 400 por
> 189 — el mayor exceso de las 4 fases, dentro del rango que ya anticipaba
> `design.md` (550–650). Se usó el **plan B ya pre-planeado en el encabezado de
> esta fase**, no el corte genérico A/B de PR2/PR3: **PR4a** = `README.md` +
> `caso-juntas-01.json` + `caso-juntas-02.json`, sin pruebas nuevas (rama
> `feat/m16-casos-torneo-tienda`, commit `38f3d97`, 268 líneas con `.meta`).
> **PR4b** = `caso-juntas-03.json` + `caso-juntas-04.json` +
> `RequirementCasesDataTests.cs` + `Docs/MODULES.md` (rama
> `feat/m16-casos-colegio-aerolinea`, sobre PR4a, commit `3933977`, 356 líneas
> con `.meta`) — el test exige los 4 casos completos para validar "exactamente
> 4", por eso va junto con los 2 casos restantes.
>
> De paso se corrigió `README.md`: seguía diciendo "mínimo 6" cuando
> `MinimoRequerimientos` se bajó a 4 desde PR1 (commit `aa44564`); quedó
> desactualizado por escribirse antes de esa decisión.

- [x] 4.1 Escribir `Data/Requirements/README.md`: esquema, regla "un caso == un archivo, `id` == nombre de archivo", reglas de redacción de `ejemplosDePregunta` (mínimo 2, mínimo 2 palabras de contenido cada uno). **Hecho (2026-09-18, adelantado). PR4a.**
- [x] 4.2 Transcribir `Data/Requirements/caso-juntas-01.json` (torneo de fútbol) desde `Data/Corpus/juntas.json` — solo contenido ya narrado, **sin `"presupuesto"`** ni ningún requerimiento inventado; ≥4 requerimientos, ≥1 por cada nivel de receptividad. **Hecho (2026-09-18, adelantado)**: 6 requerimientos, corrige además el ejemplo de `design.md` que incluía "presupuesto"/"calendario" (no narrados en el corpus, retirados). **PR4a.**
- [x] 4.3 Transcribir `Data/Requirements/caso-juntas-02.json` (tienda) con el mismo criterio. **Hecho (2026-09-18, adelantado)**: 6 requerimientos. **PR4a.**
- [x] 4.4 Transcribir `Data/Requirements/caso-juntas-03.json` (colegio) con el mismo criterio. **Hecho (2026-09-18, adelantado)**: solo 4 requerimientos reales en el corpus — motivó bajar `MinimoRequerimientos` de 6 a 4 (decisión de Jefferson, ver 1.5). **PR4b.**
- [x] 4.5 Transcribir `Data/Requirements/caso-juntas-04.json` (aerolínea) con el mismo criterio. **Hecho (2026-09-18, adelantado)**: 6 requerimientos. **PR4b.**
- [x] 4.6 **RED** — `RequirementCasesDataTests.cs` (patrón `ClinicalCasesDataTests`, `AssetDatabase.FindAssets` filtrado por `Data/Requirements/` + prefijo `caso-juntas-`): esquema mínimo por requerimiento; `id` == nombre de archivo; cobertura de los 3 niveles por caso; exactamente 4 casos; ninguna `respuesta` aparece como subcadena en ningún `desvio` de `matices.json`. **PR4b.**
- [x] 4.7 **GREEN** — Ajustar 4.2–4.5 (solo datos, ninguna clase C#) hasta que 4.6 pase. Los 4 casos ya cumplían el esquema, sin cambios de datos necesarios. **PR4b.**
- [x] 4.8 Actualizar `Docs/MODULES.md` con la fila M16 (`Runtime/RequirementResponse/`, `NpcAi.RequirementResponse`, estado, dependencias M0→M16→M10). **PR4b.**
- [x] 4.9 **Verify PR4** — 4.6 en verde para los 4 casos; diff no toca `Runtime/Core/`, `Runtime/CoreChannels/`, `Runtime/ClinicalResponse/`, `Runtime/Scenarios/`, `Runtime/Nlu/`, `Data/Corpus/`; los `.meta` de archivos/carpetas nuevos versionados. Conteo total del módulo: 67 pruebas, 4 inconclusas esperadas (ver nota de Fase 3), 0 rojas. Confirmado por Jefferson (2026-09-21).

## Dependencias entre fases
Fase 1 → Fase 2 → Fase 3 → Fase 4, estrictamente secuencial (cada PR rama de la rama del PR
anterior, stacked-to-main, mismo patrón de M0). Ninguna tarea de una fase posterior puede
empezar sin el cierre verificable (`Verify`) de la anterior.
