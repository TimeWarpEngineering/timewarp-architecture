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
- [ ] TWA analyzer + tests; AGENTS.md diagnostics table row
- [x] Skills updated (`tw-blazor-layout`, `tw-blazor`)
- [ ] SPA tests; `dev build` 0/0; `dev test`; manual page check

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

## Resume note 2 (2026-09-23, cockpit)

Pass 2 (Sonnet) committed the implementation (`af367b77`) and the master merge (`16907c74`), then hit
the 80-turn limit before gates/Results, and again left an AppHost running (stopped by cockpit).
Remaining work only: run gates in the FOREGROUND (`dev build` 0/0, `dev test`,
`dev template-smoke`), fix anything they surface, write `## Results` (what landed per design rule
1–8, the new TWA id, skills touched, gate output), commit, push. Do NOT start `dev run`/an AppHost
in this pass — record the manual page check as not performed instead.
