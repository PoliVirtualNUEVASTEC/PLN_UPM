# Tasks: M11 — Armado de sesión

## Review Workload Forecast

| Field | Value |
|---|---|
| Estimated changed lines | PR1 ~200 (`SessionDirector` + `NpcIdentity` + `Data/Npcs` + `SessionDirectorTests`), PR2 ~180 (escena + `HarnessBehaviour` + README) |
| 400-line budget risk | Bajo por PR |
| Chained PRs recommended | Sí |
| Suggested split | PR1 (lógica + pruebas) → PR2 (escena de arnés) |
| Chain strategy | stacked-to-main (PR1 → main, PR2 → PR1) |

### Suggested Work Units

| Unit | Meta | PR | Prueba enfocada | Frontera de rollback |
|---|---|---|---|---|
| 1 | `SessionDirector` + identidad de NPC + pruebas | PR1 | EditMode: `SessionDirectorTests` (elección reproducible, enrutado, cierre) | Borrar `Runtime/Harness/`, `Data/Npcs/`, `Tests/EditMode/Harness/` |
| 2 | Arnés de escritorio (escena + cáscara) | PR2 | Manual (Editor): escribir turnos, ver respuestas, cerrar con triaje | Borrar `Samples~/Harness/*.cs` y `.unity`; PR1 queda usable por el proyecto anfitrión |

## Phase 0: Guardrails (leer antes de escribir)

- [ ] 0.1 **Dependencias**: idealmente `2026-09-09-m0-puerto-respuesta-clinica`,
  `-m14-catalogo-casos-clinicos`, `-m15-respondedor-clinico` y `-m9-decision-triaje`
  mergeados. Mínimo: los **dobles** de M15 y M9 disponibles. Si falta el puerto
  `IClinicalResponder`, este cambio no compila — parar y avisar.
- [ ] 0.2 Frontera de escritura: `Runtime/Harness/`, `Data/Npcs/`, `Samples~/Harness/`,
  `Tests/EditMode/Harness/`, `Docs/MODULES.md` (sección M11), y este directorio de cambio.
- [ ] 0.3 **Cero cambio en `NpcAi.Core`.** M11 solo consume puertos. `Triage`/`ITriageBoard`
  los agrega el cambio de M0 (vía M9), no este. Si algo parece exigir tocar `Runtime/Core/`,
  parar y avisar.
- [ ] 0.4 `SessionDirector` va en `Runtime/Harness/`, **NO** en `Samples~/` (que no se
  compila con el paquete y no se puede probar). Solo `HarnessBehaviour` y la escena van en
  `Samples~/`.
- [ ] 0.5 `NpcAi.Harness.asmdef` referencia **solo `NpcAi.Core`**. `SessionDirector` recibe
  los puertos por constructor, no instancia implementaciones concretas.
- [ ] 0.6 Regla 4/5 del repo: una escena por persona; `Harness.unity` es de Jefferson.
  Conflicto en `.unity` se descarta y se rehace, no se resuelve a mano.
- [ ] 0.7 No tocar `Runtime/Scenarios/Boardroom/` (M10) ni ningún otro módulo.

## Phase 1: `SessionDirector` + identidad (PR1)

- [ ] 1.1 Crear `Data/Npcs/senora-mayor.json` y `Data/Npcs/joven.json`
  (`{ id, nombre, rangoEtario, vozId }`); `id` == nombre de archivo (convención M5/M14).
- [ ] 1.2 Crear `Runtime/Harness/NpcAi.Harness.asmdef` (`references: ["NpcAi.Core"]`) y
  `NpcIdentity.cs` (POCO + loader desde string JSON).
- [ ] 1.3 RED: `SessionDirectorTests` —
  - misma semilla + mismo catálogo ⇒ misma `(CasoActual, PersonalidadActual, NpcActual)`
    en 2 instancias;
  - iniciar una segunda sesión en el mismo director no repite el `ClinicalCaseId` anterior;
  - con un `IClinicalResponder` doble que devuelve `Handled == true`, `ProcesarTurno` NO
    llama al `IDialogueGenerator` doble (verificar con un spy/registro);
  - con `Handled == false`, `ProcesarTurno` SÍ llama a `Generate`;
  - `CerrarConTriaje` llama a `m9` y deja `SesionTerminada == true`; un segundo
    `ProcesarTurno` tras el cierre es no-op.
