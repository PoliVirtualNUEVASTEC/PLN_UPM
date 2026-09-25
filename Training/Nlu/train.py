"""Entrena las dos cabezas del clasificador de M2 sobre un encoder congelado y exporta a ONNX.

Arquitectura (design.md AD1/AD2): un encoder BERT reducido pre-entrenado, con todos
sus parametros congelados, produce un embedding por frase; sobre ese mismo embedding
compartido se entrenan dos cabezas pequenas (una capa densa + softmax cada una), una
para Intent y otra para Tone. El encoder no se re-entrena.

El encoder es un argumento de linea de comandos, no esta hardcodeado: asi se prueban
los 2-3 candidatos del spike de Fase 1 (MiniLM / DistilBERT multilingue / TinyBERT)
sin editar el script.

Uso:
    python train.py --encoder sentence-transformers/paraphrase-multilingual-MiniLM-L12-v2
    python train.py --encoder distilbert-base-multilingual-cased --epochs 40

Requiere haber corrido antes prepare_dataset.py (lee train.jsonl / val.jsonl).
Salida: Runtime/Nlu/Models/intent-tone-classifier.onnx (parametrizable con --onnx-out;
lo consume y commitea PR2 via Git LFS, PR1 solo lo genera).
"""

from __future__ import annotations

import argparse
import json
import os
import random
import sys
from collections import Counter
from pathlib import Path

import numpy as np
import torch
import torch.nn as nn
from sklearn.metrics import accuracy_score, classification_report
from transformers import AutoModel, AutoTokenizer

REPO_ROOT = Path(__file__).resolve().parents[2]

# Debajo de este numero de ejemplos en el split de validacion, la precision reportada
# para esa clase no es confiable (el aviso al final de report() se calcula con esto,
# no queda hardcodeado a una clase fija que se desactualiza cuando el corpus cambia).
LOW_SUPPORT_THRESHOLD = 5

# Espejo de Runtime/Core/Enums.cs, en el orden de declaracion. El indice de cada
# salida ONNX coincide 1:1 con el valor del enum de C# (indice 0 = Desconocida /
# Neutral), para que PR2 mapee argmax -> (Intent)i / (Tone)i sin tabla intermedia.
INTENT_CLASSES = (
    "Desconocida", "SolicitudRespetuosa", "SolicitudAgresiva", "Empatia",
    "AportaInformacion", "PreguntaFueraDeTema", "Interrupcion",
)
TONE_CLASSES = ("Neutral", "Respetuoso", "Agresivo", "Empatico", "Ansioso")

# Frases fijas del dominio para el chequeo de reproducibilidad (tarea 1.7): correr
# train.py dos veces y comparar que Intent/Tone salen identicos para cada una.
REPRO_PHRASES = (
    "por favor ayudeme con el paciente",
    "muevase ya que no tengo todo el dia",
    "entiendo que esto es dificil para usted, tomese su tiempo",
    "la presion esta en catorce sobre noventa",
    "y usted donde compro esa corbata tan fea",
    "no me interrumpa que estoy hablando",
)


def set_seed(seed: int) -> None:
    os.environ["PYTHONHASHSEED"] = str(seed)
    random.seed(seed)
    np.random.seed(seed)
    torch.manual_seed(seed)
    torch.cuda.manual_seed_all(seed)
    torch.backends.cudnn.deterministic = True
    torch.backends.cudnn.benchmark = False
    try:
        torch.use_deterministic_algorithms(True, warn_only=True)
    except Exception:  # noqa: BLE001 - best effort, algunas versiones no lo soportan
        pass


def read_split(path: Path) -> list[dict]:
    if not path.is_file():
        sys.exit(f"[train] ERROR: falta {path}; corre primero prepare_dataset.py")
    return [json.loads(line) for line in path.read_text(encoding="utf-8").splitlines() if line.strip()]


def labels_to_idx(rows: list[dict], field: str, classes: tuple[str, ...]) -> torch.Tensor:
    index = {name: i for i, name in enumerate(classes)}
    return torch.tensor([index[r[field]] for r in rows], dtype=torch.long)


def masked_mean(last_hidden: torch.Tensor, mask: torch.Tensor) -> torch.Tensor:
    """Mean pooling sobre los tokens reales (convencion de sentence-transformers)."""
    m = mask.unsqueeze(-1).type_as(last_hidden)
    return (last_hidden * m).sum(1) / m.sum(1).clamp(min=1e-9)


