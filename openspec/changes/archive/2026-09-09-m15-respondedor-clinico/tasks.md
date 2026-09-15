# Tasks: M15 — Respondedor clínico

## Review Workload Forecast

| Field | Value |
|---|---|
| Estimated changed lines | PR1 ~200 (`ClinicalCase` + loader + matcher + doble + `ClinicalFactMatcherTests` + `ClinicalCasesDataTests`), PR2 ~200 (`ClinicalResponder` real + 2 pruebas de contrato) |
| 400-line budget risk | Medio si va en un solo PR |
| Chained PRs recommended | Sí |
| Suggested split | PR1 → PR2 |
| Chain strategy | stacked-to-main (PR1 → main, PR2 → PR1) |

### Suggested Work Units

| Unit | Meta | PR | Prueba enfocada | Frontera de rollback |
|---|---|---|---|---|
| 1 | Tipo + cargador + emparejador + doble + pruebas de datos | PR1 | EditMode: `ClinicalFactMatcherTests`, `ClinicalCasesDataTests`, `ScriptedClinicalResponderTests : ClinicalResponderContract` | Borrar `Runtime/ClinicalResponse/` y `Tests/EditMode/ClinicalResponse/`; el puerto queda sin doble real |
| 2 | `ClinicalResponder` real + matiz de personalidad | PR2 | EditMode: `ClinicalResponderTests : ClinicalResponderContract` | Borrar `ClinicalResponder.cs`; el doble sigue cubriendo el puerto |

## Phase 0: Guardrails (leer antes de escribir)

- [x] 0.1 **Dependencias mergeadas**: `2026-09-09-m0-puerto-respuesta-clinica` (define
  `IClinicalResponder`, `ClinicalResponse`, `ClinicalCaseId`, `ClinicalResponderContract`)
  y `2026-09-09-m14-catalogo-casos-clinicos` (define el esquema y los 3 JSON). Si alguno no
  está en `main`, M15 no arranca — parar y avisar. Confirmados en `main` (M14 mergeado
  2026-09-15, PR #20; el ajuste de `temperaturaC` a texto también mergeado, PR aparte por
  ser cambio de datos de M14, no de M15).
- [x] 0.2 Frontera de escritura PR1: `Runtime/ClinicalResponse/` (menos `ClinicalResponder.cs`)
  y `Tests/EditMode/ClinicalResponse/`. Frontera PR2: `Runtime/ClinicalResponse/ClinicalResponder.cs`
  y `Tests/EditMode/ClinicalResponse/ClinicalResponderTests.cs`. Más `Docs/MODULES.md` en el
  último PR.
- [x] 0.3 **Cero cambio en `NpcAi.Core`.** `IClinicalResponder`, `ClinicalResponse`,
  `ClinicalCaseId`, `NpcReply` no cambian una firma. Si M15 necesita otra firma del puerto,
  parar y avisar — es cambio de contrato v3, otro alcance.
- [x] 0.4 `Tests/EditMode/Core/ClinicalResponderContract.cs` no se modifica:
  `ClinicalResponderTests` y `ScriptedClinicalResponderTests` **heredan**, no editan.
- [x] 0.5 `NpcAi.ClinicalResponse.asmdef` referencia **solo `NpcAi.Core`**. No
  `NpcAi.Core.Channels`, no `NpcAi.Nlu`, no `NpcAi.Dialogue`. Si hace falta algo de otro
  módulo, usar su doble o es cambio de contrato.
- [x] 0.6 Este módulo es de IA (M15): `/sdd-ff` no aplica — cada PR pasa por propuesta →
  diseño → tareas → revisión humana.
- [x] 0.7 No editar `Data/Cases/*.json` desde este cambio (son de M14). Si un caso necesita
  un campo nuevo, se arregla en el cambio de M14 antes de archivar cualquiera de los dos.
  (El ajuste de `temperaturaC` se hizo como fix aparte de M14, no desde aquí.)

## Phase 1: Núcleo — tipo, cargador, emparejador, doble (PR1)

- [x] 1.1 Crear `NpcAi.ClinicalResponse.asmdef` (`references: ["NpcAi.Core"]`,
  `rootNamespace: "NpcAi.ClinicalResponse"`).
