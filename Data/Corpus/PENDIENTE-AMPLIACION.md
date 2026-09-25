# Pendiente de ampliación — `Data/Corpus/`

Estado de los dos archivos de corpus frente a la meta que fija
[`Data/Corpus/README.md`](README.md): **60-100 frases por categoría de intención, por
escenario**, en español colombiano hablado, etiquetadas por `intent` y `tone`.

| Archivo | Estado | Entradas | Consumido por `Training/Nlu/` |
|---|---|---|---|
| [`emergencia.json`](emergencia.json) | ✅ **Ampliado** (2026-09-18) | 5.000 (833-834 por intención) | Sí (`CORPUS_FILES`) |
| [`juntas.json`](juntas.json) | ✅ **Ampliado** (2026-09-23) | 5.000 (833-834 por intención) | Sí (`CORPUS_FILES`) |

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

### Ampliación a 5.000 (2026-09-18)

Las 600 frases anteriores se conservaron intactas y se sumaron 4.400 nuevas, en voz del
profesional (enfermero/médico), sin frases repetidas (5.000 textos únicos tras normalizar
acentos y puntuación, y sin pares con similitud de tokens ≥ 0,72). Criterios que se
siguieron de `corpus_fuentes_ejemplos_triaje.md`: variación de largo y registro, frases con
pausas y oralidad colombiana, y ejemplos contrastivos para que "por favor", "tranquilo" o
"!" no delaten por sí solos el tono.

- **Reparto por intención:** 833 (×4) y 834 (×2) = 5.000.
- **Reparto por tono:** Neutral 1.192, Respetuoso 1.042, Ansioso 968, Agresivo 966, Empatico 832.
- **No es una grilla 6×5 uniforme.** Las celdas contradictorias por definición
  (`SolicitudAgresiva`+`Empatico`/`Respetuoso`/`Neutral`, `SolicitudRespetuosa`+`Agresivo`,
  `Empatia`+`Agresivo`, `Interrupcion`+`Empatico`, `PreguntaFueraDeTema`+`Agresivo`) siguen
  sin existir, igual que antes; por eso las 23 celdas válidas tienen entre 100 y 550 frases
  en vez de 166 cada una.
- `labeler` sigue siendo `"Luis"` en el 100 % de las entradas. Las 4.400 nuevas fueron
  generadas con apoyo de IA y **no** han pasado revisión humana (§3.5 del documento de
  fuentes exige esa revisión antes de darlas por buenas).
- `prepare_dataset.py` corre sin avisos ni errores: 5.600 entradas (con `juntas.json`),
  train=4.480, val=1.120.

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

### Ampliación a 5.000 (2026-09-23)

Las 600 frases anteriores se conservaron intactas y se sumaron 4.400 nuevas, en voz del
analista, ancladas a los 10 escenarios y a la guía de
`guia_corpus_analista_requerimientos.md` (compartida por el usuario): pares mínimos
intención/tono, muletillas colombianas de oficina, y ejemplos contrastivos para que "ya",
"por favor" o "!" no delaten por sí solos el tono. Sin frases repetidas (5.000 textos
únicos tras normalizar acentos y puntuación, y sin pares con similitud de tokens ≥ 0,72),
y sin solapes nuevos con `emergencia.json` (los 15 que hay ya existían en las 600
originales de ambos archivos).

- **Reparto por intención:** 833 (×4) y 834 (×2) = 5.000, igual que en `emergencia.json`.
- **Reparto por tono:** Respetuoso 1.198, Agresivo 1.055, Neutral 968, Ansioso 903, Empatico 876.
- **Grilla 6×6 completa, a diferencia de `emergencia.json`.** La guía del usuario sí admite
  `Interrupcion`+`Empatico` y `PreguntaFueraDeTema`+`Agresivo` (aquí el NPC es el
  interesado/cliente, no el paciente), así que las 24 combinaciones válidas están todas
  representadas, de 104 a 1.198 frases cada una según su peso en la guía.
- `labeler` es `"Luis"` en las 600 originales y `"Nataly"` en las 4.400 nuevas — ninguna
  entrada quedó con `"claude"`. Las 4.400 nuevas fueron generadas con apoyo de IA y
  **no** han pasado revisión humana.
- `prepare_dataset.py` corre sin avisos ni errores: 10.000 entradas (con `emergencia.json`),
  train=8.000, val=2.000.

---

## 3. Acuerdo entre etiquetadores — sin aplicar todavía

