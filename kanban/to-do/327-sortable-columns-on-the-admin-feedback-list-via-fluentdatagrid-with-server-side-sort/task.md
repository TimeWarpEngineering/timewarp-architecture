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

- [x] `SortBy` / `SortDescending` on `ListFeedback.Query` (+ validator, route)
- [x] Store sorts before paging (EF + in-memory), stable tiebreaker
- [x] FluentDataGrid with sortable columns; sort/page dispatch State actions; Loading bound
- [x] Tests (handler sort, state dispatch, header click)
- [x] `tw-blazor` pattern note
- [x] Gates: `dev build` 0/0, `dev test`, `dev template-smoke`, `ganda repo audit`

## Notes

- The Owner column shows a raw principal id. A display name, from profile or principal, is a
  natural follow-up, and it pairs with the admin profiles task filed the same day.

## Session

- Created: 2026-10-12 (cockpit, per Steve's screenshot of `/Admin/Feedback`)
- 2026-10-13: implemented under `ganda task work` (implement oracle). All gates green.

## Results

### What changed

- **Contract:** new `FeedbackListSortColumn` enum (`FiledAt`, `Title`, `Kind`, `Status`),
  `list-feedback/feedback-list-sort-column-contracts.cs`. `ListFeedback.Query` has `SortBy`
  (default `FiledAt`) and `SortDescending` (default `true`). Both are non-nullable with real
  defaults, so TWA0002/0003 have nothing to flag. Both go in the query string. The validator
  has `SortBy.IsInEnum()`.
- **Store:** `FeedbackListFilter` gains `SortBy` / `SortDescending`, optional with the same
  defaults. `EfFeedbackStore` and `InMemoryFeedbackStore` order the whole match set before
  `Skip`/`Take`: primary column, then `FiledAt` descending (when the primary is not FiledAt),
  then id ascending. Kind and Status order by enum name, because EF stores them as text.
  In-memory Title uses `OrdinalIgnoreCase`; Postgres uses the database collation (recorded in
  the store's Design region). **No EF index added.** The admin list filters on status first
  (existing `(Status, FiledAt)` index), the table stays small, and a per-column index would
  slow every filing for a rare view. Recorded in `EfFeedbackStore` and the entity configuration.
- **SPA:** `FeedbackAdminState` has `SortBy` / `SortDescending` and a new `SetSort` action
  (resets Page to 1). `FetchFeedbackList` puts them on the query. `FeedbackAdminListPage` now
  renders a `FluentDataGrid` with `Items` set to the state's page and `RefreshItems`. With
  `RefreshItems` set, the grid does not sort or page the rows itself. The handler maps the
  primary sort column (`ColumnId` is the enum name) and dispatches `SetSort`, then
  `FetchFeedbackList`, only when the sort differs from state. Title, Kind, Status and Filed
  are sortable `TemplateColumn`s. `PropertyColumn` lowercases enums, so it is not used. Owner
  is not sortable, and it truncates with a tooltip. `IsDefaultSortColumn` and
  `InitialSortDirection` follow state, so a re-created grid shows the stored sort. `Loading` is
  `IsAnyActive(FetchFeedbackList)`. The Feedback `Section` no longer has a
  `LoadingActionType`, because Section would replace (destroy) the grid with a spinner.
- **Paging choice:** kept the Previous/Next buttons and `SetPage`. `FluentPaginator` drives
  the grid's own `PaginationState`, which would be a second copy of Page beside
  `FeedbackAdminState`. Recorded in `FeedbackAdminListPage.razor.cs`.
- **Pattern note:** `skills/tw-blazor/SKILL.md` has a new "Server-sorted FluentDataGrid"
  section.
- **Boyscout:** bumped `TimeWarp.Nuru` / `TimeWarp.Nuru.DevCli` 3.0.0-beta.79 → beta.80 in
  `Directory.Packages.props`. Without it, `ganda repo audit` fails. Task 332 has the same scope,
  so it can close as done by this change, or no-op after this merges.

### Tests

- `features/feedback/feedback-triage-tests.cs` covers the handler with the in-memory store:
  each column in both directions, the default order, sorting before paging (Title asc, page
  2), and the id tiebreaker on a Title+Kind+FiledAt tie. It also checks that the validator
  rejects an undefined `SortBy`, and that the query string carries `SortBy` /
  `SortDescending`.
- `web-infrastructure-tests/feedback-postgres-persistence-tests.cs` covers the same sort in
  live Postgres (Testcontainers): sorting in SQL before paging, and the tiebreaker staying
  ascending in both directions.
- `web-spa-integration-tests/.../feedback-admin-state-tests.cs` checks that a `SetSort`
  dispatch puts `SortBy` / `SortDescending` on the fetch query, resets Page, and survives a
  filter change.
- `web-spa-playwright-tests/feedback-attachment-playwright-tests.cs` has
  `AdminListHeaderClick_Should_ReorderRows_And_ToggleDirection`. It files two items, then
  clicks the Title header. The rows re-order to Alpha, Zulu and Title's `aria-sort` is
  `ascending`. A second click gives `descending` and Zulu, Alpha.

### Gates (2026-10-13, this worktree)

- `dev build`: 0 warnings, 0 errors
- `dev test`: all projects passed
- `dev template-smoke`: SUCCEEDED
- `ganda repo audit`: pass, after the Nuru bump
- `dev check-version`: source 2.0.0-beta.20 is ahead of NuGet beta.19, so no bump needed

### How to validate

**Smoke:**

```bash
dotnet run source/container-apps/web/features/feedback/feedback-triage-tests.cs
dotnet run --project tests/container-apps/web/web-spa-integration-tests -- --filter-class FeedbackAdminActions_Should_
dotnet run --project tests/container-apps/web/web-infrastructure-tests -- --filter-class Feedback_Postgres
dotnet run --project tests/container-apps/web/web-spa-playwright-tests -- --filter-method AdminListHeaderClick
```

Manual: `dev run`, sign in as an administrator, file two or more feedback items, then open
`/Admin/Feedback`. Click the Title, Kind, Status and Filed headers.

**Expect:**

- The triage runfile passes 29/29, the SPA state class 8/8, the Postgres class 5/5 (or prints
  `[SKIP]` without Docker), and the Playwright test 1/1.
- Manual: the Filed header shows a down arrow on first load (newest first). Clicking a header
  re-fetches from the server and orders rows across all pages, not just the visible page.
  Clicking again flips the arrow and the order. Page resets to 1 on every sort change. Owner
  has no sort button. Title links still open `/Admin/Feedback/{id}`.
