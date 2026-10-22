# Upstream Tabs and SegmentedControl from Crunchit portal

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

- [ ] `components/composites/Tabs.razor` with `.razor.css`:
  - Root: `<div class="twe-tabs @Class" role="tablist">`.
  - Each item is a `<button role="tab">` with `aria-selected`.
  - Roving `tabindex`: `0` on the active item, `-1` on the others.
- [ ] `components/composites/SegmentedControl.razor` with `.razor.css`:
  - Root: `<div class="twe-segmented @Class" role="radiogroup">`.
  - Each item is a `<button role="radio">` with `aria-checked`.
  - The same roving `tabindex` as Tabs.
- [ ] Parameters on both, matching `FilterSelect` from 379-004:
  - `Items` (`IReadOnlyList<string>`, `EditorRequired`).
  - An optional `OptionText` (`Func<string,string>`).
  - `Value`.
  - `ValueChanged` (`EventCallback<string>`).
  - Optional `AriaLabel`.
  - The component never assigns `Value` itself. Choosing the item that is already active does
    not raise the callback.
- [ ] Keyboard on both. Left and Right move the selection and wrap at the ends. Home and End jump
      to the first and last item. A move raises `ValueChanged`, which is automatic activation,
      and focus follows the selection. Put the index math in a small pure helper `.cs` (kebab
      name) and unit-test it: next, previous, wrap, Home, End, and an empty list.
- [ ] Visible `:focus-visible` styles on both. Crunchit has none.
- [ ] Map Crunchit tokens to `--twe-*`:

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
- [ ] Use native markup, not `FluentTabs`, and record why in the Design region:
  - `FluentTabs` is panel-oriented, and this is a header-only switcher.
  - Matching the look would mean restyling shadow DOM.
  - FluentUI v5 has no segmented control.
- [ ] Style guide: add a "Tabs" card and a "Segmented Control" card after "Filter Bar" in
      `StyleGuidePage.razor`. Use neutral sample text and inert callbacks, as the 379-004 demo
      does. Do not extend the StyleGuidePage contract (379-017).
- [ ] Add a row for each component to `components/overview.md`, if it lists components.
- [ ] Render tests in `tests/container-apps/web/web-spa-integration-tests/components/`, in
      `tabs-render-tests.cs` and `segmented-control-render-tests.cs`. Cases:
  - One tab or radio per item.
  - Only the `Value` item is selected or checked.
  - The `tabindex` values are `0` and `-1`.
  - A null `Value` selects nothing, and the first item gets `tabindex` `0`.
  - `OptionText` is applied.
  - `Class` and `AdditionalAttributes` are merged onto the root.
  - `AriaLabel` lands on the root.
  - No `<style>` element.
- [ ] Gates:
  - `./bin/dev build` reports 0 warnings and 0 errors.
  - `./bin/dev template-smoke` passes.
  - `ganda repo audit` passes.
  - `git diff origin/master -- source tests | rg -i crunchit` prints nothing.

## Session

- Created: ses_ebc1f6587ffeC5oxDhO3SRDeMu (2026-10-16)
- Spec written: c2bec172-f0eb-4648-9edb-30256e7756e3 (2026-10-22)

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
