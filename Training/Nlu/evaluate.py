"""Evalua un .onnx ya entrenado sobre el split de validacion: matriz de confusion completa.

A diferencia de train.py, este script no entrena nada: carga un grafo .onnx ya
exportado (encoder + cabezas + softmax) y corre inferencia sobre val.jsonl para dar
una foto mas completa que el bloque de "Chequeo de reproducibilidad" de train.py, que
solo cubre 6 frases fijas. En particular, imprime la matriz de confusion por clase y
resalta la confusion entre AportaInformacion/PreguntaFueraDeTema y SolicitudAgresiva
(ver PENDIENTE-AMPLIACION.md, seccion 6): con solo 6 frases no se podia saber si esa
confusion es un patron sistematico o ruido de esos dos ejemplos puntuales.

No es una compuerta humana (no entrena, no cambia el modelo): es lectura de un
artefacto que ya existe.

Uso:
    python evaluate.py
    python evaluate.py --onnx Runtime/Nlu/Models/intent-tone-classifier.onnx \
                        --tokenizer Runtime/Nlu/Models/tokenizer \
                        --val-jsonl Training/Nlu/data/val.jsonl
"""

from __future__ import annotations

import argparse
import json
from pathlib import Path

import numpy as np
import onnxruntime as ort
from sklearn.metrics import confusion_matrix
from transformers import AutoTokenizer

REPO_ROOT = Path(__file__).resolve().parents[2]

# Mismo orden que Runtime/Core/Enums.cs y train.py: el indice de cada salida ONNX
# coincide 1:1 con el valor del enum de C#.
INTENT_CLASSES = (
    "Desconocida", "SolicitudRespetuosa", "SolicitudAgresiva", "Empatia",
    "AportaInformacion", "PreguntaFueraDeTema", "Interrupcion",
)
TONE_CLASSES = ("Neutral", "Respetuoso", "Agresivo", "Empatico", "Ansioso")

# Pareja de clases senalada en PENDIENTE-AMPLIACION.md seccion 6: dos de las 6 frases
# fijas de train.py confundieron estas intenciones. Se resalta aparte en el resumen
# final, ademas de aparecer en la matriz completa.
WATCH_INTENT_PAIRS = (
    ("AportaInformacion", "SolicitudAgresiva"),
    ("PreguntaFueraDeTema", "SolicitudAgresiva"),
)


def read_jsonl(path: Path) -> list[dict]:
    if not path.is_file():
        raise SystemExit(f"[evaluate] ERROR: falta {path}; corre antes prepare_dataset.py")
    return [json.loads(line) for line in path.read_text(encoding="utf-8").splitlines() if line.strip()]


def run_inference(session: ort.InferenceSession, tok, texts: list[str], max_length: int,
                   batch_size: int = 32) -> tuple[np.ndarray, np.ndarray]:
    """Tokeniza y corre el grafo .onnx completo (encoder + cabezas) por lotes."""
    intent_preds, tone_preds = [], []
    for start in range(0, len(texts), batch_size):
        batch_texts = texts[start:start + batch_size]
        enc = tok(batch_texts, padding="max_length", truncation=True,
                  max_length=max_length, return_tensors="np")
        outputs = session.run(
            ["intent_probs", "tone_probs"],
            {
                "input_ids": enc["input_ids"].astype(np.int64),
                "attention_mask": enc["attention_mask"].astype(np.int64),
            },
        )
        intent_probs, tone_probs = outputs
        intent_preds.append(intent_probs.argmax(axis=-1))
        tone_preds.append(tone_probs.argmax(axis=-1))
    return np.concatenate(intent_preds), np.concatenate(tone_preds)


def print_confusion(name: str, y_true: np.ndarray, y_pred: np.ndarray, classes: tuple[str, ...]) -> None:
    cm = confusion_matrix(y_true, y_pred, labels=list(range(len(classes))))
    width = max(len(c) for c in classes) + 1
    header = " " * width + "".join(f"{c[:10]:>11}" for c in classes)
    print(f"\n=== Matriz de confusion - {name} (filas=real, columnas=predicho) ===")
    print(header)
    for i, cls in enumerate(classes):
        row = "".join(f"{cm[i, j]:>11}" for j in range(len(classes)))
        print(f"{cls:<{width}}{row}")
    return cm


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--onnx", type=Path,
                        default=REPO_ROOT / "Runtime" / "Nlu" / "Models" / "intent-tone-classifier.onnx")
    parser.add_argument("--tokenizer", type=Path,
                        default=REPO_ROOT / "Runtime" / "Nlu" / "Models" / "tokenizer")
    parser.add_argument("--val-jsonl", type=Path,
                        default=Path(__file__).resolve().parent / "data" / "val.jsonl")
    parser.add_argument("--max-length", type=int, default=64)
    args = parser.parse_args()

    if not args.onnx.is_file():
        raise SystemExit(f"[evaluate] ERROR: no existe {args.onnx}; corre antes train.py")

    rows = read_jsonl(args.val_jsonl)
    print(f"val: {len(rows)} entradas  onnx={args.onnx.name}")

    tok = AutoTokenizer.from_pretrained(str(args.tokenizer))
    session = ort.InferenceSession(str(args.onnx), providers=["CPUExecutionProvider"])

    intent_idx = {name: i for i, name in enumerate(INTENT_CLASSES)}
    tone_idx = {name: i for i, name in enumerate(TONE_CLASSES)}
    y_intent = np.array([intent_idx[r["intent"]] for r in rows])
    y_tone = np.array([tone_idx[r["tone"]] for r in rows])

    pred_intent, pred_tone = run_inference(session, tok, [r["text"] for r in rows], args.max_length)

    cm_intent = print_confusion("INTENT", y_intent, pred_intent, INTENT_CLASSES)
    print_confusion("TONE", y_tone, pred_tone, TONE_CLASSES)

    print("\n=== Confusion especifica bajo observacion (PENDIENTE-AMPLIACION.md, seccion 6) ===")
    intent_support = {i: int((y_intent == i).sum()) for i in range(len(INTENT_CLASSES))}
    for real_name, pred_name in WATCH_INTENT_PAIRS:
        i, j = intent_idx[real_name], intent_idx[pred_name]
        n = int(cm_intent[i, j])
        support = intent_support[i]
        pct = (100 * n / support) if support else 0.0
        print(f"  Real={real_name:<20} Predicho={pred_name:<18} "
              f"{n:4d} de {support:4d} ({pct:5.1f} %)")
    print("\nSi ese porcentaje es alto (comparado con el resto de la fila en la matriz de\n"
          "arriba), es un patron sistematico y no ruido de las 2 frases del chequeo de\n"
          "train.py; si es bajo, esas 2 frases eran casos limite puntuales.")


if __name__ == "__main__":
    main()
