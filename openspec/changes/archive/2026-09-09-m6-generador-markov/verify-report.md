```yaml
schema: gentle-ai.verify-result/v1
verdict: pass
mode: implementation  # codigo + pruebas nuevas reales (no doc-only como M5)
blockers: 0
critical_findings: 0
requirements: 7/7
scenarios: 14/14
runtime_gate: human EditMode GREEN (19 pruebas de NpcAi.Dialogue.Tests: 6 MarkovChainBuilderTests + 8 MarkovDialogueGeneratorTests + 5 ScriptedDialogueGeneratorTests preexistentes)
agent_checks:
  - "git diff --cached --stat -> solo Data/Dialogue/, Runtime/Dialogue/Markov*.cs, Tests/EditMode/Dialogue/Markov*.cs, Docs/MODULES.md, openspec/changes/2026-09-09-m6-generador-markov/ (exit 0)"
  - "git status Runtime/Core Runtime/CoreChannels Tests/EditMode/Core -> sin cambios (no es cambio de contrato) (exit 0)"
  - "codegraph_explore ScriptedDialogueGenerator -> sin tocar; IDialogueGenerator/NpcReply sin cambio de firma"
  - "python -c json.load sobre los 4 Data/Dialogue/*.json -> validos, 6 frases por bloque, 12 bloques"
findings_corrected_pre_archive: 0
```

## Verification Report

**Change**: 2026-09-09-m6-generador-markov
**Capability**: `generador-dialogo-m6` (nueva)
**Mode**: Implementacion. Motor real (`MarkovDialogueGenerator`, `MarkovChainBuilder`) + 14
pruebas EditMode nuevas + corpus semilla `Data/Dialogue/`. Implementacion directa (sin ciclo
`sdd-apply`) + formalizacion OpenSpec, igual que M4 y M5. RDD apagado (Opcion B, sin receipt).

### Contexto

M6 se implemento de forma directa siguiendo `proposal.md` y `design.md` del cambio ya
planificado. El usuario corrio EditMode > Run en Unity 6 y confirmo **19 pruebas en verde** en
el ensamblado `NpcAi.Dialogue.Tests` (las 14 nuevas de M6 mas las 5 heredadas del contrato que
ya pasaba `ScriptedDialogueGenerator`). No hay `sdd-attempt` runtime que consumir (RDD off).

### Hallazgos corregidos antes de archivar

Ninguno. (En un mensaje de chat previo el agente dijo "11 pruebas" por mala aritmetica; el conteo
real siempre fue 14 nuevas / 19 en la assembly.)

### Write-Boundary Audit

`git diff` del cambio (sin ruido `.meta` preexistente de Unity en `openspec/**`):

| Path | Tipo | Ruta autorizada |
|---|---|---|
| `Data/Dialogue/README.md` + `grosero/histerico/introvertido/empatico.json` | nuevo | OK (corpus de M6) |
| `Runtime/Dialogue/MarkovChainBuilder.cs` | nuevo | OK (frontera PR2) |
| `Runtime/Dialogue/MarkovDialogueGenerator.cs` | nuevo | OK (frontera PR2) |
| `Tests/EditMode/Dialogue/MarkovChainBuilderTests.cs` | nuevo | OK (frontera PR2) |
| `Tests/EditMode/Dialogue/MarkovDialogueGeneratorTests.cs` | nuevo | OK (frontera PR2) |
| `openspec/changes/2026-09-09-m6-generador-markov/specs/generador-dialogo-m6/spec.md` | nuevo | OK |
| `openspec/changes/2026-09-09-m6-generador-markov/{tasks.md,verify-report.md}` | mod/nuevo | OK |
| `Docs/MODULES.md` (seccion M6) | mod | OK (frontera PR3) |

- Cero `Runtime/Core/`, cero `Runtime/CoreChannels/`: **no es cambio de contrato**, sin gate de gobernanza previo.
- `Runtime/Dialogue/Fakes/ScriptedDialogueGenerator.cs` sin tocar (Out of Scope, guardrail 0.2).
- `Tests/EditMode/Core/DialogueGeneratorContract.cs` sin tocar: `MarkovDialogueGeneratorTests` **hereda**, no edita (guardrail 0.4).

