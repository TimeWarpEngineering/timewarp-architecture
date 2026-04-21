# Migrate Common.Infrastructure to source/common/foundation-infrastructure/

## Parent
050-establish-root-directory-structure-source-tests-in-kebab-case

## Summary

Migrate Common.Infrastructure from `TimeWarp.Architecture/Source/Common/` to `source/common/foundation-infrastructure/` with kebab-case file and directory naming. This is a Level 2 dependency project that requires foundation-application (already migrated).

## Rationale

- **Level 2 dependency** — blocks 5 infrastructure projects: Common.Infrastructure.Tests, Api.Infrastructure, Grpc.Infrastructure, Web.Infrastructure, Common.Server
- **Small scope** — 6 source files
- **Key dependency migrated** — foundation-application is in source/
- **Infrastructure implementations** — CurrentUserService, DateTimeService (from abstractions defined in foundation-application)

## Current State

```
TimeWarp.Architecture/Source/Common/Common.Infrastructure/
├── Common.Infrastructure.csproj          ← References foundation-application
├── AssemblyMarker.cs                     ← namespace: TimeWarp.Architecture.Common.Infrastructure
├── GlobalUsings.cs                       ← 8 global usings
├── Common_Infrastructure_Module.cs       ← Module configuration
└── Services/
    ├── CurrentUserService.cs             ← namespace: TimeWarp.Architecture.Services
    └── DateTimeService.cs                ← namespace: TimeWarp.Architecture.Services
```

## Dependencies (already migrated)

| Project | Migrated To |
|---------|-------------|
| Common.Application | source/common/foundation-application/ |

## Target State

```
source/common/foundation-infrastructure/
├── foundation-infrastructure.csproj
├── assembly-marker.cs
├── global-usings.cs
├── common-infrastructure-module.cs
└── services/
    ├── current-user-service.cs
    └── date-time-service.cs
```

## Skills

- csharp

## Checklist

### Phase 1: Create Directory Structure
- [x] Create source/common/foundation-infrastructure/services/
- [x] Verify directory is empty and ready

### Phase 2: Migrate Files
- [x] git mv Common.Infrastructure.csproj → foundation-infrastructure.csproj
- [x] git mv AssemblyMarker.cs → assembly-marker.cs
- [x] git mv GlobalUsings.cs → global-usings.cs
- [x] git mv Common_Infrastructure_Module.cs → common-infrastructure-module.cs
- [x] git mv Services/CurrentUserService.cs → services/current-user-service.cs
- [x] git mv Services/DateTimeService.cs → services/date-time-service.cs

### Phase 3: Update Project File
- [x] Update csproj to minimal format (remove redundant properties inherited from source/Directory.Build.props)
- [x] Update ProjectReference path from old location to new location
  - From new location: foundation-application reference stays `..\foundation-application\foundation-application.csproj` (sibling)
- [x] Keep PackageReference for Timewarp.OptionsValidation

### Phase 4: Update Source Files
- [x] Add `#pragma warning disable CA1040` / `#pragma warning restore CA1040` to assembly-marker.cs
- [x] Verify namespaces unchanged
  - AssemblyMarker: TimeWarp.Architecture.Common.Infrastructure
  - Module: TimeWarp.Architecture.Common.Infrastructure
  - Services: TimeWarp.Architecture.Services

### Phase 5: Update Referencing Projects
Update these 5 projects' ProjectReference paths to Common.Infrastructure:
- [x] Common.Infrastructure.Tests.csproj — currently references `..\..\Source\Common\Common.Infrastructure\Common.Infrastructure.csproj`
- [x] Api.Infrastructure.csproj — currently references `..\..\..\..\Common\Common.Infrastructure\Common.Infrastructure.csproj`
- [x] Grpc.Infrastructure.csproj — currently references `..\..\..\..\Common\Common.Infrastructure\Common.Infrastructure.csproj`
- [x] Web.Infrastructure.csproj — currently references `..\..\..\..\Common\Common.Infrastructure\Common.Infrastructure.csproj`
- [x] Common.Server.csproj — currently references `..\Common.Infrastructure\Common.Infrastructure.csproj`

### Phase 6: Update Solution Files
- [x] Update TimeWarp.Architecture/TimeWarp.Architecture.slnx — redirect Common.Infrastructure project path
- [x] Update timewarp-architecture.slnx — add foundation-infrastructure project under /source/common/

