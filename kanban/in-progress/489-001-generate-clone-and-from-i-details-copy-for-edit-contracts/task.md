# Generate clone and From(I*Details) copy for edit contracts

## Description

Source generator (or analyzer-backed generator) driven by `I*Details` interfaces:

- For every Command implementing `I*Details`: `static Command From(I*Details source)` copying
  every interface member. Lists copied, not shared (`List<T>` on editable details).
- For every mutable Response implementing `I*Details` (setters): a real deep `Clone()` so
  TimeWarp.State's per-action state clone does not share a mutable instance. Get-only /
  `[Computed]` members (e.g. `GetProfile.Response.Avatar`) must survive the clone.
- Members outside the interface that the Command needs (e.g. `Version`) — decide: widen the
  interface, or a generator hook / partial method. Record the decision.

Parent: see parent kitchen for the pattern.

## Goal (implementer restatement)

An edit form needs a command filled from the response it just loaded, and TimeWarp.State needs its own copy of a mutable response so a rollback does not keep the same object. The foundation contracts generator emits `From` on a partial `Command` and a real `Clone` on a partial mutable `Response` for any interface whose name is longer than `IDetails`, starts with `I`, and ends with `Details`. Lists, arrays, sets, and dictionaries are new collections. `Version` stays off `ISiteSettingsDetails`; `UpdateSiteSettings.Command` copies it through `CopyBeyondDetails`. TWA0047 flags a mutable details response whose `Clone` returns `this`. TWA0048 flags a command or mutable response that is not partial. TWA0049 flags a partial command with no parameterless constructor. Pages and state stores stay unchanged, and `tw-web-api-contracts` is not edited here.

The GetRole clone check stays on `create-role-tests.cs`. That file's leading comment and Purpose region name the check. Its Design region records why a separate `get-role-tests.cs` is not used: Jaribu underscore names need `CA1707`, and a new runfile's `NoWarn` would sit above the per-file `.timewarp/suppression-baseline.json`.

This pass closes the three open review findings:

- The living-contract checks run inside co-located runfiles that `web-jaribu-tests` already compiles: `get-profile-tests.cs`, `update-site-settings-tests.cs`, and the GetRole clone check on `create-role-tests.cs`. A new `get-role-tests.cs` would add a `CA1707` suppression for Jaribu's underscore names, and that rise is refused. The stray `tests/analyzers/details-copy-contracts-tests.cs` is deleted.
- An explicit `object ICloneable.Clone()` counts as `Clone`. TWA0047 reports it when the body returns `this`. The generator does not emit a second public `Clone` beside it, because interface dispatch would keep calling the explicit method.
- The `I*Details`, mutable, partial-chain, `From`, `Clone`, and parameterless-constructor predicates live in one linked file under `source/analyzers/shared/`. The details-copy analyzer, `ContractAgreementAnalyzer`, and the foundation contracts generator all call that file.

Acceptance:

- `GetProfile.Response.Clone` keeps `Avatar` and does not share `DisplayName`. `UpdateProfile.Command.From` copies the details fields.
- `GetRole.Response.Clone` keeps `RoleId` and copies `Permissions` and `AgentScopes`. That check is on `create-role-tests.cs`.
- `UpdateSiteSettings.Command.From` copies `Version` 7 from the get response and 8 from the update response, and the tenant list is a new list.
- An analyzer test covers the explicit identity clone. A generator test shows that form is not given a second `Clone`.
- The five mutable responses stay partial with generated `Clone`, the five commands keep `From`, and the developer guide stays.
- `create-role-tests.cs` names `GetRole.Response.Clone` in its leading comment and Purpose region. Design says the check stays in this runfile because a new `get-role-tests.cs` would add a `CA1707` `NoWarn` above `.timewarp/suppression-baseline.json`.

## Checklist

- [x] Verify how TimeWarp.State 12.0.0-beta.17's generated state cloner treats contract
      objects (ICloneable? field copy?) and get-only members
- [x] Generator + diagnostics (e.g. mutable `I*Details` Response with `ICloneable => this`)
- [x] Unit tests for generated `From` and `Clone`
- [x] Short developer doc for the edit-form pattern under `documentation/developer/`
- [x] Review round 1: co-located living checks, explicit `ICloneable.Clone` counts as `Clone`, one shared classification file
- [x] Review round 2 M4: `create-role-tests.cs` Purpose, leading comment, and Design name the GetRole clone check and why it stays in this runfile

## Results

