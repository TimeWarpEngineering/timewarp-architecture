# 050-016: Migrate Grpc.* to source/container-apps/grpc/

## Parent
050-establish-root-directory-structure-source-tests-in-kebab-case

## Summary

Migrate the 4 Grpc container-app projects from `TimeWarp.Architecture/Source/ContainerApps/Grpc/` to `source/container-apps/grpc/` with kebab-case file and directory naming. Grpc-domain is already migrated.

## Current State

```
TimeWarp.Architecture/Source/ContainerApps/Grpc/
├── Grpc.Contracts/       ← Leaf: depends on foundation-contracts (migrated)
├── Grpc.Application/      ← Depends on Grpc.Contracts, grpc-domain (migrated)
├── Grpc.Infrastructure/   ← Depends on Grpc.Application
└── Grpc.Server/           ← Depends on Grpc.Infrastructure, Grpc.Contracts
```

## Target State

```
source/container-apps/grpc/
├── grpc-contracts/       ← already has grpc-domain as neighbor
├── grpc-application/
├── grpc-infrastructure/
└── grpc-server/
```

## Dependencies (already migrated)

| Project | Migrated To |
|---------|-------------|
| grpc-domain | source/container-apps/grpc/grpc-domain/ |
| foundation-contracts | source/foundation/foundation-contracts/ |

## Migration Order (leaf-to-root)

1. Grpc.Contracts — depends on foundation-contracts
2. Grpc.Application — depends on Grpc.Contracts, grpc-domain
3. Grpc.Infrastructure — depends on Grpc.Application
4. Grpc.Server — depends on all above

## Checklist

### Phase 1: Migrate Grpc.Contracts
- [ ] git mv Grpc.Contracts files to grpc-contracts/ with kebab-case
- [ ] Update csproj (minimal format, ProjectReferences, RootNamespace)
- [ ] Update assembly-marker.cs (CA1040 pragma)
- [ ] Update referencing projects (Grpc.Application, Grpc.Server, test projects)
- [ ] Update both solution files
- [ ] Build verify

### Phase 2: Migrate Grpc.Application
- [ ] git mv Grpc.Application files to grpc-application/ with kebab-case
- [ ] Update csproj (ProjectReferences to grpc-contracts, grpc-domain)
- [ ] Update referencing projects (Grpc.Infrastructure, test projects)
- [ ] Update both solution files
- [ ] Build verify

### Phase 3: Migrate Grpc.Infrastructure
- [ ] git mv Grpc.Infrastructure files to grpc-infrastructure/ with kebab-case
- [ ] Update csproj (ProjectReferences to grpc-application)
- [ ] Update referencing projects (Grpc.Server, test projects)
- [ ] Update both solution files
- [ ] Build verify

### Phase 4: Migrate Grpc.Server
- [ ] git mv Grpc.Server files to grpc-server/ with kebab-case
- [ ] Update csproj (ProjectReferences to grpc-infrastructure, grpc-contracts)
- [ ] Update both solution files
- [ ] Build verify

### Phase 5: Cleanup
- [ ] Remove old Grpc/ directory
- [ ] Full solution build verify
- [ ] Update kanban task

## Notes

- Namespaces remain unchanged (TimeWarp.Architecture.Grpc.*)
- Directory.Build.props for source/container-apps/ already exists with RootNamespace=TimeWarp.Architecture
- Follow same patterns as task 050-014 (Web.Contracts migration)
- Grpc.Contracts is a leaf project similar to Web.Contracts

### Implementation Plan

1. Migrate the 4 Grpc projects from `TimeWarp.Architecture/Source/ContainerApps/Grpc/` to `source/container-apps/grpc/`:
   - `Grpc.Contracts` -> `grpc-contracts`
   - `Grpc.Application` -> `grpc-application`
   - `Grpc.Infrastructure` -> `grpc-infrastructure`
   - `Grpc.Server` -> `grpc-server`
2. Use `git mv` for all committed files, converting directories and `.cs` filenames to kebab-case. Do not migrate `obj/`, `bin/`, or generated build artifacts.
3. Keep namespaces unchanged (`TimeWarp.Architecture.Grpc.*`).
4. Use the established migration patterns from tasks 050-014 and 050-015:
   - minimal project files
   - rely on `source/container-apps/Directory.Build.props` for `RootNamespace=TimeWarp.Architecture`
   - rely on root `Directory.Build.props` for `ImplicitUsings=enable` and `Nullable=enable`
   - CA1040 suppression around marker interfaces
   - nested solution paths use `../source/...`
   - root solution paths use `source/...`
5. Migrate leaf-to-root:
   - Grpc.Contracts
   - Grpc.Application
   - Grpc.Infrastructure
   - Grpc.Server
6. Update project references in:
   - Grpc.Application
   - Grpc.Infrastructure
   - Grpc.Server
   - Web.Spa conditional Grpc.Contracts reference
   - Aspire.AppHost Grpc.Server reference
   - Grpc test project references if present
   - DevOps/BuildImages.ps1 if it references Grpc.Server Dockerfile
7. Update both solution files:
   - `TimeWarp.Architecture/TimeWarp.Architecture.slnx`
   - `timewarp-architecture.slnx`
8. For Grpc.Server:
   - move Dockerfile and appsettings files with existing casing unless project convention clearly requires otherwise
   - move `Properties/launchSettings.json` according to the migration convention used in task 050-015
   - update `Protobuf Include` paths if needed after directory/file renames
   - update Dockerfile paths if they can be corrected cleanly; otherwise document as stale/pre-existing
9. Verification commands:
   - `dotnet build source/container-apps/grpc/grpc-contracts/grpc-contracts.csproj`
   - `dotnet build source/container-apps/grpc/grpc-application/grpc-application.csproj`
   - `dotnet build source/container-apps/grpc/grpc-infrastructure/grpc-infrastructure.csproj`
   - `dotnet build source/container-apps/grpc/grpc-server/grpc-server.csproj`
   - `dotnet build timewarp-architecture.slnx`
   - `dotnet build TimeWarp.Architecture/TimeWarp.Architecture.slnx` if practical; report unrelated pre-existing failures separately.

### Design Notes

- Original `Grpc.Server` did not directly reference `Grpc.Contracts`; preserve that dependency shape unless the build requires a direct reference.
- Root `Directory.Build.props` already provides `ImplicitUsings=enable` and `Nullable=enable`; do not duplicate unless a project truly requires local overrides.
- `source/container-apps/Directory.Build.props` already provides `RootNamespace=TimeWarp.Architecture` and warning suppressions extended during task 050-015.
- Aspire.AppHost is not migrated yet; update only the Grpc.Server reference path to the new Grpc.Server location.
- Generated/obj/bin folders must not be migrated.
- If a design issue or architectural problem appears during implementation, stop and report rather than applying workarounds.