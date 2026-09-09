# Tasks: M9 — Decisión de triaje y evaluación

## Review Workload Forecast

| Field | Value |
|---|---|
| Estimated changed lines | ~250 (`EmergencyScenarioObjective` ~120, `EmergencyObjectiveConfig` ~30, pruebas ~90, sección MODULES ~15); +~60 si AD1-b agrega `ITriageBoard` al cambio de M0 |
| 400-line budget risk | Bajo |
| Chained PRs recommended | No (a menos que AD1-b: entonces el trozo de `ITriageBoard` va en el PR del cambio de M0, no aquí) |
| Suggested split | PR único para M9 |
| Decision needed before apply | Sí — **AD1** (puerto `ITriageBoard` vs. API interna) se decide en la ronda de propuesta antes de escribir código |

### Suggested Work Units

| Unit | Meta | PR | Prueba enfocada | Frontera de rollback |
|---|---|---|---|---|
| 1 | `EmergencyScenarioObjective` real + veredicto de triaje | PR único (M9) | EditMode: `EmergencyScenarioObjectiveTests : ScenarioObjectiveContract`, `TriageVerdictTests` | Borrar `EmergencyScenarioObjective.cs` + config + pruebas; M9 vuelve a "solo doble" |
| (2) | `ITriageBoard` + `Triage` + `TriajeVeredicto` en `NpcAi.Core` | **solo si AD1-b — va en el PR del cambio de M0**, no en este | `ContractTypeTests`, doble `ScriptedTriageBoard` | Revertir con el cambio de M0 |

## Phase 0: Guardrails (leer antes de escribir)

- [ ] 0.1 **Resolver AD1 en la ronda de propuesta** antes de tocar código: híbrido
  (recomendado: `ITriageBoard` puerto + `RegistrarHechoObtenido` API interna) / todo-interno /
  todo-contrato. Registrar la decisión en `design.md` → AD1.
- [ ] 0.2 Frontera de escritura: `Runtime/Scenarios/Emergency/`,
  `Tests/EditMode/Scenarios/Emergency/`, `Docs/MODULES.md` (sección M9), y este directorio
  de cambio. **Nada de `Runtime/Scenarios/Boardroom/` (M10).**
- [ ] 0.3 Si AD1 elige puerto (`ITriageBoard`): ese tipo, `Triage` y `TriajeVeredicto`
  **NO se escriben en este cambio** — se agregan al cambio
  `2026-09-09-m0-puerto-respuesta-clinica` (crece su `v2`). Aquí solo se implementan.
  Si algo de este cambio parece requerir editar `Runtime/Core/`, parar y avisar.
- [ ] 0.4 `Tests/EditMode/Core/ScenarioObjectiveContract.cs` no se modifica:
  `EmergencyScenarioObjectiveTests` hereda, no edita.
- [ ] 0.5 M9 no está en la lista de módulos de IA (M1/M2/M4/M6); aun así el equipo puede
  optar por el flujo completo dado que M9 estrena spec formal y comportamiento nuevo.
- [ ] 0.6 `NpcAi.Scenarios.Emergency.asmdef` no gana referencias. M9 recibe `string campo`
  y `Triage categoria` desde M11; no referencia M14, M15 ni M2.
- [ ] 0.7 M9 **sí** puede leer `clave` de los casos de M14 (es su consumidor legítimo).

## Phase 1: Config y progreso clínico

- [ ] 1.1 Crear `EmergencyObjectiveConfig` (struct/clase de datos): `BanderasRojas`
  (lista), `TriajeEsperado` (`Triage`), `MapeoBanderaACampo` (bandera → `campo`(s) de la
  tabla de hechos). La arma M11 desde el `clave` del caso.
- [ ] 1.2 RED: `EmergencyScenarioObjectiveTests : ScenarioObjectiveContract` — `CreateSubject()`
  con una config de prueba (2–3 banderas, mapeo simple). Debe pasar la batería heredada tal
  cual (progreso `[0,1]`, `IsComplete` ⟺ `1`, reversible por `Notify`, `Notify(default)` no-op).
