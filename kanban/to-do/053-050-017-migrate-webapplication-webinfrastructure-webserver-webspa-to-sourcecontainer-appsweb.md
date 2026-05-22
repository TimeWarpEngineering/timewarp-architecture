# 050-017: Migrate Web.Application, Web.Infrastructure, Web.Server, Web.Spa to source/container-apps/web/

## Parent
050-establish-root-directory-structure-source-tests-in-kebab-case

## Summary

Migrate the 4 remaining Web container-app projects from `TimeWarp.Architecture/Source/ContainerApps/Web/` to `source/container-apps/web/` with kebab-case naming. Web.Contracts and Web.Domain are already migrated.

## Current State

```
TimeWarp.Architecture/Source/ContainerApps/Web/
├── Web.Application/      ← Depends on web-contracts (migrated), web-domain (migrated), foundation-application (migrated)
├── Web.Infrastructure/   ← Depends on Web.Application
├── Web.Server/            ← Depends on Web.Infrastructure, Web.Application, Aspire service defaults (migrated)
└── Web.Spa/               ← Depends on web-contracts (migrated), Blazor WASM project
```

## Target State

```
source/container-apps/web/
├── web-contracts/       ← already migrated
├── web-domain/          ← already migrated
├── web-application/
├── web-infrastructure/
├── web-server/
└── web-spa/
```

## Dependencies (already migrated)

| Project | Migrated To |
|---------|-------------|
| web-contracts | source/container-apps/web/web-contracts/ |
| web-domain | source/container-apps/web/web-domain/ |
| foundation-application | source/foundation/foundation-application/ |
| foundation-contracts | source/foundation/foundation-contracts/ |
| aspire-service-defaults | source/container-apps/aspire/aspire-service-defaults/ |

## Migration Order (leaf-to-root)

1. Web.Application — leaf dependency (web-contracts, web-domain, foundation)
2. Web.Infrastructure — depends on Web.Application
3. Web.Server — depends on Web.Infrastructure, Web.Application, Aspire
4. Web.Spa — depends on web-contracts; Blazor WASM project (special handling)

## Checklist

### Phase 1: Migrate Web.Application
- [ ] git mv Web.Application files to web-application/ with kebab-case
- [ ] Update csproj (minimal format, ProjectReferences to migrated projects, RootNamespace)
- [ ] Update assembly-marker.cs (CA1040 pragma)
- [ ] Update referencing projects (Web.Infrastructure, Web.Server, test projects)
- [ ] Update both solution files
- [ ] Build verify

### Phase 2: Migrate Web.Infrastructure
- [ ] git mv Web.Infrastructure files to web-infrastructure/ with kebab-case
- [ ] Update csproj (ProjectReferences to web-application)
- [ ] Update referencing projects (Web.Server, test projects)
- [ ] Update both solution files
- [ ] Build verify

### Phase 3: Migrate Web.Server
- [ ] git mv Web.Server files to web-server/ with kebab-case
- [ ] Update csproj (ProjectReferences to web-infrastructure, web-application, aspire-service-defaults)
- [ ] Update both solution files
- [ ] Build verify

### Phase 4: Migrate Web.Spa
- [ ] git mv Web.Spa files to web-spa/ with kebab-case
- [ ] Update csproj (ProjectReferences to web-contracts; special Blazor WASM handling)
- [ ] Update both solution files
- [ ] Build verify

### Phase 5: Cleanup
- [ ] Remove old Web/ directory
- [ ] Full solution build verify
- [ ] Update kanban task

## Notes

- Namespaces remain unchanged (TimeWarp.Architecture.Web.*)
- Directory.Build.props for source/container-apps/ already exists with RootNamespace=TimeWarp.Architecture
- Web.Spa is a Blazor WebAssembly project — may need special handling for static assets, npm/webpack integration, and service worker config
- Web.Server is the Aspire host entry point — references multiple container apps
- This is the largest and most complex remaining migration task
- Consider splitting Web.Spa into a separate task if Blazor WASM specifics warrant it