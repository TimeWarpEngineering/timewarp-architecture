# Evidence pass for agent anti-slop TWA rules

## Description

Research only. Mine recent agent PRs and Learnings for C# patterns agents tend to produce that appear ≥2 times across TimeWarp repos. Deliver a ranked list of candidate patterns with evidence (repo, PR/task, count). No analyzer code in this task.

After the evidence pass, each pattern that still stands can become its own TWA analyzer implement task, one rule at a time in one repo. Do not ship a speculative pack.

## Requirements

- Search recent agent PRs and kanban `## Learnings` sections for repeated C# anti-patterns.
- Rank by frequency (≥2 occurrences required to stay on the list).
- Cite concrete evidence for each row (repo, PR or task id, short description).
- Output is a ranked list only — no Roslyn / TWA analyzer code, no package scaffold.

## Checklist

- [ ] Define the search corpus (which repos, which PR window, where Learnings live)
- [ ] Collect candidate patterns with ≥2 hits
- [ ] Rank and write up the evidence list in Results
- [ ] Propose follow-on implement tasks (one rule each) — do not file them until Steven confirms

## Session

- Created: 874084 (2026-11-05)

## Notes

- Source: X web-rewrite agents eval option A (REWORK). https://github.com/TimeWarpEngineering/timewarp-intel/blob/master/evaluations/2026-11-05-x-web-rewrite-agents.md (PR https://github.com/TimeWarpEngineering/timewarp-intel/pull/103). Busara: mine real agent PRs / Learnings first; ship one TWA rule at a time. Confirmed by Steven 2026-11-05.
- Analyzers live under `source/analyzers/` in this repo when implement tasks are filed later.
- Filed only; not launched. Do not start until Steven says so.
