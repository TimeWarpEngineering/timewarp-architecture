# Round 1 — general
**Date:** 2026-09-23
**Scope reviewed:** branch task/247-one-notification-region-per-page-shell-owned-messa vs origin/master

## Summary

The diff removes every page-local outcome `FluentMessageBar` (SettingsPage, AddPasskeyPrompt,
AuthenticationPage, PasskeysPage, LoginPage, ChooseMicrosoft365Page — verified by grep: no
`<FluentMessageBar>` renders outside `components/MessageBars.razor` and the one opted-out
`StyleGuidePage` demo card), renames `ToastNotificationState` → `NotificationState` with a clean
move to `features/notification/notification-state/` (grep found zero stale `ToastNotificationState`
references outside historical `kanban/done/*` docs), and adds a new `TWA0025` Roslyn analyzer with
a `[PageLocalMessageBar(reason)]` opt-out that correctly requires a non-empty reason. I rebuilt the
convention-analyzers project and the web-spa project directly (both 0 warnings/0 errors) and ran
the affected test suites myself rather than trusting the task's Results narrative:
`timewarp-architecture-analyzers-tests` 171/171, `web-spa-integration-tests` 63/63 (including all 6
new `notification-state-tests.cs` cases and the 13 new TWA0025 analyzer tests) — all green, matching
the claimed counts exactly. Dedupe key (Intent, Title, Body), the 3-item visible cap with "+N more",
route-change clearing via `NavigationListener`, and deterministic Success auto-dismiss all check out
against both static reading and the passing tests. Risk is low; I found one minor behavioral
subtlety worth a maintainer's attention, not a functional bug.

## Issues

### Issue 1 — Severity: suggestion
- File: source/container-apps/web/projects/web-spa/features/notification/notification-state/notification-state.cs:79 (AddMessage)
- Description: When `AddMessage` finds an existing entry with the same (Intent, Title, Body), it
  refreshes `AutoDismissAt` in place but does not move the entry to the end of `MessageBarList`.
  `VisibleMessages` shows the newest `MaxVisible` (3) entries by list position, so if a message is
  already hidden behind "+N more" (i.e., 3+ newer distinct messages have arrived since), a later
  recurrence of that same failure/success — e.g. a retried operation hitting the identical error
  again — silently refreshes it without bringing it back into the visible set or otherwise signaling
  to the user that it just recurred. This is a plausible real scenario (the task's own motivating
  example was the same failure being reported from two call sites) once more than 3 distinct
  messages are in flight.
- Suggestion: Consider moving the deduped entry to the end of the list (treating a recurrence as
  "new" for cap/visibility purposes) if that better matches the intended UX, or explicitly document
  in the Design region that dedupe intentionally freezes position so a future maintainer does not
  read it as a bug. Low priority — only bites once the 3-item cap is already exceeded.
- Status: open

None found beyond the above for rules 1–8 correctness, the rename, or the analyzer's
true/false-positive behavior — static review plus re-running the affected suites did not surface
functional bugs.
