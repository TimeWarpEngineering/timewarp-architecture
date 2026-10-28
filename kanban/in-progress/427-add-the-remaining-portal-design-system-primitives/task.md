# Add the remaining portal design-system primitives

## Description

Epic 379, the style-guide catalog (421), and the location breadcrumb (423) cover the portal
shell and most of its components. Six primitives from the portal design are still missing.
A generated app should get them from the template, so the next product does not copy them
into page CSS. The generated code carries no client name.

Each new component follows the existing leaf rules: a native root so CSS isolation stamps
the scope attribute, `--twe-*` tokens, `Class` appended on that root, unmatched attributes
splatted onto it, and no product words. The page owns the data and the clicks. A component
that wires no events says so in its Design region. Each new part gets a style-guide sample,
a usage snippet in `StyleGuideParts`, a row in `components/overview.md`, and an HtmlRenderer
test in the same style as `kpi-card-render-tests.cs`.

## Requirements

- **Definition list.** A wrapping grid of term and value. The value is arbitrary content
  (text, a badge, a link), so the item is a term plus a fragment, not two strings. Blank
  terms are omitted. This replaces the one-off two-column grids on principal detail and
  role detail, and those two pages adopt it. Catalog group: Data.
- **Line chart.** A plot for the `ChartCard` child slot. The page passes points, series
  (label and `ChartSeriesTone`), and already-formatted axis labels. The plot draws
  gridlines, the lines, and a dot on the last point. It formats no money and chooses no
  colors of its own. `ChartCard` stays a frame and still draws no series. The chart-frame
  sample uses this plot instead of the decorative polylines. Catalog group: Data, either
  as its own part or as the plot inside the existing chart-frame part. One part is enough
  if the usage snippet shows the plot inside `ChartCard`.
- **Period stepper.** Previous, a label, and next. Each end can be disabled. It does not
  know years, days, or ranges. `SegmentedControl` stays the mutually exclusive choice
  (week, month, custom). The stepper raises `ValueChanged` only if the existing controls
  do; otherwise the page's buttons stay in the previous and next slots and this component
  is the chrome. Match `SegmentedControl`: the page owns the value. Catalog group: Forms.
- **Compact integration warning.** `IntegrationCard` full already shows a warning through
  `Status`. Compact cannot. Add a tone, default and warning, that paints the compact row
  the warning surface (the disconnected pull-status row). Full is unchanged. Do not add a
  second sync-bar component. The integration-cards sample shows a compact row in the
  warning tone.
- **Page split.** A primary column and a secondary column. Wide layout is about 1.6fr and
  1fr. At 1100px and below it stacks, primary first. No third column. Catalog group: Layout.
- **Numeric cells.** One table utility class: right-aligned, `font-variant-numeric: tabular-nums`.
  The page puts it on `th` and `td`. It lives with the existing table rules, not as a
  component. The grouped-table sample uses it on one numeric column.

## Out of scope

- A slot beside the `h1`. Task 424 owns the page header: title and description on the left,
  actions on the right, and `TimeWarpPage` keeps the only `h1`. A status badge sits in
  `Description` or in the definition list.
- The nav collapse toggle. That is task 426.
- A user-facing callout. Static guidance is `FluentMessageBar` (Info or Warning). `DevNote`
  stays the build annotation.
- A back button. The location breadcrumb is the way back.
- A separate sync bar, a product `Button`, or a role-preview control.
- Expandable parent/child rows, toggle-chip filters, checkbox lists, rank rows, activity
  rows, and stat rows. Those stay page markup.
- Domain screens: close grids, export rows, and integration-specific copy.

## Checklist

- [ ] Definition list, adopted on principal detail and role detail
- [ ] Line chart in the `ChartCard` plot slot; chart-frame sample uses it
- [ ] Period stepper, distinct from `SegmentedControl`
- [ ] Compact `IntegrationCard` warning tone; full variant unchanged; no second sync bar
- [ ] Page split, stacks at 1100px
- [ ] Numeric table utility, shown on the grouped-table sample
- [ ] Style-guide part, usage snippet, and `components/overview.md` row for each
- [ ] HtmlRenderer tests; `dev build` 0/0; no client name in source, tests, or skills

## Session

- Created: 3422483 (2026-10-28)
- Specified: follow-up after the portal design review (379, 421, 423, 424)

## Notes

- Source design: the portal's definition grid, revenue line chart, year and day steppers,
  disconnected sync row, wide-plus-narrow dashboard grid, and right-aligned money columns.
- Existing homes: `components/elements/ChartCard.razor`, `components/elements/IntegrationCard.razor`,
  `components/composites/SegmentedControl.razor`, `features/style-guide/style-guide-parts.cs`,
  `components/overview.md`.
- Render test pattern: `tests/container-apps/web/web-spa-integration-tests/components/kpi-card-render-tests.cs`.
