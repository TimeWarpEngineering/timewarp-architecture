# Disposition — task 247

**Date:** 2026-09-23
**Outcome:** accepted-exceptions
**Rounds:** 1
**Final open count:** 0

## Summary

Single general reviewer (effort 1) reviewed the diff (`task/247-one-notification-region-per-page-shell-owned-messa`
vs `origin/master`) against design rules 1–8 in `task.md`, rebuilding the two directly-affected
projects and re-running the affected test suites independently (`timewarp-architecture-analyzers-tests`
171/171, `web-spa-integration-tests` 63/63) rather than trusting the implementer's reported gate
results. No bugs found: page-local outcome bars are fully removed, the `NotificationState` rename
is clean with no stale references, dedupe/cap/lifetime/spacing behave as specified, and the new
TWA0025 analyzer's opt-out correctly requires a non-empty reason. One low-priority suggestion (M1)
was raised and accepted as a documented exception rather than fixed in this pass.

## Exception log (if accepted-exceptions)

| ID | Severity | Rationale | Decided by |
|----|----------|-----------|------------|
| M1 | suggestion | Dedupe on `AddMessage` refreshes an existing entry's expiry in place without moving it to the end of the list, so a recurrence of an already-hidden message (behind "+N more") doesn't re-surface. Only reachable once 3+ distinct messages are already in flight. Resolving it requires a UX/product decision (freeze position vs. treat recurrence as new) rather than a mechanical fix, same category as the original design rules that needed Steve's confirmation. | review oracle |

## Escalations

None.
