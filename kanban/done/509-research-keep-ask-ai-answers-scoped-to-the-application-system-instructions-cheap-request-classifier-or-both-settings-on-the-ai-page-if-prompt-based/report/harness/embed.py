"""Local embedding-similarity classifier: no model call. In-process ONNX (fastembed) with
BAAI/bge-small-en-v1.5, the size a .NET OnnxRuntime port would ship. Score = the best cosine
between the ask and any catalog row (site map rows plus catalog tool descriptions).

usage: uv run --with fastembed python -I embed.py [prompts-module-attr]
"""

import json
import os
import sys
import time

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import fixtures as fx  # noqa: E402
from fastembed import TextEmbedding  # noqa: E402

HERE = os.path.dirname(os.path.abspath(__file__))
MODEL = "BAAI/bge-small-en-v1.5"
attr = sys.argv[1] if len(sys.argv) > 1 else "PROMPTS"
out = "clf-embed.json" if attr == "PROMPTS" else f"clf-embed-{attr.lower()}.json"

catalog = [line[2:] for line in fx.SITE_MAP.strip().splitlines()] + fx.CATALOG_ACTIONS

load_started = time.perf_counter()
model = TextEmbedding(MODEL)
cat_vecs = list(model.embed(catalog))
load_ms = (time.perf_counter() - load_started) * 1000


def norm(v):
    s = sum(x * x for x in v) ** 0.5
    return [x / s for x in v]


cat_vecs = [norm(v) for v in cat_vecs]
rows = []
for pid, expected, prompt in getattr(fx, attr):
    started = time.perf_counter()
    q = norm(next(iter(model.query_embed(prompt))))
    scores = [sum(a * b for a, b in zip(q, c)) for c in cat_vecs]
    best = max(range(len(scores)), key=scores.__getitem__)
    ms = (time.perf_counter() - started) * 1000
    rows.append({"id": pid, "expected": expected, "score": float(scores[best]), "nearest": catalog[best], "ms": ms})

os.makedirs(os.path.join(HERE, "..", "data"), exist_ok=True)
with open(os.path.join(HERE, "..", "data", out), "w") as f:
    json.dump({"variant": "clf-embed", "model": MODEL, "catalog_rows": len(catalog), "load_ms": load_ms,
               "measured": time.strftime("%Y-%m-%dT%H:%M:%SZ", time.gmtime()), "rows": rows}, f, indent=1)
for r in sorted(rows, key=lambda r: -r["score"]):
    print(f"{r['id']} {r['expected']:6} {r['score']:.3f} {r['ms']:5.1f}ms  {r['nearest'][:60]}")
print(f"load {load_ms:.0f}ms")
