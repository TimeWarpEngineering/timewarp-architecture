# Round 2 — general
**Date:** 2026-10-18
**Scope reviewed:** fix delta `328f1acf9..04788f1d4` (round-1 base to HEAD): commits
`6e2164773` (version with the repo, M2 readers and tests) and `04788f1d4` (smoke feed and
publish-smoke wait list). Product paths: `endpoint-metadata.cs`, `contracts-generator.cs`,
`timewarp-contracts.csproj`, package `readme.md`, the two generator test files, the new
`operation-attribute-name-tests.cs`, the sourcegenerator test csproj, `template-smoke-command.cs`,
`template-publish-smoke-command.cs`, `workflow-command.cs`. Kitchen: task.md, 384-001.

## Summary

The fix pass removes the package's own `<Version>`, so `TimeWarp.Contracts` takes
`2.0.0-beta.20` from `source/Directory.Build.props`. That follows the human decision on M1.
Both `[Operation]` readers now treat a whitespace name as no operation and a whitespace
description as absent. Tests compile against the real `OperationAttribute`. The smoke-local
source map and the publish-smoke wait list name `TimeWarp.Contracts`, because
`TimeWarp.Foundation.*` does not match that id. Risk is low and no new defect was found.

## Re-verification of round-1 findings

- **M1 (bug) — fixed.** `dotnet msbuild timewarp-contracts.csproj -getProperty:Version`
  returns `2.0.0-beta.20` with PackageId `TimeWarp.Contracts`, the same as
  `foundation-contracts`. `dotnet run tools/dev-cli/dev.cs -- check-version` lists
  `TimeWarp.Contracts` among the packages checked, says the source is ahead of nuget
  `2.0.0-beta.19`, and passes. The stale comments in `workflow-command.cs` and
  `template-smoke-command.cs` are gone. No `1.0.0-beta.1` reference is left in
  `source/`, `tools/`, or `tests/`. No new version check was added, as the human decision
  required. Since generated apps get `TimeWarp.Contracts` only as a transitive dependency of
  `TimeWarp.Foundation.Contracts`, the smoke `packageSourceMapping` entry and the publish-smoke
  flatcontainer wait are both needed. Without the first, smoke restore of `2.0.0-smoke` could
  not find the package. Without the second, publish-smoke could race nuget indexing.
- **M2 (suggestion) — fixed.** `ContractsGenerator` (`contracts-generator.cs:284`, `:294`)
  and `EndpointEmitModel.ReadOperation` (`endpoint-metadata.cs:281-283`, plus
  `GetNamedStringArgument` at `:338`) use the same rule: a whitespace name returns no
  operation and a whitespace description becomes null. This matches
  `ArgumentException.ThrowIfNullOrWhiteSpace` in the attribute constructor. The tests reference
  the real `timewarp-contracts` project and assert that the attribute binds as a non-error
  symbol before running the generator, so a stub can no longer mask a rename.
  `operation-attribute-name-tests.cs` pins the FQN.

## Gates run (this round, in the claim worktree)

- `dotnet run --project tests/analyzers/timewarp-architecture-sourcegenerator-tests/...csproj`:
  135 total, 0 failed.
- `dotnet run --project tests/foundation/foundation-contracts-tests/...csproj`: 26 total,
  0 failed.
- `dev check-version` (runfile): passes and lists `TimeWarp.Contracts`.
- Not re-run: full `dev build`, `dev template-smoke`, `ganda repo audit`. task.md Results
  reports all three passing on the fix pass. The two test builds compile the touched
  projects with warnings as errors and succeeded.

## Goal restatement check

`## Goal (implementer restatement)` says "at the repo version". That matches the task's
Versioning section as amended by the recorded human decision (2026-10-18, round 1 M1). It
keeps step 2 scope and leaves the analyzer, `IQuery`/`ICommand`, roles slice, and skill on
384-001. No misread.

## Issues

None.

Observation, not a finding: the 384-001 file slug still ends in
`promote-timewarpcontracts-to-100`, while its title and body no longer include promotion.
That is a kanban filename, not product behavior. Leave it for whoever next claims 384-001.
