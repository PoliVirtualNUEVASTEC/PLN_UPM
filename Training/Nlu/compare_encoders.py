"""Repite la tarea 1.6 (comparacion de encoders candidatos) sobre el corpus ampliado.

El 2026-09-14 se compararon 4 candidatos de encoder de punta a punta y se eligio
distilbert-base-multilingual-cased por mejor accuracy/F1 en Intent y Tone (ver
openspec/changes/archive/2026-09-09-m2-clasificador-bert-reducido/tasks.md, tarea
1.6). Esa comparacion se hizo sobre el corpus que existia entonces (1200 entradas,
100 por interseccion intent x escenario). El corpus ya crecio a 10000 entradas
(ver Data/Corpus/PENDIENTE-AMPLIACION.md) y no hay garantia de que el mismo ganador
se sostenga con 8 veces mas datos.

Este script entrena y evalua cada candidato (mismo split train/val, mismo seed,
mismos hiperparametros, para que la comparacion sea justa) y al final imprime una
tabla comparativa. NO exporta ningun .onnx: una vez elegido el ganador, se corre
`train.py --encoder <ganador>` para generar el artefacto final que reemplaza al
commiteado hoy en Runtime/Nlu/Models/.

COMPUERTA HUMANA: esto es la tarea 1.6, igual que train.py es 1.6/1.7 para un solo
encoder -- ningun agente lo corre de forma autonoma (ver Training/Nlu/README.md,
seccion "Compuertas humanas"). Lo ejecuta una persona con GPU o CPU y acceso a
internet para descargar los pesos de cada candidato (una vez por candidato).

Uso:
    python compare_encoders.py
    python compare_encoders.py --encoders sentence-transformers/paraphrase-multilingual-MiniLM-L12-v2 distilbert-base-multilingual-cased
    python compare_encoders.py --epochs 40 --device cuda

Con los 4 candidatos por defecto, en CPU, esto tarda varias veces mas que una sola
corrida de train.py (una descarga + un calculo de embeddings por candidato). Es
normal, no es un error.
"""

from __future__ import annotations

import argparse
import sys
from pathlib import Path

import torch
from sklearn.metrics import accuracy_score, f1_score
from transformers import AutoTokenizer

sys.path.insert(0, str(Path(__file__).resolve().parent))
from train import (  # noqa: E402 - import despues de ajustar sys.path, a proposito
    Classifier,
    Encoder,
    INTENT_CLASSES,
    TONE_CLASSES,
    embed_texts,
    labels_to_idx,
    read_split,
    report,
    set_seed,
    train_heads,
)

REPO_ROOT = Path(__file__).resolve().parents[2]

# Los mismos 4 candidatos que ya documentaba Training/Nlu/README.md como
# "razonables para espanol colombiano hablado", y los que se compararon en 2026-09-14.
DEFAULT_CANDIDATES = (
    "sentence-transformers/paraphrase-multilingual-MiniLM-L12-v2",
    "distilbert-base-multilingual-cased",
    "microsoft/Multilingual-MiniLM-L12-H384",
    "huawei-noah/TinyBERT_General_4L_312D",
)


