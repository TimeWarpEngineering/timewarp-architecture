# Review framework

## Budget (by-diff)

- Lines changed: 473
- Effort: 2
- TCB hits: none
- Roster axes: general
- Turn cap: 120 (--max-turns; cursor uncapped)

## Scope — task 472

**Date:** 2026-11-05
**Host task:** kanban/in-progress/472-fix-empty-openapi-summary-for-contracts-whose-xml-summary-sits-on-the-outer-class/
**Diff scope:** branch `task/472-fix-empty-openapi-summary-for-contracts-whose-xml` (HEAD 10d6eca60) vs merge-base with master (40b6e4350)
**Plan / brief:** task.md Requirements and `## Goal (implementer restatement)`: summary order is request XML summary, then `[Operation(Description)]`, then outer static class XML summary, then empty
**Effort:** 2 (budget by diff)
**Reviewer roster:** general
**Session IDs:** review oracle (claude, claude-opus-5-5), headless print mode

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit. Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
