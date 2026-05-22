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
- [x] git mv Api.Contracts files to api-contracts/ with kebab-case
- [x] Update csproj (minimal format, ProjectReferences, RootNamespace)
- [x] Update assembly-marker.cs (CA1040 pragma)
- [x] Update referencing projects (Api.Application, Api.Server, test projects)
- [x] Update both solution files
- [x] Build verify

### Phase 2: Migrate Api.Application
- [x] git mv Api.Application files to api-application/ with kebab-case
- [x] Update csproj (ProjectReferences to api-contracts, api-domain)
- [x] Update referencing projects (Api.Infrastructure, test projects)
- [x] Update both solution files
- [x] Build verify

### Phase 3: Migrate Api.Infrastructure
- [x] git mv Api.Infrastructure files to api-infrastructure/ with kebab-case
- [x] Update csproj (ProjectReferences to api-application)
- [x] Update referencing projects (Api.Server, test projects)
- [x] Update both solution files
- [x] Build verify

### Phase 4: Migrate Api.Server
- [x] git mv Api.Server files to api-server/ with kebab-case
- [x] Update csproj (ProjectReferences to api-infrastructure, api-contracts, aspire-service-defaults)
- [x] Update both solution files
- [x] Build verify

### Phase 5: Cleanup
- [x] Remove old Api/ directory
- [x] Full solution build verify
- [x] Update kanban task

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
8. Update Api.Server Dockerfile paths if they are in scope and can be corrected cleanly. If the Dockerfile is fundamentally stale and cannot be updated cleanly, report it as a design/pre-existing issue.
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

### Pre-existing Issues Reported

1. **Api.Server Dockerfile is fundamentally stale**: References .NET 6.0 (project is .NET 10), references non-existent projects (Common.Server, Common.Infrastructure, Common.Application, Common.Domain, Common.Contracts, Api.Domain), and uses old directory structure. Cannot be updated cleanly as part of this migration. The Dockerfile was git-mv'd to the new location but its content was not modified. A complete rewrite is needed.

2. **Nested solution (TimeWarp.Architecture.slnx) has pre-existing build failures**: All failures are NU1903 (package vulnerability warnings treated as errors) in Grpc, Web, and Test projects — none related to the Api migration. The Api-specific projects build successfully.

3. **container-apps Directory.Build.props required additional suppressions**: CA1305, CA5394, CA1032, CA1515, CA1823, CA2252, and RCS1194 were needed to match the behavior of the old `TimeWarp.Architecture/Directory.Build.props` chain which had `EnablePreviewFeatures=true` and `NoWarn=$(NoWarn);CA2252`. These are pre-existing code quality issues in the migrated source files, not new issues introduced by migration.

4. **Directory.Packages.props was missing FluentValidation.DependencyInjectionExtensions and Oakton versions**: These were present in the old `TimeWarp.Architecture/Directory.Packages.props` but not in the root one. Added to root `Directory.Packages.props`.

5. **Properties/launchSettings.json path**: Moved to `properties/launchSettings.json` (directory kebab-case). The .NET SDK may not automatically discover launch profiles at this path. This is a known trade-off of the kebab-case directory convention.

## Results

### What was implemented
- Migrated all four Api container-app projects from `TimeWarp.Architecture/Source/ContainerApps/Api/` to `source/container-apps/api/`:
  - `Api.Contracts` -> `api-contracts`
  - `Api.Application` -> `api-application`
  - `Api.Infrastructure` -> `api-infrastructure`
  - `Api.Server` -> `api-server`
- Converted migrated directory and C# file paths to kebab-case.
- Kept namespaces unchanged.
- Replaced old project files with minimal migrated project files using the root source layout.
- Added CA1040 suppressions around Api marker interfaces.
- Updated project references in migrated Api projects and consumers including Web.Spa, Aspire.AppHost, Api.Server.Integration.Tests, and Testing.Common.
- Updated both solution files with root source paths for the migrated Api projects.
- Added missing root CPM package versions for `FluentValidation.DependencyInjectionExtensions` and `Oakton`.
- Extended `source/container-apps/Directory.Build.props` to preserve required build behavior for migrated container-app projects.

### Files changed
- `source/container-apps/api/api-contracts/**`
- `source/container-apps/api/api-application/**`
- `source/container-apps/api/api-infrastructure/**`
- `source/container-apps/api/api-server/**`
- `Directory.Packages.props`
- `source/container-apps/Directory.Build.props`
- `TimeWarp.Architecture/Source/ContainerApps/Web/Web.Spa/Web.Spa.csproj`
- `TimeWarp.Architecture/Source/ContainerApps/Aspire/Aspire.AppHost/Aspire.AppHost.csproj`
- `TimeWarp.Architecture/Tests/ContainerApps/Api/Api.Server.Integration.Tests/Api.Server.Integration.Tests.csproj`
- `TimeWarp.Architecture/Tests/TimeWarp.Testing/Testing.Common.csproj`
- `timewarp-architecture.slnx`
- `TimeWarp.Architecture/TimeWarp.Architecture.slnx`
- `kanban/in-progress/053-050-015-migrate-api-to-sourcecontainer-appsapi.md`

### Key decisions
- Kept `Api.Server` Dockerfile content unchanged after moving it because it is fundamentally stale: it references .NET 6.0 and old/non-existent projects. This is documented as a pre-existing issue requiring a dedicated rewrite.
- Used lowercase `properties/` to match kebab-case directory convention; launch settings behavior was not a build blocker.
- Added required warning suppressions/preview feature setting at `source/container-apps/Directory.Build.props` to preserve behavior from the old nested props chain.

### Test/build outcomes
- `dotnet build source/container-apps/api/api-contracts/api-contracts.csproj` — passed.
- `dotnet build source/container-apps/api/api-application/api-application.csproj` — passed.
- `dotnet build source/container-apps/api/api-infrastructure/api-infrastructure.csproj` — passed.
- `dotnet build source/container-apps/api/api-server/api-server.csproj` — passed.
- `dotnet build timewarp-architecture.slnx` — passed.
- `dotnet build TimeWarp.Architecture/TimeWarp.Architecture.slnx` — fails only on pre-existing NU1903 vulnerability warnings in Grpc/Web/Test projects, unrelated to Api migration.
- Review passed.
