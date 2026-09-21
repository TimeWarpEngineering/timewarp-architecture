# Disposition — task 242

**Date:** 2026-09-21
**Outcome:** accepted-exceptions
**Rounds:** 2
**Final open count:** 0

## Summary

Effort-1 general review of branch `task/242-bring-master-back-to-a-clean-ganda-repo-audit-beta` vs `origin/master`. Round 1 raised two suggestions: fail-closed GitHub Packages install for the new `repo-audit` job (M1, fixed) and `kanban/**` missing from workflow path filters (M2, wontfix). Round 2 re-verified the M1 workflow delta and agreed M2 stays out. No bugs. Disposition is accepted-exceptions because of M2.

## Exception log (if accepted-exceptions)

| ID | Severity | Rationale | Decided by |
|----|----------|-----------|------------|
| M2 | suggestion | Task CI guard is for new co-located `*-tests.cs` under `source/**` (already in path filters). Adding `kanban/**` would start this workflow on every kanban-only PR. Kanban research runfiles remain a local `ganda repo audit` / tw-pr concern. | orchestrator (round 1); general re-agreed round 2 |

## Escalations

- None.
