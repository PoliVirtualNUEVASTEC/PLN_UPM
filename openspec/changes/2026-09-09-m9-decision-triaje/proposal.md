# Propuesta: M9 — Decisión de triaje y evaluación (Sala de Emergencia real)

## Intent

M9 hoy es **solo doble**: `Runtime/Scenarios/Emergency/` únicamente tiene
`Fakes/ScriptedScenarioObjective.cs`, que avanza un paso fijo (±1 de 4) por cada
mejora/empeoramiento de receptividad. No hay implementación real, ni spec formal
(`openspec/specs/` no tiene `escenario-emergencia-m9`), y el objetivo no sabe nada del caso
clínico ni de la categoría de triaje que la enfermera elige al cerrar.

El bucle de la simulación necesita que M9:

1. Mida un **progreso con sentido clínico**: cuánta de la información clave del caso (la que
   sustenta las banderas rojas) ha logrado sacar la enfermera en la conversación — no solo
   "el paciente está más receptivo".
2. Reciba la **decisión de triaje** (categoría I–V) que la enfermera envía con un botón, la
   compare con `clave.triajeEsperado` del caso (M14) y produzca un **veredicto de cierre**
   (acierto / desacierto + la categoría esperada + el `cierreEsperado`), terminando la
   sesión.

Este cambio entrega la implementación real de M9 y su primera spec. Contiene **una decisión
de arquitectura sin resolver** (¿la decisión de triaje es un puerto nuevo de M0 o API
interna de M9?), que se resuelve en la ronda de propuesta de este cambio — ver
`design.md` → AD1.

## Scope

### In Scope

- `Runtime/Scenarios/Emergency/EmergencyScenarioObjective.cs`: implementación real de
  `IScenarioObjective` (hereda `ScenarioObjectiveContract` sin modificarla). `Progress01`
  combina la dimensión de receptividad (vía `Notify`, como el doble) con la dimensión de
  **información clínica obtenida**.
- Un mecanismo para que M9 sepa qué hechos del caso obtuvo la enfermera. Según AD1, esto
  es: (a) API interna `RegistrarHechoObtenido(string campo)` que M11 llama al ver una
  respuesta de M15, o (b) un puerto/canal nuevo de M0. **La primera entrega usa (a)** salvo
  que la ronda de propuesta decida (b).
- La **decisión de triaje**: según AD1, un enum `Triage` (I–V) y una operación
  `EnviarTriaje(Triage) → TriajeVeredicto` — como API de `EmergencyScenarioObjective` (sin
  cambio de contrato) o como puerto nuevo `ITriageBoard` de M0 (cambio de contrato, crece
  el alcance del cambio de M0).
- `Runtime/Scenarios/Emergency/EmergencyObjectiveConfig` o carga del caso: M9 necesita
  `clave.banderasRojas` y `clave.triajeEsperado` de M14. M9 **sí** puede leer `clave` (es su
  consumidor legítimo; M15 no).
- Doble actualizado o nuevo: `ScriptedScenarioObjective` se mantiene como doble mínimo del
  puerto `IScenarioObjective`; si AD1 crea `ITriageBoard`, se agrega su doble.
- `Tests/EditMode/Scenarios/Emergency/EmergencyScenarioObjectiveTests.cs :
  ScenarioObjectiveContract` + pruebas propias (progreso por hecho clínico obtenido;
  veredicto de acierto y de desacierto; `IsComplete` cuando se cubrió lo esperado y se
  envió el triaje correcto).
- `openspec/specs/escenario-emergencia-m9/spec.md` (primera spec formal de M9).
- Actualizar `Docs/MODULES.md`: sección M9.

### Out of Scope

- **`IScenarioObjective` no cambia** su forma (`Progress01`, `IsComplete`,
  `Notify(ReceptivityChange)`). Si M9 necesita ser notificado de hechos clínicos por el
  contrato (opción (b) de AD1), eso es cambio de M0 y se maneja como tal.
- **M10 (Sala de Juntas)** no se toca: comparte el puerto `IScenarioObjective` pero es otro
  escenario, otro dueño (Jefferson), otro cambio.
- **La UI del botón de triaje**: el widget lo pone el proyecto VR anfitrión; este cambio
  expone la operación que el botón invoca.
- **El enum `Triage` en `Intent`/`Tone`**: NO. El triaje es salida del escenario, no
  clasificación de un enunciado. Los enums de M0 no ganan miembros.
- **La conversación y los hechos** (M15), la comprensión (M2), la receptividad (M4): M9
  consume sus salidas, no las reimplementa.
