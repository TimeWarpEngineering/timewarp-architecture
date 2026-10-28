# Round 1 — general
**Date:** 2026-10-29
**Scope reviewed:** b8186f383 `drawer-playwright-tests.cs` (Design region + `AssertModalInTheShellAsync` probe), task.md Results,
`source/container-apps/web/projects/web-spa/components/elements/Drawer.razor.css` for the cause claim.

## Summary

The change explains the CI pair `paintHitsDialog: false` / `scrimHitsDialog: true` with the open slide. It then finishes the
dialog's own animations before it reads `getBoundingClientRect`. The cause checks out against the CSS:
`dialog.twe-drawer.twe-drawer--end[open]` runs `twe-drawer-in-end 180ms` from `translateX(100%)`. The bounding rect includes the
transform, so the from keyframe puts `rect.left + 24` at 1304 on a 1280px viewport. `elementFromPoint` returns null there. The
scrim point `(24, innerHeight/2)` still hits `::backdrop`, which belongs to the dialog.

The assertion is kept, not widened. The sample is still `rect.left + 24`, `min(rect.top + 12, 16)`, and `elementFromPoint`
must still walk to the dialog. The new `sampleInside` conjunct only makes the check stricter. `finish()` jumps to the end
keyframe, which is the same state the open slide reaches on its own. That is the "wait until the open animation has finished"
option the task allows, without a poll. `getAnimations()` on the dialog returns only the dialog's own animations (no `subtree`).
That is the transform animation that moves the box. The extra probe fields (`sampleX`, `sampleY`, the rect) make a future
failure easy to diagnose. The Design region was updated with the reason.

Product CSS and behavior are unchanged, and the Results say so: the user still sees a correct slide. That fits the
out-of-scope rule.

Re-verified: `dotnet test -c Release -- --filter-class DesignSystemDrawer_Given_Wasm` passed, 1 of 1, in this worktree.

The "before" loop did not reproduce the miss (20/20 passed). The Results say this openly and point to the isolated
headless reproduction instead. That is an honest record. It is not a defect in the change.

Goal restatement vs task: they match. Both ask for the cause, a race-free check of the same paint assertion, a stop if it is a
product bug, a loop of 20 or more runs with before and after counts, and the gates. No misread.

## Issues

None.
