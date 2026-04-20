# Migrate Api.Domain, Grpc.Domain, Web.Domain, and Aspire.ServiceDefaults to source/container-apps/

## Parent
050-establish-root-directory-structure-source-tests-in-kebab-case

## Summary

Migrate 4 ContainerApps leaf projects from `TimeWarp.Architecture/Source/ContainerApps/` to `source/container-apps/` with kebab-case file and directory naming. All 4 are leaf or near-leaf projects in the dependency graph with minimal files.

## Rationale

- All 4 projects are Phase 1 or early Phase 2 dependencies
- Api.Domain, Grpc.Domain, and Web.Domain are pure domain models with no dependencies (or only foundation-domain)
- Aspire.ServiceDefaults is a shared Aspire project referenced by all server projects
- Small scope: 3-7 source files each
- Migrating them unblocks their dependent Application and Server projects

## Current State

```
TimeWarp.Architecture/Source/ContainerApps/
├── Api/Api.Domain/                    ← 3 files, depends on nothing
├── Grpc/Grpc.Domain/                 ← 3 files, depends on foundation-domain
├── Web/Web.Domain/                   ← 7 files, depends on foundation-domain
└── Aspire/Aspire.ServiceDefaults/    ← 3 files, depends on nothing
```

## Target State

```
source/container-apps/
├── api/api-domain/          ← 3 files + csproj
├── grpc/grpc-domain/        ← 3 files + csproj
├── web/web-domain/          ← 7 files + csproj (in subdirectories)
└── aspire/aspire-service-defaults/ ← 3 files + csproj
```

## Skills

- csharp

## Checklist

### Phase 1: Api.Domain
- [x] Create source/container-apps/api/api-domain/
- [x] git mv 3 files (csproj, AssemblyMarker, GlobalUsings) with kebab-case names
- [x] Add CA1040 pragma to assembly-marker.cs
- [x] Update csproj: remove `<Nullable>enable</>` (inherited), update Folder path to kebab-case
- [x] Update Api.Application.csproj reference path
- [x] Update both .slnx files

### Phase 2: Grpc.Domain
- [x] Create source/container-apps/grpc/grpc-domain/
- [x] git mv 3 files with kebab-case names
- [x] Add CA1040 pragma to assembly-marker.cs
- [x] Update csproj: remove `<Nullable>enable</>`, update ProjectReference path to foundation-domain
- [x] Update Grpc.Application.csproj reference path
- [x] Update both .slnx files

### Phase 3: Web.Domain
- [x] Create source/container-apps/web/web-domain/ with subdirectories
- [x] git mv 7 files with kebab-case names (Abstractions/IInvariants.cs, Aggregates/Catalog/*.cs, Aggregates/Profile/*.cs)
- [x] Add CA1040 pragma to assembly-marker.cs
- [x] Add CA1852 pragma to Profile.cs (UnnecessarySetComparer warning)
- [x] Update csproj: remove `<Nullable>enable</>`, update ProjectReference and Folder paths
- [x] Update Web.Application.csproj reference path
- [x] Update both .slnx files

### Phase 4: Aspire.ServiceDefaults
- [x] Create source/container-apps/aspire/aspire-service-defaults/
- [x] git mv 3 files with kebab-case names (Extensions.cs, GlobalUsings.cs, csproj)
- [x] Update csproj: remove `<Nullable>enable</>` and `<ImplicitUsings>enable</>` (inherited), add CA1062 NoWarn
- [x] Keep IsAspireSharedProject property (required by Aspire SDK)
- [x] Update 4 referencing csproj paths (Api.Server, Grpc.Server, Web.Server, Yarp)
- [x] Add Aspire OpenTelemetry package versions to root Directory.Packages.props
- [x] Update both .slnx files

### Phase 5: Verify
- [x] Build verify all 4 projects individually
- [x] Build verify timewarp-architecture.slnx (10 projects, 0 warnings, 0 errors)

## Notes

### No IAssemblyMarker in Aspire.ServiceDefaults
- Aspire.ServiceDefaults has no AssemblyMarker — it's a Microsoft.Extensions.Hosting extension class
- Only the 3 Domain projects get CA1040 pragma

### Aspire-specific considerations
- `IsAspireSharedProject=true` kept in csproj (required for Aspire tooling)
- `FrameworkReference Include="Microsoft.AspNetCore.App"` kept (not a PackageReference)
- CA1062 suppressed for Aspire template code (ValidateParameters not used in extension methods)

### Web.Domain CA1852
- Web.Domain/Profile.cs had a pre-existing CA1852 warning (UnnecessarySetComparer)
- Suppressed inline with `#pragma warning disable/restore CA1852`

### Folder includes
- Api.Domain: `<Folder Include="entities\" />` (empty placeholder for future entities)
- Web.Domain: `<Folder Include="services\" />` and `<Folder Include="value-objects\" />` (placeholder)
- All converted to kebab-case

## Results

Successfully migrated 4 ContainerApps leaf projects to source/container-apps/.

### What was implemented
- Api.Domain → source/container-apps/api/api-domain/ (3 files)
- Grpc.Domain → source/container-apps/grpc/grpc-domain/ (3 files)
- Web.Domain → source/container-apps/web/web-domain/ (7 files)
- Aspire.ServiceDefaults → source/container-apps/aspire/aspire-service-defaults/ (3 files)
- All files renamed to kebab-case
- csproj files updated (removed redundant properties, updated Folder and ProjectReference paths)
- CA1040 pragma added to all 3 Domain IAssemblyMarker interfaces
- CA1852 pragma added to Web.Domain/Profile.cs (pre-existing)
- CA1062 NoWarn added to Aspire ServiceDefaults (pre-existing Aspire template code)
- Updated 7 referencing .csproj files with new paths
- Both .slnx files updated
- Aspire package versions added to root Directory.Packages.props for CPM

### Build verification
- timewarp-architecture.slnx: Build succeeded, 0 warnings, 0 errors (10 projects)

## Session
- Created: ses_2d78597cfffeIe36aerm1ibchw (2026-04-20)
- Completed: ses_25508c526ffeILfHr6rA8z2thD (2026-04-20)