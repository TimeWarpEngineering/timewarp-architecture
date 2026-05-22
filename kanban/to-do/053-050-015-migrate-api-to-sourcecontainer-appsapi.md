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