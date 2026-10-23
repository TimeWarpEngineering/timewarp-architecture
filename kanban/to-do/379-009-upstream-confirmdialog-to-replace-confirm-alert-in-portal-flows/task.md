# Upstream ConfirmDialog to replace confirm alert in portal flows

## Goal (implementer restatement)

Clear key on Admin → AI must ask before it deletes the site xAI key, and the page must still
only dispatch `ClearXaiApiKey`. The question lives in the action handler, through FluentUI
5.0.1's message box, not a new component and not `window.confirm`.

`ApiHandler` grows one overridable confirm hook that runs before the per-state semaphore. Yes
continues. No returns with no request and no error notification, so answering does not stall
other actions on that state. Handlers depend on a small `IConfirmationService` (title, message,
confirm label, cancel label → bool), registered in `program.cs`, not on FluentUI types. A
stand-in covers tests and hosts with no dialog provider.

The destructive dialog's confirm label is "Clear key" and cancel is "Cancel". The title is
"Clear the xAI API key?" and the message says the Ask panel stops until a new key is saved.
Enter does not confirm. Escape cancels.

The style guide gets a Confirmations card whose opener dispatches a style-guide-only action.
That action asks through the same service. CredentialList's inline revoke stays as it is.

Done when a handler test shows No skips the API, the notification, and the state, and Yes
clears as today; a Playwright pass on Admin → AI with a stored key shows the dialog, Cancel
leaves the key, and Clear key removes it; `./bin/dev build` is 0 warnings and 0 errors;
`./bin/dev template-smoke` passes; `ganda repo audit` passes; and `source` plus `tests` contain
no client names.

## Description

Destructive actions need a confirmation step that fits the rule that every interaction is a
TimeWarp.State action. Today the template has none: `features/admin/ai/pages/AiPage.razor`
"Clear key" (`data-qa="XaiApiKeyClear"`, action `ClearXaiApiKey`) deletes the site's xAI key
with one click.

Crunchit has no confirm component. Three page handlers call the browser's `window.confirm`
through `IJSRuntime` and carry on in the same method. That is not portable to the template's
State rule and is not themed or accessible.

An earlier app by the same author used FluentUI v4's built-in confirmation from the handler:

```csharp
IDialogReference dialog = await DialogService.ShowConfirmationAsync(
  message: "Are you sure you want to delete this module? This action cannot be undone.",
  primaryText: "Yes", secondaryText: "No", title: "Delete Module");
DialogResult result = await dialog.Result;
if (result.Cancelled) return;
await ModuleState.DeleteModule(module.ModuleId);
```

This task uses the same idea on FluentUI v5 (5.0.1), with the confirmation inside the action's
handler so the page still only dispatches. No new component and no new JavaScript.

## Decision

- **FluentUI's built-in confirmation, the earlier app's way** (Steve, 2026-10-23). Not a native
  `<dialog>` ConfirmDialog component, and no shared dialog script. This task does not depend on
  408.
- **v5 API, not v4.** The API changed. Verified from the 5.0.1 package XML docs and the
  fluent-ui-blazor MCP (dialog migration guide):
  - `IDialogService.ShowConfirmationAsync(string message, string title = "Confirmation",
    string primaryButton = "Yes", string secondaryButton = "No")` returns the result directly.
    There is no `IDialogReference` and no `dialog.Result` step. `Cancelled == false` means Yes.
  - `IDialogService.ShowMessageBoxAsync(MessageBoxOptions)` is the configurable form:
    `Title`, `Message`, `PrimaryButton`, `SecondaryButton`, `Icon`, `IconColor`,
    `PrimaryShortCut`, `SecondaryShortCut`.
  - v4 `DialogParameters`, `PrimaryAction`/`SecondaryAction` and `primaryText`/`secondaryText`
    are gone. Check exact parameter names and return types with the MCP or the package before
    coding; do not copy the v4 snippet.
- **The page only dispatches.** "Clear key" keeps dispatching `ClearXaiApiKey`. The handler asks
  and skips the request on No. `ApiHandler` already treats a null request as "skip this action".
- **Ask before the per-state lock.** `ApiHandler` holds a per-(store, state) semaphore around
  `GetRequest`. Waiting for a person inside that lock would stall every other `XaiApiKeyState`
  action (for example the status fetch) until they answer. So the question runs before the lock:
  add one small overridable hook to `ApiHandler` (for example
  `protected virtual Task<bool> ConfirmAsync(TAction action, CancellationToken ct)`, default
  `true`), called before `AcquireSemaphoreAsync`. A No returns without a request and without an
  error notification. Record this in the `ApiHandler` Design region.
- **One thin wrapper over `IDialogService`** (default chosen for this task), for example
  `IConfirmationService.ConfirmAsync(title, message, confirmLabel, cancelLabel)` returning
  `bool`, registered in `program.cs`. Handlers depend on it, not on FluentUI types. A test or a
  host with no dialog provider can register a stand-in. Keep it as small as that; no tone enum
  unless the message box needs one.
- **Safe defaults for a destructive confirm** (default chosen): the confirm label names the act
  ("Clear key"), cancel is "Cancel". Enter must not confirm a destructive action: set
  `PrimaryShortCut` so Enter does not trigger it (check what the v5 default is first). Escape
  cancels.
- **Adoption: "Clear key" only, plus a style-guide sample** (default chosen). CredentialList's
  inline two-step revoke (task 248-001, covered by render tests) stays as it is.
