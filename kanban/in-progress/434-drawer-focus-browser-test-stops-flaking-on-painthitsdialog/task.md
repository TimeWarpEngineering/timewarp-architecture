# Drawer focus browser test stops flaking on paintHitsDialog

## Goal (implementer restatement)

The drawer focus browser test sometimes reports that the open drawer misses the top-layer paint
sample while the scrim sample hits. The goal is to explain that miss from the drawer's slide
and from what the sample rectangle measures, then make the test wait until that slide has
settled (or sample a point that is inside the panel) so the same paint check stays meaningful
and stops racing. A real paint bug in the drawer would be reported instead of papered over.
Acceptance is a recorded cause, a test that still asserts the drawer paints above the shell, a
local loop of at least 20 runs with before and after counts, a clean `dev build`, and a passing
`dev test --group playwright`.

## Description

`Open_Should_MoveFocusIn_TrapTab_And_ReturnFocusOnEscape` in
`tests/container-apps/web/web-spa-playwright-tests/drawer-playwright-tests.cs` fails
intermittently in CI's `ci (playwright)` job. It failed twice on 2026-10-28: PR #591 (run
40731215557) and PR #593 (run 40744773374). Both PRs touched unrelated files, and both passed
on a re-run of the failed job and on local full runs.

The failing assertion is in `AssertModalInTheShellAsync`:
`root.GetProperty("paintHitsDialog").GetBoolean().ShouldBeTrue(probe)`. The probe output was the
same both times:

```
{"parentIsBody":false,"shellDisplay":"contents","matchesModal":true,"focusBeforeOpener":true,
 "openerTookFocus":false,"focusAfterOpenerAttempt":true,"paintHitsDialog":false,"scrimHitsDialog":true}
```

So the dialog is open, modal, and holds focus, and the scrim point hits it. Only the
`elementFromPoint(rect.left + 24, Math.min(rect.top + 12, 16))` point misses. That points to a
timing problem, for example the probe running while the drawer is still sliding in or laying out,
or the point landing outside the panel at the moment it is measured. It does not look like a
product bug.

## Requirements

- Find why that point misses on a slow CI runner. Check the drawer component's CSS for an
  open transition or animation, and what `rect` measures (the `dialog` versus its visible panel).
  Record the cause in Results.
- Fix the test so it checks the same thing (the drawer paints in the top layer above the shell)
  without a race. For example, wait until the drawer's open animation has finished
  (`getAnimations()` settled, or a stable bounding box) before probing, or probe a point that
  lies inside the panel by construction. Do not delete the assertion or widen it until it checks
  nothing.
- If the cause is a real product bug (the drawer can paint wrong for a user), stop and report it
  in the task instead of hiding it in the test.
- Prove it: run the drawer test class in a loop locally (at least 20 runs, with
  `--filter-class` and CPU throttling or reduced parallelism if that reproduces it) and record the
  before and after pass counts.

## Out of scope

- Changing drawer behavior or styling, unless the cause is a product bug and Steve approves.
- Any change outside this repository. If something here seems to need one, stop and report.

## Checklist

- [x] Cause recorded in Results
- [x] Test waits for a settled drawer, or probes a point inside the panel by construction
- [x] Loop run recorded (before and after pass counts)
- [x] `dev build` 0/0; `dev test --group playwright` passes
- [x] No client name in source, tests, or skills

## Results

The miss is the open slide, not a wrong paint. `dialog.twe-drawer--end[open]` runs
`twe-drawer-in-end` for 180ms, from `translateX(100%)` to `translateX(0)`. The dialog
element is the sheet (`position: fixed`, `inset-inline-end: 0`, width `min(36rem, 100vw)`).
`::backdrop` is the scrim and does not move with that transform. `getBoundingClientRect`
includes the transform.

The paint sample is `rect.left + 24` by `min(rect.top + 12, 16)`. On a 1280px viewport the
from keyframe puts the 576px sheet at `rect.left === 1280` with transform
`matrix(1, 0, 0, 1, 576, 0)`, so the sample x is 1304. That is past the viewport, and
`elementFromPoint` returns null. The scrim sample `(24, innerHeight / 2)` still hits the
backdrop, which the hit walk reports as the dialog. That is the CI pair
`paintHitsDialog: false`, `scrimHitsDialog: true`. A headless page with the same rule
showed that pair on the immediate read and on the first animation frame. `animation.finish()`
then left `transform: none`, `rect.left` 704, and sample `(728, 12)` inside the sheet, and
both hits were true.

A user still sees the sheet slide in over the app bar. The top layer and the scrim are
already in place while the sheet translates. Drawer CSS and behavior are unchanged.

`AssertModalInTheShellAsync` now calls `finish()` on the dialog's running, pending, or
paused animations before it reads the box. The same sample must lie inside the settled
border box and the viewport, and `elementFromPoint` must still walk to the dialog. A miss
still fails the assertion.

Loop, from `tests/container-apps/web/web-spa-playwright-tests`,
`dotnet test -c Release -- --filter-class DesignSystemDrawer_Given_Wasm` (later runs
`--no-build`):

- Before, unfixed assembly: 20 passed, 0 failed. Those runs did not hit the from-keyframe
  window. The focus wait usually outlasts the 180ms slide. `stress-ng` and `cpulimit` are
  not installed. Slowing the machine would stretch the Playwright round trip more than the
  slide, so it was not used. The isolated page above is the reproduction of the miss.
- After: 20 passed, 0 failed.

`./bin/dev build`: Build succeeded, 0 Warning(s), 0 Error(s).
`./bin/dev test --group playwright`: `web-spa-playwright-tests` passed, 32 passed, 0 failed.

No client name added.

### How to validate

**Smoke**

```bash
cd tests/container-apps/web/web-spa-playwright-tests
dotnet test -c Release -- --filter-class DesignSystemDrawer_Given_Wasm
```

**Expect**

- The class passes.
- A failing paint probe still names `paintHitsDialog` and includes `sampleX`, `sampleY`,
  and the dialog rect. The sample stays 24px inside the sheet, and its y is
  `min(rect.top + 12, 16)`.

**Automated gate**

```bash
./bin/dev build
./bin/dev test --group playwright
```

## Session

- Created: c2bec172-f0eb-4648-9edb-30256e7756e3 (2026-10-28)
- Implementation: settled the paint sample after the open slide (2026-10-29). `ganda repo audit` exited 0. `kanban-landed` warns that other claims have no usable worktree (070-001, 104-024, 104-025, 104-026, 123, 141, 198, 244, 437). Left alone. `dev build` already runs the client-name scan.
- Implement oracle: implemented by implementer-grok (grok, model grok-4.7), session 01a183eb-3f11-7290-8cb1-eee1f99a4a62, max-turns 500, Done — 2026-10-28T19:36:04Z
