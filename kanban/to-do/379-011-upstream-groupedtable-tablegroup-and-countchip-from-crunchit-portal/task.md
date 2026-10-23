# Upstream GroupedTable, TableGroup and CountChip from Crunchit portal

## Goal (implementer restatement)

A generated app should be able to show one table whose rows are already sorted into titled groups, each group with a count, without the page building the paper container, the header strip, or the number pill itself. Three native leaves do that. `GroupedTable` is the paper container and the single `<table>`, with an accessible name from `Label` and the page's `<thead>` plus `TableGroup`s as its content. `TableGroup` is one `<tbody>`: a header cell (`th scope="rowgroup"`) spanning the cascaded column count, then the page's own rows. `CountChip` is the one number pill, neutral or accent, and the group header uses it for the count.

The page owns grouping, order, and cells. These components have no group-by, no sort, no collapse, and no events. The column count travels only under the cascade name `TweGroupedTableColumnCount`. A missing cascade, a non-positive value, or any other `int` cascade falls back to one column, so a group rendered on its own still draws and does not borrow an ancestor's number. Cell padding and rules come from the global `.twe-table` rule. There is no product page in the template whose rows form a natural group, so the only adoption is the style guide, plus a row for each component in the components overview. Render tests pin the static markup, including a stand-alone group under an unrelated `CascadingValue<int>`. `dev build` is 0 warnings and 0 errors, `dev template-smoke` passes, `ganda repo audit` passes, and the source and test diff contains no client name.

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
    `AdditionalAttributes`. `ColumnCount` is passed to the groups with a named `CascadingValue`
    (`TweGroupedTableColumnCount`, not fixed), so a group does not repeat it and does not
    take an unrelated `int` cascade.
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

- [x] `components/elements/CountChip.razor` and `.razor.css`.
  - `<span class="twe-count-chip @AccentClass @Class" @attributes="AdditionalAttributes">@Value</span>`
    with `AccentClass` = `twe-count-chip--accent` when `Accent`.
  - Inline-flex, centred, `min-width` and `height` from the spacing scale (about 22px, 1.375rem),
    `border-radius: 999px` (the same literal `StatusBadge` uses), `--twe-text-helper`, 600,
    `font-variant-numeric: tabular-nums`. No hex values.
- [x] `components/elements/TableGroup.razor` and `.razor.css`.
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
  - `ColumnCount` comes from the cascading value named `TweGroupedTableColumnCount`; a
    missing cascade, or an unrelated unnamed `int`, falls back to 1 so a stand-alone render
    does not throw.
- [x] `components/elements/GroupedTable.razor` and `.razor.css`.
  - Root `<div class="twe-grouped-table @Class" @attributes="AdditionalAttributes">` (paper,
    `--twe-radius`, `--twe-shadow-sm`, `overflow-x: auto`) containing
    `<table class="twe-table" aria-label="@Label">` and a `CascadingValue` named
    `TweGroupedTableColumnCount` around `ChildContent`. Omit `aria-label` when `Label` is blank.
  - Cell padding and rules come from the global `.twe-table`; this component adds none.
- [x] Purpose and Design regions (TWA0004) on all three: one table with a row-group per
      `TableGroup`; page owns grouping, order and cells; no events, no collapse; `CountChip`
      is the one number pill.
- [x] Style guide: add a "Grouped Table" card (after "Integration Cards" if 379-010 has landed,
      else after "KPI Cards") in `features/style-guide/pages/StyleGuidePage.razor`.
  - One `GroupedTable` (`ColumnCount="3"`, `Label`, `data-qa="StyleGuideGroupedTable"`) with a
    `<thead>` (`TableHeader`), and two or three `TableGroup`s, for example "Open" (Count 2,
    Meta "2 items"), "In progress" (Count 1), and one group with no count. Rows use `Cell` and
    plain text; no row click handler (rows are not interactive in a sample).
  - A small "Count Chip" card or row beside it: 1, 12, 128 neutral, and one `Accent`.
  - Names in the sample are generic (no staff, no places). Do not extend the StyleGuidePage
    contract (379-017).
