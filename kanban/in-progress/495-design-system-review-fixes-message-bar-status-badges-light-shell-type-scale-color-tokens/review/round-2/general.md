# Round 2 — general
**Date:** 2026-11-24
**Scope reviewed:** fix delta since round 1, commit `bf6441618` (`tokens.css`, new
`tests/container-apps/web/web-spa-integration-tests/components/dark-danger-tint-tests.cs`,
the recaptured dark shots, and the `task.md` restatement, Session, and Results edits).

## Summary

The fix re-declares `--twe-danger-tint` in `body[data-theme="dark"]` as 12% of the dark
`--twe-danger` (`#ef9a9a`) into the dark `--twe-paper` (`#303030`), with a comment in both the
root and the dark block saying why. The consumers (`StatusBadge.razor.css` `.twe-badge--error`
and `MessageBar.razor.css` `--colorPaletteRedBackground1`) read the token, so both now get the
dark fill. I re-did the math by hand: 12% gives rgb(70.9, 60.7, 60.7), which matches the
painted fill recorded in Results (`color(srgb 0.278 0.238 0.238)`). Against `#ef9a9a` that is
4.89:1. At 18% it would be about 4.34:1, so the comment explaining "12%, not 18%" is correct.
`assets/08-after-status-badges-dark.png` shows dark red Error and Churned pills under pink
text.

The new test follows the pattern of the sibling `shell-breakpoint-tests.cs`: it reads
`tokens.css` as text, has Purpose and Design regions, uses the `[TestTag("Unit")]` class, and
registers through a module initializer. It requires the exact declaration twice and requires
one of those copies to be in the dark block, so deleting the dark copy fails it. The
implementer recorded `./bin/dev build` (0 warnings, 0 errors), the filtered runs of the new
class and the two other tests that read `tokens.css`, the browser check, and `ganda repo
audit` exit 0. That covers the change.

The restatement added one sentence about the dark tint. That is the round-1 defect turned into
a goal, and it fits the original task. It is not a misread.

## Prior findings

- M1 (bug): fixed. The dark block declares `--twe-danger-tint` (tokens.css:245), and the test
  pins it.

## Issues

None.