- [ ] 1.3 RED: pruebas propias de progreso — `RegistrarHechoObtenido("inicio_sintoma")` con
  ese campo ligado a una bandera sube `Progress01`; un campo no ligado no sube el
  componente clínico; obtener todas las banderas + receptividad ≥ neutral ⇒ `Progress01 == 1`.
- [ ] 1.4 GREEN: `EmergencyScenarioObjective.cs` — `Notify` (semántica de contrato para el
  componente de receptividad), `RegistrarHechoObtenido` (marca la bandera ligada como
  cubierta, idempotente por campo), `Progress01 = w_r*receptividad + w_c*clinico`
  (constantes `w_r`/`w_c` documentadas), `IsComplete`.

## Phase 2: Decisión de triaje

- [ ] 2.1 Según AD1: implementar `EnviarTriaje(Triage) → TriajeVeredicto` como API de la
  clase, o como implementación de `ITriageBoard` (interfaz añadida en el cambio de M0).
- [ ] 2.2 RED: `TriageVerdictTests` — `EnviarTriaje(config.TriajeEsperado)` ⇒
  `Acerto == true`, `Elegida == Esperada`; `EnviarTriaje` con otra categoría ⇒
  `Acerto == false`, `Esperada` correcta; `CierreEsperado` no vacío; segundo `EnviarTriaje`
  es no-op (idempotente) y no cambia el veredicto.
- [ ] 2.3 GREEN: implementar la comparación y el cierre de sesión: tras el primer envío,
  `Enviado == true`, `IsComplete` queda fijo y `Notify` deja de mutar el progreso.
- [ ] 2.4 Prueba de no-regresión: una instancia nueva **sin** `EnviarTriaje` sigue pasando
  `ScenarioObjectiveContract.La_completitud_es_reversible`.

## Phase 3: Documentación y cierre

- [ ] 3.1 Agregar/actualizar la sección **M9 — Escenario Sala de Emergencia** en
  `Docs/MODULES.md`: pasa de "solo doble" a "implementación real"; dos dimensiones de
  progreso; decisión de triaje (puerto o API según AD1); dependencia de `clave` de M14.
- [ ] 3.2 Revisar que el diff respeta los `Success Criteria` de `proposal.md` (nada en
  `Boardroom/`, nada en `Runtime/Core/` salvo lo que el cambio de M0 ya autorizó).

## Phase 4: Cierre (acciones del autor)

- [ ] 4.1 `git add` solo de `Runtime/Scenarios/Emergency/`,
  `Tests/EditMode/Scenarios/Emergency/`, `Docs/MODULES.md` y el directorio de cambio;
  `git diff --cached` antes de commitear.
- [ ] 4.2 Checklist "Antes de mergear" del `README.md` (pruebas propias en verde incluida
  la de contrato heredada, diff acotado, spec/design/tasks archivados).
- [ ] 4.3 Al cerrar: mover a `openspec/changes/archive/`, crear
  `openspec/specs/escenario-emergencia-m9/spec.md` (invariantes de `IScenarioObjective` +
  requisitos del escenario de triaje, Dado/Cuando/Entonces, trazabilidad a
  `EmergencyScenarioObjectiveTests` y `TriageVerdictTests`), `archive-report.md`.
- [ ] 4.4 PR mergeado a `main` por el autor (regla 8).
- [ ] 4.5 Registrar en el documento de contexto y en Engram (regla 10), incluida la
  decisión de AD1.

## Notas

- Este cambio depende de `2026-09-09-m14-catalogo-casos-clinicos` mergeado. Si AD1-b, depende
  también de que el cambio de M0 haya incluido `ITriageBoard`.
- Ningún agente ejecuta Unity: cada verde de Test Runner es compuerta humana.
- La calibración de `w_r`/`w_c` y del mapeo bandera→campo es tuning de datos/constantes
  posterior, no reapertura del cambio.
