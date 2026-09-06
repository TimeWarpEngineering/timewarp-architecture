# Reject invalid profile Language (BCP-47 / culture name)

## Description

Human demo of task **205** `/Profile`: **Language** accepts `en-US asdfasdf` and Save succeeds.

`ProfileDetailsValidator` only has `NotEmpty()` on Language (same for Region and Theme). Domain
`Profile.SetLanguage` is `ThrowIfNullOrWhiteSpace` only. Shared FluentValidation on `IProfileDetails`
is what the Blazor `EditForm` (`FluentValidator`) and the PUT `UpdateProfile` mediator both run —
fix the shared validator so the form and the API reject the same junk.

## Requirements

- **Language** must be a real culture name (BCP-47 / `CultureInfo`), not a substring that happens
  to start with `en-US`. `en-US asdfasdf` is invalid. `en-US` and other installed/predefined
  cultures stay valid.
- Prefer `CultureInfo.TryGetCultureInfo` (or predefined-only `GetCultureInfo`) over a hand-rolled
  regex. Spaces and trailing junk must fail.
- Keep the rule on **`ProfileDetailsValidator`** (`profile-details-contracts.cs`) so SPA EditForm
  and `UpdateProfile.Validator` stay in agreement. Tighten domain `SetLanguage` / `Invariants` to
  match so a store write cannot bypass the contract.
- Add a failing-then-passing test: UpdateProfile (or ProfileDetailsValidator via existing
  co-located tests) rejects `en-US asdfasdf` and still accepts `en-US`.
- **Also look at Region and Theme** (same `NotEmpty()`-only hole). Theme defaults are
  `system` / likely `light`/`dark` — if they are an allow-list, enforce it. Region should not
  accept `US asdf` either. Do not expand into a locale picker UI on this task.

## Checklist

- [ ] Language: reject `en-US asdfasdf`; accept `en-US`
- [ ] Shared `ProfileDetailsValidator` + domain invariants / `SetLanguage`
- [ ] Region / Theme: close the same hole if it is the same class of bug
- [ ] Co-located tests
- [ ] Results + How to validate (form Save on `/Profile` plus automated filter)

## Notes

- Origin: cockpit demo of 205 after master merge of PR #327.
- Files: `profile-details-contracts.cs`, `profile-domain.cs`, `update-profile-tests.cs` /
  `get-profile-tests.cs`, ProfilePage already binds `ProfileDetailsValidator`.

## Session

- Created: 2837694 (2026-09-06)
- Cockpit: Grok — demo finding on `/Profile` Language
