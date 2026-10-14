# Round 1 — general
**Date:** 2026-10-14
**Scope reviewed:** commit 6252a4237

## Summary

The change replaces fixed per-column `Width` with Grid mode and `minmax` tracks. It also wraps both
admin grids in a scrolling `DataGridFrame` and guards the rule with a source scan. Risk is low. I
checked against Fluent 5.0.0's bundled CSS: `[col-title-text]` and `td` already apply
`text-overflow: ellipsis`, so the frame only needs to remove the 75px sort-button floor and clip
`th`. The app bundle loads after the library bundle, so the scoped `::deep` rule wins the
specificity tie. The Playwright proof measures the overlap, width, and scroll requirements
directly. The guard covers the two grids that exist.

## Issues

### Issue 1 — Severity: suggestion
- File: source/container-apps/web/projects/web-spa/features/admin/principals/pages/PrincipalsPage.razor:107
- Description: The Principal column used to be 22rem, wide enough for the whole GUID. It now has a 14rem minimum, so at narrow widths the monospace GUID is cut off with an ellipsis and there is no tooltip to show the full value. The task says truncating columns should have tooltips, as Owner does.
- Suggestion: Add `Tooltip="true" TooltipText="@(static row => row.PrincipalId.Value.ToString())"`, the same way the feedback grid's Owner column does.
- Status: open
