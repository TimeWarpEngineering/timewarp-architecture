# Disposition — task 424

**Date:** 2026-10-28
**Outcome:** accepted-exceptions
**Rounds:** 3
**Final open count:** 0

## Summary

Round 1 (effort 3, three general reviewers) raised M1–M19: 3 bugs, 9 suggestions, and 7 nits.
Round 2 (effort 3) confirmed that all 19 were fixed. It raised M20–M25 and marked M21 and M22
wontfix. Round 3 (effort 2, one general reviewer) confirmed that M20, M23, M24, and M25 were fixed,
and raised nothing new. Final totals: bugs 3 fixed; suggestions 11 fixed; nits 9 fixed and 2
wontfix. The ledger is `review/round-3/merged.md`.

## Exception log

| ID | Severity | Rationale | Decided by |
|----|----------|-----------|------------|
| M21 | nit | If another user changes the field A→B→A between two renders, an edited draft adopts the newer version. The final value is the one that user left, so nothing is lost. Detecting the transient change would need a per-field version, which the 418 pattern does not have. | review oracle (round 2) |
| M22 | nit | One 101-character comment line in `NavMenu.razor`. Twelve other lines in that file are already over 100 characters, and no rule or analyzer enforces the width. | review oracle (round 2) |

## Escalations

- None.
