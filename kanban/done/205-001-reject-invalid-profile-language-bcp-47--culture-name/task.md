# Reject invalid profile Language (BCP-47 / culture name)

## Description

Human demo of task **205** `/Profile`: **Language** is a free-text `FluentTextInput` and accepts
`en-US asdfasdf`. **Region** is the same class of hole (`US asdf` would save).

These are **closed catalogs**, not free strings. The control must be a **dropdown** (Fluent UI
Blazor v5 `FluentSelect`), not a text box plus a regex.

`ProfileDetailsValidator` only has `NotEmpty()` on Language / Region / Theme. Domain
`SetLanguage` / `SetRegion` / `SetTheme` are `ThrowIfNullOrWhiteSpace` only. Shared
FluentValidation on `IProfileDetails` is what the Blazor `EditForm` and PUT `UpdateProfile`
both run — keep that agreement, but the **UX is a select from the same catalog**.

## Requirements

### Language — ISO / BCP-47 dropdown

- **Not** a text input. `FluentSelect` bound to `IProfileDetails.Language`.
- Options are a **curated closed set** of BCP-47 tags the template actually supports
  (e.g. `en-US`, `en-GB`, `fr-FR`, …). Do **not** dump every `CultureInfo.GetCultures()`
  entry (hundreds of rows is not a demo).
- Stored value stays the tag (`en-US`), label is human (`English (United States)`).
- `en-US asdfasdf` cannot be entered. API still rejects any Language not in the catalog
  (typed PUT / mock).

### Region — ISO 3166-1 dropdown

- Same treatment as Language. **Not** a text input.
- Closed set of ISO 3166-1 alpha-2 codes (e.g. `US`, `GB`, `FR`). Stored code, labeled name.
- Default remains `US`. `US asdf` cannot be entered; API rejects unknown codes.

### Theme — allow-list dropdown

- Same hole: `NotEmpty()` only. Closed set: `system`, `light`, `dark` (defaults already
  `system`). Dropdown, not free text.

### Shared catalog + validator

- Catalog lives where contracts can see it (not the domain assembly). Validator
  `Must` be in that set. Domain `SetLanguage` / `SetRegion` / `SetTheme` / `Invariants`
  match so a store write cannot bypass.
- ProfilePage: replace the three `FluentTextInput`s. Alias and Email stay text.
- Tests: reject `en-US asdfasdf` and `US asdf`; accept current defaults `en-US` / `US` /
  `system`.

## Checklist

- [x] Language: FluentSelect from curated BCP-47 catalog
- [x] Region: FluentSelect from ISO 3166-1 alpha-2 catalog
- [x] Theme: FluentSelect `system` / `light` / `dark`
- [x] Shared `ProfileDetailsValidator` + domain invariants / setters
- [x] Co-located tests
- [x] Results + How to validate (form cannot type junk; Save still works for `en-US` / `US`)
- [x] Implementation review disposition (`clean`)

## Notes

- Origin: cockpit demo of 205 after master merge of PR #327.
- Maintainer (2026-09-06): Language and Region are fixed ISO sets — **dropdowns**, not
  validated text boxes. Earlier brief said “do not expand into a locale picker”; that is
  superseded.
- Files: `profile-details-contracts.cs`, `profile-domain.cs`, `ProfilePage.razor`,
  `update-profile-tests.cs`. Fluent UI v5 `FluentSelect` (two type params).

## Results

Language, Region, and Theme are closed catalogs. `ProfilePage` binds Fluent UI v5
`FluentSelect<ProfileCatalog.Entry, string>` to `ProfileCatalog` (contracts).
`ProfileDetailsValidator` `Must` membership so EditForm and PUT `UpdateProfile` agree.
Domain `Create` / `SetLanguage` / `SetRegion` / `SetTheme` / `Invariants` duplicate the
code sets (domain cannot reference contracts) so a store write cannot bypass.

Junk such as `en-US asdfasdf`, `US asdf`, and `neon` is rejected. Defaults `en-US` /
`US` / `system` (and every catalog entry) are accepted. Alias and Email stay text inputs.

Did not exercise `/Profile` in a live browser this session (Aspire host not started).
Form behavior is covered by the catalog + FluentSelect binding plus validator tests.

### How to validate

**Smoke**

```bash
dotnet run source/container-apps/web/features/profile/update-profile/update-profile-tests.cs -- --filter-method TrailingJunk
cd tests/container-apps/web/web-domain-tests && dotnet test -c Release -- --filter-method trailing_junk
dotnet run tools/dev-cli/dev.cs -- run
```

Then sign in, open `/Profile`, open the Language / Region / Theme dropdowns, pick
`English (United States)` / `United States` / `System`, Save.

**Expect**

- Trailing-junk tests fail validation / throw `ArgumentException`; catalog-default tests pass.
- Language, Region, and Theme are dropdowns, not text boxes — `en-US asdfasdf` cannot be typed.
- Save with `en-US` / `US` / `system` succeeds and the page still shows those values.

### Review

| Field | Value |
|-------|--------|
| Effort / roster | 1 · general only |
| Rounds | 1 |
| Final counts | open 0 · fixed 0 · wontfix 0 (all severities) |
| Disposition | **clean** |
| Paths | `review/review-framework.md`, `review/round-1/general.md`, `review/round-1/merged.md`, `review/disposition.md` |

Round 1 raised no issues. Catalog and domain code sets match; FluentSelect compiles (`web-spa` 0/0); validator and domain junk/default tests pass. No exceptions.

## Session

- Created: 2837694 (2026-09-06)
- Cockpit: Grok — demo finding on `/Profile` Language; dropdown requirement
- Implementation: Grok implementer — catalogs + FluentSelect + validator/domain + tests
- Review: Grok review oracle — effort 1 general, disposition `clean` (2026-09-06)
- CI (PR #328): web-jaribu aggregator reported PropertyName `language`/`region`/`theme` (JSON camelCase after host tests mutate ValidatorOptions.Global); standalone stays PascalCase. Assertions accept either, plus ErrorMessage; template-smoke web-jaribu expected 127→132.
