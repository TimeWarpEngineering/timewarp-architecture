# Upstream ConfirmDialog to replace confirm alert in portal flows

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

- [ ] Confirm the v5 `IDialogService` API (MCP plus package docs) and record the exact calls
      used in the wrapper's Design region
- [ ] `ApiHandler`: overridable confirm hook that runs before the per-state semaphore; No skips
      the action silently. Design region updated.
- [ ] Confirmation wrapper over `IDialogService.ShowMessageBoxAsync` (or
      `ShowConfirmationAsync` if labels and shortcuts suffice), registered in `program.cs`
- [ ] `ClearXaiApiKey` handler overrides the hook: title "Clear the xAI API key?", a message that
      says what stops working (the Ask panel) until a new key is saved, "Clear key" / "Cancel"
- [ ] Enter does not confirm the destructive dialog; Escape cancels
- [ ] Style guide: a "Confirmations" card with one opener that dispatches an existing harmless
      action through the same hook, or a small style-guide-only action (no page-local flag)
- [ ] Tests:
  - Handler-level test: hook says No → no API call, no notification, state unchanged; Yes →
    clears as today. Use the wrapper's stand-in, not a mock of first-party code.
  - Playwright (model: `tests/container-apps/web/web-spa-playwright-tests/drawer-playwright-tests.cs`):
    on Admin → AI with a stored key, click Clear key, the dialog shows, Cancel leaves the key,
    Clear key clears it. Update any existing test that expects Clear to act immediately.
- [ ] Gates:
  - `./bin/dev build` reports 0 warnings and 0 errors.
  - `./bin/dev template-smoke` passes.
  - `ganda repo audit` passes.
  - `git diff origin/master -- source tests | rg -i crunchit` prints nothing (no client names of
    any kind in generated code).

## Session

- Created: ses_ebc1f6587ffeC5oxDhO3SRDeMu (2026-10-16)
- Spec rewritten: c2bec172-f0eb-4648-9edb-30256e7756e3 (2026-10-23) after Steve chose FluentUI's
  built-in confirmation over a native-dialog component.

## Notes

- FluentUI dialogs need `FluentProviders`, which `components/layouts/FluentUIRequiredFeatures.razor`
  renders in the app layout. `CommandPalette.razor` notes that headless hosts have no dialog
  provider; the wrapper is the seam for those.
- `ApiHandler` (`features/base/api-handler.cs`): `GetRequest` returning null is the sanctioned
  skip, but it runs inside the semaphore, which is why the hook sits before it.
- Out of scope: `source/features/web-mcp-page-load.ts` (agent navigation permission, its own
  `window.confirm`), CredentialList's inline revoke, the Drawer (379-008, 408).
