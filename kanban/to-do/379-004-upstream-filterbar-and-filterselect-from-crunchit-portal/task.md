# Upstream FilterBar and FilterSelect from Crunchit portal

## Goal (implementer restatement)

List pages should share one brand-neutral filter toolbar. `FilterBar` is a paper bar with
`role="toolbar"`: an optional search box, then whatever controls the page passes as
`ChildContent`. `FilterSelect` is a muted label beside a `FluentSelect<string,string>`. Neither
component stores the filter or fetches. They raise `EventCallback`s, and the page handler only
dispatches TimeWarp.State actions.

Search waits inside `FilterBar`. Each keystroke restarts a 300 ms timer backed by `TimeProvider`,
so a test can complete the wait without sleeping. When the timer fires, `SearchTextChanged` runs
once with the latest text, on the renderer via `InvokeAsync`. Clearing the box or pressing Escape
fires immediately. A newer keystroke cancels the pending callback. Dispose cancels the timer, and
nothing fires after that.

The admin feedback list drops its hand-built filter row for this bar. Status, Kind, and Severity
keep their items, option text, and `data-qa` values. Show closed stays a checkbox in
`ChildContent`. The list query already keeps rows whose title contains `Search`, so this page
shows the box and sends that existing field. No new server filter. The style guide shows the bar
with sample text and callbacks that do not write local fields.

## Description

Port Crunchit's `FilterBar` and `FilterSelect` into the template as brand-neutral components, so
list pages share one filter toolbar.

- `FilterBar` is a paper toolbar with `role="toolbar"`. It holds an optional search box and the
  filter controls passed as `ChildContent`.
- `FilterSelect` is a muted inline label followed by a select.

In Crunchit, search fires a callback on every keystroke. The page then fetches, so typing
"acme" fetches four times.

**Decision (Steve 2026-10-22):** include search, debounced. The component waits for a pause in
typing and then fires `SearchTextChanged` once.

Components hold no state and no fetch logic. They raise `EventCallback`s, as `FluentSelect`
does. The page handler's only job is to dispatch TimeWarp.State actions (`tw-blazor`). Do not
port Crunchit's pattern of page handlers writing local fields (`StatusFilterUi = value`).

The adopting page is `features/admin/feedback/pages/FeedbackAdminListPage.razor`. Its filters
are hand-built today. The generated code contains no client name.

Follow the conventions of 379-001 (`SectionHeader`) and 379-003 (`KpiCard`):

- native root with a `twe-` class
- `Class` parameter and `CaptureUnmatchedValues` `AdditionalAttributes`, as `Card.razor` does
- no Crunchit `DisplayComponent`, `ParentComponent`, or `CssBuilder`
- isolated `.razor.css` with `--twe-*` tokens
- Purpose and Design regions (TWA0004)
- static `HtmlRenderer` render tests

## Checklist

- [x] `components/composites/FilterBar.razor` with `.razor.css`.
  - Root: `<div class="twe-filter-bar @Class" role="toolbar">`.
  - Parameters: `ShowSearch` (default true), `SearchPlaceholder` (default "Search…"),
    `SearchText`, `SearchTextChanged` (`EventCallback<string>`), `SearchDelay` (`TimeSpan`,
    default 300 ms), and `ChildContent`.
  - The search box is FluentUI v5. Choose `FluentSearch`, or `FluentTextInput` with
    `Icons.Regular.Size16.Search`, using the fluent-ui-blazor MCP, and record the choice in
    Design. No Blazicons.
- [x] Debounce, owned by `FilterBar`:
  - Each keystroke restarts the timer. When the timer fires, `SearchTextChanged` fires once with
    the latest text.
  - Clearing the box (the clear button or Escape) fires immediately.
  - A newer keystroke cancels the pending callback.
  - The component implements `IDisposable` or `IAsyncDisposable` and cancels the timer on
    dispose. Nothing fires after dispose.
  - Callbacks go through `InvokeAsync`.
  - The Design region records why this timing lives in the component and why the page keeps
    only action dispatch.
