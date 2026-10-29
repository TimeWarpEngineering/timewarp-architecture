# Copy icon on every message bar

## Description

Steve, 2026-10-29: every message bar should have a copy icon that copies the bar's text to the clipboard.

Shell notification rows already do this (done task 313): `MessageBars.razor` puts an icon-only Copy button on each `NotificationState` row, and `CopyMessage` writes the text through `ClipboardJsModule`. This task gives that same control to every other rendered `FluentMessageBar`.

## Requirements

- Each rendered message bar has an icon-only Copy button with accessible name "Copy message".
- Copied text matches task 313: the title, or the title, a blank line, and the body when the bar has body text. Copy the message text. Leave out button labels. On "Name this passkey", copy the title and leave the nickname draft out.
- The button only dispatches a TimeWarp.State action. The handler owns clipboard interop and reuses `ClipboardJsModule`. TWA0026 stays clean: no `IJSRuntime` in a component and no `[DirectComponentSideEffect]` opt-out.
- A successful copy swaps the icon to a check labelled "Copied" for the same short interval task 313 uses. A clipboard failure reports a Warning through `NotificationState` ("Couldn't copy the message.") and does not throw.
- Bars that already have actions keep those actions. Copy sits with them. That covers both bars in `AddPasskeyPrompt.razor` (Save / Skip, and Add a passkey / Later).
- One shared control paints the button, and the shell host uses it too, so a new bar gets the icon from that control. A convention analyzer fails the build when a product `FluentMessageBar` is rendered outside that control and outside the shell host. Record the new diagnostic id in `AnalyzerReleases.Unshipped.md`. Analyzer test fixtures that stub `FluentMessageBar` stay exempt.
- Update the design-system "Message bars" usage snippet when the markup authors copy changes. That snippet is a `<pre>` on the single-part view (`DesignSystemParts`), not a second live bar.

### Bars still missing the icon

- `features/admin/site-settings/pages/AuthenticationPage.razor` — "Configuration drift"
- `features/identity/pages/microsoft-365-choose-page/ChooseMicrosoft365Page.razor` — "Session expired"
- `features/identity/components/AddPasskeyPrompt.razor` — "Name this passkey" and "Add a passkey"
- `features/design-system/samples/MessageBarsSample.razor` — Success, Error, Warning, and Information samples

Leave these as page-local bars. TWA0025 already allows static Info and Warning guidance outside the shell host. Moving them into `NotificationState` is out of scope.

## Goal (implementer restatement)

Every message bar the SPA paints gets the task 313 Copy icon, and a new bar cannot skip it.

- One shared `MessageBar` component (`components/MessageBar.razor`) wraps `FluentMessageBar`
  and paints the icon-only Copy button ("Copy message", swaps to a "Copied" check for
  `CopiedConfirmationInterval`). The shell `MessageBars` host paints its rows through it.
- The four page-local call sites (Configuration drift, Session expired, both passkey bars,
  the four design-system samples) render `MessageBar` instead of `FluentMessageBar`. Their own
  actions (Save / Skip, Add a passkey / Later) stay beside Copy.
- Copy only dispatches. Shell rows keep `CopyMessage(id)`; page bars dispatch a new
  `NotificationState` action that writes `ClipboardText(title, body)` through
  `ClipboardJsModule`. A failure is the same Warning ("Couldn't copy the message.") and
  never throws. Copied text is title, or title + blank line + body; button labels and the
  nickname input are not copied.
- A new convention analyzer reports `FluentMessageBar` rendered anywhere except the shared
  control and the shell host, recorded in `AnalyzerReleases.Unshipped.md`. TWA0025 keeps
  catching Error/Success page bars now that they render through `MessageBar`.
- The design-system "Message bars" `<pre>` snippet shows the `MessageBar` markup.
- Acceptance: tests for copied text, the failure Warning, and the Copy button dispatching
  on the shared control and each design-system sample; analyzer tests; `dev build` clean;
  `ganda repo audit` passes.

## Checklist

- [x] Shared copy control used by the shell host and every other message bar
- [x] Copy icon on the four call sites listed above, including both passkey bars
- [x] Analyzer so a new product `FluentMessageBar` cannot skip the control
- [x] Tests: copied text, failure Warning, and that each new button dispatches the action
- [x] `dev build` 0 warnings / 0 errors and `ganda repo audit` pass

## Session

- Created: 01a18418-875e-7d10-9d0a-d9d22584627c (2026-10-28)
- Spec: 01a18418-875e-7d10-9d0a-d9d22584627c (2026-10-29)
- Implement: ganda task work implementer, Claude Opus 5.5 (2026-10-29)
- Implement oracle: implemented by implementer-claude (claude, model claude-opus-5-5), session c192672d-1610-4f51-acc0-b722d1d2a4f0, max-turns 500, Done — 2026-10-29T16:54:36Z
- Review oracle round 1: Claude Fable 5.1 (`ganda task work`, headless, effort 3, roster general, turn cap 200); `general` reviewer sub-agent Claude Sonnet — 2026-10-30. Outcome: Blocked, fix pass needed for M1-M4 (`review/round-1/merged.md`).
- Implement fix-2 (round 1 findings M1-M4): ganda task work implementer, Claude Opus 5.5 (2026-10-30)
- Review oracle: review by reviewer-claude-fable (claude, model claude-fable-5-1), session 199d19aa-d5a5-471f-992b-50a05e2ab744, max-turns 200 — 2026-10-29T17:05:47Z

