# Review framework

## Budget (by-diff)

- Lines changed: 634
- Effort: 2
- TCB hits: none
- Roster axes: general
- Turn cap: 120 (--max-turns; cursor uncapped)

# Review framework — task 070-008

**Date:** 2026-10-08
**Host task:** kanban/to-do/070-008-yarp-forwards-the-public-host-as-x-forwarded-host-on-every-target-instead-of-preserving-the-original-host/
**Diff scope:** branch `task/070-008-…` vs `origin/master` (commit 8a4499690)
**Plan / brief:** task.md Decision + Requirements (option 2: YARP sets X-Forwarded-Host on web routes; HttpRequestHostAccessor reads it, selection-only, fail-closed)
**Effort:** 2 (by-diff budget) — roster axes: general
**Reviewer roster:** general (review oracle, Claude Opus 5.5, headless ganda task work)
**Session IDs:** headless review oracle session (ganda task work)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