- [x] `components/composites/FilterSelect.razor` with `.razor.css`.
  - Root: `<div class="twe-filter-select @Class">`, holding a muted `<span>` label and a
    `FluentSelect<string,string>`.
  - Parameters: `Label`, `Items` (`EditorRequired`), optional `OptionText`
    (`Func<string,string>`), `Value`, and `ValueChanged`.
  - Pass `AriaLabel="@Label"`.
  - `AdditionalAttributes` (including `data-qa`) are forwarded to the select, so existing
    selectors keep working.
  - Use the same `FluentSelect` API the feedback page already uses. No combobox.
- [x] Tokens:
  - Crunchit `paper`, `radius`, `radius-sm`, `shadow-sm`, `rule`, `background`, `ink`, and
    `muted` map to their `--twe-*` counterparts.
  - `muted-2` maps to `--twe-muted`, unless a new token is clearly needed.
  - The orange focus ring becomes `--twe-primary`.
  - Font sizes use `--twe-text-helper` and `--twe-text-body`. Spacing uses `--twe-space-*`.
  - No hex values in the new CSS.
- [x] FluentUI CSS follows `tw-blazor-css-strategy`:
  - Anchor on the native root, then a targeted `::deep > fluent-select` and the search host for
    size, as `FormField.razor.css` does.
  - Use `::part()` for shadow internals.
  - No `::deep *` and no `<style>` element.
  - Drop Crunchit's data-URI chevron.
- [x] Adopt in `FeedbackAdminListPage.razor`.
  - Replace the horizontal `FluentStack` inside `<Section Title="Filter">` with a `<FilterBar>`.
  - That bar holds three `<FilterSelect>`s: Status, Kind, and Severity. Keep the same `Items`,
    the `StatusLabel`, `KindLabel`, and `SeverityLabel` option text, and the same `data-qa`
    values (`AdminFeedbackStatus`, `AdminFeedbackKind`, `AdminFeedbackSeverity`).
  - The "Show closed" checkbox (`AdminFeedbackIncludeClosed`) stays as `ChildContent`. There is
    no toggle component.
  - The existing handlers stay as they are: `SetFilter`, then `FetchFeedbackList`.
  - Search: only if the feedback list query can already filter by text. Otherwise
    `ShowSearch="false"` there, and the Results say so. Do not add server-side search in this
    task.
  - Keep the `Section` wrapper and its `LoadingActionType`.
- [x] Style guide: add a "Filter Bar" `Card` after "Section Header" in `StyleGuidePage.razor`.
  - It holds a search box and two `FilterSelect`s, with neutral sample text.
  - The demo's callbacks go through style-guide state or are inert. No `@bind` to local fields.
  - Do not extend the StyleGuidePage contract; that is 379-017.
- [x] Add a row for each new component to `components/overview.md`, if that file lists
      components.
- [x] Render tests in `tests/container-apps/web/web-spa-integration-tests/components/`, in
      `filter-bar-render-tests.cs` and `filter-select-render-tests.cs`, modelled on
      `section-header-render-tests.cs`. Cases:
  - The toolbar role and the `twe-` class are present.
  - The search box is hidden when `ShowSearch` is false.
  - The placeholder and value render.
  - `ChildContent` renders.
  - `Class` and `AdditionalAttributes` are merged.
  - The label and every option render.
  - The label is omitted when empty.
  - `OptionText` and the selected value are applied.
  - `data-qa` reaches the select.
- [x] Debounce test, deterministic: inject a `TimeProvider` (or a fake timer), with no real
      sleeps.
  - Several keystrokes produce one callback with the last text.
  - A clear fires immediately.
  - Dispose cancels the pending callback.
- [x] Gates:
  - `./bin/dev build` reports 0 warnings and 0 errors.
  - `./bin/dev template-smoke` passes.
  - `ganda repo audit` passes.
  - `git diff origin/master -- source tests | rg -i crunchit` prints nothing.
  - The existing feedback-admin tests pass, plus a Playwright check that the admin feedback
    filters still filter.

