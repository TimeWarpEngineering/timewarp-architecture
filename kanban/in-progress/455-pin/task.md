# Pin Ardalis.GuardClauses to exactly 5.0.0

## Description

Ardalis packages are moving to a commercial license after 5.0.0. Version 5.0.0 is the last MIT-licensed release of Ardalis.GuardClauses. `Directory.Packages.props` currently has `Version="5.0.0"`, which NuGet treats as a minimum (>= 5.0.0), so a future update or floating resolution could pull a commercial version.

Approved by Steven (relayed by Amina, 2026-10-29). Evaluation: https://github.com/TimeWarpEngineering/timewarp-intel/blob/master/evaluations/2026-10-29-ardalis-packages-commercial.md (Busara: GREEN).

## Goal (implementer restatement)

Ardalis.GuardClauses 5.0.0 is the last MIT release. The central package pin is a bare `5.0.0`, which NuGet reads as 5.0.0 or newer, so a later restore or package update can select a commercial build. I will change only that one `PackageVersion` in the root `Directory.Packages.props` to the exact range `[5.0.0]`, leave every other package pin alone, prove restore still selects 5.0.0, and prove the solution still builds with no new errors or warnings.

Acceptance I am holding this change to:

- The root pin is `Version="[5.0.0]"` and no other package version moves.
- `dotnet restore` resolves Ardalis.GuardClauses to exactly 5.0.0.
- The solution build is clean.

## Requirements

- In `Directory.Packages.props` (around line 68) change
  `<PackageVersion Include="Ardalis.GuardClauses" Version="5.0.0" />`
  to
  `<PackageVersion Include="Ardalis.GuardClauses" Version="[5.0.0]" />`
- No other package changes.

## Checklist

- [x] Change the version to the exact-match range `[5.0.0]`
- [x] `dotnet restore` resolves Ardalis.GuardClauses 5.0.0
- [x] Solution builds clean

## Acceptance

- Restore resolves Ardalis.GuardClauses 5.0.0 exactly.
- Build is clean (no new errors or warnings).

## Session

- Implementation: grok session 01a1880f-2795-7733-a6aa-478f87f5972c (2026-10-29)
- `ganda repo audit` exits 0. One pre-existing advisory remains: `kanban-landed` warns that claims 070-001, 104-024, 104-025, 104-026, 123, 141, 198, and 244 have no usable worktree. That warning is not caused by this pin, and creating those worktrees is other claims' work.

## Results

The root central pin for Ardalis.GuardClauses is now the exact range `[5.0.0]`. No other `PackageVersion` changed. Restore of `timewarp-architecture.slnx` succeeded. `dotnet nuget why` on `TimeWarp.Foundation.Contracts` reports `Ardalis.GuardClauses@5.0.0 (= 5.0.0)`. `dotnet list package` on foundation-contracts and web-spa shows requested `[5.0.0]` and resolved `5.0.0`. Every `project.assets.json` that names the package names `Ardalis.GuardClauses/5.0.0` only. `./bin/dev build --quiet` printed `Build completed successfully!` (Release, warnings are errors).

### How to validate

Smoke:

```bash
dotnet restore timewarp-architecture.slnx
dotnet nuget why source/foundation/foundation-contracts/foundation-contracts.csproj Ardalis.GuardClauses
dotnet list source/foundation/foundation-contracts/foundation-contracts.csproj package
dotnet list source/container-apps/web/projects/web-spa/web-spa.csproj package
./bin/dev build --quiet
```

Expect:

- `dotnet nuget why` shows `Ardalis.GuardClauses@5.0.0 (= 5.0.0)`.
- Both `dotnet list package` rows show requested `[5.0.0]` and resolved `5.0.0`.
- `./bin/dev build --quiet` prints `Build completed successfully!` and exits 0.
