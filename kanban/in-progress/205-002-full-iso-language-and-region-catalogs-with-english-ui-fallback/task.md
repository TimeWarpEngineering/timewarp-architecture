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

- [x] Replace handwritten Language/Region HashSets with BCL ISO catalogs
- [x] FluentSelect/Combobox bound to those catalogs (Theme stays 3-way)
- [x] Thai selectable and stored; junk still rejected
- [x] UI culture remains en-US; document recognize vs apply
- [x] Tests + smoke count; Results + How to validate

## Notes

- Parent **205**; immediate predecessor **205-001** (PR #328) shipped the closed lists and
  dropdowns. This task opens the catalogs, not the identity kernel.
- Files: `profile-details-contracts.cs` (`ProfileCatalog`), `profile-domain.cs`,
  `ProfilePage.razor`, `update-profile-tests.cs`, how-to-progressive-profile how-to,
  `web-spa/program.cs` `SetIsoCulture` (leave as English fallback; comment if needed).

## Session

- Created: 3443420 (2026-09-07)
- Cockpit: Grok — maintainer: full ISO recognize-all, English UI until i18n exists
- Implementer: Grok session 01a07977-cc23-74f0-bc39-7439055da552 (2026-09-07)

## Results

Opened Language/Region from the 205-001 demo allow-lists to BCL ISO catalogs. Stored
preference and applied UI stay two layers: any valid specific culture / alpha-2 region
can be selected and persisted (including `th-TH` / `TH`); SPA UI culture remains `en-US`.

**What landed**

- `ProfileCatalog` builds Language from `CultureInfo.GetCultures(SpecificCultures)` (English
  display names) and Region from distinct `RegionInfo.TwoLetterISORegionName` (length 2).
- Validators: Language is `CultureInfo.GetCultureInfo(name, predefinedOnly: true)` and
  non-neutral (this SDK has no `TryGetCultureInfo`). Region is membership in that
  GetCultures-derived set — `new RegionInfo(alpha2)` rejects a few catalog codes (`EH`,
  `DG`, `EA`, `IC`). Domain repeats the same BCL checks; it does not reference contracts.
  Theme is still `system` / `light` / `dark`.
- `/Profile` Language and Region are searchable `FluentCombobox`; Theme stays 3-way
  `FluentSelect`. No free-form option fragment, so junk cannot be submitted as a typed value.
- `SetIsoCulture()` still hardcodes `en-US`. Recognize-vs-apply is documented on the catalog
  Design region, `program.cs`, and the progressive-profile how-to. Create-if-missing defaults
  stay `en-US` / `US` / `system`.

**Files**

- `source/container-apps/web/features/profile/profile-details-contracts.cs`
- `source/container-apps/web/features/profile/profile-domain.cs`
- `source/container-apps/web/projects/web-spa/pages/ProfilePage.razor`
- `source/container-apps/web/projects/web-spa/program.cs`
- `source/container-apps/web/features/profile/update-profile/update-profile-tests.cs`
- `tests/container-apps/web/web-domain-tests/profile-tests.cs`
- `documentation/developer/how-to-guides/how-to-progressive-profile-and-agent-human-link.md`
- `tools/dev-cli/services/template-smoke-harness.cs` (web-jaribu expected 132 → 134)

**Tests**

- `dotnet run tools/dev-cli/dev.cs -- build` — 0/0
- UpdateProfile runfile — 15/15 (Thai validation + persist; junk still fails; catalog
  entries accepted by domain)
- `web-domain-tests` filter `Profile_` — 30/30
- `web-jaribu-tests` — **134/134** (matches smoke expected count)

**Not in scope:** `.resx` / satellite assemblies; wiring `DefaultThreadCurrentUICulture`
from `Profile.Language`; DID/VC profile; identity kernel.

### How to validate

**Smoke**

```bash
dotnet run tools/dev-cli/dev.cs -- build
# expect: Build succeeded. 0 Warning(s) 0 Error(s)

dotnet run source/container-apps/web/features/profile/update-profile/update-profile-tests.cs
# expect: 15 passed, including ThaiLanguageAndIndependentRegion_Should_PassValidation
#         and Authenticated_thai_language_and_region_Should_Persist;
#         LanguageWithTrailingJunk / RegionWithTrailingJunk still fail

cd tests/container-apps/web/web-jaribu-tests && dotnet test -c Release
# expect: succeeded 134 / failed 0
```

Optional UI (when Aspire is up): sign in, open `/Profile`, search Language for
`Thai (Thailand)` (`th-TH`) and Region for `Thailand` (`TH`), Save. Chrome stays English.

**Expect**

- `th-TH` + `TH` and `th-TH` + `US` validate and persist.
- `en-US asdfasdf` and `US asdf` still fail validation.
- Defaults `en-US` / `US` / `system` still pass.
- Template-smoke web-jaribu expected count is 134.

**Automated gate**

```bash
dotnet run tools/dev-cli/dev.cs -- build
dotnet run source/container-apps/web/features/profile/update-profile/update-profile-tests.cs
cd tests/container-apps/web/web-domain-tests && dotnet test -c Release -- --filter-class Profile_
cd tests/container-apps/web/web-jaribu-tests && dotnet test -c Release
```

**Not in scope:** live Aspire `/Profile` click-through; Thai UI translations.
