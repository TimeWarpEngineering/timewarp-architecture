# Round 1 — merged findings
**Date:** 2026-10-14
**Sources:** general (+ orchestrator verification)

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 1 | 0 |
| suggestion | 0 | 0 | 1 |
| nit | 0 | 1 | 1 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: source/container-apps/web/projects/web-spa/features/application/modals/agent-ask/AgentAsk.razor:396
- Description: `EditModeFocusPending` was set on every choice but only cleared by a freshly mounted pill. Verified: `EnsureAgentAsync` defers the rebuild while a turn is in progress (`TurnInProgress && BuiltGeneration == generation`), so the pill was not remounted and the stale flag later pulled focus to the new pill (after the turn ended, or after New conversation).
- Suggestion: make the handoff self-limiting.
- Source: general
- Disposition notes: `EditModeChose` now sets the flag only when no turn defers the rebuild (`!TurnInProgress`); in the deferred case the menu already returned focus to the same pill. `NewConversationAsync` clears the flag. Residual: a build that throws leaves the flag until the next mount — the panel shows an error state then and this is accepted. Design regions updated.

### M2 — Severity: nit — Status: fixed
- File: source/container-apps/web/projects/web-spa/features/application/modals/agent-ask/AskEditModePill.razor:5
- Description: Design region referenced another product's pill and had a ragged wrap. (The `isChecked != true || EditMode == mode` guard was verified correct; no change.)
- Source: general
- Disposition notes: reworded to present-tense rationale ("A bare 'Ask' would read as the panel's name") and re-wrapped.

### M3 — Severity: suggestion — Status: wontfix
- File: tests/container-apps/web/web-spa-integration-tests/features/application/ask-edit-mode-pill-tests.cs:51
- Description: Component test dispatches SetEditMode via the store rather than the pill's choose path; no deferred-rebuild test.
- Source: general
- Disposition notes: HtmlRenderer cannot raise the web component's CheckedChanged; the choose path is covered end-to-end by `AskSurface_Given_Wasm` (mouse + keyboard). A deferred-rebuild Playwright case needs a turn held in Streaming/AwaitingInput, which the WASM lane has no controllable chat-client stub for; out of proportion for this UI swap. Decided by: review oracle.

### M4 — Severity: nit — Status: wontfix
- File: tests/container-apps/web/web-spa-playwright-tests/ask-surface-playwright-tests.cs:490
- Description: Risk that `aria-expanded` is absent (not "false") on a remounted pill.
- Source: general
- Disposition notes: falsified empirically — `AskSurface_Given_Wasm` re-run by the review oracle after the M1 fix passed 1/1, so fluent-menu sets `aria-expanded="false"` on connect. Asserting the exact value is the stronger ARIA check. Decided by: review oracle.

## Duplicates / conflicts

- None.
