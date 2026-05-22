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