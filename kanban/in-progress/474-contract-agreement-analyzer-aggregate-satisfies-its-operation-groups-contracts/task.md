# Contract-agreement analyzer: aggregate satisfies its operation group's contracts

## Description

Resolution of task 461: no entity generator. Instead, a build check (AGENTS.md: "when two things
must agree, generate one from the other or add a build check") that the hand-written aggregate
has what its contracts want. It runs where both sides are visible: `web-application` references
`web-contracts` and `web-domain`; metadata (attributes, members, types) is enough. Contracts stay
UI-first, the domain stays domain-first, the analyzer proves they agree.

## Requirements

- New convention analyzer, next TWA id, in `timewarp-architecture-convention-analyzers`. It must
  load in `web-application` (check that project references the convention analyzers; add if not).
- Linking: an aggregate root is linked to an `[Operation]` group by a small attribute on the
  root, `[SatisfiesOperations("admin.roles")]` (name open), or by convention from the slice
  namespace when unambiguous; record the rule. Profile and Role are the two roots at first.
- Checks, each its own diagnostic with a clear message:
  1. Every member of the group's shared details interface (the interface both create and update
     commands implement) exists on the root with a compatible type.
  2. Every command in the group whose name starts with `Set` and has one list member has a
     mutation `Set<Name>` on the root taking a collection of that element type.
  3. Every query response in the group is projectable: each response member maps to a root
     member by name and compatible type, or the response is marked as computed.
  4. A group with a root but a command that matches none of the above: warning, with
     `[NotAggregate]` on the operation as the opt-out.
- Member name match is exact. Drift is an error, not a silent mapping (this is the point).
- Tests in `tests/analyzers/` with Roles and Profile shapes, including each failure.
- `AnalyzerReleases.Unshipped.md`, and a paragraph in `tw-aggregate-pattern` and
  `tw-web-api-contracts` naming the rule.
- Gates: `dev build` 0 warnings with the analyzer on and Roles (473) and Profile passing, analyzer
  tests, `dev template-smoke`, `ganda repo audit`. Version bump (analyzer package ships).

## Goal (implementer restatement)

Add a build check, not a generator: a convention analyzer that runs in the one compilation that
sees both `web-contracts` and `web-domain` (web-application) and proves each hand-written aggregate
root has what its `[Operation]` group's contracts ask for. Contracts stay UI-first, the domain stays
domain-first, and drift between them fails the build.

Acceptance:

- A root links to an operation group by a recorded convention (slice namespace), with no
  declaration on the entity; an ambiguous link is reported, not guessed.
- Four diagnostics, each with a clear message: details-interface member missing or incompatible
  on the root; `Set<Name>` list command with no `Set<Name>` mutation taking that element type;
  query response member that does not map to a root member (unless marked computed); a command in
  a rooted group that none of those checks covers (warning, `[NotAggregate]` opt-out).
- Names match exactly; where Roles or Profile drift today, fix the side that is wrong and record it.
- Jaribu analyzer tests for Roles and Profile shapes, each failure included.
- AnalyzerReleases, `tw-aggregate-pattern` and `tw-web-api-contracts` name the rule.
- Gates: `dev build` 0 warnings with Roles and Profile passing, analyzer tests, `dev template-smoke`,
  `ganda repo audit`, version bump.

## Checklist

- [x] Analyzer with the four checks and tests (TWA0039–TWA0042, plus TWA0043 for an ambiguous link)
- [x] Loads in web-application; Roles and Profile pass
- [x] AnalyzerReleases and skills updated
- [x] Gates green; version: ships in the unreleased 2.0.0-beta.32 (see Results)

## Session

- Created: 66368815-cdd5-46be-bde8-a09d94e1cdc8 (2026-11-05)
- Implemented: ganda task work implement oracle (Claude Opus 5.5), 2026-11-05. A helper subagent did
  the contract-side renames (Alias, PermissionIds) and the Profile `[Operation]` attributes.

## Results

- **Analyzer:** `ContractAgreementAnalyzer` in `timewarp-architecture-convention-analyzers`
  (`contract-agreement-analyzer.cs`, `contract-agreement-types.cs`). It is a CompilationAction that
  reads symbols only. It scans the compilation's own types, plus referenced assemblies that reference
  the `IAggregateRoot` assembly or the `TimeWarp.Contracts` assembly.
  - TWA0039 (error): an `I*Details` member is missing on the root or has a different type.
  - TWA0040 (error): a `Set<Name>` list command has no `Set<Name>(collection)` mutation.
  - TWA0041 (error): a query-response member does not project from the root. A `ListResponse<T>`
    is checked through `T`, and `[Computed]` skips a member or a whole response.
  - TWA0042 (warning): a command matches none of those checks. `[NotAggregate("reason")]` opts it out.
  - TWA0043 (warning): one group links to two roots.
- **Linking rule (recorded in `tw-aggregate-pattern` and the analyzer Design region):** convention
  only, with no attribute on the entity (461 resolution: "no declaration on the entity"). A root's
  slice namespace is its namespace without a trailing `.Domain`. The root satisfies every group with
  an operation in that namespace, and then every operation of the group is checked. A group in two
  roots' slice namespaces is ambiguous (TWA0043). `[SatisfiesOperations]` was not needed.
