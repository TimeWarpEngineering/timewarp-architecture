# Round 1 — merged findings
**Date:** 2026-10-30
**Sources:** general (sub-agent report in `general.md`), review oracle verification (gates re-run, claims re-checked against the tree)

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 2 | 0 | 0 |
| nit | 2 | 0 | 0 |

No bug was raised. Every requirement bullet in task.md `## Requirements` and all four call sites are
met in code; all gates pass (see `review-framework.md`). The four findings below are open and go to
one fix pass on this task id.

## Issues

### M1 — Severity: suggestion — Status: open
- File: source/analyzers/timewarp-architecture-convention-analyzers/direct-fluent-message-bar-analyzer.cs:17-22 (Design region) and :144-170 (`IsHostPath` / `EndsWithSegment`); tests/analyzers/timewarp-architecture-analyzers-tests/direct-fluent-message-bar-analyzer-tests.cs:184-190
- Description: The host exemption is a suffix match. `EndsWithSegment(normalized, "components/MessageBar.razor", '/')` and the flattened `_` form accept any path ending in `components/MessageBar.razor` or `components/MessageBars.razor`, so a feature-folder file such as `features/identity/components/MessageBar.razor` (that folder already exists for `AddPasskeyPrompt.razor`) can render a raw `FluentMessageBar` with no TWA0037. The Design region says "a same-named file in another folder is still reported", which is only true when that folder is not itself named `components`. The only "other folder" test uses `features/notes/` (no `components` segment), so the hole is untested. TWA0032 has the same suffix shape, so this is inherited, but the comment for TWA0037 overstates what the rule does. Verified by the review oracle against the file.
- Suggestion: Pick one and make the comment and tests agree with the code. (a) Tighten: require the generated type's containing namespace to be the app `Components` namespace (last segment `Components`, same test TWA0025 now uses) and the type name to be `MessageBar` / `MessageBars`, in addition to or instead of the path suffix. (b) Keep the suffix match, reword the Design region (analyzer and test file) to "a same-named file outside a `components/` folder is still reported", and add a `features/x/components/MessageBar_razor.g.cs` test that records the intended behavior.
- Source: general
- Disposition notes:

### M2 — Severity: suggestion — Status: open
- File: source/container-apps/web/projects/web-spa/features/notification/notification-state/notification-state.cs:157-166 (`RemoveExpired`); tests/container-apps/web/web-spa-integration-tests/features/notification/message-bar-copy-tests.cs:65-76
- Description: `RemoveExpired` was rewritten so the confirmation clears only when `CopiedUntil` has passed or the copied shell row itself auto-expired (`copiedRowExpired`). The logic is correct for every scenario walked (copied row dismissed → `RemoveMessage`; copied Success row auto-expires → `copiedRowExpired`; page-local bar copied while an unrelated Success row expires → kept; `CopiedUntil` elapses → cleared). But no test reaches the new `copiedRowExpired` branch, and no test exercises "unrelated row expires while a page-local confirmation is showing" with a row actually in the list: `Keep_Confirmation_Through_An_Earlier_Expiry_Pass` runs with an empty list, and the task 313 tests only add Error and Info rows (re-verified by the review oracle: `notification-copy-tests.cs` never copies a Success row). Deleting the `copiedRowExpired` clause would pass every existing test. A behavior change with no test on the changed branch is below this repo's bar ("Tests: copied text, failure Warning …" is a checklist item on this task).
- Suggestion: Add two state tests beside the existing ones: (a) add a Success notification, `CopyMessage` it, send `ExpireMessagesActionSet.Action(row.AutoDismissAt)` with `AutoDismissAt < CopiedUntil`, assert the row is gone and `CopiedMessageId` is null; (b) `CopyMessageBar("bar-1", …)`, add a Success notification, expire at that row's `AutoDismissAt` (before `CopiedUntil`), assert the row is gone and `CopiedMessageId` is still `"bar-1"`.
- Source: general
- Disposition notes:

### M3 — Severity: nit — Status: open
- File: source/analyzers/timewarp-architecture-convention-analyzers/AnalyzerReleases.Unshipped.md:5-6
- Description: The new reservation note reads "TWA0036 is named by to-do task 452's spec (ISO date rendering); leave it for that rule." `source/analyzers/` is packed into generated apps, where `kanban/` does not exist, and "to-do" is a column name that goes stale when task 452 moves. The neighbouring retired/reserved entries name the rule's subject, not a task state.
- Suggestion: Reword to the subject only, e.g. "TWA0036 is reserved for the ISO date rendering rule."
- Source: general
- Disposition notes:

### M4 — Severity: nit — Status: open
- File: source/container-apps/web/projects/web-spa/features/design-system/samples/NotificationsSample.razor:13; source/container-apps/web/projects/web-spa/features/design-system/samples/MessageBarsSample.razor:4 and features/design-system/design-system-parts.cs:438 (part title)
- Description: Stale wording after the conversion. `NotificationsSample.razor:13` says ExceptionNotificationHandler "records a FluentMessageBar"; it records a `NotificationState` row that the shell paints through the shared `MessageBar`. The card and part title "Message bars (FluentUI)" still labels the sample as the FluentUI component although the sample now renders the shared control.
- Suggestion: Required: say "records a message bar" (or "a NotificationState row") in the NotificationsSample comment. Optional: drop "(FluentUI)" from the sample title and the part title only if `DesignSystemPage_Should_` does not assert that string; leave it if the rename ripples.
- Source: general
- Disposition notes:

## Duplicates / conflicts

- Single reviewer; nothing collapsed. The review oracle re-verified each claim (M1 by reading
  `IsHostPath`, M2 by grepping the task 313 tests for Success rows, M3 by confirming
  `source/analyzers` is template content, M4 by reading the two files) before merging.
- The general reviewer's `## Verified` list (requirements 1-8, call sites, timer reach through
  `TimeWarpPage` / `TimeWarpFocusedPage`, `MessageBars` subscription with zero rows, TWA0025
  silence on the control's `Intent="@Intent"`, no `ConfigurationPolicyDrift` / `MessageBarBody`
  selectors elsewhere) matches the review oracle's own reading and is adopted as verified.

## Next

Fix pass on task 445 (same id, no sibling task) for M1-M4, then `round-2/` re-review carrying these
ids with updated status.
