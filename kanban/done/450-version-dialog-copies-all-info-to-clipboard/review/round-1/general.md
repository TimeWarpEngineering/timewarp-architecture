# Round 1 — general
**Date:** 2026-10-30
**Scope reviewed:** commit 720786e51 vs its parent (web-spa AssemblyInfoModal copy button, task 450)

## Summary
The change adds an icon-only Copy button to the version dialog. It dispatches a new `ApplicationState.CopyAssemblyInfo` action whose handler writes `AssemblyInfoText.ClipboardText` through `ClipboardJsModule`, with a Warning bar on JSException or interop timeout. It closely follows the existing `AgentLinksState.CopyOnboarding` / `NotificationState.CopyMessage` / `MessageBar` precedents. I found no correctness bugs and no analyzer-silencing changes. The remaining items are about the "one list so they cannot drift" claim, which holds for the property rows but not for the two URL rows, and about test coverage of the modal wiring.

## Issues
### Issue 1 — Severity: suggestion
- File: source/container-apps/web/projects/web-spa/features/application/modals/assembly-info/AssemblyInfoModal.razor:25-34 and assembly-info-text.cs:51-63
- Description: The task goal and the `assembly-info-text.cs` Design region say the display and the copy come from one list, so they cannot drift. That is true only for the 20 property rows (`Properties` feeds both the `foreach` and `ClipboardText`). The two URL rows are listed twice by hand. The modal hard-codes `<LinkDisplay Url=@AssemblyInfo.CommitUrl .../>` and `<LinkDisplay Url=@AssemblyInfo.RepositoryUrl .../>`. `ClipboardText` separately hard-codes `(nameof(CommitUrl), ...)` and `(nameof(RepositoryUrl), ...)`. A third link added to the dialog would silently not be copied. The Title row has the same shape: the `Section Title` and `ClipboardText`'s `Title` entry are two separate places. AGENTS.md says "When two things must agree, generate one from the other or add a build check."
- Suggestion: Add `AssemblyInfoText.Links(AssemblyInfo)` returning `(Name, Url, DisplayText)` for the non-blank links. The modal renders `LinkDisplay` from it, and `ClipboardText` concatenates `Links(...).Select(l => (l.Name, l.Url))`. The URL rows then share one list the same way the property rows do.
- Status: open

### Issue 2 — Severity: suggestion
- File: tests/container-apps/web/web-spa-integration-tests/features/application/assembly-info-copy-tests.cs:10-15 (Design region) and :54-67
- Description: The tests render `AssemblyInfoCopyButton` alone. The Design region justifies this with "ModalContainer needs a ModalController cascade", but `command-palette-tests.cs` (`RenderShellModalsAsync`, ~line 538) already renders a modal through a `ModalController` with `HtmlRenderer` and `SetActiveModal`. So the modal can be tested. `AssemblyInfoModal`'s own wiring is therefore untested. That wiring is the `ActionContent` that replaces the container's default Close (verified in `ModalContainer.razor`: the default `data-qa="ModalButton"` Close renders only when `ActionContent is null`), the re-declared Close button with `data-qa="ModalButton"`, the Copy placement beside it, and the `Properties` loop. If `ActionContent` were dropped, Close would be back to a single default button and Copy would vanish with no test failing. The other direction is also open: if the re-declared Close lost its `CloseModal` handler or its `data-qa`, nothing would notice. No other test references `ModalButton`.
- Suggestion: Add one render test that follows the command-palette precedent. Render `ModalController` with `AssemblyInfoModal`, active after `SetActiveModal(AssemblyInfoModal.ModalId)`. Assert the markup contains exactly one `data-qa="ModalButton"` and one `data-qa="AssemblyInfoCopy"`, plus a property row such as `Version:`. The test must confirm `OnActivate` actually loads `AssemblyInfo` in that harness; if it does not, assert the spinner state shows no Copy button. Also update the Design region's claim.
- Status: open

### Issue 3 — Severity: nit
- File: source/container-apps/web/projects/web-spa/features/application/application-state/application-state.copy-assembly-info.cs:44-68
- Description: This is the third copy of the same try/catch (the `JSException` or live-token `OperationCanceledException` filter, then a swallowed cancelled-token catch) and of the same body string "The browser blocked clipboard access. Select the text and copy it instead." The other two are `AgentLinksState.CopyOnboarding` and `NotificationState.WriteAndConfirmAsync`. It matches the existing precedent exactly, so it is not a defect. Three agreeing copies of a user-facing string and an exception filter is the "must agree" shape AGENTS.md warns about.
- Suggestion: Optional, and cheap to do now. Hoist the failure body and the exception filter into `ClipboardJsModule` (for example a `ClipboardJsModule.IsDenied(exception, token)` helper plus a `DeniedBody` const), or leave a Design-region note that the three copies are known. Do not widen this task if it risks the other two handlers' tests.
- Status: open

