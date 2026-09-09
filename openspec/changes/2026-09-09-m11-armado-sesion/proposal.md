# Propuesta: M11 — Armado de sesión (Banco de pruebas / raíz de composición)

## Intent

M11 hoy es **solo `Samples~/Harness/README.md`**: no hay escena `.unity` ni script de
arnés. El bucle de la simulación necesita una **raíz de composición** que, al iniciar una
sesión:

1. Elija un **cuerpo de NPC** (señora mayor / joven).
2. Elija un **caso clínico al azar** del catálogo de M14.
3. Elija una **personalidad al azar** de las 4 de M5.
4. Cablee el pipeline: `M1 ISpeechToText → M2 IIntentClassifier → (enrutador: M15 | M6) →
   M4 IReceptivityEngine → M8 INpcPresenter`, con M13 registrando la conversación y M9
   midiendo el objetivo.
5. Exponga el cierre: el envío de la categoría de triaje (M9) que termina la sesión.

Este cambio entrega esa raíz: un `SessionDirector` (lógica pura, probable) más el arnés de
escritorio (escena + `MonoBehaviour` delgado) que lo ejecuta sin VR. El proyecto VR
anfitrión reutiliza el mismo `SessionDirector` desde su escena de sala de espera.

Decisión ya fijada (2026-09-09): el **enrutado clínico/social vive aquí** — "pregunta a
M15; si `ClinicalResponse.Handled == false`, pregunta a M6". No hace falta un clasificador
aparte.

## Scope

### In Scope

