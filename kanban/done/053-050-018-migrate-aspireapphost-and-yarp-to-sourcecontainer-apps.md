# 050-018: Migrate Aspire.AppHost and Yarp to source/container-apps/

## Parent
050-establish-root-directory-structure-source-tests-in-kebab-case

## Summary

Migrate the Aspire AppHost and Yarp reverse proxy from `TimeWarp.Architecture/Source/ContainerApps/` to `source/container-apps/`. Aspire-service-defaults is already migrated.

## Current State

```
TimeWarp.Architecture/Source/ContainerApps/Aspire/
└── Aspire.AppHost/         ← Depends on all container apps (references them at runtime)

TimeWarp.Architecture/Source/ContainerApps/Yarp/
└── Yarp.csproj + config files   ← Standalone reverse proxy
```

## Target State

```
source/container-apps/
├── aspire/
│   ├── aspire-service-defaults/   ← already migrated
│   └── aspire-app-host/
└── yarp/
```

## Dependencies

| Project | Status |
|---------|--------|
| aspire-service-defaults | Already migrated |
| All container apps (Web, Api, Grpc) | Must be migrated first (tasks 050-015, 050-016, 050-017) |

## Checklist

### Phase 1: Migrate Aspire.AppHost
- [x] Create aspire-app-host/ under source/container-apps/aspire/ with kebab-case (previously prepared references)
- [x] Update/create csproj with corrected relative paths to all container apps + Yarp + Aspire.Hosting packages via CPM
- [x] Update both solution files
- [x] Build verify

### Phase 2: Migrate Yarp
- [x] Create yarp/ under source/container-apps/ with kebab-case files
- [x] Create minimal yarp.csproj (Dockerfile intentionally omitted per prior decision)
- [x] Add missing CPM PackageVersions for Yarp packages
- [x] Update both solution files
- [x] Build verify

### Phase 3: Cleanup
- [x] Remove old Aspire/ and Yarp/ directories under TimeWarp.Architecture/Source/ContainerApps/
- [x] Full solution build verify (individual projects + root slnx references)
- [x] Update kanban task

## Notes

- **This task depends on tasks 050-015, 050-016, and 050-017** — Aspire.AppHost references all container apps
- Aspire.AppHost uses Aspire SDK and service discovery — may have special project type constraints
- Yarp is a standalone project with config files (appsettings.json, Dockerfile) that need kebab-case attention
- Namespaces remain unchanged
- Directory.Build.props for source/container-apps/ already exists with RootNamespace=TimeWarp.Architecture

## Completion Notes (2026-05)

- Successfully migrated:
  - `TimeWarp.Architecture/Source/ContainerApps/Aspire/Aspire.AppHost/` → `source/container-apps/aspire/aspire-app-host/`
  - `TimeWarp.Architecture/Source/ContainerApps/Yarp/` → `source/container-apps/yarp/`
- All source files moved with kebab-case directory structure and lowercase common files (`program.cs`, `global-usings.cs`, `assembly-marker.cs`, `properties/`, etc.).
- **Dockerfile for Yarp was not migrated** (consistent with previous decision on task 053-050-017 that Dockerfiles are obsolete with Aspire).
- Created minimal `yarp.csproj` and `aspire-app-host.csproj` with all relative `ProjectReference` paths corrected for the new `source/container-apps/` layout (fixed off-by-one `../` errors during implementation).
- Added required Central Package Management entries in root `Directory.Packages.props`:
  - `Aspirant.Hosting.Yarp`, `Microsoft.Extensions.ServiceDiscovery.Yarp`, `Yarp.ReverseProxy`
  - `Aspire.Hosting.AppHost`, `Aspire.Hosting.Azure.CosmosDB` (to satisfy Aspire AppHost SDK checks and conditional references)
- Updated both solution files (`timewarp-architecture.slnx` and `TimeWarp.Architecture/TimeWarp.Architecture.slnx`) — the inner solution had pre-existing conditional `#if (yarp)` and `/07-Aspire/` folders that were corrected from old paths.
- Legacy directories fully removed via `git rm`.
- Both new projects build successfully (`yarp.csproj` and `aspire-app-host.csproj`).
- Pre-existing environment limitations (no `node` for TypeScript in web-spa, stale analyzer reference in web-spa) surfaced during broader builds but are unrelated to this migration.
- Card moved from `to-do/` → `in-progress/` → `done/`.

**Verification commands run:**
- `dotnet build source/container-apps/yarp/yarp.csproj` — passed
- `dotnet build source/container-apps/aspire/aspire-app-host/aspire-app-host.csproj` — passed
- Root solution now includes the migrated projects under `/source/container-apps/`