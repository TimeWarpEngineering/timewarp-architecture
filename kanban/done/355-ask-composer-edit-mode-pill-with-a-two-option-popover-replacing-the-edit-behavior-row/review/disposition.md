# Disposition — task 355

**Date:** 2026-10-14
**Outcome:** accepted-exceptions
**Rounds:** 1
**Final open count:** 0

## Summary

One general reviewer (effort 2, by-diff budget) raised 4 findings. The one bug (stale focus-restore
flag when a mid-turn mode change defers the rebuild) was verified against `EnsureAgentAsync` and
fixed in AgentAsk.razor; the Design-region nit was fixed. Two were closed wontfix with rationale.
After fixes: `dev build` 0 warnings / 0 errors, `AskEditModePill_Should_` 2/2,
`AskSurface_Given_Wasm` 1/1 (re-run by the review oracle). The fix is small and confined to the
behavior M1 flagged, so no second round was opened.

## Exception log

| ID | Severity | Rationale | Decided by |
|----|----------|-----------|------------|
| M3 | suggestion | Choose path covered by Playwright; deferred-turn test needs a controllable streaming stub the WASM lane lacks | review oracle |
| M4 | nit | Falsified by green Playwright run after fix; exact "false" is the stronger check | review oracle |

## Escalations

- None.
