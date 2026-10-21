# Review framework

## Budget (by-diff)

- Lines changed: 3202
- Effort: 3
- TCB hits: none
- Roster axes: general
- Turn cap: 200 (--max-turns; cursor uncapped)

## Scope

**Date:** 2026-10-22
**Host task:** kanban/to-do/398-admin-managed-xai-api-key-write-only-encrypted-at-rest/
**Diff scope:** branch `task/398-admin-managed-xai-api-key-write-only-encrypted-at`, commit `ee675a318` vs its parent `85ce8a719` (the task commit; 46 files). The local `master` ref is stale, so `master...HEAD` also shows the task 402 and 403 merges. Those merges are out of scope.
**Plan / brief:** task.md `## Goal (implementer restatement)`, `## Checklist`, `## Results`
**Effort:** 3. Roster axis: general. The orchestrator ran one general pass, split into two read-only subagents: the server and storage half, and the SPA and tests half. The orchestrator re-verified each finding against the tree.
**Reviewer roster:** general
**Session IDs:** review oracle (claude-opus-5-5, ganda task work, headless), session id not exposed. Subagents adc95486e2571e8f2 (server) and a7d9ef2d146ecc994 (SPA and tests).

## Ground rules

- Reviewers are read-only on product code and write only under `review/round-N/`.
- Severity is bug, suggestion, or nit. Status starts as open.
- Do not invent issues to fill space. Zero issues is a valid outcome.
- Address the diff and the surrounding call sites, and re-verify falsifiable claims against the repo.
- Prior rounds are immutable. New work goes in `round-(N+1)/`.
