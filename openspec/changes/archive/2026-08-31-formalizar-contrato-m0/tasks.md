# Tasks: Formalizar el contrato v1 del Nucleo M0

## Review Workload Forecast

| Field | Value |
|-------|-------|
| Estimated changed lines | 600-700 autorales (~6 de runtime; el resto pruebas + changelog) |
| 400-line budget risk | High |
| Chained PRs recommended | No |
| Suggested split | UN PR `size:exception`; grupos A/B/C/D como commits revertibles internos |
| Delivery strategy | exception-ok |
| Chain strategy | size-exception |

Decision needed before apply: No
Chained PRs recommended: No
Chain strategy: size-exception
400-line budget risk: High

Entrega: UN solo PR marcado `size:exception`, aceptado por el usuario 2026-08-31. Revision pesada asumida. No queda ninguna decision de encadenamiento pendiente.

### Suggested Work Units

| Unit | Goal | PR | Focused test command | Runtime harness | Rollback boundary |
|------|------|----|----------------------|-----------------|-------------------|
| A | Meta del contrato: G1, G2, G15 | PR unico, commit A | EditMode > Run All (gate humano) | N/A: sin capa e2e automatizada; M11 Samples~/Harness es cableado manual | `git revert` commit A; cero runtime, cero modulos |
| B | Canales: G3 | PR unico, commit B | EditMode > Run All (gate humano) | N/A (idem) | `git revert` commit B; el asmdef de prueba vuelve a 3 referencias |
| C | Tipos y DTO: G4, G5, G6, G12, G13, G14 | PR unico, commit C | EditMode > Run All (gate humano) | N/A (idem) | `git revert` commit C; `Dtos.cs` vuelve a `a7a0590` |
| D | Semantica de puertos: G7-G11 | PR unico, commit D | EditMode > Run All (gate humano) | N/A (idem) | `git revert` commit D; las bases corren menos `[Test]` |

## Trazabilidad (gap -> requisito de spec -> grupo)

| Gap | Requisito | Grupo |
|-----|-----------|-------|
| G1 | Version del contrato | A |
| G2 | Pureza del ensamblado NpcAi.Core | A |
| G15 | Enforcement/doc de invariantes por puerto (changelog) | A |
| G3 | Semantica de EventChannel + Canales concretos del nucleo | B |
| G4 | Enums congelados en v1 | C |
| G5 | DTO NpcReply | C |
| G6 | DTO ReceptivityChange | C |
| G13 | DTO IntentResult | C |
| G12 | Politica de igualdad de los DTO (changelog + 2 pruebas de reflexion, remediacion verify) | C |
| G14 | Clamp de floats diferido a v2 (solo changelog) | C |
| G7 | Puerto IReceptivityEngine | D |
| G8 | Puerto IDialogueGenerator | D |
| G9 | Puerto IIntentClassifier | D |
| G10 | Puerto ISpeechToText | D |
| G11 | Puerto IScenarioObjective | D |

Ya cubiertos por `ContractTypeTests`/bases actuales, sin tarea nueva: Puerto IPhysicalActionSource, Puerto INpcPresenter.

Remediacion post-`sdd-verify` (2026-08-31): `sdd-verify` marco como CRITICO que el requisito `DTO Utterance` (2 escenarios MUST) no tenia prueba, pese a figurar aqui como "ya cubierto". Cerrado en `Tests/EditMode/Core/ContractTypeTests.cs` (sin cambio de runtime): `Utterance_IsEmpty_refleja_el_texto`, `Utterance_guarda_los_campos_sin_recortar_ni_validar_rangos`. Ademas se cerraron las advertencias W1 (G12: `Solo_PersonalityId_implementa_IEquatable_en_la_v1`, `Los_DTO_usan_igualdad_estructural_por_defecto`) y W4 (`PersonalityId_nulo_o_solo_espacios_es_None`). `Utterance` es byte-identico a `a7a0590`, asi que estas pruebas son de caracterizacion y DEBERIAN quedar verdes en la primera corrida.

## Phase 0: Guardrails (leer antes de editar; sin codigo)

