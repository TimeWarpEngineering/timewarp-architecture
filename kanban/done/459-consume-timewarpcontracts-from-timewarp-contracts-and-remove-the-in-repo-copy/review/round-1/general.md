# Round 1 — general
**Date:** 2026-11-01
**Scope reviewed:** same as framework (`3cfa29bae` vs merge base with `master`)

## Summary

The change deletes `source/foundation/timewarp-contracts/` and consumes the nuget.org
`TimeWarp.Contracts` `2.0.0-beta.30` package from `foundation-contracts` and the source-generator
tests. It also drops the id from the template-smoke, template-publish-smoke, and workflow pack lists,
rewords the skills and `AGENTS.md`, bumps Nuru to `4.0.0-beta.11`, and commits the audit `.gitignore`
lines. Risk is low. The deleted source is byte-identical to the package's source commit. Every gate
the implementer reported reproduces green.

## Verification performed

- **Package equals the deleted source.** For each `.cs` file under `source/foundation/timewarp-contracts/`
  at the merge base, I diffed it against `source/timewarp-contracts/` at timewarp-contracts commit
  `0b1c70e` (the commit in the package nuspec `<repository commit>`). All 13 `.cs` files are identical.
  Only `readme.md` and the `.csproj` differ, and only for repository-local reasons: the version-line
  wording, and the old project's ProjectReference to this repo's convention analyzers.
- **The six deleted tests did move.** Each of `operation-attribute-{group,name,title}-tests.cs`,
  `operation-catalog-{group,tool-hint}-tests.cs`, and `tool-hint-attribute-tests.cs` is
  byte-identical to the file under `tests/timewarp-contracts-tests/` at `0b1c70e`. The remaining
  foundation tests (`contract-serialization-defaults`, `http-api-service`, `mock-authentication-defaults`,
  `null-api-service`, `shared-problem-details-forward`) use `TimeWarp.Foundation.*` and correctly stay.
- **Package assembly name.** The cached nupkg ships `lib/net10.0` and `lib/net11.0`
  `timewarp-contracts.dll`. The forward test's `ShouldBe("timewarp-contracts")` therefore still holds.
  The `TypeForwardedTo` in `shared-problem-details-type-forward.cs` resolves against the package.
- **No stale references.** `git grep -i timewarp-contracts` (excluding kanban) leaves only the
  `Directory.Packages.props` provenance comment and the forward test's assembly name. No slnx,
  workflow, template.json, dev-cli, or Design-region text points at the old source path. Analyzer
  and generator metadata names (`TimeWarp.Contracts.OperationAttribute` / `DestructiveAttribute` /
  `OpenWorldAttribute`) are unchanged.
- **Consumer pin, not platform pin.** `TemplateSmokeHarness.PlatformPinIncludeFragments` has never
  listed `TimeWarp.Contracts`. `msbuild/timewarp-platform-packages.props` has no entry for it, and
  `dev check-version`'s package list (discovered from packable `source/` projects) omits it. The
  task asked to "update dev check-version". The restatement correctly found that no code change was
  needed because discovery drops the deleted project. Verified.
- **Smoke feed mapping.** Without the `TimeWarp.Contracts` `<package pattern>` in the smoke-local
  feed, the id falls through to nuget.org. The smoke run proves generated `web-contracts` (which
  uses `global using TimeWarp.Contracts`) restores and builds.

## Gates re-run by reviewer (2026-11-01, this worktree)

| Gate | Result |
|------|--------|
| `dotnet run tools/dev-cli/dev.cs -- build` | 0 warnings, 0 errors |
| `tests/foundation/foundation-contracts-tests` (`dotnet test -c Release --no-build`) | 25/25 |
| `tests/analyzers/timewarp-architecture-sourcegenerator-tests` | 154/154 |
| `dev check-version` | exit 0; package list omits `TimeWarp.Contracts` |
| `dev lint-skills` | 9/9 + eval.yaml valid |
| `dev template-smoke` (default variant) | exit 0, `Template smoke SUCCEEDED`; aggregators 442/442, 9/9, 14/14 |
| `ganda repo audit` | exit 0; 34 pass, 1 fail = `kanban-landed` warning naming other tasks' claims (070-001, 104-02x, 123, 141, 198, 244, 454), not this change |

The dev-cli was run through the runfile, not the AOT `./bin/dev`, because this diff edits dev-cli sources.

## Issues

None.

## Observations (not findings)

- The `Directory.Packages.props` comment names `2.0.0-beta.30` as the version timewarp-contracts task
  002 released. That is provenance, and it stays true after a later pin bump. It is not a second
  copy of the pin.
- Branch is 2 commits behind `master`; both are `land kanban 459` board commits touching only this
  task's `task.md`. No product conflict.
