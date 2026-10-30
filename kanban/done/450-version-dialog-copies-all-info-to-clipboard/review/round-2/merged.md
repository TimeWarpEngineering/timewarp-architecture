# Round 2 — merged findings
**Date:** 2026-10-30
**Sources:** general (sonnet subagent), orchestrator verification (build, tests, call sites)
**Scope:** fix-pass commit 92906f076 (`152c2f866..HEAD`, product paths only); re-verification of round-1 M1–M5 plus a scan of the fix delta. Round 1 (`review/round-1/`) is frozen.

## Orchestrator verification

- `dotnet build tests/container-apps/web/web-spa-integration-tests/web-spa-integration-tests.csproj`: 0 warnings, 0 errors (builds web-spa, its analyzers, and the test project).
- `--filter-class AssemblyInfoCopy_`: total 7, failed 0, succeeded 7 (the 5 round-1 tests plus the two ModalController-hosted modal tests).
- `--filter-class CopyMessage` (CopyMessage_ and CopyMessageBar_, the two other `TryWriteTextAsync` callers): total 16, failed 0, succeeded 16.
- Grep over `source/` and `tests/`: "blocked clipboard access" occurs only at `clipboard-js-module.cs:25`; the `JSException ||` filter only at `clipboard-js-module.cs:43`; no `CopyFailedBody` / `AssemblyInfoCopyFailedBody` reference remains (M3 holds at the code level).
- `PropertyDisplay.razor` renders `<span class="twe-prop__name">@PropertyName:</span>`, so the modal test's `>Version:<` assertion matches real markup. `ModalController.razor:22` invokes `modal.OnActivate` on a parameter change, so the test host's render-then-open order is what makes `Activate` load the real Web.Spa `AssemblyInfo` (M2 holds).
- `Close_The_Dialog_From_Its_Own_Close_Button` pre-sends `SetActiveModal` to the store; the test host keeps its own `ActiveModalId`, so that send is what makes the later `ActiveModalId.ShouldBeNull` non-vacuous. Not dead.
- Implementation re-checked against `## Description` and `## Goal (implementer restatement)`: still aligned (copy icon, every displayed row plus both URLs, plain text, Warning on denial, Copied checkmark). No misread.
- Noted, no finding: the fix pass re-wrapped the Design regions of `agent-links-state.copy-onboarding.cs` (line 8 is 150 columns beside 89-column neighbours) and `application-state.copy-assembly-info.cs` (line 12 is 134 columns). The repo sets no line limit and web-spa already has 32 comment lines over 130 columns, so this is not raised. Worth tidying if M6 is taken.

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 2 | 0 |
| nit | 3 | 3 | 0 |

Rollup across rounds: 8 raised (M1–M8); 5 fixed; 3 open (all nit); 0 wontfix.

## Prior findings (carried from round 1)

### M1 — Severity: suggestion — Status: fixed
- File: `assembly-info-text.cs:17-19, 52-72`; `AssemblyInfoModal.razor:25-35`
- Evidence: `AssemblyInfoText.Title(AssemblyInfo?)` and `Links(AssemblyInfo)` join `Properties`. The modal renders the Section heading from `Title`, the rows from `Properties`, and the `LinkDisplay` rows (URL and display text) from `Links`. `ClipboardText` is `NonBlank(Title) ++ Properties ++ Links.Select(l => (l.Name, l.Url))`. No `AssemblyInfo.CommitUrl` / `RepositoryUrl` / `Title` is read in the modal outside `AssemblyInfoText`. The Design region of the modal and of `assembly-info-text.cs` now describe this (see M6 for one remaining overstatement).
- Source: general; orchestrator (diff read)
- Disposition notes: fixed in 92906f076.

### M2 — Severity: suggestion — Status: fixed
- File: `assembly-info-copy-tests.cs:108-150, 206-238`
- Evidence: `Render_Copy_Beside_One_Close_In_The_Open_Dialog` renders a private `ModalHost` (`ModalController` + `AssemblyInfoModal`) with `HtmlRenderer`, first closed (no `ModalButton`), then opened; asserts exactly one `ModalButton`, exactly one `AssemblyInfoCopy`, Copy before Close, and a `Version:` row. `Close_The_Dialog_From_Its_Own_Close_Button` renders the same host with `ClickRenderer`, clicks `ModalButton`, and asserts the store's `ActiveModalId` is cleared. The Design region no longer claims the modal cannot be rendered; it names the command-palette precedent and the one deliberate difference (open after the first render so `OnActivate` runs). Orchestrator ran both: pass.
- Source: general; orchestrator (test run)
- Disposition notes: fixed in 92906f076.

