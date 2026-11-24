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

## Goal (implementer restatement)

Decide what the Playwright suite (`tests/container-apps/web/web-spa-playwright-tests/`) is
worth now that task 501 took it off every pull request, and say where it should run.

Acceptance:

- `report/analysis.md` in this kitchen, in the `tw-analysis-report` analysis outline.
- Every test class listed with the behavior it proves, why it needs a browser, its runtime
  where measured, and the non-browser tests that cover the same feature.
- Git and kanban history sorted into real product bugs the suite caught versus flake and
  upkeep cost, with commit or task ids.
- The three options (retire, nightly only, path-gated and sharded on pull requests) compared
  on measured data, one recommendation, and the follow-up scope recorded under `## Results`.
- No product code edits.

## Checklist

- [x] Inventory all Playwright test classes, runtimes, and overlap with existing unit/integration/Aspire tests
- [x] Check git/kanban history for real bugs caught by Playwright vs churn
- [x] Compare options (retire, nightly-only, path-gated + sharded) and write `report/analysis.md`
- [x] Record recommendation and follow-up scope in `## Results`

## Session

- Created: 2026-11-23 (from Task 500 walk performance review)
- 2026-11-25: implement oracle (ganda task work). Inventory and history were read by two helper agents and spot-checked against the cited task files. CI data came from the gh jobs API and run 45246814905. Wrote `report/analysis.md`. Reserved follow-up id 523 with `ganda kanban reserve` and added its kitchen on this branch (the task 375 → 376 precedent).
- Implement oracle: implemented by implementer-claude (claude, vendor anthropic, model claude-opus-5-5, response model claude-opus-5-5), session b3e5b7a6-c28a-48fc-8318-45adcf4e97ed, max-turns 500, Done — 2026-11-24T19:58:05Z

## Notes

- Companion task **501** disables `ci (playwright)` in `workflow.yml` immediately.
- Parent performance review: **500** (`kanban/archived/500-walk-performance-review-for-tasks-499-483-494-and-498/report/analysis.md`) and follow-up **514** (`kanban/done/514-walk-performance-review-for-tasks-489-001-005-495-491-490-453-501-and-503/report/analysis.md`).

## Results

**Recommendation: option 2, nightly only. It is already live, so CI needs no change.**

- Keep the suite. Do not run it on pull requests.
- The nightly `template-smoke --matrix --app-tests` already runs it in every web-on variant.
  On 2026-11-23 each variant passed 38/38 in about 590s
  (run https://github.com/TimeWarpEngineering/timewarp-architecture/actions/runs/45246814905).
- In the 130 `ci (playwright)` jobs from 2026-10-29 to 2026-11-24 (median 640s, p90 810s,
  1,367 runner-minutes), no browser test failed. Both red jobs were `SYSLIB1031` compile
  errors, which `ci (integration)` and template-smoke also reported.
- The real bugs the suite found came from running a new class on its own pull request:
  task 351 (Tag pick races), task 371 (tooltip light-dismiss closing menus), and task 295
  (attachment 415, `fluent-textarea` tag). None of them was a regression in older code.
- Do not retire it: the focus, first-paint, popover, clipboard, file-chooser, and microphone
  checks have no non-browser equivalent.
- Do not path-gate it on pull requests now. A filter wide enough to cover package bumps and
  server changes matches 32 of 40 product merges. The wslc image also has no browsers
  (task 415).

Full analysis: `report/analysis.md`.

**Follow-up scope**

- **523** (to-do, kitchen on this branch): `dev test <file…>` runs the classes declared in a
  test file, so the walk's per-file gate runs an added or edited Playwright class before the
  pull request. The same task corrects the "local only" comments in `test-groups.cs` and
  `workflow-command.cs` and adds a line to the AGENTS.md Tests row.
- Not filed:
  - Option 3, path-gated and sharded on pull requests. Revisit only if a nightly Playwright
    failure turns out to be a regression that a pull-request gate would have stopped.
  - Trimming the nightly variants. The nightly is off the walk's critical path, and task 376
    shows variant-specific failures happen.

### How to validate

- Smoke: open `kanban/in-progress/502-research-and-evaluate-playwright-tests-for-value/report/analysis.md`.
  Then run `gh run view 45246814905 --log | grep 'web-spa-playwright-tests:'`.
- Expect: the report has the six `tw-analysis-report` sections, the options table, and the
  recommendation. The grep prints five lines, one per variant, each reading
  `38/38 succeeded`.
  `kanban/to-do/523-dev-test-runs-the-classes-in-a-test-file-so-walk-gates-run-playwright-classes.md`
  exists. No file under `source/`, `tests/`, `tools/`, or `.github/` changed on this branch.

### Gates

- `./bin/dev build`: passed (client-name scan passed; solution built, 0 warnings, 0 errors).
- Test files: none. This research task added or edited no test file and no production code
  (only kanban files changed), so no test file applies.
- `ganda repo audit`: passed with one advisory warning, `vscode-window-icon`
  (`.vscode/settings.json` missing in this worktree). `ganda repo audit --fix` wrote the
  gitignored `.vscode/settings.json`, so nothing changed in git. The rerun passed every check.
