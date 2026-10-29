# Review framework

## Budget (by-diff)

- Lines changed: 21
- Effort: 1
- TCB hits: none
- Roster axes: general
- Turn cap: 80 (--max-turns; cursor uncapped)

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

## Round 3 — 2026-10-29

**Why:** the walk resumed after PR #600 `ci (integration)` failed twice (recorded on `task.md`,
commit `3ebc8b917`). The product tree is unchanged since `0264769f3`.
**Diff scope:** `ce32e80af..aa9c7b76e` outside `kanban/` (task-438 commits only), plus the CI failure.
**Effort:** 3 (host by-diff budget above). **Roster:** general.
**Session IDs:** review oracle, claude-fable-5-1 (headless print mode; no session id exposed).
**Outcome:** M1–M3 stay fixed; new M4 (bug, open) and M5 (nit, open). Fix pass requested; the
disposition is reopened until round 4.

## Round 4 — 2026-10-29

**Why:** re-review after the fix pass requested in round 3 (host round 2, fix pass 1 of 5).
**Diff scope:** `e9f8e11bc..866e57a34` outside `kanban/` (round-3 base to HEAD). The delta is empty: the
only commit is a `task.md` landing commit. The implement-fix-2 oracle returned Done in 26 ms with no
product change.
**Effort:** 1 (host by-diff budget above, 3 lines). **Roster:** general.
**Session IDs:** review-2 oracle, claude-fable-5-1 (headless print mode; no session id exposed).
**Outcome:** no new findings. M4 (bug) and M5 (nit) stay open because the tree is identical to round 3.
Fix pass requested again; the disposition stays reopened until round 5.

## Round 5 — 2026-10-29

**Why:** re-review after the fix pass requested in round 4 (host review-3, implement-fix-3).
**Diff scope:** `828e09324..b1a6cb150` outside `kanban/` (round-4 base to HEAD): one commit touching
`api-server/program.cs` (`DisableAutoDiscovery = true`) and `open-api-payment-responses-server.cs`
(`FreeRoutesNever402` built from the route constants); 5 insertions, 2 deletions.
**Effort:** 1 (host by-diff budget above, 21 lines). **Roster:** general.
**Session IDs:** review-3 oracle, claude-fable-5-1 (headless print mode; no session id exposed).
**Outcome:** M4 and M5 verified fixed; no new findings. `dev build` 0 warnings; `dev test --group
integration` 8 of 8 projects passed with no `Duplicate routes detected!`. Disposition: clean.
