# Round 1 — merged findings
**Date:** 2026-07-31
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 4 | 0 | 0 |
| nit | 2 | 0 | 0 |

## Issues

### M1 — Severity: suggestion — Status: open
- File: `findings.md` §3, §5
- Description: C underspecified — **C-create** (per-class graph, simple dispose, Fixie cost
  parity) vs **C-share** (process-static/refcount, fewer boots, hard dispose). “Idempotent
  factory” + “per-class boot parity” contradict. Softens “no structural blockers.”
- Suggestion: Default **C-create** day one; C-share/E only after measured aggregator cost.
- Source: general
- Disposition notes:

### M2 — Severity: suggestion — Status: open
- File: `findings.md` §6–§7
- Description: Missing migration topology choice: **α** suite-shaped port vs **β** co-locate +
  suite shrink vs hybrid.
- Suggestion: State hybrid as default recommendation (product co-locate; topology suites stay
  suite-shaped).
- Source: general
- Disposition notes:

### M3 — Severity: suggestion — Status: open
- File: `findings.md` §3 / inventory foundation Testcontainers Lazy
- Description: Process-static Lazy for postgres not folded into C rules.
- Suggestion: Document as migrates-under-C or explicit exception.
- Source: general
- Disposition notes:

### M4 — Severity: suggestion — Status: open
- File: §6 endorsement amendment
- Description: Draft north star endorsable with C-create amendment (see general Issue 6).
- Suggestion: Lock Jaribu-only + C+A C-create + two-lane Aspire + data-gated E.
- Source: general
- Disposition notes:

### M5 — Severity: nit — Status: open
- File: `findings.md` §1
- Description: Cite TimeWarp.Fixie 3.1.0 `TestExecution.Run` / decompile evidence for headline.
- Source: general
- Disposition notes:

### M6 — Severity: nit — Status: open
- File: `findings.md` §1 / §7
- Description: Keep ~14 boots / ~24 files approximate; point at inventory consumer table.
- Source: general
- Disposition notes:

## Duplicates / conflicts

- None. M4 is the decision-facing rollup of M1 + research endorsement.

## Verified non-issues

- Headline Fixie per-class SP: **CONFIRMED** via package decompile
- aspire-tests xUnit third framework: **CONFIRMED**
- MOCK_AUTHENTICATION compile-time: **CONFIRMED**
