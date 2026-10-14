# Round 1 — general
**Date:** 2026-10-14
**Scope reviewed:** commit 5db62ea8b vs 5db62ea8b~1

## Summary
The pill, its Purpose/Design region, CSS isolation, one-@code layout and the removal of the old row are clean, and no source, test or skill still references `twe-agent-ask__edit` or the "Edit behavior" row (only kanban history). The one real weakness is the `EditModeFocusPending` handoff: it assumes a mode change always remounts the pill promptly, but the rebuild can be deferred or fail, leaving the flag set so a later mount steals focus. The `isChecked != true || EditMode == mode` guard looks right. Tests cover the spec well; two small gaps noted.

## Issues

### Issue 1 — Severity: bug
- File: source/container-apps/web/projects/web-spa/features/application/modals/agent-ask/AgentAsk.razor:396
- Description: `EditModeFocusPending` is set when the pill raises `Chose` and cleared only when a freshly mounted pill consumes it on first render. SetEditMode only changes state; the pill is remounted only if `EnsureAgentAsync` actually rebuilds (new `Conversation`, `@key="Conversation"`). `EnsureAgentAsync` returns without rebuilding (and without remounting) when (a) a turn is in progress (`TurnInProgress && BuiltGeneration == generation`, deferred until the status callback), (b) `Building` is true, (c) `IChatClient` is null, (d) `SelectAsync`/`RestoreAsync` throws, or (e) `Disposed`/Readiness changes. In every such case the flag stays true. Consequences: in (a) the pill grabs focus whenever the deferred rebuild finally lands, even if the user has since moved to the composer or clicked elsewhere; in (b)-(d) the flag survives until some unrelated remount (New conversation, a path-change rebuild, reopening the panel if the component survives) and the new pill steals focus then. Note also that during the rebuild `ReleaseAgent` sets `Agent = null`, so a render in the gap shows "Starting Ask…" and the pill unmounts there; that path still works, but the pending flag has no expiry.
- Suggestion: make the handoff self-limiting. For example, store the `BuiltMode` the focus is waiting for (`FocusPendingMode = mode`) and only pass `RestoreFocus` when `BuiltMode == FocusPendingMode`, and clear the flag in `NewConversationAsync`, `CloseAsync`, and whenever `EnsureAgentAsync` completes a build without a mode change. Alternatively clear it on any focus move away from the pill (focusout on the lead div) so a stale request cannot fire later.
- Status: open

### Issue 2 — Severity: nit
- File: source/container-apps/web/projects/web-spa/features/application/modals/agent-ask/AskEditModePill.razor:5
- Description: Reviewed question 2: the guard `isChecked != true || AgentSurfaceState.EditMode == mode` is correct for radio CheckedChanged. A radio item raises `CheckedChanged(true)` only on selection, the unselected sibling may raise `false` when the web component clears it (ignored by the first clause), and re-choosing the current mode (or a parameter-driven re-set of `Checked`) is ignored by the second clause. No change needed. Separate nit: the Design region mixes comparison to another product ("Cloudflare's pill says 'Ask'") and has a ragged line wrap at "The native root anchors the". The repo's region guidance (tw-agent-context-regions) prefers present-tense design rationale without outside-product history.
- Suggestion: Reword to state why the label is "Ask first"/"Auto-edit" ("a bare 'Ask' would read as the panel's name") and re-wrap line 20.
- Status: open

### Issue 3 — Severity: suggestion
- File: tests/container-apps/web/web-spa-integration-tests/features/application/ask-edit-mode-pill-tests.cs:51
- Description: The component test sends `SetEditMode` directly through the store, so it never exercises the pill's own `ChooseAsync` path (guard, dispatch, `Chose`/`RestoreFocus`/`FocusRestored`). That path, including the focus handoff in Issue 1, is covered only by the WASM Playwright test, which runs against one happy-path rebuild. HtmlRenderer cannot raise `CheckedChanged` through the web component, but the bUnit-free approach can still invoke the callback via the component instance, or the rebuild-deferred case can be added to Playwright (choose a mode while a turn is streaming and assert focus is not stolen afterwards).
- Suggestion: Add a test for the deferred-rebuild case once Issue 1 is fixed; optionally a render test with `RestoreFocus=true` asserting `FocusRestored` fires once (needs a JS runtime double that allows `focus`).
- Status: open

### Issue 4 — Severity: nit
- File: tests/container-apps/web/web-spa-playwright-tests/ask-surface-playwright-tests.cs:490
- Description: After each remount, `AttributeShouldBeAsync(pill, "aria-expanded", "false")` assumes the fresh pill's `aria-expanded` is the string "false". `pill` is a locator so it re-resolves to the new element, but if the web component only writes `aria-expanded` after the first open/close cycle (not on connect), the attribute is absent on a remounted pill and the poll compares null to "false". The first assertion after Enter-to-choose (rebuild case) is the exposed one. Polling with 50 x 100 ms makes it deterministic when the component does set it, so this is a risk only, not a confirmed failure. Otherwise the waits are bounded polls or `WaitFor` with timeouts, no fixed sleeps, and the old `.twe-agent-ask__edit` / "Edit behavior" absence is asserted.
- Suggestion: Assert `aria-expanded` is not "true" (null or "false") after a choice instead of equal to "false".
- Status: open
