# Multi-target TimeWarp.Contracts to net10.0 and net11.0 and release 2.0.0-beta.21

## Description

`TimeWarp.Contracts` 2.0.0-beta.20 on nuget.org contains only `lib/net11.0/timewarp-contracts.dll` with a single `net11.0` dependency group. The project inherits `<TargetFramework>net11.0</TargetFramework>` from the repository root `Directory.Build.props`. Its two intended consumers outside this repo both build on net10.0: TimeWarp.Nuru (root `Directory.Build.props`, net10.0; the net11 upgrade is Nuru task 481, not done) and Roslynk (`Source/Directory.Build.props`, net10.0). Roslynk task 002 is blocked on restore with NU1202 against beta.20, and Nuru 508-001 would block the same way.

The package is attributes, DTOs and one interface, so it has no reason to require net11. Multi-target it to `net10.0;net11.0`, bump the repository version so `dev check-version` passes, and release so a net10.0 asset is on nuget.org. Dependencies already allow it: `TimeWarp.Mediator.Contracts` 14.0.0-beta.5 ships `lib/net10.0`; `OneOf` is netstandard.

## Requirements

- `source/foundation/timewarp-contracts/timewarp-contracts.csproj` builds for `net10.0` and `net11.0`. The root props set `TargetFramework` unconditionally and are evaluated before the project body, so the project must clear it (`<TargetFramework></TargetFramework>`) and set `<TargetFrameworks>net10.0;net11.0</TargetFrameworks>`; check `Directory.Build.targets` and `source/foundation/Directory.Build.props` for anything keyed on `TargetFramework` (the `AssemblyMarkerNamespace` condition is on project name and is fine).
- The packed nupkg has `lib/net10.0` and `lib/net11.0` assemblies and XML docs, and two dependency groups. Verify with `unzip -l` on the artifact, not only by a green build.
- `TimeWarp.Foundation.Contracts` stays net11.0 and keeps depending on `TimeWarp.Contracts` at the repo version; the type-forward of `SharedProblemDetails` still resolves.
- Repository `<Version>` bumped to 2.0.0-beta.21 in `source/Directory.Build.props`, together with any platform `PackageVersion` pins that this monorepo requires to equal `<Version>` (task 124 convention), in the same commit. `dev check-version` passes.
- Existing gates green: `dev build`, `tests/analyzers/timewarp-architecture-sourcegenerator-tests` (135), `tests/foundation/foundation-contracts-tests` (26), `dev template-smoke` (it maps `TimeWarp.Contracts` to the local feed; confirm the multi-target package restores there), `ganda repo audit`.
- A net10.0 consumer proof in the task results: a throwaway net10.0 console or test project (not committed) restoring the packed nupkg from the local feed and compiling a class with `[Operation]` and `OneOf<Response, SharedProblemDetails>`.
- Release is the human `tw-release` gate after merge (`dev release` from clean synced master); record the nuget.org URL for 2.0.0-beta.21 in Results when it is live. Roslynk 002 and Nuru 508-001 reference that version.

## Checklist

- [ ] csproj multi-targets net10.0;net11.0; nothing else keyed on TargetFramework breaks
- [ ] nupkg verified: lib/net10.0 and lib/net11.0, two dependency groups
- [ ] Version 2.0.0-beta.21 (+ equal pins); dev check-version passes
- [ ] dev build, both test projects, template-smoke, repo audit green
- [ ] net10.0 consumer proof recorded in Results
- [ ] After merge: released; nuget.org URL recorded; Roslynk 002 unblocked

## Notes

- Unblocks Roslynk 002 (fork TimeWarpEngineering/Roslynk, blocked 2026-10-18 at implement) and Nuru 508-001.
- Consider whether the package should also target `netstandard2.0` for analyzer or generator consumers; not required now (this repo's analyzers match `[Operation]` by metadata name, no assembly reference). Record the decision either way.
- Parent context: 384 (package introduced), 386 (template adoption), timewarp-nuru 508.

## Session

- Created: 3018120 (2026-10-18)