**Result: PASS.**

### Spec Compliance Matrix — generador-dialogo-m6 (7 requisitos / 14 escenarios)

| # | Requisito | Escenario | Prueba en verde | Resultado |
|---|---|---|---|---|
| 1 | Un adaptador nuevo detras de un puerto que no cambia | El generador real hereda el mismo contrato que el doble | 5 heredadas de `DialogueGeneratorContract` en `MarkovDialogueGeneratorTests` + `ScriptedDialogueGeneratorTests` sigue verde | COMPLIANT |
| 2 | Nunca devuelve texto vacio, con respaldo escalonado | Los tres estados de receptividad nunca dan texto vacio | `DialogueGeneratorContract.Nunca_devuelve_texto_vacio` | COMPLIANT |
| 2 | | En volumen, ninguna personalidad ni estado da texto vacio | `MarkovDialogueGeneratorTests.Ninguna_personalidad_ni_estado_devuelve_texto_vacio_en_volumen` (3000 llamadas) | COMPLIANT |
| 2 | | Un id desconocido cae en el respaldo sin lanzar | `MarkovDialogueGeneratorTests.Un_id_desconocido_usa_el_respaldo_y_no_lanza` | COMPLIANT |
| 2 | | Funciona sin personalidad asignada | `DialogueGeneratorContract.Funciona_sin_personalidad_asignada` | COMPLIANT |
| 3 | Las etiquetas nunca son nulas y son fijas por receptividad | Las etiquetas no son nulas | `DialogueGeneratorContract.Las_etiquetas_nunca_son_nulas` + inspeccion de `EtiquetasDe` vs `ScriptedDialogueGenerator` | COMPLIANT |
| 4 | Receptivo y NoReceptivo producen texto distinto | Un NPC no receptivo no responde igual que uno receptivo | `DialogueGeneratorContract.Un_NPC_no_receptivo_no_responde_igual_que_uno_receptivo` | COMPLIANT |
| 5 | Generate NO es determinista | Dos llamadas iguales no estan obligadas a coincidir | `DialogueGeneratorContract.Generate_no_esta_obligado_a_ser_determinista` | COMPLIANT |
| 6 | La cadena de Markov solo recombina el corpus semilla | El paseo solo usa palabras del corpus | `MarkovChainBuilderTests.El_paseo_solo_usa_palabras_del_corpus_semilla` (200 paseos) | COMPLIANT |
| 6 | | El paseo respeta la cota de largo maximo | `MarkovChainBuilderTests.El_paseo_respeta_la_cota_de_largo_maximo` | COMPLIANT |
| 6 | | Corpus degenerado devuelve cadena vacia sin lanzar | `MarkovChainBuilderTests.Corpus_vacio_...`, `...Corpus_nulo_...`, `...Una_frase_de_una_sola_palabra_...` | COMPLIANT |
| 6 | | Cota menor que dos devuelve cadena vacia | `MarkovChainBuilderTests.Largo_maximo_menor_que_dos_produce_paseo_vacio` | COMPLIANT |
| 7 | El corpus semilla es dato de M6 | El corpus de disco trae exactamente las cuatro personalidades | `MarkovDialogueGeneratorTests.El_corpus_de_disco_trae_exactamente_las_cuatro_personalidades_de_M5` | COMPLIANT |
| 7 | | El generador consume el corpus indexado por personalidad y receptividad | `MarkovDialogueGeneratorTests.Ninguna_personalidad_ni_estado_...` (recorre las 4 x 3 combinaciones) + inspeccion de `ResolverBloque` (esquema/minimo por bloque: `design.md` "File Inventory" + `Data/Dialogue/README.md`) | COMPLIANT |

**Compliance summary: 14/14 escenarios con prueba cubridora en verde (3 combinan con inspeccion:
tabla de etiquetas, esquema del corpus); 7/7 requisitos cubiertos; 0 GAP.**

### Coherencia con el diseno