class Encoder(nn.Module):
    """Envuelve el encoder pre-entrenado congelado + mean pooling."""

    def __init__(self, name: str):
        super().__init__()
        self.backbone = AutoModel.from_pretrained(name, attn_implementation="eager")
        for p in self.backbone.parameters():
            p.requires_grad_(False)
        self.backbone.eval()
        self.hidden_size = int(self.backbone.config.hidden_size)

    def forward(self, input_ids: torch.Tensor, attention_mask: torch.Tensor) -> torch.Tensor:
        out = self.backbone(input_ids=input_ids, attention_mask=attention_mask)
        return masked_mean(out.last_hidden_state, attention_mask)


class Classifier(nn.Module):
    """Encoder congelado + dos cabezas densas con softmax (AD2). Es lo que se exporta a ONNX."""

    def __init__(self, encoder: Encoder):
        super().__init__()
        self.encoder = encoder
        self.intent_head = nn.Linear(encoder.hidden_size, len(INTENT_CLASSES))
        self.tone_head = nn.Linear(encoder.hidden_size, len(TONE_CLASSES))

    def forward(self, input_ids: torch.Tensor, attention_mask: torch.Tensor):
        emb = self.encoder(input_ids, attention_mask)
        intent_probs = torch.softmax(self.intent_head(emb), dim=-1)
        tone_probs = torch.softmax(self.tone_head(emb), dim=-1)
        return intent_probs, tone_probs


@torch.no_grad()
def embed_texts(encoder: Encoder, tok, texts: list[str], max_length: int, device: str) -> torch.Tensor:
    chunks = []
    for start in range(0, len(texts), 32):
        batch = tok(texts[start:start + 32], padding=True, truncation=True,
                    max_length=max_length, return_tensors="pt").to(device)
        chunks.append(encoder(batch["input_ids"], batch["attention_mask"]).cpu())
    return torch.cat(chunks, dim=0)


def train_heads(model: Classifier, emb: torch.Tensor, y_intent: torch.Tensor, y_tone: torch.Tensor,
                epochs: int, batch_size: int, lr: float, device: str) -> None:
    params = list(model.intent_head.parameters()) + list(model.tone_head.parameters())
    opt = torch.optim.Adam(params, lr=lr)
    loss_fn = nn.CrossEntropyLoss()
    emb, y_intent, y_tone = emb.to(device), y_intent.to(device), y_tone.to(device)
    n = emb.shape[0]
    model.intent_head.train()
    model.tone_head.train()
    for epoch in range(1, epochs + 1):
        perm = torch.randperm(n)
        total = 0.0
        for start in range(0, n, batch_size):
            idx = perm[start:start + batch_size]
            opt.zero_grad()
            li = loss_fn(model.intent_head(emb[idx]), y_intent[idx])
            lt = loss_fn(model.tone_head(emb[idx]), y_tone[idx])
            loss = li + lt
            loss.backward()
            opt.step()
            total += float(loss) * len(idx)
        if epoch == 1 or epoch % 5 == 0 or epoch == epochs:
            print(f"  epoch {epoch:3d}/{epochs}  loss={total / n:.4f}")


@torch.no_grad()
def report(model: Classifier, emb: torch.Tensor, y_intent: torch.Tensor, y_tone: torch.Tensor,
           device: str) -> None:
    model.intent_head.eval()
    model.tone_head.eval()
    emb = emb.to(device)
    pi = model.intent_head(emb).argmax(-1).cpu().numpy()
    pt = model.tone_head(emb).argmax(-1).cpu().numpy()
    low_support: list[str] = []
    for name, y_true, y_pred, classes in (
        ("INTENT", y_intent.numpy(), pi, INTENT_CLASSES),
        ("TONE", y_tone.numpy(), pt, TONE_CLASSES),
    ):
        print(f"\n=== Precision por clase - {name} (split de validacion) ===")
        print(f"accuracy global: {accuracy_score(y_true, y_pred):.3f}")
        print(classification_report(
            y_true, y_pred, labels=list(range(len(classes))), target_names=list(classes),
            zero_division=0, digits=3))
        support = Counter(y_true.tolist())
        for i, cls in enumerate(classes):
            n = support.get(i, 0)
            if n < LOW_SUPPORT_THRESHOLD:
                low_support.append(f"{name.capitalize()}.{cls} ({n} ejemplo(s) en val)")
    if low_support:
        print("\nNOTA: estas clases tienen pocos ejemplos en el split de validacion (< "
              f"{LOW_SUPPORT_THRESHOLD}) y su precision no es fiable todavia: "
              + ", ".join(low_support) + " (ver Data/Corpus/PENDIENTE-AMPLIACION.md).")


