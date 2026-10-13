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

- [ ] Primary column min + grow; flexible secondary columns; ellipsis headers
- [ ] Horizontal scroll below min width; shared rule for all grids
- [ ] Feedback + principals grids updated; tw-blazor note
- [ ] Playwright narrow/panel-open checks
- [ ] Gates: `dev build` 0/0, `dev test`, `ganda repo audit`

## Session

- Created: 2026-10-14 (cockpit, from Steve's screenshot)
