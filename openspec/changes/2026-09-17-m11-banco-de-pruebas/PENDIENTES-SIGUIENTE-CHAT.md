# M11 — Pendientes al cambiar de chat (2026-09-18)

Estado: **PR1 (`SessionDirector`) completo, 19/19 tareas cerradas, compuerta humana
confirmada en verde por el usuario.** Rama `feat/m11-01-session-director`, último commit
`b714c15`, ya pusheada a `origin`.

## Inmediato: abrir PR1

- [ ] Abrir PR de `feat/m11-01-session-director` -> `main` (`gh pr create`). Delivery:
  `size:exception` (confirmado por el usuario 2026-09-18, ~890 líneas reales vs. presupuesto
  de 400 — ver `tasks.md` Review Workload Forecast).
- [ ] Antes de abrir: `git fetch origin main && git diff main...feat/m11-01-session-director --stat`
  para confirmar que la rama sigue sin tocar nada fuera de `Runtime/Harness/`,
  `Tests/EditMode/Harness/`, `openspec/changes/2026-09-17-m11-banco-de-pruebas/` (la única
  excepción esperada es el borrado ya aprobado de `openspec/changes/2026-09-09-m11-armado-sesion/`).
- [ ] **El merge a `main` lo debe ejecutar el usuario.** El clasificador auto-mode de Claude
  Code bloquea `gh pr merge` sobre ramas compartidas incluso con autorización verbal previa —
  patrón repetido en M2, M7 y M9 esta misma sesión. No reintentar el merge vía Bash.
- [ ] Tras el merge: `git diff main...<rama-mas-completa> --stat` como verificación final
  (hábito establecido tras encontrar trabajo varado en M2 y M7 por orden de merge incorrecto).

## Después del merge de PR1

- [ ] `sdd-verify` sobre `2026-09-17-m11-banco-de-pruebas`.
- [ ] `sdd-archive` — mueve el cambio a `openspec/changes/archive/`, promueve
  `specs/banco-de-pruebas-m11/spec.md` a `openspec/specs/`.

## PR2 (separado, no iniciar hasta que PR1 esté mergeado y archivado)

Alcance ya decidido en `proposal.md`/`design.md`, explícitamente diferido de PR1:
- `Samples~/Harness/HarnessBehaviour.cs` — el `MonoBehaviour` delgado que compone
  `SessionDirector` en una escena real (Samples~ nunca compila con el paquete: es código de
  host-app solo al importarse).
- `Samples~/Harness/README.md` (actualizar), escena de ejemplo.
- `HarnessBehaviourWiringTests` — su propio addendum de spec (spec.md de PR1 ya deja explícito
  que este PR2 queda fuera).
- Sección M11 de `Docs/MODULES.md` (hoy sigue diciendo "solo README").
- Necesita su propio ciclo SDD corto (probablemente propose+spec+design abreviados dado que
  PR1 ya fijó toda la arquitectura — a decidir cuando se retome).

## Backlog conocido, no de M11, no urgente

- **M9↔M15 gap de índice de bandera roja**: decidido omitir en esta entrega de M11 (ver
  proposal.md, decisión 2026-09-17). El componente clínico de `Progress01` de M9 se queda en 0
  mientras tanto. Arreglo correcto: cambio SDD separado sobre M15 que exponga qué
  `Hecho`/`Campo` coincidió — no es tarea de M11.
- **M10 (Boardroom)**: sigue siendo doble genérico, explícitamente fuera de alcance de M11.
- **M16 (`IRequirementResponder`)**: cero código todavía, explícitamente fuera de alcance de M11.
- **M7**: 3 `PhysicalAction` diferidas (`EntregarObjeto`/`SenalarPantalla`/`GestoCalma`)
  pendientes de una decisión de paquete de interacción con controlador — no relacionado con M11.

## Contexto que el siguiente chat necesita para no repetir errores ya resueltos

- **AD1 (design.md)**: `IScenarioObjective` (puerto congelado M0) no tiene `AssignCase` —
  `SessionDirector` lo alcanza vía 2 delegados `Action<T>` inyectados por constructor, nunca
  referenciando el ensamblado concreto de M9. Si alguien propone "que SessionDirector llame
  directo a M9", es un retroceso: ya se descartó por imposible.
- Los 5 espías locales de prueba (`EspiasDeArnes.cs`) existen porque ningún doble compartido
  (`Scripted*`) puede observar lo que la spec exige — no son duplicación accidental.
- El bug real encontrado durante `sdd-apply`: el namespace de módulo `NpcAi.ClinicalResponse`
  choca con el DTO `NpcAi.Core.ClinicalResponse` para cualquier código anidado bajo `NpcAi` —
  requiere calificar `Core.ClinicalResponse` explícitamente (mismo fix ya usado en
  `ScriptedClinicalResponder`).
- El agente de `sdd-apply` no tiene Unity Editor disponible: su evidencia RED→GREEN fue un
  arnés `dotnet test`/NUnit 3.x en el scratchpad de la sesión, nunca dentro del repo. Esto NO
  sustituye la compuerta humana real de Unity Test Runner — que ya se corrió y confirmó en
  verde por el usuario el 2026-09-18.
