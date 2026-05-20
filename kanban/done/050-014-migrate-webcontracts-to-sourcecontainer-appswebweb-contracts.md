# Migrate Web.Contracts to source/container-apps/web/web-contracts/

## Parent
050-establish-root-directory-structure-source-tests-in-kebab-case

## Summary

Migrate Web.Contracts from `TimeWarp.Architecture/Source/ContainerApps/Web/` to `source/container-apps/web/web-contracts/` with kebab-case file and directory naming. This is a large contracts project (~70 files including generated) that provides the API contracts for the Web application.

## Rationale

- **Large but contained scope** — ~70 files, mostly feature contracts (Commands, Queries, DTOs)
- **Web-specific** — Admin, Analytics, Auth, Authentication, Chat, Profile, TodoItems features
- **Foundation dependency** — References foundation-contracts (already migrated)
- **Blocks 3 projects** — Web.Application, Web.Server.Integration.Tests, Web.Spa
- **Completes Web container-apps layer** — Web.Domain already migrated

## Current State

```
TimeWarp.Architecture/Source/ContainerApps/Web/Web.Contracts/
├── Web.Contracts.csproj                  ← References foundation-contracts + mixins
├── AssemblyMarker.cs
├── GlobalUsings.cs
├── Extensions/AssemblyExtensions.cs
├── Types/SignalRResult.cs
├── Mixins/                               ← Morris.Moxy mixins (CreateCommand, DeleteCommand, etc.)
├── Generated/                            ← Morris.Moxy generated files (gitignored mostly)
└── Features/
    ├── Admin/
    │   ├── Modules/ModuleIds.cs
    │   ├── Roles/
    │   │   ├── Commands/CreateRole.cs, DeleteRole.cs, UpdateRole.cs
    │   │   ├── Queries/GetRole.cs, GetRoles.cs
    │   │   └── RoleDetails.cs
    │   └── Users/UserIds.cs
    ├── Analytics/TrackEvent.cs, TrackEventValidiation.mixin.cs
    ├── Auth/Queries/GetSignInToken.cs
    ├── Authentication/Queries/GetCurrentUser.cs
    ├── Authorization/RoleIds.cs
    ├── Chat/
    │   ├── ChatHubConstants.cs
    │   ├── ClientToServer/SendMessage.cs
    │   └── ServerToClient/ReceiveMessage.cs
    ├── Hello/Hello.cs
    ├── Infrastructure/InternalsVisibleToClientAndServer.cs
    ├── Profile/Queries/GetProfile.cs
    └── TodoItems/
        ├── Commands/CreateTodoItem.cs, DeleteTodoItem.cs, UpdateTodoItem.cs
        ├── Queries/GetTodoItemById.cs, SearchTodoItems.cs
        └── TodoItemDto.cs
```

## Dependencies (already migrated)

| Project | Migrated To |
|---------|-------------|
| foundation-contracts | source/foundation/foundation-contracts/ |

## Target State

```
source/container-apps/web/web-contracts/
├── web-contracts.csproj
├── assembly-marker.cs
├── global-usings.cs
├── extensions/assembly-extensions.cs
├── types/signal-r-result.cs
├── mixins/
├── features/
│   ├── admin/
│   │   ├── modules/module-ids.cs
│   │   ├── roles/
│   │   │   ├── commands/create-role.cs, delete-role.cs, update-role.cs
│   │   │   ├── queries/get-role.cs, get-roles.cs
│   │   │   └── role-details.cs
│   │   └── users/user-ids.cs
│   ├── analytics/track-event.cs, TrackEventValidiation.mixin, track-event-validiation.mixin.cs
│   ├── auth/queries/get-sign-in-token.cs
│   ├── authentication/queries/get-current-user.cs
│   ├── authorization/role-ids.cs
│   ├── chat/
│   │   ├── chat-hub-constants.cs
│   │   ├── client-to-server/send-message.cs
│   │   └── server-to-client/receive-message.cs
│   ├── hello/hello.cs
│   ├── infrastructure/internals-visible-to-client-and-server.cs
│   ├── profile/queries/get-profile.cs
│   └── todo-items/
│       ├── commands/create-todo-item.cs, delete-todo-item.cs, update-todo-item.cs
│       ├── queries/get-todo-item-by-id.cs, search-todo-items.cs
│       └── todo-item-dto.cs
```

