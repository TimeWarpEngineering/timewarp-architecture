# Consume TimeWarp.Contracts from timewarp-contracts and remove the in-repo copy

## Description

`TimeWarp.Contracts` moves to its own repository, TimeWarpEngineering/timewarp-contracts (task
002 there). Task 384 recorded the plan: build it here first, extract it before Nuru's first
release that references it. Nuru has referenced it since v4.0.0-beta.1, so the extraction is due.

This task deletes `source/foundation/timewarp-contracts/` and consumes the published package
instead. It starts after timewarp-contracts 002 has released a version to nuget.org. That
dependency is in another repo, so it is a Note here, not a `## Depends on` id.

## Goal (implementer restatement)

The extraction is already on this branch: this repository no longer builds `TimeWarp.Contracts` and consumes the package from timewarp-contracts. This walk rolls the open PR forward to the packages nuget.org has now, and keeps the audit clean, before merge.

This walk is done when:

- `Directory.Packages.props` pins every package `ganda nuget outdated` reports to the latest on nuget.org. The named moves are `TimeWarp.Contracts` `2.0.0-beta.30` to `2.0.0-beta.31`, the seven `TimeWarp.State*` packages `12.0.0-beta.15` to `12.0.0-beta.16`, `Scalar.AspNetCore` `2.18.6` to `2.18.7`, and `Microsoft.OpenApi` `2.13.0` to `3.11.0`. If ASP.NET Core's OpenAPI package pins `Microsoft.OpenApi` below 3, the walk stops and records the restore error. It does not force a conflicting pin.
- `TimeWarp.Mediator` moves forward to 15.x. `ganda nuget outdated` skips prereleases on the stable `14.0.0` pin, so the walk reads the timewarp-mediator `v15.0.0-beta.4` notes and follows any package split or rename. Pins never move backward. A defect that belongs in timewarp-mediator stops the walk. That repository is not edited from here.
- `ganda repo audit --fix` is committed and `ganda repo audit` is clean.
- The extraction stays intact: the in-repo project is gone, the contracts pin stays a consumer pin, the type forward still resolves, and the smoke commands do not pack `TimeWarp.Contracts`.
- `dev build` is 0 warnings and 0 errors. The two test projects, `dev template-smoke`, `dev check-version`, `dev lint-skills`, and `ganda repo audit` pass. Results names the final pins.

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
- [x] Outdated packages moved to the latest on nuget.org; `Microsoft.OpenApi` stays `2.13.0` after NU1608
- [x] `TimeWarp.Mediator` `15.0.0-beta.4` (package id reused; analyzers ride in that package)
- [x] Gates re-run after the roll

## Results

`TimeWarp.Contracts` `2.0.0-beta.31` is the nuget.org package this repo consumes. The extraction still follows timewarp-contracts task 002 (`v2.0.0-beta.30`, commit `0b1c70e3`); the pin moved because nuget.org now has `2.0.0-beta.31`. The pin is a consumer pin: the id is absent from `msbuild/timewarp-platform-packages.props` and from `PlatformPinIncludeFragments`. `dev check-version` lists packable projects under `source/` and the deleted project is off that list.

`source/foundation/timewarp-contracts/` and its solution entry are deleted. `foundation-contracts` and `timewarp-architecture-sourcegenerator-tests` take `PackageReference Include="TimeWarp.Contracts"`. The `SharedProblemDetails` type forward stays. The six Jaribu files task 002 moved are deleted: `operation-attribute-group-tests.cs`, `operation-attribute-name-tests.cs`, `operation-attribute-title-tests.cs`, `operation-catalog-group-tests.cs`, `operation-catalog-tool-hint-tests.cs`, and `tool-hint-attribute-tests.cs`.

`template-smoke` packs the platform projects into the local feed and leaves `TimeWarp.Contracts` on nuget.org. `template-publish-smoke` waits on packages this repo publishes. `workflow` pack list omits the deleted project. `AGENTS.md` and `skills/tw-web-api-contracts` name the package and its own version line. Analyzer metadata names are unchanged.

Audit ride-along from the extraction: `TimeWarp.Nuru` and `TimeWarp.Nuru.DevCli` are `4.0.0-beta.11`. `ganda repo audit --fix` added `bin/dev`, `bin/dev.dbg`, and `bin/dev.exe` to `.gitignore`. The package-roll audit on 2026-11-01 changed nothing further.

Package roll (scope add 2026-11-01). `ganda nuget outdated` is clean except the OpenApi major below.

