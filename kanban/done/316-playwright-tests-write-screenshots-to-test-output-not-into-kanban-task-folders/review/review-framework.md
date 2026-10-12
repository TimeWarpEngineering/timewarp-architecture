# Review framework

## Budget (by-diff)

- Lines changed: 242
- Effort: 2
- TCB hits: none
- Roster axes: general
- Turn cap: 120 (--max-turns; cursor uncapped)

# Review framework — task 316

**Date:** 2026-10-12
**Host task:** kanban/to-do/316-playwright-tests-write-screenshots-to-test-output-not-into-kanban-task-folders/
**Diff scope:** branch task/316-playwright-tests-write-screenshots-to-test-output vs master (`.github/workflows/workflow.yml`, `tests/container-apps/web/web-spa-playwright-tests/*`)
**Plan / brief:** task.md Requirements — shared screenshot helper writing to test output, remove kanban path lookups, guard, CI upload path.
**Effort:** 2 (by-diff budget); roster axes: general
**Reviewer roster:** general
**Session IDs:** review oracle under `ganda task work` (Claude Opus 5.5)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
