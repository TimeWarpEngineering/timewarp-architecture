"""Summarize ../data/*.json into the markdown tables used in analysis.md and prompt-set.md.

usage: python3 -I analyze.py
"""

import json
import os
import statistics as st
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import fixtures as fx  # noqa: E402

DATA = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "data")


def load(name):
    path = os.path.join(DATA, f"{name}.json")
    return json.load(open(path))["rows"] if os.path.exists(path) else []


def p90(xs):
    xs = sorted(xs)
    return xs[min(len(xs) - 1, int(round(0.9 * (len(xs) - 1))))]


def main_ok(r):
    return r["verdict"] == "helped" if r["expected"] == "in" else r["verdict"] == "declined"


def clf_ok(r):
    return (r["label"] == "in_scope") == (r["expected"] == "in")


EMBED_THRESHOLD = None


def embed_threshold():
    rows = load("clf-embed")
    # Oracle threshold: the lowest in-scope score on the main set, so every in-scope ask passes.
    return min(r["score"] for r in rows if r["expected"] == "in")


def embed_ok(r, t):
    return (r["score"] >= t) == (r["expected"] == "in")


def tally(rows, ok):
    ins = [r for r in rows if r["expected"] == "in"]
    outs = [r for r in rows if r["expected"] != "in"]
    return (sum(map(ok, rows)), len(rows), sum(map(ok, ins)), len(ins), sum(map(ok, outs)), len(outs))


def fmt_tally(t):
    return f"{t[0]}/{t[1]} | {t[2]}/{t[3]} | {t[4]}/{t[5]}"


def accuracy_table():
    t = embed_threshold()
    print("| Approach | Main set: all | in-scope helped | off-topic blocked | Held-out: all | in-scope | off-topic |")
    print("|---|---|---|---|---|---|---|")
    for label, name, ok in [
        ("Before (current instructions)", "baseline", main_ok),
        ("1. Instructions only, scope v1", "scoped-v1", main_ok),
        ("1. Instructions only, scope v2", "scoped", main_ok),
        ("2a. xAI grok-4.20 non-reasoning, JSON schema", "clf-xai", clf_ok),
        ("2b. Cloudflare Workers AI llama-3.1-8b-fast, JSON schema", "clf-cf-llama", clf_ok),
        ("2c. Cloudflare Llama Guard 3 (AI Gateway guardrail model)", "clf-cf-guard", clf_ok),
        ("3. Both: 2a gates, scope v2 answers", "combined", main_ok),
    ]:
        main = load(name)
        held = load(f"{name}-heldout")
        print(f"| {label} | {fmt_tally(tally(main, ok))} | {fmt_tally(tally(held, ok)) if held else 'not run | | '} |")
    e_ok = lambda r: embed_ok(r, t)  # noqa: E731
    print(f"| 2d. Local embedding, bge-small, threshold {t:.3f} | {fmt_tally(tally(load('clf-embed'), e_ok))} | "
          f"{fmt_tally(tally(load('clf-embed-heldout'), e_ok))} |")
    print()
    print("Main-model and LLM-classifier rows are two runs per prompt (60 rows main, 20 held-out) unless noted; "
          "baseline held-out is one run; Llama Guard held-out is one run; the embedding check is deterministic (one run).")


