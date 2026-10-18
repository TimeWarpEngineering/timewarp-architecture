# Round 1 — general
**Date:** 2026-10-18
**Scope reviewed:** branch vs `master`: `timewarp-contracts.csproj`, its `readme.md`, `source/Directory.Build.props`, `timewarp-templates/Directory.Build.props`, `Directory.Packages.props`, task.md

## Summary

The project clears the inherited `TargetFramework` and sets `TargetFrameworks` to `net10.0;net11.0`. The net10.0 inner build keeps the convention analyzers by putting `SetTargetFramework=TargetFramework=net11.0` on the existing analyzer `ProjectReference` through `Update`. The version and all twelve platform pins move to 2.0.0-beta.21 in one commit (task 124 policy). The goal restatement matches the task. Risk is low and I found no defects.

Checks I ran myself, not taken from the implementer's report:

- `unzip -l` on `artifacts/packages/TimeWarp.Contracts.2.0.0-beta.21.nupkg`: it has `lib/net10.0` and `lib/net11.0`, each with a dll and an xml. The nuspec has two dependency groups, `net10.0` and `net11.0`, each with `OneOf` 3.0.271 and `TimeWarp.Mediator.Contracts` 14.0.0-beta.5.
- `dotnet build` of the csproj, Release: 0 warnings, 0 errors, both TFMs.
- `ResolveProjectReferences -getItem:Analyzer` at `TargetFramework=net10.0`: it resolves `convention-analyzers/bin/Release/net11.0/timewarp-architecture-convention-analyzers.dll`. The `Update` path matches the `Include` from `source/Directory.Build.props`, so net10 compilations still run the analyzers. In package mode (`UseAnalyzerPackages=true`) the `Update` matches nothing and does nothing, which is correct.
- Throwaway net10.0 console in `/tmp/rv387-*` with an isolated package folder and the local feed: `[Operation("proof.ping")]` on `IRequest<OneOf<R, SharedProblemDetails>>` builds with 0 warnings and 0 errors, and `project.assets.json` compiles `lib/net10.0/timewarp-contracts.dll`. `SharedProblemDetails` is in namespace `TimeWarp.Foundation.Types` (existing design, outside this diff).
- `tests/foundation/foundation-contracts-tests`: 26 total, 0 failed.
- `./bin/dev check-version`: exit 0, one prerelease increment ahead of beta.20, `TimeWarp.Contracts` listed.
- No `2.0.0-beta.20` is left in props, targets or csproj files. The only remaining hits are fixture file names in `tests/tools/dev-cli-tests/template-nupkg-tests.cs`, which are not version pins.

## Issues

None.
