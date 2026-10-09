# Round 2 — merged findings
**Date:** 2026-10-10
**Sources:** general (re-review of fix commit 2655f37e0)

## Counts (final, M1–M16 carried + N1–N3)

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 5 | 0 |
| suggestion | 0 | 8 | 0 |
| nit | 0 | 6 | 0 |

## Resolved prior

M1–M16: re-verified as fixed against commit 2655f37e0 (see `general.md` prior-findings table). Status fixed.

## Issues

### N1 — Severity: bug — Status: fixed
- File: source/container-apps/web/projects/web-spa/features/application/modals/agent-ask/AgentAsk.razor (EnsureAgentAsync, Dispose)
- Description: If the panel is disposed (navigation) while EnsureAgentAsync awaits, the build later assigns an agent, context, functions and a status subscription to a disposed component, and nothing frees them.
- Suggestion: Set a Disposed flag in Dispose, check it after the awaits, free the locals, and guard OnStatusChanged.
- Source: general
- Disposition notes: Fixed. There is a `Disposed` flag. The build returns after SelectAsync when the panel is disposed. After RestoreAsync it disposes the local conversation and agent and releases the fields. OnStatusChanged ignores callbacks once the panel is disposed. The Design region is updated.

### N2 — Severity: suggestion — Status: fixed
- File: AgentAsk.razor (TurnInProgress guard)
- Description: With an approval card pending, the guard that defers the rebuild also blocked New conversation.
- Suggestion: Let a generation change bypass the guard.
- Source: general
- Disposition notes: Fixed. The guard is now `TurnInProgress && BuiltGeneration == generation`, so New conversation rebuilds at once and abandons the turn. The Design region says so.

### N3 — Severity: nit — Status: fixed
- File: tests/container-apps/web/web-spa-playwright-tests/ask-surface-playwright-tests.cs:183-192
- Description: The edit-mode rebuild check could pass before the rebuild happened.
- Suggestion: Wait for a sentinel that proves the rebuild.
- Source: general
- Disposition notes: Fixed. The panel root carries `data-built-mode`, the mode of the agent actually built, and it is set only after RestoreAsync. Playwright waits for `[data-built-mode=AutomaticallyEdit]` and then asserts the answer is still shown. Playwright passed 3/3.

## Duplicates / conflicts

- None.