- `TimeWarp.Contracts` `2.0.0-beta.31`.
- `TimeWarp.Mediator` `15.0.0-beta.4`. Release `v15.0.0-beta.4` reuses that id for the contracts assembly and ships the analyzers in it. `TimeWarp.Mediator.Contracts` and `TimeWarp.Mediator.Analyzers` stop at `14.0.0` and are no longer referenced. `TimeWarp.Mediator.Generators` is `15.0.0-beta.4` with `PrivateAssets=all` on the hosts. Handlers, notifications, and pipeline behaviors return `ValueTask`. Behaviors take `MessageHandlerDelegate<TRequest, TResponse>` and call `next(request, cancellationToken)`. `AddGeneratedMediator` is internal in `TimeWarp.Mediator.Generated`; the three hosts import that namespace inside their own namespace. `IStreamPipelineBehavior` is gone, so the debugger services page lists only `IPipelineBehavior`. `dev-cli` and `agent-identity-cli` stay on `TimeWarp.Nuru` `4.0.0-beta.11`, which still restores `TimeWarp.Mediator.Contracts` `14` transitively. Those compilations do not reference `TimeWarp.Mediator` `15`, so they keep `ICommandHandler`.
- `TimeWarp.State`, `TimeWarp.State.Blazor`, `TimeWarp.State.Plus`, `TimeWarp.State.Telemetry`, `TimeWarp.State.DevTools.Export`, `TimeWarp.State.DevTools.Hosting`, and `TimeWarp.State.DevTools.Wasm` are `12.0.0-beta.16`.
- `Scalar.AspNetCore` `2.18.7`.
- `Microsoft.OpenApi` stays `2.13.0`. `foundation-server` restore of `3.11.0` raised NU1608 (warning as error): `Microsoft.AspNetCore.OpenApi` `10.0.11` requires `Microsoft.OpenApi` (`>= 2.7.5 && < 3.0.0`) but version `Microsoft.OpenApi` `3.11.0` was resolved. The pin was not forced. The FastEndpoints major stays task 257.

Gates on 2026-11-01 after the roll:

- `dev build`: 0 warnings, 0 errors (10.29s, incremental after the restore that pulled the new packages).
- `tests/foundation/foundation-contracts-tests`: 25 passed, 0 failed (Release, from the project directory). The forward test is in that run.
- `tests/analyzers/timewarp-architecture-sourcegenerator-tests`: 154 passed, 0 failed.
- `dev check-version`: exit 0. Source `2.0.0-beta.30` is one prerelease ahead of architecture nuget `2.0.0-beta.29`. Packages checked: TimeWarp.402, TimeWarp.Architecture.Analyzers, TimeWarp.Architecture.Attributes, TimeWarp.Architecture.Generators, TimeWarp.Foundation.Application, TimeWarp.Foundation.Contracts, TimeWarp.Foundation.Domain, TimeWarp.Foundation.Infrastructure, TimeWarp.Foundation.Server, TimeWarp.Identity, TimeWarp.Modules. `TimeWarp.Contracts` is absent.
- `dev lint-skills`: 9/9, eval.yaml valid.
- `dev template-smoke`: exit 0 in 264s. `Template smoke SUCCEEDED`. SmokeDefault build 0 warnings, 0 errors, including `web-contracts.dll`. Standalone co-located tests 5/5, 5/5, and 2/2. Jaribu aggregators 442/442, 9/9, and 14/14.
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

**Expect:** `dev build` reports 0 warnings and 0 errors. Both test suites pass, and the foundation suite still asserts `SharedProblemDetails` lives in assembly `timewarp-contracts`. `dev check-version` exits 0 and its package list omits `TimeWarp.Contracts`. `dev template-smoke` prints `Template smoke SUCCEEDED` and the generated `web-contracts` project builds. `dev lint-skills` and `ganda repo audit` exit 0. `Directory.Packages.props` contains `<PackageVersion Include="TimeWarp.Contracts" Version="2.0.0-beta.31" />`, `<PackageVersion Include="TimeWarp.Mediator" Version="15.0.0-beta.4" />`, the seven `TimeWarp.State*` packages at `12.0.0-beta.16`, `Scalar.AspNetCore` at `2.18.7`, and `Microsoft.OpenApi` at `2.13.0`. `source/foundation/timewarp-contracts/` is absent.

### Review disposition

