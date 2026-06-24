# Migrate Common.Contracts and Common.Domain to source/common/

## Parent
050-establish-root-directory-structure-source-tests-in-kebab-case

## Summary

Migrate two Common layer projects from `TimeWarp.Architecture/Source/Common/` to `source/common/` with kebab-case file/directory naming. These are leaf projects with no internal dependencies, making them safe to migrate early.

## Rationale

- Common.Contracts is the most depended-upon project in the solution (referenced by Common.Application, Api.Contracts, Web.Contracts, Grpc.Contracts)
- Common.Domain is the second most depended-upon (referenced by Common.Application, Web.Domain, Grpc.Domain)
- Both are leaf projects (no ProjectReferences to other solution projects)
- Migrating them first unblocks Level 1 and Level 2 projects in the dependency graph
- Directory names `foundation-contracts`/`foundation-domain` align with planned `TimeWarp.Foundation.*` NuGet naming (task 051)

## Current State

```
TimeWarp.Architecture/Source/Common/
├── Common.Contracts/          ← 26 source files + 8 mixin files + Generated/
│   ├── Base/                   ← 13 files (interfaces, DTOs, enums)
│   ├── Behaviors/              ← 1 file (FluentValidationBehavior)
│   ├── Configuration/           ← 1 file (ServiceNames)
│   ├── Mixins/                 ← 8 files (4 .mixin + 4 .md)
│   ├── Services/               ← 1 file (IApiService)
│   ├── Types/                  ← 3 files
│   └── Validators/             ← 1 file
└── Common.Domain/              ← 6 source files + Generated/
    ├── Entities/Base/          ← 3 files
    └── Enumeration/            ← 1 file
```

## Target State

```
source/common/
├── Directory.Build.props       ← chains to source/, adds CA suppressions
├── foundation-contracts/
│   ├── foundation-contracts.csproj
│   ├── assembly-marker.cs
│   ├── global-usings.cs
│   ├── base/                   ← kebab-case filenames
│   ├── behaviors/
│   ├── configuration/
│   ├── mixins/                 ← directory kebab-case, filenames stay PascalCase
│   ├── services/
│   ├── types/
│   └── validators/
└── foundation-domain/
    ├── foundation-domain.csproj
    ├── assembly-marker.cs
    ├── global-usings.cs
    ├── entities/base/
    └── enumeration/
```

## Skills

- csharp

## Checklist

### Phase 1: Create Directory Structure
- [x] Create source/common/foundation-contracts/ with subdirectories
- [x] Create source/common/foundation-domain/ with subdirectories

### Phase 2: Migrate Common.Contracts files
- [x] git mv all source files to kebab-case names
- [x] Keep Mixin filenames PascalCase (Roslyn AdditionalFiles convention)
- [x] Rename Mixins/ directory to mixins/
- [x] Skip Generated/ directory (gitignored, auto-regenerated)

### Phase 3: Migrate Common.Domain files
- [x] git mv all source files to kebab-case names
- [x] Skip Generated/ directory

### Phase 4: Update Project Files
- [x] Write foundation-contracts.csproj (minimal, inherits from source/Directory.Build.props)
- [x] Write foundation-domain.csproj (minimal)
- [x] Update Mixins\ paths to mixins\ in csproj
- [x] Add CA1040 pragma to both assembly-marker.cs files
- [x] Create source/common/Directory.Build.props (chains to source/, CA suppressions)

### Phase 5: Update Referencing Projects
- [x] Common.Application.csproj — update 2 ProjectReferences
- [x] Api.Contracts.csproj — update ProjectReference + 3 AdditionalFiles
- [x] Web.Contracts.csproj — update ProjectReference + 3 AdditionalFiles
- [x] Grpc.Contracts.csproj — update ProjectReference
- [x] Web.Domain.csproj — update ProjectReference
- [x] Grpc.Domain.csproj — update ProjectReference
- [x] Add PackageVersion entries to root Directory.Packages.props for CPM

