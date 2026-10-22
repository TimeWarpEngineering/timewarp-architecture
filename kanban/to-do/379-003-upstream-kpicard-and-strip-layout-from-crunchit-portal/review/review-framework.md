# Review framework

## Budget (by-diff)

- Lines changed: 622
- Effort: 2
- TCB hits: none
- Roster axes: general
- Turn cap: 120 (--max-turns; cursor uncapped)

## Round 1

**Date:** 2026-10-22
**Host task:** kanban/to-do/379-003-upstream-kpicard-and-strip-layout-from-crunchit-portal/
**Diff scope:** branch `task/379-003-upstream-kpicard-and-strip-layout-from-crunchit-po` vs `master` (commit bb69ad159), product paths `source/` and `tests/`
**Plan / brief:** task.md Checklist and `## Goal (implementer restatement)` — KpiCard element, KpiTone enum, KpiStrip composite, two tokens, style guide card, render tests
**Effort:** 2 (budget by diff); roster axes: general only
**Reviewer roster:** general (review oracle, claude-opus-5-5, inline)
**Session IDs:** review oracle session not reported by host

### Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`

### Gates re-run by the reviewer

- `dotnet test -c Debug -- --filter-class KpiCard_Should_` — 7/7 passed
- `dotnet test -c Debug --no-build -- --filter-class KpiStrip_Should_` — 2/2 passed
- `git diff master...HEAD -- source tests | grep -ic crunchit` — 0
- Not re-run: `./bin/dev build` (Release), `./bin/dev template-smoke`, `ganda repo audit` (implementer-reported green)
