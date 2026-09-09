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

- [ ] 0.1 **Dependencias mergeadas**: `2026-09-09-m0-puerto-respuesta-clinica` (define
  `IClinicalResponder`, `ClinicalResponse`, `ClinicalCaseId`, `ClinicalResponderContract`)
  y `2026-09-09-m14-catalogo-casos-clinicos` (define el esquema y los 3 JSON). Si alguno no
  está en `main`, M15 no arranca — parar y avisar.
- [ ] 0.2 Frontera de escritura PR1: `Runtime/ClinicalResponse/` (menos `ClinicalResponder.cs`)
  y `Tests/EditMode/ClinicalResponse/`. Frontera PR2: `Runtime/ClinicalResponse/ClinicalResponder.cs`
  y `Tests/EditMode/ClinicalResponse/ClinicalResponderTests.cs`. Más `Docs/MODULES.md` en el
  último PR.
- [ ] 0.3 **Cero cambio en `NpcAi.Core`.** `IClinicalResponder`, `ClinicalResponse`,
  `ClinicalCaseId`, `NpcReply` no cambian una firma. Si M15 necesita otra firma del puerto,
  parar y avisar — es cambio de contrato v3, otro alcance.
- [ ] 0.4 `Tests/EditMode/Core/ClinicalResponderContract.cs` no se modifica:
  `ClinicalResponderTests` y `ScriptedClinicalResponderTests` **heredan**, no editan.
- [ ] 0.5 `NpcAi.ClinicalResponse.asmdef` referencia **solo `NpcAi.Core`**. No
  `NpcAi.Core.Channels`, no `NpcAi.Nlu`, no `NpcAi.Dialogue`. Si hace falta algo de otro
  módulo, usar su doble o es cambio de contrato.
- [ ] 0.6 Este módulo es de IA (M15): `/sdd-ff` no aplica — cada PR pasa por propuesta →
  diseño → tareas → revisión humana.
- [ ] 0.7 No editar `Data/Cases/*.json` desde este cambio (son de M14). Si un caso necesita
  un campo nuevo, se arregla en el cambio de M14 antes de archivar cualquiera de los dos.

## Phase 1: Núcleo — tipo, cargador, emparejador, doble (PR1)

- [ ] 1.1 Crear `NpcAi.ClinicalResponse.asmdef` (`references: ["NpcAi.Core"]`,
  `rootNamespace: "NpcAi.ClinicalResponse"`).
- [ ] 1.2 Crear `ClinicalCase.cs`: POCO con `Paciente` y `Hechos` (lista de `Hecho`).
  **Sin** `Clave`. Namespace `NpcAi.ClinicalResponse`.
- [ ] 1.3 Spike de deserialización: probar `JsonUtility` contra `Data/Cases/caso-01.json`
  real. Si no soporta el esquema (arrays anidados, `temperaturaC: null`), decidir entre
  Newtonsoft (declarar `com.unity.nuget.newtonsoft-json` en `package.json`) o un mini-parser.
  Registrar la decisión en `design.md` → Open Questions.
- [ ] 1.4 RED: `ClinicalFactMatcherTests.cs` — normalización (minúsculas, sin tildes, sin
  signos); "¿desde cuándo le empezó el dolor?" → campo `inicio_sintoma` del caso 1;
  "buenos días, ¿cómo se siente?" → `-1`; empate por menor índice.
- [ ] 1.5 GREEN: `ClinicalFactMatcher.cs` — `Normalizar` + `Match` determinista (orden
  explícito, sin `Random`, sin depender de orden de `Dictionary`).
- [ ] 1.6 RED: `ClinicalCasesDataTests.cs` — carga los 3 `Data/Cases/caso-*.json`; valida
  esquema, `id` == nombre de archivo, `triajeEsperado` ∈ {`I`..`V`}, `signosVitales` con
  los 6 campos, `hechos` ≥ mínimo (8), cada `hecho` con ≥ 2 `ejemplosDePregunta`; y que
  ninguna `respuesta` contiene el `triajeEsperado` ni una `banderaRoja` del caso.
- [ ] 1.7 GREEN: `ClinicalCaseLoader.cs` — `TryParse` que satisface `ClinicalCasesDataTests`
  y no mapea `clave`.
- [ ] 1.8 Crear `Fakes/ScriptedClinicalResponder.cs` (doble): 3–4 hechos embebidos, sin
  carga de archivo; `IsReady == true` tras `AssignCase` con id no `None`; determinista.
