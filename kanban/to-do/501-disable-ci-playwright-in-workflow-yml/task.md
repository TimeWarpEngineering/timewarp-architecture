# Disable ci (playwright) in workflow.yml

## Description

Remove the `playwright` matrix leg from `.github/workflows/workflow.yml` so pull requests and merges no longer wait 10–15 minutes for Playwright browser tests while Task 502 evaluates the suite's value.

Measured baseline from Task 500 (`report/analysis.md` Item 2): `ci (playwright)` ran 10–15 minutes on every PR — including non-UI diffs like PR #656 (`14m 46s`) and PR #654 (`11m 02s`) — while `ci (fast)`, `ci (publish)`, `template-smoke`, and `ci (integration)` all finished in 3.5–7 minutes.

## Requirements

- Remove the `playwright` entry (`--phase test --group playwright`) from `jobs.ci-group.strategy.matrix.include` in `.github/workflows/workflow.yml`.
- Remove the `Upload Playwright screenshots` step from `jobs.ci-group.steps` and update the header comment listing `ci-group` jobs.
- Keep `tests/container-apps/web/web-spa-playwright-tests/` and `TestGroups.Playwright` (`tools/dev-cli/services/test-groups.cs`) intact so `dev test --group playwright` still runs locally and Task 502 can evaluate the suite.
- Verify `ci` required-check status still passes cleanly with the remaining `fast`, `integration`, and `publish` matrix legs.

## Checklist

- [ ] Remove `playwright` matrix entry and `Upload Playwright screenshots` step from `.github/workflows/workflow.yml`
- [ ] Update header comment in `.github/workflows/workflow.yml`
- [ ] Run `./bin/dev build`, `dev-cli-tests`, and `ganda repo audit`

## Session

- Created: 2026-11-23 (from Task 500 walk performance review)

## Notes

- Companion research task: **502** (evaluate Playwright suite value and long-term disposition).
- Parent performance review: **500** (`kanban/to-do/500-walk-performance-review-for-tasks-499-483-494-and-498/report/analysis.md`).