| Decision | Seguida? | Nota |
|---|---|---|
| AD1 Markov de palabras orden 2 (bigramas) | Si | `MarkovChainBuilder`: `Dictionary<(string,string), List<string>>`, sentinel null = fin de frase |
| AD2 indexar por `(PersonalityId, Receptivity)` — 12 bloques | Si | `_bloques[(id, estado)]`; `None`/desconocido -> respaldo agrupado por estado |
| AD3 `IntentResult` se acepta pero no condiciona | Si | Parametro presente, no se usa para elegir bloque; documentado en spec R7 |
| AD4 `Data/Dialogue/<personalidad>.json`, 1 archivo por personalidad, 3 listas adentro | Si | 4 archivos + README; esquema `{ personalidad, Receptivo[], Neutral[], NoReceptivo[] }` |
| AD5 reintento acotado + frase semilla verbatim ante paseo degenerado | Si | `GenerarTexto`: 5 reintentos -> `FraseSemillaAlAzar` -> `RespaldoDuro` (frase fija real, no placeholder) |
| AD6 `EmotionTag`/`AnimationCue` de tabla fija por `Receptivity` | Si | `EtiquetasDe`, mismos valores que el doble (receptivo/asentir, molesto/cruzar_brazos, neutral/idle) |
| AD7 gate = verde EditMode humano + volumen | Si | 19 pruebas verde atestiguadas; `Ninguna_personalidad_ni_estado_...` = 3000 llamadas dentro de NUnit |

### Discrepancia detectada en el design (no bloqueante)

`design.md` seccion "Unidad de modulo" afirma que `Runtime/Dialogue` tiene
`noEngineReferences: true`. El asmdef real es `noEngineReferences: false` (refs `NpcAi.Core` +
`NpcAi.Core.Channels`), coherente con el esbozo de interfaz del propio design que usa `TextAsset`.
`MarkovDialogueGenerator` usa `UnityEngine` (`TextAsset`, `JsonUtility`); `MarkovChainBuilder`
queda como C# puro. No afecta el resultado; conviene corregir la frase del design en un ajuste
menor de docs.

### Issues Found

**CRITICAL**: Ninguno.

**WARNING**: Ninguno.

**SUGGESTION** (no bloqueante)
1. `design.md` "Unidad de modulo": corregir `noEngineReferences: true` -> `false`.
2. Corpus semilla chico (6 frases por bloque): la calidad literaria del texto generado es de
   iteracion de datos, no de este cambio (proposal -> Risks). Ampliar `Data/Dialogue/` no reabre M6.
3. `IntentResult` sin usar: condicionar la cadena por `Intent` es mejora de seguimiento
   (proposal -> Out of Scope).
4. Prueba manual de 1000 llamadas (task 2.5): cubierta en espiritu por la prueba de volumen NUnit
   de 3000 llamadas; el script/boton de Editor separado queda opcional para el equipo.

### Verdict

**PASS — archive-ready.**

Los 7 requisitos y los 14 escenarios de `generador-dialogo-m6` trazan cada uno a al menos una
prueba EditMode en verde (3 combinan con inspeccion de tabla/esquema). La frontera de escritura
esta contenida en `Data/Dialogue/`, `Runtime/Dialogue/`, `Tests/EditMode/Dialogue/`, `Docs/` y
`openspec/`. No es cambio de contrato (no toca `Runtime/Core/`). `ScriptedDialogueGenerator`,
`IDialogueGenerator` y `NpcReply` intactos.

### Completeness (tasks.md)

| Fase | Estado | Nota |
|------|--------|------|
| 0. Guardrails | `[x]` 0.1-0.6 | frontera confirmada; no es cambio de contrato |
| 1. Corpus (PR1) | `[x]` 1.1-1.3 | 1.4 (revision cruzada 2o integrante) abierta — accion del equipo |
| 2. Codigo + tests (PR2) | `[x]` 2.1-2.4, 2.6 | 2.5 `[~]` cubierto por la prueba de volumen NUnit |
| 3. Spec + docs (PR3) | `[x]` 3.1-3.3 | |
| 4. Cierre | `[x]` 4.3 (`sdd-archive` inline) | 4.1-4.2, 4.4 = acciones del autor pre-merge |

### Next steps for the pipeline

`sdd-archive` inline — el cambio es archive-eligible.