`Data/Corpus/README.md` fija como regla de calidad: **10 % de las frases doble-etiquetadas
para medir acuerdo entre etiquetadores**. Hoy `labeler` es `"Luis"` o `"Nataly"` en el
100 % de las 10.000 entradas combinadas — no hay ni una frase con doble etiqueta
independiente de un tercer etiquetador.

**Pendiente concreto:** un segundo integrante (distinto de quien puso `"Nataly"`) etiqueta
independientemente al menos el 10 % (≈ 1.000 de las 10.000) para poder calcular una
métrica de acuerdo (p. ej. Cohen's kappa). Sin esta medición no hay forma de saber si las
etiquetas de `intent`/`tone` son consistentes entre personas o reflejan el criterio de una sola.

---

## 4. Resumen accionable

| Pendiente | Prioridad | Alcance |
|---|---|---|
| ~~Ampliar y rebalancear `emergencia.json`~~ | ✅ Hecho | 5.000 entradas en voz de enfermero, `Empatico` 1→876+ |
| ~~Ampliar y rebalancear `juntas.json`~~ | ✅ Hecho | 5.000 entradas en voz de analista, grilla 6×6 completa |
| ~~Split train/val sin agrupar frases casi-idénticas~~ | ✅ Hecho (2026-09-23) | Ver sección 5 |
| ~~Entrenar y evaluar M2 con el corpus ampliado~~ | ✅ Hecho (2026-09-25) | Ver sección 6 |
| Revisión humana de las 8.800 frases generadas con IA | Alta | 0 revisadas todavía. Al revisar, prestar atención especial a los límites `AportaInformacion` / `PreguntaFueraDeTema` vs. `SolicitudAgresiva` — ver hallazgo en sección 6 |
| Doble etiquetado del 10 % por un segundo etiquetador | Media | 0 frases doble-etiquetadas todavía; regla de `README.md` sin aplicar |
| Repetir la comparación de 4 candidatos de encoder (tarea 1.6) sobre el corpus de 10.000 | Alta | 0 hecho. El encoder commiteado hoy (`distilbert-base-multilingual-cased`) ganó esa comparación el 2026-09-14 sobre un corpus 8× más chico (1.200 entradas); no hay garantía de que siga ganando. Script nuevo: `Training/Nlu/compare_encoders.py` |
| Reentrenar con el encoder elegido y reemplazar el `.onnx` vía Git LFS | Media | Depende de la fila anterior. Generado hoy con MiniLM (no el commiteado) en `Runtime/Nlu/Models/`, sin exportar todavía el definitivo; ver `Training/Nlu/README.md` |

---

## 5. Split train/val: frases casi-idénticas ya quedan agrupadas (2026-09-23)

`corpus_fuentes_ejemplos_triaje.md` (sección 9) exige que "las frases muy parecidas o
las variaciones de una misma frase" permanezcan en la misma partición, para que la
métrica de validación no salga inflada por fuga de datos. `Training/Nlu/prepare_
dataset.py` repartía al azar sin esa regla; ya se corrigió.

**Qué cambió:** `stratified_split` ahora arma primero clusters de frases con similitud
de tokens (Jaccard) ≥ 0.72 — el mismo umbral usado para descartar casi-duplicados al
construir el corpus — y reparte esos clusters como unidad indivisible entre train y
val, nunca frases sueltas dentro de un cluster.

**Resultado medido sobre las 10.000 entradas combinadas:**
- 40 grupos de frases casi-idénticas (80 entradas) detectados y mantenidos juntos.
  Antes de este cambio, cualquiera de esos 40 pares podía quedar partido entre train
  y val por puro azar del `seed`.
- Incluye las 15 frases idénticas que ya existían entre `emergencia.json` y
  `juntas.json` (ver sección 1): las 15 quedaron confirmadas del mismo lado.
- 5 de esos grupos mezclan clases `intent||tone` distintas (mismo texto casi calcado,
  etiquetas diferentes); no se pueden repartir sin sesgar la estratificación, así que
  caen enteros a `train` con aviso por stderr — quedan disponibles para revisión manual
  si alguien quiere usarlos también en validación.
- El tamaño final del split prácticamente no cambia: train=8.002 / val=1.998 (antes
  8.000/2.000 exactos); la diferencia es el costo de no partir esos 40 grupos.
- Verificado con un script aparte: cero frases (exactas o del mismo cluster) aparecen
  a la vez en `train.jsonl` y en `val.jsonl`.

No se tocó el documento fuente (`corpus_fuentes_ejemplos_triaje.md`): el requisito
sigue ahí, y ahora el código efectivamente lo cumple.

---

## 6. Entrenamiento y evaluación de M2 (2026-09-25)

Corrida end-to-end del pipeline (`Training/Nlu/train.py`) sobre el corpus ampliado de
10.000 entradas, por una persona con GPU/CPU y acceso a internet, según exige la
sección "Compuertas humanas" de `Training/Nlu/README.md` (ningún agente entrena el
modelo de forma autónoma).

```
python train.py --encoder sentence-transformers/paraphrase-multilingual-MiniLM-L12-v2 --device cpu
```

**Resultado (split de validación, 1.998 entradas):**

| | accuracy global |
|---|---|
| Intent | 0.778 |
| Tone | 0.774 |

Ninguna clase con soporte real colapsó a 0 (la única que el script marca con pocos
ejemplos es `Intent.Desconocida`, con 0 en val, porque esa clase nunca aparece en el
corpus — es el valor "sin clasificar" del enum, no una intención que se etiquete).

**Reproducibilidad (tarea 1.7): confirmada, corrida tres veces.** No solo coincidió
la clase predicha para las 6 frases fijas de `REPRO_PHRASES` — coincidieron también
las probabilidades, dígito por dígito, y la curva de loss completa de las tres
corridas. Determinismo total en CPU con `seed=42`.

**Hallazgo inicial (de las 6 frases fijas) y su confirmación con la matriz de
confusión completa:** de las 6 frases del chequeo de reproducibilidad, 2 salieron
con `Intent` predicho con baja confianza — `"la presion esta en catorce sobre
noventa"` → `SolicitudAgresiva` (p=0.448) en vez de `AportaInformacion`, y `"y usted
donde compro esa corbata tan fea"` → `SolicitudAgresiva` (p=0.559) en vez de
`PreguntaFueraDeTema`. Con solo 2 de 6 frases no se podía saber si era un patrón
sistemático o ruido puntual, así que se corrió `Training/Nlu/evaluate.py` (nuevo,
ver más abajo) para mirar la matriz de confusión completa sobre las 1.998 entradas
de validación. **Resultado: no es un sesgo específico hacia `SolicitudAgresiva`.**

- `PreguntaFueraDeTema→SolicitudAgresiva`: 5 de 333 (1.5 %) — es la intención mejor
  clasificada de las seis (88.9 % de acierto); esa frase concreta fue ruido, no una
  debilidad real.
- `AportaInformacion→SolicitudAgresiva`: 25 de 333 (7.5 %) — real, pero no es "la"
  confusión de esa clase. `AportaInformacion` es una de las dos intenciones más
  débiles (72.7 % de acierto, junto con `Interrupcion` en 72.1 %), y sus errores se
  reparten casi igual entre `SolicitudAgresiva` (25), `Empatia` (25) e
  `Interrupcion` (19) — no hay un imán específico hacia "solicitud agresiva".

**Hallazgo más grande, no anticipado:** en `Tone`, la confusión más fuerte de toda
la matriz es `Agresivo↔Neutral` — 68 de 404 frases de `Agresivo` real (16.8 %) se
predijeron como `Neutral`, y 36 de 431 de `Neutral` real (8.4 %) como `Agresivo`.
`Agresivo` es el tono con menor recall (70.3 %).

Vale la pena que quien haga la revisión humana del corpus (sección 3) revise en
particular cómo están etiquetadas las fronteras `AportaInformacion` /
`Interrupcion` (las intenciones más débiles) y `Agresivo` / `Neutral` (la confusión
de tono más grande) — no la pareja que se sospechaba al principio.

**`.onnx` generado, no commiteado.** `Runtime/Nlu/Models/intent-tone-classifier.onnx`
y `Runtime/Nlu/Models/tokenizer/` quedaron en el disco de quien entrenó. Subirlos al
repo vía Git LFS es tarea de "PR2" (ver `Training/Nlu/README.md`), fuera del alcance
de este documento.

**Nuevo script `Training/Nlu/evaluate.py`.** Carga el `.onnx` ya exportado y corre
inferencia sobre `val.jsonl` completo (no requiere entrenar, no es una compuerta
humana): imprime la matriz de confusión por clase para `Intent` y `Tone`. Se usó
para el análisis de arriba; ver `Training/Nlu/README.md` para el uso.

Este documento no reemplaza a `Data/Corpus/README.md` (que define el esquema y la meta) —
es un snapshot puntual de la brecha frente a esa meta.
