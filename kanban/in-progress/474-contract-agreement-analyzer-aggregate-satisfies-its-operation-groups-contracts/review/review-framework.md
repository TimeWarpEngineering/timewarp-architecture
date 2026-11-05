# Review framework

## Budget (by-diff)

- Lines changed: 1841
- Effort: 3
- TCB hits: none
- Roster axes: general
- Turn cap: 200 (--max-turns; cursor uncapped)

## Scope

**Date:** 2026-11-05
**Host task:** kanban/in-progress/474-contract-agreement-analyzer-aggregate-satisfies-its-operation-groups-contracts/
**Diff scope:** branch `task/474-contract-agreement-analyzer-aggregate-satisfies-it` vs `master` (merge base), commit 8054eff67 plus kanban landings
**Plan / brief:** `task.md` Requirements and `## Goal (implementer restatement)`: convention analyzer
TWA0039–TWA0043 (contract agreement between an aggregate root and its `[Operation]` group), the
`[NotAggregate]` / `[Computed]` attributes, Profile and Roles contract renames, tests, skills.
**Effort:** 3 (by-diff budget), roster axis general
**Reviewer roster:** general
**Session IDs:** review oracle, ganda task work (Claude Opus 5.5), 2026-11-05

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
