# Disposition — task 134

**Date:** 2026-07-29
**Outcome:** accepted-exceptions
**Rounds:** 1
**Final open count:** 0

## Summary

Single-reviewer round confirmed all four implementation claims independently (solution build
0/0 with co-located `-tests.cs` present; contracts runfile 5/5; integration runfile 2/2 on a
real host at :7255; aggregator 7/7 via `dotnet test`) and surfaced three findings, all
dispositioned wontfix-on-spike-branch: two verified bugs (template engine strips
`#if !JARIBU_MULTI` breaking generated apps; `dev test`'s per-project invocation fails on MTP
projects) and one suggestion (exclude-glob validation blind spot). All three are captured in
`findings.md` as confirmed adoption blockers / decision evidence for the follow-up tasks —
which is exactly the spike's purpose. The spike branch never merges, so none of the three
defects can reach dev or generated apps in this state.

## Exception log

| ID | Severity | Rationale | Decided by |
|----|----------|-----------|------------|
| M1 | bug | Spike-branch-only; template-safety fix is the follow-up adoption task's core design work; recorded as confirmed blocker in findings.md | orchestrator |
| M2 | bug | Spike-branch-only; `dev test` change explicitly out of spike scope per task.md; recorded as confirmed blocker in findings.md | orchestrator |
| M3 | suggestion | Carve-out already marked non-permanent inline; tradeoff recorded in findings.md as evidence for the strategic mechanism decision | orchestrator |

## Escalations

- None during review. The three strategic post-spike questions (carve-out mechanism, `dev test`
  discovery shape, Aspire tier) are presented to the human in Results — they are follow-up
  decisions, not review stalemates.
