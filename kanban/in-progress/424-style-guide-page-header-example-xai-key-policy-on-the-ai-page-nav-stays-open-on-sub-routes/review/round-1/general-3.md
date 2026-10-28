# Round 1 — general-3
**Date:** 2026-10-28
**Scope reviewed:** items 1, 4, 6, and 7. `PageHeaderSample.razor(.css)`,
`DesignSystemPage.razor(.cs)`, the `TimeWarpPage` Design region and CSS, `DesignSystemCatalog.razor.css`,
`tokens.css`, `app.css`, the skills (`tw-blazor`, `tw-blazor-layout`, `tw-slice-isolation`), the
rename, and the breadcrumb and catalog Playwright tests.

## Summary

The Page header part covers the spec: no second `h1`, Outline "Sync" and Primary "Add item" with
icons, the placement rule in `tw-blazor-layout` and the `TimeWarpPage` Design region, and no dummy
buttons in the page's own header. The rename is clean. `style-?guide` and `sg-` greps outside
`kanban/` are empty. All 27 sample `.razor`/`.razor.css` files are pure moves with prefix changes,
and only `PageHeaderSample` is new. The breadcrumb padding and negative margin keep the trail in
place and keep the ellipsis, and its Playwright check is real (outline width and offset, all four
sides inside the list box). Catalog rows are 4 + 20 + 4 = 28px and Playwright checks 26–32px. The
weak spots are one assertion that now passes for the wrong reason, a copied title-bar stylesheet
that has already drifted, and a new token that Results don't mention.

## Issues

### Issue 1 — Severity: bug
- File: tests/container-apps/web/web-spa-integration-tests/features/design-system/design-system-page-contract-tests.cs:63
- Description: `html.ShouldContain("twe-page__actions")` was meant to prove the shell renders its
  Actions slot. The page now passes no Actions, so `TimeWarpPage.razor` leaves that div out. The
  assertion passes only because the All view renders `PageHeaderSample`, which uses the same class.
  It now checks the opposite of its purpose, and no test asserts the dummy header buttons are gone.
- Suggestion: assert the shell header has no actions. For example, require exactly one
  `twe-page__actions` and require it inside `data-qa="PageHeaderExample"`, or render a non-header
  part and assert the class is absent.
- Status: open

### Issue 2 — Severity: suggestion
- File: source/container-apps/web/projects/web-spa/features/design-system/samples/PageHeaderSample.razor.css:8-39
- Description: the sample copies the shell's `.twe-page__header`, `__lead`, `__title`, `__description`,
  and `__actions` rules from `TimeWarpPage.razor.css` under the same names. It has already drifted:
  the shell's description has `margin: 0 0 var(--twe-space-2)` (`TimeWarpPage.razor.css:168`), the
  sample's has `margin: 0`. The shell's description is a `div` and the sample's is a `p`. Repo rule:
  when two things must agree, generate one from the other or add a build check.
- Suggestion: put the title bar in one component with a heading-level parameter and use it in both
  places, or add a check that the rules match and use `ds-` names in the sample.
- Status: open

### Issue 3 — Severity: suggestion
- File: source/container-apps/web/projects/web-spa/wwwroot/css/tokens.css:75-77
- Description: the change adds a new token, `--twe-space-1: 4px`. The spec allows a new token only
  if none fits, and Results use it without saying it is new or why. Nine comments still describe
  the scale as "8, 12, 24, 48": `tokens.css:83`, `SectionHeader`, `FilterBar`, `SegmentedControl`,
  `Tabs`, `ChartCard`, `EmptyState`, `IntegrationCard`, and `KpiCard` `.razor.css`.
- Suggestion: say in Results why 4px was needed (or use existing tokens), and update those comments
  to include 4.
- Status: open

### Issue 4 — Severity: suggestion
- File: skills/tw-blazor/SKILL.md:228, 302, 316
- Description: the rename rewrote these reference paths to `DesignSystemPage.razor`, which no longer
  has the Section card, button appearances, or Forms card. They live in `samples/SectionSample.razor`,
  `samples/ButtonsSample.razor`, and `samples/FormsSample.razor`. The references were already wrong
  before the rename.
- Suggestion: point each reference at its sample file.
- Status: open

### Issue 5 — Severity: nit
- File: tests/container-apps/web/web-spa-playwright-tests/breadcrumb-playwright-tests.cs:84-91
- Description: `rolesActive` returns false when `a[href='/Admin/Roles']` is missing, so
  `ShouldBeFalse` passes even if the item never rendered.
- Suggestion: assert the item exists first, or return three states.
- Status: open

### Issue 6 — Severity: nit
- File: source/container-apps/web/projects/web-spa/features/design-system/samples/PageHeaderSample.razor:13
- Description: the new sample uses the `ph-sample` prefix. Every other sample uses `ds-`, the prefix
  this task set.
- Suggestion: rename the classes to `ds-page-header` and `ds-page-header__rule`.
- Status: open

### Issue 7 — Severity: nit
- File: source/container-apps/web/projects/web-spa/features/design-system/samples/PageHeaderSample.razor:22, 28
- Description: the example buttons dispatch `NotificationState.AddNotification` (allowed, and both are
  `FluentButton`). But they post only "Sync." and "Add item.", the toast-only behavior Steve
  objected to, now inside a labelled example.
- Suggestion: optional. Drop the handlers, or post a message that says this is a placement demo.
- Status: open
