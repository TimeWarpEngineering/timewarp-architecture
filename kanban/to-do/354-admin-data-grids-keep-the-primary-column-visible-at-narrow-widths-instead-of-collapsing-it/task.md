# Admin data grids keep the primary column visible at narrow widths instead of collapsing it

## Problem (2026-10-14)

On `/Admin/Feedback`, with the Ask panel open, the **Title** column collapses to zero width:
- its header text overlaps "Kind" (it reads "Kitld");
- the title links vanish from the rows.

**Cause:** in `FeedbackAdminListPage.razor` (task 327), Kind, Status, Filed and Owner have fixed
widths totalling **45rem (720px)**: 9 + 9 + 13 + 14rem. Title has none, so it gets the leftover,
which is zero when the card's content area is about 720px. That happens at any narrow viewport, not
only with the panel. `features/admin/principals/pages/PrincipalsPage.razor` (task 328) uses fixed
widths the same way.

## Requirements

- The **primary column** (Title; Principal) keeps a minimum and grows, for example
  `minmax(14rem, 1fr)` via the grid's column template. Header text truncates with an ellipsis and
  never overflows into a neighbour.
- The other columns use flexible sizing (`minmax`/`fr`, smaller minimums). Owner and similar columns
  truncate (they already have tooltips).
- Below the grid's minimum total width, the grid **scrolls horizontally inside its card** instead of
  crushing columns.
- **One rule for every admin grid:** put it in a shared style or component wrapper (follow
  `tw-blazor-css-strategy`), and add a line to `tw-blazor`'s "Server-sorted FluentDataGrid"
  section. Apply it to the feedback and principals grids, and to any other `FluentDataGrid`.
- **Playwright:** at a narrow viewport, and with the Ask panel open, no header cell overlaps its
  neighbour, the primary column's cells are visible (width > 0) and links are clickable, and the
  grid scrolls when below its minimum.

## Checklist

- [x] Primary column min + grow; flexible secondary columns; ellipsis headers
- [x] Horizontal scroll below min width; shared rule for all grids
- [x] Feedback + principals grids updated; tw-blazor note
- [x] Playwright narrow/panel-open checks
- [x] Gates: `dev build` 0/0, `dev test`, `ganda repo audit`

## Session

- Created: 2026-10-14 (cockpit, from Steve's screenshot)
- 2026-10-14 implementer (ganda task work): implemented, gates green.

## Results

- **Shared rule:** new `web-spa/components/composites/DataGridFrame.razor` (+ `.razor.css`). A
  native root with `overflow-x: auto` so the grid scrolls inside its card below its minimum
  width; anchored `::deep` rules give header/body cells `min-width: 0`, clip header cells, and
  drop Fluent's 75px sort-button floor so header text ends in an ellipsis instead of spilling
  into the neighbour.
- **Column sizing:** both grids switch from `DisplayMode.Table` + per-column `Width` to
  `DisplayMode.Grid` + `GridTemplateColumns` minmax tracks (Fluent forbids mixing the two):
  - Feedback: `minmax(14rem, 2fr) minmax(7rem, 0.6fr) minmax(7rem, 0.6fr) minmax(11rem, 1fr) minmax(8rem, 1fr)`
  - Principals: `minmax(14rem, 2fr) minmax(8rem, 1fr) minmax(10rem, 1.2fr) minmax(6rem, 0.5fr) minmax(6rem, 0.5fr) minmax(5rem, 0.4fr)`
  These are the only two `FluentDataGrid`s in the app.
- **Build check:** `web-spa-integration-tests/.../data-grid-frame-guard-tests.cs` fails when a
  `FluentDataGrid` under web-spa is not inside `DataGridFrame`, lacks Grid mode +
  `GridTemplateColumns`, or a column sets `Width`.
- **Docs:** `tw-blazor` "Server-sorted FluentDataGrid" gains item 7; `components/overview.md`
  lists `DataGridFrame`.
- **Playwright:** `FeedbackAttachment_Given_Wasm.AdminGrids_Should_KeepThePrimaryColumn_WhenNarrow_Or_AskIsOpen`
  checks /Admin/Feedback at 1280 with the Ask panel docked, then both admin grids at 640 wide:
  header cells ≥ 40px and non-overlapping, header content clipped to its cell, primary cells
  ≥ 200px with a visible link that opens the item, and the frame scrolls (principals at 640:
  frame 534/784px scrolled 250) while the page itself does not.
- **Gates:** `dev build` 0 warnings 0 errors; `dev test` passed (all suites, incl. the new
  guard and Playwright proof); `ganda repo audit` clean.

### How to validate

**Smoke:**

```bash
dev build
cd tests/container-apps/web/web-spa-integration-tests && dotnet bin/Release/net11.0/web-spa-integration-tests.dll --filter-class DataGridFrameGuard
cd ../web-spa-playwright-tests && dotnet bin/Release/net11.0/web-spa-playwright-tests.dll --filter-method AdminGrids
```

Or by hand: `dev run`, sign in as an admin, open `/Admin/Feedback`, open Ask, then narrow the
window to ~640px; repeat on `/Admin/Principals`.

**Expect:** both test runs pass. By hand, the Title / Principal column stays at least 14rem wide
with clickable links, header labels never overlap (long ones end in "…"), and below the grid's
minimum the grid scrolls sideways inside its card while the page does not. Screenshots land in
`web-spa-playwright-tests/bin/Release/net11.0/playwright-screenshots/FeedbackAttachment_Given_Wasm/admin-grid-*.png`.