- **Persistir el veredicto**: M13 (bitácora) puede suscribirse al resultado en un cambio
  posterior; no es parte de este.

## Capabilities

### New Capabilities

- `escenario-emergencia-m9` (nuevo): primera spec formal de M9. Formaliza los invariantes
  ya fijados por `ScenarioObjectiveContract` (progreso en `[0,1]`, `IsComplete` ⟺
  `Progress01 == 1`, reversible, `Notify(default)` no-op) más los propios del escenario de
  triaje: el progreso refleja información clínica obtenida, el veredicto compara con
  `clave.triajeEsperado`, la sesión cierra al enviar el triaje.

### Modified Capabilities

- `contrato-nucleo-m0`: **solo si AD1 elige la opción (b)** (puerto `ITriageBoard`). En ese
  caso el cambio de M0 (`2026-09-09-m0-puerto-respuesta-clinica`) crece para agregar
  también `ITriageBoard`, `Triage` y `TriajeVeredicto`, y este cambio pasa a ser
  "M9 impl-only". Si AD1 elige (a), `contrato-nucleo-m0` no se toca.

## Approach

**Dos dimensiones de progreso, una fórmula.** El doble actual solo mira receptividad.
`EmergencyScenarioObjective` combina:

- **Receptividad** (vía `Notify(ReceptivityChange)`, sin cambio de contrato): que el
  paciente colabore.
- **Información clínica obtenida**: fracción de las banderas rojas del caso que la enfermera
  ha tocado en la conversación. M9 mapea cada `banderaRoja` de `clave` a uno o más `campo`
  de la tabla de hechos; cuando M15 responde ese `campo`, ese ítem cuenta como cubierto.

`Progress01 = w_r * progresoReceptividad + w_c * progresoClinico`, con pesos documentados en
el `design.md`. `IsComplete` cuando `Progress01 == 1` **y** se envió un triaje — sigue
siendo reversible mientras no se cierre.

**Cómo se entera M9 de un hecho clínico obtenido** — el nudo de AD1:

- Opción (a), sin cambio de contrato: `EmergencyScenarioObjective` expone
  `RegistrarHechoObtenido(string campo)` como API propia (no del puerto). M11, que es la
  raíz de composición y ve la `ClinicalResponse` de M15, la llama con el `campo` cuando
  `Handled == true`. Igual que `ReceptivityEngine` (M4) tiene API más allá de
  `IReceptivityEngine`.
- Opción (b), cambio de contrato: un `EventChannel<...>` o un `Notify` sobrecargado para
  hechos clínicos. Más limpio a largo plazo, pero mete a M9 en el cambio de M0.

**La decisión de triaje** — la otra mitad de AD1:

- `EnviarTriaje(Triage categoria)` compara con `clave.triajeEsperado` y devuelve
  `TriajeVeredicto { Elegida, Esperada, Acerto, CierreEsperado }`. Marca el objetivo como
  cerrado (`IsComplete` deja de ser reversible tras el envío).
- Como API de `EmergencyScenarioObjective` (opción a) o como puerto `ITriageBoard` de M0
  (opción b).

**Recomendación de este documento**: opción **(b) acotada** — agregar `ITriageBoard` +
`Triage` + `TriajeVeredicto` al cambio de M0, y dejar la dimensión de "hecho clínico
obtenido" como API interna (opción a) por ahora. Razón: la decisión de triaje es una salida
de primera clase de la simulación (la UI la dispara, M13 querrá registrarla, las pruebas de
integración la necesitan abstracta); el conteo de hechos obtenidos es cocina interna del
escenario que puede formalizarse después si hace falta. La ronda de propuesta confirma o
cambia esto.

## Affected Areas

| Área | Impacto | Descripción |
|---|---|---|
| `Runtime/Scenarios/Emergency/EmergencyScenarioObjective.cs` | Nuevo | Implementación real de `IScenarioObjective` |
| `Runtime/Scenarios/Emergency/` (config / carga del caso) | Nuevo | Lee `clave.banderasRojas` y `clave.triajeEsperado` de M14 |
| `Runtime/Scenarios/Emergency/Fakes/ScriptedScenarioObjective.cs` | Sin cambio (o +doble de `ITriageBoard` si AD1-b) | Sigue siendo el doble del puerto |
| `Tests/EditMode/Scenarios/Emergency/` | Nuevo | `EmergencyScenarioObjectiveTests : ScenarioObjectiveContract` + pruebas de veredicto |
| `openspec/specs/escenario-emergencia-m9/spec.md` | Nuevo (al archivar) | Primera spec formal |
| `Docs/MODULES.md` | Modificado | Sección M9 |
| `NpcAi.Core` | Sin cambio, **o** +`ITriageBoard`/`Triage`/`TriajeVeredicto` (AD1-b, dentro del cambio de M0) | Depende de AD1 |
| `Data/Cases/` | Sin cambio | M9 lo lee; es de M14 |

