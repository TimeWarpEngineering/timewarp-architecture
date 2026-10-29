# Round 1 — merged findings
**Date:** 2026-10-30
**Sources:** general (sonnet subagent), orchestrator verification (build, tests, call sites)

## Orchestrator verification

- `dotnet build tests/container-apps/web/web-spa-integration-tests/web-spa-integration-tests.csproj`: 0 warnings, 0 errors (builds web-spa and its analyzers).
- `--filter-class AssemblyInfoCopy_`: total 5, failed 0, succeeded 5.
- `--filter-class CopyMessage` (CopyMessage_ and CopyMessageBar_ regressions): total 16, failed 0, succeeded 16.
- Implementation checked against `## Description` and `## Goal (implementer restatement)`: the restatement matches the task (copy icon, every displayed row plus both URLs, plain text). No misread. The one gap against the restatement's own "one list so they cannot drift" clause is M1 below.
- Handler is line-for-line the `AgentLinksState.CopyOnboarding` precedent (same filter, `OutcomeNotification` Warning, `[CommandPaletteOptOut]`). Button only dispatches (TWA0026). `ModalContainer` renders its default Close only when `ActionContent is null`, so re-declaring Close with `data-qa="ModalButton"` is correct; `.twe-modal__actions` is a flex row with gap. Decompiled `AssemblyInfo` (TimeWarp.State.Plus 12.0.0-beta.15) confirms the fixture's expected text (ShortHash = last 6 chars, CommitUrl = RepositoryUrl + `/commit/` + hash, blank Description and empty InternalsVisibleTo dropped).
- Verified the general reviewer's load-bearing claims: `command-palette-tests.cs:537` `RenderShellModalsAsync` renders a modal through `ModalController` with `HtmlRenderer` (M2 holds); `TimeWarp.State.Plus.Extensions` is a global using in web-spa (`global-usings.cs:91`) and the test project (`global-usings.cs:29`) (M5 holds; IDE0005 did not fire in the build, but the directive is redundant by fact).
- Noted, no finding: the Warning bar on a failed copy lands in the shell MessageBars region (z-index 200) under the modal backdrop (z-index 1000, 50% alpha). It is dimmed while the dialog is open and fully visible after Close, and only navigation clears it. Same reachability as any handler-published outcome while a modal is up; not specific to this change.

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 2 | 0 | 0 |
| nit | 3 | 0 | 0 |

## Issues

### M1 — Severity: suggestion — Status: open
- File: source/container-apps/web/projects/web-spa/features/application/modals/assembly-info/AssemblyInfoModal.razor:28-33; source/container-apps/web/projects/web-spa/features/application/modals/assembly-info/assembly-info-text.cs:51-63
- Description: The goal restatement and the `assembly-info-text.cs` Design region claim the display and the copy come from one list so they cannot drift. That holds for the 20 property rows only. The two `LinkDisplay` rows are hand-listed in the modal and `ClipboardText` hand-lists `CommitUrl` and `RepositoryUrl` separately, so a link added to the dialog would silently not be copied (AGENTS.md: when two things must agree, generate one from the other). The `Section Title` and the `Title` clipboard entry have the same two-place shape.
- Suggestion: Add `AssemblyInfoText.Links(AssemblyInfo)` returning the non-blank `(Name, Url, DisplayText)` rows; the modal renders `LinkDisplay` from it and `ClipboardText` appends `Links(...).Select(l => (l.Name, l.Url))`. Keep the Title entry as is or route it the same way; update the Design region so its claim is exactly true.
- Source: general
- Disposition notes:

### M2 — Severity: suggestion — Status: open
- File: tests/container-apps/web/web-spa-integration-tests/features/application/assembly-info-copy-tests.cs:10-15, 54-67
- Description: Tests render `AssemblyInfoCopyButton` alone; the Design region says the modal cannot be rendered because `ModalContainer` needs a `ModalController` cascade. `command-palette-tests.cs:537` (`RenderShellModalsAsync`) already renders a modal through `ModalController` with `HtmlRenderer` after `SetActiveModal`, so that justification is wrong. The modal's own wiring is therefore uncovered: the `ActionContent` swap that replaces the container's default Close, the re-declared Close (`data-qa="ModalButton"`, `CloseModal`), Copy's placement beside it, and the `Properties` loop. Dropping `ActionContent` or the Close handler would fail no test; no other test references `ModalButton`.
- Suggestion: Add one render test on the command-palette precedent: render `ModalController` hosting `AssemblyInfoModal`, send `SetActiveModal(AssemblyInfoModal.ModalId)`, and assert the markup has exactly one `data-qa="ModalButton"`, one `data-qa="AssemblyInfoCopy"`, and a property row such as `Version:`. If `OnActivate` does not load `AssemblyInfo` in that harness, assert the spinner state shows no Copy button instead and say so. Correct the Design region's claim.
- Source: general
- Disposition notes:

### M3 — Severity: nit — Status: open
- File: source/container-apps/web/projects/web-spa/features/application/application-state/application-state.copy-assembly-info.cs:50-72
- Description: Third copy of the clipboard-failure shape: the `JSException` / live-token `OperationCanceledException` filter, the swallowed cancelled-token catch, and the body string "The browser blocked clipboard access. Select the text and copy it instead." The other two are `AgentLinksState.CopyOnboarding` and `NotificationState.WriteAndConfirmAsync`. It matches precedent exactly and is not a defect, but three agreeing copies of a user-facing string and an exception filter is the shape AGENTS.md says to collapse.
- Suggestion: Hoist the shared pieces into `ClipboardJsModule` (for example a `DeniedBody` const and an `IsDenied(Exception, CancellationToken)` filter helper, or a `TryWriteTextAsync` that returns false on denial/timeout and rethrows a caller cancel) and use it from all three handlers. Keep the three handlers' own state updates and warnings where they are. If the `NotificationState` call site cannot take it without touching its tests, apply it to the two `OutcomeNotification` handlers and note the remaining copy in the Design region.
- Source: general
- Disposition notes:

### M4 — Severity: nit — Status: open
- File: tests/container-apps/web/web-spa-integration-tests/features/application/assembly-info-copy-tests.cs:124-140; source/container-apps/web/projects/web-spa/features/application/modals/assembly-info/assembly-info-text.cs:44
- Description: `CommitDate` is the one row whose formatting changed in this commit (`ToString("yyyy-MM-dd HH:mm:ss")` to `CultureInfo.InvariantCulture`), and it is in the task's screenshot list, but the fixture never sets `CommitDate` metadata so the line is unexercised. Decompiled `AssemblyInfo.CommitDate` is `DateTime.TryParse(GetMetadata("CommitDate"))`, so one more `AssemblyMetadataAttribute` is enough.
- Suggestion: Add a `CommitDate` metadata attribute (for example `2026-10-28T12:34:56`) to `FakeAssemblyInfo` and the matching `CommitDate: 2026-10-28 12:34:56` line to `ExpectedText` in display order (after `ShortHash`, before `CommitUrl`).
- Source: general
- Disposition notes:

### M5 — Severity: nit — Status: open
- File: source/container-apps/web/projects/web-spa/features/application/modals/assembly-info/assembly-info-text.cs:15; tests/container-apps/web/web-spa-integration-tests/features/application/assembly-info-copy-tests.cs:24
- Description: `using TimeWarp.State.Plus.Extensions;` is already a global using in both projects (`web-spa/global-usings.cs:91`, `web-spa-integration-tests/global-usings.cs:29`). The build did not raise IDE0005 for it, but the directive is redundant.
- Suggestion: Remove both redundant usings (Roslynk `remove_unused_usings` per `.editorconfig`'s IDE0005 note).
- Source: general, orchestrator (verified)
- Disposition notes:

## Duplicates / conflicts

- None. One reviewer; the orchestrator added verification evidence to the general findings and raised no separate findings.

## Evaluate

- 0 bugs. 5 open (2 suggestions, 3 nits). All five are concrete, small, and on the files this commit touched, so round 1 requests one fix pass on this task id rather than wontfixing on effort grounds (template repo: getting it right outranks move-cost). Re-review in `round-2/` carries M1–M5 with updated status.
