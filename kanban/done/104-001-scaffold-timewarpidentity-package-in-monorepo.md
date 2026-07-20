# Scaffold TimeWarp.Identity package in monorepo

## Parent

104

## Description

Create the TimeWarp.Identity project(s), wire CPM/solution, AssemblyMarker, empty public surface. Build green under `dev build`. Home: foundation-style or source package path consistent with existing monorepo layout — pick the simplest place that can grow into a publishable package later.

## Requirements

- Project builds with warnings-as-errors
- Referenced only where needed (no mass-wire yet)
- Purpose regions on seed files

## Checklist

- [x] Create csproj + folder
- [x] Solution / Directory.Build wiring
- [x] AssemblyMarker
- [x] `dev build` includes package cleanly

## Notes

Package name locked: **TimeWarp.Identity**. Not Passwordless.dev wrapper.

### Depends on

None — start here.

### Implementation plan (104-001)

#### Location (decided)

| Item | Value |
|------|--------|
| Path | `source/libraries/timewarp-identity/` |
| csproj | `timewarp-identity.csproj` |
| PackageId / Title | `TimeWarp.Identity` |
| Namespace | `TimeWarp.Identity` |
| AssemblyName | default kebab (`timewarp-identity`) like modules |

Why not `source/foundation/`: Foundation packages are `TimeWarp.Foundation.*`. Identity is a product capability package, peer to future TimeWarp.402. Precedent: `source/libraries/timewarp-modules/`.

#### Files to create

1. `source/libraries/timewarp-identity/timewarp-identity.csproj` — packable, PackageId, no ProjectReference/PackageReference
2. `source/libraries/timewarp-identity/assembly-marker.cs` — `IAssemblyMarker` + Purpose region + CA1040 suppress (clone modules)

Omit `global-usings.cs` until needed.

#### Wiring

1. Add project to `timewarp-architecture.slnx` under `/source/libraries/`
2. Append to `PackableProjects` in `tools/dev-cli/endpoints/workflow-command.cs` (recommended)
3. Do **not** add Directory.Packages.props PackageVersion yet
4. Do **not** mass-wire apps or invent UseIdentityPackages
5. No tests project (104-006)

#### Verify

- `dev build` → 0/0
- Optional: `dotnet pack` → `TimeWarp.Identity.*.nupkg`
- No app ProjectReference to Identity yet

#### Out of scope

Principal/Credential (002), WebAuthn (003), agent keys (004), multi-cred (005), tests (006), Passwordless, ADRs/skills, foundation path.

#### Patterns to clone

- `source/libraries/timewarp-modules/timewarp-modules.csproj`
- `source/libraries/timewarp-modules/assembly-marker.cs`

## Session

- Created: 2026-07-16
- Plan: 2026-07-16 (orchestrate-task 104-001)
- Implementation: 2026-07-16
- Review: clean (template exclude fix)

## Results

### Summary
Scaffolded empty packable **TimeWarp.Identity** under `source/libraries/timewarp-identity/`, matching the `timewarp-modules` pattern. Wired into solution and pack pipeline; template excludes identity source when `foundationPackages=true`.

### Files changed
| Action | Path |
|--------|------|
| Created | `source/libraries/timewarp-identity/timewarp-identity.csproj` |
| Created | `source/libraries/timewarp-identity/assembly-marker.cs` |
| Edited | `timewarp-architecture.slnx` |
| Edited | `tools/dev-cli/endpoints/workflow-command.cs` (PackableProjects + Design) |
| Edited | `.template.config/template.json` (exclude under foundationPackages) |

### Key decisions
- Location: `source/libraries/` not `foundation/` (PackageId `TimeWarp.Identity`, not Foundation.*)
- Empty surface only: `IAssemblyMarker` public type
- No app ProjectReference, no CPM PackageVersion, no domain types
- Template exclude aligned with modules for dual-mode packaging

### Build / tests
- `dev build`: **0 warnings, 0 errors**
- Review: first pass found missing template exclude (fixed); re-review clean

### Next
104-002 Principal / Credential / TrustTier domain model
