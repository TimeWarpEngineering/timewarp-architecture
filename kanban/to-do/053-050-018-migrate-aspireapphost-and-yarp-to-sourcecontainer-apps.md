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
- [ ] git mv Aspire.AppHost files to aspire-app-host/ with kebab-case
- [ ] Update csproj (ProjectReferences to all container apps, Aspire SDK reference)
- [ ] Update both solution files
- [ ] Build verify

### Phase 2: Migrate Yarp
- [ ] git mv Yarp files to yarp/ with kebab-case
- [ ] Update csproj (minimal format, package references)
- [ ] Update both solution files
- [ ] Build verify

### Phase 3: Cleanup
- [ ] Remove old Aspire/ and Yarp/ directories
- [ ] Full solution build verify
- [ ] Update kanban task

## Notes

- **This task depends on tasks 050-015, 050-016, and 050-017** — Aspire.AppHost references all container apps
- Aspire.AppHost uses Aspire SDK and service discovery — may have special project type constraints
- Yarp is a standalone project with config files (appsettings.json, Dockerfile) that need kebab-case attention
- Namespaces remain unchanged
- Directory.Build.props for source/container-apps/ already exists with RootNamespace=TimeWarp.Architecture