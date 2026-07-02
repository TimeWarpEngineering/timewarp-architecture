# Remove CosmosDb from template

CosmosDb is Azure-specific, requires the local emulator for dev, and adds little beyond what
Postgres already demonstrates in the template. Remove it entirely rather than keeping it as an
optional feature flag.

## Requirements

- No CosmosDb source, packages, Aspire resources, config, scripts, or docs remain in the repo
- Postgres remains the sole persistence example (EF Core + Aspire)
- `dev build` and `dev test` pass
- `dotnet new timewarp-architecture` generates a clean-building solution (no `--cosmosdb` flag)

## Checklist

### Source removal
- [x] Delete `source/container-apps/web/web-infrastructure/persistence/cosmos-db-context.cs`
- [x] Delete `source/container-apps/web/web-server/modules/cosmos-db-module.cs`
- [x] Delete `source/container-apps/web/web-server/hosted-services/cosmos-db-context-startup-hosted-service.cs`
- [x] Delete Cosmos-only `profile-configuration.cs` and fix `web-infrastructure/global-usings.cs`
- [x] Remove Cosmos constants from `source/container-apps/aspire/aspire-app-host/constants.cs`
- [x] Remove `#if cosmosdb` blocks from `aspire-app-host/program.cs` and `aspire-app-host.csproj`
- [x] Remove `#if(cosmosdb)` blocks from `web-server/program.cs` and `global-usings.cs`
- [x] Remove `cosmosdb` from `web-server.csproj` `DefineConstants`
- [x] Remove `Aspire.Microsoft.Azure.Cosmos` and `Aspire.Microsoft.EntityFrameworkCore.Cosmos` from `web-server.csproj`
- [x] Remove `Microsoft.EntityFrameworkCore.Cosmos` from `web-infrastructure.csproj`
- [x] Remove `CosmosDbOptions` sections from `appsettings.Kubernetes_Docker.json` (web-server, yarp)
- [x] Clean commented Cosmos refs in `api-server/program.cs`

### Template & tooling
- [x] Remove `cosmosdb` symbol and `(!cosmosdb)` exclude block from `.template.config/template.json`
- [x] Remove Cosmos packages from `Directory.Packages.props`
- [x] Delete `scripts/run-cosmos-db-emulator.ps1`
- [x] Remove CosmosDb references from test `appsettings.json` files

### Documentation & related tasks
- [x] Update `timewarp-templates/documentation/timewarp-architecture-template/Overview.md` (emulator install, Cosmos links)
- [x] Update `CLAUDE.md` feature-flags list (drop `cosmosdb`)
- [x] Update task 071 scope — remove `cosmosdb` row from per-flag table
- [x] Update task 061 — drop cosmos emulator script migration item
- [x] Update task 070 — drop cosmosdb from AppHost flag references

### Verification
- [x] `dev build` green (0 errors, 0 warnings)
- [x] `dev test` green

## Results

Removed all CosmosDb wiring: Aspire resource + emulator, EF Core context/module, NuGet packages,
template `--cosmosdb` flag, emulator script, and docs. Postgres remains as the persistence example.
Profile entity and GetProfile handler unchanged (still mock data; no DB backing yet).

## Session

- Created: 2026-07-01
- Implementation: 2026-07-01