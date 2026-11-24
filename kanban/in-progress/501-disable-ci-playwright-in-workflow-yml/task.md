# Disable ci (playwright) in workflow.yml

## Description

Remove the `playwright` matrix leg from `.github/workflows/workflow.yml` so pull requests and merges no longer wait 10–15 minutes for Playwright browser tests while Task 502 evaluates the suite's value.

Measured baseline from Task 500 (`report/analysis.md` Item 2): `ci (playwright)` ran 10–15 minutes on every PR — including non-UI diffs like PR #656 (`14m 46s`) and PR #654 (`11m 02s`) — while `ci (fast)`, `ci (publish)`, `template-smoke`, and `ci (integration)` all finished in 3.5–7 minutes.

## Requirements

- Remove the `playwright` entry (`--phase test --group playwright`) from `jobs.ci-group.strategy.matrix.include` in `.github/workflows/workflow.yml`.
- Remove the `Upload Playwright screenshots` step from `jobs.ci-group.steps` and update the header comment listing `ci-group` jobs.
- Keep `tests/container-apps/web/web-spa-playwright-tests/` and `TestGroups.Playwright` (`tools/dev-cli/services/test-groups.cs`) intact so `dev test --group playwright` still runs locally and Task 502 can evaluate the suite.
- Verify `ci` required-check status still passes cleanly with the remaining `fast`, `integration`, and `publish` matrix legs.

## Goal (implementer restatement)

Stop CI from running the Playwright browser tests on every push and pull request, so a PR stops
waiting 10–15 minutes on `ci (playwright)`. The suite itself stays in the repo and still runs
locally with `dev test --group playwright`; task 502 decides its long-term fate.

Acceptance criteria:

- `jobs.ci-group.strategy.matrix.include` in `.github/workflows/workflow.yml` has only `fast`,
  `integration`, and `publish`.
- The `Upload Playwright screenshots` step is gone, and the header comment lists only the three
  remaining `ci-group` jobs and says why playwright is off.
- `tests/container-apps/web/web-spa-playwright-tests/` and `TestGroups.Playwright` are unchanged in
  behavior (only the `test-groups.cs` comment that said CI uploads its screenshots is corrected).
- The `ci` aggregate job still passes: it needs `ci-group` to be `success` or `skipped`, and a
  three-leg matrix reports `success` when those legs pass.
- `./bin/dev build`, the dev-cli test files that read `workflow.yml` or cover `TestGroups`, and
  `ganda repo audit` pass.

## Checklist

- [x] Remove `playwright` matrix entry and `Upload Playwright screenshots` step from `.github/workflows/workflow.yml`
- [x] Update header comment in `.github/workflows/workflow.yml`
- [x] Run `./bin/dev build`, `dev-cli-tests`, and `ganda repo audit`

## Session

- Created: 2026-11-23 (from Task 500 walk performance review)
- 2026-11-24 implementer: removed the `playwright` matrix leg and the screenshot upload step;
  header comment and the `TestGroups` Design comment now say the group is local-only since 501.
- Implement oracle: implemented by implementer-claude (claude, vendor anthropic, model claude-opus-5-5, response model claude-opus-5-5), session 4241ffd3-b42d-4a89-92fe-5f377f8b1f11, max-turns 500, Done — 2026-11-24T02:25:57Z
- Review oracle: reviewed by reviewer-grok (grok, vendor xai, model grok-4.7), session 01a20b5f-d012-7c51-8126-f96ec33ed622, max-turns 80, effort 1 (general), clean — 2026-11-24
- Review oracle: review by reviewer-grok (grok, vendor xai, model grok-4.7, response model grok-4.7-build), session 01a20b5f-d012-7c51-8126-f96ec33ed622, max-turns 80, artifacts general.md, merged.md, disposition.md — 2026-11-24T02:30:05Z

## Review

- Round 1: Done

## Notes

- Companion research task: **502** (evaluate Playwright suite value and long-term disposition).
- Parent performance review: **500** (`kanban/to-do/500-walk-performance-review-for-tasks-499-483-494-and-498/report/analysis.md`).
- Review: `review/review-framework.md`, `review/round-1/merged.md`, `review/disposition.md`.

## Results

Changes:

- `.github/workflows/workflow.yml`: `ci-group` matrix is `fast`, `integration`, `publish`. The
  `Upload Playwright screenshots` step is removed. The header comment lists the three jobs and says
  the playwright group is off in CI (task 501) while task 502 weighs the suite.
- `tools/dev-cli/services/test-groups.cs`: comments only (Purpose and the playwright line in
  Design). `TestGroups.Playwright`, `Names`, and the grouping logic are unchanged, so
  `dev test --group playwright` still runs the suite locally.
- The `ci` aggregate job needs `ci-group` to be `success` or `skipped`; a three-leg matrix reports
  `success` when its legs pass, so the required check is unaffected. The `coverage` upload step's
  `matrix.name != 'publish'` condition still fits.

Gates:

- `./bin/dev build`: succeeded, 0 warnings, 0 errors.
- `dotnet run --project tests/tools/dev-cli-tests -c Release --no-build`: 343/343 passed. The
  files that cover this change (`test-groups-tests.cs`, plus `skill-spec-lint-tests.cs` and
  `aspire-run-tests.cs`, which read `workflow.yml`) are not standalone runfiles (no
  `#if !JARIBU_MULTI` guard); they compile into `dev-cli-tests.csproj`, so the project ran them.
- `ganda repo audit`: passed, with 1 advisory warning, `vscode-window-icon` (`.vscode/settings.json`
  missing). `ganda repo audit --fix` created that gitignored local file; a second `--fix` pass
  shows the check PASS. Nothing committed from it.

Review:

- Round 1, effort 1, roster general. Reviewer did not re-run build, test, or audit.
- Counts: bug 0, suggestion 0, nit 0; open 0, fixed 0, wontfix 0.
- Disposition: **clean**. The goal restatement matches the original task. No misread.
- Paths: `review/review-framework.md`, `review/round-1/general.md`, `review/round-1/merged.md`, `review/disposition.md`.

### How to validate

Smoke: on this branch's pull request, open the checks list; separately run
`grep -n -i playwright .github/workflows/workflow.yml` and `./bin/dev test --help`.

Expect: the PR shows `ci (fast)`, `ci (integration)`, `ci (publish)` and the aggregate `ci`, with no
`ci (playwright)`, and `ci` is green. The grep hits only the header comment lines. `dev test --help`
still lists `playwright` as a `--group` value.

### Evidence boundaries

- **Proved:** none.
- **Trusted assumptions:** GitHub Actions sets `needs.ci-group.result` to `success` when every matrix leg succeeds, and to `skipped` when the job `if` is false. Task 366 says the stable required check, when CI is required, is the aggregate `ci`, not `ci (playwright)`.
- **Empirical checks:** recorded `./bin/dev build` (0 warnings, 0 errors); recorded `dotnet run --project tests/tools/dev-cli-tests -c Release --no-build` (343/343); recorded `ganda repo audit` (pass, one advisory `vscode-window-icon` cleared locally and not committed). Review read the matrix, the removed upload step, `TestGroups`, and the `ci` job condition. Review did not re-run those gates.
- **Out of scope:** the live pull-request checks list (no pull request during this review). Whether the Playwright suite stays (task 502).
