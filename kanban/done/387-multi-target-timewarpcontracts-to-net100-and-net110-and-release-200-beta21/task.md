# Multi-target TimeWarp.Contracts to net10.0 and net11.0 and release 2.0.0-beta.21

## Goal (implementer restatement)

Ship a `TimeWarp.Contracts` package that a net10.0 project can restore, while this repository keeps building it for net11.0. Clear the inherited single `TargetFramework` and multi-target `net10.0;net11.0`, pack both assemblies with their XML docs and a dependency group per framework, and leave `TimeWarp.Foundation.Contracts` on net11.0 with the `SharedProblemDetails` type forward intact. Bump the repo version and the matching platform pins to `2.0.0-beta.21` so `dev check-version` passes. Prove a throwaway net10.0 project compiles `[Operation]` plus `OneOf<Response, SharedProblemDetails>` against the packed nupkg. Publishing to nuget.org stays the human release step after merge.

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

- [x] csproj multi-targets net10.0;net11.0; nothing else keyed on TargetFramework breaks
- [x] nupkg verified: lib/net10.0 and lib/net11.0, two dependency groups
- [x] Version 2.0.0-beta.21 (+ equal pins); dev check-version passes
- [x] dev build, both test projects, template-smoke, repo audit green
- [x] net10.0 consumer proof recorded in Results
- [ ] After merge: released; nuget.org URL recorded; Roslynk 002 unblocked

## Review

- Round 1: Done

## Notes

- Unblocks Roslynk 002 (fork TimeWarpEngineering/Roslynk, blocked 2026-10-18 at implement) and Nuru 508-001.
- Consider whether the package should also target `netstandard2.0` for analyzer or generator consumers; not required now (this repo's analyzers match `[Operation]` by metadata name, no assembly reference). Record the decision either way.
- Parent context: 384 (package introduced), 386 (template adoption), timewarp-nuru 508.

## Session

- Created: 3018120 (2026-10-18)
- Implementation: 2026-10-18. Multi-target, version bump, gates, and the net10 consumer proof are on this branch. nuget.org publish stays the human `tw-release` step after merge.
- Implement oracle: implemented by implementer-grok (grok, model grok-4.7), session not reported, max-turns 500, Done — 2026-10-18T07:52:13Z
- Review oracle: tw-implementation-review, effort 1, roster general (claude, model claude-opus-5-5), session not reported, max-turns 80, 1 round, disposition clean — 2026-10-18
- Review oracle: review by reviewer-claude (claude, model claude-opus-5-5), session not reported, max-turns 80 — 2026-10-18T07:56:53Z

## Results

`TimeWarp.Contracts` builds and packs for `net10.0` and `net11.0` at repo version `2.0.0-beta.21`. `TimeWarp.Foundation.Contracts` stays `net11.0`. Its project reference still pulls `TimeWarp.Contracts` at that version, and `SharedProblemDetails` still resolves from `timewarp-contracts` (foundation-contracts-tests, including the type-forward test).

`netstandard2.0` is not a target. This repo's analyzers match `[Operation]` by metadata name and do not reference the assembly. `net10.0` covers the Nuru and Roslynk hosts named in the task.

The net10 inner build cannot project-reference the net11 convention analyzers. `SetTargetFramework=TargetFramework=net11.0` on that reference is the SDK escape hatch, so the net10 compilation still runs the analyzers and the analyzer assembly stays net11.0.

`artifacts/packages/TimeWarp.Contracts.2.0.0-beta.21.nupkg` (`unzip -l` plus the nuspec):

- `lib/net10.0/timewarp-contracts.dll` and `timewarp-contracts.xml`
- `lib/net11.0/timewarp-contracts.dll` and `timewarp-contracts.xml`
- dependency groups `net10.0` and `net11.0`, each `OneOf` 3.0.271 and `TimeWarp.Mediator.Contracts` 14.0.0-beta.5

A throwaway net10.0 console at `/tmp/tw-contracts-net10-proof` (not committed) restored that nupkg from `artifacts/packages`. `project.assets.json` compiles `lib/net10.0/timewarp-contracts.dll`. The program defines `[Operation("proof.ping")]` on `Query : IRequest<OneOf<Response, SharedProblemDetails>>` and builds with 0 warnings and 0 errors.

`dev template-smoke` packs `TimeWarp.Contracts.2.0.0-smoke.nupkg` with the same two `lib` folders, evicts the stale cache entry, and the generated SmokeDefault app restores `TimeWarp.Contracts/2.0.0-smoke` as `lib/net11.0/timewarp-contracts.dll`. Template smoke succeeded.

nuget.org does not have 2.0.0-beta.21 yet. `dev check-version` reports source `2.0.0-beta.21` is one prerelease increment ahead of NuGet `2.0.0-beta.20` and lists `TimeWarp.Contracts`. The human release after merge is `dev release` from a clean synced master (`tw-release`). Record `https://www.nuget.org/packages/TimeWarp.Contracts/2.0.0-beta.21` in this section when that package is live. Roslynk 002 and Nuru 508-001 take that version after the cut.

### Review

Disposition is **clean**: 1 round, effort 1, one `general` reviewer. Final counts are 0 for every severity (bug, suggestion, nit) and every status (open, fixed, wontfix). The reviewer re-ran these checks:

- the nupkg `lib/` layout and the two nuspec dependency groups
- a net10.0 `ResolveProjectReferences` that resolves the net11.0 convention analyzer
- a fresh net10.0 consumer build against the local feed, which compiles `lib/net10.0`
- foundation-contracts-tests (26 passed)
- `dev check-version` (exit 0)

`SharedProblemDetails` is in namespace `TimeWarp.Foundation.Types`, so the consumer proof needs that `using`. Artifacts are in `review/review-framework.md`, `review/round-1/general.md`, `review/round-1/merged.md` and `review/disposition.md`.

### How to validate

**Smoke**

```bash
unzip -l artifacts/packages/TimeWarp.Contracts.2.0.0-beta.21.nupkg
unzip -p artifacts/packages/TimeWarp.Contracts.2.0.0-beta.21.nupkg TimeWarp.Contracts.nuspec
cd tests/foundation/foundation-contracts-tests && dotnet test -c Release --no-build --no-restore
cd tests/analyzers/timewarp-architecture-sourcegenerator-tests && dotnet test -c Release --no-build --no-restore
```

The throwaway consumer, from a clean directory with a NuGet.config that lists `artifacts/packages` and nuget.org:

```bash
dotnet new console -n proof -f net10.0 --no-restore
# PackageReference TimeWarp.Contracts 2.0.0-beta.21
# [Operation("proof.ping")] class Query : IRequest<OneOf<Response, SharedProblemDetails>>
dotnet build proof.csproj -c Release
```

**Expect**

The nupkg lists `lib/net10.0` and `lib/net11.0` assemblies and XML docs. The nuspec has two dependency groups, `net10.0` and `net11.0`. foundation-contracts-tests reports 26 passed. sourcegenerator-tests reports 135 passed. The net10.0 consumer build succeeds and its assets file compiles `lib/net10.0/timewarp-contracts.dll`.

**Automated gate**

```bash
./bin/dev check-version
./bin/dev build
./bin/dev template-smoke
ganda repo audit
```

`dev check-version` exits 0 with source `2.0.0-beta.21` ahead of NuGet `2.0.0-beta.20`. `dev build` is 0 warnings and 0 errors. `dev template-smoke` prints `Template smoke SUCCEEDED`. `ganda repo audit` passes.
