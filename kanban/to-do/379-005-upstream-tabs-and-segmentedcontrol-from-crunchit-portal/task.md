# Upstream Tabs and SegmentedControl from Crunchit portal

## Goal (implementer restatement)

The template ships two generic switchers and nothing else adopts them. `Tabs` is an underline
bar for changing sub-views. `SegmentedControl` is a pill group with one option on. Each is a
native root with a `twe-` class, a `Class` parameter, and unmatched attributes on that root.
Callers pass string items, an optional text function, `Value`, and `ValueChanged`. The control
never writes `Value` and never navigates. Choosing the item that is already current does not
raise the callback. A page that wants a URL change dispatches a State action and renders
`Value` from State.

Left and Right move the selection and wrap. Home and End jump to the ends. A move raises
`ValueChanged` and focus follows that item. Those four keys are cancelled only when no Alt,
Ctrl, Meta, or Shift modifier is held, so browser Back/Forward and page scroll still work.
The key names live once on `RovingSelection`. The cancel script is built from that list, and
both roots bind that script. The index math stays in that helper, with tests for next,
previous, wrap, Home, End, an empty list, and every cancelled key. Parameters, focus, choose,
and key handling live on one abstract base. Each razor file keeps its own markup and ARIA
roles. The method-local move is camelCase.

Styles use the existing `--twe-*` tokens. The active tab uses the accent token, and both
controls have a visible keyboard focus ring. There is no orange literal and no new token.
The style guide shows a Tabs card and a Segmented Control card after Filter Bar, with sample
text and callbacks that do not store a value. Render tests pin the markup, including the
cancel script. The build, template smoke, and repo audit pass, and the source and test diff
does not name the portal.

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
wrap, Home, End, and the empty list. `HandledKeys` is the only key list. `CancelScript` is built
from that list, and `FromKey` returns a move only for a key on it. The script calls
`preventDefault` only when Alt, Ctrl, Meta, and Shift are all up. Both roots bind that script.
`RovingValueGroup` owns the parameters, focus, choose, and key handling. Each razor file keeps
its markup and ARIA roles. The method-local move is `move`. A move raises `ValueChanged` only
when the landing item differs from the current value. The following render focuses that button.
Active tab text and the underline use `--twe-primary`. The segmented track, active pill, text,
rule, and shadow use tokens that were already in `tokens.css`. No new token. The style guide
shows both cards after Filter Bar with sample text and no stored value. `ganda repo audit` was
already green; nothing was turned off.

### How to validate

Smoke:

- `./bin/dev build` — completed successfully, 0 warnings, 0 errors.
- `./bin/dev template-smoke` — SmokeDefault OK, 0 warnings, 0 errors. Generated Jaribu
  aggregators 405/405, 9/9, and 14/14. Template smoke succeeded.
- `ganda repo audit` — 32 passed, 0 failed, 1 skipped.
- From `tests/container-apps/web/web-spa-integration-tests`: `dotnet test -c Debug --nologo -- --filter-class Tabs_Should_`
  (5 passed), `--filter-class SegmentedControl_Should_` (5 passed), and
  `--filter-class RovingSelection_Should_` (10 passed).
- `git diff origin/master -- source tests | rg -i crunchit` — no match.

### Review

Round 1 (effort 3, general reviewer) reported 4 findings. This fix pass implements them.
Round 2 has not run. Paths: `review/review-framework.md`, `review/round-1/general.md`, and
`review/round-1/merged.md`.

- M1 (bug): `CancelScript` calls `preventDefault` only when `altKey`, `ctrlKey`, `metaKey`, and `shiftKey` are all false. Both render suites read that guard off the root `onkeydown`.
- M2: `HandledKeys` is the key list. `CancelScript` is built from it, and `FromKey` ignores any other key. Both roots bind `CancelScript`.
- M3: `RovingValueGroup` owns the shared parameters, focus, choose, key handling, and Design prose. `RovingMarkup` shares the render parsers.
- M4: the pattern variable is `move`.

### Expect

- Style Guide, after Filter Bar, shows a Tabs card and a Segmented Control card. Tabs reads
  Overview, Activity, and Notes, with Overview selected. The segmented control reads Day, Week,
  and Month, with Week checked. Changing them does not write page fields.
- Left and Right move the selection and wrap. Home and End jump to the ends. Each unmodified
  move raises `ValueChanged` and focus follows that item. Alt+Left and Alt+Right still go to
  the browser. Ctrl+Home and Ctrl+End still scroll the page. A page that maps a tab to a URL
  dispatches a State action; the control does not navigate.

## Session

- Created: ses_ebc1f6587ffeC5oxDhO3SRDeMu (2026-10-16)
- Spec written: c2bec172-f0eb-4648-9edb-30256e7756e3 (2026-10-22)
- Implement oracle: Tabs, SegmentedControl, RovingSelection, style-guide cards, and render tests. Build, template-smoke, and audit are green. 2026-10-22.
- Implement oracle: implemented by implementer-cursor (cursor, model grok-4.7-high), session not reported, max-turns uncapped (no --max-turns flag; bound 500), Done — 2026-10-22T11:15:17Z
- Review oracle round 1: claude (claude-opus-5-5), headless ganda task work, effort 3, roster
  general. Session id not exposed. 4 open findings (M1–M4); fix pass requested. 2026-10-22.
- Review oracle: review by reviewer-claude (claude, model claude-opus-5-5), session not reported, max-turns 200 — 2026-10-22T11:18:28Z
- Fix pass (implement-fix-2): modifier guard, one key list, `RovingValueGroup`, and camelCase `move`. Build, template-smoke, and audit are green. 2026-10-22.

## Review

- Round 1: fix pass implemented M1, M2, M3, and M4. Round 2 has not run.

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