## Skills

- csharp

## Checklist

### Phase 1: Create Directory Structure
- [x] Create source/container-apps/web/web-contracts/ with all subdirectories
- [x] Verify empty directories are ready

### Phase 2: Migrate Files (kebab-case)
- [x] git mv Web.Contracts.csproj → web-contracts.csproj
- [x] git mv AssemblyMarker.cs → assembly-marker.cs
- [x] git mv GlobalUsings.cs → global-usings.cs
- [x] git mv Extensions/AssemblyExtensions.cs → extensions/assembly-extensions.cs
- [x] git mv Types/SignalRResult.cs → types/signal-r-result.cs
- [x] git mv Mixins/ → mixins/ (keep PascalCase file names)
- [x] git mv all Feature/ files to kebab-case directories/files
- [x] Skip Generated/ (gitignored, Morris.Moxy auto-regenerates)

### Phase 3: Update Project File
- [x] Update csproj to minimal format (remove redundant properties inherited)
- [x] Update ProjectReference path to foundation-contracts
- [x] Update AdditionalFiles paths to foundation-contracts mixins
- [x] Keep all Morris.Moxy-related AdditionalFiles entries
- [x] Keep PackageReferences (FluentValidation, Morris.Moxy, Passwordless, protobuf-net.Grpc, etc.)
- [x] Add RootNamespace=(TimeWarp.Architecture) for Morris.Moxy generator compatibility
- [x] Remove stale DependencyValidation1.layerdiagram AdditionalFiles entry
- [x] Remove stale commented-out PackageReference/ProjectReference entries

### Phase 4: Update Source Files
- [x] Add CA1040 pragma to assembly-marker.cs
- [x] Verify namespaces unchanged (TimeWarp.Architecture.Features.*)

### Phase 5: Update Referencing Projects
Update these 3 projects' ProjectReference paths to Web.Contracts:
- [x] Web.Application.csproj
- [x] Web.Server.Integration.Tests.csproj
- [x] Web.Spa.csproj

### Phase 6: Update Solution Files
- [x] Update TimeWarp.Architecture/TimeWarp.Architecture.slnx — redirect Web.Contracts project path
- [x] Update timewarp-architecture.slnx — add web-contracts project under /source/container-apps/web/

### Phase 7: Cleanup and Verify
- [x] Remove old Web.Contracts/ directory
- [x] Build verify web-contracts individually
- [x] Build verify timewarp-architecture.slnx

### Phase 8 (additional): Infrastructure
- [x] Create source/container-apps/Directory.Build.props with RootNamespace and CA suppressions
- [x] Add missing PackageVersion entries to root Directory.Packages.props (Microsoft.Extensions.DependencyInjection, Microsoft.Extensions.Options.ConfigurationExtensions, Morris.Moxy, Passwordless, protobuf-net.Grpc)
- [x] Migrate Web.Contracts.csproj.DotSettings → web-contracts.csproj.DotSettings with updated kebab-case paths
- [x] Create features/auth/commands/ empty directory for Folder Include

## Notes

### Morris.Moxy Integration
- Web.Contracts uses Morris.Moxy for mixin code generation
- Mixins directory has .mixin files (CreateCommand, DeleteCommand, GetListQuery, GetQuery, UpdateCommand)
- Generated/ directory contains Moxy-generated files — skipped in migration (gitignored, regenerates)
- AdditionalFiles entries must be preserved for Moxy to work
- RootNamespace must be set to `TimeWarp.Architecture` so Morris.Moxy generates attributes in the correct namespace (matching global usings)

### Feature Organization
Web.Contracts follows the Feature pattern:
- Features/Admin/Roles — CRUD operations for roles
- Features/Analytics — Event tracking
- Features/Auth — Passwordless authentication
- Features/Authentication — Current user queries
- Features/Authorization — Role IDs
- Features/Chat — SignalR hub contracts (ClientToServer, ServerToClient)
- Features/TodoItems — Classic CRUD example feature

