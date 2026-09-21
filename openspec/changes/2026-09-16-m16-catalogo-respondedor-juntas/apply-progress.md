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
| 3.1–3.3 (PR3a) | Hechas y verificadas | Test Runner en verde, confirmado por Jefferson (2026-09-21). Commit `607e847` en `feat/m16-matices-bank` |
| 3.4–3.5 (PR3b) | Hechas y verificadas | 6/10 heredadas en verde + 4 inconclusas por `Assume` (esperadas) + 3 propias en verde, confirmado por Jefferson (2026-09-21). Commit `4caa20b` en `feat/m16-respondedor-real` |
| 3.6a / 3.6b | Hechas | Ver sección "Cierre PR3" |
| 4.1–4.5 | Hechas por adelantado | README + 4 casos redactados, sin commitear hasta la Fase 4 |
| 4.6–4.9 | Sin empezar | |

Los `- [ ]` de `tasks.md` para 3.1–3.6 **no se tocaron**: el orquestador los marca solo tras la
evidencia del Test Runner del usuario (mismo patrón exacto que 1.1–1.6 y 2.1–2.8b).

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

- **PR2a** — rama `feat/m16-matcher-politica` (renombrada desde `feat/m16-matcher-politica-doble`, mismo punto sobre `feat/m16-tipo-cargador`). Commit `946021b`: `RequirementMatcher.cs` + `RequirementDisclosurePolicy.cs` + sus pruebas + los `.meta` de carpeta `Runtime/RequirementResponse.meta` y `Tests/EditMode/RequirementResponse.meta` que venían sin rastrear desde PR1 (riesgo cerrado). 286 líneas con `.meta`. **Abierto: [#47](https://github.com/PoliVirtualNUEVASTEC/PLN_UPM/pull/47)**, base `feat/m16-tipo-cargador`.
- **PR2b** — rama `feat/m16-doble-scripted`, creada sobre `feat/m16-matcher-politica`. Commit `17aed9e`: `Fakes/ScriptedRequirementResponder.cs` + su prueba de contrato + `Fakes.meta`. 197 líneas con `.meta`. Commit `bb3e8b7` (docs, cierre 2.1-2.8b). **Abierto: [#48](https://github.com/PoliVirtualNUEVASTEC/PLN_UPM/pull/48)**, base `feat/m16-matcher-politica` (stacked sobre PR2a).
- `Data/Requirements/` (README + 4 casos, adelanto de Fase 4) sigue sin commitear, para el PR4.

## Lote PR3 — banco de matices + adaptador real (2026-09-21)

Rama de trabajo: pendiente de crear por el orquestador sobre `feat/m16-doble-scripted` (rama de
PR2b, aún sin mergear). Sin commits ni push: todo queda en el árbol de trabajo, sobre las ramas
ya existentes de PR1/PR2a/PR2b.

### Archivos nuevos (líneas autoradas, sin `.meta`)

| Archivo | Líneas | Tarea |
|---|---|---|
| `Data/Requirements/matices.json` | 36 | 3.1 |
| `Tests/EditMode/RequirementResponse/PersonalityStyleBankTests.cs` | 136 | 3.2 |
| `Runtime/RequirementResponse/PersonalityStyleBank.cs` | 118 | 3.3 |
| `Tests/EditMode/RequirementResponse/RequirementResponderTests.cs` | 149 | 3.4 |
| `Runtime/RequirementResponse/RequirementResponder.cs` | 88 | 3.5 |
| **Total** | **527** | presupuesto 400: **excedido en 127** |

Sin carpetas nuevas: los 5 archivos caen en `Data/Requirements/`, `Runtime/RequirementResponse/`
y `Tests/EditMode/RequirementResponse/`, las 3 ya existentes desde PR1/PR2. Cada archivo trae su
`.meta` (`fileFormatVersion: 2` + `guid` nuevo; `TextScriptImporter` para el `.json`).

### TDD Cycle Evidence

| Tarea | Archivo de prueba | Capa | Safety net | RED | GREEN | TRIANGULATE | REFACTOR |
|---|---|---|---|---|---|---|---|
| 3.2 / 3.3 | `PersonalityStyleBankTests.cs` (9 `[Test]`) | Unidad | N/A (clase nueva) | Escrita antes que `PersonalityStyleBank` (clase inexistente, no compila) | Escrita, **pendiente de Test Runner** | Carga de las 4 personalidades, rotación por módulo con índices 0/1/2, índice negativo/`int.MinValue`/`int.MaxValue`, personalidad desconocida, `PersonalityId.None`, JSON basura/nulo/vacío, banco `Fallback` aislado | No necesario (métodos ya de una expresión / guard clauses) |
| 3.4 / 3.5 | `RequirementResponderTests.cs` (10 heredados + 3 propios) | Contrato | N/A (clase nueva) | Escrita antes que `RequirementResponder` (clase inexistente, no compila) | Escrita, **pendiente de Test Runner** | `Revelado` con "estadisticas", `AunNoRevelado` con el mismo requerimiento bajando la receptividad, comparación `grosero` vs `empatico` sobre la misma pregunta | No necesario |

### Work Unit Evidence

| Evidencia | Valor |
|---|---|
| Comando de prueba focal | Test Runner EditMode, filtro `PersonalityStyleBankTests\|RequirementResponderTests`, más `RequirementCaseLoaderTests\|RequirementMatcherTests\|RequirementDisclosurePolicyTests\|ScriptedRequirementResponderTests` como regresión de PR1/PR2. Resultado: **no ejecutado por el agente** (pendiente del usuario; el agente no tiene Unity) |
| Harness de runtime | N/A: sin escena ni enrutador de sala de juntas (M10/M11 no existen aún), mismo motivo que PR1/PR2 |
| Frontera de rollback | Borrar los 5 archivos de la tabla anterior; PR1/PR2a/PR2b quedan intactos (el respondedor real no reemplaza al doble, coexisten) |

Conteo esperado en `NpcAi.RequirementResponse.Tests` tras este lote: 40 (PR1+PR2) + 9
(`PersonalityStyleBankTests`) + 13 (10 heredados + 3 propios de `RequirementResponderTests`) =
**62 pruebas**. De las 13 de `RequirementResponderTests`, **9 en verde y 4 inconclusas por
`Assume`** (ver "Desviaciones y decisiones" #1 abajo — corrige el pronóstico de `tasks.md`).
Ninguna prueba debe quedar roja; una inconclusa fuera de esas 4 documentadas sí sería un fallo
real.

### Verificación estática (solo lo demostrable sin Unity)

- Los `.asmdef` de runtime y de pruebas **no se tocaron**: el de runtime sigue refiriendo solo
  `NpcAi.Core` (ya traía `noEngineReferences: false`, correcto porque `PersonalityStyleBank`
  usa `UnityEngine.JsonUtility`, igual que `RequirementCaseLoader`).
- `git status --porcelain` tras este lote: solo archivos nuevos bajo `Data/Requirements/`,
  `Runtime/RequirementResponse/` y `Tests/EditMode/RequirementResponse/` (más ruido preexistente
  de otras tareas/módulos ajenos a este cambio, ya presente antes de esta sesión). Ningún archivo
  de `Runtime/Core/`, `Runtime/CoreChannels/`, `Runtime/ClinicalResponse/`, `Runtime/Receptivity/`
  ni de otro módulo aparece modificado.
- `rg -i "Debug\.Log|Is\.AnyOf|Co-Authored-By|Claude|Anthropic"` sobre los 5 archivos nuevos: sin
  coincidencias.
- No hay pre-chequeo de compilación fuera de Unity para este lote (a diferencia de PR2, no se
  rearmó el shim de NUnit): la revisión fue manual línea por línea contra las firmas exactas de
  `design.md` (`TryParse`, `PrefijoRevelado`, `Desvio`, constructor de `RequirementResponder`) y
  contra el patrón ya verificado de `ClinicalResponder`/`RequirementCaseLoader`. Riesgo residual:
  un error de compilación solo lo revela el Test Runner real.

### Desviaciones y decisiones

1. **Corrección al pronóstico de `tasks.md` sobre cuántas pruebas heredadas quedan `Assume`-omitidas.**
   `tasks.md` (tarea 3.4, redactada antes de escribir esta clase) listó 6 pruebas heredadas como
   dependientes de "presupuesto" (`Cuando_revela_...`, `Cuando_aun_no_revela_...`,
   `El_RequirementId_esta_poblado_...`, `Es_determinista_...`, `Baja_receptividad_...`,
   `AssignCase_es_idempotente_...`) y solo 4 en verde. Leyendo linea por linea
   `Tests/EditMode/Core/RequirementResponderContract.cs` (M0, congelada) antes de escribir el
   fixture: **`Es_determinista_en_Outcome_texto_y_RequirementId_para_la_misma_entrada`** y
   **`AssignCase_es_idempotente_con_el_mismo_par`** NO tienen ningún `Assume.That(...Outcome...)`
   — solo comparan que 2-3 llamadas den el mismo resultado entre sí, y esa consistencia se
   cumple igual de bien cuando las 3 llamadas dan `NoAplica` (que es lo que pasa con el fixture
   real, sin "presupuesto"). Por lo tanto corren en **verde**, no inconclusas. Las que SI
   dependen de un `Assume` sobre `Outcome == Revelado`/`AunNoRevelado`/`!= NoAplica` son
   exactamente 4: `Cuando_revela_el_texto_no_es_vacio_y_los_tags_no_son_nulos`,
   `Cuando_aun_no_revela_responde_con_un_desvio_no_vacio`,
   `El_RequirementId_esta_poblado_si_y_solo_si_el_turno_aplica`,
   `Baja_receptividad_y_alta_receptividad_no_dan_el_mismo_texto`. Resultado real esperado: **6 de
   10 heredadas en verde** (no 4), **4 inconclusas por `Assume`** (no 6). Documentado en el
   comentario XML de `RequirementResponderTests.cs` y aquí; no se oculta ni se fuerza. Bandera
   para `sdd-verify`: la tabla de trazabilidad de la spec `respondedor-requerimientos-m16` y la
   nota de `tasks.md` deberían reconciliarse con este conteo real.
2. **`PersonalityStyleBank.Fallback` es un banco vacío, no un caso especial en código.** AD10
   pide "un prefijo vacío y un desvío neutro constante" para `matices.json` inválido/ausente/sin
   la personalidad. Se implementó como una instancia estática de la misma clase con un
   diccionario vacío: `PrefijoRevelado`/`Desvio` ya degradan a ese comportamiento para cualquier
   personalidad no encontrada, así que el `Fallback` lo reutiliza sin duplicar lógica. No es una
   desviación del diseño, es la forma más simple de cumplirlo con el mismo código de producción.
3. **Sin prueba dedicada de "el banco no conoce `Requerimiento.Respuesta`".** AD6 lo garantiza
   por construcción (la firma de `Desvio` no recibe el texto del requerimiento, solo el índice) y
   ya lo ejerce indirectamente `RequirementResponderTests.La_respuesta_del_caso_no_aparece_cuando_AunNoRevelado`.
   No se agregó una prueba de reflexión/análisis estático adicional: no la pide `tasks.md`.

### Riesgos e incidencias

- **Presupuesto excedido: 527 líneas autoradas frente a 400 (127 de exceso), por encima incluso
  del rango recalculado de `tasks.md` (420–460).** La causa principal es
  `RequirementResponderTests.cs` (149 líneas): hereda 10 pruebas de contrato, embebe 2 fixtures
  JSON completos (caso + matices) para no depender de IO, y documenta en un comentario XML largo
  la corrección de la desviación #1 de arriba. **No se decide la partición en este lote** (mismo
  criterio que PR2): el corte natural, sin retrabajo, por unidad de trabajo, es:
  - **A** = `Data/Requirements/matices.json` + `PersonalityStyleBankTests.cs` +
    `PersonalityStyleBank.cs` (290 líneas, compila solo sobre PR1/PR2, no depende de
    `RequirementResponder`).
  - **B** = `RequirementResponderTests.cs` + `RequirementResponder.cs` (237 líneas, depende de
    `PersonalityStyleBank` de A).
  Ambas unidades caen bajo el presupuesto de 400 por separado. Alternativa: aceptar
  `size:exception` para un PR3 único de 527 líneas. Decide el orquestador/mantenedor, igual que
  con PR2 (447 → PR2a/PR2b).
- **Riesgo de compilación sin verificación previa.** A diferencia de PR2, este lote no se corrió
  contra un shim de NUnit fuera de Unity; la única verificación es lectura manual contra las
  firmas de `design.md`. El Test Runner del usuario es la primera compilación real de este lote.
- **Acoplamiento con el catálogo real, ahora también desde el respondedor real.** Igual que R7 de
  `design.md` ya advertía para el doble: si `caso-juntas-01.json` cambia sus ids o ejemplos de
  pregunta, hay que actualizar el fixture embebido de `RequirementResponderTests.cs` en el mismo
  commit o sus 3 pruebas propias empiezan a fallar (no a quedar `Assume`, porque no usan
  `Assume`).
- **Gap conocido y ya documentado (decisión #66): sin "presupuesto" en el catálogo real.** No es
  nuevo de este lote, pero aquí se materializa por primera vez contra la implementación real (ver
  desviación #1).

## Cierre PR3 (2026-09-21)

Jefferson confirmó el Test Runner: exactamente las 4 pruebas heredadas esperadas quedaron en
naranja (`Assume`, ver corrección de conteo en `tasks.md`), el resto verde. Presupuesto: se
decidió **partir en PR3a/PR3b** (mismo criterio que PR2, exceso mayor esta vez: 127 líneas).

- **PR3a** — rama `feat/m16-matices-bank` (renombrada desde `feat/m16-matices-respondedor`,
  mismo punto sobre `feat/m16-doble-scripted`). Commit `607e847`: `Data/Requirements/matices.json`
  + `PersonalityStyleBank.cs` + su prueba + `Data/Requirements.meta` (folder meta, primera vez
  que ese directorio entra a git). 309 líneas con `.meta`. **Abierto:
  [#50](https://github.com/PoliVirtualNUEVASTEC/PLN_UPM/pull/50)**, base `feat/m16-doble-scripted`.
- **PR3b** — rama `feat/m16-respondedor-real`, creada sobre `feat/m16-matices-bank`. Commit
  `4caa20b`: `RequirementResponder.cs` + su prueba de contrato. 241 líneas con `.meta`. Commit
  `fe93251` (docs, cierre 3.1-3.6b). **Abierto:
  [#51](https://github.com/PoliVirtualNUEVASTEC/PLN_UPM/pull/51)**, base `feat/m16-matices-bank`
  (stacked sobre PR3a).
- `Data/Requirements/README.md` + `caso-juntas-01..04.json` (adelanto de Fase 4) siguen sin
  commitear.

### Próximo paso

Fase 4 (catálogo de datos + `RequirementCasesDataTests` + `Docs/MODULES.md`), rama nueva sobre
`feat/m16-respondedor-real` una vez que #50/#51 avancen en revisión, ya con el README y los 4
`.json` de `Data/Requirements/` redactados desde PR1 pero sin commitear.
