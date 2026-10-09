# Round 3 — general
**Date:** 2026-10-10
**Scope reviewed:** merge e09074295 conflict resolution + 5cb3a6023

## Summary
The conflict resolution keeps both sides. From 293: Unauthenticated, Error with problem text and Retry, the NotConfigured-only setup command, and the probe in `AuthenticationStateListener` at startup and on every auth change. The shell no longer probes. From 292: the docked panel, edit modes, per-conversation credential, restored transcript, the expired header, WebMCP parity, and the privacy notice, `SupportUrl` (normalized) and credential lifetime from the server answer, which reset to `XaiChatDefaults` on any failure.

`ChatProbeCompleted` and `ModalController` have no remaining references for Ask. The Purpose and Design regions in all five conflicted files describe the merged behavior. Sign in and Retry dispatch State actions (`CloseAskPanel`, then `RouteState.ChangeRoute`; `LoadChatConfiguration`), so no page-local navigation was added. The 293 Playwright sign-in tests still work against the panel: Ctrl-K `CommandPaletteAsk` opens it, `AgentAskClose` closes it, and the "hidden" wait is satisfied because the panel unmounts. A sign-out is a full page load (`ProfileState.SignOut`), so the 292 credential and transcript cannot carry over to the next principal through 293's in-app re-probe.

Two minor findings follow.

## Issues
### Issue 1 — Severity: suggestion
- File: source/container-apps/web/projects/web-spa/features/application/modals/agent-ask/AgentAsk.razor:258
- Description: `SignInAsync` is the only behavior the merge rewrote rather than combined. It now closes the panel instead of the modal and routes to Login with the current page as the return path. No test runs it. `agent-ask-readiness-tests.cs` only checks that `AgentAskSignInButton` is in the markup, because HtmlRenderer cannot click. `ask-sign-in-playwright-tests.cs` signs in through Home's `HomeSignIn`, not through the Ask button. If the close step or the return path broke, everything would still pass.
- Suggestion: Add a Playwright step that clicks `AgentAskSignInButton` from a non-Home page while signed out. It should assert that the URL is `/Login?...returnUrl=<that page>` and that `[data-qa=AgentAsk]` is gone. Alternatively, add an in-proc test that dispatches the same two actions and checks `IsPanelOpen == false` and the route.
- Status: open

### Issue 2 — Severity: nit
- File: source/container-apps/web/projects/web-spa/features/application/modals/agent-ask/AgentAsk.razor:38
- Description: `public const string ModalId = nameof(AgentAsk);` has no references. Its last user was 293's `agent-ask-readiness-tests.cs` (`SetActiveModal(AgentAsk.ModalId)`), and the merge replaced that with `OpenAskPanel`. The Design region says the panel is "Docked on the shell, not a modal", so a leftover `ModalId` suggests a modal path that no longer exists.
- Suggestion: Remove the constant. The `modals/agent-ask/` folder name has the same problem, but it predates the merge and moving it is a separate placement change.
- Status: open
