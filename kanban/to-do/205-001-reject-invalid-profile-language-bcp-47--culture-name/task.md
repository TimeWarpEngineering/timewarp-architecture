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

- [ ] Language: FluentSelect from curated BCP-47 catalog
- [ ] Region: FluentSelect from ISO 3166-1 alpha-2 catalog
- [ ] Theme: FluentSelect `system` / `light` / `dark`
- [ ] Shared `ProfileDetailsValidator` + domain invariants / setters
- [ ] Co-located tests
- [ ] Results + How to validate (form cannot type junk; Save still works for `en-US` / `US`)

## Notes

- Origin: cockpit demo of 205 after master merge of PR #327.
- Maintainer (2026-09-06): Language and Region are fixed ISO sets — **dropdowns**, not
  validated text boxes. Earlier brief said “do not expand into a locale picker”; that is
  superseded.
- Files: `profile-details-contracts.cs`, `profile-domain.cs`, `ProfilePage.razor`,
  `update-profile-tests.cs`. Fluent UI v5 `FluentSelect` (two type params).

## Session

- Created: 2837694 (2026-09-06)
- Cockpit: Grok — demo finding on `/Profile` Language; dropdown requirement