### Phase 7: Cleanup and Verify
- [x] Remove old Common.Infrastructure/ directory
- [x] Build verify foundation-infrastructure individually: `dotnet build source/common/foundation-infrastructure/foundation-infrastructure.csproj`
- [x] Build verify timewarp-architecture.slnx (now 12 projects)

## Notes

### ProjectReference Path from New Location
From source/common/foundation-infrastructure/:
- foundation-application: `..\foundation-application\foundation-application.csproj` (sibling)

### Referencing Project Path Changes
For projects in TimeWarp.Architecture/Tests/Common/Common.Infrastructure.Tests/:
- Before: `..\..\Source\Common\Common.Infrastructure\Common.Infrastructure.csproj` (2 up to Tests/, down to Source/)
- After: `..\..\..\..\source\common\foundation-infrastructure\foundation-infrastructure.csproj` (4 up to root, into source/)

For projects in TimeWarp.Architecture/Source/ContainerApps/Api/Api.Infrastructure/:
- Before: `..\..\..\..\Common\Common.Infrastructure\Common.Infrastructure.csproj` (4 up to Source/, down to Common/)
- After: `..\..\..\..\..\..\..\source\common\foundation-infrastructure\foundation-infrastructure.csproj` (7 up to root, into source/)

### Namespace Decisions
- Namespace stays `TimeWarp.Architecture.Common.Infrastructure` (AssemblyMarker, Module)
- Namespace stays `TimeWarp.Architecture.Services` (service implementations)
- Future rename to `TimeWarp.Foundation.*` is task 051

### Services Pattern
Common.Infrastructure provides implementations for abstractions defined in Common.Application:
- `ICurrentUserService` → `CurrentUserService` (reads from HttpContext claims)
- `IDateTimeService` → `DateTimeService` (UtcNow + unique timestamp generation)

## Results

Successfully migrated Common.Infrastructure to source/common/foundation-infrastructure/.

### What was implemented
- 6 source files migrated with kebab-case naming
- csproj ProjectReference updated to sibling path (../foundation-application/)
- Added FrameworkReference for Microsoft.AspNetCore.App (project uses IHttpContextAccessor but never declared it)
- Added Timewarp.OptionsValidation version to root Directory.Packages.props (CPM requires it outside TimeWarp.Architecture/)
- CA1040 pragma added to assembly-marker.cs
- Updated 5 referencing project paths (Common.Server, Api.Infrastructure, Grpc.Infrastructure, Web.Infrastructure, Common.Infrastructure.Tests)
- Both .slnx files updated
- Namespace unchanged (TimeWarp.Architecture.Common.Infrastructure, TimeWarp.Architecture.Services)

### Files changed
- source/common/foundation-infrastructure/ (new, 6 source files)
- source/common/foundation-infrastructure/foundation-infrastructure.csproj (added FrameworkReference, updated ProjectReference)
- TimeWarp.Architecture/Source/Common/Common.Server/Common.Server.csproj (path update)
- TimeWarp.Architecture/Source/ContainerApps/Api/Api.Infrastructure/Api.Infrastructure.csproj (path update)
- TimeWarp.Architecture/Source/ContainerApps/Grpc/Grpc.Infrastructure/Grpc.Infrastructure.csproj (path update)
- TimeWarp.Architecture/Source/ContainerApps/Web/Web.Infrastructure/Web.Infrastructure.csproj (path update)
- TimeWarp.Architecture/Tests/Common/Common.Infrastructure.Tests/Common.Infrastructure.Tests.csproj (path update)
- TimeWarp.Architecture/TimeWarp.Architecture.slnx (redirected path)
- timewarp-architecture.slnx (added project)
- Directory.Packages.props (added Timewarp.OptionsValidation version)

### Key decisions
- Added Microsoft.AspNetCore.App FrameworkReference — CurrentUserService requires IHttpContextAccessor
- Added CPM entry for Timewarp.OptionsValidation — required now that project is outside TimeWarp.Architecture/ CPM scope

### Build verification
- timewarp-architecture.slnx: Build succeeded, 0 warnings, 0 errors (12 projects)

## Session
- Created: ses_2d78597cfffeIe36aerm1ibchw (2026-04-21)
- Completed: ses_25185f64dffe8dyvCD7klkpyTX (2026-04-21)