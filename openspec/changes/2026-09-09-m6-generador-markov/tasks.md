# Tasks: M6 — Generador de diálogo por cadenas de Markov

## Review Workload Forecast

| Field | Value |
|---|---|
| Estimated changed lines | PR1 ~180 (corpus semilla JSON + README), PR2 ~250 (C# + pruebas), PR3 ~60 (spec + docs) |
| 400-line budget risk | Bajo por PR |
| Chained PRs recommended | Sí |
| Suggested split | PR1 → PR2 → PR3 |
| Chain strategy | stacked-to-main (PR1 → main, PR2 → PR1, PR3 → PR2) |

### Suggested Work Units

| Unit | Meta | PR | Prueba enfocada | Frontera de rollback |
|---|---|---|---|---|
| 1 | Corpus semilla (`Data/Dialogue/`) | PR1 | Manual: revisión de que cada bloque `(personalidad, receptividad)` tiene al menos una frase válida | Borrar `Data/Dialogue/`; ningún código lo referencia todavía |
| 2 | `MarkovChainBuilder` + `MarkovDialogueGenerator` | PR2 | EditMode: `MarkovChainBuilderTests`, `MarkovDialogueGeneratorTests : DialogueGeneratorContract` | Borrar `Runtime/Dialogue/MarkovChainBuilder.cs` y `MarkovDialogueGenerator.cs`; `ScriptedDialogueGenerator` sigue siendo el motor real, igual que hoy |
| 3 | Spec formal + documentación | PR3 | Manual: revisión de que `openspec/specs/generador-dialogo-m6/spec.md` traza a `MarkovDialogueGeneratorTests` | Revertir el commit de docs/spec; no afecta código |

## Phase 0: Guardrails (leer antes de escribir)

- [ ] 0.1 Frontera de escritura de PR1: SOLO `Data/Dialogue/` y este directorio de cambio
  (`openspec/changes/2026-09-09-m6-generador-markov/`). Nada en `Runtime/` todavía.
- [ ] 0.2 Frontera de escritura de PR2: `Runtime/Dialogue/MarkovChainBuilder.cs`,
  `Runtime/Dialogue/MarkovDialogueGenerator.cs`,
  `Tests/EditMode/Dialogue/MarkovDialogueGeneratorTests.cs`,
  `Tests/EditMode/Dialogue/MarkovChainBuilderTests.cs`. **No tocar
  `Runtime/Dialogue/Fakes/ScriptedDialogueGenerator.cs`** (Out of Scope — queda como doble de
  referencia).
- [ ] 0.3 Cero cambio en `Runtime/Core/Ports.cs`, `Dtos.cs`, `Enums.cs`, `PersonalityId.cs`.
  `IDialogueGenerator`, `NpcReply` no cambian una firma. Si algo de este cambio parece requerir
  tocar `Runtime/Core/` o `Runtime/CoreChannels/`, parar y avisar — cambio de contrato necesita
  co-revisión de todos los dueños de módulo, fuera de lo que este cambio autoriza.
- [ ] 0.4 `Tests/EditMode/Core/DialogueGeneratorContract.cs` no se modifica en ningún PR:
  `MarkovDialogueGeneratorTests` hereda, no edita, la base.
- [ ] 0.5 Este módulo es IA (M6): `/sdd-ff` no aplica — cada PR pasa por el flujo completo
  propuesta → diseño → tareas → revisión humana, sin atajos.
- [ ] 0.6 No confundir `Data/Dialogue/` (lo que dice el NPC, nuevo en este cambio) con
  `Data/Corpus/` (lo que dice el usuario, de M3) — son corpus distintos con propósitos
  distintos; no editar `Data/Corpus/` desde este cambio.

## Phase 1: Corpus semilla (PR1)

- [ ] 1.1 Definir el esquema JSON de `Data/Dialogue/<personalidad>.json`: un objeto con una
  clave por valor de `Receptivity` (`"Receptivo"`, `"Neutral"`, `"NoReceptivo"`), cada una con
  una lista de frases de ejemplo en el tono de esa personalidad.
- [ ] 1.2 Crear `Data/Dialogue/README.md`: explica el propósito del corpus (semilla para las
  cadenas de Markov de M6), el esquema, y la diferencia explícita con `Data/Corpus/` (M3, lo que
  dice el *usuario*, no el NPC).
- [ ] 1.3 Redactar `Data/Dialogue/grosero.json`, `histerico.json`, `introvertido.json`,
  `empatico.json` — mínimo 5 frases por bloque `(personalidad, receptividad)` (12 bloques en
  total), coherentes con la caracterización de cada personalidad ya usada en
  `Data/Personalities/<personalidad>.asset` (M5) y en las historias de usuario de la Fase 2 del
  cronograma.
- [ ] 1.4 Revisión cruzada por al menos un segundo integrante del equipo (mismo espíritu que la
  regla de doble etiquetado del 10% en `Data/Corpus/README.md`, aunque aquí no es una regla
  formal todavía): confirmar que el tono de cada bloque es reconociblemente distinto entre
  personalidades y entre estados de receptividad.

> PR1 es solo datos (JSON + README): no requiere Test Runner ni Unity Editor para validarse, solo
> revisión de contenido.

## Phase 2: `MarkovChainBuilder` + `MarkovDialogueGenerator` (PR2)

- [ ] 2.1 RED: `Tests/EditMode/Dialogue/MarkovChainBuilderTests.cs` — con una lista vacía,
  `Walk` devuelve `""` sin lanzar; con una sola frase de una palabra, `Walk` no lanza; con un
  corpus de varias frases, `Walk` siempre devuelve una cadena compuesta de palabras que aparecen
  en el corpus semilla (nunca inventa tokens fuera del vocabulario de entrada).
- [ ] 2.2 GREEN: Crear `Runtime/Dialogue/MarkovChainBuilder.cs` (tabla de bigramas, paseo
  aleatorio con `System.Random` inyectado para poder fijar semilla en pruebas, largo máximo de
  palabras como salvaguarda contra bucles).
- [ ] 2.3 RED: `Tests/EditMode/Dialogue/MarkovDialogueGeneratorTests.cs :
  DialogueGeneratorContract` — `CreateSubject()` carga el corpus semilla real de
  `Data/Dialogue/`.
- [ ] 2.4 GREEN: Crear `Runtime/Dialogue/MarkovDialogueGenerator.cs` implementando
  `IDialogueGenerator` (indexación por `(PersonalityId, Receptivity)`, reintento acotado +
  respaldo a frase semilla verbatim, tabla fija de `EmotionTag`/`AnimationCue` por
  `Receptivity`) hasta pasar la batería heredada completa (`Nunca_devuelve_texto_vacio`,
  `Las_etiquetas_nunca_son_nulas`, `Funciona_sin_personalidad_asignada`,
  `Un_NPC_no_receptivo_no_responde_igual_que_uno_receptivo`,
  `Generate_no_esta_obligado_a_ser_determinista`).
- [ ] 2.5 Prueba manual dedicada (no NUnit, script o botón de Editor): para las 4 personalidades
  × 3 estados, llamar `Generate` 1000 veces cada uno y confirmar 0 textos vacíos — cubre el caso
  de paseo degenerado con el corpus semilla chico de PR1, que un solo caso de prueba EditMode no
  ejercita por volumen.
- [ ] 2.6 Confirmar que ningún otro punto del código pasa a instanciar
  `MarkovDialogueGenerator` todavía de forma automática: la integración real en una escena de
  composición queda para M11 (Harness) cuando exista, según `design.md` → Migration/Rollout.

> PR2 depende del corpus semilla de PR1.

## Phase 3: Spec formal y documentación (PR3)

- [ ] 3.1 Crear `openspec/specs/generador-dialogo-m6/spec.md` — primera spec formal de M6,
  trazando los requisitos ya fijados por `DialogueGeneratorContract.cs` a
  `MarkovDialogueGeneratorTests` (mismo patrón que `perfiles-personalidad-m5/spec.md` usó sobre
  el esquema de M4).
- [ ] 3.2 Actualizar la sección M6 de `Docs/MODULES.md`: reemplazar "solo doble" por el estado
  real (generador por cadenas de Markov, tamaño del corpus semilla usado, referencia a la nueva
  spec).
- [ ] 3.3 Revisar que el diff acumulado de PR1+PR2+PR3 respeta exactamente la lista de
  `Success Criteria` de `proposal.md` (ninguna carpeta fuera de `Data/Dialogue/`,
  `Runtime/Dialogue/`, `Tests/EditMode/Dialogue/`, `Docs/`, `openspec/`).

## Phase 4: Cierre (acciones del autor, cada PR)

- [ ] 4.1 `git add` solo de las carpetas del PR correspondiente; `git diff --cached` antes de
  cualquier commit, confirmando que no se cruza a otro módulo.
- [ ] 4.2 Checklist "Antes de mergear" del `README.md` (pruebas propias en verde, diff acotado,
  spec/design/tasks archivados, rama al día con `main`, decisiones registradas).
- [ ] 4.3 Al cerrar el último PR: mover este cambio a `openspec/changes/archive/` con su
  `archive-report.md`.
- [ ] 4.4 PR mergeado a `main` por el autor (regla 8).

## Notas

- Ningún agente ejecuta Unity de forma autónoma: cada verde de Test Runner es compuerta humana,
  igual que en M1, M2, M4 y M5.
- PR1 es el único que puede avanzar sin escribir código — es una buena unidad para repartir entre
  quien redacta el corpus semilla y quien implementa `MarkovChainBuilder` en paralelo.
- Si el corpus semilla de PR1 resulta insuficiente una vez visto el resultado real en escena,
  ampliarlo es una edición de datos, no una reapertura de este cambio.
