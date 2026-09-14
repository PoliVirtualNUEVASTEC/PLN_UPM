# Pendiente de ampliación — `Data/Corpus/`

Estado de los dos archivos de corpus frente a la meta que fija
[`Data/Corpus/README.md`](README.md): **60-100 frases por categoría de intención, por
escenario**, en español colombiano hablado, etiquetadas por `intent` y `tone`.

| Archivo | Estado | Entradas | Consumido por `Training/Nlu/` |
|---|---|---|---|
| [`emergencia.json`](emergencia.json) | ✅ **Completado** (2026-09-10) | 600 (100 por intención) | Sí (`CORPUS_FILES`) |
| [`juntas.json`](juntas.json) | ✅ **Completado** (2026-09-14) | 600 (100 por intención) | Sí (`CORPUS_FILES`) |

Snapshot original: 2026-09-09, sobre `emergencia.json` (181) + `juntas.json` (180) tal como
estaban en `origin/main`. Este documento **no se actualiza solo**: si el corpus vuelve a
cambiar hay que recalcularlo.

---

## 1. `emergencia.json` — completado

Reescrito de raíz el 2026-09-10. Tres cosas cambiaron respecto del snapshot original:

1. **Voz del hablante.** El corpus estaba en voz del **paciente** (quien consulta). El
   clasificador de M2 clasifica lo que dice el **usuario/jugador**, que en el simulador de
   triaje es el **profesional de enfermería** — lo prueba el contrato M0:
   `IClinicalResponder.Respond(Utterance nurseUtterance, IntentResult intent)`. Las 600
   frases nuevas están en voz del enfermero dirigiéndose al paciente NPC.
2. **Volumen.** De 30 a **100 frases por intención** (techo de la meta del README).
3. **Balance de `Tone`.** `Empatico` pasó de **1 a 95**; ninguna clase queda por debajo de 95.

### Distribución final (600 entradas)

| Tone | Cantidad | % |
|---|---|---|
| Neutral | 150 | 25.0 % |
| Agresivo | 125 | 20.8 % |
| Respetuoso | 125 | 20.8 % |
| Ansioso | 105 | 17.5 % |
| Empatico | 95 | 15.8 % |

`python Training/Nlu/prepare_dataset.py` corre sin errores de esquema.

---

## 2. `juntas.json` — completado

Reescrito de raíz el 2026-09-14, con el mismo criterio que `emergencia.json`. Cuatro cosas
cambiaron respecto del snapshot original:

1. **Escenario redefinido.** El corpus original modelaba una reunión de junta directiva
   (ventas del trimestre, presupuesto). El escenario real de M10 es **levantamiento de
   requerimientos**: el usuario/jugador es un **analista de requerimientos** que entra a la
   sala de juntas y, mediante preguntas dirigidas, le extrae al NPC (un interesado/cliente)
   la información necesaria para modelar un sistema. Los casos de uso concretos están en
   [`Casos_de_uso_Sala_Juntas.md`](../../Casos_de_uso_Sala_Juntas.md) (torneo de fútbol,
   minimercado, restaurante escolar, aerolínea) y las frases quedaron ancladas a esos cuatro
   dominios en vez de a generalidades corporativas.
2. **Voz del hablante.** Igual que en emergencia: las 600 frases están en voz del
   **analista**, no del interesado. `SolicitudRespetuosa`/`SolicitudAgresiva` son preguntas
   que el analista le hace al NPC; `AportaInformacion` es el analista confirmando o
   resumiendo lo que entendió, no el interesado explicando su negocio.
3. **Volumen.** De 30 a **100 frases por intención**.
4. **Balance de `Tone`.** `Empatico` pasó de **0 a 95**.

### Distribución final (600 entradas)

| Tone | Cantidad | % |
|---|---|---|
| Neutral | 150 | 25.0 % |
| Agresivo | 125 | 20.8 % |
| Respetuoso | 125 | 20.8 % |
| Ansioso | 105 | 17.5 % |
| Empatico | 95 | 15.8 % |

### Crosstab intención × tono (mismo reparto natural que en `emergencia.json`)

No es una grilla rígida 20×5: `SolicitudAgresiva` e `Interrupcion` no llevan `Empatico`
(se contradice con la intención); `SolicitudRespetuosa` y `Empatia` no llevan `Agresivo`.

| Intent \ Tone | Agresivo | Ansioso | Empatico | Neutral | Respetuoso |
|---|---|---|---|---|---|
| AportaInformacion | 20 | 20 | 20 | 20 | 20 |
| Empatia | 0 | 10 | 45 | 20 | 25 |
| Interrupcion | 35 | 15 | 0 | 25 | 25 |
| PreguntaFueraDeTema | 0 | 15 | 10 | 55 | 20 |
| SolicitudAgresiva | 70 | 30 | 0 | 0 | 0 |
| SolicitudRespetuosa | 0 | 15 | 20 | 30 | 35 |

### Pipeline combinado

`Training/Nlu/prepare_dataset.py` ya lista `juntas.json` en `CORPUS_FILES` y corre limpio
sobre los dos escenarios juntos:

```
corpus: 1200 entradas (emergencia.json, juntas.json)
split: train=960  val=240
intent: 160/40 por clase, las 6 parejas iguales
tone train: Agresivo 200, Ansioso 168, Empatico 152, Neutral 240, Respetuoso 200
```

Estratificado sin avisos de clases sin ejemplos.

---

## 3. Acuerdo entre etiquetadores — sin aplicar todavía

`Data/Corpus/README.md` fija como regla de calidad: **10 % de las frases doble-etiquetadas
para medir acuerdo entre etiquetadores**. Hoy `labeler` es `"Luis"` en el 100 % de las 1200
entradas combinadas (600 + 600) — no hay ni una frase etiquetada por una segunda persona.

**Pendiente concreto:** un segundo integrante etiqueta independientemente al menos el 10 %
(≈ 120 de las 1200) para poder calcular una métrica de acuerdo (p. ej. Cohen's kappa). Sin
esta medición no hay forma de saber si las etiquetas de `intent`/`tone` son consistentes
entre personas o reflejan el criterio de uno solo.

---

## 4. Resumen accionable

| Pendiente | Prioridad | Alcance |
|---|---|---|
| ~~Ampliar y rebalancear `emergencia.json`~~ | ✅ Hecho | 600 entradas en voz de enfermero, `Empatico` 1→95 |
| ~~Ampliar y rebalancear `juntas.json`~~ | ✅ Hecho | 600 entradas en voz de analista, escenario redefinido a toma de requerimientos, `Empatico` 0→95 |
| Doble etiquetado del 10 % por un segundo etiquetador | Media | 0 frases doble-etiquetadas todavía; regla de `README.md` sin aplicar |

Este documento no reemplaza a `Data/Corpus/README.md` (que define el esquema y la meta) —
es un snapshot puntual de la brecha frente a esa meta.
