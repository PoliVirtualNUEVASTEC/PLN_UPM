"""Carga, valida y divide el corpus de M3 para entrenar el clasificador de M2.

Carga Data/Corpus/emergencia.json y Data/Corpus/juntas.json (ver CORPUS_FILES), valida
el esquema de cada entrada y produce un split train/validacion estratificado por la
combinacion intent x tone en la medida en que el tamano de cada clase lo permita. Las
clases con un solo ejemplo (si las hubiera) no se pueden estratificar: caen enteras a
train y se avisa por stderr, sin abortar.

Antes de repartir, agrupa las frases casi identicas (similitud de tokens >= NEAR_DUP_
THRESHOLD) para que una frase y su variacion no queden separadas entre train y val: si
quedaran separadas, el modelo veria en entrenamiento algo casi igual a lo que se usa
para medirlo, e inflaria artificialmente la metrica de validacion (ver
corpus_fuentes_ejemplos_triaje.md, seccion 9, "Las frases muy parecidas o las
variaciones de una misma frase deben permanecer en la misma particion").

Uso:
    python prepare_dataset.py [--corpus-dir DIR] [--out-dir DIR]
                              [--val-fraction 0.2] [--seed 42]

Salida (en --out-dir, por defecto Training/Nlu/data):
    train.jsonl, val.jsonl   una entrada del corpus por linea
    split_summary.json       conteos por intent, por tone y por intent x tone
"""

from __future__ import annotations

import argparse
import json
import random
import re
import sys
import unicodedata
from collections import Counter
from pathlib import Path

NEAR_DUP_THRESHOLD = 0.72

REPO_ROOT = Path(__file__).resolve().parents[2]

# Espejo de Runtime/Core/Enums.cs. Si el contrato M0 cambia estos miembros, este
# pipeline queda desactualizado a proposito hasta que alguien lo alinee a mano.
INTENT_MEMBERS = (
    "Desconocida", "SolicitudRespetuosa", "SolicitudAgresiva", "Empatia",
    "AportaInformacion", "PreguntaFueraDeTema", "Interrupcion",
)
TONE_MEMBERS = ("Neutral", "Respetuoso", "Agresivo", "Empatico", "Ansioso")
REQUIRED_KEYS = ("text", "intent", "tone", "scenario", "labeler")
# emergencia.json (triaje, voz del enfermero) y juntas.json (levantamiento de
# requerimientos, voz del analista) ya estan reescritos con volumen y balance de tono
# resueltos (ver Data/Corpus/PENDIENTE-AMPLIACION.md).
CORPUS_FILES = ("emergencia.json", "juntas.json")


def _warn(msg: str) -> None:
    print(f"[prepare_dataset] AVISO: {msg}", file=sys.stderr)


def load_corpus(corpus_dir: Path) -> list[dict]:
    """Lee y concatena los archivos del corpus. Falla si falta alguno."""
    entries: list[dict] = []
    for name in CORPUS_FILES:
        path = corpus_dir / name
        if not path.is_file():
            sys.exit(f"[prepare_dataset] ERROR: no se encuentra {path}")
        data = json.loads(path.read_text(encoding="utf-8"))
        if not isinstance(data, list):
            sys.exit(f"[prepare_dataset] ERROR: {path} no es una lista JSON")
        for i, row in enumerate(data):
            if not isinstance(row, dict):
                sys.exit(f"[prepare_dataset] ERROR: {name}[{i}] no es un objeto JSON")
            entries.append({**row, "_src": f"{name}[{i}]"})
    return entries


