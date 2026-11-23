# Research and evaluate Playwright tests for value

## Description

Evaluate the Playwright browser test suite (`tests/container-apps/web/web-spa-playwright-tests/`) and recommend whether it should be retired, moved to a nightly schedule only, or selectively re-enabled (path-gated and sharded) on pull requests.

Task 501 disabled `ci (playwright)` in `.github/workflows/workflow.yml` because it was the serial CI bottleneck on every PR (10–15 minutes per run; Task 500 `report/analysis.md` Item 2).

## Requirements

- Produce a structured analysis report at `report/analysis.md` in this task's kitchen (following `tw-analysis-report`).
- Inventory every test class in `tests/container-apps/web/web-spa-playwright-tests/`: what behavior it verifies, measured runtime (Task 500 SigNoz baseline: `DarkThemePlaywright_` `76.9s`, `FeedbackAttachmentPlaywright_` `69.5s` + `35.6s`, etc.), and overlap with `web-spa-integration-tests` and `aspire-tests`.
- Review git/kanban history for real production/UI regressions actually caught by the Playwright suite vs maintenance/flakiness cost.
- Compare concrete options with clear trade-offs:
  1. Retire and delete the Playwright suite (rely on `web-spa-integration-tests` + `aspire-tests`).
  2. Run Playwright nightly only (alongside `template-smoke --matrix --app-tests`).
  3. Re-enable on PRs only when UI/web-spa paths change, sharded/parallelized on `wslc` self-hosted runners.
- Deliver a clear recommendation and scoped follow-up task(s). No product code edits in this research task.

## Checklist

- [ ] Inventory all Playwright test classes, runtimes, and overlap with existing unit/integration/Aspire tests
- [ ] Check git/kanban history for real bugs caught by Playwright vs churn
- [ ] Compare options (retire, nightly-only, path-gated + sharded) and write `report/analysis.md`
- [ ] Record recommendation and follow-up scope in `## Results`

## Session

- Created: 2026-11-23 (from Task 500 walk performance review)

## Notes

- Companion task **501** disables `ci (playwright)` in `workflow.yml` immediately.
- Parent performance review: **500** (`kanban/to-do/500-walk-performance-review-for-tasks-499-483-494-and-498/report/analysis.md`).
