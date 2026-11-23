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

Edit forms need a command built from the fetched response, and TimeWarp.State needs a real copy of a mutable response so a rollback does not share that instance. This task adds that copy machinery to the contract assemblies. It does not change pages or state stores.

A source generator in the foundation contracts generators, already attached to web-contracts, emits two members from any interface whose name starts with `I` and ends with `Details`:

- On a partial class named `Command`, `static Command From(I*Details source)` copies every interface property. `List<T>`, arrays, sets, dictionaries, and the matching collection interfaces are new collections. Elements are shared unless the element type is a reference type that implements `ICloneable` (strings stay shared).
- On a partial class named `Response` whose details interface has a non-init setter, `ICloneable.Clone()` does a memberwise copy and then replaces those collections so the snapshot does not share them. Get-only and `[Computed]` auto-properties, including `GetProfile.Response.Avatar`, survive because memberwise clone copies their backing fields. Get-only collection auto-properties are rewritten through `UnsafeAccessor` on `<Name>k__BackingField`.

Members that are not on the interface stay off it. `Version` is a concurrency token, not a form field, so `ISiteSettingsDetails` is not widened. The generator declares `static partial void CopyBeyondDetails(Command command, I*Details source)` with no access modifier. `UpdateSiteSettings.Command` implements that hook and copies `Version` from `GetSiteSettings.Response` or `UpdateSiteSettings.Response`. Route ids, `EmailCopy`, and `AttachmentIds` are not copied unless a later hook says so.

A convention analyzer reports three errors: TWA0047 when a mutable details response's `Clone` returns `this`, TWA0048 when the command or mutable response (or a containing type) is not partial, and TWA0049 when a partial command has no parameterless instance constructor. Immutable `Clone() => this` on a type that does not implement a setter-bearing details interface stays legal.

Acceptance:

- The five mutable details responses (`GetProfile`, `UpdateProfile`, `GetRole`, `GetSiteSettings`, `UpdateSiteSettings`) are partial, and the generator supplies `Clone`.
- `From` exists on `CreateRole.Command`, `UpdateRole.Command`, `UpdateProfile.Command`, `UpdateSiteSettings.Command`, and `SubmitFeedback.Command`.
- Tests show list copies are not the same reference, `Avatar` and get-only lists survive `Clone`, and `Version` round-trips through the hook.
- `documentation/developer/` explains the edit-form pattern, the State cloner behavior (pinned package is 12.0.0-beta.19; beta.17 uses the same `ICloneable` short-circuit), and the `Version` decision.
- The flow skill `tw-web-api-contracts` is not edited here.

## Checklist

- [x] Verify how TimeWarp.State 12.0.0-beta.17's generated state cloner treats contract
      objects (ICloneable? field copy?) and get-only members
- [x] Generator + diagnostics (e.g. mutable `I*Details` Response with `ICloneable => this`)
- [x] Unit tests for generated `From` and `Clone`
- [x] Short developer doc for the edit-form pattern under `documentation/developer/`

## Results

`DetailsCopyGenerator` emits `From` on each partial `Command` that implements `I*Details`, and a real `ICloneable.Clone` on each partial mutable `Response`. Lists, sets, dictionaries, and rank-1 arrays are copied. Get-only and `[Computed]` auto-properties survive `MemberwiseClone`; get-only collections are rewritten through `UnsafeAccessor` on the backing field. `Version` stays off `ISiteSettingsDetails`. The generator declares `static partial void CopyBeyondDetails(Command command, ISiteSettingsDetails source)` with no access modifier, and `UpdateSiteSettings.Command` copies `Version` from `GetSiteSettings.Response` or `UpdateSiteSettings.Response`.

TimeWarp.State is pinned at 12.0.0-beta.19. The 12.0.0-beta.17 generator has the same choice: a nested `ICloneable` is cloned by calling `Clone()` and is not field-copied, so `Clone() => this` shares the snapshot. A type that is not `ICloneable` is constructed and field-copied, including auto-property backing fields, so get-only members survive. `List<T>` is deep-copied. Identity clone stays legal only for an immutable contract. TWA0047 flags a mutable details response whose `Clone` returns `this`. TWA0048 flags a command or mutable response that is not partial. TWA0049 flags a partial command with no parameterless constructor.

`skills/tw-web-api-contracts` was not edited. It should say that `I*Details` drives generated `From` and a real `Clone`, that `Version` stays off the interface and uses `CopyBeyondDetails`, that `Clone() => this` is only for an immutable contract, and that a mutable details response must be partial.

Guide: `documentation/developer/guides/edit-form-details-copy.md`.

### How to validate

Smoke: `dotnet run --file tests/analyzers/details-copy-contracts-tests.cs`

Expect: three tests pass. `GetProfile.Response.Clone` keeps `Avatar` and does not share `DisplayName`. `GetRole.Response.Clone` keeps `RoleId` and copies `Permissions`. `UpdateSiteSettings.Command.From` copies `Version` 7 from the get response and 8 from the update response, and the tenant list is a new list.

Gates:

- `./bin/dev build` — succeeded, 0 warnings, 0 errors.
- `dotnet run --file tests/analyzers/details-copy-contracts-tests.cs` — passed, 3 tests.
- `tests/analyzers/timewarp-architecture-analyzers-tests/details-copy-analyzer-tests.cs` — not a standalone runfile (no `#if !JARIBU_MULTI`). `./bin/dev test` does not take a file. CI runs the project. Verified from that project directory: `dotnet test -c Release -- --filter-class DetailsCopyAnalyzer_Tests` — passed, 10 tests.
- `tests/analyzers/timewarp-architecture-sourcegenerator-tests/details-copy-generator-tests.cs` — not a standalone runfile (no `#if !JARIBU_MULTI`). CI runs the project. Verified from that project directory: `dotnet test -c Release -- --filter-class DetailsCopyGenerator_Tests` — passed, 1 test.
- `ganda repo audit` — exit 0. 43 passed, 2 skipped, 1 advisory warning: `vscode-window-icon` (`.vscode/settings.json` is gitignored local state, pre-existing). The contracts runfile adds no `NoWarn`.

## Parent

- 489