- [ ] 1.4 GREEN: `Runtime/Harness/SessionDirector.cs` —
  - constructor con semilla, catálogos y puertos (ver `design.md` → Interfaces);
  - elección con `System.Random(seed)` propio; guarda el último `ClinicalCaseId`;
  - en la construcción: `m15.AssignCase`, `m4.Reset`, arma la config de M9 desde el caso;
  - `ProcesarTurno` con el enrutado de `design.md` → Data Flow;
  - `CerrarConTriaje` delega en M9 y fija `SesionTerminada`.
- [ ] 1.5 MANUAL (Editor): Test Runner → EditMode, `SessionDirectorTests` en verde contra
  los dobles publicados por los otros módulos.

## Phase 2: Arnés de escritorio (PR2)

- [ ] 2.1 Crear `Samples~/Harness/HarnessBehaviour.cs`: construye los puertos (reales donde
  estén mergeados, dobles donde no; config editable), instancia `SessionDirector` con una
  semilla visible, conecta un campo de texto (`ProcesarTurno`) y 5 botones I–V
  (`CerrarConTriaje`), y muestra el log de turnos + el progreso + el veredicto.
- [ ] 2.2 Crear `Samples~/Harness/Harness.unity`: Canvas con input, log scroll, barra de
  progreso, botones de triaje, y un campo para la semilla. Una sola escena, autor único.
- [ ] 2.3 Actualizar `Samples~/Harness/README.md`: requisitos, cómo abrir la escena, qué
  puerto está real y cuál es doble en esta entrega, cómo fijar la semilla para reproducir
  una sesión.
- [ ] 2.4 MANUAL (Editor): abrir la escena, jugar una sesión completa — escribir preguntas
  de anamnesis (el NPC responde hechos del caso), una frase social (responde M6), ver el
  progreso subir, cerrar con un botón de triaje y leer el veredicto.

## Phase 3: Documentación y cierre

- [ ] 3.1 Reescribir la sección **M11 — Banco de pruebas** de `Docs/MODULES.md`: pasa de
  "solo README" a "escena real + `SessionDirector`"; describe el enrutado, la elección
  reproducible, y qué módulos cablea reales vs. dobles.
- [ ] 3.2 Revisar que el diff respeta los `Success Criteria` de `proposal.md`.

## Phase 4: Cierre (acciones del autor, cada PR)

- [ ] 4.1 `git add` solo de las carpetas del PR; `git diff --cached` antes de commitear.
- [ ] 4.2 Checklist "Antes de mergear" del `README.md` (pruebas propias en verde, diff
  acotado, spec/design/tasks archivados, rama al día con `main`; para la escena: sin
  conflicto de `.unity`).
- [ ] 4.3 Al cerrar el último PR: mover el cambio a `openspec/changes/archive/`; si el
  equipo quiso spec formal, crear `openspec/specs/armado-sesion-m11/spec.md` (elección
  reproducible, enrutado, cierre; trazabilidad a `SessionDirectorTests`);
  `archive-report.md`.
- [ ] 4.4 PR mergeado a `main` por el autor (regla 8).
- [ ] 4.5 Registrar en el documento de contexto y en Engram (regla 10).

## Notas

- Este es el **último cambio de la cadena**: integra M0 + M14 + M15 + M9 en una escena
  jugable. Puede entregarse contra dobles y actualizarse a reales de a uno.
- El proyecto VR anfitrión (sala de espera, llamar por el nombre, locomoción) reutiliza
  `SessionDirector`; construir ese lado no es parte de este cambio ni del repo del paquete.
- Ningún agente ejecuta Unity ni arma la escena de forma autónoma: cada verde de Test
  Runner y la demo manual son compuertas humanas.
