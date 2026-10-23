# Round 1 — merged findings
**Date:** 2026-10-24
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 1 | 0 | 0 |
| suggestion | 1 | 0 | 0 |
| nit | 2 | 0 | 0 |

## Issues

### M1 — Severity: bug — Status: open
- File: source/container-apps/web/projects/web-spa/features/style-guide/pages/StyleGuidePage.razor:342 (and :347)
- Description: `Class="sg-follow"` on `PermissionGrid` and `DevNote` is dead under CSS isolation. `.sg-follow[b-iy1milohjd]` (the page scope) cannot match the child roots, which carry their own scope attribute. The grid and the note get no top margin (Wall A, `tw-blazor-css-strategy`).
- Suggestion: Put `sg-follow` on a native wrapper the page authors (for example `<div class="sg-follow" data-qa="StyleGuidePermissionGrid">`), or use an anchored `::deep` from a native page element.
- Source: general
- Disposition notes:

### M2 — Severity: suggestion — Status: open
- File: source/container-apps/web/projects/web-spa/components/elements/RoleCard.razor:6
- Description: `role="radio"` cards in a `role="radiogroup"` sample have no roving tab stop and no arrow keys. The repo already has that pattern (`RovingSelection`, `SegmentedControl`). The Design region does not say that the parent owns it.
- Suggestion: Name the parent's keyboard duty (a roving `tabindex` passed through unmatched attributes, plus the arrow keys) in the Design region and the `overview.md` row, and point at `RovingSelection` / `SegmentedControl`. Optionally set `tabindex` 0 / −1 in the style-guide sample.
- Source: general
- Disposition notes:

### M3 — Severity: nit — Status: open
- File: tests/container-apps/web/web-spa-integration-tests/components/permission-grid-render-tests.cs:40
- Description: `ShouldContain("Granted")` also matches `Not granted`, so the granted branch is not pinned on its own.
- Suggestion: Assert `">Granted<"` or count occurrences.
- Source: general
- Disposition notes:

### M4 — Severity: nit — Status: open
- File: source/container-apps/web/projects/web-spa/components/elements/PermissionGrid.razor:41
- Description: The hidden state span is `twe-perm-grid__name`, and the capability name is `__label`.
- Suggestion: Rename it to `twe-perm-grid__state` in the Razor and CSS files.
- Source: general
- Disposition notes:

## Duplicates / conflicts

- None. A single reviewer.

## Operator notes (not findings)

- **Base branch.** The claim worktree started at `origin/feature/379-portal-rest` (the 379-012 merge, PR #567). The implementer reset the task branch to `origin/master`, so `ganda context` now reports base `master`. That feature branch also edits `StatusBadge.razor` (the same Design-comment hunk, plus `BadgeStatus.Error`), `badge-status.cs`, `StyleGuidePage.razor`, `StyleGuidePage.razor.css`, `tokens.css`, and `components/overview.md`. A PR from this branch will textually conflict with `feature/379-portal-rest` in those files, whichever lands second. The operator decides whether 379-013 targets `master` or the 379 work-set stack. A fix pass on product code does not decide it.
- Verified: the four test classes pass (18/18, Release). The contrast figures in the token comment are correct (4.44:1 and about 5.5:1). The client-name search of `master...HEAD -- source tests` has 0 matches. `dev build`, `template-smoke`, and `ganda repo audit` were not re-run by the reviewer.
