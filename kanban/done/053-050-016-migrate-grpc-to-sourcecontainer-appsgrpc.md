# 050-016: Migrate Grpc.* to source/container-apps/grpc/

## Parent
050-establish-root-directory-structure-source-tests-in-kebab-case

## Summary

Migrate the 4 Grpc container-app projects from `TimeWarp.Architecture/Source/ContainerApps/Grpc/` to `source/container-apps/grpc/` with kebab-case file and directory naming. Grpc-domain is already migrated.

## Current State

```
source/container-apps/grpc/
├── grpc-contracts/      ✅ Migrated
├── grpc-application/    ✅ Migrated
├── grpc-infrastructure/ ✅ Migrated
├── grpc-server/         ✅ Migrated
└── grpc-domain/          (previously migrated)
```

## Checklist

### Phase 1: Migrate Grpc.Contracts
- [x] git mv Grpc.Contracts files to grpc-contracts/ with kebab-case
- [x] Update csproj (minimal format, ProjectReferences, RootNamespace)
- [x] Update assembly-marker.cs (CA1040 pragma)
- [x] Update referencing projects (Grpc.Application, Grpc.Server, test projects)
- [x] Update both solution files
- [x] Build verify

### Phase 2: Migrate Grpc.Application
- [x] git mv Grpc.Application files to grpc-application/ with kebab-case
- [x] Update csproj (ProjectReferences to grpc-contracts, grpc-domain)
- [x] Update referencing projects (Grpc.Infrastructure, test projects)
- [x] Update both solution files
- [x] Build verify

### Phase 3: Migrate Grpc.Infrastructure
- [x] git mv Grpc.Infrastructure files to grpc-infrastructure/ with kebab-case
- [x] Update csproj (ProjectReferences to grpc-application)
- [x] Update referencing projects (Grpc.Server, test projects)
- [x] Update both solution files
- [x] Build verify

### Phase 4: Migrate Grpc.Server
- [x] git mv Grpc.Server files to grpc-server/ with kebab-case
- [x] Update csproj (ProjectReferences to grpc-infrastructure, foundation-server, aspire-service-defaults)
- [x] Update both solution files
- [x] Build verify

### Phase 5: Cleanup
- [x] Remove old Grpc/ directory (removed empty dirs and obj/bin)
- [x] Full solution build verify
- [x] Update kanban task

## Notes

