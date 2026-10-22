# Disposition — task 379-004

**Date:** 2026-10-22
**Outcome:** accepted-exceptions
**Rounds:** 2
**Final open count:** 0

## Summary

Round 1 (general) raised M1–M3: a double fetch on Escape plus search-clear, missing
FilterBar-level clear/Escape tests, and reflection in the tests. Fix commit a78c089c9 resolved
all three; round 2 verified them on the fix delta and the FilterBar render tests passed 14/14.
Round 2 raised one nit (M4), accepted as wontfix.

## Exception log (if accepted-exceptions)

| ID | Severity | Rationale | Decided by |
|----|----------|-----------|------------|
| M4 | nit | FilterBar publishes the raw box text by contract; trimming is the consuming state's rule. One redundant fetch per pause on a trailing-space change, correct result. | review oracle (round 2) |

## Escalations

- None.
