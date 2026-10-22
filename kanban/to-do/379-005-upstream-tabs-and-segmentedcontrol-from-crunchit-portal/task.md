# Upstream Tabs and SegmentedControl from Crunchit portal

## Goal (implementer restatement)

The template gets two generic switchers. `Tabs` is an underline bar for changing sub-views.
`SegmentedControl` is a pill group where one option is on. Both are native elements with a
`twe-` class, a `Class` parameter, and unmatched attributes on the root. They take string
items, an optional text function, the current `Value`, and `ValueChanged`. They never write
`Value` and never navigate. Choosing the item that is already current does not raise the
callback. A page that wants a URL change dispatches a State action; the page renders `Value`
from State.

Left and Right move the selection and wrap. Home and End jump to the ends. A move raises
`ValueChanged` and focus follows that item. The index math lives in one pure helper, with
tests for next, previous, wrap, Home, End, and an empty list. Styles use the existing
`--twe-*` tokens. The active tab uses the accent token, and both controls have a visible
keyboard focus ring. There is no orange literal and no new token.

Nothing in the product pages adopts them. The style guide shows a Tabs card and a Segmented
Control card after Filter Bar, with sample text and callbacks that do not store a value.
Render tests pin the markup. The build, template smoke, and repo audit pass, and the source
and test diff does not name the portal.

## Description

Port two Crunchit components into the template as generic components that expose a value and an
`EventCallback` and nothing else:

- `Tabs`, an underline sub-view switcher.
- `SegmentedControl`, a pill group of mutually exclusive options.

Follow the conventions of 379-001, 379-003 and 379-004:

- A native root with a `twe-` class.
- A `Class` parameter, plus `CaptureUnmatchedValues` `AdditionalAttributes` (as in `Card.razor`).
- None of Crunchit's `DisplayComponent`, `ParentComponent` or `CssBuilder`.
- Isolated `.razor.css` using `--twe-*` tokens.
- Purpose and Design regions.
- Static `HtmlRenderer` render tests.

Each component raises `ValueChanged`. The page handler only dispatches a TimeWarp.State action,
and the page renders `Value` from State.

In Crunchit, both components set `Value` themselves. `ClientsPage` also maps tabs to routes by
calling `NavigationManager.NavigateTo` from the page. Neither pattern comes across. The
components never navigate. A page that maps tabs to URLs dispatches a State action that
navigates, the same way the existing handlers do (for example `sign-in-state.redirect-to-login.cs`).

No template page has tabs or toggles today, so the style guide is the only adopter.

The generated code contains no client name.

## Checklist

- [x] `components/composites/Tabs.razor` with `.razor.css`:
  - Root: `<div class="twe-tabs @Class" role="tablist">`.
  - Each item is a `<button role="tab">` with `aria-selected`.
  - Roving `tabindex`: `0` on the active item, `-1` on the others.
- [x] `components/composites/SegmentedControl.razor` with `.razor.css`:
  - Root: `<div class="twe-segmented @Class" role="radiogroup">`.
  - Each item is a `<button role="radio">` with `aria-checked`.
  - The same roving `tabindex` as Tabs.
- [x] Parameters on both, matching `FilterSelect` from 379-004:
  - `Items` (`IReadOnlyList<string>`, `EditorRequired`).
  - An optional `OptionText` (`Func<string,string>`).
  - `Value`.
  - `ValueChanged` (`EventCallback<string>`).
  - Optional `AriaLabel`.
  - The component never assigns `Value` itself. Choosing the item that is already active does
    not raise the callback.
- [x] Keyboard on both. Left and Right move the selection and wrap at the ends. Home and End jump
      to the first and last item. A move raises `ValueChanged`, which is automatic activation,
      and focus follows the selection. Put the index math in a small pure helper `.cs` (kebab
      name) and unit-test it: next, previous, wrap, Home, End, and an empty list.
- [x] Visible `:focus-visible` styles on both. Crunchit has none.
- [x] Map Crunchit tokens to `--twe-*`:

  | Crunchit token | Template token or role |
  | --- | --- |
  | `paper-2` | segmented track background |
  | `paper` | active segment background |
  | `ink`, `ink-2`, `muted` | text |
  | `rule` | tablist bottom border |
  | `orange`, `orange-deep` | active underline and text, mapped to the template accent (`--twe-primary`), not an orange literal |
  | `text-table-cell` | the existing text-size token |
  | hard-coded `rgba(0,0,0,.06)` shadow | `--twe-shadow-sm` |

  Add a token to `wwwroot/css/tokens.css` only when nothing fits. No hex values, no `<style>`.
- [x] Use native markup, not `FluentTabs`, and record why in the Design region:
  - `FluentTabs` is panel-oriented, and this is a header-only switcher.
  - Matching the look would mean restyling shadow DOM.
  - FluentUI v5 has no segmented control.
- [x] Style guide: add a "Tabs" card and a "Segmented Control" card after "Filter Bar" in
      `StyleGuidePage.razor`. Use neutral sample text and inert callbacks, as the 379-004 demo
      does. Do not extend the StyleGuidePage contract (379-017).
