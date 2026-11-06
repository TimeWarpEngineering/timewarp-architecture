# Round 1 — merged findings
**Date:** 2026-11-06
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 1 | 0 |
| suggestion | 0 | 0 | 0 |
| nit | 0 | 1 | 0 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: kanban/in-progress/481-evidence-pass-for-agent-anti-slop-twa-rules/task.md:101
- Description: The "Ranked patterns" table is not in hit-count order, which the acceptance
  criterion "ranked by hit count" requires. Out of order: row 7 (~23) below row 6 (16); row 10
  (~11) above row 11 (~12); row 12 (~10) above row 13 (~11); row 14 (6) above row 15 (~8); row
  16 (3) above row 17 (4).
- Suggestion: Reorder strictly by hit count and renumber; update row-number references in
  "What this means for an anti-slop rule pack" and in each proposed follow-on task.
- Source: general
- Disposition notes: fixed in implement-fix-2. Rows now run strictly by hit count (old 6↔7,
  10↔11, 14↔15, 16↔17 swapped; ties keep sweep order), and every row reference in the rule-pack
  paragraph, the proposals, and How to validate was renumbered.

### M2 — Severity: nit — Status: fixed
- File: kanban/in-progress/481-evidence-pass-for-agent-anti-slop-twa-rules/task.md:119
- Description: Row 12 headline `~10` disagrees with its repo split, which sums to 11.
- Suggestion: Make headline and split agree; place the row by the corrected count.
- Source: general
- Disposition notes: fixed in implement-fix-2. Headline is now `~11` to match the split; the
  row sits with the other ~11 rows at #12.

## Duplicates / conflicts

- None (single reviewer).