- [x] 0.1 Alcance de escritura: SOLO `Runtime/Core/`, `Runtime/CoreChannels/`, `Tests/EditMode/Core/`, `Docs/CONTRACT-CHANGELOG.md`. Cualquier tarea que toque otra ruta esta mal cortada: parar y avisar. (Confirmado: el diff solo toca `Runtime/Core/Dtos.cs`, `Runtime/Core/Ports.cs`, `Tests/EditMode/Core/*`, `Docs/CONTRACT-CHANGELOG.md`.)
- [x] 0.2 Prohibido agregar miembros `abstract` nuevos a las bases `*Contract.cs`: solo metodos `[Test]` no abstractos. (Cumplido: cero miembros `abstract` nuevos; solo `[Test]` concretos y un `private sealed class` local.)
- [x] 0.3 `Contract.Version` se queda en `1`. NO cambiar `const` a `static readonly`. (Sin cambios en `Contract.cs`.)
- [x] 0.4 Regla de operacion: si un `[Test]` nuevo pone rojo a uno de los 7 dobles, NO arreglar el doble aqui; registrar un cambio SDD para el dueno de ese modulo y ajustar o diferir la asercion. (No se detecto ningun conflicto: ver 4.7. G8 se caracteriza sin asertar (des)igualdad para no romper el doble determinista de M6.)
- [x] 0.5 Colision de nombres: calificar `Core.Receptivity` en todo archivo de prueba nuevo o modificado. (Cumplido en `ContractTypeTests.cs`, `EventChannelTests.cs`, `ReceptivityEngineContract.cs`.)
- [ ] 0.6 Gobernanza: es cambio de contrato; integra SOLO en la ventana del lunes con co-aprobacion de todos los duenos de modulo.  <!-- GOBERNANZA MANUAL: agendar ventana del lunes + co-aprobacion -->


## Phase 1: Grupo A - Meta del contrato (commit A) [paralelizable con B y D]

- [x] 1.1 RED (G1): crear `Tests/EditMode/Core/ContractVersionChangelogTests.cs`: `Path.GetFullPath("Packages/com.poli.npc-ai/Docs/CONTRACT-CHANGELOG.md")`, parsear el mayor `## v<N>`, `Assert.AreEqual(Contract.Version, N)`; `Assert.Ignore` si el archivo no existe.
- [x] 1.2 RED (G2): crear `Tests/EditMode/Core/CoreAssemblyPurityTests.cs`: reflexion sobre `typeof(NpcAi.Core.Contract).Assembly.GetReferencedAssemblies()`; cero nombres `UnityEngine*`/`UnityEditor*`, cero `NpcAi.*` distinto de si mismo.
- [x] 1.3 GREEN/doc (G15): reescribir la seccion v1 de `Docs/CONTRACT-CHANGELOG.md`: invariantes por puerto, `Score`/`ReasonCode` como diagnostico no tipado, asimetria de determinismo (`Classify` determinista vs `Generate` no), centinelas (`0` seguro, `Ninguna` no se emite), politica de igualdad de DTO, nota sobre `const`, puntero de enforcement a `Tests/EditMode/Core/`.
- [x] 1.4 Gate humano: EditMode > Run All; commit A en verde. (2026-08-31: EditMode 100% verde tras fix de AssertEnumCongelado.)


## Phase 2: Grupo B - Canales (commit B) [paralelizable con A y D]

- [x] 2.1 (G3) Editar `Tests/EditMode/Core/NpcAi.Core.Tests.asmdef`: agregar `"NpcAi.Core.Channels"` a `references`.
- [x] 2.2 RED (G3): crear `Tests/EditMode/Core/EventChannelTests.cs` via `ScriptableObject.CreateInstance<T>()`: `Raise` sin suscriptores no lanza; `Raise` entrega el payload a cada suscriptor; `Unsubscribe` corta la entrega; `Subscribe(null)`/`Unsubscribe(null)` no lanzan; los 5 canales concretos `sealed` heredan `EventChannel<T>` con el `T` esperado; `OnDisable` limpia (`DestroyImmediate` + `Raise` posterior no invoca al handler viejo).
- [x] 2.3 GREEN (G3): `Runtime/CoreChannels/EventChannel.cs` ya satisface todas las aserciones (guardas nulas en `Subscribe`/`Unsubscribe`, `Raise` con `_listeners?.Invoke`, `OnDisable => _listeners = null`). CERO cambio de runtime; las pruebas de `EventChannelTests` son de caracterizacion. Pendiente de confirmar en Unity 6 que `DestroyImmediate` dispara `OnDisable` sincrono antes de la asercion (open question D6).
- [x] 2.4 Gate humano: EditMode > Run All; commit B en verde. (2026-08-31: EditMode 100% verde.)


## Phase 3: Grupo C - Tipos y DTO (commit C) [unico ciclo runtime; orden interno estricto]