## Results

- `components/MessageBar.razor` is the one message bar. It wraps `FluentMessageBar` and paints
  the icon-only Copy button first in the actions slot, then the caller's `ActionsTemplate`.
  `Title` and `Body` are the copied text; `ChildContent` (the passkey nickname input) is not
  copied. It carries the Ask Tag mark that used to sit on the shell rows.
- `MessageBars.razor` paints each row as `<MessageBar NotificationId=…>`, so shell rows still
  dispatch `CopyMessage(id)` (task 313, unchanged behavior).
- New `NotificationState.CopyMessageBar(barId, title, body)` for page-local bars. It shares
  `WriteAndConfirmAsync` with `CopyMessage`: same `ClipboardText`, same
  `CopiedConfirmationInterval`, same "Couldn't copy the message." Warning, no throw.
  `ClipboardText(title, body)` is the shared overload (a blank body is no body). Body is a
  plain string because the generated dispatch method drops nullability. The action carries
  `[CommandPaletteOptOut]`.
- `RemoveExpired` now clears the "Copied" confirmation only when it ran out or the copied row
  itself expired. Before, any expiry pass cleared an id that was not a shell row, which would
  cut a page-local bar's confirmation short.
- Call sites converted: Authentication "Configuration drift" (`data-qa="ConfigurationPolicyDrift"`
  now sits on the bar), Microsoft 365 "Session expired", both `AddPasskeyPrompt` bars (Save /
  Skip and Add a passkey / Later stay), and the four `MessageBarsSample` bars. The
  design-system "Message bars" snippet in `DesignSystemParts` shows the `MessageBar` markup.
- Analyzer **TWA0037** (`DirectFluentMessageBarAnalyzer`): raw `FluentMessageBar` outside
  `components/MessageBar.razor` and `components/MessageBars.razor` fails the build. The host
  exemption is by type: `MessageBar` / `MessageBars` in `{RootNamespace}.Components`
  (`build_property.RootNamespace`), not a path suffix, so a same-named component in a feature
  `components/` folder is still reported (review M1). WASM-only, so analyzer test stubs are
  exempt; no opt-out attribute. Recorded in `AnalyzerReleases.Unshipped.md`. TWA0036 is left
  free; the reserved-ids comment reserves it for the ISO date rendering rule.
- **TWA0025** now also checks the shared `MessageBar` (type named `MessageBar` in a namespace
  whose last segment is `Components`). Without this, converting bars to the control would hide
  Error/Success page bars from the rule. `MessageBarsSample` keeps its `[PageLocalMessageBar]`
  opt-out for that reason.
- Docs: `skills/tw-blazor` "Operation outcomes" names the shared `MessageBar` and TWA0037;
  `components/overview.md` matches.
- Tests: the task 313 doubles moved to `notification-copy-test-doubles.cs` (ClickRenderer gained
  parameters, per-index clicks, and diff-set aria-labels). New `CopyMessageBar_Should_` (7 tests):
  format, write + confirm + expiry, confirmation survives an earlier expiry pass, rejection
  Warning, timeout Warning, control Copy dispatches and swaps to "Copied" with ChildContent not
  copied and the bar's own action still working, and each of the four design-system sample
  buttons copies its own bar. Analyzer tests: `Should_Require_The_Shared_MessageBar` (13) and two
  new TWA0025 cases (shared MessageBar Error flags; same-named type elsewhere does not).
- The passkey, Authentication, and Microsoft 365 bars are not rendered in a test: their state
  setup needs the closed-box Aspire host, which workers do not start. They get the button from
  the same control, and TWA0037 stops them from going back to a raw bar.
