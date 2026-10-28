# Review framework

## Budget (by-diff)

- Lines changed: 1819
- Effort: 3
- TCB hits: none
- Roster axes: general
- Turn cap: 200 (--max-turns; cursor uncapped)

## Framework — task 427

**Date:** 2026-10-28
**Host task:** kanban/in-progress/427-add-the-remaining-portal-design-system-primitives/
**Diff scope:** branch `task/427-add-the-remaining-portal-design-system-primitives` vs `master` (merge base), commit e3c76a611 (product) and 247c4282a (kitchen)
**Plan / brief:** task.md `## Goal (implementer restatement)` and `## Requirements`: DefinitionList/DefinitionItem, LineChart, PeriodStepper, compact IntegrationCard warning tone, PageSplit, `twe-numeric`
**Effort:** 3 (by-diff budget); roster axes: general
**Reviewer roster:** general (review oracle, claude-opus-5-5)
**Session IDs:** review oracle (ganda task work, headless print mode)

### Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit. Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