- [x] 3.1 RED (G4): en `Tests/EditMode/Core/ContractTypeTests.cs` agregar 4 pruebas de congelamiento de enum: `Enum.GetNames`/`GetValues` vs lista ordenada esperada (nombre=valor) + conteo para `Intent`, `Tone`, `PhysicalAction`, `Core.Receptivity`; `-1` y `1` explicitos en `Receptivity`.
- [x] 3.2 RED (G5, G6): en `ContractTypeTests.cs` agregar aserciones de guarda nula: `new NpcReply(null, null, null)` -> tres `""` e `IsEmpty == true`; `new ReceptivityChange(Neutral, Neutral, 0, null).ReasonCode == ""`. Fallan hoy. (Autoradas ANTES del cambio a Dtos.cs.)
- [x] 3.3 GREEN (G5, G6): en `Runtime/Core/Dtos.cs`, `Text = text ?? string.Empty` (NpcReply) y `ReasonCode = reasonCode ?? string.Empty` (ReceptivityChange). UNICO cambio de runtime, 2 lineas efectivas.
- [x] 3.4 RED -> GREEN (G13): en `ContractTypeTests.cs`, 2 aserciones de que `IntentResult.Unknown(latencyMs)` preserva `LatencyMs` y mantiene `Desconocida`/`Neutral`/`0f`. (Caracterizacion: verde con el codigo actual.)
- [x] 3.5 Doc (G12, G14): registrar `IEquatable`/`GetHashCode` de DTO diferido a v2 y clamp de floats diferido, SOLO en `Docs/CONTRACT-CHANGELOG.md`. Sin codigo. (Hecho en la reescritura del changelog del grupo A.)
- [x] 3.6 REFACTOR: unificar las tablas esperadas de enum si quedan duplicadas; sin cambiar aserciones. (Helper unico `AssertEnumCongelado`; una tabla por enum, sin duplicacion.)
- [x] 3.7 Gate humano: EditMode > Run All; commit C en verde. (2026-08-31: EditMode 100% verde.)


## Phase 4: Grupo D - Semantica de puertos (commit D) [paralelizable con A y B]

- [x] 4.1 Editar `Runtime/Core/Ports.cs`: solo notas XML-doc para G7, G8, G9, G11 (sin cambio de comportamiento).
- [x] 4.2 RED (G7): `Tests/EditMode/Core/ReceptivityEngineContract.cs`, `[Test]` no abstracto: `Current` es `Neutral` hasta `Reset`; `Reset(pid)` determinista e idempotente.
- [x] 4.3 RED (G8): `Tests/EditMode/Core/DialogueGeneratorContract.cs`, `[Test]` no abstracto: `Generate` NO exige determinismo (dos llamadas con la misma entrada PUEDEN diferir).
- [x] 4.4 RED (G9): `Tests/EditMode/Core/IntentClassifierContract.cs`, `[Test]` no abstracto: `Classify` NO DEBE lanzar en ningun estado; `private sealed class ClasificadorNoListo : IIntentClassifier` DENTRO del archivo para el camino `IsReady == false`; DEBERIA devolver `IntentResult.Unknown`.
- [x] 4.5 RED (G10): `Tests/EditMode/Core/SpeechToTextContract.cs`, `[Test]` no abstracto: nada antes del primer `StartListening`; `StartListening` doble; `Start`/`Stop`/`Start` reanuda; fan-out multi-suscriptor; emitir sin suscriptores no lanza.
- [x] 4.6 RED (G11): `Tests/EditMode/Core/ScenarioObjectiveContract.cs`, `[Test]` no abstracto: `IsComplete` si y solo si `Progress01 == 1` (tol `1e-4`); completitud REVERSIBLE; `Notify(default)` no-op.
- [x] 4.7 GREEN (G7-G11): los `[Test]` heredados corren contra los 7 dobles sin tocarlos. Verificado por lectura: los 7 dobles satisfacen los nuevos invariantes (`ScriptedReceptivityEngine.Current` arranca Neutral e `Reset` es idempotente; `ScriptedSpeechToText.Emit` respeta `IsListening`; `ScriptedIntentClassifier.Classify` nunca lanza; ambos `ScriptedScenarioObjective` cumplen el si-y-solo-si con `_pasos` acotado en `[0,4]` y `Notify(default)` no-op). NINGUN conflicto de la regla 0.4 detectado; ningun doble tocado.
- [x] 4.8 Gate humano: EditMode > Run All; commit D en verde. (2026-08-31: EditMode 100% verde.)


## Phase 5: Cierre

- [x] 5.1 `git diff` (sin `--cached`, aun sin stage por instruccion): confirmado que solo se tocan `Runtime/Core/Dtos.cs`, `Runtime/Core/Ports.cs`, `Tests/EditMode/Core/*` (10 modificados + 3 nuevos con su `.meta`), `Docs/CONTRACT-CHANGELOG.md`. Cero rutas fuera de las 4 autorizadas; cero `Runtime/*/Fakes/`; cero otro modulo.
- [x] 5.2 Gate humano final: un humano corre EditMode > Run All en Unity 6 y registra verde. (2026-08-31: 28/28 verde en NpcAi.Core.Tests + DLL de modulo. D6 confirmado empiricamente. Objetivo nativo sdd-attempt settled `passed`.)
- [ ] 5.3 Agendar la integracion en la ventana del lunes con co-aprobacion registrada de todos los duenos de modulo.  <!-- GOBERNANZA MANUAL PENDIENTE -->


## Notas TDD

- Ciclo RED -> GREEN real solo en Phase 3 (3.2 antes de 3.3). El resto son pruebas de caracterizacion del comportamiento ya existente: PUEDEN quedar verdes en la primera corrida.
- Ningun agente puede ejecutar Unity: cada "Gate humano" es una compuerta manual.
- Grupos A, B y D son independientes en tiempo de compilacion y se pueden desarrollar en paralelo; C es el unico con cambio de runtime.
