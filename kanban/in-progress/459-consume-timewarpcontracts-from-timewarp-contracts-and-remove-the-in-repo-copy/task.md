# Consume TimeWarp.Contracts from timewarp-contracts and remove the in-repo copy

## Description

`TimeWarp.Contracts` moves to its own repository, TimeWarpEngineering/timewarp-contracts (task
002 there). Task 384 recorded the plan: build it here first, extract it before Nuru's first
release that references it. Nuru has referenced it since v4.0.0-beta.1, so the extraction is due.

This task deletes `source/foundation/timewarp-contracts/` and consumes the published package
instead. It starts after timewarp-contracts 002 has released a version to nuget.org. That
dependency is in another repo, so it is a Note here, not a `## Depends on` id.

## Goal (implementer restatement)

This repository stops building `TimeWarp.Contracts` and consumes the package timewarp-contracts task 002 published: `2.0.0-beta.30` on nuget.org (release `v2.0.0-beta.30`, repository commit `0b1c70e3`).

This walk is done when:

- `source/foundation/timewarp-contracts/` and its `timewarp-architecture.slnx` entry are gone.
- `Directory.Packages.props` pins `TimeWarp.Contracts` at `2.0.0-beta.30`. That pin is a consumer pin. `dev check-version` discovers packable projects under `source/`, so deleting the project drops the id from the version gate. The pin stays off the platform pin lists (`msbuild/timewarp-platform-packages.props` and `PlatformPinIncludeFragments`).
- `foundation-contracts` and the source-generator tests reference the package. `SharedProblemDetails` still type-forwards, and `shared-problem-details-forward-tests.cs` passes.
- The six Jaribu files task 002 moved are deleted here: `operation-attribute-group-tests.cs`, `operation-attribute-name-tests.cs`, `operation-attribute-title-tests.cs`, `operation-catalog-group-tests.cs`, `operation-catalog-tool-hint-tests.cs`, and `tool-hint-attribute-tests.cs`. Foundation tests that need `TimeWarp.Foundation.*` stay.
- `template-smoke` and `template-publish-smoke` no longer pack or wait for `TimeWarp.Contracts` as a package this repo publishes. The release pack list drops the deleted project. Generated apps restore the package from nuget.org.
- Skills and `AGENTS.md` name the package. `dev lint-skills` passes.
- Analyzer metadata names stay `TimeWarp.Contracts.OperationAttribute`, `DestructiveAttribute`, and `OpenWorldAttribute`.
- `TimeWarp.Nuru` and `TimeWarp.Nuru.DevCli` are pinned at `4.0.0-beta.11`, the latest on nuget.org. `ganda repo audit --fix` is committed and `ganda repo audit` is clean.
- `dev build` is 0 warnings and 0 errors. The two test projects, `dev template-smoke`, `dev check-version`, and `dev lint-skills` pass.

## Requirements

- Delete `source/foundation/timewarp-contracts/` and its entry in `timewarp-architecture.slnx`.
- Pin `TimeWarp.Contracts` in `Directory.Packages.props` at the version timewarp-contracts 002
  released. It is no longer a platform pin that must equal `<Version>`. Update `dev check-version`
  (and any platform pin list in `msbuild/timewarp-platform-packages.props`) so it does not
  require that.
- `source/foundation/foundation-contracts/foundation-contracts.csproj`: replace the
  `ProjectReference` to `timewarp-contracts` with `PackageReference Include="TimeWarp.Contracts"`.
  The `SharedProblemDetails` type forward stays and must still resolve
  (`shared-problem-details-forward-tests.cs` passes).
- `tests/analyzers/timewarp-architecture-sourcegenerator-tests`: replace its `ProjectReference`
  to `timewarp-contracts` with the package reference.
- `source/foundation/Directory.Build.props`: remove the `timewarp-contracts`
  `AssemblyMarkerNamespace` line.
