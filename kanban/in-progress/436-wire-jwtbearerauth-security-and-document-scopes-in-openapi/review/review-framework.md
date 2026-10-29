# Review framework

## Budget (by-diff)

- Lines changed: 773
- Effort: 2
- TCB hits: none
- Roster axes: general
- Turn cap: 120 (--max-turns; cursor uncapped)

## Scope

**Date:** 2026-10-29
**Host task:** kanban/in-progress/436-wire-jwtbearerauth-security-and-document-scopes-in-openapi/
**Diff scope:** implementation commit `0f66e2636` on `task/436-wire-jwtbearerauth-security-and-document-scopes-in`
(the other commits ahead of the merge base are task 435 and kanban landings). Product files:
`source/foundation/foundation-server/common-server-module.cs`,
`source/foundation/foundation-server/open-api-bearer-security-server.cs`,
`tests/container-apps/web/web-server-integration-tests/open-api-bearer-security-tests.cs`.
**Plan / brief:** `## Goal (implementer restatement)` in task.md, checked against the original Description and Checklist.
**Effort:** 2. **Reviewer roster:** general (the review oracle did the review itself; no subagents were spawned).
**Session IDs:** review oracle claude-opus-5-5 (ganda task work, headless).

## Ground rules

- Reviewers are read-only on product code. They write only under `review/round-N/`.
- Severity is bug, suggestion, or nit. Status starts as open.
- Do not invent issues. Zero issues is a valid outcome.
- Review the diff and the call sites around it. Re-check any claim that can be tested against the repo.
- Prior rounds are immutable. New work goes in `round-(N+1)/`.

## Round 2

**Diff scope:** fix delta `10bd0ffa8..da2cb14ac` (round-1 `base.sha` → fix commit).
**Reviewer roster:** general (review oracle claude-opus-5-5, no subagents). Re-verifies M1 and M2 and scans the delta for new defects.