def cost_table():
    base = load("baseline")
    scoped = load("scoped")
    print("| Approach | Where it runs | Added latency, median | p90 | Added tokens per turn | Added cost per turn (USD) |")
    print("|---|---|---|---|---|---|")
    dp = st.median(s["turn"]["prompt_tokens"] for s in scoped) - st.median(b["turn"]["prompt_tokens"] for b in base)
    bm = [b["turn"]["ms"] for b in base]
    sm = [s["turn"]["ms"] for s in scoped]
    print(f"| Before: one grok-4.7 turn (reference, not added) | web-server relay | {st.median(bm):.0f} ms turn | "
          f"{p90(bm):.0f} ms | {st.median(b['turn']['prompt_tokens'] for b in base):.0f} prompt | "
          f"{st.mean(b['turn']['cost_usd'] for b in base):.5f} mean turn |")
    print(f"| 1. Instructions only (scope v2) | web-server relay (appends to Instructions) | "
          f"{st.median(sm) - st.median(bm):+.0f} ms (turn {st.median(sm):.0f}) | turn {p90(sm):.0f} ms | +{dp:.0f} prompt | "
          f"{dp * 0.50 / 1e6:.6f} cached / {dp * 2.00 / 1e6:.6f} uncached |")
    for label, name in [("2a. xAI grok-4.20 non-reasoning", "clf-xai"),
                        ("2b. Workers AI llama-3.1-8b-fast", "clf-cf-llama"),
                        ("2c. Workers AI Llama Guard 3", "clf-cf-guard")]:
        rows = load(name)
        ms = [r["ms"] for r in rows]
        toks = st.median(r["prompt_tokens"] + r["completion_tokens"] for r in rows)
        cost = st.mean(r["cost_usd"] for r in rows)
        where = "web-server relay, HTTPS to api.x.ai" if "xai" in name else "web-server relay, HTTPS to Workers AI"
        print(f"| {label} | {where} | +{st.median(ms):.0f} ms | +{p90(ms):.0f} ms | {toks:.0f} classifier | {cost:.6f} |")
    e = load("clf-embed")
    ems = [r["ms"] for r in e]
    print(f"| 2d. Local embedding (bge-small ONNX, in process) | web-server relay | +{st.median(ems):.0f} ms | "
          f"+{p90(ems):.0f} ms | 0 model tokens | 0 (CPU only) |")
    print()
    comb = load("combined")
    for kind, sel in [("in-scope", lambda r: r["expected"] == "in"), ("off-topic", lambda r: r["expected"] != "in")]:
        rows = [r for r in comb if sel(r)]
        ms = [r["ms_total"] for r in rows]
        cost = st.mean(r["gate"]["cost_usd"] + r["turn"].get("cost_usd", 0) for r in rows)
        print(f"- Combined, {kind} asks: end-to-end median {st.median(ms):.0f} ms, p90 {p90(ms):.0f} ms, "
              f"mean cost {cost:.5f} USD per ask ({len(rows)} rows).")
    for kind, sel in [("in-scope", lambda r: r["expected"] == "in"), ("off-topic", lambda r: r["expected"] != "in")]:
        for name in ("baseline", "scoped"):
            rows = [r for r in load(name) if sel(r)]
            print(f"- {name}, {kind} asks: grok-4.7 turn median {st.median(r['turn']['ms'] for r in rows):.0f} ms, "
                  f"mean cost {st.mean(r['turn']['cost_usd'] for r in rows):.5f} USD, "
                  f"median completion+reasoning tokens "
                  f"{st.median(r['turn']['completion_tokens'] + r['turn']['reasoning_tokens'] for r in rows):.0f}.")


def short(v):
    return {"helped": "helped", "declined": "declined", "complied_off_topic": "ANSWERED",
            "in_scope": "in", "out_of_scope": "out", "unsafe": "unsafe"}[v]


def per_prompt(set_name, prompts, suffix=""):
    t = embed_threshold()
    names = ["baseline", "scoped-v1", "scoped", "clf-xai", "clf-cf-llama", "clf-cf-guard", "combined"]
    data = {n: load(n + suffix) for n in names}
    emb = {r["id"]: r for r in load("clf-embed" + suffix)}
    print(f"| Id | Expect | Prompt | Before | Instr v1 | Instr v2 | xAI clf | CF llama | CF guard | Embed (score) | Both |")
    print("|---|---|---|---|---|---|---|---|---|---|---|")
    for pid, expected, prompt in prompts:
        cells = []
        for n in names:
            rows = [r for r in data[n] if r["id"] == pid]
            if not rows:
                cells.append("not run")
                continue
            key = "label" if n.startswith("clf") else "verdict"
            ok = clf_ok if n.startswith("clf") else main_ok
            vals = [short(r[key]) + ("" if ok(r) else " ✗") for r in rows]
            cells.append(" / ".join(vals))
        e = emb[pid]
        epass = "in" if e["score"] >= t else "out"
        emark = "" if embed_ok(e, t) else " ✗"
        cells.insert(6, f"{epass}{emark} ({e['score']:.2f})")
        p = prompt.replace("|", "\\|")
        print(f"| {pid} | {expected} | {p} | " + " | ".join(cells) + " |")


if __name__ == "__main__":
    what = sys.argv[1] if len(sys.argv) > 1 else "all"
    if what in ("all", "accuracy"):
        accuracy_table()
        print()
    if what in ("all", "cost"):
        cost_table()
        print()
    if what in ("all", "prompts"):
        per_prompt("main", fx.PROMPTS)
        print()
        per_prompt("held-out", fx.HELDOUT, "-heldout")
