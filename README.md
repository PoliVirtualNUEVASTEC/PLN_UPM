# com.poli.npc-ai

Modulo de IA para personalidad y receptividad de NPC en entornos de VR/AR.
Paquete UPM consumido por el entorno de VR **por tag fijo**, nunca por rama.

Trabajo de grado (practica profesional) — Politecnico Colombiano Jaime Isaza Cadavid — proyecto macro FIN01.
Jefferson Estiven Aristizabal Quiceno · Luis Miguel Canaveral Restrepo · Asesor: Luis Fernando Gonzalez Alvaran

## Que hay aqui

`NpcAi.Core` es el contrato: tipos de datos, siete puertos y nada mas. **No contiene logica.**
Todo lo demas son modulos que se conocen entre si unicamente a traves de ese contrato.

| ID | Modulo | Carpeta | Expone |
|---|---|---|---|
| M0 | Nucleo de contratos | `Runtime/Core`, `Runtime/CoreChannels` | los 7 puertos y los DTO |
| M1 | Percepcion de voz | `Runtime/Speech` | `ISpeechToText` |
| M2 | Comprension de lenguaje | `Runtime/Nlu` | `IIntentClassifier` |
| M3 | Corpus y etiquetado | `Data/Corpus` | datos JSON |
| M4 | Motor de receptividad | `Runtime/Receptivity` | `IReceptivityEngine` |
| M5 | Perfiles de personalidad | `Data/Personalities` | 4 ScriptableObjects |
| M6 | Generador de dialogo | `Runtime/Dialogue` | `IDialogueGenerator` |
| M7 | Entrada fisica VR | `Runtime/VrInput` | `IPhysicalActionSource` |
| M8 | Presentador de NPC | `Runtime/Presentation` | `INpcPresenter` |
| M9 | Escenario Sala de Emergencia | `Runtime/Scenarios/Emergency` | `IScenarioObjective` |
| M10 | Escenario Sala de Juntas | `Runtime/Scenarios/Boardroom` | `IScenarioObjective` |
| M11 | Banco de pruebas | `Samples~/Harness` | herramienta |
| M12 | Documentacion | `Docs/` | manuales e informe |
| M13 | Bitacora de sesion | `Runtime/SessionLog` (planeado) | registro persistente por sesion |

## Duenos de modulo

Reparto inicial (sujeto a cambios; al reasignar, actualizar esta tabla).

| Dueno | Modulos |
|---|---|
| Luis Miguel Canaveral Restrepo | M1, M2, M3, M7, M9 |
| Jefferson Estiven Aristizabal Quiceno | M4, M5, M6, M8, M10, M11 |
| Compartido (Luis + Jefferson) | M0, M12 |
| Asignado | M13 |

## Las diez reglas de convivencia

1. **Nadie edita fuera de sus modulos.** Sin excepciones, ni "es un cambio chiquito".
2. **Si te falta algo de otro modulo, no esperas: usas su doble y sigues.**
3. **El contrato cambia en un cambio SDD propio**, revisado antes del merge por el otro dueno compartido de M0 (o el asesor). Sin dia fijo ni quorum de todos los duenos.
4. **Una escena por persona.** Nunca dos personas en la misma escena o el mismo prefab.
5. **Conflicto en `.unity` o `.prefab` -> se descarta y se rehace.** No se resuelve a mano.
6. **Ramas cortas: un cambio SDD no dura mas de tres dias.**
7. **`git add` solo de tus carpetas** antes de `gentle-ai review start --projection staged`, y siempre `git diff --cached` antes.
8. **Cada quien mergea su propio PR** una vez cumplida la checklist "Antes de mergear".
9. **`gentle-ai upgrade && gentle-ai sync` el mismo dia, los dos.**
10. **Toda decision relevante va al documento de contexto del proyecto**, ademas de a Engram.

## Antes de mergear (checklist del autor)

Cada quien mergea su propio PR cuando, y solo cuando, se cumple todo esto:

1. **Pruebas EditMode propias en verde**, incluidas las de contrato del modulo.
2. **El diff toca solo tus carpetas** — confirmado con `git diff --cached`.
3. **spec/design/tasks del cambio archivados en OpenSpec** y versionados en git.
4. **Doble publicado** y pasando las mismas pruebas de contrato que la implementacion real.
5. **Rama al dia con `main`**, sin conflictos; un conflicto en `.unity`/`.prefab`/`.asset` se descarta y se rehace (regla 5).
6. **Decisiones relevantes registradas** en el documento de contexto y en Engram (regla 10).
7. **Si es cambio de contrato (M0):** ademas, revisado antes del merge por el otro dueno compartido de M0 o por el asesor.

## Definition of Done de un modulo

1. `spec.md`, `design.md` y `tasks.md` archivados en OpenSpec y versionados en git.
2. Pruebas propias en verde, ejecutables sin VR y sin ningun otro modulo real.
3. Doble publicado y pasando las mismas pruebas de contrato que la implementacion real.
4. Receipt de revision generado con `--projection staged`.
5. Checklist "Antes de mergear" cumplida y PR mergeado a `main` por su autor.
6. Decisiones relevantes registradas en el documento de contexto del proyecto.

## Correr las pruebas

Unity -> `Window > General > Test Runner` -> pestana **EditMode** -> *Run All*.
Todas las pruebas de M0 corren sin escena, sin VR y sin ninguna implementacion real.
