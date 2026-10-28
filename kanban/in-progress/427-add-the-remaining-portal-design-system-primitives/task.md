# Add the remaining portal design-system primitives

## Goal (implementer restatement)

A generated app should get six missing portal primitives from the template, so the next product does not copy them into page CSS. Nothing in the new code names a client.

Acceptance:

- A definition list is a wrapping grid. Each item is a term plus arbitrary content, not two strings. A blank term is left out. Principal detail and role detail use it in place of their one-off fact layouts.
- A line chart fills the `ChartCard` plot slot. The page passes points, a label and `ChartSeriesTone` per series, and axis labels that are already formatted. The plot draws gridlines, the lines, and a dot on each series' last point. It formats no money and picks no colors. `ChartCard` stays a frame. The chart-frame sample and its usage snippet show the plot inside `ChartCard`.
- A period stepper is previous, a label, and next. Either end can be disabled. It does not know years, days, or ranges. The page owns the label. It raises `ValueChanged` the way `SegmentedControl` does, and it is not that control.
- Compact `IntegrationCard` takes a tone, default or warning. Warning paints that row with the warning surface. Full is unchanged. There is no second sync bar. The integration-cards sample shows a compact warning row.
- A page split has a primary column and a secondary column, about 1.6fr and 1fr. At 1100px and below it stacks, primary first. There is no third column.
- One table utility class right-aligns a cell and sets tabular numerals. It lives with the existing table rules. The grouped-table sample puts it on one numeric column's `th` and `td`.
- Each new part has a catalog entry, a usage snippet, and an overview row. The numeric class and the compact tone are recorded on the existing table and integration rows.
- HtmlRenderer tests follow the KPI card render tests. `dev build` reports 0 errors and 0 warnings. Source, tests, and skills contain no client name.

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

- [x] Definition list, adopted on principal detail and role detail
- [x] Line chart in the `ChartCard` plot slot; chart-frame sample uses it
- [x] Period stepper, distinct from `SegmentedControl`
- [x] Compact `IntegrationCard` warning tone; full variant unchanged; no second sync bar
- [x] Page split, stacks at 1100px
- [x] Numeric table utility, shown on the grouped-table sample
- [x] Style-guide part, usage snippet, and `components/overview.md` row for each
- [x] HtmlRenderer tests; `dev build` 0/0; no client name in source, tests, or skills

## Session

- Created: 3422483 (2026-10-28)
- Specified: follow-up after the portal design review (379, 421, 423, 424)
- Implementation: grok task-work (2026-10-28). Definition list, line chart, period stepper, page split, compact integration tone, and `twe-numeric`.
- `ganda repo audit --fix` landed kitchen 428 (same holder, drifted from origin-home). Eight claims still warn that they have no usable worktree (070-001, 104-024, 104-025, 104-026, 123, 141, 198, 244). That warning is pre-existing and not fixable from this id. Audit then passed with that advisory.

## Results

The template now carries the six primitives. Principal detail and role detail use `DefinitionList`. The chart-frame sample plots `LineChart` inside `ChartCard`. `PeriodStepper` raises `ValueChanged` and stays separate from `SegmentedControl`. Compact `IntegrationCard` warning uses `IntegrationCardTone.Warning`; full still uses `Status`. `PageSplit` is 1.6fr and 1fr and stacks at 1100px. `twe-numeric` sits with `.twe-table`. Catalog parts are definition list (Data), period stepper (Forms), and page split (Layout). The line chart, the tone, and the numeric class update the existing chart-frame, integration-cards, and grouped-table parts. `./bin/dev build` finished with 0 warnings and 0 errors, and its client-name scan passed. `git diff -- source tests skills` has no client name.

HtmlRenderer, Release, from `tests/container-apps/web/web-spa-integration-tests`: `DefinitionList_Should_` 5/5, `LineChart_Should_` 6/6, `PeriodStepper_Should_` 4/4, `PageSplit_Should_` 4/4, `TableNumeric_Should_` 2/2, `IntegrationCard_Should_` 10/10, `DesignSystemPage_Should_` 6/6, plus `GroupedTable_Should_` 9/9 and `ChartCard_Should_` 5/5. The live design-system page was not opened in a browser. The catalog contract renders every part with HtmlRenderer.

### How to validate

Smoke: `./bin/dev build`, then from `tests/container-apps/web/web-spa-integration-tests` run `dotnet test -c Release -- --filter-class DefinitionList_Should_`, `LineChart_Should_`, `PeriodStepper_Should_`, `PageSplit_Should_`, `TableNumeric_Should_`, `IntegrationCard_Should_`, and `DesignSystemPage_Should_`.

Expect: build reports 0 warnings and 0 errors. Those classes pass (5, 6, 4, 4, 2, 10, and 6). The all-parts page contains the definition list, the line chart inside the chart frame, the period stepper, a compact integration warning row, the page split, and `twe-numeric` on the grouped table, and it does not show the omitted blank term.

## Notes

- Source design: the portal's definition grid, revenue line chart, year and day steppers,
  disconnected sync row, wide-plus-narrow dashboard grid, and right-aligned money columns.
- Existing homes: `components/elements/ChartCard.razor`, `components/elements/IntegrationCard.razor`,
  `components/composites/SegmentedControl.razor`, `features/style-guide/style-guide-parts.cs`,
  `components/overview.md`.
- Render test pattern: `tests/container-apps/web/web-spa-integration-tests/components/kpi-card-render-tests.cs`.
