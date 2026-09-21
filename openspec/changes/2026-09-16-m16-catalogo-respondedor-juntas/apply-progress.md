# Apply progress: M16 — Catálogo y respondedor de requerimientos de sala de juntas

- **Cambio**: `2026-09-16-m16-catalogo-respondedor-juntas`
- **Store**: hybrid (este archivo + Engram, topic `sdd/2026-09-16-m16-catalogo-respondedor-juntas/apply-progress`)
- **Modo**: Strict TDD. Corredor de pruebas manual: Unity Test Runner > EditMode > Run All. Ningún agente ejecuta Unity; el verde es compuerta humana.
- **Entrega**: `auto-chain`, `stacked-to-main`, presupuesto de revisión 400 líneas.

## Estado acumulado por tarea

| Tarea(s) | Estado | Nota |
|---|---|---|
| 1.1–1.6 (PR1) | Hechas y verificadas | Test Runner en verde, confirmado por Jefferson (2026-09-18). PR [#45](https://github.com/PoliVirtualNUEVASTEC/PLN_UPM/pull/45) abierto |
| 2.1–2.4 (PR2a) | Hechas y verificadas | Test Runner en verde, confirmado por Jefferson (2026-09-21). Commit `946021b` en `feat/m16-matcher-politica` |
| 2.5–2.7 (PR2b) | Hechas y verificadas | Test Runner en verde, confirmado por Jefferson (2026-09-21). Commit `17aed9e` en `feat/m16-doble-scripted` |
| 2.8a / 2.8b | Hechas | Ver sección "Cierre PR2" |
| 3.1–3.6 (PR3) | Sin empezar | |
| 4.1–4.5 | Hechas por adelantado | README + 4 casos redactados, sin commitear hasta la Fase 4 |
| 4.6–4.9 | Sin empezar | |

Los `- [ ]` de `tasks.md` para 2.1–2.8 **no se tocaron**: el orquestador los marca solo tras la evidencia del Test Runner.

## Lote PR1 (arrastrado, ya cerrado)

- `RequirementCase` + `RequirementCaseLoader` (395 líneas) en la rama `feat/m16-tipo-cargador`, sobre `main`. `main` avanzó mientras tanto con M9 real y M11 PR1 de Luis Miguel; el rebase fue limpio, sin conflictos.
- Test Runner en verde confirmado por Jefferson. `MinimoRequerimientos` se bajó de 6 a 4 (commit `aa44564`) porque el dominio real de colegio (`caso-juntas-03`) solo llega a 4 requerimientos narrados en `Data/Corpus/juntas.json` sin inventar contenido; los 2 tests de frontera se reescribieron y el Test Runner se re-confirmó en verde (commit `b1cae3b`).
- En paralelo se cerró un pendiente de M0: `verify-report.md` (PASS con observaciones), ya en `main` (commit `d459373`).
- Preparación adelantada de la Fase 4 (sin commitear): `Data/Requirements/README.md` y `caso-juntas-01..04.json`, transcritos de `Data/Corpus/juntas.json`, sin `"presupuesto"` (decisión #66).

## Lote PR2 — matcher + puerta + doble (2026-09-18)

Rama de trabajo: `feat/m16-matcher-politica-doble` (creada desde `feat/m16-tipo-cargador`; PR1 sigue abierto). Sin commits ni push: todo queda en el árbol de trabajo.

### Archivos nuevos (líneas autoradas, sin `.meta`)

| Archivo | Líneas | Tarea |
|---|---|---|
| `Runtime/RequirementResponse/RequirementMatcher.cs` | 75 | 2.2 |
| `Runtime/RequirementResponse/RequirementDisclosurePolicy.cs` | 25 | 2.4 |
| `Runtime/RequirementResponse/Fakes/ScriptedRequirementResponder.cs` | 89 | 2.5 |
| `Tests/EditMode/RequirementResponse/RequirementMatcherTests.cs` | 104 | 2.1 |
| `Tests/EditMode/RequirementResponse/RequirementDisclosurePolicyTests.cs` | 58 | 2.3 |
| `Tests/EditMode/RequirementResponse/ScriptedRequirementResponderTests.cs` | 96 | 2.6 |
| **Total** | **447** | presupuesto 400: **excedido en 47** |

Carpeta nueva que necesita `.meta` de Unity: `Runtime/RequirementResponse/Fakes/`.

### TDD Cycle Evidence

| Tarea | Archivo de prueba | Capa | Safety net | RED | GREEN | TRIANGULATE | REFACTOR |
|---|---|---|---|---|---|---|---|
| 2.1 / 2.2 | `RequirementMatcherTests.cs` (8 `[Test]`) | Unidad | N/A (nuevo) | Escrita antes que `RequirementMatcher` | Escrita, **pendiente de Test Runner** | 2 coincidencias con índices distintos, turno social + control, cobertura parcial, guardas, empate | Helper `Palabras` extraído (sin duplicar el `Split`) |
| 2.3 / 2.4 | `RequirementDisclosurePolicyTests.cs` (9 `[TestCase]` + 1 `[Test]`) | Unidad | N/A (nuevo) | Escrita antes que `RequirementDisclosurePolicy` | Escrita, **pendiente de Test Runner** | Matriz 3×3 completa + conteo 6/3/0 | No necesario (cuerpo de una expresión) |
| 2.6 / 2.5 / 2.7 | `ScriptedRequirementResponderTests.cs` (10 heredados + 5 propios) | Contrato | N/A (nuevo) | Escrita **antes** que el doble (ver desviación 1) | Escrita, **pendiente de Test Runner** | Umbrales `NoReceptivo`/`Neutral`/`Receptivo`, 3 `Intent`, ids conocidos y desconocido | No necesario |

### Work Unit Evidence

| Evidencia | Valor |
|---|---|
| Comando de prueba focal | Test Runner EditMode, filtros `RequirementMatcherTests`, `RequirementDisclosurePolicyTests`, `ScriptedRequirementResponderTests`, más `RequirementCaseLoaderTests` como regresión de PR1. Resultado: **no ejecutado por el agente** (pendiente del usuario) |
| Harness de runtime | N/A: sin escena ni enrutador de sala de juntas (M10/M11 no existen aún) |
| Frontera de rollback | Borrar los 6 archivos de la tabla anterior; PR1 y `NpcAi.Core` quedan intactos |

Conteo esperado en `NpcAi.RequirementResponse.Tests`: 40 pruebas (8 + 10 + 15 + 7 de PR1) y **ninguna omitida/inconclusa**. Una prueba amarilla (`Assume`) del doble sería un fallo del doble, no un resultado aceptable.

### Verificación estática (2.8, solo lo demostrable sin Unity)

- Los `.asmdef` no cambiaron: el de runtime referencia solo `NpcAi.Core`.
- `git diff` de archivos rastreados: vacío. Sin cambios en `Runtime/Core/`, `Runtime/CoreChannels/`, `Runtime/ClinicalResponse/`, `Data/Corpus/`, `Docs/`, `Tests/EditMode/Core/`. `Data/Requirements/` sin tocar.
- Sin `Debug.Log`, sin `using UnityEngine` (las dos menciones son comentarios), sin `Is.AnyOf`, sin atribución de IA.
- **Pre-chequeo informal, NO es evidencia del Test Runner**: se compiló el subconjunto de C# puro con el SDK .NET 10 (`LangVersion 9.0`) y un shim de NUnit hecho a mano fuera del repo, y se ejecutaron las fuentes reales de prueba. Compiló sin errores; 43 casos, 0 fallos, 0 inconclusos. Además 4 mutantes de la lógica (`>=` por `>`, `IsSubsetOf` por `Overlaps`, recorrido inverso del emparejador, doble que filtra la respuesta) fueron detectados por al menos una prueba. El shim y los mutantes viven en el scratchpad de la sesión, no en el repo. No cubre `RequirementCaseLoader` (usa `JsonUtility`).

### Desviaciones y decisiones

1. **Orden 2.6 antes de 2.5.** `tasks.md` lista el doble (2.5) antes de su prueba RED (2.6), lo que contradice Strict TDD. Se escribió `ScriptedRequirementResponderTests` primero (referencia una clase inexistente) y después el doble.
2. **Trazabilidad `El_Intent_no_cambia_el_emparejamiento`.** La spec asigna esa prueba a `RequirementMatcherTests`, pero `Match(string, IReadOnlyList<Requerimiento>)` (firma del diseño) no recibe `Intent`: una prueba ahí sería tautológica. La independencia de `Intent` (AD5) se prueba donde puede romperse, en `Respond`: `ScriptedRequirementResponderTests.El_Intent_no_cambia_el_requerimiento_emparejado` (usa `PreguntaFueraDeTema`, el caso que rompería un filtro). PR3 debe agregar el equivalente sobre el respondedor real; la fila de la tabla de trazabilidad de la spec debería reconciliarse en `sdd-verify`.
3. **Doble sin matiz de personalidad.** El diseño habla de "prefijos y desvíos constantes"; el doble usa un único desvío constante y ningún prefijo. Cumple el contrato y deja el matiz (`PersonalityStyleBank`) al respondedor real de PR3.
4. **Pruebas propias del doble (5).** El diseño estimaba ~20 líneas para `ScriptedRequirementResponderTests`; quedaron 96. Es la causa principal del exceso de presupuesto.
5. **Guarda de requerimiento nulo en `Match`.** Se conserva la guarda de M15 (`?.`) y se agregó su prueba; no hay requisito de spec que la exija.

### Riesgos e incidencias

- **Presupuesto excedido: 447 líneas frente a 400.** Corte natural sin retrabajo, por unidad de trabajo: **A** = `RequirementMatcher` + `RequirementDisclosurePolicy` + sus 2 archivos de prueba (262 líneas, compila solo sobre PR1); **B** = `ScriptedRequirementResponder` + sus pruebas (185 líneas, depende de A). Alternativa: aceptar `size:exception`. Decide el orquestador/mantenedor.
- **Carpetas `.meta` de PR1 sin versionar.** `Runtime/RequirementResponse.meta` y `Tests/EditMode/RequirementResponse.meta` están sin rastrear desde antes de este lote (PR1 versionó los `.meta` de archivos pero no los de carpeta; `ClinicalResponse` sí versiona los suyos). Conviene incluirlos en el PR que corresponda para no romper los GUID de carpeta.
- **Acoplamiento con el catálogo (R7 de `design.md`).** El doble reconoce `caso-juntas-01..04`; si el catálogo cambia de nomenclatura hay que moverlo en el mismo commit.
- **Dato sintético `presupuesto` solo en el doble.** El catálogo real no lo declara (decisión #66); PR3 debe documentar los `[Test]` heredados omitidos por `Assume` en `RequirementResponderTests`.

## Cierre PR2 (2026-09-21)

Jefferson confirmó 40/40 en verde, 0 omitidas (filtros `RequirementMatcherTests|RequirementDisclosurePolicyTests|ScriptedRequirementResponderTests|RequirementCaseLoaderTests`). Presupuesto: se decidió **partir en PR2a/PR2b** en vez de `size:exception` (ver nota al inicio de la Fase 2 en `tasks.md`).

- **PR2a** — rama `feat/m16-matcher-politica` (renombrada desde `feat/m16-matcher-politica-doble`, mismo punto sobre `feat/m16-tipo-cargador`). Commit `946021b`: `RequirementMatcher.cs` + `RequirementDisclosurePolicy.cs` + sus pruebas + los `.meta` de carpeta `Runtime/RequirementResponse.meta` y `Tests/EditMode/RequirementResponse.meta` que venían sin rastrear desde PR1 (riesgo cerrado). 286 líneas con `.meta`.
- **PR2b** — rama `feat/m16-doble-scripted`, creada sobre `feat/m16-matcher-politica`. Commit `17aed9e`: `Fakes/ScriptedRequirementResponder.cs` + su prueba de contrato + `Fakes.meta`. 197 líneas con `.meta`.
- Ambos commits locales, sin `push`. `Data/Requirements/` (README + 4 casos, adelanto de Fase 4) sigue sin commitear, para el PR4.

### Próximo paso

Falta `push` de ambas ramas y abrir 2 PRs en GitHub: PR2a con base `feat/m16-tipo-cargador`, PR2b con base `feat/m16-matcher-politica` (stacked-to-main, igual que M0). Después, Fase 3 (`PersonalityStyleBank`, `matices.json`, `RequirementResponder`).
