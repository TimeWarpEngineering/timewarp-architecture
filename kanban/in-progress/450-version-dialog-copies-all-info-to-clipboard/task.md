# Version dialog copies all info to clipboard

## Description

The version dialog (footer version button opens AssemblyInfoModal) shows the
full assembly record but offers no way to copy it. Add a copy icon action that
puts all of the displayed information on the clipboard as plain text.

Reported 2026-10-28 with screenshot of the Web.Spa dialog (Description,
Configuration, Company, Product, Version, FileVersion, InformationalVersion,
InternalsVisibleTo, CommitHash, ShortHash, CommitDate, CommitUrl,
RepositoryUrl).

## Goal (implementer restatement)

The footer version dialog (AssemblyInfoModal) gets an icon-only Copy button that puts
everything the dialog shows on the clipboard as plain text, so a user can paste the build
identity into a bug report.

Acceptance:

- Copy button (FluentUI Copy glyph, `data-qa="AssemblyInfoCopy"`) renders only once the
  assembly record is loaded; it is absent while AssemblyInfo is null (the spinner state).
- The text is one `Name: value` line per displayed property, plus `CommitUrl` and
  `RepositoryUrl`, in display order. Blank values are skipped exactly as PropertyDisplay skips
  them (the display and the copy come from one list so they cannot drift).
- The button only dispatches an action (TWA0026); the handler writes through
  ClipboardJsModule. A clipboard rejection or interop timeout becomes a Warning bar; a success
  swaps the icon to a Checkmark labelled "Copied" until the dialog is reopened.
- Jaribu test: the copied text contains the commit hash and both URLs and omits blank
  properties; the button is not rendered when AssemblyInfo is null; denial yields the Warning.

## Checklist

- [x] Add a copy button (FluentUI copy glyph, data-qa AssemblyInfoCopy) to
  AssemblyInfoModal that serializes every displayed property plus the two URLs
  as "Name: value" lines and writes it via AskAiJsModule.CopyTextAsync (or the
  ClipboardJsModule WriteText path with a Warning bar on denial).
- [x] Omit blank properties the same way PropertyDisplay does.
- [x] Cover with a test (serialized text contains the commit hash and both
  URLs; button hidden while AssemblyInfo is null).

## Session

- Created: ses_e7be8301fffeuQA2WOJDPvWuQh (2026-10-28)
- Implement oracle: implemented by implementer-claude (claude, model claude-opus-5-5), session 224a923e-4224-4f0b-86cd-4ab674ed19a6, max-turns 500, Done — 2026-10-29T20:19:14Z
- Review oracle round 1: review-claude (claude, model claude-fable-5-1, headless print mode, turn cap 120), effort 2, roster general (sonnet subagent) + orchestrator verification — 2026-10-30. Blocked: fix pass requested for M1–M5.
- Review oracle: review by reviewer-claude-fable (claude, model claude-fable-5-1), session a0253f08-39dd-478a-872f-807baa310f45, max-turns 120 — 2026-10-29T20:25:57Z
- Implement-fix oracle (round 1 fix pass): implementer-claude (claude, model claude-opus-5-5), headless print mode — 2026-10-30. Fixed M1–M5; review round 2 is next.
- Review oracle round 2: review-claude (claude, model claude-fable-5-1, headless print mode, turn cap 120), effort 2, roster general (sonnet subagent) + orchestrator verification — 2026-10-30. M1–M5 confirmed fixed; Blocked: fix pass requested for M6–M8 (nits).

## Results

- `AssemblyInfoText` (`features/application/modals/assembly-info/assembly-info-text.cs`) is the
  one source for everything the dialog shows: `Title` (the Section heading), `Properties` (the
  PropertyDisplay loop), and `Links` (the LinkDisplay loop, `CommitUrl` / `RepositoryUrl` with
  their display text). `ClipboardText` is built from those same three members as `Name: value`
  lines. Blank values are dropped with PropertyDisplay's IsNullOrWhiteSpace test.
- `AssemblyInfoCopyButton.razor`: an icon-only FluentButton (Copy glyph, `data-qa="AssemblyInfoCopy"`,
  aria-label "Copy version info") that renders only when AssemblyInfo is non-null and only
  dispatches. It swaps to Checkmark / "Copied" after a successful copy.
- `ApplicationState.CopyAssemblyInfo` (`application-state.copy-assembly-info.cs`) writes through
  `ClipboardJsModule.TryWriteTextAsync`. A Denied write (JSException or interop timeout)
  publishes a Warning OutcomeNotification ("Couldn't copy the version info."). `IsAssemblyInfoCopied` is cleared by
  `SetActiveModal` and `Initialize`. The action is `[CommandPaletteOptOut]`, like CopyOnboarding.
- AssemblyInfoModal renders its heading, rows, and links from `AssemblyInfoText` and uses
  `ActionContent` for Copy plus its own Close (`data-qa="ModalButton"` is kept).
- `ClipboardJsModule` now owns the clipboard-failure shape the three copy handlers shared:
  `TryWriteTextAsync` returns `Written`, `Denied` (JSException, or OperationCanceledException
  with a live caller token), or `Cancelled` (caller token cancelled), and `DeniedBody` is the one
  Warning body. `NotificationState.WriteAndConfirmAsync`, `AgentLinksState.CopyOnboarding`, and
  `ApplicationState.CopyAssemblyInfo` switch on it and keep their own titles and state updates.
  The three duplicate `CopyFailedBody` / `AssemblyInfoCopyFailedBody` constants are gone; the
  notification tests assert `ClipboardJsModule.DeniedBody`.