`DetailsCopyGenerator` emits `From` on each partial `Command` that implements `I*Details`, and a real `ICloneable.Clone` on each partial mutable `Response`. Lists, sets, dictionaries, and rank-1 arrays are copied. Get-only and `[Computed]` auto-properties survive `MemberwiseClone`; get-only collections are rewritten through `UnsafeAccessor` on the backing field. `Version` stays off `ISiteSettingsDetails`. The generator declares `static partial void CopyBeyondDetails(Command command, ISiteSettingsDetails source)` with no access modifier, and `UpdateSiteSettings.Command` copies `Version` from `GetSiteSettings.Response` or `UpdateSiteSettings.Response`.

TimeWarp.State is pinned at 12.0.0-beta.19. The 12.0.0-beta.17 generator has the same choice: a nested `ICloneable` is cloned by calling `Clone()` and is not field-copied, so `Clone() => this` shares the snapshot. A type that is not `ICloneable` is constructed and field-copied, including auto-property backing fields, so get-only members survive. `List<T>` is deep-copied. Identity clone stays legal only for an immutable contract. TWA0047 flags a mutable details response whose `Clone` returns `this`. TWA0048 flags a command or mutable response that is not partial. TWA0049 flags a partial command with no parameterless constructor.

`skills/tw-web-api-contracts` was not edited. It should say that `I*Details` drives generated `From` and a real `Clone`, that `Version` stays off the interface and uses `CopyBeyondDetails`, that `Clone() => this` is only for an immutable contract, and that a mutable details response must be partial.

Guide: `documentation/developer/guides/edit-form-details-copy.md`.

Review round 1 is implemented. `DetailsCopyClassification` in `source/analyzers/shared/details-copy-classification.cs` is the one copy of the `I*Details`, mutable, partial-chain, `From`, `Clone`, and parameterless-constructor predicates. The convention analyzer, `ContractAgreementAnalyzer`, and the foundation contracts generator all call it. `Clone` includes an explicit `object ICloneable.Clone()`. TWA0047 reports that form when the body returns `this`. When the type already has that method, the generator does not emit a second public `Clone`, because interface dispatch would keep calling the explicit method.

The stray `tests/analyzers/details-copy-contracts-tests.cs` is deleted. `web-jaribu-tests` compiles `get-profile-tests.cs`, `create-role-tests.cs` (the GetRole clone check), and `update-site-settings-tests.cs`. A separate `get-role-tests.cs` was not added: Jaribu's underscore names need a `CA1707` suppression, and a new runfile's `NoWarn` is above `.timewarp/suppression-baseline.json`. `create-role-tests.cs` already carries that suppression and already sits on the aggregator glob.

Review round 2 M4: the leading comment and Purpose region of `create-role-tests.cs` name `GetRole.Response.Clone`. The Design region says the check stays in this runfile because a new `get-role-tests.cs` would add a `CA1707` `NoWarn` above the per-file `.timewarp/suppression-baseline.json`.

### How to validate

Smoke:

- `dotnet run --file source/container-apps/web/features/profile/get-profile/get-profile-tests.cs`
- `dotnet run --file source/container-apps/web/features/admin/roles/create-role/create-role-tests.cs`
- `dotnet run --file source/container-apps/web/features/settings/update-site-settings/update-site-settings-tests.cs`

Expect: `GetProfile.Response.Clone` keeps `Avatar` and does not share `DisplayName`, and `UpdateProfile.Command.From` copies the details fields. `GetRole.Response.Clone` keeps `RoleId` and copies `Permissions` and `AgentScopes`. `UpdateSiteSettings.Command.From` copies `Version` 7 from the get response and 8 from the update response, and the tenant list is a new list.

Gates:

- `./bin/dev build` — succeeded, 0 warnings, 0 errors. This is the solution build of the analyzer and generator edits. The later create-role test move is not a solution project.
- `dotnet run --file source/container-apps/web/features/profile/get-profile/get-profile-tests.cs` — passed, 15 tests.
- `dotnet run --file source/container-apps/web/features/admin/roles/create-role/create-role-tests.cs` — passed, 6 tests, including `Clone_Should_KeepRoleId_And_CopyLists`.
- `dotnet run --file source/container-apps/web/features/settings/update-site-settings/update-site-settings-tests.cs` — passed, 12 tests, including `From_Should_CopyVersion_And_NotShareTenants`.
- `tests/analyzers/timewarp-architecture-analyzers-tests/details-copy-analyzer-tests.cs` — not a standalone runfile (no `#if !JARIBU_MULTI`). `./bin/dev test` does not take a file. CI runs the project. Verified from that project directory, so its `global.json` selects the Microsoft.Testing.Platform runner: `dotnet test -c Release --no-build --no-restore -- --filter-class DetailsCopyAnalyzer_Tests` — passed, 11 tests, including `Explicit_Identity_Clone_Flags_TWA0047`.
- `tests/analyzers/timewarp-architecture-sourcegenerator-tests/details-copy-generator-tests.cs` — not a standalone runfile (no `#if !JARIBU_MULTI`). CI runs the project. Same invocation from that project directory: `dotnet test -c Release --no-build --no-restore -- --filter-class DetailsCopyGenerator_Tests` — passed, 2 tests, including `Explicit_Identity_Clone_Is_Not_Replaced`.
- `ganda repo audit` — exit 0. 43 passed, 2 skipped, 1 advisory warning: `vscode-window-icon` (`.vscode/settings.json` is missing; gitignored local state, pre-existing). `analyzer-suppressions` passed: 843 suppressions, none above the baseline.

