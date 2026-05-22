# 050-015: Migrate Api.* to source/container-apps/api/

## Parent
050-establish-root-directory-structure-source-tests-in-kebab-case

## Summary

Migrate the 4 Api container-app projects from `TimeWarp.Architecture/Source/ContainerApps/Api/` to `source/container-apps/api/` with kebab-case file and directory naming. Api-domain is already migrated.

## Current State

```
TimeWarp.Architecture/Source/ContainerApps/Api/
├── Api.Contracts/       ← Leaf: depends on foundation-contracts (already migrated)
├── Api.Application/     ← Depends on Api.Contracts, api-domain (migrated)
├── Api.Infrastructure/  ← Depends on Api.Application
└── Api.Server/          ← Depends on Api.Infrastructure, Api.Contracts, Aspire service defaults
```

## Target State

```
source/container-apps/api/
├── api-contracts/       ← already has api-domain as neighbor
├── api-application/
├── api-infrastructure/
└── api-server/
```

## Dependencies (already migrated)

| Project | Migrated To |
|---------|-------------|
| api-domain | source/container-apps/api/api-domain/ |
| foundation-contracts | source/foundation/foundation-contracts/ |
| aspire-service-defaults | source/container-apps/aspire/aspire-service-defaults/ |

## Migration Order (leaf-to-root)

1. Api.Contracts — depends on foundation-contracts
2. Api.Application — depends on Api.Contracts, api-domain
3. Api.Infrastructure — depends on Api.Application
4. Api.Server — depends on all above

## Checklist

### Phase 1: Migrate Api.Contracts
- [ ] git mv Api.Contracts files to api-contracts/ with kebab-case
- [ ] Update csproj (minimal format, ProjectReferences, RootNamespace)
- [ ] Update assembly-marker.cs (CA1040 pragma)
- [ ] Update referencing projects (Api.Application, Api.Server, test projects)
- [ ] Update both solution files
- [ ] Build verify

### Phase 2: Migrate Api.Application
- [ ] git mv Api.Application files to api-application/ with kebab-case
- [ ] Update csproj (ProjectReferences to api-contracts, api-domain)
- [ ] Update referencing projects (Api.Infrastructure, test projects)
- [ ] Update both solution files
- [ ] Build verify

### Phase 3: Migrate Api.Infrastructure
- [ ] git mv Api.Infrastructure files to api-infrastructure/ with kebab-case
- [ ] Update csproj (ProjectReferences to api-application)
- [ ] Update referencing projects (Api.Server, test projects)
- [ ] Update both solution files
- [ ] Build verify

### Phase 4: Migrate Api.Server
- [ ] git mv Api.Server files to api-server/ with kebab-case
- [ ] Update csproj (ProjectReferences to api-infrastructure, api-contracts, aspire-service-defaults)
- [ ] Update both solution files
- [ ] Build verify

### Phase 5: Cleanup
- [ ] Remove old Api/ directory
- [ ] Full solution build verify
- [ ] Update kanban task

## Notes

- Namespaces remain unchanged (TimeWarp.Architecture.Api.*)
- Directory.Build.props for source/container-apps/ already exists with RootNamespace=TimeWarp.Architecture
- Follow same patterns as task 050-014 (Web.Contracts migration)
- Api.Contracts is a leaf project similar to Web.Contracts — good candidate for the same migration approach

### Implementation Plan

1. Migrate the 4 Api projects from `TimeWarp.Architecture/Source/ContainerApps/Api/` to `source/container-apps/api/`:
   - `Api.Contracts` -> `api-contracts`
   - `Api.Application` -> `api-application`
   - `Api.Infrastructure` -> `api-infrastructure`
   - `Api.Server` -> `api-server`
2. Use `git mv` for all committed files, converting directories and `.cs` filenames to kebab-case.
3. Keep namespaces unchanged (`TimeWarp.Architecture.Api.*`).
4. Use the migration patterns from task 050-014:
   - minimal project files
   - `RootNamespace=TimeWarp.Architecture`
   - CA1040 suppression around marker interfaces
   - update solution paths to `../source/...` from nested solution and `source/...` from root solution
5. Migrate leaf-to-root:
   - Api.Contracts
   - Api.Application
   - Api.Infrastructure
   - Api.Server
6. Update project references in:
   - Api.Application
   - Api.Infrastructure
   - Api.Server
   - Web.Spa conditional Api.Contracts reference
   - Aspire.AppHost Api.Server reference
   - Api.Server.Integration.Tests references
   - Testing.Common Api.Server reference
7. Update both solution files:
   - `TimeWarp.Architecture/TimeWarp.Architecture.slnx`
   - `timewarp-architecture.slnx`
8. Update Api.Server Dockerfile paths if they are in scope and can be corrected cleanly. If the Dockerfile is fundamentally stale and cannot be updated cleanly as part of the move, report it as a design/pre-existing issue.
9. Verification commands:
   - `dotnet build source/container-apps/api/api-contracts/api-contracts.csproj`
   - `dotnet build source/container-apps/api/api-application/api-application.csproj`
   - `dotnet build source/container-apps/api/api-infrastructure/api-infrastructure.csproj`
   - `dotnet build source/container-apps/api/api-server/api-server.csproj`
   - `dotnet build timewarp-architecture.slnx`
   - `dotnet build TimeWarp.Architecture/TimeWarp.Architecture.slnx` if practical; report unrelated pre-existing failures separately.

### Design Notes

- Root `Directory.Build.props` already provides `ImplicitUsings=enable` and `Nullable=enable`; do not duplicate unless a project truly requires local overrides.
- Api.Server is a web SDK project and may have Docker/appsettings/launchSettings path implications.
- Aspire.AppHost is not migrated yet; update only the Api.Server reference path to the new Api.Server location.
- Generated/obj/bin folders must not be migrated.
- If a design issue or architectural problem appears during implementation, stop and report rather than applying workarounds.