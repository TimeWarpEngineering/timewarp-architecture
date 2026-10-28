# Drawer focus browser test stops flaking on paintHitsDialog

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

- [ ] Cause recorded in Results
- [ ] Test waits for a settled drawer, or probes a point inside the panel by construction
- [ ] Loop run recorded (before and after pass counts)
- [ ] `dev build` 0/0; `dev test --group playwright` passes
- [ ] No client name in source, tests, or skills

## Session

- Created: c2bec172-f0eb-4648-9edb-30256e7756e3 (2026-10-28)
