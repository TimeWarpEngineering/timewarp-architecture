# Review framework

## Budget (by-diff)

- Lines changed: 26
- Effort: 1
- TCB hits: none
- Roster axes: general
- Turn cap: 80 (--max-turns; cursor uncapped)

## Scope

**Date:** 2026-10-22
**Host task:** kanban/to-do/398-admin-managed-xai-api-key-write-only-encrypted-at-rest/
**Diff scope:** branch `task/398-admin-managed-xai-api-key-write-only-encrypted-at`, commit `ee675a318` vs its parent `85ce8a719` (the task commit; 46 files). The local `master` ref is stale, so `master...HEAD` also shows the task 402 and 403 merges. Those merges are out of scope.
**Plan / brief:** task.md `## Goal (implementer restatement)`, `## Checklist`, `## Results`
**Effort:** 3. Roster axis: general. The orchestrator ran one general pass, split into two read-only subagents: the server and storage half, and the SPA and tests half. The orchestrator re-verified each finding against the tree.
**Reviewer roster:** general
**Session IDs:** review oracle (claude-opus-5-5, ganda task work, headless), session id not exposed. Subagents adc95486e2571e8f2 (server) and a7d9ef2d146ecc994 (SPA and tests).

## Round 2 scope

**Diff scope:** the fix commit `df2f61ddf` vs `9df34f363` (the round-1 review commit). That is 42 files, and the host by-diff budget counted 1050 lines. Effort 3, roster general. The orchestrator did one pass inline, with no subagents. It re-verified M1–M12 and scanned the delta for new defects.
**Session IDs:** review oracle round 2 (claude-opus-5-5, ganda task work, headless), session id not exposed.

## Round 3 scope

**Diff scope:** the fix commit `b5fe0b2fd` vs `f17e1a577` (the round-2 review commit). That is 3 files, and the host by-diff budget counted 26 lines. Effort 1, roster general. The orchestrator did one pass inline, with no subagents. It re-verified M13 and scanned the delta for new defects.
**Session IDs:** review oracle round 3 (claude-opus-5-5, ganda task work, headless), session id not exposed.

## Ground rules

- Reviewers are read-only on product code and write only under `review/round-N/`.
- Severity is bug, suggestion, or nit. Status starts as open.
- Do not invent issues to fill space. Zero issues is a valid outcome.
- Address the diff and the surrounding call sites, and re-verify falsifiable claims against the repo.
- Prior rounds are immutable. New work goes in `round-(N+1)/`.
