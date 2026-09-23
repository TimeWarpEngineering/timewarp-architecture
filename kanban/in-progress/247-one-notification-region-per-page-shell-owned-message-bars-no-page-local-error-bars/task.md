# One notification region per page: shell-owned message bars, no page-local error bars

## Description

Operation errors currently render in two places at once and with inconsistent spacing. On
Settings, "Already on this account: This passkey is already on this account." appears as a bar
under the breadcrumb (the shell's `MessageBars` fed by `ProblemDetailsNotificationHandler`) AND as a
second bar at the bottom of the page (`SettingsPage.razor:170-179` renders its own
`FluentMessageBar` for the same failure). Other pages do the same ad hoc (`AddPasskeyPrompt.razor:64`,
`admin/site-settings/pages/AuthenticationPage.razor:99,138`). The bottom copy prefixes a generic
"Error" title and concatenates problem Title + Detail into one sentence.

Task 058-001 already moved the template off toasts onto `FluentMessageBar` and gave the shell a
single host (`components/MessageBars.razor`, painted at the top of `TimeWarpPage` and
`TimeWarpFocusedPage`). This task finishes that design: one region, one owner, one shape.

## Design (confirmed by Steve, 2026-09-23)

1. **One region.** Every page has exactly one notification region: the shell's `MessageBars`,
   directly below the page header/breadcrumb and above the first card. Pages, cards, and feature
   components never render a `FluentMessageBar` for an operation outcome (success or failure).
   They report outcomes to the notification state and the shell paints them. Static, contextual
   guidance inside a component (e.g. AddPasskeyPrompt's "you need a passkey" info/warning) is
   not an outcome and stays inline — that is the one allowed page-local use.
2. **One shape.** A bar has `Intent`, `Title`, optional `Body`. For a `SharedProblemDetails`:
   Title = problem `Title`, Body = `Detail`; never "Error" as a title and never `Title: Detail`
   glued into one string. If Detail repeats Title, show it once. Success bars carry the
   operation's own sentence ("Passkey added.") with no "Success" prefix.
3. **Dedupe.** The state keys messages by (Intent, Title, Body). Pushing an identical message
   replaces the existing one instead of stacking, so a pipeline handler and a page reporting the
   same failure yield one bar. Cap the visible stack at 3 with a "+N more" affordance.
4. **Lifetime.** Errors stay until dismissed or the route changes (clear on navigation via the
   existing route state so a stale failure does not follow the user to the next page). Success
   bars auto-dismiss after a short interval and are also dismissible.
5. **Spacing.** The region owns its layout: a `tokens.css`-driven top/bottom margin and a fixed
   gap between bars, so bars never touch the breadcrumb, the first card, or each other. No
   per-page margins.
6. **Field validation is a different class.** Blazilla/field-level messages stay next to the
   field; they never go to the region.
7. **Naming.** `ToastNotificationState` no longer describes what it is. Rename to
   `NotificationState` (or `MessageBarState`) together with its ActionSets; the file/folder move
   follows the SPA convention.
8. **Enforcement, not memory.** Add a convention analyzer (next free TWA id) that flags
   `FluentMessageBar` in web-spa `.razor` outside `components/MessageBars.razor` when
   `Intent` is `Error` or `Success`; `Info`/`Warning` with static content is allowed (rule 1).
   Opt-out attribute or comment marker with a reason, consistent with the other TWA opt-outs.

## Requirements

- Remove the page-local outcome bars named above; route those outcomes through the state.
- Implement rules 2–5 in the state + `MessageBars.razor`; reconcile Design regions (the current
  one says "Section and Card have no error slot", keep that reasoning, add the single-owner rule).
- Rule 7 rename; rule 8 analyzer with tests (positive, negative, opt-out) in the analyzer suite.
- Update `tw-blazor-layout` (shell owns the region) and `tw-blazor` (no page-local outcome bars)
  skills in one short section each.
- Tests: SPA integration — same problem reported twice renders once; navigation clears errors;
  success auto-dismisses; problem Title/Detail land in Title/Body. Existing 058-001 message-bar
  tests keep passing.
- Gates: `dev build` 0/0 (analyzer change ⇒ full rebuild), `dev test`, manual check of Settings and
  Passkeys pages in `dev run` recorded in Results.

## Checklist

- [x] Design confirmed by Steve 2026-09-23, as written (rules 1–8, including the analyzer)
- [x] Page-local outcome bars removed (SettingsPage, AddPasskeyPrompt error, AuthenticationPage save error; also PasskeysPage, LoginPage, ChooseMicrosoft365Page)
- [x] Title/Body shape, dedupe, cap, lifetime, spacing tokens
- [x] `ToastNotificationState` renamed → `NotificationState` (`features/notification/notification-state/`)
- [x] TWA analyzer + tests; AGENTS.md diagnostics table row
- [x] Skills updated (`tw-blazor-layout`, `tw-blazor`)
- [x] SPA tests; `dev build` 0/0; `dev test`; manual page check (build/test/template-smoke green; manual `dev run` check not performed — see Results)

## Notes

- Origin: screenshot review 2026-09-23 — duplicate "Already on this account" bars top and bottom
  of Settings; earlier screenshot showed a third variant with a Dismiss button on Passkeys.
- Related: 058-001 (toasts → message bars, shell `MessageBars` host), 246 (disable Revoke on the
  last credential — its inline hint is rule 1's allowed static guidance, not an outcome bar).
- Cockpit session: https://claude.ai/code/session_01QYpqCSgnvvLRpXrMKxu5ED

## Session

- Created: https://claude.ai/code/session_01QYpqCSgnvvLRpXrMKxu5ED (2026-09-23)
- Implement (ganda task work, headless Claude implementer): 2026-09-23

## Resume note (2026-09-23, cockpit)

The previous headless pass left ~56 files of implementation UNCOMMITTED and the walk hung with no
oracle process. Pick up from the working tree: review the diff against the confirmed design,
finish anything missing, run gates IN THE FOREGROUND (`dev build` 0/0, `dev test`,
`dev template-smoke`), commit, finish the walk. Do NOT leave an AppHost running when done
(`aspire stop`); it shares the user-secrets id with origin-home and a live instance there
rewrote the postgres password. If you need `dev run` for a manual check, stop it before exiting.
Master has since merged 246, 248-001 and 248-002 (CredentialList Revoke naming, nickname row,
last-used); `git merge origin/master` before the gates and keep both sides.

## Results

### What landed (per design rule)

- **Rule 1 (one region).** `SettingsPage.razor`, `AddPasskeyPrompt.razor`, `AuthenticationPage.razor`,
  `PasskeysPage.razor`, `LoginPage.razor`, and `ChooseMicrosoft365Page.razor` no longer render their
  own outcome `FluentMessageBar`; operation outcomes report through `NotificationState` and the
  shell's `components/MessageBars.razor` paints the single region below the header/breadcrumb.
  Static contextual guidance (e.g. AddPasskeyPrompt's "you need a passkey" info) stays inline —
  the one allowed page-local use.
- **Rule 2 (one shape).** `NotificationState` messages carry `Intent` / `Title` / optional `Body`.
  `notification-state.problem-details-notification-handler.cs` maps `SharedProblemDetails.Title` →
  `Title`, `Detail` → `Body`, dropping `Body` when `Detail` repeats `Title` (no generic "Error"
  title, no glued `Title: Detail` string). Success messages carry the operation's own sentence with
  no "Success" prefix.
- **Rule 3 (dedupe/cap).** `notification-state.cs` keys messages by `(Intent, Title, Body)`;
  pushing an identical message replaces the existing entry. `MessageBars.razor` caps the visible
  stack at 3 with a "+N more" affordance.
- **Rule 4 (lifetime).** `notification-state.navigation-listener.cs` clears error messages on route
  change (existing route state); `notification-state.expire-messages.cs` auto-dismisses success
  messages after a short interval. All messages are also manually dismissible
  (`notification-state.dismiss-message.cs`).
- **Rule 5 (spacing).** `MessageBars.razor` / its CSS own top/bottom margin and inter-bar gap via
  `tokens.css` custom properties; no per-page margins remain around the region.
- **Rule 6 (field validation untouched).** Blazilla field-level messages were not touched — they
  stay next to the field, not routed through `NotificationState`.
- **Rule 7 (rename).** `ToastNotificationState` → `NotificationState`, moved to
  `source/container-apps/web/projects/web-spa/features/notification/notification-state/` with its
  ActionSets split by concern (`add-notification`, `clear-on-navigation`, `dismiss-message`,
  `exception-notification-handler`, `expire-messages`, `outcome-notification-handler`,
  `problem-details-notification-handler`, `report-problem`).
- **Rule 8 (enforcement).** New analyzer **TWA0025** (`PageLocalMessageBarAnalyzer`,
  `source/analyzers/timewarp-architecture-convention-analyzers/page-local-message-bar-analyzer.cs`)
  flags a `FluentMessageBar` with `Intent` `Error`/`Success` outside
  `components/MessageBars.razor` in web-spa razor/`@code`; `Info`/`Warning` and unresolvable
  (e.g. ternary) intents are silently allowed. Opt-out: `[PageLocalMessageBar(reason)]`
  (`source/analyzers/timewarp-architecture-attributes/page-local-message-bar-attribute.cs`),
  non-empty reason required. AGENTS.md diagnostics table and
  `AnalyzerReleases.Unshipped.md` both carry the TWA0025 row.

### Tests

- Analyzer suite: `tests/analyzers/timewarp-architecture-analyzers-tests/page-local-message-bar-analyzer-tests.cs`
  — positive (Error/Success outside host), negative (Warning/Info, MessageBars host itself,
  non-Blazor project, generated-code exemption, unresolvable/dynamic intent), opt-out
  (`[PageLocalMessageBar(reason)]`, empty-reason still fires).
- SPA integration: `tests/container-apps/web/web-spa-integration-tests/features/notification/notification-state-tests.cs`
  — `Put_Problem_Title_And_Detail_In_Title_And_Body`, `Drop_Body_When_Detail_Repeats_Title`,
  `Render_The_Same_Problem_Once_When_Reported_By_Page_And_Handler` (dedupe),
  `Keep_Distinct_Messages_And_Cap_The_Visible_Stack`, `Clear_Errors_When_The_Route_Changes`,
  `Auto_Dismiss_Success_But_Keep_Errors`. Existing 058-001 message-bar tests keep passing
  (same suite).
- Skills updated: `tw-blazor-layout` (shell owns the single notification region) and `tw-blazor`
  (no page-local outcome bars) each carry one short section on the rule.

### Gates (run 2026-09-23 in the claim worktree)

- `./bin/dev build` — 0 warnings / 0 errors (full rebuild, analyzer change).
- `./bin/dev test` — every suite green: `timewarp-architecture-analyzers-tests` 171/171 (includes
  the new TWA0025 tests), `web-spa-integration-tests` 63/63 (includes the six new
  `notification-state-tests.cs` cases), `web-jaribu-tests` 199/199, `web-server-integration-tests`
  253/254 (1 intentionally-skipped `RunForever`), remaining suites all green; exit 0 overall.
- `./bin/dev template-smoke` — SmokeNoApi generated app builds 0/0, package-mode/skills checks
  pass, co-located and MTP aggregator tests pass in the generated app.
- Manual `dev run` check of Settings/Passkeys pages: **not performed this pass** — deferred per the
   host's no-AppHost-in-headless-pass instruction (a live AppHost on this box shares the
  user-secrets id with origin-home and a prior session's instance there rewrote the postgres
  password). The SPA integration tests above exercise the same duplicate-bar and dismiss/dedupe
  scenarios the manual check would cover; a human/interactive session should still eyeball
  Settings and Passkeys once.

### How to validate

**Smoke**

```bash
./bin/dev build
# expect: Build succeeded. 0 Warning(s) 0 Error(s)

./bin/dev test
# expect: every suite "Test run summary: Passed!"; web-spa-integration-tests and
#   timewarp-architecture-analyzers-tests both green; exit code 0 overall
```

**Expect**

- No `.razor` file under `source/container-apps/web/projects/web-spa` outside
  `components/MessageBars.razor` renders a `FluentMessageBar` with `Intent="MessageBarIntent.Error"`
  or `Intent="MessageBarIntent.Success"` without a `[PageLocalMessageBar(reason)]` opt-out —
  `grep -rn "MessageBarIntent.Error\|MessageBarIntent.Success" source/container-apps/web/projects/web-spa --include=*.razor`
  should show only `MessageBars.razor` (plus any explicitly opted-out component).
- Reporting the same `SharedProblemDetails` failure from both a pipeline handler and a page
  produces exactly one visible bar (see `Render_The_Same_Problem_Once_When_Reported_By_Page_And_Handler`).
- Navigating away from a page with a visible error bar clears it (see
  `Clear_Errors_When_The_Route_Changes`).

## Resume note 2 (2026-09-23, cockpit)

Pass 2 (Sonnet) committed the implementation (`af367b77`) and the master merge (`16907c74`), then hit
the 80-turn limit before gates/Results, and again left an AppHost running (stopped by cockpit).
Remaining work only: run gates in the FOREGROUND (`dev build` 0/0, `dev test`,
`dev template-smoke`), fix anything they surface, write `## Results` (what landed per design rule
1–8, the new TWA id, skills touched, gate output), commit, push. Do NOT start `dev run`/an AppHost
in this pass — record the manual page check as not performed instead.