def validate(entries: list[dict]) -> None:
    """Valida el esquema de cada entrada. Acumula todos los errores y aborta si hay alguno."""
    errors: list[str] = []
    for e in entries:
        src = e["_src"]
        for key in REQUIRED_KEYS:
            val = e.get(key)
            if not isinstance(val, str) or not val.strip():
                errors.append(f"{src}: campo '{key}' ausente o vacio")
        if isinstance(e.get("intent"), str) and e["intent"] not in INTENT_MEMBERS:
            errors.append(f"{src}: intent '{e['intent']}' no es miembro de NpcAi.Core.Intent")
        if isinstance(e.get("tone"), str) and e["tone"] not in TONE_MEMBERS:
            errors.append(f"{src}: tone '{e['tone']}' no es miembro de NpcAi.Core.Tone")
        extra = set(e) - set(REQUIRED_KEYS) - {"_src"}
        if extra:
            _warn(f"{src}: campos extra ignorados {sorted(extra)}")
    if errors:
        for msg in errors:
            print(f"[prepare_dataset] ERROR: {msg}", file=sys.stderr)
        sys.exit(f"[prepare_dataset] {len(errors)} entradas invalidas; corregir el corpus antes de entrenar")


def _token_set(text: str) -> frozenset[str]:
    normalized = unicodedata.normalize("NFD", text.lower())
    normalized = "".join(c for c in normalized if unicodedata.category(c) != "Mn")
    return frozenset(re.findall(r"[a-z0-9ñ]+", normalized))


def _cluster_near_duplicates(entries: list[dict], threshold: float) -> list[list[int]]:
    """Agrupa indices de `entries` con similitud de tokens (Jaccard) >= threshold.

    Bloquea por palabras de mas de 4 letras para no comparar cada par (O(n^2) sobre
    miles de entradas). Usa union-find para que la cercania sea transitiva.
    """
    token_sets = [_token_set(e["text"]) for e in entries]
    by_word: dict[str, list[int]] = {}
    for i, toks in enumerate(token_sets):
        for w in toks:
            if len(w) > 4:
                by_word.setdefault(w, []).append(i)

    parent = list(range(len(entries)))

    def find(x: int) -> int:
        while parent[x] != x:
            parent[x] = parent[parent[x]]
            x = parent[x]
        return x

    def union(a: int, b: int) -> None:
        ra, rb = find(a), find(b)
        if ra != rb:
            parent[ra] = rb

    for i, toks in enumerate(token_sets):
        seen: set[int] = set()
        for w in toks:
            if len(w) <= 4:
                continue
            for j in by_word[w]:
                if j <= i or j in seen:
                    continue
                seen.add(j)
                union_size = len(toks | token_sets[j])
                if union_size == 0:
                    continue
                if len(toks & token_sets[j]) / union_size >= threshold:
                    union(i, j)

    clusters: dict[int, list[int]] = {}
    for i in range(len(entries)):
        clusters.setdefault(find(i), []).append(i)
    return list(clusters.values())


def stratified_split(entries: list[dict], val_fraction: float, seed: int) -> tuple[list[dict], list[dict]]:
    """Split estratificado por la clave intent||tone, con degradacion para clases chicas,
    manteniendo juntas en la misma particion las frases casi identicas.

    - Las entradas se agrupan primero en clusters de casi-duplicados (ver
      _cluster_near_duplicates). Cada cluster se reparte como una unidad indivisible:
      nunca queda mitad en train y mitad en val.
    - Un cluster cuyas entradas no comparten la misma clase intent||tone no se puede
      asignar a una sola clase sin distorsionar la estratificacion: cae entero a train
      y se avisa por stderr (es un caso raro; ver el aviso para revisarlo a mano).
    - Dentro de cada clase, se reparten clusters (no entradas sueltas) hasta acercarse
      a round(n * val_fraction) entradas en val, acotado a [1, n-1] igual que antes.
    - Clase con menos de 2 clusters o menos de 2 entradas: no estratificable, cae
      entera a train y se avisa por stderr.
    """
    rng = random.Random(seed)
    clusters = _cluster_near_duplicates(entries, NEAR_DUP_THRESHOLD)
    multi = [c for c in clusters if len(c) > 1]
    if multi:
        _warn(f"{len(multi)} grupo(s) de frases casi identicas (similitud >= "
              f"{NEAR_DUP_THRESHOLD}) con {sum(len(c) for c in multi)} entradas en total; "
              "se mantienen juntas en la misma particion")

    by_class: dict[str, list[list[int]]] = {}
    train: list[dict] = []
    val: list[dict] = []
    for cluster in clusters:
        keys = {f"{entries[i]['intent']}||{entries[i]['tone']}" for i in cluster}
        if len(keys) == 1:
            by_class.setdefault(keys.pop(), []).append(cluster)
        else:
            _warn(f"grupo de {len(cluster)} frases casi identicas mezcla clases "
                  f"{sorted(keys)}: cae entero a train sin repartir")
            train.extend(entries[i] for i in cluster)

    for key in sorted(by_class):
        class_clusters = by_class[key]
        rng.shuffle(class_clusters)
        n = sum(len(c) for c in class_clusters)
        if n < 2 or len(class_clusters) < 2:
            for cluster in class_clusters:
                train.extend(entries[i] for i in cluster)
            _warn(f"clase '{key}' con {n} ejemplo(s) en {len(class_clusters)} grupo(s): "
                  "no estratificable, cae entera a train")
            continue
        n_val_target = min(max(round(n * val_fraction), 1), n - 1)
        n_val_so_far = 0
        for cluster in class_clusters:
            if n_val_so_far < n_val_target:
                val.extend(entries[i] for i in cluster)
                n_val_so_far += len(cluster)
            else:
                train.extend(entries[i] for i in cluster)

    rng.shuffle(train)
    rng.shuffle(val)
    return train, val


