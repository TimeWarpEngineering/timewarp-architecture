# Playwright tests write screenshots to test output, not into kanban task folders

## Problem

`dev test` overwrites committed screenshots under `kanban/done/*`. The worker on task 313 had to
restore them by hand.

Two Playwright suites resolve their screenshot path by walking up to the repo root and globbing
`kanban/**/<task-number>-*`:
- `tests/container-apps/web/web-spa-playwright-tests/ask-surface-playwright-tests.cs`:
  `ScreenshotPath(fileName, taskPrefix = "296")`, also called with `"307"`;
- `tests/container-apps/web/web-spa-playwright-tests/feedback-attachment-playwright-tests.cs`:
  same idea, with tasks 295 and 302.

So every test run:
- dirties the working tree with regenerated PNGs inside closed task folders. These are review
  evidence, which should be immutable once a task is done;
- couples template tests to the monorepo's kanban numbering. Generated apps ship `tests/` but no
  `kanban/`. `feedback-attachment` falls back to the test output folder.
  **`ask-surface` does not fall back:** `Directory.GetDirectories(Path.Combine(directory,
  "kanban"), …)` throws in a generated app, and `.Single(...)` throws if the task folder is moved
  or archived. That is a latent failure in every generated app.

## Requirements

- Screenshots always go to the test's own output, for example
  `<test bin>/playwright-screenshots/<test-class>/<file>.png`, through one shared helper both
  suites use. Remove every `kanban` path lookup and task-number parameter from tests.
- A run must leave `git status` clean. Add a check:
  - a dev-cli or analyzer-style test that no file under `tests/` references `kanban` as a path;
  - or have `dev test` fail if the tree is dirty after the run.

  Pick one and record it.
- The screenshots already committed under `kanban/done/{295,296,302,307}-*` stay as they are,
  because they are historical evidence. Don't delete or regenerate them.
- If a task needs fresh screenshots as review evidence, the worker copies them from test output
  into the task folder deliberately, as part of that task. Document this in one line in the shared
  helper's Design region.
- Gates: `dev build` 0/0; the two Playwright suites pass; `git status` clean after `dev test`;
  `dev template-smoke` (generated app's Playwright tests do not touch `kanban/`); `ganda repo audit`.

## Checklist

- [x] Shared screenshot-path helper writing to test output; both suites use it
- [x] No `kanban` path references under `tests/` + guard (test or dirty-tree check)
- [x] Gates incl. clean `git status` after `dev test`

## Session

- Created: 2026-10-12 (cockpit, per Steve; found by the 313 worker)
- 2026-10-12: implemented under `ganda task work` (implement oracle).
- 2026-10-12: reviewed under `ganda task work` (review oracle, effort 2, general); accepted-exceptions.

## Results

- New `tests/container-apps/web/web-spa-playwright-tests/playwright-screenshot.cs`:
  `PlaywrightScreenshot.PathFor<TTest>(file)` writes to
  `<test bin>/playwright-screenshots/<test class>/<file>`. Its Design region has the one-line
  rule: a worker copies shots into a task folder by hand when they are review evidence.
- All four web-spa Playwright suites use it: ask-surface and feedback-attachment (dropped the
  kanban glob and the task-number parameter), plus ask-sign-in and first-paint (dropped
  `artifacts/playwright/<n>`), so there is one rule. `feedback-details-302.png` is now
  `feedback-details.png`.
- **Guard chosen: source-scan test** (not a dirty-tree check in `dev test`).
  `screenshot-path-guard-tests.cs` (`[TestTag("Unit")]`) scans `tests/**/*.cs` (skipping
  bin/obj and itself) for a string literal that starts with `kanban` as a path segment. It ships
  in generated apps and passes there. It is a test and not a `dev test` dirty-tree check because
  it fails at the source line that causes the problem and does not depend on the developer's
  working-tree state.
- CI `Upload Playwright screenshots` now uploads
  `tests/container-apps/web/web-spa-playwright-tests/bin/**/playwright-screenshots/`.
- The committed shots under `kanban/done/{295,296,302,307}-*` are unchanged.

### Gates (2026-10-12)

- `dev build`: succeeded (0 warnings / 0 errors; warnings are errors).
- web-spa-playwright-tests (`dotnet run`): 8/8 passed, including the guard. 27 files were
  written under `bin/Debug/net11.0/playwright-screenshots/`.
- `dev test`: every project passed except aspire-tests 39/40.
  `IngressSmoke_Given_.RolesThroughIngress_Should_Forbidden_Given_MockPrincipal_WithoutAdminRole`
  got 401 because ingress port 63620 was bound by a `yarp` process another session started
  from the `master` checkout ("address already in use"). This is an environment collision; the
  change does not touch aspire or ingress. Rerun once that port is free.
- `git status` was clean after `dev test` and after the Playwright run.
- `dev template-smoke`: SUCCEEDED. The generated SmokeDefault `web-spa-playwright-tests` also
  ran with no `kanban/` folder: 8/8 passed, with shots in its bin.
- `dev check-version`: 2.0.0-beta.20 is ahead of nuget 2.0.0-beta.19, no bump needed.
- `ganda repo audit`: passes.

### Review disposition (2026-10-12)

- Rounds: 1. Effort 2, roster: general (review oracle, Claude Opus 5.5).
- Final counts: bug 0, suggestion 0, nit 1 (wontfix); 0 open.
- Disposition: **accepted-exceptions**. M1 (nit): the guard regex only matches literals that start
  with `kanban`; accepted because that is the form that caused the problem, and widening it risks
  false positives.
- Artifacts: `review/review-framework.md`, `review/round-1/merged.md`, `review/disposition.md`.

### How to validate

- **Smoke:** `cd tests/container-apps/web/web-spa-playwright-tests && dotnet run`, then
  `git status --short` at the repo root and
  `find tests/container-apps/web/web-spa-playwright-tests/bin -path '*playwright-screenshots*' -type f`.
- **Expect:** 8/8 tests pass, including `TestSources_Should_.Not_Reference_Kanban_As_A_Path`.
  `git status` is empty, and nothing under `kanban/done/{295,296,302,307}-*` changed. The PNGs
  are under `bin/<config>/net11.0/playwright-screenshots/{AskSurface_Given_Wasm,AskSignIn_Given_Wasm,FeedbackAttachment_Given_Wasm,FirstPaint_Given_PrerenderedShell}/`.
  Adding `Path.Combine(root, "kanban")` to any test `.cs` makes the guard fail and name that file.
