# Round 1 — tests
**Date:** 2026-10-10
**Scope reviewed:** branch vs master

## Summary
The parity test is not tautological in structure: the in-app side goes through `CatalogAgentFunctions.Create` (wrapper, page_context append) and the WebMCP side through `WebMcpPublisher.DescribeAsync`, and it compares name, description, schema, approval bit and order across every known route, extra routes, four principals and both modes. It uses Shouldly only and follows the co-located Jaribu partial pattern. The gaps are in what it does not drive: the real publish and panel build paths, in-app behaviour for an expired credential in ask mode, and the new panel behaviours in Playwright.

## Issues
### Issue 1 — Severity: suggestion
- File: tests/container-apps/web/web-spa-integration-tests/features/application/catalog-agent-parity-tests.cs:272
- Description: Both sides start from the same `CatalogAgentToolSet.SelectAsync`, so name/permission/page filtering cannot diverge in this test by construction. It only proves the two wrappers agree. The list actually published (`WebMcpPublisher.PublishAsync` via `WebMcpRegistration.ApplyAsync`) and the list the panel actually builds (`AgentAsk.razor` rebuild at line ~165) are not exercised; a regression that bypasses `DescribeAsync` or passes the wrong mode (for example `PublishAsync` reading `AgentSurfaceState.EditMode`) would pass.
- Suggestion: Add one case that sets the mode on `AgentSurfaceState`, runs `PublishAsync` against a recording `IWebMcpModelContext`, and compares the captured list with the in-app list built the way the panel builds it. At minimum cover the mode flowing from state into `PublishAsync`.
- Status: open

### Issue 2 — Severity: suggestion
- File: tests/container-apps/web/web-spa-integration-tests/features/application/catalog-agent-parity-tests.cs:177
- Description: design.md section 4 says an expired credential in ask mode "still prompts, then refuses" on both drivers. The ask-mode prompt-then-refuse path is tested only for WebMCP (lines 186-191). The in-app path is tested only in automatic mode (lines 200-209), so the in-app approval-then-credential ordering is unverified. Likewise `Automatically_Edit_Runs_Without_A_Prompt` (line 161) covers only WebMCP; the in-app automatic run is not asserted to execute successfully with a valid credential.
- Suggestion: Add the ask-mode in-app case (approval wrapper first, then `ExpiredError`) and a positive in-app auto-edit run that increments the counter.
- Status: open

### Issue 3 — Severity: nit
- File: tests/container-apps/web/web-spa-integration-tests/features/application/catalog-agent-parity-tests.cs:92
- Description: The credential at lines 92-98 and in `RememberConversationAsync` (line 320) is issued with `DateTimeOffset.UtcNow` and a 1 hour lifetime, but the issuer caps expiry at the next UTC midnight. Within the last moments before 00:00 UTC, `Denial(...)` (which reads the real clock) can see the credential as expired, so the "allowed" assertions would fail intermittently. `Denial` already takes an injectable `utcNow`.
- Suggestion: Use a fixed `now` for the issue call and pass the same value to `Denial(..., utcNow)` in the unit-style assertions. For the dispatcher/function cases a fixed clock is not injectable, so accept the tiny window or issue with a lifetime from a mid-day anchor.
- Status: open

### Issue 4 — Severity: suggestion
- File: tests/container-apps/web/web-spa-playwright-tests/ask-surface-playwright-tests.cs:145
- Description: The new Playwright assertions mostly check presence. The edit-mode switch is only waited for (`AskEditMode`) and never toggled. The privacy notice is only asserted absent (count 0), so the positive and dismiss behaviour is untested in the browser, and the integration test covers only the pure `ShouldShow` predicate, not the dismiss action. The `@` menu is only asserted in its empty state. Copy/thumbs/Support are waited for but never clicked (so `SubmitFeedback` dispatch is unverified). The "See reasoning" disclosure is not asserted. Suggestion count is `>= 3` while the design describes five task-shaped cards. The panel rebuild on mode or generation change (`BuiltMode`/`BuiltGeneration`) and the credential issue at `AgentAsk.razor:110` are not covered anywhere.
- Suggestion: Add a state-level test for `DismissPrivacyNotice`, and extend the browser test to toggle edit mode and assert the stored mode, and to assert the exact suggestion count. A test that the panel issues a conversation credential on first send would cover the only production caller of the issuer.
- Status: open