### Phase 6: Update Solution Files
- [x] Update TimeWarp.Architecture.slnx (redirect Common.Contracts and Common.Domain paths)
- [x] Add both projects to timewarp-architecture.slnx under source/common/

### Phase 7: Cleanup and Verify
- [x] Remove old Common.Contracts/ and Common.Domain/ directories
- [x] Build verify foundation-contracts and foundation-domain individually
- [x] Build verify timewarp-architecture.slnx

## Notes

### Naming Decisions
- Directory name `foundation-contracts/foundation-domain` chosen to align with future `TimeWarp.Foundation.*` namespace (task 051)
- Namespace stays unchanged as `TimeWarp.Architecture.Common.Contracts` / `TimeWarp.Architecture.Common.Domain`
- Mixin file names (.mixin, .md) stay PascalCase — Roslyn source generator convention references them by filename
- Mixin directory renamed to `mixins/` (kebab-case) since csproj AdditionalFiles paths were updated

### CA Suppressions
- `source/common/Directory.Build.props` suppresses 16 CA warnings that pre-existed in the Common code
- These warnings arise because `source/Directory.Build.props` inherits a stricter AnalysisLevel from root
- CAs suppressed: CA1002, CA1036, CA1040, CA1062, CA1304, CA1311, CA1715, CA1720, CA1725, CA1819, CA1852, CA2007, CA2016, CA2201, CA2227

### Dependency Impact
- Common.Application depends on both Common.Contracts and Common.Domain (not yet migrated)
- Api.Contracts, Web.Contracts, Grpc.Contracts depend on Common.Contracts
- Web.Domain, Grpc.Domain depend on Common.Domain
- All 6 referencing projects had their ProjectReference paths updated

## Results

Successfully migrated Common.Contracts and Common.Domain to source/common/.

### What was implemented
- Common.Contracts → source/common/foundation-contracts/ (26 source files + 8 mixin files)
- Common.Domain → source/common/foundation-domain/ (6 source files)
- All files renamed to kebab-case (except Mixin files which stay PascalCase for Roslyn)
- Directory structure: base/, behaviors/, configuration/, mixins/, services/, types/, validators/ (Contracts) and entities/base/, enumeration/ (Domain)
- Added CA1040 pragma to IAssemblyMarker interfaces
- Added source/common/Directory.Build.props with CA suppressions for pre-existing code patterns
- Updated 6 referencing project .csproj paths (Common.Application, Api.Contracts, Web.Contracts, Web.Domain, Grpc.Contracts, Grpc.Domain)
- Updated AdditionalFiles mixin paths from Mixins/ to mixins/ in Contracts csproj and consumers
- Updated both .slnx files
- Namespaces remain unchanged (TimeWarp.Architecture.Common.*) — rename to TimeWarp.Foundation.* is task 051
- Added missing PackageVersion entries to root Directory.Packages.props for CPM compliance

### Files changed
- source/common/foundation-contracts/ (new, 26+ source files)
- source/common/foundation-domain/ (new, 6 source files)
- source/common/Directory.Build.props (new, CA suppressions)
- source/common/foundation-contracts/foundation-contracts.csproj (renamed, updated Mixins paths)
- source/common/foundation-domain/foundation-domain.csproj (renamed, minimal)
- Various referencing .csproj files (path updates)
- Both .slnx files updated
- Directory.Packages.props (added CPM entries)

### Key decisions
- Directory name: foundation-contracts/foundation-domain (aligned with future TimeWarp.Foundation.* namespace)
- Mixin file names kept PascalCase (Roslyn convention), directory renamed to mixins/ (kebab-case)
- Generated/ directories skipped (gitignored, auto-regenerated)
- CA warnings suppressed in Directory.Build.props rather than individual files (pre-existing code patterns)

### Build verification
- timewarp-architecture.slnx: Build succeeded, 0 warnings, 0 errors
- Individual projects: foundation-contracts and foundation-domain build clean

## Session
- Created: ses_2d78597cfffeIe36aerm1ibchw (2026-04-20)
- Completed: ses_2551326acffeL07HLbsGZR0YO1 (2026-04-20)