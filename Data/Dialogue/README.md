# M6 — Corpus semilla de diálogo

Frases de ejemplo que **dice el NPC** (el paciente), en español hablado. Son la
semilla de las cadenas de Markov de M6 (`MarkovDialogueGenerator`): no se
devuelven tal cual, se usan para construir una tabla de bigramas y generar texto
nuevo por paseo aleatorio.

- Un archivo por personalidad: `grosero.json`, `histerico.json`,
  `introvertido.json`, `empatico.json`. Son las 4 personalidades de M5
  (`Data/Personalities/<personalidad>.asset`).
- Esquema por archivo: un objeto con una lista de frases por estado de
  `NpcAi.Core.Receptivity`.

  ```json
  {
    "personalidad": "grosero",
    "Receptivo":    ["...", "..."],
    "Neutral":      ["...", "..."],
    "NoReceptivo":  ["...", "..."]
  }
  ```

- Mínimo 5 frases por bloque `(personalidad, estado)` — 12 bloques en total.
- El tono de cada bloque debe ser reconociblemente distinto entre personalidades
  y entre estados. Lo fija `DialogueGeneratorContract`
  (`Un_NPC_no_receptivo_no_responde_igual_que_uno_receptivo`).

## No confundir con `Data/Corpus/` (M3)

| | `Data/Corpus/` (M3) | `Data/Dialogue/` (M6, acá) |
|---|---|---|
| Quién habla | El **usuario** (enfermero en formación) | El **NPC** (paciente) |
| Para qué | Entrenar el clasificador de intención/tono de M2 | Sembrar las cadenas de Markov de M6 |
| Etiquetas | `intent` y `tone` por frase | Ninguna: solo el estado de receptividad agrupa |
| Consumidor | `Training/Nlu/` (entrenamiento offline) | `MarkovDialogueGenerator` en runtime |

Ampliar este corpus o ajustar el tono es una edición de datos: **no** reabre el
cambio SDD de M6.