- `tests/foundation/foundation-contracts-tests/`: delete the tests that moved to
  timewarp-contracts in its task 002 (the `operation-attribute-*`, `operation-catalog-*`, and
  `tool-hint-attribute-tests.cs` files that 002 moved). Delete only files 002 actually moved.
- dev-cli: `template-smoke` and `template-publish-smoke` no longer pack `TimeWarp.Contracts` into
  the local feed. Remove it from their package lists and the local-feed `<package pattern>`
  mapping (`tools/dev-cli/endpoints/template-smoke-command.cs`,
  `tools/dev-cli/endpoints/template-publish-smoke-command.cs`). Generated apps restore it from
  nuget.org like any other dependency. Fix the comments that say this repo packs it.
- Generated apps: confirm a generated app still compiles `[Operation]` contracts
  (`source/container-apps/web/projects/web-contracts/global-usings.cs` uses
  `TimeWarp.Contracts`). `dev template-smoke` covers this.
- Skills: `skills/tw-web-api-contracts/` (`SKILL.md`, `references/neutral-operations.md`) and
  `AGENTS.md` name the package, not its location. Change any text that says it is built in this
  repo or versions with it. Run `dev lint-skills`.
- Analyzers keep matching `TimeWarp.Contracts.OperationAttribute`, `DestructiveAttribute`, and
  `OpenWorldAttribute` by metadata name. No analyzer change is expected; Design-region comments
  that point at the old source path get the new repo instead.
- Gates: `dev build` (0 warnings, 0 errors), both test projects above, `dev template-smoke`,
  `dev check-version`, `ganda repo audit`, `dev lint-skills`.
- Audit fix rides along. When this spec was published, `ganda repo audit` failed two checks:
  `nuru` (TimeWarp.Nuru 4.0.0-beta.10 pinned, 4.0.0-beta.11 latest on nuget.org) and
  `required-gitignore-entries` (root `.gitignore` missing required entries). Run
  `ganda repo audit --fix` and commit what it changes. Bump the Nuru pin to the latest version
  at implement time and fix whatever that bump breaks. `ganda repo audit` must be clean.

## Checklist

- [x] timewarp-contracts 002 released; version recorded here
- [x] In-repo project and slnx entry deleted
- [x] Package pin added; check-version no longer ties it to `<Version>`
- [x] foundation-contracts and sourcegenerator tests use the package; type forward test passes
- [x] Moved tests deleted here
- [x] dev-cli smoke commands stop packing it
- [x] Skills and AGENTS.md updated; `dev lint-skills` green
- [x] `ganda repo audit --fix` committed; Nuru pin at latest; audit clean
- [x] Gates green

## Results

`TimeWarp.Contracts` `2.0.0-beta.30` is the nuget.org package from TimeWarpEngineering/timewarp-contracts task 002 (GitHub release `v2.0.0-beta.30`, repository commit `0b1c70e3`). `Directory.Packages.props` pins that version. The pin is a consumer pin: the id is absent from `msbuild/timewarp-platform-packages.props` and from `PlatformPinIncludeFragments`. `dev check-version` lists packable projects under `source/` and the deleted project is off that list.

`source/foundation/timewarp-contracts/` and its solution entry are deleted. `foundation-contracts` and `timewarp-architecture-sourcegenerator-tests` take `PackageReference Include="TimeWarp.Contracts"`. The `SharedProblemDetails` type forward stays. The six Jaribu files task 002 moved are deleted: `operation-attribute-group-tests.cs`, `operation-attribute-name-tests.cs`, `operation-attribute-title-tests.cs`, `operation-catalog-group-tests.cs`, `operation-catalog-tool-hint-tests.cs`, and `tool-hint-attribute-tests.cs`.

`template-smoke` packs the platform projects into the local feed and leaves `TimeWarp.Contracts` on nuget.org. `template-publish-smoke` waits on packages this repo publishes. `workflow` pack list omits the deleted project. `AGENTS.md` and `skills/tw-web-api-contracts` name the package and its own version line. Analyzer metadata names are unchanged.