def export_onnx(model: Classifier, tok, out_path: Path, opset: int, max_length: int) -> None:
    out_path.parent.mkdir(parents=True, exist_ok=True)
    model.eval()
    sample = tok(["frase de ejemplo para trazar el grafo"], padding="max_length",
                 truncation=True, max_length=max_length, return_tensors="pt")
    torch.onnx.export(
        model, (sample["input_ids"], sample["attention_mask"]), str(out_path),
        input_names=["input_ids", "attention_mask"],
        output_names=["intent_probs", "tone_probs"],
        dynamic_axes={"input_ids": {0: "batch", 1: "seq"},
                      "attention_mask": {0: "batch", 1: "seq"},
                      "intent_probs": {0: "batch"}, "tone_probs": {0: "batch"}},
        opset_version=opset, do_constant_folding=True,
    )
    _tag_labels(out_path)
    tok.save_pretrained(out_path.parent / "tokenizer")
    print(f"\nONNX exportado: {out_path}")
    print(f"tokenizador (lo necesita PR2 para Sentis): {out_path.parent / 'tokenizer'}")


def _tag_labels(out_path: Path) -> None:
    """Graba el orden de clases como metadata del ONNX para que PR2 no dependa del script."""
    import onnx  # import local: solo se necesita al exportar

    graph = onnx.load(str(out_path))
    for key, value in (("intent_classes", ",".join(INTENT_CLASSES)),
                       ("tone_classes", ",".join(TONE_CLASSES))):
        entry = graph.metadata_props.add()
        entry.key, entry.value = key, value
    onnx.save(graph, str(out_path))


@torch.no_grad()
def repro_check(model: Classifier, tok, max_length: int, device: str) -> None:
    print("\n=== Chequeo de reproducibilidad (tarea 1.7): diff este bloque entre dos corridas ===")
    batch = tok(list(REPRO_PHRASES), padding=True, truncation=True,
                max_length=max_length, return_tensors="pt").to(device)
    intent_probs, tone_probs = model(batch["input_ids"], batch["attention_mask"])
    for phrase, ip, tp in zip(REPRO_PHRASES, intent_probs.cpu(), tone_probs.cpu()):
        i, t = int(ip.argmax()), int(tp.argmax())
        print(f"  [{INTENT_CLASSES[i]:<20} p={float(ip[i]):.3f}] "
              f"[{TONE_CLASSES[t]:<12} p={float(tp[t]):.3f}]  {phrase}")


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--encoder", required=True,
                        help="id del encoder pre-entrenado (Hugging Face); NO tiene default a proposito")
    parser.add_argument("--data-dir", type=Path, default=Path(__file__).resolve().parent / "data")
    parser.add_argument("--onnx-out", type=Path,
                        default=REPO_ROOT / "Runtime" / "Nlu" / "Models" / "intent-tone-classifier.onnx")
    parser.add_argument("--epochs", type=int, default=30)
    parser.add_argument("--batch-size", type=int, default=16)
    parser.add_argument("--lr", type=float, default=1e-3)
    parser.add_argument("--max-length", type=int, default=64)
    parser.add_argument("--opset", type=int, default=14)
    parser.add_argument("--seed", type=int, default=42)
    parser.add_argument("--device", default="auto", choices=["auto", "cpu", "cuda"])
    args = parser.parse_args()

    device = ("cuda" if torch.cuda.is_available() else "cpu") if args.device == "auto" else args.device
    set_seed(args.seed)
    print(f"encoder={args.encoder}  device={device}  seed={args.seed}  epochs={args.epochs}")

    train_rows = read_split(args.data_dir / "train.jsonl")
    val_rows = read_split(args.data_dir / "val.jsonl")
    print(f"train={len(train_rows)}  val={len(val_rows)}")

    tok = AutoTokenizer.from_pretrained(args.encoder)
    encoder = Encoder(args.encoder).to(device)
    model = Classifier(encoder).to(device)

    print("calculando embeddings del encoder congelado...")
    emb_train = embed_texts(encoder, tok, [r["text"] for r in train_rows], args.max_length, device)
    emb_val = embed_texts(encoder, tok, [r["text"] for r in val_rows], args.max_length, device)

    yi_train = labels_to_idx(train_rows, "intent", INTENT_CLASSES)
    yt_train = labels_to_idx(train_rows, "tone", TONE_CLASSES)
    yi_val = labels_to_idx(val_rows, "intent", INTENT_CLASSES)
    yt_val = labels_to_idx(val_rows, "tone", TONE_CLASSES)

    print("entrenando las dos cabezas...")
    train_heads(model, emb_train, yi_train, yt_train,
                args.epochs, args.batch_size, args.lr, device)

    report(model, emb_val, yi_val, yt_val, device)
    export_onnx(model, tok, args.onnx_out, args.opset, args.max_length)
    repro_check(model, tok, args.max_length, device)
    print("\nlisto. Recorda: este entrenamiento es prueba de humo del pipeline, no un modelo "
          "de produccion, hasta que el corpus se amplie (PENDIENTE-AMPLIACION.md).")


if __name__ == "__main__":
    main()
