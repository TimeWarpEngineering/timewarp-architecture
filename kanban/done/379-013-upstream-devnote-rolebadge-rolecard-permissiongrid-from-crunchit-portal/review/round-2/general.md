# Round 2 — general
**Date:** 2026-10-24
**Scope reviewed:** the fix delta `787fa86db..8041a392f` (`source` and `tests`: 7 files, +51 / −27), checked against round-1 M1–M4, `## Goal (implementer restatement)`, and the original task.

## Summary

The fix commit closes all four round-1 findings and adds no new defects. The style-guide permission grid and dev note now sit inside native `div.sg-follow` wrappers. The page authors those divs, so they carry the page scope, and `.sg-follow { margin-top }` in `StyleGuidePage.razor.css` now matches. `RoleCard`'s Design region and its `overview.md` row name the parent's roving tabindex and arrow keys, and they point at `RovingSelection` and `SegmentedControl`. The style-guide sample sets `tabindex` 0 / −1, and that works because `RoleCard` captures unmatched attributes and splats them onto the button. The test pins `>Granted<`, and the hidden span is `twe-perm-grid__state` in both the Razor and CSS files.

## Verification

- M1: `.sg-follow` is defined at `StyleGuidePage.razor.css:13`. The wrappers are page-authored native divs. `data-qa="StyleGuidePermissionGrid"` and `data-qa="StyleGuideDevNote"` moved to the wrappers, and a search of `source` and `tests` found no other code that reads those ids.
- M2: `RoleCard.razor:22` declares `CaptureUnmatchedValues`, and `:36` splats the attributes onto the button. The render test asserts `tabindex="0"` on the root.
- M3: both granted assertions use `>Granted<`.
- M4: no reference to `twe-perm-grid__name` remains in `source` or `tests`, apart from the test's `ShouldNotContain`.
- Re-run (Release): `RoleCard_Should_` 4/4 and `PermissionGrid_Should_` 5/5 passed. The client-name search of the delta found 0 matches.
- The restatement matches the task. The keyboard sentence it gained comes from M2 and does not change scope. No misread.

## Issues

None.
