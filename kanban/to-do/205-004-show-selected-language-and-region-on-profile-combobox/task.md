# Show selected Language and Region on Profile Combobox

## Description

Human demo after **205-003** (PR #330, Save no longer 401): `/Profile` **Save persists**, but
Language and Region do not show the current selection in the closed field. The control
shows the search placeholder (`Search languages` / `Search regions`). Opening the list
and scrolling/filtering finds the correct selected option. Theme (`FluentSelect`) already
shows `System` / `Light` / `Dark`.

Resting UX must be the **selected catalog label**, not a search prompt. Search is how you
*change* the value on a long ISO list, not how you *read* it.

## Requirements

### Resting state (closed control)

After load, after Save, and after leaving `/Profile` and coming back:

- Language shows the catalog **label** for the stored tag (e.g. `English (United States)`
  for `en-US`, `Thai (Thailand)` for `th-TH`) — not `Search languages`, not a blank, not
  only the raw tag unless that is also the label.
- Region shows the catalog **label** (e.g. `United States` for `US`, `Thailand` for `TH`).
- Theme stays as today (`FluentSelect` showing the selected label).

### Search still works

- The ISO catalogs are hundreds of rows. Keep a searchable picker so Thai is findable.
- Placeholder “Search …” is fine **only while the field is focused / empty of a
  selection**. It must not be the resting display when `Details.Language` / `Details.Region`
  already have a catalog value.

### Why this happens (start here, do not lock to one patch)

`ProfilePage.razor` binds:

```xml
<FluentCombobox TOption="ProfileCatalog.Entry" TValue="string"
  Items="@ProfileCatalog.Languages"
  OptionValue="@(static entry => entry?.Code)"
  OptionText="@(static entry => entry?.Label ?? string.Empty)"
  @bind-Value="Details.Language"
  Placeholder="Search languages" … />
```

Fluent UI Blazor **v5** Combobox docs (MCP `get_component_details`): *“Because combobox
inputs always allow people to enter information, the selections will not replace the
placeholder text by default.”* Theme is `FluentSelect` (short list) and does replace.

Possible fixes (implementer picks the smallest that actually shows the label):

1. Combobox parameters so the closed input displays `OptionText` for the bound `Value`
   (e.g. `SelectedItems` / current-value / no Placeholder when selected). Consult Fluent
   UI Blazor MCP for v5 `FluentCombobox` — do not guess v4 `SelectedOption`.
2. If Combobox cannot show a selection by design, **do not** leave a search-only resting
   state. Use a control (or composition) that is searchable **and** displays the selected
   label when closed. A line of text under the field is a last resort, not the first.
3. Do **not** dump the full ISO catalog into an unscrolled `FluentSelect` unless you prove
   it remains usable (typeahead / virtualization). Unsearchable 800-row selects fail Thai.

`FreeOption` stays unset — junk like `en-US asdfasdf` still cannot be submitted.

### Locked (do not regress)

- Full BCL ISO catalogs (205-002). Theme `system|light|dark`.
- Identity-session cookie on PUT (205-003). Do not turn `Authentication:UseMock` on.
- Passkey / session / token never take `IProfileStore`.
- Stored Language is still not applied as UI culture (`SetIsoCulture` stays `en-US`).

### Tests / demo

- Prefer a page/component test if the SPA test host can assert the closed combobox
  display text. If not, document a live `/Profile` How to validate that a stranger can run.
- Bump template-smoke web-jaribu expected count only if the aggregator suite grows.

## Checklist

- [ ] Closed Language/Region show selected catalog labels after load, Save, and revisit
- [ ] Search still finds Thai (and other ISO rows); junk still rejected
- [ ] Theme Select unchanged
- [ ] Results + How to validate (include live `/Profile` steps)

## Notes

- Parent **205**. Immediate predecessor **205-003** (Save 401) — product bug is Combobox
  display, not persistence.
- Cockpit: timewarp-flow Grok session `01a03d38-9611-7620-aae5-848e15dafa94` (2026-09-07).
  Do **not** implement in cockpit.
- File: `source/container-apps/web/projects/web-spa/pages/ProfilePage.razor`.
  Package: `Microsoft.FluentUI.AspNetCore.Components` `5.0.0-rc.5-26219.1`.
- Live Aspire at report: architecture `master` `10499c53`, ingress
  `https://arch.timewarp.work` / `https://localhost:63610`, web-server
  `https://localhost:63611`. Restart onto this task branch to demo.

## Session

- Created: 3977825 (2026-09-07)
- Cockpit: Grok `01a03d38-9611-7620-aae5-848e15dafa94` — Combobox shows Search instead of selection