- Gates run in this worktree: `dev build` 0 warnings / 0 errors; `CopyMessage_Should_` +
  `CopyMessageBar_Should_` 14/14; `CommandPalette_Should_` 45/45; `DesignSystemPage_Should_` 6/6;
  analyzer message-bar classes 28/28; `dev lint-skills` pass; `dev template-smoke` (default
  variant) succeeded; `dev check-version` source 2.0.0-beta.29 ahead of NuGet beta.28;
  `ganda repo audit` passes with one advisory (`kanban-landed`: 8 other tasks' claims have no
  local worktree; not this task's claim, nothing to fix here). A temporary raw bar on
  `ChooseMicrosoft365Page.razor` produced `error TWA0037` in the real build (reverted).

### Review (tw-implementation-review)

- Round 1 (2026-10-30), effort 3, roster `general`: review oracle re-ran every gate in this
  worktree and all pass (`dev build` 0/0, Copy tests 14/14, analyzer MessageBar 13/13 and
  Message_Bars 15/15, `ganda repo audit` pass with the same unrelated advisory, `dev lint-skills`
  9/9, `dev template-smoke` default variant succeeded). Goal restatement matches the task; no
  misread.
- Round 1 counts: bug 0; suggestion 2 open; nit 2 open. M1 TWA0037 host exemption is a suffix
  match that exempts any `…/components/MessageBar.razor` while the Design region says otherwise;
  M2 the rewritten `RemoveExpired` branch has no test that reaches it; M3 the TWA0036 reservation
  note names a kanban task state in a packed file; M4 stale "FluentMessageBar" / "(FluentUI)"
  wording in the design-system samples.
- Disposition: **pending** — fix pass requested on this task id for M1-M4, then `review/round-2/`.

### Fix pass (round 1 findings)

- M1 fixed (option a): TWA0037 exempts by type identity (`{RootNamespace}.Components.MessageBar`
  / `.MessageBars`), and the path is no longer consulted. Design regions in the analyzer and the
  test file say so, and `MessageBar.razor` notes that its `@namespace` carries the exemption.
  New tests: feature `components/` folder with the default namespace flags, feature
  `@namespace` flags, a non-host type in a host-named file flags, renamed root namespace exempts,
  host code-behind `.cs` exempts, no `RootNamespace` means no exemption. Path-form tests were removed
  because paths no longer matter. `Should_Require_The_Shared_MessageBar` 16/16.
- M2 fixed: `Clear_Confirmation_When_The_Copied_Row_Expires_First` (copied Success row expires
  before `CopiedUntil` → row gone, `CopiedMessageId` null) and
  `Keep_Page_Bar_Confirmation_When_An_Unrelated_Row_Expires` (page bar copied, unrelated Success
  row expires → row gone, `CopiedMessageId` still `bar-1`). A backdated internal `AddMessage`
  makes the row expire before `CopiedUntil`. Mutation check: with the `copiedRowExpired` clause
  disabled, the first test fails.
- M3 fixed: `AnalyzerReleases.Unshipped.md` reads "TWA0036 is reserved for the ISO date rendering
  rule."
- M4 fixed: `NotificationsSample.razor` says the handler records a NotificationState row. The
  sample card and part title are now "Message bars"; no test asserted "(FluentUI)".
- Gates after the fix pass: `dev build` 0 warnings / 0 errors; Copy tests 16/16;
  analyzer `MessageBar` 16/16 and `Message_Bars` 15/15; `DesignSystemPage_Should_` 6/6;
  `ganda repo audit` exit 0 after `ganda kanban reconcile` landed this kitchen (the remaining
  `kanban-landed` warning is 8 other claims with no local worktree); `dev template-smoke`
  (default variant) succeeded.
- Paths: `review/review-framework.md`, `review/round-1/general.md`, `review/round-1/merged.md`.

### How to validate

**Smoke**

1. `dev run`, then raise a shell notification (Design System → Notifications), open the
   Authentication page with config drift, the Microsoft 365 choose page with an expired
   session, both passkey prompts, and Design System → Message bars.
2. On each bar, activate the Copy icon and paste into a text field.

**Expect**

- Pasted text is the title, plus a blank line and the body when the bar has body text. "Name
  this passkey" pastes only its title, never the nickname draft.
- The icon reads "Copied" for about two seconds, then returns to "Copy message".
- Passkey Save, Skip, Add a passkey, and Later still work; shell bars still copy and dismiss.
- With clipboard permission denied, a Warning bar "Couldn't copy the message." appears.

**Automated gate**

```bash
dev build
dotnet run --project tests/container-apps/web/web-spa-integration-tests -- --filter-class Copy
dotnet run --project tests/analyzers/timewarp-architecture-analyzers-tests -- --filter-class MessageBar
dotnet run --project tests/analyzers/timewarp-architecture-analyzers-tests -- --filter-class Message_Bars
```

(`dev test` has no `--filter-class`; the filter is a Jaribu option on the test project.)

## Review

- Round 1: Blocked — open M1, M2, M3, M4

## Notes

Task 313 format and confirmation live in `NotificationState.ClipboardText`, `CopiedConfirmationInterval`, and `MessageBars.razor`. Reuse them.

## How to validate

**Smoke**

1. `dev run`, then raise a shell notification, the Authentication "Configuration drift" bar, the Microsoft 365 "Session expired" bar, both passkey prompts, and Design System → Message bars.
2. On each bar, activate Copy.

**Expect**

- Clipboard text is the title, plus a blank line and the body when the bar has body text.
- The icon reads "Copied", then returns to "Copy message".
- Passkey Save, Skip, Add a passkey, and Later still work.
- Shell bars from task 313 still copy.

**Automated gate**

```bash
dev build
dev test --filter-class CopyMessage_Should_
```

Add the new runfile class name to that filter once it exists.
