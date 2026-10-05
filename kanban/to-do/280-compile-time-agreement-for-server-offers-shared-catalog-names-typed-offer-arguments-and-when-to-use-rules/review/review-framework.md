# Review framework

## Budget (by-diff)

- Lines changed: 1115
- Effort: 3
- TCB hits: none
- Roster axes: general
- Turn cap: 200 (--max-turns; cursor uncapped)

# Review framework — task 280

**Date:** 2026-10-05
**Host task:** kanban/to-do/280-compile-time-agreement-for-server-offers-shared-catalog-names-typed-offer-arguments-and-when-to-use-rules/
**Diff scope:** branch `task/280-…` vs `master` (commit 7215eadeb; 25 files, +1009/−106)
**Plan / brief:** task.md Requirements 1–5 — shared offer names, typed `[ActionOffer]` records, TWA0029/TWA0030 analyzer, test decision, tw-blazor when-to-use rules
**Effort:** 3
**Reviewer roster:** general, tests
**Session IDs:** headless ganda task work review oracle (claude-opus-5-5)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