- [x] 1.2 Crear `ClinicalCase.cs`: POCO con `Paciente` y `Hechos` (lista de `Hecho`).
  **Sin** `Clave`. Namespace `NpcAi.ClinicalResponse`.
- [x] 1.3 Spike de deserialización: probar `JsonUtility` contra `Data/Cases/caso-01.json`
  real. Decisión: `JsonUtility`, con `temperaturaC` movido a texto en M14 (ver
  `design.md` → Open Questions, resuelto).
- [x] 1.4 `ClinicalFactMatcherTests.cs` — normalización (minúsculas, sin tildes, sin
  signos); "¿desde cuándo le empezó el dolor?" → campo `inicio_sintoma`;
  "buenos días, ¿cómo se siente?" → `-1`; empate por menor índice.
- [x] 1.5 `ClinicalFactMatcher.cs` — `Normalizar` + `Match` determinista (orden
  explícito, sin `Random`, sin depender de orden de `Dictionary`).
- [x] 1.6 `ClinicalCasesDataTests.cs` — carga los 3 `Data/Cases/caso-*.json`; valida
  esquema, `id` == nombre de archivo, `triajeEsperado` ∈ {`I`..`V`}, `signosVitales` con
  los 6 campos, `hechos` ≥ mínimo (8), cada `hecho` con ≥ 2 `ejemplosDePregunta`; y que
  ninguna `respuesta` contiene el `triajeEsperado` ni una `banderaRoja` del caso.
- [x] 1.7 `ClinicalCaseLoader.cs` — `TryParse` que satisface `ClinicalCasesDataTests`
  y no mapea `clave`.
- [x] 1.8 Crear `Fakes/ScriptedClinicalResponder.cs` (doble): 3 hechos embebidos, sin
  carga de archivo; `IsReady == true` tras `AssignCase` con un id conocido
  (`caso-01`/`caso-02`/`caso-03`, no cualquier id no-`None` — necesario para que
  `AssignCase_con_caso_desconocido_no_lanza_y_deja_no_listo` de la base compartida pase
  también contra el doble); determinista.
- [x] 1.9 `ScriptedClinicalResponderTests : ClinicalResponderContract` —
  `CreateSubject()` devuelve el doble (la base ya hace `AssignCase(caso-01, ...)` sola).
  Pasa la batería heredada + 2 pruebas propias (id desconocido, saludo no manejado).
- [x] 1.10 MANUAL (Editor de Unity): Test Runner → EditMode, verde en `ClinicalFactMatcherTests`,
  `ClinicalCasesDataTests`, `ScriptedClinicalResponderTests`. Confirmado por el usuario
  2026-09-15 (Run All en verde, tras corregir el choque de namespace `ClinicalResponse`).

## Phase 2: `ClinicalResponder` real (PR2)

- [x] 2.1 `ClinicalResponderTests : ClinicalResponderContract` — `CreateSubject()`
  construye `new ClinicalResponder(cargarJson)` donde `cargarJson` solo resuelve `"caso-01"`
  (cualquier otro id, incluido `"no-existe"`, devuelve `null` — necesario para que
  `AssignCase_con_caso_desconocido_no_lanza_y_deja_no_listo` siga pasando). Pruebas propias:
  la `respuesta` aparece como subcadena intacta; un saludo devuelve `Handled == false`;
  `PersonalityId.None` e `"introvertido"` no agregan matiz.
- [x] 2.2 `ClinicalResponder.cs` — constructor `Func<ClinicalCaseId,string> cargarJson` (puede
  ser `null`: `AssignCase` lo protege, nunca lanza pase lo que pase); `AssignCase` carga y
  valida (id desconocido / delegado nulo / JSON inválido ⇒ `IsReady == false`, sin lanzar);
  `Respond` normaliza, empareja con `ClinicalFactMatcher`, arma el `NpcReply` con matiz de
  personalidad (`grosero` → "Ya le dije, "; `empatico` → "Claro, doctora. "; `histerico` →
  "¡Ay, doctora! " — decisión tomada en esta entrega, no estaba en `proposal.md`;
  `introvertido`/`None`/desconocida → sin matiz) y `EmotionTag`/`AnimationCue` de tabla fija
  por `campo` (`dolor`→`dolor`/`gesto_dolor`, `mareo`→`mareo`/`gesto_mareo`, resto →
  `neutral`/`idle`). Determinista.
