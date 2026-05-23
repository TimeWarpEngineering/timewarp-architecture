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

### Implementation Plan

**1. Projects to migrate** (from `TimeWarp.Architecture/Source/ContainerApps/Web/` to `source/container-apps/web/`):
- `Web.Application` → `web-application`
- `Web.Infrastructure` → `web-infrastructure`
- `Web.Server` → `web-server`
- `Web.Spa` → `web-spa`

**2. Migration method:**
- Use `git mv` for committed files
- Convert directories and source filenames to kebab-case
- Do NOT migrate `obj/`, `bin/`, `node_modules/`, generated build artifacts, or generated client output unless committed/source-controlled and clearly required

**3. Namespaces:** Keep unchanged (`TimeWarp.Architecture.Web.*` and feature namespaces)

**4. Follow established migration patterns from tasks 050-014 through 050-016:**
- Minimal project files
- Rely on `source/container-apps/Directory.Build.props` for `RootNamespace=TimeWarp.Architecture`
- Rely on root `Directory.Build.props` for `ImplicitUsings=enable` and `Nullable=enable`
- CA1040 suppression around marker interfaces where applicable
- Nested solution paths use `../source/...`
- Root solution paths use `source/...`

**5. Leaf-to-root migration order:**
1. Web.Application
2. Web.Infrastructure
3. Web.Server
4. Web.Spa (highest risk due to Blazor WASM/npm/TypeScript/Tailwind)

**6. Project references to update:**
- Web.Infrastructure
- Web.Server
- Web.Server.Integration.Tests
- Web.Spa.Integration.Tests
- Testing.Common
- Aspire.AppHost
- Web.Spa conditional references to api-contracts/grpc-contracts if needed

**7. Solution files to update:**
- `TimeWarp.Architecture/TimeWarp.Architecture.slnx`
- `timewarp-architecture.slnx`

**8. Scripts and Docker-related references to update:**
- `TimeWarp.Architecture/DevOps/Docker/BuildImages.ps1`
- Web.Server Dockerfile
- `RunTailwind.ps1`
- `RunNpmInstall.ps1` if present
- EF/Postgres helper scripts if they reference Web.Infrastructure/Web.Server
- Describe/run scripts if they reference Web.Server/Web.Spa paths

**9. Web.Server specifics:**
- Preserve UserSecretsId/DefineConstants/package references
- Update Dockerfile paths if they can be corrected cleanly
- Handle temporary Aspire.AppHost Constants.cs include path carefully since Aspire.AppHost is not migrated yet

**10. Web.Spa specifics:**
- Update TypeScript/npm/Tailwind paths (`tsconfig.json`, `.eslintrc.js`, `tailwind.config.js`, package scripts if needed)
- Update MSBuild content/remove/item paths to kebab-case
- Handle `$(ProjectName)` changes caused by csproj rename (`Web.Spa` → `web-spa`)
- Preserve static asset casing under `wwwroot` unless clearly source-controlled convention requires change
- Convert folders to kebab-case, but keep `.razor` and `.razor.cs` component filenames PascalCase
- Ensure Razor component namespaces are explicit; do not rely on folder-derived Razor auto-namespaces
- Preserve component classes and namespaces

**11. Verification commands:**
- `dotnet build source/container-apps/web/web-application/web-application.csproj`
- `dotnet build source/container-apps/web/web-infrastructure/web-infrastructure.csproj`
- `dotnet build source/container-apps/web/web-server/web-server.csproj`
- `dotnet build source/container-apps/web/web-spa/web-spa.csproj`
- `dotnet build timewarp-architecture.slnx`
- `dotnet build TimeWarp.Architecture/TimeWarp.Architecture.slnx` if practical; report unrelated pre-existing failures separately

### Design Notes

- Web.Spa is the highest-risk part because Razor component filenames and explicit namespaces must be preserved while folders move to kebab-case.
- Aspire.AppHost is not migrated yet; only update Web.Server reference paths into the new Web.Server location.
- Web.Spa and Web.Server may have cross-references in Tailwind/static asset build steps.
- Generated/obj/bin/node_modules artifacts must not be migrated.
- Folders should be kebab-case; `.razor` component files must stay PascalCase; Razor namespaces must be explicit and unchanged.
- **If a design issue or architectural problem appears during implementation, stop and report rather than applying workarounds.**
