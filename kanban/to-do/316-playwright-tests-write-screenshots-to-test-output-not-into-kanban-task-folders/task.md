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

- [ ] Shared screenshot-path helper writing to test output; both suites use it
- [ ] No `kanban` path references under `tests/` + guard (test or dirty-tree check)
- [ ] Gates incl. clean `git status` after `dev test`

## Session

- Created: 2026-10-12 (cockpit, per Steve; found by the 313 worker)