## Risks

| Riesgo | Prob. | Mitigación |
|---|---|---|
| Mapear `banderasRojas` (texto libre) a `campo` de hechos es frágil | Alta | AD de M14 (Open Question) contempla estructurar `banderasRojas`; si M9 lo necesita, se estructura en M14 antes de archivar. Mientras tanto, el mapeo vive en la config de M9 y se prueba con los 3 casos |
| La fórmula de `Progress01` (pesos `w_r`/`w_c`) es arbitraria y no valida contra un criterio real | Media | Documentada en `design.md` como "tuning de record" (patrón M5); ajustarla es editar constantes, no cambiar el contrato. El criterio de aceptación es el contrato de `IScenarioObjective`, no la calibración fina |
| AD1 sin resolver bloquea el cambio | Media | La ronda de propuesta de **este** cambio la resuelve antes de escribir código; la recomendación (b-acotada) es el punto de partida |
| `IsComplete` deja de ser reversible tras `EnviarTriaje` — ¿viola `ScenarioObjectiveContract.La_completitud_es_reversible`? | Media | Esa prueba solo ejercita `Notify`; no envía triaje. La irreversibilidad post-cierre es un estado nuevo que la spec de M9 define y que la prueba de contrato no contradice (no llama `EnviarTriaje`). Verificar al implementar que la batería heredada sigue verde |
| Se cuela lógica de UI o de M11 en M9 | Media | M9 solo expone operaciones y estado; no elige caso, no dibuja botón, no orquesta el pipeline |

## Rollback Plan

Revertir borra `EmergencyScenarioObjective.cs` y sus pruebas; `Runtime/Scenarios/Emergency/`
vuelve a "solo doble", igual que hoy. Si AD1-b agregó `ITriageBoard` al cambio de M0, esa
parte se revierte con el cambio de M0 (baja `Contract.Version`). Ningún módulo depende de
`EmergencyScenarioObjective` por nombre hasta que M11 lo cablee.

## Dependencies

- **`2026-09-09-m14-catalogo-casos-clinicos` mergeado**: M9 lee `clave.triajeEsperado` y
  `clave.banderasRojas`.
- **`2026-09-09-m0-puerto-respuesta-clinica`**: si AD1-b, este cambio de M0 crece para
  incluir `ITriageBoard`; si AD1-a, no hay dependencia dura, solo el vocabulario del caso.
- `contrato-nucleo-m0` (define `IScenarioObjective`, `ReceptivityChange`).
- `2026-09-09-m15-respondedor-clinico` (para que M11 tenga qué registrar); no es dependencia
  de compilación de M9.

## Success Criteria

- [ ] `EmergencyScenarioObjectiveTests : ScenarioObjectiveContract` pasa la batería
      heredada sin modificar la base.
- [ ] `Progress01` sube cuando la enfermera obtiene un hecho ligado a una bandera roja del
      caso; llega a `1` cuando se cubren todas y la receptividad no está en el piso.
- [ ] `EnviarTriaje` con la categoría de `clave.triajeEsperado` devuelve `Acerto == true`;
      con otra, `Acerto == false` y `Esperada` correcta; en ambos casos incluye
      `CierreEsperado`.
- [ ] Tras `EnviarTriaje`, la sesión queda cerrada (documentado en la spec de M9).
- [ ] `openspec/specs/escenario-emergencia-m9/spec.md` existe con trazabilidad a las pruebas.
- [ ] `Docs/MODULES.md` sección M9 actualizada.
- [ ] El diff no toca `Runtime/Scenarios/Boardroom/` (M10), `NpcAi.Core` (salvo la parte de
      `ITriageBoard` que, si aplica, va en el cambio de M0), ni nada fuera de
      `Runtime/Scenarios/Emergency/`, `Tests/EditMode/Scenarios/Emergency/`, `Docs/`,
      `openspec/`.

## Decisiones del usuario (confirmadas 2026-09-09)

1. La decisión de triaje y su evaluación viven en M9 (impl. real), no en un enum de
   `Intent`/`Tone`.
2. **AD1 (puerto nuevo vs. API interna para la decisión de triaje) se resuelve en la ronda
   de propuesta de este cambio**, con la recomendación (b-acotada) como punto de partida.
3. M9 es de Luis.
