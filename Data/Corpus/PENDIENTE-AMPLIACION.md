# Pendiente de ampliación — `Data/Corpus/`

Este documento es el análisis línea por línea de qué le falta al corpus actual
(`emergencia.json` + `juntas.json`) para llegar a la meta que ya fija
[`Data/Corpus/README.md`](README.md): **60-100 frases por categoría de intención, por
escenario**. Se generó el 2026-09-09, contando directamente sobre los dos archivos tal como
están commiteados en `origin/main` (PR "docs/corpus-m3-y-modulos-m13", 2026-09-04).

Es un entregable de referencia para quien ejecute la actividad 3.1 del cronograma
("Preparación y ampliación del corpus etiquetado de enunciados") y para el pipeline de
entrenamiento de `Training/Nlu/` (cambio `2026-09-09-m2-clasificador-bert-reducido`), que
consume este corpus tal como esté cuando se entrene.

## 1. Volumen por intención — brecha frente a la meta (60-100)

Ambos archivos tienen exactamente 30 frases por intención (emergencia.json tiene 31 en
`Empatia`, por una frase de más). La meta del README es 60-100 por categoría **por escenario**,
así que la brecha se calcula por archivo, no sobre el total combinado.

### `emergencia.json` (181 entradas)

| Intent | Actual | Meta (mínimo) | Faltan (mínimo) | Faltan (para 100) |
|---|---|---|---|---|
| SolicitudRespetuosa | 30 | 60 | 30 | 70 |
| SolicitudAgresiva | 30 | 60 | 30 | 70 |
| Empatia | 31 | 60 | 29 | 69 |
| AportaInformacion | 30 | 60 | 30 | 70 |
| PreguntaFueraDeTema | 30 | 60 | 30 | 70 |
| Interrupcion | 30 | 60 | 30 | 70 |

### `juntas.json` (180 entradas)

| Intent | Actual | Meta (mínimo) | Faltan (mínimo) | Faltan (para 100) |
|---|---|---|---|---|
| SolicitudRespetuosa | 30 | 60 | 30 | 70 |
| SolicitudAgresiva | 30 | 60 | 30 | 70 |
| Empatia | 30 | 60 | 30 | 70 |
| AportaInformacion | 30 | 60 | 30 | 70 |
| PreguntaFueraDeTema | 30 | 60 | 30 | 70 |
| Interrupcion | 30 | 60 | 30 | 70 |

**Lectura**: los dos archivos están exactamente al 50% del mínimo de la meta (30 de 60) en
las 12 celdas intención×escenario, sin excepción. No hay una categoría "casi lista" ni una
"crítica" en términos de volumen puro — la brecha es uniforme. Faltan **179 frases** en
`emergencia.json` y **180 frases** en `juntas.json` solo para tocar el piso de la meta (349 en
total); llegar al techo (100) requiere 419 y 420 respectivamente (839 en total).

## 2. Desbalance de `Tone` — no lo resuelve solo ampliar el volumen

El README no fija una meta explícita por `Tone`, pero el desbalance actual es severo al punto de
que duplicar el volumen sin corregirlo específicamente no soluciona nada: si se agregan 30
frases más por intención en la misma proporción de tono que hoy, `Empatico` seguiría teniendo
prácticamente cero ejemplos.

### `emergencia.json` — distribución de `Tone` (181 entradas)

| Tone | Cantidad | % |
|---|---|---|
| Neutral | 61 | 33.7% |
| Ansioso | 42 | 23.2% |
| Respetuoso | 38 | 21.0% |
| Agresivo | 39 | 21.5% |
| **Empatico** | **1** | **0.6%** |

### `juntas.json` — distribución de `Tone` (180 entradas)

| Tone | Cantidad | % |
|---|---|---|
| Neutral | 75 | 41.7% |
| Agresivo | 47 | 26.1% |
| Respetuoso | 42 | 23.3% |
| Ansioso | 16 | 8.9% |
| **Empatico** | **0** | **0%** |

**`Tone.Empatico` es la brecha crítica del corpus**: 1 ejemplo en `emergencia.json`, 0 en
`juntas.json`, de 361 frases combinadas. Ningún clasificador — por reglas o entrenado — puede
aprender a reconocer una clase con 0-1 ejemplos. Esta es la razón por la que el cambio
`2026-09-09-m2-clasificador-bert-reducido` marca este punto como el riesgo de más alta
probabilidad de todo el cambio (ver su `proposal.md` → `Risks`).

