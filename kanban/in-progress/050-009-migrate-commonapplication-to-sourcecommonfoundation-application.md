# Migrate Common.Application to source/common/foundation-application/

## Parent
050-establish-root-directory-structure-source-tests-in-kebab-case

## Summary

Migrate Common.Application from `TimeWarp.Architecture/Source/Common/` to `source/common/foundation-application/` with kebab-case file and directory naming. This is a Level 1 dependency project that requires foundation-contracts, foundation-domain, and timewarp-modules — all already migrated.

## Rationale

- **Level 1 dependency** — blocks 4 projects: Common.Infrastructure, Api.Application, Grpc.Application, Web.Application
- **Small scope** — only 5 source files (2 abstractions, AssemblyMarker, GlobalUsings, csproj)
- **All dependencies migrated** — foundation-contracts, foundation-domain, timewarp-modules are in source/
- **Enables next migration wave** — Common.Infrastructure, X.Application projects depend on this

## Current State

```
TimeWarp.Architecture/Source/Common/Common.Application/
├── Common.Application.csproj          ← References 3 migrated projects
├── AssemblyMarker.cs                  ← namespace: TimeWarp.Architecture.Common.Application
├── GlobalUsings.cs                    ← Empty placeholder
└── Abstractions/
    ├── ICurrentUserService.cs         ← namespace: TimeWarp.Architecture.Abstractions
    └── IDateTimeService.cs            ← namespace: TimeWarp.Architecture.Abstractions
```

## Dependencies (already migrated)

| Project | Migrated To |
|---------|-------------|
| Common.Contracts | source/common/foundation-contracts/ |
| Common.Domain | source/common/foundation-domain/ |
| TimeWarp.Modules | source/libraries/timewarp-modules/ |

## Target State

```
source/common/foundation-application/
├── foundation-application.csproj
├── assembly-marker.cs
├── global-usings.cs
└── abstractions/
    ├── i-current-user-service.cs
    └── i-date-time-service.cs
```

## Skills

- csharp

## Checklist

### Phase 1: Create Directory Structure
- [ ] Create source/common/foundation-application/abstractions/
- [ ] Verify directory is empty and ready

### Phase 2: Migrate Files
- [ ] git mv Common.Application.csproj → foundation-application.csproj
- [ ] git mv AssemblyMarker.cs → assembly-marker.cs
- [ ] git mv GlobalUsings.cs → global-usings.cs
- [ ] git mv Abstractions/ICurrentUserService.cs → abstractions/i-current-user-service.cs
- [ ] git mv Abstractions/IDateTimeService.cs → abstractions/i-date-time-service.cs

### Phase 3: Update Project File
- [ ] Update csproj to minimal format (remove redundant properties inherited from source/Directory.Build.props)
- [ ] Update ProjectReference paths from `..\..\..\..\source\...` to correct relative paths from new location
  - foundation-contracts: `..\foundation-contracts\foundation-contracts.csproj` (sibling)
  - foundation-domain: `..\foundation-domain\foundation-domain.csproj` (sibling)
  - timewarp-modules: `..\..\libraries\timewarp-modules\timewarp-modules.csproj` (up to source/, down to libraries/)

### Phase 4: Update Source Files
- [ ] Add `#pragma warning disable CA1040` / `#pragma warning restore CA1040` to assembly-marker.cs
- [ ] Verify namespaces unchanged (TimeWarp.Architecture.Common.Application, TimeWarp.Architecture.Abstractions)

### Phase 5: Update Referencing Projects
Update these 4 projects' ProjectReference paths to Common.Application:
- [ ] Common.Infrastructure.csproj — currently references `..\Common.Application\Common.Application.csproj`
- [ ] Api.Application.csproj — currently references `..\..\..\..\Common\Common.Application\Common.Application.csproj`
- [ ] Grpc.Application.csproj — currently references `..\..\..\..\Common\Common.Application\Common.Application.csproj`
- [ ] Web.Application.csproj — currently references `..\..\..\..\Common\Common.Application\Common.Application.csproj`

### Phase 6: Update Solution Files
- [ ] Update TimeWarp.Architecture/TimeWarp.Architecture.slnx — redirect Common.Application project path
- [ ] Update timewarp-architecture.slnx — add foundation-application project under /source/common/

### Phase 7: Cleanup and Verify
- [ ] Remove old Common.Application/ directory
- [ ] Build verify foundation-application individually: `dotnet build source/common/foundation-application/foundation-application.csproj`
- [ ] Build verify timewarp-architecture.slnx (now 11 projects)

## Notes

### ProjectReference Path Changes
From the old location (TimeWarp.Architecture/Source/Common/Common.Application/):
- `..\..\..\..\source\common\foundation-contracts\` (4 up to root, into source/)

From the new location (source/common/foundation-application/):
- `..\foundation-contracts\foundation-contracts.csproj` (1 up to common/, sibling)
- `..\foundation-domain\foundation-domain.csproj` (1 up to common/, sibling)
- `..\..\libraries\timewarp-modules\timewarp-modules.csproj` (1 up to common/, 1 up to source/, down to libraries/)

### Referencing Project Path Changes
For projects in TimeWarp.Architecture/Source/Common/Common.Infrastructure/:
- Before: `..\Common.Application\Common.Application.csproj` (1 up, sibling)
- After: `..\..\..\..\..\source\common\foundation-application\foundation-application.csproj` (5 up to root, into source/)

For projects in TimeWarp.Architecture/Source/ContainerApps/Api/Api.Application/:
- Before: `..\..\..\..\Common\Common.Application\Common.Application.csproj` (4 up, down to Common/)
- After: `..\..\..\..\..\..\..\source\common\foundation-application\foundation-application.csproj` (7 up to root, into source/)

### Namespace Decisions
- Namespace stays `TimeWarp.Architecture.Common.Application` (AssemblyMarker)
- Namespace stays `TimeWarp.Architecture.Abstractions` (service interfaces)
- Future rename to `TimeWarp.Foundation.*` is task 051

### Why No GlobalUsings Content?
Common.Application/GlobalUsings.cs is currently empty (just a comment). This is intentional — the project is a thin abstraction layer. Content may be added in the future.

## Results

To be filled after completion.

## Session
- Created: ses_2d78597cfffeIe36aerm1ibchw (2026-04-21)