- [x] Add rows to `components/overview.md` for `GroupedTable`, `TableGroup` and `CountChip`.
- [x] Render tests, `components/grouped-table-render-tests.cs`. They pin only what the static
      render shows:
  - Renders one `<table>` and the group `<tbody>`s inside it (count of `<table>` is 1 for two
    groups).
  - `aria-label` is on the table when `Label` is set and absent when blank.
  - Each group header is `th scope="rowgroup"` with `colspan` equal to `ColumnCount`.
  - Title renders; count chip appears only when `Count` is set (zero renders `0`, null renders
    none); meta appears only when it has text.
  - Group rows follow the header row in the same `<tbody>`.
  - A stand-alone group under an unrelated `CascadingValue<int>` still spans one column.
  - Root carries `twe-grouped-table`, `Class` merges and does not replace it, `data-qa` passes
    through, no `<style` in the output, and a script-bearing title stays encoded.
- [x] Render tests, `components/count-chip-render-tests.cs`: value renders; neutral has no accent
      class; `Accent` adds `twe-count-chip--accent`; `Class` merges; `data-qa` passes through.
- [x] No Playwright test: there is no interaction (no collapse, no handlers).
- [x] Gates:
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
- Implemented: grouped table, row group, and count chip; style guide only (2026-10-23)
- Implement oracle: implemented by implementer-cursor (cursor, model grok-4.7-high), session not reported, max-turns uncapped (no --max-turns flag; bound 500), Done — 2026-10-23T17:15:04Z
- Review round 1 (2026-10-24): review oracle, Claude Code claude-opus-5-5, effort 2, roster general. Open: M1 suggestion (named ColumnCount cascade), M2 nit (false row-hover claim in a Design comment). No disposition yet; fix pass requested. Artifacts: `review/review-framework.md`, `review/round-1/general.md`, `review/round-1/merged.md`.
- Review oracle: review by reviewer-claude (claude, model claude-opus-5-5), session not reported, max-turns 120 — 2026-10-23T17:17:43Z
- Fix pass (2026-10-24): M1 names the column cascade `TweGroupedTableColumnCount`. M2 drops the row-hover claim. Implement-fix-2 oracle.
- Review round 2 (2026-10-24): review oracle, Claude Code claude-opus-5-5, effort 1, roster general, max-turns 80. Scope: fix delta `c9f69e6f7..d9678e5a8`. M1 and M2 fixed, no new findings. `GroupedTable_Should_` re-run 9 of 9 pass. Disposition: clean. Artifacts: `review/round-2/general.md`, `review/round-2/merged.md`, `review/disposition.md`.

## Results

`GroupedTable`, `TableGroup`, and `CountChip` are native leaves under `components/elements`. `GroupedTable` is a paper container (`twe-grouped-table`: `--twe-paper`, `--twe-radius`, `--twe-shadow-sm`, `overflow-x: auto`) around one `<table class="twe-table">`. `Label` becomes `aria-label` on that table, and a blank label omits the attribute. `ColumnCount` cascades to the groups under the name `TweGroupedTableColumnCount` (`TableGroup.ColumnCountCascadeName`) and is not fixed. An unnamed `int` cascade does not set the span. `TableGroup` is a `<tbody>`. Its first row is `<th scope="rowgroup">` spanning the cascaded column count, with the title, an accent `CountChip` when `Count` is set (including zero), and the meta when it has text. A missing cascade, a non-positive value, or an unrelated `CascadingValue<int>` spans one column. `CountChip` is the one number pill: neutral is `--twe-paper-2` / `--twe-ink`, accent is `--twe-primary` / `--twe-on-primary`. The group header uses the accent chip because the header fill is `--twe-paper-2`, the same fill as the neutral chip. No new tokens. The pill is `1.375rem` (22px is not a spacing step). Header padding is `--twe-space-3` by `--twe-space-6` (12px by 24px). The chip's side padding is half of `--twe-space-2`. No events, no collapse, no JavaScript.

FluentUI stays at 5.0.1, so the spec's finding stands: `FluentDataGrid` has no grouping, and `FluentCounterBadge` is not used. No product page in the template has a natural group, so nothing but the style guide adopts these components. A later task can adopt them where a page loads a full list that groups. The style guide, after Integration Cards, shows a three-column table labeled "Work items" (`data-qa="StyleGuideGroupedTable"`) with Open (count 2, meta "2 items"), In progress (count 1), and Closed (no count), plus a Count Chip card with 1, 12, 128, and an accent 7. Sample names are generic. `components/overview.md` has a row for each component.