- Tests: `tests/container-apps/web/web-spa-integration-tests/features/application/assembly-info-copy-tests.cs`
  (7 tests, in-proc C-create, reusing the NotificationCopy_ recording doubles). They use a
  dynamic-assembly AssemblyInfo fixture (including CommitDate metadata): exact text including
  the commit hash, the CommitDate line, and both URLs, blanks omitted, button absent when
  AssemblyInfo is null, click writes and confirms, reopen clears, denial and timeout give the
  Warning. Two modal tests host AssemblyInfoModal in a ModalController (closed, then opened so
  OnActivate runs): the open dialog has exactly one `ModalButton` and one `AssemblyInfoCopy`,
  Copy before Close, and a `Version:` row; clicking `ModalButton` clears `ActiveModalId`.
  Deleting the re-declared Close fails both (mutation-checked).
- Gates run: `ganda repo audit` passes (one advisory: kanban-landed lists other tasks' stale
  claims, which are not on this id). `dev build` gives 0 warnings and 0 errors. The new class
  passed 5/5. CopyMessage passed 16/16, CommandPalette 45/45, and the RazorRaw and RazorLoading
  guards 1/1 each. `dev template-smoke` (default variant) succeeded.
- Fix pass (2026-10-30) gates: `dev build` 0 warnings / 0 errors. AssemblyInfoCopy_ 7/7,
  CopyMessage 16/16, CommandPalette 45/45. `CopyOnboarding` has no test class; it compiles and
  its outcomes are unchanged. `ganda repo audit`: kanban-landed needed this kitchen landed on
  origin-home (fixed with `ganda kanban reconcile`); the remaining Warning lists
  other tasks with no worktree. Audit exits 0.
- Not run: the ActionCatalog and Catalog classes use the Aspire session lane. In this headless
  worker they fail at DCP environment setup (portForServing web-server-https), which is not
  caused by this change.

### Review

- Round 1 (2026-10-30), effort 2, roster general + orchestrator verification. Reviewer re-ran the
  build (0/0) and the tests (AssemblyInfoCopy_ 5/5, CopyMessage regressions 16/16). Goal
  restatement matches the task; no misread.
- Counts: bug 0 · suggestion 2 open · nit 3 open · fixed 0 · wontfix 0.
- Open: M1 links and Title are not on the shared list the "cannot drift" claim names; M2 the
  modal's ActionContent/Close wiring has no render test although a ModalController harness
  exists; M3 third copy of the clipboard-failure filter and body string; M4 fixture skips the
  reformatted CommitDate row; M5 redundant `TimeWarp.State.Plus.Extensions` usings.
- Disposition: fix pass done on this id (2026-10-30), all five addressed. M1 fixed:
  `AssemblyInfoText.Title` and `Links` join `Properties`, the modal renders from them, and
  `ClipboardText` is built from all three. M2 fixed: two ModalController-hosted modal tests; the
  test Design region no longer claims the modal cannot be rendered. M3 fixed:
  `ClipboardJsModule.TryWriteTextAsync` + `DeniedBody`, used by all three copy handlers. M4
  fixed: CommitDate metadata and its expected line. M5 fixed: both redundant usings removed.
  Re-review lands in `review/round-2/`.
- Round 2 (2026-10-30), effort 2, roster general + orchestrator verification, scope = fix-pass commit
  92906f076 only. Reviewer re-ran the build (0/0) and the tests (AssemblyInfoCopy_ 7/7, CopyMessage
  16/16) and grepped for the collapsed clipboard-failure strings (one copy left, in
  ClipboardJsModule). Goal restatement still matches the task; no misread.
- Round 2 counts: bug 0 · suggestion 0 open / 2 fixed · nit 3 open / 3 fixed · wontfix 0.
  M1–M5 fixed. New: M6 the `assembly-info-text.cs` Design claim "Everything the dialog shows comes
  from here" still overstates (fallback heading and spinner); M7 two test usings out of ordinal
  order; M8 `ClipboardJsModule.WriteTextAsync` should be private so handlers cannot bypass
  `TryWriteTextAsync`.
- Round 2 disposition: fix pass requested on this id for M6–M8; re-review in `review/round-3/`.
- Paths: `review/review-framework.md`, `review/round-1/merged.md`, `review/round-2/general.md`,
  `review/round-2/merged.md`.

### How to validate

Smoke:

```bash
dotnet build tests/container-apps/web/web-spa-integration-tests/web-spa-integration-tests.csproj
dotnet run --project tests/container-apps/web/web-spa-integration-tests/web-spa-integration-tests.csproj --no-build -- --filter-class AssemblyInfoCopy_
```

Then, manually: run the app with `dev run`, click the footer version button, click the copy icon
beside Close, and paste.

Expect: the test run shows `total: 7 failed: 0 succeeded: 7`. In the app, the icon turns into a
checkmark labelled "Copied". The pasted text is `Title: Web.Spa` followed by one `Name: value`
line per row the dialog shows (blank rows absent), ending with
`CommitUrl: …/commit/<hash>` and `RepositoryUrl: …`. If the browser denies clipboard access, a
Warning bar reads "Couldn't copy the version info.".

## Review

- Round 1: Blocked — open M1, M2, M3, M4, M5
- Round 1 fix pass: M1–M5 implemented; awaiting round 2.
- Round 2: M1–M5 fixed. Blocked — open M6, M7, M8 (nits); fix pass requested, then round 3.

## Notes

Modal lives at features/application/modals/assembly-info/AssemblyInfoModal.razor.
Existing clipboard precedent: AskAnswerBar.razor CopyAsync through
AskAiJsModule.CopyTextAsync; shell notification CopyMessage through
source/features/clipboard.ts WriteText with denial surfaced as a Warning bar.