**`Tone.Ansioso` en `juntas.json` (16 de 180, 8.9%) es una brecha secundaria**: no es cero, pero
está claramente por debajo de las demás clases de ese archivo — vale la pena priorizarlo también
al redactar frases nuevas, aunque no sea tan grave como `Empatico`.

### Crosstab intención × tono (para dirigir la redacción de frases nuevas)

La siguiente tabla muestra cuántas frases hay hoy por cada combinación intención×tono, para que
quien redacte frases nuevas sepa exactamente qué celdas llenar primero (las marcadas en 0 o 1 son
las más urgentes).

**`emergencia.json`**

| Intent \ Tone | Agresivo | Ansioso | Empatico | Neutral | Respetuoso |
|---|---|---|---|---|---|
| AportaInformacion | 0 | 8 | **0** | 21 | 1 |
| Empatia | 1 | 10 | **1** | 9 | 10 |
| Interrupcion | 9 | 6 | **0** | 7 | 8 |
| PreguntaFueraDeTema | 0 | 9 | **0** | 16 | 5 |
| SolicitudAgresiva | 28 | 2 | **0** | 0 | 0 |
| SolicitudRespetuosa | 1 | 7 | **0** | 8 | 14 |

**`juntas.json`** (este archivo no tiene ninguna frase con `Tone.Empatico`; columna omitida)

| Intent \ Tone | Agresivo | Ansioso | Neutral | Respetuoso |
|---|---|---|---|---|
| AportaInformacion | 0 | 6 | 24 | 0 |
| Empatia | 2 | 2 | 13 | 13 |
| Interrupcion | 10 | 6 | 2 | 12 |
| PreguntaFueraDeTema | 1 | 0 | 26 | 3 |
| SolicitudAgresiva | 30 | 0 | 0 | 0 |
| SolicitudRespetuosa | 4 | 2 | 10 | 14 |

**Sugerencia de priorización al redactar**: enfocar primero `Empatia` × `Empatico` (la
combinación semánticamente más natural — la intención "Empatía" debería tener representación en
tono empático, no solo neutral/respetuoso) en ambos archivos, y luego cubrir al menos un puñado
de frases con `Tone.Empatico` en las demás intenciones donde tenga sentido (p. ej.
`AportaInformacion` dicho con calidez), para que el modelo tenga alguna variedad de contexto y no
solo memorice una única frase.

## 3. Acuerdo entre etiquetadores — regla de calidad sin aplicar todavía

`Data/Corpus/README.md` fija como regla de calidad: **10% de las frases doble-etiquetadas para
medir acuerdo entre etiquetadores**. Hoy el campo `labeler` es `"Luis"` en el 100% de las 361
entradas combinadas (181 + 180) — no hay una sola frase etiquetada por una segunda persona, así
que el acuerdo entre etiquetadores no se puede medir todavía.

**Pendiente concreto**: sobre el corpus ampliado (o incluso sobre una muestra del actual, como
punto de partida), un segundo integrante del equipo debe etiquetar independientemente al menos
el 10% de las frases (≈ 36 de 361 hoy; la cifra crece con la ampliación) para poder calcular una
métrica de acuerdo (p. ej. Cohen's kappa) entre los dos etiquetadores. Sin esta medición, no hay
forma de saber si las etiquetas de `intent`/`tone` son consistentes entre personas o reflejan el
criterio de una sola.

## 4. Resumen accionable

| Pendiente | Prioridad | Alcance |
|---|---|---|
| Ampliar volumen a 60-100 por intención por escenario | Alta | 349 frases mínimo (839 para el techo), repartidas en las 12 celdas intención×escenario por igual |
| Cubrir `Tone.Empatico` en ambos archivos | Crítica | Hoy 1 de 361; sin esto, ningún modelo entrenado puede reconocer esta clase |
| Reforzar `Tone.Ansioso` en `juntas.json` | Media | Hoy 16 de 180 (8.9%), claramente por debajo de las demás clases de ese archivo |
| Doble etiquetado del 10% por un segundo etiquetador | Media | Ninguna frase doble-etiquetada todavía; regla de `README.md` sin aplicar |

Este documento no reemplaza a `Data/Corpus/README.md` (que define el esquema y la meta) — es un
snapshot puntual de la brecha frente a esa meta, para que la actividad 3.1 del cronograma tenga
un punto de partida concreto en vez de "ampliar el corpus" en abstracto. Si el corpus cambia,
este análisis debe recalcularse — no se actualiza automáticamente.