Review round 1 fix pass: M1 names the cascade on both sides, so a stand-alone `TableGroup` under a `CascadingValue<int>` of 7 still spans one column, and a descendant with an unnamed `int` cascade does not receive the column count. M2 drops "row hover" from the `GroupedTable` Design region and the CSS header. `app.css` `.twe-table` supplies cell padding and rules, and it has no row-hover rule. The signed-in Style Guide was not reopened. The visible sample is unchanged, and the new case is the HtmlRenderer test.

The isolated group-header rule beats the global `.twe-table th` rule. In Chromium, with the compiled scope attributes, a column header stays muted `#767a86` at `8px 12px`. A group header is ink `#1a1d28` on paper-2 `#f5f5f7` at `12px 24px`. The first group has no top border. The next group has a 1px top border. Both keep a bottom border. Accent text `#ffffff` on `#55409c` is 8.08:1. Neutral ink `#1a1d28` on paper-2 `#f5f5f7` is 15.43:1. Both meet 4.5:1. Meta uses the existing `--twe-muted` token, which measures 3.94:1 on paper-2. That token is 4.29:1 on paper as well, so this task leaves it. A nowrap cell inside a 280px frame scrolled (`scrollWidth` 854, `clientWidth` 280). The signed-in Style Guide route was not opened. Markup is pinned by the HtmlRenderer tests. There is no Playwright test.

Review disposition: **clean** after 2 rounds, with a general reviewer in both. Round 1 used effort 2 and round 2 used effort 1. Final counts: bug 0. Suggestion 1, fixed (M1, the named column cascade). Nit 1, fixed (M2, the row-hover comment). 0 open, 0 wontfix. See `review/round-2/merged.md` and `review/disposition.md`.

### How to validate

**Smoke**

```bash
cd tests/container-apps/web/web-spa-integration-tests && dotnet test -c Release --no-build -- --filter-class CountChip_Should_
cd tests/container-apps/web/web-spa-integration-tests && dotnet test -c Release --no-build -- --filter-class GroupedTable_Should_
```

**Expect:** `CountChip_Should_` 4 passed, 0 failed. The value renders, including zero. Neutral has no `twe-count-chip--accent`. `Accent` adds that class. `Class` merges and `data-qa` passes through. `GroupedTable_Should_` 9 passed, 0 failed. Two groups render one `<table>` and two `<tbody>` elements. `aria-label` is on the table when `Label` has text and absent when it is blank. Each header is `th scope="rowgroup"` with `colspan` equal to `ColumnCount`. A group rendered alone spans one column. A group rendered under an unrelated `CascadingValue<int>` of 7 still has `colspan="1"` and does not have `colspan="7"`. The title renders. The count chip, with `twe-count-chip--accent`, appears for 2 and for 0, and is absent when `Count` is null. Meta appears only when it has text. Group rows follow the header in the same `<tbody>`. The root keeps `twe-grouped-table` when `Class` is set, `data-qa` passes through, the output has no `<style`, and a title of `<script>` stays `&lt;script&gt;` in the raw markup.

**Automated gate**

```bash
./bin/dev build
./bin/dev template-smoke
ganda repo audit
git diff origin/master -- source tests | rg -i "crunchit|copic"
```

`dev build`: 0 warnings, 0 errors. `dev template-smoke`: `Template smoke SUCCEEDED` (SmokeDefault). `ganda repo audit`: Passed 32, Failed 0, Skipped 1. The client-name search prints nothing. `CountChip_Should_`: 4 passed, 0 failed. `GroupedTable_Should_`: 9 passed, 0 failed. `lint-skills` was not run; `skills/` did not change.

## Review

- Round 1: Blocked — open M1, M2
- Round 1 fix pass: M1 and M2 are implemented.
- Round 2: Done
- **Review disposition: clean.** Two rounds. Effort 2 in round 1 and effort 1 in round 2. The roster was general in both rounds. Final counts: bug 0, suggestion 1 fixed (M1), nit 1 fixed (M2). 0 open, 0 wontfix. Paths: `review/review-framework.md`, `review/round-2/merged.md`, `review/disposition.md`.
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