- **Outcome:** clean. 1 round, effort 3, roster `general`.
- **Final counts:** bug 0, suggestion 0, nit 0. None open, fixed, or wontfix.
- **Verified:** the deleted `.cs` sources and the six deleted tests are byte-identical to timewarp-contracts commit `0b1c70e`, the commit in the `2.0.0-beta.30` nuspec. The package assembly is `timewarp-contracts.dll`, so the type forward test holds.
- **Gates the reviewer re-ran on 2026-11-01:** `dev build` (0 warnings, 0 errors), foundation-contracts-tests 25/25, sourcegenerator-tests 154/154, `dev check-version` exit 0, `dev lint-skills` 9/9, `dev template-smoke` SUCCEEDED, `ganda repo audit` exit 0. The audit's only failure is the `kanban-landed` warning about other tasks' claims.
- **Artifacts:** `review/review-framework.md`, `review/round-1/general.md`, `review/round-1/merged.md`, `review/disposition.md`.

## Review

- Round 1: Done

## Notes

- Waits on timewarp-contracts task 002 (cross-repo; nuget.org release, not just merge).
- Do not change timewarp-contracts from this task. A missing feature or bug there is reported back,
  not fixed here.
- Nuru and Roslynk pins are their own agents' job.

## Session

- Created: 8c10267a-9869-468f-94f3-f663f4e20b06 (2026-10-31)
- Implementation: Grok 4.7 (ganda task work), grok session 01a19559-359c-7fd0-9997-88a47bd70dd2 (2026-11-01). Pinned TimeWarp.Contracts 2.0.0-beta.30, deleted the in-repo project, bumped TimeWarp.Nuru to 4.0.0-beta.11, and committed the audit gitignore lines. `kanban-landed` advisory is other claims.
- Implement oracle: implemented by implementer-grok (grok, vendor xai, model grok-4.7, response model grok-4.7-build), session 01a19559-359c-7fd0-9997-88a47bd70dd2, max-turns 500, Done — 2026-11-01T04:45:38Z
- Review oracle: Claude Opus 5.5 (claude-opus-5-5), body tw-implementation-review, effort 3, roster general, turn cap 200 — round 1, disposition clean (0 findings). The goal restatement matches the task and nothing was misread. The task asked to update `dev check-version`. The restatement found that discovery drops the deleted project, and the reviewer verified that. — 2026-11-01
- Review oracle: review by reviewer-claude (claude, vendor anthropic, model claude-opus-5-5), session not reported, max-turns 200 — 2026-11-01T04:55:07Z
- Scope add 2026-11-01 (Steven, PR #620 open): roll package updates and audit fixes into this PR before merge.
  - Update every package `ganda nuget outdated` reports, to the latest on nuget.org: `TimeWarp.Contracts` 2.0.0-beta.30 → 2.0.0-beta.31; `TimeWarp.State*` (7 packages) 12.0.0-beta.15 → 12.0.0-beta.16; `Scalar.AspNetCore` 2.18.6 → 2.18.7; `Microsoft.OpenApi` 2.13.0 → 3.11.0 (major: if ASP.NET Core's OpenAPI pins it below 3, report that with the restore error rather than forcing it).
  - TimeWarp.Mediator: `ganda nuget outdated` skips prereleases on a stable pin. nuget.org has `TimeWarp.Mediator.Generators` 15.0.0-beta.4, while `.Contracts` and `.Analyzers` stop at 14.0.0 (timewarp-mediator release v15.0.0-beta.4). Read the 15.0 release notes and migrate forward to 15.x, including any package split or rename. Never pin backward. If 15.x needs a change in timewarp-mediator, stop and report; do not change that repo.
  - Run `ganda repo audit --fix` and commit what it changes; the audit must be clean.
  - Re-run every gate in Requirements after the bumps (`dev build`, both test projects, `dev template-smoke`, `dev check-version`, `dev lint-skills`, `ganda repo audit`). Update Results with the final pins.
- Package roll: Grok 4.7 (ganda task work), session 01a1958a-bcdb-7e33-af6e-acd9a6a2ab1e (2026-11-01). Moved the outdated pins, migrated the product hosts to TimeWarp.Mediator 15.0.0-beta.4, left Microsoft.OpenApi at 2.13.0 after NU1608, and left the Nuru CLIs on Mediator.Contracts 14.
- Implement oracle: implemented by implementer-grok (grok, vendor xai, model grok-4.7, response model grok-4.7-build), session 01a1958a-bcdb-7e33-af6e-acd9a6a2ab1e, max-turns 500, Done — 2026-11-01T05:53:26Z