- [x] Add a row for each component to `components/overview.md`, if it lists components.
- [x] Render tests in `tests/container-apps/web/web-spa-integration-tests/components/`, in
      `tabs-render-tests.cs` and `segmented-control-render-tests.cs`. Cases:
  - One tab or radio per item.
  - Only the `Value` item is selected or checked.
  - The `tabindex` values are `0` and `-1`.
  - A null `Value` selects nothing, and the first item gets `tabindex` `0`.
  - `OptionText` is applied.
  - `Class` and `AdditionalAttributes` are merged onto the root.
  - `AriaLabel` lands on the root.
  - No `<style>` element.
- [x] Gates:
  - `./bin/dev build` reports 0 warnings and 0 errors.
  - `./bin/dev template-smoke` passes.
  - `ganda repo audit` passes.
  - `git diff origin/master -- source tests | rg -i crunchit` prints nothing.

## Results

`Tabs` and `SegmentedControl` live under `components/composites/`. Both are native roots with
`Class` and unmatched attributes on that root. They take string items, optional `OptionText`,
`Value`, and `ValueChanged`. They never assign `Value` and never navigate. Choosing the current
item does not raise the callback. `RovingSelection` (`roving-selection.cs`) owns next, previous,
wrap, Home, End, and the empty list. A move raises `ValueChanged` only when the landing item
differs from the current value. The following render focuses that button. Active tab text and
the underline use `--twe-primary`. The segmented track, active pill, text, rule, and shadow use
tokens that were already in `tokens.css`. No new token. The style guide shows both cards after
Filter Bar with sample text and no stored value. `ganda repo audit` was already green; nothing
was turned off.

### How to validate

Smoke:

- `./bin/dev build` — completed successfully, 0 warnings, 0 errors.
- `./bin/dev template-smoke` — SmokeDefault OK, 0 warnings, 0 errors. Generated Jaribu
  aggregators 405/405, 9/9, and 14/14. Template smoke succeeded.
- `ganda repo audit` — 32 passed, 0 failed, 1 skipped.
- From `tests/container-apps/web/web-spa-integration-tests`: `dotnet test -- --filter-class Tabs_Should_`
  (4 passed), `--filter-class SegmentedControl_Should_` (4 passed), and
  `--filter-class RovingSelection_Should_` (9 passed).
- `git diff origin/master -- source tests | rg -i crunchit` — no match.

### Review

Round 1 (effort 3, general reviewer) has 4 open findings: 1 bug, 2 suggestions, and 1 nit.
Disposition is pending until a fix pass and round 2.

- M1 (bug): the root `onkeydown` script cancels Alt, Ctrl, Meta, and Shift arrow, Home, and End
  chords, which the C# handler ignores.
- M2: keep the cancelled keys and the `FromKey` keys in one source.
- M3: the two `@code` blocks are duplicates.
- M4: rename the local variable `MoveResult`.

Re-run during review: Tabs 4/4, SegmentedControl 4/4, RovingSelection 9/9, and the client-name
grep is empty. Paths: `review/review-framework.md`, `review/round-1/general.md`, and
`review/round-1/merged.md`.

### Expect

- Style Guide, after Filter Bar, shows a Tabs card and a Segmented Control card. Tabs reads
  Overview, Activity, and Notes, with Overview selected. The segmented control reads Day, Week,
  and Month, with Week checked. Changing them does not write page fields.
- Left and Right move the selection and wrap. Home and End jump to the ends. Each move raises
  `ValueChanged` and focus follows that item. A page that maps a tab to a URL dispatches a
  State action; the control does not navigate.

## Session

- Created: ses_ebc1f6587ffeC5oxDhO3SRDeMu (2026-10-16)
- Spec written: c2bec172-f0eb-4648-9edb-30256e7756e3 (2026-10-22)
- Implement oracle: Tabs, SegmentedControl, RovingSelection, style-guide cards, and render tests. Build, template-smoke, and audit are green. 2026-10-22.
- Implement oracle: implemented by implementer-cursor (cursor, model grok-4.7-high), session not reported, max-turns uncapped (no --max-turns flag; bound 500), Done — 2026-10-22T11:15:17Z
- Review oracle round 1: claude (claude-opus-5-5), headless ganda task work, effort 3, roster
  general. Session id not exposed. 4 open findings (M1–M4); fix pass requested. 2026-10-22.

## Notes

- Crunchit source is under
  `/home/steve/worktrees/github.com/Crunchitfs/crunchit/master/source/container-apps/web/projects/web-spa/`:
  - `components/Tabs.razor(.css)` and `components/SegmentedControl.razor(.css)`.
  - Tabs is used in `features/clients/pages/ClientsPage.razor(.cs)`, which has the routes
    `/clients/revenue` and `/clients/me-close`, and in `ClientDetailPage.razor`.
  - SegmentedControl is used in `StaffPage.razor`, `DashboardPage.razor`, `ClientsPage.razor`
    and `ClientDetailPage.razor`.
- Model files in the template: `components/elements/Card.razor`,
  `components/composites/FilterSelect.razor`, and
  `tests/.../components/filter-bar-render-tests.cs`.
- Defaults chosen at spec time (2026-10-22), each matching an earlier sibling:
  - Adopt in the style guide only. No invented UI on real pages.
  - String items with `OptionText`, the same shape as `FilterSelect`.
  - `radiogroup` semantics for the segmented control.
- Out of scope: every other 379 sibling, shell slots (379-015), and 379-017.