### Mixins and AdditionalFiles
- The csproj references mixins from foundation-contracts (already migrated to source/foundation/)
- Paths updated to `..\..\foundation\foundation-contracts\mixins\` relative paths
- Morris.Moxy-generated files reference original file paths — regenerated correctly after clean build

### Namespace Decisions
- Namespaces stay `TimeWarp.Architecture.Features.*` (Admin, Analytics, Auth, etc.)
- Feature-specific namespaces reflect the domain, not the directory structure
- This aligns with the pattern used across all Contracts projects

### Infrastructure Changes
- Created `source/container-apps/Directory.Build.props` with `<RootNamespace>TimeWarp.Architecture</RootNamespace>` and CA warning suppressions matching `source/foundation/Directory.Build.props`
- Added CA1024 and CA2211 to suppressions (needed by existing web-contracts code)
- Added package versions to root `Directory.Packages.props`: Microsoft.Extensions.DependencyInjection, Microsoft.Extensions.Options.ConfigurationExtensions, Morris.Moxy, Passwordless, protobuf-net.Grpc

### Pre-existing Build Issues (not related to this migration)
- `../../source/` paths in TimeWarp.Architecture.slnx for previously migrated projects (web-domain, api-domain, grpc-domain, aspire-service-defaults) are incorrect — should be `../source/`. Web-contracts path is correct.
- Web.Application has missing AspNetCore SDK reference
- Web.Spa and test projects have NuGet vulnerability warnings treated as errors

## Results

### What was implemented
- Migrated Web.Contracts from `TimeWarp.Architecture/Source/ContainerApps/Web/Web.Contracts/` to `source/container-apps/web/web-contracts/`.
- Converted migrated directory and C# file paths to kebab-case consistent with root `source/` migration conventions.
- Preserved Morris.Moxy `.mixin` filename casing where required (`TrackEventValidiation.mixin` and mixins under `mixins/`) for generator compatibility.
- Skipped `Generated/` because Morris.Moxy regenerates generated sources.
- Added `source/container-apps/Directory.Build.props` so migrated container-app projects inherit root source configuration, `RootNamespace=TimeWarp.Architecture`, and required warning suppressions.
- Updated `web-contracts.csproj` to a minimal migrated form with corrected ProjectReference, AdditionalFiles, package references, RootNamespace, and stale entry cleanup.
- Updated project references in Web.Application, Web.Spa, and Web.Server.Integration.Tests.
- Updated root and nested solution files, including fixing previously migrated `../source/...` paths in `TimeWarp.Architecture.slnx`.
- Updated Rider/ReSharper DotSettings namespace skip paths for the migrated structure.

### Files changed
- `source/container-apps/web/web-contracts/**` — migrated Web.Contracts files.
- `source/container-apps/Directory.Build.props` — new shared container-app source configuration.
- `Directory.Packages.props` — added missing package versions required by migrated Web.Contracts under root CPM.
- `TimeWarp.Architecture/Source/ContainerApps/Web/Web.Application/Web.Application.csproj`
- `TimeWarp.Architecture/Source/ContainerApps/Web/Web.Spa/Web.Spa.csproj`
- `TimeWarp.Architecture/Tests/ContainerApps/Web/Web.Server.Integration.Tests/Web.Server.Integration.Tests.csproj`
- `timewarp-architecture.slnx`
- `TimeWarp.Architecture/TimeWarp.Architecture.slnx`
- `kanban/in-progress/050-014-migrate-webcontracts-to-sourcecontainer-appswebweb-contracts.md`

### Key decisions
- Kept namespaces unchanged.
- Kept `TrackEventValidiation` spelling and `.mixin` casing because Morris.Moxy uses the `.mixin` filename as the generated attribute name.
- Added `RootNamespace=TimeWarp.Architecture` for migrated container-app source projects so source generator output matches existing namespace/global using expectations.
- Fixed nested solution paths for previously migrated projects when review found they still used invalid `../../source/...` paths.

### Test/build outcomes
- `dotnet build source/container-apps/web/web-contracts/web-contracts.csproj` — passed.
- `dotnet build timewarp-architecture.slnx` — passed.
- Review passed after fixes.
- `dotnet build TimeWarp.Architecture/TimeWarp.Architecture.slnx` no longer fails on missing migrated project paths; remaining failures are pre-existing NU1903 vulnerability warnings in test projects and outside this task scope.

## Session
- Created: ses_2d78597cfffeIe36aerm1ibchw (2026-04-21)
- Completed: (2026-05-20)