- [x] 2.3 (confirmado en verde por el usuario 2026-09-15: 23/23 en
  `NpcAi.ClinicalResponse.Tests.dll`, 419/419 en el proyecto completo) Verificar la
  batería heredada completa de `ClinicalResponderContract`
  (`Reporta_si_esta_listo_sin_lanzar`, `Sin_caso_asignado_Respond_devuelve_NoAplica`,
  `Respond_no_lanza_en_ningun_estado`,
  `Cuando_responde_el_texto_no_es_vacio_y_los_tags_no_son_nulos`,
  `Es_determinista_en_Handled_y_en_el_texto_para_la_misma_entrada`,
  `AssignCase_es_idempotente_con_el_mismo_par`,
  `AssignCase_con_caso_desconocido_no_lanza_y_deja_no_listo`).
- [x] 2.4 Automatizada en vez de manual (más fuerte, mismo costo):
  `Mil_llamadas_con_la_misma_entrada_no_varian_para_ninguna_personalidad` en
  `ClinicalResponderTests` — 1000 llamadas a `Respond` con la misma entrada para las 4
  personalidades → 0 variaciones en `Handled` ni en `Reply.Text`. Confirmado en verde
  2026-09-15.
- [x] 2.5 Confirmado por inspección: solo `ClinicalResponderTests`/`ClinicalCasesDataTests`
  instancian `ClinicalResponder`, y solo con datos de prueba embebidos o `Data/Cases/` vía
  `ClinicalCaseLoader` desde las pruebas de datos. Ningún `Runtime/` fuera de
  `ClinicalResponse/` lo referencia — la integración en escena sigue siendo de M11
  (`2026-09-09-m11-armado-sesion`), que todavía no existe como escena.

## Phase 3: Documentación y cierre (último PR)

- [x] 3.1 Agregar la sección **M15 — Respondedor clínico** a `Docs/MODULES.md`: carpeta,
  dueño (Nataly Álvarez, reasignado), qué hace, contrato que consume (`IClinicalResponder`),
  técnica (tabla de hechos, determinista), doble, dependencia de M14, estado real.
- [x] 3.2 Revisado: el diff acumulado de PR1+PR2 solo toca `Runtime/ClinicalResponse/` y
  `Tests/EditMode/ClinicalResponse/` (más `openspec/` y `Docs/MODULES.md` en este PR3). Sin
  `package.json`: el spike de 1.3 no obligó a Newtonsoft (se resolvió con `JsonUtility` +
  ajuste de datos en M14).

## Phase 4: Cierre (acciones del autor, cada PR)

- [x] 4.1 `git add` solo de las carpetas del PR; `git diff --cached` antes de commitear.
- [x] 4.2 Checklist "Antes de mergear" del `README.md` (pruebas propias en verde incluida
  la de contrato, diff acotado, spec/design/tasks archivados, rama al día con `main`).
- [x] 4.3 Al cerrar el último PR: mover el cambio a `openspec/changes/archive/`, crear
  `openspec/specs/respondedor-clinico-m15/spec.md` (satisface `respuesta-clinica-m0`;
  requisitos propios: respuesta desde la tabla de hechos, nunca inventa, `clave` no se
  filtra, matiz no altera el dato; trazabilidad a las pruebas), `archive-report.md`.
- [ ] 4.4 PR mergeado a `main` por el autor (regla 8). **Pendiente: este PR3.**
- [ ] 4.5 Registrar en el documento de contexto y en Engram (regla 10). No aplica para este
  agente: sin acceso a Engram/`gentle-ai` en este entorno.

## Notas

- Ningún agente ejecuta Unity ni entrena nada: M15 no entrena — es tabla + emparejador.
- PR1 puede avanzar apenas M0 y M14 estén en `main`; PR2 depende de PR1.
- Si el emparejador resulta pobre en la escena real, ampliar `ejemplosDePregunta` en
  `Data/Cases/` (M14) es edición de datos, no reapertura de M15.
