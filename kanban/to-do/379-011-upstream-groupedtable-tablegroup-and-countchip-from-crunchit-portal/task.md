# Upstream GroupedTable, TableGroup and CountChip from Crunchit portal

## Description

Port Crunchit's `GroupedTable`, `TableGroup` and `CountChip` into the template as native,
brand-neutral components, so a generated app can show "one table, rows sorted into titled groups,
each group with a count" without each page hand-rolling the header strip and the number pill.

Crunchit's `GroupedTable` (`components/GroupedTable.razor`, `.razor.css`) works like this:

- Parameters: `ChildContent` only (it inherits a `ParentComponent` base for class and attribute
  merging).
- Markup: one `<div>` root, a paper container (paper fill, radius, small shadow,
  `overflow: hidden`) that holds a sequence of `TableGroup` sections.
- Header comment: "per Bill's Staff group-by view". No behaviour, no state, no events.

Crunchit's `TableGroup` (`components/TableGroup.razor`, `.razor.css`) works like this:

- Parameters: `Title` (required string), `Count` (`int?`), `Meta` (string), `ChildContent`.
- Markup: a `<div>` root with a header `<div>` (flex row, 12px gap, 12px 22px padding, tinted
  `paper-2` fill, soft rule below) holding a `<span>` title (13.5px, 600), a `<span>` count pill
  when `Count` is not null, and a right-aligned `<span>` meta (12.5px, muted) when set. Then
  `ChildContent`. Groups are separated by a 1px rule; the last has none.
- The count pill CSS is a copy of `CountChip`'s accent variant with a hex `#fff`.
- Not collapsible. No sorting, no group-by logic: the page does the grouping.

Crunchit's `CountChip` (`components/CountChip.razor`, `.razor.css`) works like this:

- Parameters: `Value` (int), `Accent` (bool). Inherits `DisplayComponent`.
- Markup: `<span class="crunchit-count [--accent]">@Value</span>`. A 22px-high pill, 22px minimum
  width, 999px radius, helper-size 600 text. Neutral is `paper-2` fill and `ink` text; accent is
  an orange fill with hex `#fff` text.

How Crunchit uses them (all in `features/staff/pages/StaffPage.razor` plus the style guide):

- Staff group-by view: `GroupedMembers` is an `IGrouping<string, StaffListItemDto>` computed on
  the page (the group key is the staff location type, "On Shore" / "Off Shore"). The page loops
  and renders one `<TableGroup Title="@group.Key" Count="@group.Count()">` per group. **Inside
  each group the page writes its own `<table class="crunchit-table staff-table">` with its own
  `<thead>`**, wrapped in a `data-table-wrap` div.
- `CountChip` neutral in the "Locations" column (`row.AssignedClientCount`) and next to the
  "Hidden M365 accounts" disclosure header. Style guide: four chips (1, 3, 12, and an accent 7),
  plus one Grouped Table card with two groups using `Meta="2 active"`.
- The table cell styles (`.crunchit-table`, `.cell-primary`, `.num`) are global in `app.css`.

Defects in the Crunchit versions:

- Hex literal `#fff` in two stylesheets; `--crunchit-orange`; pixel literals for type sizes and
  padding; `crunchit-*` class names and a "Bill's" comment.
- **Not one table.** Each group is a separate `<table>` with a repeated `<thead>`: column widths
  differ between groups (auto layout per table), a screen reader sees N unrelated tables, and the
  column headers are read N times.
- The group header is a `<div>` of `<span>`s, so the table has no group semantics at all. The
  count is bare text next to the title with no relation to it.
- The count pill is styled twice (once in `CountChip`, once in `TableGroup`).
- Staff rows use `<tr @onclick>`, which is not keyboard reachable. The detail handler
  (`OpenDetailDrawerAsync`) is a page-local method, not a State action. Not part of these
  components, but the style guide sample must not copy it.
- `GroupedTable` has no accessible name.

Template today: there is none of the three. Tables are either `FluentDataGrid` inside
`DataGridFrame` (Principals, Feedback admin, Weather, Superhero), all server-sorted and paged
through State, or a plain `<table class="twe-table">` with `TableHeader` and `Cell`
(`RolesListPage`, `CredentialList`). `StatusBadge` shows a status pill; `RolesListPage` shows
"N permissions" as muted text. Nothing shows a number pill.

COPIC (FluentUI v4) has no grouped table or count badge. Its only table is a `FluentDataGrid` of
modules with no grouping; nothing to borrow.

FluentUI v5 (5.0.1, checked in `Microsoft.FluentUI.AspNetCore.Components.xml`):