def _dist(entries: list[dict], field: str) -> dict[str, int]:
    return dict(sorted(Counter(e[field] for e in entries).items()))


def _cross_dist(entries: list[dict]) -> dict[str, int]:
    pairs = Counter(f"{e['intent']}||{e['tone']}" for e in entries)
    return dict(sorted(pairs.items()))


def _clean(entries: list[dict]) -> list[dict]:
    return [{k: e[k] for k in REQUIRED_KEYS} for e in entries]


def write_outputs(out_dir: Path, train: list[dict], val: list[dict]) -> None:
    out_dir.mkdir(parents=True, exist_ok=True)
    for name, rows in (("train.jsonl", train), ("val.jsonl", val)):
        with (out_dir / name).open("w", encoding="utf-8") as fh:
            for row in rows:
                fh.write(json.dumps(row, ensure_ascii=False) + "\n")
    summary = {
        "train_size": len(train),
        "val_size": len(val),
        "intent": {"train": _dist(train, "intent"), "val": _dist(val, "intent")},
        "tone": {"train": _dist(train, "tone"), "val": _dist(val, "tone")},
        "intent_x_tone_train": _cross_dist(train),
        "intent_x_tone_val": _cross_dist(val),
    }
    (out_dir / "split_summary.json").write_text(
        json.dumps(summary, ensure_ascii=False, indent=2), encoding="utf-8")


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--corpus-dir", type=Path, default=REPO_ROOT / "Data" / "Corpus")
    parser.add_argument("--out-dir", type=Path, default=Path(__file__).resolve().parent / "data")
    parser.add_argument("--val-fraction", type=float, default=0.2)
    parser.add_argument("--seed", type=int, default=42)
    args = parser.parse_args()

    if not 0.0 < args.val_fraction < 1.0:
        sys.exit("[prepare_dataset] ERROR: --val-fraction debe estar en (0, 1)")

    entries = load_corpus(args.corpus_dir)
    validate(entries)
    train, val = stratified_split(entries, args.val_fraction, args.seed)
    train, val = _clean(train), _clean(val)
    write_outputs(args.out_dir, train, val)

    print(f"corpus: {len(entries)} entradas ({', '.join(CORPUS_FILES)})")
    print(f"split: train={len(train)}  val={len(val)}  (val_fraction={args.val_fraction}, seed={args.seed})")
    print(f"intent train: {_dist(train, 'intent')}")
    print(f"intent val:   {_dist(val, 'intent')}")
    print(f"tone train:   {_dist(train, 'tone')}")
    print(f"tone val:     {_dist(val, 'tone')}")
    print(f"archivos escritos en: {args.out_dir}")
    if _dist(train, "tone").get("Empatico", 0) <= 1 or "Empatico" not in _dist(val, "tone"):
        _warn("Tone.Empatico casi no tiene ejemplos: su precision sera mala hasta que "
              "Data/Corpus/PENDIENTE-AMPLIACION.md se resuelva")


if __name__ == "__main__":
    main()
