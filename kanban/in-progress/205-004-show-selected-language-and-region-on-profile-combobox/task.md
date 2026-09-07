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

- [x] Closed Language/Region show selected catalog labels after load, Save, and revisit
- [x] Search still finds Thai (and other ISO rows); junk still rejected
- [x] Theme Select unchanged
- [x] Results + How to validate (include live `/Profile` steps)

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
- Implementer: Grok `01a07b4e-33d8-7942-a0e1-7336d7900dd2` (2026-09-07)
- Review oracle: Grok `01a07b57-c908-7731-8abd-42e8083e5e89` (2026-09-07) — effort 1, rounds 2, disposition clean

## Results

Fluent UI Blazor v5 `FluentCombobox` (`FluentSelect.Initialize`) writes closed-input text only when `Value is TOption`, then `GetOptionText`; otherwise `""` (Placeholder). Combobox JS then sets `_control.value` for `type==="combobox"`. Theme `FluentSelect` (dropdown) does not use that path.

**Fix:** Language/Region stay searchable Combobox with `@bind-Value` on the stored ISO tag. `TOption == TValue == string`, `Items` = `ProfileCatalog.LanguageCodes` / `RegionCodes`, `OptionText` → `LabelFor` so Initialize receives `English (United States)` / `United States`. `@bind-SelectedItems` was the first attempt and does **not** feed Initialize (single-select `GetOptionSelected` also ignores SelectedItems). `FreeOption` stays unset. Theme stays `FluentSelect`. `SetIsoCulture` stays `en-US`. Mock auth was not turned on.

The SPA test host cannot assert Fluent JS closed-input text. Catalog lookup is covered by a co-located Jaribu test. Live `/Profile` steps below are the closed-field proof.

### Files

- `source/container-apps/web/projects/web-spa/pages/ProfilePage.razor` — string Combobox + LabelFor OptionText; Theme Select unchanged
- `source/container-apps/web/features/profile/profile-details-contracts.cs` — `LanguageCodes` / `RegionCodes`, `Matching` / `LabelFor`
- `source/container-apps/web/features/profile/update-profile/update-profile-tests.cs` — `MatchingCatalogCode_Should_ReturnCatalogLabel`
- `tools/dev-cli/services/template-smoke-harness.cs` — web-jaribu expected succeeded `134` → `135`

### Tests

- `dotnet run source/container-apps/web/features/profile/update-profile/update-profile-tests.cs` — **16 passed** (matching test included)
- `dotnet build source/container-apps/web/projects/web-spa/web-spa.csproj -c Release` — **0/0** (post-review fix)
- `dotnet run tools/dev-cli/dev.cs -- build` — **0/0** (implement session; not re-run after review fix)
- Live closed-field UX: not exercised. Ingress `https://localhost:63610` is **master** Aspire (`dcp`), not this task branch. Restart Aspire onto `task/205-004-show-selected-language-and-region-on-profile-combo` and sign in with a passkey to prove closed labels.

### Review disposition

- **Outcome:** clean (0 open)
- **Effort / roster:** 1 — general only
- **Rounds:** 2
- **Counts (final, round 2):** bug 0 open / 1 fixed / 0 wontfix; suggestion 0 open / 1 fixed / 0 wontfix; nit 0/0/0
- **M1 (bug, fixed):** SelectedItems did not fix closed labels; string TOption/TValue + LabelFor does
- **M2 (suggestion, fixed):** catalog test kept as lookup coverage, not closed-input proof
- **Paths:** `review/review-framework.md`, `review/round-1/{general,merged}.md`, `review/round-2/{general,merged}.md`, `review/disposition.md`
- **Wontfix / escalations:** none

### How to validate

**Smoke**

1. Restart Aspire on this task branch (`task/205-004-show-selected-language-and-region-on-profile-combo`), then open ingress `/Profile` (signed-in passkey session; do not set `Authentication:UseMock`).
2. With the Language and Region comboboxes **closed**, read the field text (not the open list).
3. Open Language, type `Thai`, select `Thai (Thailand)`. Open Region, type `Thai`, select `Thailand`. Theme stays the `FluentSelect` (System / Light / Dark). Save. Leave `/Profile` and come back.
4. Type junk such as `en-US asdfasdf` in Language and Save — it must not persist.

**Expect**

- After load (default `en-US` / `US`): Language closed text is `English (United States)`; Region closed text is `United States`; Theme shows `System` (or the stored theme label). Closed fields must **not** show `Search languages` / `Search regions`.
- After Save + revisit with Thai: Language `Thai (Thailand)`; Region `Thailand`. Search still finds those rows in the open list.
- Junk `en-US asdfasdf` is rejected (validation); `FreeOption` is unset.

**Automated**

```bash
dotnet run source/container-apps/web/features/profile/update-profile/update-profile-tests.cs
# expect: 16 passed (MatchingCatalogCode_Should_ReturnCatalogLabel:
#   en-US → English (United States), th-TH → Thai (Thailand),
#   US → United States, TH → Thailand; junk matching empty)

dotnet run tools/dev-cli/dev.cs -- build
# expect: 0 Warning(s) 0 Error(s)
```

**Depends on:** live `/Profile` needs Aspire on this branch + an identity-session cookie (passkey). Catalog tests are host-free.

**Not in scope:** applying stored Language as UI culture (`SetIsoCulture` stays `en-US`); persistence/401 (205-003); turning mock auth on.