- [ ] 1.9 RED/GREEN: `ScriptedClinicalResponderTests : ClinicalResponderContract` —
  `CreateSubject()` devuelve el doble tras `AssignCase`. Pasa la batería heredada.
- [ ] 1.10 MANUAL (Editor de Unity): Test Runner → EditMode, verde en `ClinicalFactMatcherTests`,
  `ClinicalCasesDataTests`, `ScriptedClinicalResponderTests`.

## Phase 2: `ClinicalResponder` real (PR2)

- [ ] 2.1 RED: `ClinicalResponderTests : ClinicalResponderContract` — `CreateSubject()`
  construye `new ClinicalResponder(id => jsonDePrueba)` y hace `AssignCase`. Añadir pruebas
  propias de M15: la `respuesta` del caso aparece como subcadena intacta del `Reply.Text`;
  un saludo devuelve `Handled == false`; `PersonalityId.None` no agrega matiz.
- [ ] 2.2 GREEN: `ClinicalResponder.cs` — constructor `Func<ClinicalCaseId,string> cargarJson`;
  `AssignCase` carga y valida (id desconocido / JSON inválido ⇒ `IsReady == false`, sin
  lanzar); `Respond` normaliza, empareja con `ClinicalFactMatcher`, arma el `NpcReply` con
  matiz de personalidad (prefijo/sufijo fijo; `None` sin matiz) y `EmotionTag`/`AnimationCue`
  de tabla fija. Determinista.
- [ ] 2.3 Verificar la batería heredada completa de `ClinicalResponderContract`
  (`Reporta_si_esta_listo_sin_lanzar`, `Sin_caso_asignado_Respond_devuelve_NoAplica`,
  `Respond_no_lanza_en_ningun_estado`,
  `Cuando_responde_el_texto_no_es_vacio_y_los_tags_no_son_nulos`,
  `Es_determinista_en_Handled_y_en_el_texto_para_la_misma_entrada`,
  `AssignCase_es_idempotente_con_el_mismo_par`,
  `AssignCase_con_caso_desconocido_no_lanza_y_deja_no_listo`).
- [ ] 2.4 Prueba manual dedicada: 1000 llamadas a `Respond` con la misma entrada para las 4
  personalidades → 0 variaciones en `Handled` ni en `Reply.Text`.
- [ ] 2.5 Confirmar que ningún otro punto del código instancia `ClinicalResponder`
  automáticamente: la integración en escena es de M11 (`2026-09-09-m11-armado-sesion`).

## Phase 3: Documentación y cierre (último PR)

- [ ] 3.1 Agregar la sección **M15 — Respondedor clínico** a `Docs/MODULES.md`: carpeta,
  dueño (Luis), qué hace, contrato que consume (`IClinicalResponder`), técnica (tabla de
  hechos, determinista), doble, dependencia de M14, estado real.
- [ ] 3.2 Revisar que el diff acumulado respeta los `Success Criteria` de `proposal.md`
  (ninguna carpeta fuera de `Runtime/ClinicalResponse/`, `Tests/EditMode/ClinicalResponse/`,
  `Docs/`, `openspec/`; y `package.json` solo si el spike de 1.3 obligó a Newtonsoft).

## Phase 4: Cierre (acciones del autor, cada PR)

- [ ] 4.1 `git add` solo de las carpetas del PR; `git diff --cached` antes de commitear.
- [ ] 4.2 Checklist "Antes de mergear" del `README.md` (pruebas propias en verde incluida
  la de contrato, diff acotado, spec/design/tasks archivados, rama al día con `main`).
- [ ] 4.3 Al cerrar el último PR: mover el cambio a `openspec/changes/archive/`, crear
  `openspec/specs/respondedor-clinico-m15/spec.md` (satisface `respuesta-clinica-m0`;
  requisitos propios: respuesta desde la tabla de hechos, nunca inventa, `clave` no se
  filtra, matiz no altera el dato; trazabilidad a las pruebas), `archive-report.md`.
- [ ] 4.4 PR mergeado a `main` por el autor (regla 8).
- [ ] 4.5 Registrar en el documento de contexto y en Engram (regla 10).

## Notas

- Ningún agente ejecuta Unity ni entrena nada: M15 no entrena — es tabla + emparejador.
- PR1 puede avanzar apenas M0 y M14 estén en `main`; PR2 depende de PR1.
- Si el emparejador resulta pobre en la escena real, ampliar `ejemplosDePregunta` en
  `Data/Cases/` (M14) es edición de datos, no reapertura de M15.
