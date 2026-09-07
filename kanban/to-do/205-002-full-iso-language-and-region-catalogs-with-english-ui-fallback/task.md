# Full ISO language and region catalogs with English UI fallback

## Description

**205-001** closed Language/Region to a hand-picked demo subset (15 BCP-47 tags, 27 alpha-2
regions). Thai (`th-TH` / `TH`) is rejected by the API even though the SPA never applies
`Profile.Language` to UI culture — `SetIsoCulture()` already hardcodes `en-US`.

This is a **template** progressive profile (passkey first, optional chrome after the principal
exists). **Recognizing a locale is not implementing translations.** The dropdown should offer
the full ISO catalogs so any valid tag can be stored. Runtime UI stays English until a later
i18n task ships resources for that tag.

Supersedes the 205-001 Design line “curated demo subsets — not CultureInfo.GetCultures() and
not every ISO 3166-1 row.”

## Requirements

### Recognize vs apply (two layers)

| Layer | This task | Not this task |
|--------|-----------|----------------|
| **Catalog / store** | Full valid ISO codes. Thai is selectable and persisted. | Shipping `.resx` / satellite assemblies for Thai |
| **Junk** | Still reject `en-US asdfasdf`, `US asdf`, empty | — |
| **UI culture** | Keep `SetIsoCulture()` on `en-US` (English fallback) | Switching `DefaultThreadCurrentUICulture` from profile |
| **Theme** | Unchanged closed set `system` / `light` / `dark` | — |
| **DID / VC profile** | Out of scope (later portable identity) | Do not fold profile into TimeWarp.Identity |

Locked 104/205: passkey/key/session/token never take `IProfileStore`. Profile stays product
chrome, not the identity kernel.

### Language

- Stored shape stays BCP-47 **specific culture** (`en-US`, `th-TH`), matching today’s column.
- Catalog = predefined specific cultures (`CultureInfo.GetCultures(CultureTypes.SpecificCultures)`
  and/or `TryGetCultureInfo`), not a 15-row HashSet.
- Validator + domain `SetLanguage` / `Invariants`: **valid culture name**, not membership in
  a handwritten list. Domain still cannot reference the contracts assembly — use the same
  BCL check, not a duplicated string table.
- Dropdown: `FluentSelect` or `FluentCombobox` if the list is too long to scan (searchable).
  Label is English display name (`Thai (Thailand)`), value is the tag.

### Region

- Stored shape stays ISO 3166-1 **alpha-2** (`US`, `TH`).
- Catalog = full two-letter region set (e.g. `RegionInfo` from those cultures, distinct
  `TwoLetterISORegionName`). Independent of Language (`th-TH` + `US` is valid).
- Same validator/domain story as Language. Combobox if needed.

### English fallback

- Do **not** wire profile language into SPA culture on this task.
- Document in the Profile catalog Design region and the progressive-profile how-to:
  stored preference vs applied UI; missing resources → English (`en-US`).
- Defaults on create-if-missing stay `en-US` / `US` / `system`.

### Tests

- `th-TH` / `TH` **pass** validation and persist (the 205-001 allow-list would have failed).
- `en-US asdfasdf` / `US asdf` still **fail**.
- Existing `en-US` / `US` / `system` still pass.
- Aggregator `web-jaribu-tests` PropertyName may be camelCase when hosts mutate
  `ValidatorOptions.Global` (205-001 CI). Assert Pascal or camel, or ErrorMessage.
- Bump template-smoke web-jaribu expected count if the suite grows.

## Checklist

- [ ] Replace handwritten Language/Region HashSets with BCL ISO catalogs
- [ ] FluentSelect/Combobox bound to those catalogs (Theme stays 3-way)
- [ ] Thai selectable and stored; junk still rejected
- [ ] UI culture remains en-US; document recognize vs apply
- [ ] Tests + smoke count; Results + How to validate

## Notes

- Parent **205**; immediate predecessor **205-001** (PR #328) shipped the closed lists and
  dropdowns. This task opens the catalogs, not the identity kernel.
- Files: `profile-details-contracts.cs` (`ProfileCatalog`), `profile-domain.cs`,
  `ProfilePage.razor`, `update-profile-tests.cs`, how-to-progressive-profile how-to,
  `web-spa/program.cs` `SetIsoCulture` (leave as English fallback; comment if needed).

## Session

- Created: 3443420 (2026-09-07)
- Cockpit: Grok — maintainer: full ISO recognize-all, English UI until i18n exists
