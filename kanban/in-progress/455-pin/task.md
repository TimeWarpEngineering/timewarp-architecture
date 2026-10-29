# Pin Ardalis.GuardClauses to exactly 5.0.0

## Description

Ardalis packages are moving to a commercial license after 5.0.0. Version 5.0.0 is the last MIT-licensed release of Ardalis.GuardClauses. `Directory.Packages.props` currently has `Version="5.0.0"`, which NuGet treats as a minimum (>= 5.0.0), so a future update or floating resolution could pull a commercial version.

Approved by Steven (relayed by Amina, 2026-10-29). Evaluation: https://github.com/TimeWarpEngineering/timewarp-intel/blob/master/evaluations/2026-10-29-ardalis-packages-commercial.md (Busara: GREEN).

## Requirements

- In `Directory.Packages.props` (around line 68) change
  `<PackageVersion Include="Ardalis.GuardClauses" Version="5.0.0" />`
  to
  `<PackageVersion Include="Ardalis.GuardClauses" Version="[5.0.0]" />`
- No other package changes.

## Checklist

- [ ] Change the version to the exact-match range `[5.0.0]`
- [ ] `dotnet restore` resolves Ardalis.GuardClauses 5.0.0
- [ ] Solution builds clean

## Acceptance

- Restore resolves Ardalis.GuardClauses 5.0.0 exactly.
- Build is clean (no new errors or warnings).
