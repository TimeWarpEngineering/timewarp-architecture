# Disposition — task 271

**Date:** 2026-10-09
**Outcome:** accepted-exceptions
**Rounds:** 3
**Final open count:** 0

## Summary

Effort 3: roster general, security, tests in round 1, then re-review rounds 2 and 3. Round 1 raised 17 merged findings: 4 bug, 9 suggestion, 4 nit. 16 were fixed in 3330690db. The fixes cover:
- per-call approval correlation and a busy refusal for a second call;
- navigation cancelling a pending call, and a re-check after approval;
- a re-check when the chat driver invokes a tool;
- binding with the contract seam serializer options, plus enum schema;
- the current record in page_context for /Profile and /Admin/Authentication;
- binding before approval, with the bound values shown and executed;
- an explicit read-only allow-list;
- the negative-path, concurrency and timeout tests.

Round 2 verified all of those and raised 3 new findings (1 suggestion, 2 nit). All three were fixed in aa75fadff and verified in round 3.

Final totals across rounds: bug 4 fixed; suggestion 10 fixed; nit 5 fixed, 1 wontfix.

## Exception log

| ID | Severity | Rationale | Decided by |
|----|----------|-----------|------------|
| M13 | nit | WebMCP tools are published twice on page-to-page navigation. Both triggers are needed: the first render covers page-to-page moves, and LocationChanged covers same-page route changes and focused pages. The replace is idempotent, and the reason is recorded in the WebMcpAgentSurface Design comment. | orchestrator |

## Escalations

- None. M8 (an in-browser agent that can act on the DOM can click Approve) is documented as a known limit and a follow-up in design.md: a trusted gesture or WebMCP `requestUserInteraction`.
