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

## Las diez reglas de convivencia

1. **Nadie edita fuera de sus modulos.** Sin excepciones, ni "es un cambio chiquito".
2. **Si te falta algo de otro modulo, no esperas: usas su doble y sigues.**
3. **El contrato solo cambia los lunes**, en un cambio SDD propio y co-aprobado por todos los duenos.
4. **Una escena por persona.** Nunca dos personas en la misma escena o el mismo prefab.
5. **Conflicto en `.unity` o `.prefab` -> se descarta y se rehace.** No se resuelve a mano.
6. **Ramas cortas: un cambio SDD no dura mas de tres dias.**
7. **`git add` solo de tus carpetas** antes de `gentle-ai review start --projection staged`, y siempre `git diff --cached` antes.
8. **Nadie mergea su propio PR.**
9. **`gentle-ai upgrade && gentle-ai sync` el mismo dia, los dos.**
10. **Toda decision relevante va al documento de contexto del proyecto**, ademas de a Engram.

## Definition of Done de un modulo

1. `spec.md`, `design.md` y `tasks.md` archivados en OpenSpec y versionados en git.
2. Pruebas propias en verde, ejecutables sin VR y sin ningun otro modulo real.
3. Doble publicado y pasando las mismas pruebas de contrato que la implementacion real.
4. Receipt de revision generado con `--projection staged`.
5. PR aprobado por otra persona y mergeado a `main`.
6. Decisiones relevantes registradas en el documento de contexto del proyecto.

## Correr las pruebas

Unity -> `Window > General > Test Runner` -> pestana **EditMode** -> *Run All*.
Todas las pruebas de M0 corren sin escena, sin VR y sin ninguna implementacion real.