M4 fix pass (comment and region only; the other runfiles were not re-run):

- `./bin/dev build` — succeeded, 0 warnings, 0 errors.
- `dotnet run --file source/container-apps/web/features/admin/roles/create-role/create-role-tests.cs` — passed, 6 tests, including `Clone_Should_KeepRoleId_And_CopyLists`.
- `ganda repo audit` — exit 0. 43 passed, 2 skipped, 1 advisory warning: `vscode-window-icon` (`.vscode/settings.json` is missing; gitignored local state, pre-existing). `analyzer-suppressions` passed: 843 suppressions, none above the baseline.

### Review

- Round 1 (effort 3, roster: general): 3 open findings, 2 bug and 1 suggestion.
  The fix pass implemented M1, M2, and M3. Disposition waits for review round 2.
  - M1: CI never runs `tests/analyzers/details-copy-contracts-tests.cs`.
  - M2: an explicit `ICloneable.Clone() => this` gets past TWA0047 and the generator.
  - M3: the analyzer and the generator repeat the same predicates.
- Round 2 (effort 3, roster: general): re-checked the fix delta 2f96e5bec. M1, M2, and M3
  are fixed. One new nit is open:
  - M4 (nit): `create-role-tests.cs` now has the GetRole Clone check, but its Purpose region
    and leading comment still say the file covers CreateRole only, and nothing in the file
    says why the check sits there.
  - Counts: bug 0 open / 2 fixed; suggestion 0 open / 1 fixed; nit 1 open.
  The M4 fix pass updated the leading comment, Purpose region, and Design region.
  Disposition waits for review round 3.
- Paths: `review/review-framework.md`, `review/round-1/general.md`, `review/round-1/merged.md`,
  `review/round-2/general.md`, `review/round-2/merged.md`.

## Parent

- 489

## Session

- Implement oracle: implemented by implementer-grok (grok, vendor xai, model grok-4.7, response model grok-4.7-build), session 01a207f0-ea12-7e12-9d95-5fbdbe3d9ac9, max-turns 500, Done — 2026-11-23T11:29:10Z
- Review oracle round 1: Claude Opus 5.5 (claude-opus-5-5), effort 3, roster general, turn cap 200.
  Result: Blocked, fix pass for M1, M2, and M3 — 2026-11-23
- Review oracle: review by reviewer-claude (claude, vendor anthropic, model claude-opus-5-5, response model claude-opus-5-5), session 4daa86a6-ea16-4380-8f58-fcd2f2a2708b, max-turns 200, artifacts general.md, merged.md — 2026-11-23T11:35:14Z
- Review oracle round 2: Claude Opus 5.5 (claude-opus-5-5), effort 3, roster general, turn cap 200.
  Result: Blocked, fix pass needed for M4 — 2026-11-23
- Fix pass (implement-fix-2): Grok 4.7, 2026-11-23. M1 living checks are on the three co-located runfiles above (GetRole on `create-role-tests.cs`). M2 explicit `ICloneable.Clone` counts as `Clone`. M3 predicates live in `source/analyzers/shared/details-copy-classification.cs`.
- Fix pass (implement-fix-3): Grok 4.7, 2026-11-23. M4: `create-role-tests.cs` leading comment and Purpose name `GetRole.Response.Clone`. Design says a new `get-role-tests.cs` would add a `CA1707` `NoWarn` above `.timewarp/suppression-baseline.json`.

## Review

- Round 1: Blocked — open M1, M2, M3
- Fix pass: M1, M2, and M3 implemented. Host runs review round 2.
- Round 2: Blocked — open M4
- Fix pass: M4 implemented. Host runs review round 3.
