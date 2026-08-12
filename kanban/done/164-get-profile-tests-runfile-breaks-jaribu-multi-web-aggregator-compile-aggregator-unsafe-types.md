# get-profile-tests runfile breaks JARIBU_MULTI web aggregator compile (aggregator-unsafe types)

## Description

Found while running 147-007's template-smoke gate (tier 3 runs the generated app's Jaribu
family aggregators): `web-jaribu-tests` failed to compile `get-profile-tests.cs` (tasks 148/150)
— pre-existing in the monorepo too; nobody had run the web aggregator since the file landed.

Root cause: under JARIBU_MULTI the aggregator references the full host closure including
Web.Spa, whose `Profile.razor` component is ALSO `TimeWarp.Architecture.Features.Profiles.Profile`.
The test file's namespace block is that same namespace, and namespace members beat usings, so
bare `Profile` flipped from the domain entity to the Blazor component (cascading CS0029/CS1061/
CS7036). Additionally the aggregator does not inherit the runfile's `#:property NoWarn`, so its
`var` usage tripped IDE0008 as errors there.

## Checklist

- [x] Pin the domain entity via `using DomainProfile = …Domain.Profile;` alias (same-name alias
      trips CS0576 because the namespace contains a `Profile` member — distinct name required)
- [x] Replace all `var` with explicit types (aggregator-safe authoring; runfile NoWarn does not
      apply under JARIBU_MULTI)
- [x] Prove BOTH modes: aggregator `dotnet build` 0/0 AND standalone `dotnet run` 10/10
- [x] Full web aggregator suite green: 54/54 (first-ever run including the profile runfiles)

## Results

Fixed in `c8cb3ca3` — see checklist. Lesson recorded in the file's using-block comment: runfiles
whose namespace collides with SPA component namespaces must alias domain types under a distinct
name, and must not rely on `#:property NoWarn` for style rules the aggregator enforces.

### How to validate

**Automated**
```bash
cd tests/container-apps/web/web-jaribu-tests && dotnet test -c Release
# expect: total: 54, failed: 0
dotnet run source/container-apps/web/features/profile/get-profile/get-profile-tests.cs
# expect: Total: 10, Passed: 10 (standalone mode, from repo root)
```

Not in scope: a general analyzer guarding aggregator-unsafe runfile authoring (candidate for
the prefer-analyzers backlog if this recurs).

## Session

- 2026-08-05/06 claude: found via 147-007 smoke tier 3, root-caused (Web.Spa Profile.razor
  namespace collision), fixed, both compile modes + full aggregator verified, done.
