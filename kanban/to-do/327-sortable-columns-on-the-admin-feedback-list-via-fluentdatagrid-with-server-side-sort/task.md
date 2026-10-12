# Sortable columns on the admin feedback list, via FluentDataGrid with server-side sort

## Description

Steve, 2026-10-12, on `/Admin/Feedback`: "FluentUI has sortable columns doesn't it? lets make them
sortable."

Today `features/admin/feedback/pages/FeedbackAdminListPage.razor` renders a plain
`<table class="twe-table">` with `TableHeader` cells (Title, Kind, Status, Filed, Owner) and
server-side paging (`ListFeedback.Query` `Page` / `PageSize`).

**Sorting must be server-side.** The list is paged on the server, so sorting only the current page
would show a wrong order across pages.

## Requirements

- **Contract:** `ListFeedback.Query` gains `SortBy` (a closed enum: Title, Kind, Status, FiledAt)
  and `SortDescending`. The default stays FiledAt descending, which is today's newest-first order.
  The query-string route includes them. There are validator rules for the enum. TWA0002/0003
  nullability must agree.
- **Store:** `IFeedbackStore.ListAllAsync` (`FeedbackListFilter`) takes the sort and applies it
  **before** paging, in both the EF and in-memory stores, with a stable tiebreaker on id. Add an EF
  index only if needed for the common sorts, and record why if one is added.
- **SPA:** replace the table with **`FluentDataGrid`**: `PropertyColumn` / `TemplateColumn` with
  `Sortable="true"` on Title, Kind, Status and Filed. Owner is not sortable: it's a raw principal
  id today.
  - Use the grid's server-side mode (`ItemsProvider`, or equivalent v5 API; check the FluentUI v5
    docs via the fluent-ui-blazor MCP). Each sort or page change **dispatches a
    `FeedbackAdminState` action**. The handler fetches; components only dispatch (TWA0022 /
    TWA0026, "every interaction is a State action").
  - Keep the existing filters and Previous/Next paging, or move to the grid's paginator if v5
    offers one that fits the same action flow. Record the choice.
  - Bind `Loading` to the fetch action's in-flight state (`tw-blazor` already notes the
    `FluentDataGrid Loading=IsLoading` pattern).
  - Title stays a link to `/Admin/Feedback/{id}`.
  - Styling follows `tw-blazor-css-strategy`.
- **Tests:**
  - contract/handler sort (each column, both directions, sort before paging, tiebreaker);
  - SPA state test that a sort dispatch carries `SortBy` / `SortDescending`;
  - Playwright or component check that clicking a header re-orders and toggles the direction
    indicator.
- **Pattern note:** this is the first `FluentDataGrid` with server-side sort in the template. Add a
  short note to `skills/tw-blazor` describing the pattern (grid sort → State action →
  `Query.SortBy`), so the other admin lists can follow it later. Don't convert the other lists in
  this task.

## Checklist

- [ ] `SortBy` / `SortDescending` on `ListFeedback.Query` (+ validator, route)
- [ ] Store sorts before paging (EF + in-memory), stable tiebreaker
- [ ] FluentDataGrid with sortable columns; sort/page dispatch State actions; Loading bound
- [ ] Tests (handler sort, state dispatch, header click)
- [ ] `tw-blazor` pattern note
- [ ] Gates: `dev build` 0/0, `dev test`, `dev template-smoke`, `ganda repo audit`

## Notes

- The Owner column shows a raw principal id. A display name, from profile or principal, is a
  natural follow-up, and it pairs with the admin profiles task filed the same day.

## Session

- Created: 2026-10-12 (cockpit, per Steve's screenshot of `/Admin/Feedback`)
