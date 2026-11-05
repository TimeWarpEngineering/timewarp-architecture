# Review framework

## Budget (by-diff)

- Lines changed: 894
- Effort: 3
- TCB hits: none
- Roster axes: general
- Turn cap: 200 (--max-turns; cursor uncapped)

## Framework — task 475

**Date:** 2026-11-05
**Host task:** kanban/in-progress/475-bring-feedbackitem-contracts-under-the-contract-agreement-analyzer/
**Diff scope:** branch `task/475-bring-feedbackitem-contracts-under-the-contract-ag`, commit 882428205 vs 2da1279fb (the implement commit; 25 files, +765/−64)
**Plan / brief:** task.md `## Goal (implementer restatement)`: `[Operation]` on all 13 feedback requests, `IFeedbackItemDetails` on Submit, and analyzer extensions (mirrored enums, child projection, wrapped projection) so `FeedbackItem` passes TWA0039–TWA0043 with justified opt-outs only
**Effort:** 3 (by-diff budget), roster axis general
**Reviewer roster:** general (review oracle, Claude Opus 5.5)
**Session IDs:** review oracle under ganda task work, 2026-11-05

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
