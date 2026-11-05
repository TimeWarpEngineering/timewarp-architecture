# Round 2 — general
**Date:** 2026-11-06
**Scope reviewed:** the fix delta since round 1, `82a46d8fd..6c3fa0457` (commit 6c3fa0457, which
changes only `task.md`). Effort 1, roster general.

## Summary

The fix pass reorders the "Ranked patterns" table by hit count and renumbers it. It also fixes
row 12's headline count. The table now runs 68, 38, 31, 25, 24, ~23, 16, ~14, ~12, ~12, ~11, ~11,
~11, ~8, 6, 4, 3, so it is in descending order. The tied rows (9/10 at ~12, and 11/12/13 at ~11)
keep their sweep order, as the new intro sentence says. Row 12 is now `~11`, and its split
(amuru 5, ganda 3, nuru 1, state 2) sums to 11.

I checked every row reference against the new numbering:

- The rule-pack paragraph puts rows 1, 6, 11 and 14 in the families no rule can catch. That
  matches the Detect column. It lists rows 2–5, 7–10, 12, 13 and 15 as rule candidates.
- Row 4's "see row 7" now points at the suppression row.
- Proposals 1 to 10 cite rows 7, 2, 4, 9, 15, 5, 3, 12, 13 and 10. Each one names the right
  pattern.
- How to validate cites rows 3, 7 and 2.

No stale reference to the old numbering is left. The diff is kanban-only:
`git diff --stat master...HEAD -- source tests` prints nothing. I re-ran the first three smoke
greps, and each returned what Results says: taratibu 013 line 98, the arch 210 `<Pending>`
justification, and the ganda 552-015 review files.

The restatement still matches the original task, so there is no misread to record.

## Issues

None.

## Prior findings

- M1 (bug, table not in hit-count order): **fixed**. Verified as above.
- M2 (nit, row 12 headline vs split): **fixed**. Verified as above.