- `FluentDataGrid` has **no grouping** (no group-by, no group header row, no row-group type;
  `DataGridRowType` is Default / Header / StickyHeader). A grouped view needs our own markup.
- `FluentCounterBadge` exists (`Count`, `OverflowCount`, `ShowZero`, `ShowEmpty`, `Dot`,
  `ShowWhen`) as does `FluentBadge`. They are decoration badges in the Fluent palette and size,
  and have no neutral or accent look that follows the `--twe-*` tokens. See Decision.

What we take: the paper container, the tinted group header strip with title, count pill and
right-aligned meta, and the neutral and accent number pill.

What we drop:

- `ParentComponent`, `DisplayComponent`, `CssBuilder`, `crunchit-*` variables, `--crunchit-orange`,
  the `#fff` literals, pixel literals for colour and type.
- One table per group and the page-local `<table>` markup inside a group (see Decision).
- The duplicated count pill CSS in `TableGroup`; it renders a `CountChip`.
- The staff-specific rows, `RoleBadge`, `Avatar` cells and the click-row handler.

Follow the conventions of 379-001 through 379-010:

- Native roots with `twe-` classes, a `Class` parameter and
  `[Parameter(CaptureUnmatchedValues = true)] AdditionalAttributes` splatted on the root, as
  `EmptyState` and `KpiCard` do.
- Isolated `.razor.css` with `--twe-*` tokens. No hex values, no `<style>`.
- Purpose and Design regions (TWA0004).
- Static `HtmlRenderer` Jaribu tests in
  `tests/container-apps/web/web-spa-integration-tests/components/`, modelled on
  `kpi-card-render-tests.cs`.
- The generated code contains no client name.

## Decision

- **Slot-based, not data-driven** (Steve, 2026-10-23: the page groups, the components lay out): `GroupedTable` and `TableGroup`
  are presentational leaves with `ChildContent`, like Crunchit. The page groups its own data
  (`GroupBy` in the page or State) and loops. Not a generic `GroupedTable<TItem>` with `Items`,
  `GroupBy` and a row template: it would own grouping and column logic, duplicate what the page
  already does in one LINQ line, and fight `FluentDataGrid`'s server paging.
- **One real `<table>`, groups as `<tbody>`** (default chosen for this task): fixes the Crunchit
  defect.
  - `GroupedTable` renders `<div class="twe-grouped-table" ...>` (paper container, horizontal
    scroll) holding `<table class="twe-table twe-grouped-table__table">`. Its `ChildContent`
    is the page's `<thead>` followed by `TableGroup`s.
  - `TableGroup` renders a `<tbody class="twe-table-group">` whose first row is the group
    header: `<tr class="twe-table-group__header"><th scope="rowgroup" colspan="@ColumnCount">`
    with the title, a `CountChip` when `Count` is set, and the meta. `ChildContent` is the
    group's `<tr>` rows (the page uses `Cell` or `<td>`).
  - Several `<tbody>` in one table is valid HTML, keeps one set of column widths, one header,
    and gives assistive tech a real row group.