def evaluate_candidate(name: str, train_rows: list[dict], val_rows: list[dict],
                        epochs: int, batch_size: int, lr: float, max_length: int,
                        seed: int, device: str) -> dict:
    print(f"\n{'=' * 70}\nCandidato: {name}\n{'=' * 70}")
    set_seed(seed)  # mismo seed para cada candidato: la unica variable es el encoder

    tok = AutoTokenizer.from_pretrained(name)
    encoder = Encoder(name).to(device)
    model = Classifier(encoder).to(device)

    print("calculando embeddings del encoder congelado...")
    emb_train = embed_texts(encoder, tok, [r["text"] for r in train_rows], max_length, device)
    emb_val = embed_texts(encoder, tok, [r["text"] for r in val_rows], max_length, device)

    yi_train = labels_to_idx(train_rows, "intent", INTENT_CLASSES)
    yt_train = labels_to_idx(train_rows, "tone", TONE_CLASSES)
    yi_val = labels_to_idx(val_rows, "intent", INTENT_CLASSES)
    yt_val = labels_to_idx(val_rows, "tone", TONE_CLASSES)

    print("entrenando las dos cabezas...")
    train_heads(model, emb_train, yi_train, yt_train, epochs, batch_size, lr, device)

    # Reporte completo por clase, igual que train.py, para poder mirar el detalle de
    # cada candidato si el resumen final no alcanza para decidir.
    report(model, emb_val, yi_val, yt_val, device)

    model.intent_head.eval()
    model.tone_head.eval()
    with torch.no_grad():
        emb_val_dev = emb_val.to(device)
        pi = model.intent_head(emb_val_dev).argmax(-1).cpu().numpy()
        pt = model.tone_head(emb_val_dev).argmax(-1).cpu().numpy()

    return {
        "encoder": name,
        "hidden_size": encoder.hidden_size,
        "n_params": sum(p.numel() for p in encoder.backbone.parameters()),
        "intent_acc": accuracy_score(yi_val.numpy(), pi),
        "intent_f1_macro": f1_score(yi_val.numpy(), pi, average="macro", zero_division=0),
        "tone_acc": accuracy_score(yt_val.numpy(), pt),
        "tone_f1_macro": f1_score(yt_val.numpy(), pt, average="macro", zero_division=0),
    }


def print_summary(results: list[dict]) -> None:
    print(f"\n\n{'=' * 90}\nRESUMEN COMPARATIVO (split de validacion)\n{'=' * 90}")
    header = (f"{'Encoder':<52}{'Params':>10}{'Int.acc':>9}{'Int.F1':>9}"
              f"{'Ton.acc':>9}{'Ton.F1':>9}")
    print(header)
    for r in results:
        short_name = r["encoder"].split("/")[-1][:50]
        print(f"{short_name:<52}{r['n_params'] / 1e6:>9.1f}M{r['intent_acc']:>9.3f}"
              f"{r['intent_f1_macro']:>9.3f}{r['tone_acc']:>9.3f}{r['tone_f1_macro']:>9.3f}")
    print("\nParams = tamano del encoder congelado (mas grande => mas lento/pesado on-device,")
    print("ver el limite de ~20-60M que ninguno de los 4 candidatos originales cumplia).")
    print("\nEsta tabla no elige un ganador automaticamente: la decision cruza accuracy con el")
    print("presupuesto de rendimiento en el dispositivo objetivo (Quest), y es del equipo, no")
    print("de este script. Una vez decidido, correr:")
    print("  python train.py --encoder <encoder-elegido>")
    print("para generar el .onnx final que reemplaza al commiteado hoy en Runtime/Nlu/Models/.")


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--encoders", nargs="+", default=list(DEFAULT_CANDIDATES),
                        help="ids de Hugging Face a comparar (default: los 4 candidatos del README)")
    parser.add_argument("--data-dir", type=Path, default=Path(__file__).resolve().parent / "data")
    parser.add_argument("--epochs", type=int, default=30)
    parser.add_argument("--batch-size", type=int, default=16)
    parser.add_argument("--lr", type=float, default=1e-3)
    parser.add_argument("--max-length", type=int, default=64)
    parser.add_argument("--seed", type=int, default=42)
    parser.add_argument("--device", default="auto", choices=["auto", "cpu", "cuda"])
    args = parser.parse_args()

    device = ("cuda" if torch.cuda.is_available() else "cpu") if args.device == "auto" else args.device
    print(f"device={device}  seed={args.seed}  epochs={args.epochs}  "
          f"candidatos={len(args.encoders)}")

    train_rows = read_split(args.data_dir / "train.jsonl")
    val_rows = read_split(args.data_dir / "val.jsonl")
    print(f"train={len(train_rows)}  val={len(val_rows)}")

    results = []
    for i, name in enumerate(args.encoders, start=1):
        print(f"\n[{i}/{len(args.encoders)}] descargando/cargando {name}...")
        results.append(evaluate_candidate(
            name, train_rows, val_rows, args.epochs, args.batch_size, args.lr,
            args.max_length, args.seed, device))

    print_summary(results)


if __name__ == "__main__":
    main()
