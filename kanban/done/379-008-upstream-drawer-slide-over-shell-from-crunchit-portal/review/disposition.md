# Disposition — task 379-008

**Date:** 2026-10-23
**Outcome:** accepted-exceptions
**Rounds:** 3
**Final open count:** 0

## Summary

Round 1 (effort 3) raised M1–M4. Round 2 (effort 2) confirmed the fixes for M1 and M2 and
raised M5 and M6: a module close still called `OnClose` once the queued `close` event fired,
and the browser check could not see it. Round 3 (effort 1) re-verified the `pendingModuleClose`
fix against the compiled module in Chromium and raised nothing new. Five findings are fixed and
one nit is wontfix.

## Exception log (if accepted-exceptions)

| ID | Severity | Rationale | Decided by |
|----|----------|-----------|------------|
| M4 | nit | `Show` or `Close` interop on every render keeps the dialog in sync with `IsOpen`, and both calls return at once when the dialog is already in that state. | review oracle (round 1) |

## Escalations

- None.