## Results

`FilterBar` and `FilterSelect` live under `components/composites/`. FluentUI 5.0.1 has no
`FluentSearch`, so the search box is `FluentTextInput` (`TextInputType.Search`,
`Icons.Regular.Size16.Search`, `ImmediateDelay` 0). The bar owns the pause via `TimeProvider`.
This FluentUI build renders `FluentSelect` as `fluent-field` around `fluent-dropdown`, so the
isolated CSS and the render tests target that host and still name `fluent-select`.

The admin feedback list uses the bar. `ListFeedback.Query.Search` already keeps rows whose
title contains the text, so the page shows the box and sends that existing field through
`SetFilter`, then `FetchFeedbackList`. Whitespace is stored as null. No new server filter.
Show closed stays a checkbox in `ChildContent`. The style-guide card is sample text with no
callbacks. `ganda repo audit` was already green; nothing was turned off.

### How to validate

Smoke:

- `./bin/dev build --quiet` — completed successfully (warnings are errors).
- `./bin/dev template-smoke` — SmokeDefault OK, 0 warnings, 0 errors. Generated Jaribu
  aggregators 405/405, 9/9, and 14/14. Template smoke succeeded.
- `ganda repo audit` — 32 passed, 0 failed, 1 skipped.
- From `tests/container-apps/web/web-spa-integration-tests`: `dotnet test -- --filter-class Filter`
  (14 passed) and `--filter-class FeedbackAdmin` (11 passed).
- From `tests/container-apps/web/web-spa-playwright-tests`:
  `dotnet test -- --filter-method AdminListFilters_Should_NarrowTheRows` (passed). Kind
  `FeatureRequest` leaves one row; clearing the kind and typing `bug` leaves the bug row.
- `git diff origin/master -- source tests | rg -i crunchit` — no match.

Expect:

- Style Guide, after Section Header, shows a Filter Bar card: search placeholder "Search…",
  value "Report", a Status select on Open, and a Kind select whose empty item reads "All".
  Changing them does not write page fields.
- Admin Feedback keeps the Filter section. The bar has a title search, Status, Kind, Severity,
  and Show closed. `data-qa` stays `AdminFeedbackStatus`, `AdminFeedbackKind`,
  `AdminFeedbackSeverity`, and `AdminFeedbackIncludeClosed`.
- Typing a title waits for the pause, then the grid shows only matching titles. Clearing the
  box or pressing Escape fetches immediately. Status, Kind, Severity, and Show closed still
  narrow the same rows, and they keep the current search text.

## Session

- Created: ses_ebc1f6587ffeC5oxDhO3SRDeMu (2026-10-16)
- Spec written: c2bec172-f0eb-4648-9edb-30256e7756e3 (2026-10-22)

## Notes

- Crunchit source is under
  `/home/steve/worktrees/github.com/Crunchitfs/crunchit/master/source/container-apps/web/projects/web-spa/`:
  - `components/FilterBar.razor` and `components/FilterSelect.razor`, each with its CSS.
  - Usage: `features/clients/pages/ClientsPage.razor` lines 53–64 (search, Status, QBO) and
    `features/staff/pages/StaffPage.razor`.
  - Demo: `features/style-guide/pages/StyleGuidePage.razor` lines 101–104.
- Template paths under `source/container-apps/web/projects/web-spa/`:
  - `components/{composites,forms}/`
  - `features/admin/feedback/pages/FeedbackAdminListPage.razor(.cs)`
  - The actions in `feedback-admin-state.set-filter.cs` and `…fetch-feedback-list.cs`.
  - `wwwroot/css/tokens.css`
- Out of scope:
  - A clear-filters button. Crunchit keeps it page-level.
  - The segmented toggle, which is 379-005.
  - Shell search, which is 379-015.
- Decided defaults (2026-10-22): the checkbox stays as `ChildContent`, and `FluentSelect` rather
  than a combobox.
