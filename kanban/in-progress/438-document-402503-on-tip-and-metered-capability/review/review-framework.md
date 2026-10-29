# Review framework

## Budget (by-diff)

- Lines changed: 679
- Effort: 2
- TCB hits: none
- Roster axes: general
- Turn cap: 120 (--max-turns; cursor uncapped)

## Scope — task 438

**Date:** 2026-10-29
**Host task:** kanban/in-progress/438-document-402503-on-tip-and-metered-capability/
**Diff scope:** branch `task/438-document-402503-on-tip-and-metered-capability`, commits after
`ce32e80af` (move to in-progress): `3b31d56f3` (fix) and `1e59e8258` (kanban land). The by-diff
count above is against the master merge-base, which also carries tasks 435 and 436. Those
commits are out of scope.
**Plan / brief:** `task.md` Description and `## Goal (implementer restatement)`: OpenAPI lists
402 and 503 on `GET`/`POST /api/tip` and `GET /api/demo/metered-capability`, the metered
operation names scope `demo:invoke`, free routes stay without 402.
**Effort:** 2
**Reviewer roster:** general
**Session IDs:** review oracle, claude-opus-5-5 (headless print mode; no session id exposed)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