- **Parameters** (default chosen for this task):
  - `GroupedTable`: `ColumnCount` (required int, the number of columns in the page's thead),
    `Label` (string, becomes `aria-label` on the `<table>`), `ChildContent`, `Class`,
    `AdditionalAttributes`. `ColumnCount` is passed to the groups with a `CascadingValue`
    (not fixed), so a group does not repeat it.
  - `TableGroup`: `Title` (required), `Count` (`int?`), `Meta` (string), `ChildContent`,
    `Class`, `AdditionalAttributes` (on the `<tbody>`).
  - `CountChip`: `Value` (int), `Accent` (bool), `Class`, `AdditionalAttributes`.
- **Not collapsible** (default chosen for this task): Crunchit's groups do not collapse and no
  template list needs it. Collapse needs per-group expanded state, which is a State action and a
  page-owned `Expanded` / `OnToggle` pair. That is a later task when a page asks.
- **No sorting inside the component.** Row order and group order come from the page.
- **`CountChip` is its own small leaf** (default chosen for this task), not `FluentCounterBadge`:
  the Fluent badge cannot take the `--twe-*` neutral and accent fills without `::deep`/`::part`
  overrides, which costs more than the six-line component. `TableGroup` reuses it for the group
  count, so the pill CSS exists once. `StatusBadge` is a different thing (a status word with a
  dot) and is not extended.
- **Accent maps to the brand**: neutral is `--twe-paper-2` fill with `--twe-ink` text; accent is
  `--twe-primary` fill with `--twe-on-primary` text. No new tokens.
- **Enum files**: none. `Accent` is a bool (two looks, no growth expected).
- **No JavaScript, no events, no `[DirectComponentSideEffect]`.**
- **File names** (TW0001 kebab applies to `.cs` only): `components/elements/GroupedTable.razor`,
  `TableGroup.razor`, `CountChip.razor` and their `.razor.css`, in `components/elements/`
  beside `StatusBadge`.

## Adoption site

None in the template fits, and inventing one would be dishonest:

- `PrincipalsPage` and `FeedbackAdminListPage` are `FluentDataGrid`s sorted and paged on the
  server through State actions. Grouping client-side would break paging and sorting, and
  grouping server-side is a contract change.
- `RolesListPage` and `CredentialList` are plain tables, but their rows have no natural group
  (roles have no category; credentials are one kind).
- Nothing shows a count that would read better as a pill. `RolesListPage`'s "N permissions" text
  is tested by `PermissionCount`; changing it is churn for no gain.

So this task is **style guide only**. A later task adopts the components where a real grouped
list appears (for example a list grouped by status once a page loads all rows). Say so in the
Results.

## Checklist

- [ ] `components/elements/CountChip.razor` and `.razor.css`.
  - `<span class="twe-count-chip @AccentClass @Class" @attributes="AdditionalAttributes">@Value</span>`
    with `AccentClass` = `twe-count-chip--accent` when `Accent`.
  - Inline-flex, centred, `min-width` and `height` from the spacing scale (about 22px, 1.375rem),
    `border-radius: 999px` (the same literal `StatusBadge` uses), `--twe-text-helper`, 600,
    `font-variant-numeric: tabular-nums`. No hex values.
- [ ] `components/elements/TableGroup.razor` and `.razor.css`.
  - `<tbody class="twe-table-group @Class" @attributes="AdditionalAttributes">`, first row
    `<tr class="twe-table-group__header"><th scope="rowgroup" colspan="@ColumnCount">` with a
    flex `<div>` inside the `th` (a `th` cannot be `display: flex` and keep `colspan` reliably):
    title span (`--twe-text-body`, 600, `--twe-ink`), `CountChip` when `Count` is not null, and
    the meta span (`--twe-text-helper`, `--twe-muted`, `margin-inline-start: auto`) when `Meta`
    has text.
  - Header fill `--twe-paper-2`, `--twe-rule` lines, padding from `--twe-space-*`. Group
    separation is a top rule on the header row; none above the first group.
  - Overrides the global `.twe-table th` rule (muted 600, bottom border) for the group header
    only; isolated selectors are more specific, confirm in the browser.
  - `ColumnCount` comes from the cascading value; a missing cascade falls back to 1 so a
    stand-alone render does not throw.
- [ ] `components/elements/GroupedTable.razor` and `.razor.css`.
  - Root `<div class="twe-grouped-table @Class" @attributes="AdditionalAttributes">` (paper,
    `--twe-radius`, `--twe-shadow-sm`, `overflow-x: auto`) containing
    `<table class="twe-table" aria-label="@Label">` and `<CascadingValue Value="ColumnCount">`
    around `ChildContent`. Omit `aria-label` when `Label` is blank.
  - Row hover and cell padding come from the global `.twe-table`; this component adds none.
- [ ] Purpose and Design regions (TWA0004) on all three: one table with a row-group per
      `TableGroup`; page owns grouping, order and cells; no events, no collapse; `CountChip`
      is the one number pill.
- [ ] Style guide: add a "Grouped Table" card (after "Integration Cards" if 379-010 has landed,
      else after "KPI Cards") in `features/style-guide/pages/StyleGuidePage.razor`.
  - One `GroupedTable` (`ColumnCount="3"`, `Label`, `data-qa="StyleGuideGroupedTable"`) with a
    `<thead>` (`TableHeader`), and two or three `TableGroup`s, for example "Open" (Count 2,
    Meta "2 items"), "In progress" (Count 1), and one group with no count. Rows use `Cell` and
    plain text; no row click handler (rows are not interactive in a sample).
  - A small "Count Chip" card or row beside it: 1, 12, 128 neutral, and one `Accent`.
  - Names in the sample are generic (no staff, no places). Do not extend the StyleGuidePage
    contract (379-017).
- [ ] Add rows to `components/overview.md` for `GroupedTable`, `TableGroup` and `CountChip`.
- [ ] Render tests, `components/grouped-table-render-tests.cs`. They pin only what the static
      render shows:
  - Renders one `<table>` and the group `<tbody>`s inside it (count of `<table>` is 1 for two
    groups).
  - `aria-label` is on the table when `Label` is set and absent when blank.
  - Each group header is `th scope="rowgroup"` with `colspan` equal to `ColumnCount`.
  - Title renders; count chip appears only when `Count` is set (zero renders `0`, null renders
    none); meta appears only when it has text.
  - Group rows follow the header row in the same `<tbody>`.
  - Root carries `twe-grouped-table`, `Class` merges and does not replace it, `data-qa` passes
    through, no `<style` in the output, and a script-bearing title stays encoded.
- [ ] Render tests, `components/count-chip-render-tests.cs`: value renders; neutral has no accent
      class; `Accent` adds `twe-count-chip--accent`; `Class` merges; `data-qa` passes through.
- [ ] No Playwright test: there is no interaction (no collapse, no handlers).
- [ ] Gates:
  - `./bin/dev build` reports 0 warnings and 0 errors.
  - `./bin/dev template-smoke` passes.
  - `./bin/dev lint-skills` is not needed unless `skills/` changes.
  - `ganda repo audit` passes.
  - `git diff origin/master -- source tests | rg -i "crunchit|copic"` prints nothing.

## Accessibility

- The group is a real row group: one `<table>`, one `<thead>`, then a `<tbody>` per group whose
  header cell is `<th scope="rowgroup">`. A screen reader announces the group title when moving
  into its rows; column headers are announced once and apply to every group.
- The table has an accessible name through `Label` (`aria-label`). Pages should always pass it;
  a sighted heading above the table does not name the table for assistive tech.
- The count sits inside the group header cell, so it is read with the title ("Open 2"). Put
  words in `Meta` ("2 items") when the bare number would be ambiguous. The chip is decoration
  around a number and adds no role.
- No collapse, so no `aria-expanded`, no focus handling and no keyboard behaviour in these
  components. If a later task adds collapse, the header becomes a button inside the `th` with
  `aria-expanded` and `aria-controls`.
- The component adds no landmark and no heading. The outline stays with the page and section
  headings.
- Rows that open a detail view should use a real link in the first cell (as `RolesListPage`
  does), not `<tr @onclick>`. The sample does not demonstrate a clickable row.
- Contrast: accent is `--twe-on-primary` on `--twe-primary` (white on brand purple); neutral is
  `--twe-ink` on `--twe-paper-2`. Both should pass 4.5:1; confirm and record in the Results.
- Wide tables scroll sideways inside the container (`overflow-x: auto`). A scroll container with
  no focusable child is not keyboard scrollable in some browsers; if the table has no link or
  button, add `tabindex="0"` and a `role="region"` with the label only after checking the
  behaviour, not by default.
- Render tests pin markup only (roles, scope, colspan, attributes). They cannot see layout,
  scrolling or contrast.

## Session

- Created: ses_ebc1f6587ffeC5oxDhO3SRDeMu (2026-10-16)
- Spec drafted: c2bec172-f0eb-4648-9edb-30256e7756e3 (2026-10-23)

## Notes

- Crunchit source is under
  `/home/steve/worktrees/github.com/Crunchitfs/crunchit/master/source/container-apps/web/projects/web-spa/`:
  - `components/GroupedTable.razor(.css)`, `components/TableGroup.razor(.css)`,
    `components/CountChip.razor(.css)`.
  - Usage: `features/staff/pages/StaffPage.razor` (group-by loop at about line 124; chip at
    about lines 164 and 180); both in `features/style-guide/pages/StyleGuidePage.razor`.
  - Global table styles: `wwwroot/css/app.css` (`.crunchit-table`).
- COPIC has no equivalent; checked, nothing to port.
- Template targets:
  - `source/container-apps/web/projects/web-spa/components/elements/` (new files; `StatusBadge`
    stays as is).
  - `components/Cell.razor` and `components/TableHeader.razor` exist for the sample rows.
  - `wwwroot/css/app.css` holds the global `.twe-table`; do not edit it unless the group header
    cannot override it with the isolated selectors.
  - `features/style-guide/pages/StyleGuidePage.razor`, `components/overview.md`.
- FluentUI v5 (5.0.1): `FluentDataGrid` has no grouping; `FluentCounterBadge` and `FluentBadge`
  exist but are not used. Only template components and the plain table are needed. The
  implementer re-checks this if the package pin moves.
- Boundaries: do not change `DataGridFrame`, `FluentDataGrid` pages, `StatusBadge`, the shell
  (379-015) or the StyleGuidePage contract (379-017).
- Parent 379 stays in-progress until all its children are done.
