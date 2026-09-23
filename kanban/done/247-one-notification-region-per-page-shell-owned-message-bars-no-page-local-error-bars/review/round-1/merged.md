# Round 1 — merged findings
**Date:** 2026-09-23
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 0 | 1 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: suggestion — Status: wontfix
- File: source/container-apps/web/projects/web-spa/features/notification/notification-state/notification-state.cs:79 (AddMessage)
- Description: When `AddMessage` finds an existing entry with the same (Intent, Title, Body), it
  refreshes `AutoDismissAt` in place but does not move the entry to the end of `MessageBarList`.
  `VisibleMessages` shows the newest `MaxVisible` (3) entries by list position, so if a message is
  already hidden behind "+N more" (3+ newer distinct messages arrived since), a later recurrence of
  that same failure/success silently refreshes without re-surfacing into the visible set.
- Suggestion: Move the deduped entry to the end of the list on recurrence, or document in the
  Design region that dedupe intentionally freezes position.
- Source: general
- Disposition notes: Accepted as a documented exception. This is an edge case that only bites once
  the 3-item visible cap is already exceeded (3+ distinct messages in flight simultaneously), and
  resolving it either way (freeze position vs. bump to end) is a UX/product call, not a mechanical
  bug fix — moving the entry on recurrence would need its own confirmation from Steve on intended
  behavior, same as the original design rules did. Filing here rather than blocking disposition;
  worth a follow-up task if it's observed in practice. Decided by: review oracle (task 247, round 1).

## Duplicates / conflicts

None — single reviewer, single finding.