### Issue 4 — Severity: nit
- File: tests/container-apps/web/web-spa-integration-tests/features/application/assembly-info-copy-tests.cs:55-65 (fixture) and assembly-info-text.cs:40
- Description: The fixture never sets `CommitDate` metadata or `AlgorithmId`. `CommitDate` is the one row whose formatting changed in this commit, from culture-sensitive `ToString("yyyy-MM-dd HH:mm:ss")` to `CultureInfo.InvariantCulture`. It is also named in the task's screenshot list. The row logic is shared, so the risk is low, but the invariant-format change is unexercised. Verified against the decompiled `TimeWarp.State.Plus` 12.0.0-beta.15 `AssemblyInfo`: `CommitDate` is `DateTime.TryParse(GetMetadata("CommitDate"))`, so a metadata attribute with e.g. "2026-10-28T12:34:56" is enough.
- Suggestion: Add a `CommitDate` `AssemblyMetadata` attribute to `FakeAssemblyInfo` and one expected line "CommitDate: 2026-10-28 12:34:56" in `ExpectedText`.
- Status: open

### Issue 5 — Severity: nit
- File: source/container-apps/web/projects/web-spa/features/application/modals/assembly-info/assembly-info-text.cs:15 and tests/.../assembly-info-copy-tests.cs:24
- Description: `using TimeWarp.State.Plus.Extensions;` is already a global using in both projects (`web-spa/global-usings.cs:91`, `web-spa-integration-tests/global-usings.cs:29`). `.editorconfig` sets IDE0005 to warning. The orchestrator's `dev build` will show whether it fires; `application-state.copy-assembly-info.cs` (`using TimeWarp.Architecture.Services;`, global at `global-usings.cs:87`) follows the same pattern as the existing handlers, so this may not be reported.
- Suggestion: Remove the redundant usings if the build reports IDE0005, or for tidiness regardless.
- Status: open

## Verified claims (no issue)
- `PropertyDisplay` skips blanks with `string.IsNullOrWhiteSpace(Value)`, and `LinkDisplay` does the same for `Url`. `NonBlank` uses the same test.
- `ModalContainer` renders the default Close (`data-qa="ModalButton"`, Primary, `CloseModal`) only when `ActionContent is null`, so the modal's re-declared Close is the right way to keep Close next to Copy.
- The dynamic-assembly fixture yields the expected text. From the `TimeWarp.State.Plus` `AssemblyInfo` decompile: `Version` is `GetName().Version` (1.2.3.4), `CommitHash` is the part of `InformationalVersion` after `+`, and `ShortHash` is the last 6 characters ("234567"). `CommitUrl` is `RepositoryUrl.TrimEnd('/') + "/commit/" + hash`. `InternalsVisibleTo` is an empty string (dropped), `Description` is whitespace (dropped), and `Culture` and the other absent attributes are null. `ExpectedText` matches line for line.
- The handler is the same shape as `AgentLinksState.CopyOnboarding`: same exception filter, `OutcomeNotification` Warning via `IPublisher<ClientPipeline>`, `[CommandPaletteOptOut]`. The button only dispatches (TWA0026). The `MessageBar` icon swap and aria-label/Title pattern is copied faithfully. `\n` joining matches `NotificationState.ClipboardText`.
- `SetActiveModal` is the footer's open path (`TimeWarpPage.razor:111`), so clearing `IsAssemblyInfoCopied` there resets Copied on reopen. The `ApplicationState` setters stay private and mutation stays in handlers.
- `.twe-modal__actions` is `display:flex; gap:8px; justify-content:flex-end`, so Copy plus Close lay out correctly.
- File names are kebab-case `.cs` (TW0001), and Purpose/Design regions are present (TWA0004). The `.razor` files use the in-`@code` comment form that `MessageBar.razor` uses. Tests are Jaribu plus Shouldly with no new frameworks. No analyzer or audit suppression was added.
- Not run, per instructions: the build and the tests.
