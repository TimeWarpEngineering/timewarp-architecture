# Round 1 — general
**Date:** 2026-11-06
**Scope reviewed:** branch `task/481-evidence-pass-for-agent-anti-slop-twa-rules` vs `master`
(kanban-only: `task.md` moved to-do → in-progress, `## Results` written; 235 lines).

## Summary

Research-only task. The deliverable is the `## Results` evidence table in `task.md`; no product
`source/` or `tests/` changed (`git diff --stat master...HEAD -- source tests` is empty). The
corpus, method limits, covered-rule column, detectability column, watch list, and unfiled
follow-on proposals all match the acceptance criteria in `## Goal (implementer restatement)`,
and the restatement matches the original task (no misread). I re-ran the "How to validate"
smoke: all four greps return what Results says (taratibu 013 line 98; arch 210 `<Pending>`;
ganda 552-015 review files; 992 arch `merged.md` log entries). I spot-checked ten more
citations in sibling repos (ganda 282, 493; nuru 556; taratibu 058; mediator 021-003; arch 393,
435; terminal 023; amuru 099) and the rule-coverage claims (IDE0060 silent at
`.editorconfig:96-97` per task 172; no RCS0056 / Roslynator line-length config;
`Roslynator.Formatting.Analyzers` referenced in `Directory.Build.props:105`; TWA0039–0043 are
`ContractAgreementAnalyzer` contract/aggregate rules). All held. The one real defect is that the
"ranked" table is not in hit-count order, which the acceptance criteria require.

## Issues

### Issue 1 — Severity: bug
- File: kanban/in-progress/481-evidence-pass-for-agent-anti-slop-twa-rules/task.md:101
- Description: Acceptance criterion "The list is ranked by hit count" is not met. The table
  order breaks hit-count order in five places: row 7 (~23) sits below row 6 (16); row 10 (~11)
  sits above row 11 (~12); row 12 (~10) sits above row 13 (~11); row 14 (6) sits above row 15
  (~8); row 16 (3) sits above row 17 (4). Readers (and the follow-on task picker) take the `#`
  column as priority, so the misorder misstates which families are most frequent. The prose
  ("rows 1, 7, 10 and 15", "rows 2–6, 8, 9 and 11–14") and the proposals cite row numbers, so a
  reorder must update those references too.
- Suggestion: Reorder the table strictly by hit count (ties: keep current order or break by
  repo spread), renumber, and update every row-number reference in "What this means…" and in
  "Proposed follow-on implement tasks". Alternatively, if a different ranking key was intended,
  state that key explicitly — but the acceptance criterion says hit count.
- Status: open

### Issue 2 — Severity: nit
- File: kanban/in-progress/481-evidence-pass-for-agent-anti-slop-twa-rules/task.md:119
- Description: Row 12 headline says `~10` hits but its repo split (amuru 5, ganda 3, nuru 1,
  state 2) sums to 11. Every other row's split sums to its headline.
- Suggestion: Make the headline match the split (or fix the split), and place the row
  accordingly when Issue 1 is fixed.
- Status: open