- Namespaces remain unchanged (TimeWarp.Architecture.Grpc.*)
- Directory.Build.props for source/container-apps/ provides RootNamespace=TimeWarp.Architecture
- Grpc.Server dependency shape preserved: does NOT directly reference Grpc.Contracts (only via Grpc.Infrastructure → Grpc.Application → Grpc.Contracts)
- Protobuf Include path updated from `Protos\greet.proto` to `protos\greet.proto` (lowercase directory)
- Properties/launchSettings.json moved to properties/launchSettings.json (lowercase per convention)
- Pre-existing code quality warnings (CA1050, CA1051, CA1848, CA1849) scoped to grpc-server.csproj only, not globally suppressed
- System.ServiceModel.Primitives upgraded from 8.1.2 to 10.0.652802 (net10.0 compatible)
- System.Security.Cryptography.Xml 10.0.6 added as direct PackageReference in grpc-contracts.csproj to patch CVE-2026-33116 (GHSA-37gx/GHSA-w3x6), overriding the vulnerable transitive version. No NU1903 suppression needed.
- Both PackageVersion entries added to root Directory.Packages.props for CPM
- Web.Spa Grpc.Contracts reference updated to new path
- Aspire.AppHost Grpc.Server reference updated to new path
- BuildImages.ps1 Grpc.Server Dockerfile path updated to `source\container-apps\grpc\grpc-server\`
- Dockerfile updated from stale pre-migration paths to current `source/` layout with .NET 10.0 target
- TimeWarp.Architecture.slnx Grpc projects reordered to dependency order: grpc-contracts, grpc-domain, grpc-application, grpc-infrastructure, grpc-server
- All four new csproj files use no BOM with trailing newline (matching migrated project conventions)

### Pre-existing Issues (not introduced by this migration)

- TimeWarp.Architecture.slnx has pre-existing NU1903 failures from System.Security.Cryptography.Xml in Web.Spa, Testing.Common, and integration test projects (these use old-style Directory.Build.props without CPM and without the patched version)
- TimeWarp.Architecture.slnx has pre-existing path resolution issues: web-contracts and analyzers paths resolve incorrectly (verified — still present)
- BuildImages.ps1: Other Docker paths (`Source\ContainerApps\Web\Web.Server`, `Source\ContainerApps\Api\Api.Server`, `Source\ContainerApps\Yarp`) still use old PascalCase paths; only grpc-server was updated

## Results

### What was implemented
- Migrated all four Grpc container-app projects from `TimeWarp.Architecture/Source/ContainerApps/Grpc/` to `source/container-apps/grpc/`:
  - `Grpc.Contracts` -> `grpc-contracts`
  - `Grpc.Application` -> `grpc-application`
  - `Grpc.Infrastructure` -> `grpc-infrastructure`
  - `Grpc.Server` -> `grpc-server`
- Converted migrated directory and C# file paths to kebab-case.
- Kept namespaces unchanged.
- Replaced old project files with minimal migrated project files using the root source layout.
- Added CA1040 suppressions around Grpc marker interfaces.
- Updated project references in migrated Grpc projects and consumers including Web.Spa and Aspire.AppHost.
- Updated `BuildImages.ps1` to point at the moved Grpc Dockerfile.
- Updated both solution files with root source paths for the migrated Grpc projects.
- Added missing root CPM package versions for Grpc/protobuf-net/System.ServiceModel dependencies.
- Patched the System.Security.Cryptography.Xml vulnerability without suppressing NU1903 by updating System.ServiceModel.Primitives and adding System.Security.Cryptography.Xml 10.0.6 as a direct dependency where needed.
- Updated Grpc.Server Dockerfile to the root `source/` layout and .NET 10.0.
- Scoped Grpc.Server-specific analyzer suppressions to `grpc-server.csproj` rather than global container-app props.

### Files changed
- `source/container-apps/grpc/grpc-contracts/**`
- `source/container-apps/grpc/grpc-application/**`
- `source/container-apps/grpc/grpc-infrastructure/**`
- `source/container-apps/grpc/grpc-server/**`
- `Directory.Packages.props`
- `source/container-apps/Directory.Build.props`
- `TimeWarp.Architecture/Source/ContainerApps/Web/Web.Spa/Web.Spa.csproj`
- `TimeWarp.Architecture/Source/ContainerApps/Aspire/Aspire.AppHost/Aspire.AppHost.csproj`
- `TimeWarp.Architecture/DevOps/Docker/BuildImages.ps1`
- `timewarp-architecture.slnx`
- `TimeWarp.Architecture/TimeWarp.Architecture.slnx`
- `kanban/in-progress/053-050-016-migrate-grpc-to-sourcecontainer-appsgrpc.md`

### Key decisions
- Preserved the original Grpc.Server dependency shape: Grpc.Server does not directly reference Grpc.Contracts; it reaches contracts through Grpc.Infrastructure -> Grpc.Application -> Grpc.Contracts.
- Updated `protos/greet.proto` casing in the project file to match the new kebab-case/lowercase directory path.
- Rewrote the Grpc Dockerfile rather than leaving it stale because it could be corrected cleanly to the root `source/` layout.
- Avoided NU1903 suppression; fixed the vulnerable transitive dependency instead.
- Kept appsettings filenames with existing casing while converting source directories/files to kebab-case.

### Test/build outcomes
- `dotnet build source/container-apps/grpc/grpc-contracts/grpc-contracts.csproj` — passed.
- `dotnet build source/container-apps/grpc/grpc-application/grpc-application.csproj` — passed.
- `dotnet build source/container-apps/grpc/grpc-infrastructure/grpc-infrastructure.csproj` — passed.
- `dotnet build source/container-apps/grpc/grpc-server/grpc-server.csproj` — passed.
- `dotnet build timewarp-architecture.slnx` — passed.
- Review passed after fixes.

### Pre-existing/out-of-scope notes
- `TimeWarp.Architecture.slnx` still has unrelated legacy path/audit issues outside the migrated Grpc scope.
- Other Docker image paths in `BuildImages.ps1` still point to old Web/Api/Yarp locations and will be addressed by later migration tasks.