Audit ride-along: `TimeWarp.Nuru` and `TimeWarp.Nuru.DevCli` are `4.0.0-beta.11`. `ganda repo audit --fix` added `bin/dev`, `bin/dev.dbg`, and `bin/dev.exe` to `.gitignore`.

Gates on 2026-11-01:

- `dev build`: 0 warnings, 0 errors (28.80s).
- `tests/foundation/foundation-contracts-tests`: 25 passed, 0 failed (Release, `--no-build`). The forward test is in that run.
- `tests/analyzers/timewarp-architecture-sourcegenerator-tests`: 154 passed, 0 failed.
- `dev check-version`: exit 0. Source `2.0.0-beta.30` is one prerelease ahead of architecture nuget `2.0.0-beta.29`. Packages checked: TimeWarp.402, TimeWarp.Architecture.Analyzers, TimeWarp.Architecture.Attributes, TimeWarp.Architecture.Generators, TimeWarp.Foundation.Application, TimeWarp.Foundation.Contracts, TimeWarp.Foundation.Domain, TimeWarp.Foundation.Infrastructure, TimeWarp.Foundation.Server, TimeWarp.Identity, TimeWarp.Modules.
- `dev lint-skills`: 9/9, eval.yaml valid.
- `dev template-smoke`: exit 0 in 273s. `Template smoke SUCCEEDED`. SmokeDefault build 0 warnings, 0 errors, including `web-contracts.dll`. Standalone co-located tests 5/5, 5/5, and 2/2. Jaribu aggregators 442/442, 9/9, and 14/14.
- `ganda repo audit`: exit 0. `nuru` pass. `required-gitignore-entries` pass. One advisory warning, `kanban-landed`, names other claims (070-001, 104-024, 104-025, 104-026, 123, 141, 198, 244, 454) whose worktrees are missing. That warning is non-blocking.

### How to validate

**Smoke:**

```bash
./bin/dev build
( cd tests/foundation/foundation-contracts-tests && dotnet test -c Release )
( cd tests/analyzers/timewarp-architecture-sourcegenerator-tests && dotnet test -c Release )
./bin/dev check-version
./bin/dev template-smoke
./bin/dev lint-skills
ganda repo audit
```

Run the two `dotnet test` commands from each project directory. Microsoft.Testing.Platform rejects `dotnet test <csproj>` on the .NET 10 SDK.

**Expect:** `dev build` reports 0 warnings and 0 errors. Both test suites pass, and the foundation suite still asserts `SharedProblemDetails` lives in assembly `timewarp-contracts`. `dev check-version` exits 0 and its package list omits `TimeWarp.Contracts`. `dev template-smoke` prints `Template smoke SUCCEEDED` and the generated `web-contracts` project builds. `dev lint-skills` and `ganda repo audit` exit 0. `Directory.Packages.props` contains `<PackageVersion Include="TimeWarp.Contracts" Version="2.0.0-beta.30" />`, and `source/foundation/timewarp-contracts/` is absent.

## Notes

- Waits on timewarp-contracts task 002 (cross-repo; nuget.org release, not just merge).
- Do not change timewarp-contracts from this task. A missing feature or bug there is reported back,
  not fixed here.
- Nuru and Roslynk pins are their own agents' job.

## Session

- Created: 8c10267a-9869-468f-94f3-f663f4e20b06 (2026-10-31)
- Implementation: Grok 4.7 (ganda task work), grok session 01a19559-359c-7fd0-9997-88a47bd70dd2 (2026-11-01). Pinned TimeWarp.Contracts 2.0.0-beta.30, deleted the in-repo project, bumped TimeWarp.Nuru to 4.0.0-beta.11, and committed the audit gitignore lines. `kanban-landed` advisory is other claims.
- Implement oracle: implemented by implementer-grok (grok, vendor xai, model grok-4.7, response model grok-4.7-build), session 01a19559-359c-7fd0-9997-88a47bd70dd2, max-turns 500, Done — 2026-11-01T04:45:38Z