- `SessionDirector` (lógica pura de C#): recibe los puertos (por interfaz, reales o dobles),
  el catálogo de casos (M14) y el de personalidades (M5), una semilla, y:
  - elige `(ClinicalCaseId, PersonalityId, NpcId)` al azar con la semilla (reproducible),
    evitando repetir el último caso;
  - hace `AssignCase` en M15 y `Reset` en M4 con lo elegido;
  - arma la config de M9 desde el `clave` del caso;
  - implementa `ProcesarTurno(Utterance)`: M2 clasifica → M15 responde → si `!Handled`, M6
    genera → M4 evalúa → M8 reproduce → M13 registra → M9 actualiza progreso;
  - expone `CerrarConTriaje(Triage)` → delega en M9 y marca la sesión terminada.
- `Data/Npcs/senora-mayor.json`, `Data/Npcs/joven.json` — identidad mínima del NPC
  (`{ id, nombre, rangoEtario, vozId }`). Ver Open Questions: puede ser su propio
  micro-módulo si el equipo es estricto con "un cambio = una carpeta".
- `Samples~/Harness/`: escena `.unity` de escritorio, `HarnessBehaviour.cs` (campo de texto
  para escribir el turno + botones de triaje), y el README actualizado. Cablea
  **implementaciones reales donde existan, dobles donde no** (M1, M7, M8 probablemente
  dobles en la primera entrega; M2/M4/M5/M6/M13/M15/M9 reales si ya están mergeados).
- `SessionDirectorTests` (EditMode): elección reproducible con semilla; no repite el último
  caso; el enrutado manda a M15 cuando `Handled` y a M6 cuando no; `CerrarConTriaje`
  delega en M9.
- Actualizar `Docs/MODULES.md`: sección M11.

### Out of Scope

- **Ningún cambio de contrato.** M11 consume los puertos existentes. `Triage`/`ITriageBoard`
  (si existen) los agrega el cambio de M0 vía M9, no este.
- **La sala de espera y la locomoción** (llamar al paciente por su nombre, caminar a
  triaje): eso es escena, navegación y animación del **proyecto VR anfitrión**. El harness
  de escritorio simula el inicio escribiendo el nombre del NPC; no modela el espacio.
- **Cuerpos, prefabs, rig, voz sintetizada reales**: del proyecto anfitrión y de M8. M11
  usa placeholders.
- **M10 (Sala de Juntas)**: otro escenario, otro dueño; M11 podría armarlo también, pero
  este cambio se enfoca en el de emergencia/triaje.
- **Las implementaciones de M15, M9, M6**: sus propios cambios. M11 las cablea, no las
  escribe.
- **Métricas del Objetivo 4** (Sprint 13-14): M11 se convierte en el instrumento de
  medición más adelante; este cambio solo arma la sesión.

## Capabilities

### New Capabilities

- `armado-sesion-m11` (nuevo, opcional): si el equipo quiere spec formal del `SessionDirector`
  (elección reproducible, enrutado, cierre). El arnés de escena en `Samples~/` no se
  especifica formalmente (es demostración, no puerto).

### Modified Capabilities

- Ninguna.

## Approach

**`SessionDirector` separado del `MonoBehaviour`.** La lógica de elección, enrutado y cierre
es C# puro y vive en un ensamblado `Runtime/` (`NpcAi.Harness` o similar) para poder
probarla en EditMode. El `HarnessBehaviour` en `Samples~/Harness/` es una cáscara: lee el
campo de texto, llama `ProcesarTurno`, dibuja el estado. `Samples~/` no se compila con el
paquete, así que **toda la lógica testeable tiene que estar fuera de `Samples~/`**.

**Enrutado clínico/social, trivial.** `ProcesarTurno`:

```
var intent = m2.Classify(utterance.Text);
var clin   = m15.Respond(utterance, intent);
NpcReply reply = clin.Handled ? clin.Reply : m6.Generate(personality, m4.Current, intent);
m8.Play(reply);
m13.Registrar(utterance, reply);
var change = m4.Evaluate(intent, PhysicalAction.Ninguna);
m9.Notify(change);
if (clin.Handled) m9.RegistrarHechoObtenido(campoDe(clin));   // según AD1 de M9
```

**Elección reproducible.** `SessionDirector(seed)` usa un `System.Random(seed)` propio; dos
directores con la misma semilla y el mismo catálogo eligen el mismo `(caso, personalidad,
npc)`. Guarda el último `ClinicalCaseId` para no repetirlo en la sesión siguiente.

**Identidad del NPC como dato.** `Data/Npcs/*.json`: `nombre` (para "llamar por el nombre"
en el anfitrión), `rangoEtario` (`mayor` / `joven`), `vozId` (para M8). El caso clínico es
**agnóstico del cuerpo**: cualquier NPC actúa cualquier caso; `SessionDirector` elige los
dos por separado.

**Dobles donde falte real.** Igual criterio que el README de M11: se arma contra los dobles
y se reemplaza de a uno. Un flag o config del `SessionDirector` dice qué implementación usar
para cada puerto.

## Affected Areas

| Área | Impacto | Descripción |
|---|---|---|
| `Runtime/Harness/SessionDirector.cs` (+`.asmdef`) | Nuevo | Lógica de elección, enrutado y cierre; ref: `NpcAi.Core` (+ los dobles vía `NpcAi.Core.*`? no — recibe puertos por interfaz) |
| `Data/Npcs/senora-mayor.json`, `joven.json` | Nuevo | Identidad mínima del NPC |
| `Samples~/Harness/Harness.unity` | Nuevo | Escena de escritorio sin VR |
| `Samples~/Harness/HarnessBehaviour.cs` | Nuevo | Cáscara: input de texto + botones de triaje |
| `Samples~/Harness/README.md` | Modificado | Cómo correr el arnés, qué está real y qué es doble |
| `Tests/EditMode/Harness/SessionDirectorTests.cs` | Nuevo | Elección reproducible, enrutado, cierre |
| `Docs/MODULES.md` | Modificado | Sección M11 |
| `NpcAi.Core` | Sin cambio | M11 solo consume puertos |

## Risks

| Riesgo | Prob. | Mitigación |
|---|---|---|
| `Samples~/` no se compila con el paquete → si la lógica queda ahí, no hay pruebas | Alta | `SessionDirector` va en `Runtime/Harness/`, fuera de `Samples~/`; solo la cáscara `MonoBehaviour` queda en `Samples~/` |
| Este cambio depende de 4 cambios previos mergeados (M0, M14, M15, M9) | Alta | Se puede entregar por etapas: primero `SessionDirector` contra **dobles** de M15/M9 (que sus cambios publican), y cablear los reales cuando estén. El arnés no bloquea en tener todo real |
| Conflicto en `Harness.unity` entre dos personas | Media | Regla 4/5 del repo: una escena por persona; M11 es de Jefferson. Conflicto en `.unity` se descarta y se rehace |
| `Data/Npcs/` hace que este cambio toque dos carpetas (`Samples~/` + `Data/` + `Runtime/Harness/`) | Media | Ver Open Questions: `Data/Npcs/` puede ser un micro-módulo aparte (M16) si el equipo es estricto. `Runtime/Harness/` + `Samples~/Harness/` se consideran el mismo módulo M11 (raíz de composición) |
| `SessionDirector` se convierte en un "módulo dios" que reimplementa lógica de otros | Media | Solo elige, enruta y delega. No clasifica, no genera, no evalúa, no puntúa: llama a los puertos |
| La aleatoriedad hace las demos no reproducibles para el jurado | Baja | Semilla explícita; el arnés muestra la semilla y permite fijarla |

## Rollback Plan

Revertir borra `Runtime/Harness/`, `Samples~/Harness/*.cs`/`.unity`, `Data/Npcs/` y las
pruebas. M11 vuelve a "solo README". Ningún otro módulo depende de `SessionDirector`. No hay
migración.

## Dependencies

- **`2026-09-09-m0-puerto-respuesta-clinica`**: `IClinicalResponder`, `ClinicalResponse`,
  `ClinicalCaseId` (y `Triage`/`ITriageBoard` si M9 los llevó a M0).
- **`2026-09-09-m14-catalogo-casos-clinicos`**: el catálogo que `SessionDirector` samplea.
- **`2026-09-09-m15-respondedor-clinico`**: al menos su **doble** (`ScriptedClinicalResponder`)
  para armar contra él; el real cuando esté.
- **`2026-09-09-m9-decision-triaje`**: al menos su doble; el `EmergencyScenarioObjective`
  real cuando esté.
- M2, M4, M5, M6, M13: sus dobles ya están en `main`; los reales donde existan.

## Success Criteria

- [ ] `SessionDirectorTests` en verde: misma semilla ⇒ misma terna `(caso, personalidad,
      npc)`; no repite el último caso; el enrutado llama a M15 y cae a M6 solo con
      `Handled == false`; `CerrarConTriaje` delega en M9 y marca la sesión terminada.
- [ ] `SessionDirector` vive en `Runtime/Harness/` (fuera de `Samples~/`) y su `.asmdef` no
      referencia otros módulos de `Runtime/` (recibe puertos por interfaz de `NpcAi.Core`).
- [ ] El arnés de escritorio corre en el Editor: se escribe un turno, el NPC responde
      (hecho clínico o frase social según corresponda), el progreso de M9 sube, y un botón
      de triaje cierra la sesión con veredicto.
- [ ] `Docs/MODULES.md` sección M11 actualizada.
- [ ] El diff no toca `NpcAi.Core`, `Runtime/Scenarios/Boardroom/`, ni módulos ajenos;
      se limita a `Runtime/Harness/`, `Samples~/Harness/`, `Data/Npcs/`,
      `Tests/EditMode/Harness/`, `Docs/`, `openspec/`.

## Decisiones del usuario (confirmadas 2026-09-09)

1. El **enrutado clínico/social vive en M11** (raíz de composición): "pregunta a M15; si
   `!Handled`, pregunta a M6".
2. `SessionDirector` elige cuerpo, caso y personalidad **por separado** y al azar, con
   semilla reproducible.
3. M11 es de Jefferson.
