# Review framework

## Budget (by-diff)

- Lines changed: 130
- Effort: 1
- TCB hits: none
- Roster axes: general
- Turn cap: 80 (--max-turns; cursor uncapped)

## Scope — task 486

**Date:** 2026-11-10
**Host task:** kanban/in-progress/486-split-timewarparchitectureanalyzers-move-generic-code-style-rules-to-timewarpsourcegenerators/
**Diff scope:** branch `task/486-split-timewarparchitectureanalyzers-move-generic-c` vs merge-base `7624ca457` with master (head `e345d6ffc`). 64 files. About 1,200 of the lines are the audit-seeded `.timewarp/suppression-baseline.json`.
**Plan / brief:** `## Goal (implementer restatement)` in `task.md`. Pin TimeWarp.SourceGenerators 1.0.0-beta.13, delete TWA0004, TWA0023, and TWA0038 (sources and tests) from TimeWarp.Architecture.Analyzers, and update docs. Riding along: audit boy-scout fixes (baseline, githooks, pwsh workflow, Nuru 4.0.0-beta.12 / Amuru 3 / Terminal 2, Mediator 15 handler shape in tools/).
**Effort:** 3 (by-diff budget), roster axis general
**Reviewer roster:** general (review oracle, claude-opus-5-5)
**Session IDs:** review oracle under `ganda task work` (headless print mode; the harness exposes no session id)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`

## Round 2 scope

- Delta `e345d6ffc..b4001294e` (round-1 base to fix head): `AGENTS.md`, `documentation/developer/guides/analyzer-package-split.md`, `source/analyzers/timewarp-architecture-attributes/type-stem-identifier-attribute.cs`, plus kanban kitchen edits.
- Effort 1, roster general (review oracle, claude-opus-5-5). Re-verify M1 and M2; scan the fix delta for new defects.
