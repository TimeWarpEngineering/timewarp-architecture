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
│   ├── analytics/track-event.cs, track-event-validiation.mixin.cs
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
- [ ] Create source/container-apps/web/web-contracts/ with all subdirectories
- [ ] Verify empty directories are ready

### Phase 2: Migrate Files (kebab-case)
- [ ] git mv Web.Contracts.csproj → web-contracts.csproj
- [ ] git mv AssemblyMarker.cs → assembly-marker.cs
- [ ] git mv GlobalUsings.cs → global-usings.cs
- [ ] git mv Extensions/AssemblyExtensions.cs → extensions/assembly-extensions.cs
- [ ] git mv Types/SignalRResult.cs → types/signal-r-result.cs
- [ ] git mv Mixins/ → mixins/ (keep PascalCase file names)
- [ ] git mv all Feature/ files to kebab-case directories/files
- [ ] Skip Generated/ (gitignored, Morris.Moxy auto-regenerates)

### Phase 3: Update Project File
- [ ] Update csproj to minimal format (remove redundant properties inherited)
- [ ] Update ProjectReference path to foundation-contracts
- [ ] Update AdditionalFiles paths to foundation-contracts mixins
- [ ] Keep all Morris.Moxy-related AdditionalFiles entries
- [ ] Keep PackageReferences (FluentValidation, Morris.Moxy, Passwordless, protobuf-net.Grpc, etc.)

### Phase 4: Update Source Files
- [ ] Add CA1040 pragma to assembly-marker.cs
- [ ] Verify namespaces unchanged (TimeWarp.Architecture.Features.*)

### Phase 5: Update Referencing Projects
Update these 3 projects' ProjectReference paths to Web.Contracts:
- [ ] Web.Application.csproj
- [ ] Web.Server.Integration.Tests.csproj
- [ ] Web.Spa.csproj

### Phase 6: Update Solution Files
- [ ] Update TimeWarp.Architecture/TimeWarp.Architecture.slnx — redirect Web.Contracts project path
- [ ] Update timewarp-architecture.slnx — add web-contracts project under /source/container-apps/web/

### Phase 7: Cleanup and Verify
- [ ] Remove old Web.Contracts/ directory
- [ ] Build verify web-contracts individually
- [ ] Build verify timewarp-architecture.slnx

## Notes

### Morris.Moxy Integration
- Web.Contracts uses Morris.Moxy for mixin code generation
- Mixins directory has .mixin files (CreateCommand, DeleteCommand, GetListQuery, GetQuery, UpdateCommand)
- Generated/ directory contains Moxy-generated files — skipped in migration (gitignored, regenerates)
- AdditionalFiles entries must be preserved for Moxy to work

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
- Paths need updating from `..\..\..\..\..\source\foundation\...` to correct relative path
- Morris.Moxy-generated files reference original file paths — may need rebuild to regenerate

### Namespace Decisions
- Namespaces stay `TimeWarp.Architecture.Features.*` (Admin, Analytics, Auth, etc.)
- Feature-specific namespaces reflect the domain, not the directory structure
- This aligns with the pattern used across all Contracts projects

### Implementation Plan

1. Migrate Web.Contracts from `TimeWarp.Architecture/Source/ContainerApps/Web/Web.Contracts/` to `source/container-apps/web/web-contracts/`.
2. Use `git mv` for all committed files, converting directories and `.cs` filenames to kebab-case.
3. Keep mixin filenames PascalCase under `mixins/` (`CreateCommand.mixin`, `DeleteCommand.mixin`, `GetListQuery.mixin`, `GetQuery.mixin`, `UpdateCommand.mixin`, plus docs).
4. Migrate the `.DotSettings` file as `web-contracts.csproj.DotSettings`.
5. Skip `Generated/` because Morris.Moxy regenerates it.
6. Update `web-contracts.csproj`:
   - Reduce to minimal project format consistent with migrated peers.
   - Update `ProjectReference` to `source/foundation/foundation-contracts/foundation-contracts.csproj` using the correct relative path from `source/container-apps/web/web-contracts/`.
   - Update `AdditionalFiles`, `Compile Remove`, `None Remove`, and `Folder Include` paths from PascalCase to kebab-case.
   - Preserve package references and Morris.Moxy AdditionalFiles.
   - Remove stale comments and stale AdditionalFiles such as `DependencyValidation1.layerdiagram` if present and no longer valid.
7. Update `assembly-marker.cs` with CA1040 suppression around marker interface.
8. Leave namespaces unchanged.
9. Update referencing projects:
   - `TimeWarp.Architecture/Source/ContainerApps/Web/Web.Application/Web.Application.csproj`
   - `TimeWarp.Architecture/Source/ContainerApps/Web/Web.Spa/Web.Spa.csproj`
   - `TimeWarp.Architecture/Tests/ContainerApps/Web/Web.Server.Integration.Tests/Web.Server.Integration.Tests.csproj`
10. Update solution files:
    - `TimeWarp.Architecture/TimeWarp.Architecture.slnx`
    - root `timewarp-architecture.slnx`
11. Remove old empty `Web.Contracts/` directory after migration.
12. Verify with:
    - `dotnet build source/container-apps/web/web-contracts/web-contracts.csproj`
    - `dotnet build TimeWarp.Architecture/Source/ContainerApps/Web/Web.Application/Web.Application.csproj`
    - `dotnet build TimeWarp.Architecture/Source/ContainerApps/Web/Web.Spa/Web.Spa.csproj`
    - `dotnet build timewarp-architecture.slnx`
    - `dotnet build TimeWarp.Architecture/TimeWarp.Architecture.slnx`

### Design Notes

- `TrackEventValidiation.mixin` spelling appears existing and should be preserved as part of migration.
- `Web.Contracts.csproj.DotSettings` may contain path-based ReSharper/Rider settings; migrate it as-is unless build/review requires changes.
- `Features/Auth/Commands/` is an empty folder represented by a csproj `Folder Include`; keep as `features/auth/commands/`.
- `InternalsVisibleTo` values are assembly names, not paths, and need no updates.
- If a design issue or architectural problem appears during implementation, stop and report rather than applying workarounds.

## Results

To be filled after completion.

## Session
- Created: ses_2d78597cfffeIe36aerm1ibchw (2026-04-21)