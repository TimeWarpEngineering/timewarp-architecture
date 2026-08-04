# Scaffold TimeWarp.402 package in monorepo

## Parent

104

## Description

Create TimeWarp.402 project, solution wiring, AssemblyMarker. Keep payment graph loadable without poisoning free-route cold starts in hosts (software tip pattern: isolate tip middleware).

## Requirements

- Builds clean
- Package name **TimeWarp.402**
- Ready for challenge/settle types

## Checklist

- [x] csproj + solution
- [x] AssemblyMarker (generated via Directory.Build.targets + AssemblyMarkerNamespace)
- [x] Purpose on seed files (no hand-authored `.cs` — overview.md documents layout; generated marker only)

## Notes

Can proceed once PrincipalId type/location known from 002.

### Depends on

104-002 (PrincipalId) — known at `TimeWarp.Identity.PrincipalId`; this scaffold
does **not** reference Identity yet (empty surface; ledger/composition later).

### Implementation plan (104-007) — overnight 2026-08-04

Mirror **104-001** Identity scaffold (libraries peer, not foundation).

| Item | Value |
|------|--------|
| Path | `source/libraries/timewarp-402/` |
| csproj | `timewarp-402.csproj` |
| PackageId / Title | `TimeWarp.402` (locked epic name) |
| C# namespace / AssemblyMarkerNamespace | `TimeWarp.X402` — `TimeWarp.402` is not a legal C# identifier |
| AssemblyName | default kebab (`timewarp-402`) |

**Create:** packable csproj (no deps yet); optional `overview.md` + Purpose-bearing
seed if any hand-authored `.cs` (prefer generated `IAssemblyMarker` only, like
post-generation identity — no hand marker file).

**Wire:** slnx `/source/libraries/`; `PackableProjects` + Design in
`workflow-command.cs`; template exclude + `VendoredPlatformRelativeTrees` +
smoke pack list in `template-smoke-command.cs` / `template.json` (same dual-mode
exclude as identity/modules). **Do not** add CPM `PackageVersion` or
`Use402Packages` yet; **do not** mass-wire hosts.

**Verify:** `dev build` 0/0; optional `dotnet pack` → `TimeWarp.402.*.nupkg`.

**Out of scope:** challenge/settle (008), tip (009), ledger (010), app refs,
PrincipalId project ref (later tasks).

## Session

- Created: 2026-07-16
- Plan + implement: 2026-08-04 overnight (continuous session, epic 104)
- Review: clean (effort 1, round 1)

## Results

### Summary

Scaffolded empty packable **TimeWarp.402** under `source/libraries/timewarp-402/`,
peer to Identity/Modules. PackageId is the locked product name; C# surface uses
**`TimeWarp.X402`** because `TimeWarp.402` is not a legal namespace. Wired into
solution, pack pipeline, and template dual-mode exclude. No host references, no
protocol types (008+).

### Files changed

| Action | Path |
|--------|------|
| Created | `source/libraries/timewarp-402/timewarp-402.csproj` |
| Created | `source/libraries/timewarp-402/overview.md` |
| Edited | `source/libraries/Directory.Build.props` (AssemblyMarkerNamespace) |
| Edited | `timewarp-architecture.slnx` |
| Edited | `tools/dev-cli/endpoints/workflow-command.cs` (PackableProjects) |
| Edited | `tools/dev-cli/endpoints/template-smoke-command.cs` (pack + vendored exclude) |
| Edited | `.template.config/template.json` (exclude tree) |

### Key decisions

- Location: `source/libraries/` not foundation (PackageId product capability, not Foundation.*)
- Namespace split: PackageId `TimeWarp.402` / C# `TimeWarp.X402` (documented in overview + csproj)
- Empty surface: generated `IAssemblyMarker` only; ready for challenge/settle types in 008
- No CPM pin / Use402Packages / app ProjectReference yet (matches 104-001 deferral)

### Build / tests

- `./bin/dev build`: **0 warnings, 0 errors**
- `dotnet pack`: `TimeWarp.402.2.0.0-beta.14.nupkg`

### Review

- Effort 1, round 1 general; disposition **clean** (`review/disposition.md`)

### Next

104-008 challenge / verify / settle / disabled-is-503 policy

### How to validate

**Automated / build**
```bash
./bin/dev build
# expect: 0 Warning(s), 0 Error(s); timewarp-402 in the build list
dotnet pack source/libraries/timewarp-402/timewarp-402.csproj -c Release -o /tmp/tw402-pack
ls /tmp/tw402-pack/TimeWarp.402.*.nupkg
# expect: nupkg PackageId TimeWarp.402
```

**Expect**
- Project at `source/libraries/timewarp-402/timewarp-402.csproj` with PackageId `TimeWarp.402`
- C# namespace / marker: `TimeWarp.X402` (not invalid `TimeWarp.402`)
- Solution entry under `/source/libraries/` in `timewarp-architecture.slnx`
- Template exclude includes `source/libraries/timewarp-402/**`

**Not in scope:** challenge/settle types (008+); host references.

