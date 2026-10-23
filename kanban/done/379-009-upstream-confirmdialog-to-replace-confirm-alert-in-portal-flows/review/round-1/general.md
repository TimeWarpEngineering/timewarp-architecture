# Round 1 — general
**Date:** 2026-10-23
**Scope reviewed:** branch vs `master` (d6f54b5f6), `source/` and `tests/`: the ApiHandler hook,
the confirmation service and its registration, the ClearXaiApiKey override, the
ClearDeclined / Ask-reload interaction, the style-guide sample, and both new test files.

## Summary

The change matches the task and the implementer restatement. `ApiHandler.HandleAsync` calls
`ConfirmAsync` after the auth check and before `AcquireSemaphoreAsync`. The default is yes. No
returns with no request and no error. `DialogConfirmationService` uses `ShowMessageBoxAsync`
with an empty `PrimaryShortCut`, so Enter does not confirm, and `SecondaryShortCut` "Escape".
The page still only dispatches `ClearXaiApiKey`, and the style guide dispatches `ConfirmSample`.
Risk is low. The only issues are one state-encapsulation slip and one duplicated assertion.

Gates the reviewer re-ran in this worktree:

- `dotnet run` on `web-spa-integration-tests`: 393/393 passed. `--list-tests` shows
  `No_Should_Skip_Without_A_Call_Or_A_Notification` and `Yes_Should_Clear_And_Notify`.
- `dotnet run` on `web-spa-playwright-tests`: 26/26 passed, including
  `ClearKey_Should_Ask_And_LeaveTheKey_Until_Confirmed`.
- `./bin/dev build`: 0 warnings, 0 errors.
- `ganda repo audit`: passes all checks.
- `git diff master -- source tests | grep -ic crunchit`: 0.

Also checked: some in-proc SPA test containers do not register `IConfirmationService`. They do
not break, because handlers resolve lazily and those suites never send `ClearXaiApiKey` or
`ConfirmSample`; the full integration suite above passes. Web.Server composes
`Web.Spa.Program.ConfigureServices`, so prerender also gets the registration.

## Issues

### Issue 1 — Severity: suggestion
- File: source/container-apps/web/projects/web-spa/features/admin/ai/xai-api-key-state/xai-api-key-state.cs:27
- Description: `ClearDeclined` is `internal bool ClearDeclined { get; set; }`. Every other
  status flag on this state (`SaveFailed`, `ManagedByHost`, …) is `{ get; private set; }` and is
  written only from a nested handler. With the internal setter, any component or service in
  Web.Spa can change `XaiApiKeyState` without going through an action, which breaks the rule
  that state changes go through TimeWarp.State actions. The only writer is
  `ClearXaiApiKeyActionSet.Handler.ConfirmAsync`. That handler is nested in `XaiApiKeyState`, so
  it can still use a private setter.
- Suggestion: make it `{ get; private set; }` (keep `internal` or match `SaveFailed`'s `public`
  getter; `AgentSurfaceState`'s refresh handler only reads it).
- Status: open

### Issue 2 — Severity: nit
- File: tests/container-apps/web/web-spa-integration-tests/features/admin/ai/clear-xai-api-key-confirmation-tests.cs:42
- Description: `standIn.CallCount.ShouldBe(1);` is asserted twice in
  `No_Should_Skip_Without_A_Call_Or_A_Notification` (lines 42 and 45).
- Suggestion: drop one of the two.
- Status: open
