# Training/Nlu — pipeline de entrenamiento offline de M2

Entrena el clasificador de intencion y tono de M2 y lo exporta a ONNX. Es Python
puro, vive fuera del paquete Unity, no lo empaqueta Unity y no lleva `.asmdef` ni
`.meta`. El runtime de Unity solo **infiere** sobre el `.onnx` que produce este
pipeline (eso es PR2).

Arquitectura (ver `openspec/changes/2026-09-09-m2-clasificador-bert-reducido/design.md`):
encoder BERT reducido pre-entrenado con **todos sus parametros congelados** (AD1);
sobre el **mismo embedding congelado**, dos cabezas pequenas (capa densa + softmax)
una para `Intent` y otra para `Tone` (AD2); exportacion a ONNX una sola vez (AD3).

## Requisitos

- Python 3.10 o superior.
- GPU NVIDIA opcional (una GTX 1660 de 6 GB alcanza para este volumen); sin GPU
  corre en CPU, mas lento.
- Internet **una sola vez** para descargar los pesos del encoder desde Hugging
  Face. Despues todo corre sin conexion.

## Instalacion

```bash
cd Training/Nlu
pip install -r requirements.txt
```

Las versiones estan fijadas a proposito: el modelo se debe poder re-derivar mas
adelante sin sorpresas de versiones.

## Paso 1 — preparar el corpus

```bash
python prepare_dataset.py
```

Carga `Data/Corpus/emergencia.json` y `Data/Corpus/juntas.json` (los dos archivos
listados en `CORPUS_FILES`), valida el esquema de cada entrada
(`text`/`intent`/`tone`/`scenario`/`labeler`, con `intent`/`tone` miembros validos
de `NpcAi.Core.Intent` / `NpcAi.Core.Tone`) y escribe el split combinado en
`Training/Nlu/data/` (`train.jsonl`, `val.jsonl`, `split_summary.json`).

El split se estratifica por la combinacion `intent x tone` **en la medida en que
el tamano de cada clase lo permita**, agrupando antes las frases casi identicas
(similitud de tokens >= 0.72) para que no queden separadas entre train y val (ver
`corpus_fuentes_ejemplos_triaje.md`, seccion 9). Las clases con un solo ejemplo no
se pueden estratificar: caen enteras a `train` y el script lo avisa por stderr, sin
abortar.

Opciones: `--corpus-dir`, `--out-dir`, `--val-fraction` (default `0.2`),
`--seed` (default `42`).

## Paso 2 — entrenar y exportar

```bash
python train.py --encoder sentence-transformers/paraphrase-multilingual-MiniLM-L12-v2
```

El encoder es un **argumento obligatorio, no esta hardcodeado**: asi se prueban
los candidatos del spike de Fase 1 sin editar el script. Candidatos razonables
para espanol colombiano hablado: `sentence-transformers/paraphrase-multilingual-MiniLM-L12-v2`,
`distilbert-base-multilingual-cased`, `microsoft/Multilingual-MiniLM-L12-H384`,
`huawei-noah/TinyBERT_General_4L_312D`.

Opciones: `--epochs` (30), `--batch-size` (16), `--lr` (1e-3), `--max-length`
(64), `--opset` (14), `--seed` (42), `--device` (`auto`/`cpu`/`cuda`),
`--onnx-out` (default `Runtime/Nlu/Models/intent-tone-classifier.onnx`).

`train.py` congela el encoder, entrena las dos cabezas, valida contra el split y
exporta el grafo (encoder + cabezas + softmax) a ONNX. Junto al `.onnx` guarda el
tokenizador en `tokenizer/`; PR2 lo necesita para alimentar Sentis con los mismos
`input_ids`.

## Como leer las metricas por clase

Al terminar, `train.py` imprime un `classification_report` de scikit-learn **por
separado para `Intent` y para `Tone`**, con precision, recall y F1 **por clase**
(no solo la agregada) sobre el split de validacion. El orden de clases es el de
`NpcAi.Core.Enums` (indice 0 = `Desconocida` / `Neutral`), el mismo que tendran
las salidas del `.onnx`.

Si alguna clase tiene menos de `LOW_SUPPORT_THRESHOLD` (5) ejemplos en el split de
validacion, `train.py` lo avisa por su nombre al final del reporte: su precision no
es fiable todavia. **Es un resultado esperado en ese caso**, no un fallo del
pipeline: se corrige ampliando el corpus para esa clase, no tocando estos scripts
(ver `Data/Corpus/PENDIENTE-AMPLIACION.md`). El aviso se calcula sobre el soporte
real de cada corrida, no sobre una clase fija, para no quedar desactualizado si el
corpus vuelve a cambiar.

## Reproducibilidad

`train.py` usa semilla fija (`--seed`, default `42`) e imprime al final un bloque
"Chequeo de reproducibilidad" con el `Intent`/`Tone` predicho para un conjunto
fijo de frases. Para confirmar reproducibilidad (tarea 1.7): correr `train.py` dos
veces sobre el mismo corpus y verificar que ese bloque sale identico en `Intent` y
`Tone` (la confianza y la latencia pueden variar minimamente, como permite el
contrato del puerto).

## Paso 3 — evaluacion mas a fondo (opcional)

```bash
python evaluate.py
```

`train.py` solo imprime `classification_report` (precision/recall/f1 por clase) y
el chequeo de reproducibilidad sobre 6 frases fijas. `evaluate.py` complementa eso:
carga el `.onnx` ya exportado (no entrena nada, no es una compuerta humana) y corre
inferencia sobre `val.jsonl` completo para imprimir la **matriz de confusion**
completa de `Intent` y de `Tone` — util para ver, por ejemplo, si una clase se
confunde sistematicamente con otra en particular o si sus errores estan repartidos
entre varias (ver el caso analizado en `Data/Corpus/PENDIENTE-AMPLIACION.md`,
seccion 6).

Opciones: `--onnx`, `--tokenizer`, `--val-jsonl`, `--max-length` (64); por defecto
apunta a la salida de `train.py`.

## Donde queda el `.onnx`

Por defecto en `Runtime/Nlu/Models/intent-tone-classifier.onnx`. **PR1 solo lo
genera**; quien lo copia al repo y lo commitea via Git LFS es PR2. El orden de
clases queda ademas grabado como metadata del propio `.onnx` (`intent_classes`,
`tone_classes`).

## Compuertas humanas

Correr el pipeline de punta a punta (tarea 1.6) y confirmar la reproducibilidad
(tarea 1.7) las ejecuta una persona con GPU y acceso a internet para la descarga
inicial del encoder: ningun agente entrena el modelo de forma autonoma, igual que
en M1, M4 y M5.
