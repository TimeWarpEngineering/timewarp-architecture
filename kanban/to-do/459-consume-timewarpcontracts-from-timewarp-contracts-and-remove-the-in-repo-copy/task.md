# Consume TimeWarp.Contracts from timewarp-contracts and remove the in-repo copy

## Description

`TimeWarp.Contracts` moves to its own repository, TimeWarpEngineering/timewarp-contracts (task
002 there). Task 384 recorded the plan: build it here first, extract it before Nuru's first
release that references it. Nuru has referenced it since v4.0.0-beta.1, so the extraction is due.

This task deletes `source/foundation/timewarp-contracts/` and consumes the published package
instead. It starts after timewarp-contracts 002 has released a version to nuget.org. That
dependency is in another repo, so it is a Note here, not a `## Depends on` id.

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

## Checklist

- [ ] timewarp-contracts 002 released; version recorded here
- [ ] In-repo project and slnx entry deleted
- [ ] Package pin added; check-version no longer ties it to `<Version>`
- [ ] foundation-contracts and sourcegenerator tests use the package; type forward test passes
- [ ] Moved tests deleted here
- [ ] dev-cli smoke commands stop packing it
- [ ] Skills and AGENTS.md updated; `dev lint-skills` green
- [ ] Gates green

## Notes

- Waits on timewarp-contracts task 002 (cross-repo; nuget.org release, not just merge).
- Do not change timewarp-contracts from this task. A missing feature or bug there is reported back,
  not fixed here.
- Nuru and Roslynk pins are their own agents' job.

## Session

- Created: 8c10267a-9869-468f-94f3-f663f4e20b06 (2026-10-31)