- **Name and type rules:** names match exactly. The only key rule is `<Root>Id` → `Id`. Types are
  compatible when they are the same type with the same nullability, when the root type is a value
  wrapper of the contract type (a typed id through `Value`, or a one-property child record), or when
  both are collections of compatible elements.
- **Reported once:** a compilation skips a root/group pair when one of its references already sees
  both sides. web-application reports. web-server and the test projects stay quiet. A test covers this.
- **Loads in web-application:** it was already loaded. Convention analyzers are wired repo-wide for
  `source/` (`EnableConventionAnalyzers`), so no csproj change was needed. I proved it fires by
  temporarily removing `[Computed]` from `Avatar` and `[NotAggregate]` from DeleteRole: the
  web-application build failed with TWA0041 and TWA0042. Then I restored both.
- **Drift found and the side fixed (contract side both times; no domain, EF, or migration change):**
  - Profile: `IProfileDetails.Alias` against the domain's `Profile.DisplayName`. I renamed it to
    `DisplayName` on the contracts (details interface, GetProfile/UpdateProfile, validator constant
    `MaxDisplayNameLength`), the handlers, the SPA profile state and pages, and the tests. The domain
    word `DisplayName` is what the rest of the system uses (identity principal, profile directory).
    Renaming the domain side instead would have needed a column-rename migration.
    - Changed: the wire JSON key is now `displayName`, the agent page-facts key is `displayName`,
      and the `data-qa` attribute is `ProfileDisplayName`.
  - Roles: `GetRole.Response.PermissionIds` and `GetRoles.RoleDto.PermissionIds` against the domain's
    `Role.Permissions`. I renamed both to `Permissions`, so the wire key is now `permissions`. The
    `SetRolePermissions` command and response keep `PermissionIds`, because check 2 does not compare
    member names.
- **Opt-outs (each with a reason):**
  - `[NotAggregate]` on `DeleteRole.Command`: only the route id travels.
  - `[NotAggregate]` on the three `user-xai-api-key.*` operations: they are grouped under `profile`
    for hosts, but the key is a protected secret row, not Profile state.
  - `[Computed]` on `GetProfile.Response.Avatar`: the handler renders it from the user id.
- **Profile linked:** `GetProfile.Query` and `UpdateProfile.Command` gained
  `[Operation("profile.get" / "profile.update", Group = "profile")]`. Without those, nothing linked
  `Profile` to a group.
- **Attributes:** `NotAggregateAttribute(string reason)` and `ComputedAttribute` are in
  `timewarp-architecture-attributes`. The analyzer matches them by simple name.
- **Skills:**
  - `tw-aggregate-pattern` has a "Contract agreement" section: the rule, the link, the type rules,
    and the id table.
  - `tw-web-api-contracts` has a paragraph after the I*Details step. To stay under the 500-line
    vally cap, the Blazilla binding paragraph moved into `references/mutability.md`.
- **Version:** I did not bump it. `dev check-version` reports that 2.0.0-beta.32 is in source and
  that NuGet's latest is beta.31. beta.32 is bumped but unreleased, so this analyzer and the
  attributes ship in beta.32. A bump to beta.33 would skip a version; I tried it and reverted it.
- **Gates:**
  - `dev build`: 0 warnings, 0 errors.
  - `dev test --group fast`: 14/14 projects passed, including the analyzer tests (289 tests, 20 new).
  - web-server-integration-tests: 437/437 passed.
  - web-jaribu-tests (co-located): 464/464 passed.
  - `dev template-smoke`: passed.
  - `dev lint-skills`: passed.
  - `dev check-version`: passed.
  - `ganda repo audit`: passes, with two pre-existing advisory warnings that this change did not
    cause. They are kanban-landed claims on other tasks and the line cap on migration Designer files.
- **Not run locally:** the integration group's Aspire AppHost suites (web-spa-integration-tests,
  aspire-tests) and Playwright. Worktree AppHosts share user secrets, so those are left to CI.
  - The SPA integration tests touched by the rename compile in `dev build`. Their assertions
    changed only in member names.

### How to validate

- **Smoke:** `./bin/dev build` in the task worktree, then
  `(cd tests/analyzers/timewarp-architecture-analyzers-tests && dotnet run)`. To see the rule fire,
  delete `[Computed]` above `Avatar` in
  `source/container-apps/web/features/profile/get-profile/get-profile-contracts.cs` and run
  `(cd source/container-apps/web/projects/web-application && dotnet build)`.
- **Expect:**
  - The build reports 0 warnings and 0 errors.
  - The analyzer tests report `failed: 0`, and the `Should_Check_Contract_Agreement` cases are
    included.
  - With `[Computed]` removed, web-application fails with `TWA0041:
    'TimeWarp.Architecture.Features.Profiles.GetProfile.Response.Avatar' does not project from
    'Profile' (operation group 'profile')`. Restore the attribute afterwards.

## Notes

Follows 461, after 473. Task 475 brings FeedbackItem under it; task 476 decides whether the
projection and mutation call can then be generated.
