# Round 1 — general
**Date:** 2026-10-22
**Scope reviewed:** branch vs master, `source/` and `tests/` (KpiCard, KpiTone, KpiStrip, SplattedClass, tokens.css, StyleGuidePage, two render-test files)

## Summary

The change adds the `KpiCard` tile, the `KpiTone` enum, and the `KpiStrip` grid, plus two tokens
and a style-guide card. It matches the checklist and the implementer restatement: string-only
parameters, blank optionals omitted, tone drawn as a border modifier, one 160px auto-fit grid with
`--twe-space-3` and `min-width: 0`, isolated CSS with no `<style>` and no `::deep`, Purpose and
Design regions, and no product-name leak. Render tests pass (7/7, 2/2). Risk is low. The one
design concern is a new `SplattedClass` helper that adds a second idiom for a caller `class`
where the repo already has one.

## Issues

### Issue 1 — Severity: suggestion
- File: source/container-apps/web/projects/web-spa/components/splatted-class.cs:13
- Description: `SplattedClass` (new, 43 lines) merges a caller `class` out of `AdditionalAttributes`
  so it does not overwrite the root class. The repo already solves this a different way: `Card`
  (`components/elements/Card.razor:14,36`), `FormSection`, and `FormField` declare
  `[Parameter] public string? Class` and render `class="twe-card @Class"`. Blazor matches parameter
  names case-insensitively, so a caller's `class="x"` binds to `Class` and never reaches the splat
  dictionary. The template now has two idioms for one concern, and the new one is used only by
  these two components.
- Suggestion: Drop `splatted-class.cs`. Give `KpiCard` and `KpiStrip` a `Class` parameter and render
  `class="twe-kpi … @Class"` / `class="twe-kpi-strip @Class"` with `@attributes="AdditionalAttributes"`,
  as `Card` does. Keep the existing `Pass_Extra_Attributes_To_The_Root` tests (they should still
  pass), and update both Design regions to say a caller class binds to `Class`.
- Status: open

### Issue 2 — Severity: nit
- File: tests/container-apps/web/web-spa-integration-tests/components/kpi-strip-render-tests.cs:42
- Description: `closeAt` looks for any `</div>` after the child, so the check "child is inside the
  strip" passes trivially; it does not tie that `</div>` to the strip root.
- Suggestion: Assert the child sits inside the root element, for example that the markup between the
  strip root's opening tag and the final `</div>` contains `data-qa="StripChild"`, or that
  `html.TrimEnd().EndsWith("</div>")` and `childAt` is greater than the end of `RootTag`.
- Status: open

## Notes (not findings)

- The spec says to replace a hand-rolled `.sg-kpis` demo. That class does not exist on master or on
  this branch, so adding a new "KPI Cards" card was the correct reading.
- `SectionHeader`, `Section`, and `DataGridFrame` render a fixed class before
  `@attributes="AdditionalAttributes"` and have no `Class` parameter, so a caller `class`
  overwrites their root class. This predates this diff and is out of scope here.
