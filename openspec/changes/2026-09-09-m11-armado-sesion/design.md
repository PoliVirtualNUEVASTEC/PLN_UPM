# Design: M11 — Armado de sesión

## Technical Approach

Dos piezas: `SessionDirector` (C# puro, en `Runtime/Harness/`, probable en EditMode) y el
arnés de escritorio (`Samples~/Harness/`, escena + `MonoBehaviour` delgado, no compilado
con el paquete). `SessionDirector` recibe los puertos por interfaz — reales o dobles —,
elige `(caso, personalidad, npc)` con una semilla, cablea el pipeline y expone
`ProcesarTurno` y `CerrarConTriaje`. El enrutado clínico/social es una rama simple dentro de
`ProcesarTurno`.

### Unidad de módulo y grafo de referencias

    NpcAi.Harness -> [NpcAi.Core]                 (Runtime/Harness/ — SessionDirector)
    Samples~/Harness/  -> HarnessBehaviour usa NpcAi.Harness + los ensamblados de dobles
                          reales que quiera cablear (esto es una app, no un módulo con
                          restricción de referencias)

`SessionDirector` referencia **solo `NpcAi.Core`**: recibe `ISpeechToText`,
`IIntentClassifier`, `IClinicalResponder`, `IDialogueGenerator`, `IReceptivityEngine`,
`INpcPresenter`, `IScenarioObjective` (y `ITriageBoard` si existe) por constructor. No
referencia M2, M6, M15… por nombre. El `HarnessBehaviour` (en `Samples~/`, que es una
aplicación de demostración) sí puede referenciar los ensamblados concretos para instanciar
los dobles o los reales.

## Architecture Decisions

| # | Decisión | Elección | Alternativas rechazadas | Razón |
|---|---|---|---|---|
| AD1 | Dónde vive la lógica | `SessionDirector` en `Runtime/Harness/` (nuevo `.asmdef`), fuera de `Samples~/` | Todo en `Samples~/Harness/` | `Samples~/` (con tilde) no lo compila Unity como parte del paquete: no habría pruebas EditMode. La lógica testeable tiene que estar en `Runtime/` |
| AD2 | Cómo recibe los puertos | Por constructor, como interfaces de `NpcAi.Core` | `SessionDirector` instancia los reales/dobles él mismo | Inyección deja a `SessionDirector` sin referencias a otros módulos y permite probarlo con dobles. Quién construye qué es decisión del `HarnessBehaviour` (o del proyecto VR anfitrión) |
| AD3 | Enrutado clínico/social | Rama en `ProcesarTurno`: `clin.Handled ? clin.Reply : m6.Generate(...)` | Un `ITurnRouter` puerto nuevo; un clasificador de "clínico vs social" | La señal `Handled` de `ClinicalResponse` ya resuelve el enrutado; un puerto nuevo sería superficie de contrato para una rama de una línea |
| AD4 | Aleatoriedad | `System.Random(seed)` propio de `SessionDirector`; semilla explícita, visible en el arnés | `UnityEngine.Random` (estado global, no reproducible); sin semilla | Reproducibilidad para pruebas y para demos ante el jurado. Evita repetir el último `ClinicalCaseId` guardándolo entre sesiones del mismo director |
| AD5 | Elección de cuerpo y caso | Independientes: `npc = sample(Data/Npcs)`, `caso = sample(Data/Cases, ≠ último)` | Ligar un caso a un cuerpo (`npcSugerido` en el caso) | Máxima variedad con 2 cuerpos × 3 casos × 4 personalidades = 24 combinaciones; el caso clínico no depende de la edad del cuerpo en estos 3 casos |
| AD6 | Identidad del NPC | `Data/Npcs/*.json` (`id`, `nombre`, `rangoEtario`, `vozId`), creado en este cambio | Hardcodear los 2 NPC en `SessionDirector`; meterlo en M5 o M14 | "Lo variable es dato". No va en M5 (personalidad ≠ cuerpo) ni en M14 (identidad ≠ caso). Ver Open Questions sobre si es micro-módulo propio |
| AD7 | Qué implementación por puerto | Config del `HarnessBehaviour`: real si está mergeada, doble si no | Solo dobles; solo reales | Igual criterio que el README de M11: reemplazo de a uno. La primera entrega probablemente: M1/M7/M8 dobles; M2/M4/M5/M6/M13/M15/M9 reales |

## Data Flow

    Inicio de sesión (arnés: escribir el nombre del NPC / proyecto VR: llamar por el nombre)
              │
              ▼
    new SessionDirector(seed, catalogoCasos, catalogoPersonalidades, catalogoNpcs, puertos...)
              │  elige (npc, caso ≠ último, personalidad)
              │  m15.AssignCase(caso.Id, personalidad);  m4.Reset(personalidad)
              │  m9 = new EmergencyScenarioObjective(configDesde(caso.clave))
              ▼
    ── por turno (texto del arnés o voz del anfitrión) ──
    ProcesarTurno(utterance):
        intent  = m2.Classify(utterance.Text)
        clin    = m15.Respond(utterance, intent)
        reply   = clin.Handled ? clin.Reply : m6.Generate(personalidad, m4.Current, intent)
        m8.Play(reply)
        m13.Registrar(utterance, reply)
        change  = m4.Evaluate(intent, PhysicalAction.Ninguna)
        m9.Notify(change)
        if (clin.Handled) m9.RegistrarHechoObtenido(clin.Campo)      // AD1 de M9
              │
    ── cierre (botón de triaje) ──
    CerrarConTriaje(Triage.II) → m9.EnviarTriaje(Triage.II) → TriajeVeredicto
              │  sesión terminada; el arnés muestra el veredicto

## File Inventory

| Archivo | Rol |
|---|---|
| `Runtime/Harness/NpcAi.Harness.asmdef` | `references: ["NpcAi.Core"]` |
| `Runtime/Harness/SessionDirector.cs` | Elección reproducible, `ProcesarTurno`, `CerrarConTriaje`, `enrutado` |
| `Runtime/Harness/NpcIdentity.cs` | POCO `{ Id, Nombre, RangoEtario, VozId }` + loader desde `Data/Npcs/` |
| `Data/Npcs/senora-mayor.json`, `Data/Npcs/joven.json` | Identidad de los 2 NPC |
| `Samples~/Harness/Harness.unity` | Escena de escritorio (Canvas: input de texto, log, botones I–V) |
| `Samples~/Harness/HarnessBehaviour.cs` | Construye los puertos (reales/dobles), instancia `SessionDirector`, conecta la UI |
| `Samples~/Harness/README.md` | Actualizado: cómo correr, qué está real y qué es doble, cómo fijar la semilla |
| `Tests/EditMode/Harness/NpcAi.Harness.Tests.asmdef` | Ref: `NpcAi.Core`, `NpcAi.Harness`, y los dobles necesarios |
| `Tests/EditMode/Harness/SessionDirectorTests.cs` | Elección reproducible, enrutado, cierre |

## Interfaces / Contracts

```csharp
// Runtime/Harness/SessionDirector.cs
public sealed class SessionDirector
{
    public SessionDirector(
        int seed,
        IReadOnlyList<ClinicalCaseId> casos,
        IReadOnlyList<PersonalityId> personalidades,
        IReadOnlyList<NpcIdentity> npcs,
        IIntentClassifier m2, IClinicalResponder m15, IDialogueGenerator m6,
        IReceptivityEngine m4, INpcPresenter m8, IScenarioObjective m9,
        ISessionRecorder m13 /* seam interno de M13 */);

    public ClinicalCaseId CasoActual { get; }
    public PersonalityId  PersonalidadActual { get; }
    public NpcIdentity    NpcActual { get; }
    public float          Progreso => /* m9.Progress01 */;

    public NpcReply ProcesarTurno(Utterance utterance);
    public TriajeVeredicto CerrarConTriaje(Triage categoria);   // tipo según AD1 de M9
    public bool     SesionTerminada { get; }
}
```

## Testing Strategy

| Archivo | Qué fija |
|---|---|
| `SessionDirectorTests` | Semilla fija ⇒ misma `(caso, personalidad, npc)` en 2 instancias; llamar `nuevaSesion` no repite el último `ClinicalCaseId`; con un `IClinicalResponder` doble que devuelve `Handled=true`, `ProcesarTurno` NO llama al `IDialogueGenerator` doble; con `Handled=false`, sí lo llama; `CerrarConTriaje` invoca `m9` y deja `SesionTerminada` |
| Dobles usados | `ScriptedIntentClassifier`, `ScriptedClinicalResponder`, `ScriptedDialogueGenerator`, `ScriptedReceptivityEngine`, `RecordingNpcPresenter`, `ScriptedScenarioObjective`, `InMemorySessionStore` — todos ya publicados por sus módulos |
| Arnés de escena | Verificación manual en el Editor (no EditMode): escribir turnos, ver respuestas, cerrar con triaje |

Ejecución EditMode: Test Runner de Unity 6. El arnés de `Samples~/` se prueba a mano.
Ningún agente ejecuta Unity: el verde y la demo son compuertas humanas.

## Migration / Rollout

Sin migración. El proyecto VR anfitrión, cuando exista su escena de sala de espera,
construye los puertos reales y usa el mismo `SessionDirector`. El arnés de escritorio queda
como banco de pruebas y, en Sprint 13-14, como instrumento de medición del Objetivo 4.

Rollback: revertir borra `Runtime/Harness/`, `Samples~/Harness/*`, `Data/Npcs/` y las
pruebas; M11 vuelve a "solo README".

## Open Questions

- [ ] ¿`Data/Npcs/` es parte de este cambio (M11 como raíz de composición posee su config)
      o un micro-módulo aparte (M16) para respetar "un cambio = una carpeta de `Data/`"?
      Recomendación: dentro de este cambio; promover a módulo propio solo si crece.
- [ ] ¿`SessionDirector` recibe el seam `ISessionRecorder` de M13 directamente, o M13 se
      auto-suscribe a canales y `SessionDirector` no lo conoce? Depende de si el arnés
      cablea M13 por canales (`UtteranceChannel` + canal de `NpcReply`) o por llamada
      directa. La versión por canales es más fiel al diseño de M13.
- [ ] ¿El arnés cablea `EmergencyScenarioObjective` real o su doble en la primera entrega?
      Depende de si `2026-09-09-m9-decision-triaje` ya está mergeado.
- [ ] ¿`CerrarConTriaje` recibe `Triage` (enum de M0, si M9 lo llevó allí) o un `int`/string
      que `SessionDirector` traduce? Se alinea con la decisión AD1 de M9.