- **The confirmation is not a palette or agent bypass.** `ClearXaiApiKey` is already
  `[CommandPaletteOptOut]`. The confirm runs in the handler, so any dispatcher (page, palette,
  agent surface) gets the same question.

## Checklist

- [x] Confirm the v5 `IDialogService` API (MCP plus package docs) and record the exact calls
      used in the wrapper's Design region
- [x] `ApiHandler`: overridable confirm hook that runs before the per-state semaphore; No skips
      the action silently. Design region updated.
- [x] Confirmation wrapper over `IDialogService.ShowMessageBoxAsync` (or
      `ShowConfirmationAsync` if labels and shortcuts suffice), registered in `program.cs`
- [x] `ClearXaiApiKey` handler overrides the hook: title "Clear the xAI API key?", a message that
      says what stops working (the Ask panel) until a new key is saved, "Clear key" / "Cancel"
- [x] Enter does not confirm the destructive dialog; Escape cancels
- [x] Style guide: a "Confirmations" card with one opener that dispatches an existing harmless
      action through the same hook, or a small style-guide-only action (no page-local flag)
- [x] Tests:
  - Handler-level test: hook says No → no API call, no notification, state unchanged; Yes →
    clears as today. Use the wrapper's stand-in, not a mock of first-party code.
  - Playwright (model: `tests/container-apps/web/web-spa-playwright-tests/drawer-playwright-tests.cs`):
    on Admin → AI with a stored key, click Clear key, the dialog shows, Cancel leaves the key,
    Clear key clears it. Update any existing test that expects Clear to act immediately.
- [x] Gates:
  - `./bin/dev build` reports 0 warnings and 0 errors.
  - `./bin/dev template-smoke` passes.
  - `ganda repo audit` passes.
  - `git diff origin/master -- source tests | rg -i crunchit` prints nothing (no client names of
    any kind in generated code).

## Results

Clear key asks inside `ClearXaiApiKey`'s handler, before the per-state lock. The page still
only dispatches. `DialogConfirmationService` calls FluentUI 5.0.1
`IDialogService.ShowMessageBoxAsync` with an empty primary shortcut so Enter does not confirm,
and `SecondaryShortCut` `"Escape"` so Escape cancels. `ConfirmationStandIn` is the test seam.
A declined clear sets `XaiApiKeyState.ClearDeclined` so the Ask reload does not treat the skip
as a successful clear. The style guide Confirmations card dispatches `ConfirmSample`, which
asks through the same service and posts a shell note only on yes.

`./bin/dev build` — 0 warnings, 0 errors.
`./bin/dev template-smoke` — succeeded (SmokeDefault build 0/0; generated Jaribu 405 + 9 + 14).
`ganda repo audit` — 32 passed, 0 failed.
`git diff origin/master -- source tests | rg -i crunchit` — no output.

### Review

Round 1, effort 2, roster general. 0 bug, 1 suggestion (M1, open), 1 nit (M2, open).
Disposition is pending: a fix pass is needed for M1 and M2. The reviewer re-ran the gates in
this worktree. web-spa integration tests passed 393/393, including both new handler tests.
web-spa Playwright passed 26/26, including the clear-key test. `./bin/dev build` reported
0 warnings and 0 errors, and `ganda repo audit` passed. Paths:
`review/review-framework.md`, `review/round-1/general.md`, `review/round-1/merged.md`.

### How to validate

Smoke: Sign in as an administrator, open Admin → AI, save a site key, and click Clear key.
Press Enter, then Cancel, then open it again and press Escape. Open it once more and click
the dialog's Clear key. Then grant developer access, open Style Guide, click Ask to confirm,
press Escape, open it again, and click Confirm.

Expect: The dialog title is "Clear the xAI API key?" and the message says Ask stops working
until a new key is saved. Enter, Cancel, and Escape leave the stored suffix in place and
leave the dialog closed after Cancel or Escape. The dialog's Clear key removes the key, the
status reads "No key stored.", and the shell shows "xAI API key cleared." On the style guide,
Escape posts no "Sample confirmed." note; Confirm does.

## Session

- Created: ses_ebc1f6587ffeC5oxDhO3SRDeMu (2026-10-16)
- Spec rewritten: c2bec172-f0eb-4648-9edb-30256e7756e3 (2026-10-23) after Steve chose FluentUI's
  built-in confirmation over a native-dialog component.
- Implemented the FluentUI v5 message-box confirm, the ApiHandler hook, the style-guide sample,
  and the handler plus Playwright coverage (2026-10-23). Audit was already green; no boy-scout
  fixes.
- Implement oracle: implemented by implementer-cursor (cursor, model grok-4.7-high), session not reported, max-turns uncapped (no --max-turns flag; bound 500), Done — 2026-10-23T03:20:28Z
- Review oracle round 1 (claude-opus-5-5, effort 2, roster general, session id not reported),
  2026-10-23: 2 open findings (M1 suggestion, M2 nit), fix pass requested.

## Notes

- FluentUI dialogs need `FluentProviders`, which `components/layouts/FluentUIRequiredFeatures.razor`
  renders in the app layout. `CommandPalette.razor` notes that headless hosts have no dialog
  provider; the wrapper is the seam for those.
- `ApiHandler` (`features/base/api-handler.cs`): `GetRequest` returning null is the sanctioned
  skip, but it runs inside the semaphore, which is why the hook sits before it.
- Out of scope: `source/features/web-mcp-page-load.ts` (agent navigation permission, its own
  `window.confirm`), CredentialList's inline revoke, the Drawer (379-008, 408).