### M3 — Severity: nit — Status: fixed
- File: `clipboard-js-module.cs:25-51`; the three handlers
- Evidence: `ClipboardJsModule.DeniedBody` and `TryWriteTextAsync` returning `WriteResult { Written, Denied, Cancelled }`. `AgentLinksState.CopyOnboarding`, `ApplicationState.CopyAssemblyInfo`, and `NotificationState.WriteAndConfirmAsync` switch on it and keep their own titles and state updates; each switch covers all three members. Filter and Cancelled semantics equal the removed catch blocks. The three duplicate body constants are gone and the notification tests assert `ClipboardJsModule.DeniedBody`.
- Source: general; orchestrator (grep)
- Disposition notes: fixed in 92906f076.

### M4 — Severity: nit — Status: fixed
- File: `assembly-info-copy-tests.cs:42, 50, 187-199`
- Evidence: `Metadata("CommitDate", "2026-10-28T12:34:56")` in the fixture; `CommitDate: 2026-10-28 12:34:56` in `ExpectedText` between `ShortHash` and `CommitUrl`, which matches `Properties` order (`CommitDate` last) followed by `Links`. Exact-text test passes.
- Source: general; orchestrator (test run)
- Disposition notes: fixed in 92906f076.

### M5 — Severity: nit — Status: fixed
- File: `assembly-info-text.cs`; `assembly-info-copy-tests.cs:19-23`
- Evidence: both `using TimeWarp.State.Plus.Extensions;` directives removed. The usings the test file added (`Microsoft.AspNetCore.Components`, `.Rendering`, `.Web`, `.Web.HtmlRendering`, `Microsoft.Extensions.Logging`) are not global in the test project and are each used. Build 0 warnings.
- Source: general; orchestrator (build, global-usings grep)
- Disposition notes: fixed in 92906f076.

## New findings (fix delta)

### M6 — Severity: nit — Status: open
- File: source/container-apps/web/projects/web-spa/features/application/modals/assembly-info/assembly-info-text.cs:7
- Description: The Design region opens "Everything the dialog shows comes from here". The dialog also shows the `"Assembly"` fallback heading (`AssemblyInfoModal.razor:25`) and the loading spinner (`:37-40`), neither of which comes from `AssemblyInfoText`; and the Title, Properties, Links order is written once in the markup and once in `ClipboardText`, so the order still agrees by convention. The claim is true for every value the clipboard copies (Copy renders only after the record loads). Round 1's M1 was an overstated claim in this same region, so the wording should be exactly true this time.
- Suggestion: Narrow the sentence, for example "Every value the dialog shows comes from here (the loading state and the fallback heading aside)". No code change. If taken, re-wrap the 150- and 134-column Design lines in `agent-links-state.copy-onboarding.cs:8` and `application-state.copy-assembly-info.cs:12` at the same time (noted above).
- Source: general
- Disposition notes:

### M7 — Severity: nit — Status: open
- File: tests/container-apps/web/web-spa-integration-tests/features/application/assembly-info-copy-tests.cs:23-24
- Description: `using Microsoft.AspNetCore.Components.Web.HtmlRendering;` precedes `using Microsoft.AspNetCore.Components.Web;`. Every other using in this file and in the sibling test files is in ordinal order. No analyzer enforces ordering (`dotnet_sort_system_directives_first = false`, no IDE0065 rule), so this is cosmetic.
- Suggestion: Swap the two lines.
- Source: general
- Disposition notes:

### M8 — Severity: nit — Status: open
- File: source/container-apps/web/projects/web-spa/services/clipboard-js-module.cs:53
- Description: `WriteTextAsync` stays `internal` but its only caller is now `TryWriteTextAsync`. The Design region says `TryWriteTextAsync` is "the one place the copy handlers classify a failure"; an internal `WriteTextAsync` lets a future handler bypass it and re-grow the try/catch copy M3 just collapsed. Making it private turns the Design claim into something the compiler enforces (AGENTS.md: when two things must agree, add a check).
- Suggestion: Make `WriteTextAsync` `private`. `TryWriteTextAsync` stays `internal`, consistent with the class.
- Source: general; orchestrator (agrees: this guards the M3 invariant)
- Disposition notes:

## Duplicates / conflicts

- None. One reviewer; the orchestrator added verification evidence and raised no separate finding. The orchestrator's line-width candidate was withdrawn after checking repo precedent and is recorded as a note under M6.

## Evaluate

- 0 bugs, 0 suggestions open. 3 open nits (M6, M7, M8), all on files this fix pass touched, each a one-line or one-sentence change. M8 protects the invariant M3 introduced; M6 is the same kind of overstated Design claim M1 was. Per the round-1 rule on this task (getting it right outranks move-cost; no wontfix on effort grounds), round 2 requests one fix pass on this task id for M6–M8. Re-review lands in `review/round-3/` carrying M1–M8 with updated status. Expected round-3 outcome: clean.
