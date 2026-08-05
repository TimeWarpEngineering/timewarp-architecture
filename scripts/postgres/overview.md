# Entity Framework Core PowerShell scripts

Helpers for `PostgresDbContext` migrations. Schema home (task 147-007):

`source/container-apps/web/platform/postgres/migrations/`

**Runtime path (147-007):** Aspire AppHost `AddEFMigrations` (`web-migrations` resource,
`RunDatabaseUpdateOnStart`). These scripts are for scaffolding and ad-hoc CLI use.

**Once per machine:** `dotnet tool restore` (pins `dotnet-ef` in `.config/dotnet-tools.json`).

## Scripts

### add-migration.ps1

```powershell
.\add-migration.ps1 -MigrationName "YourMigrationName"
```

### update-database.ps1

Applies pending migrations using the design-time factory connection resolution.
Under Aspire, prefer `dev run` / the `web-migrations` resource instead.

### drop-database.ps1

Drops the database resolved by the design-time factory.

### reset-database-migrations.ps1

Deletes the migrations folder, scaffolds a new initial migration, and updates the database.
**Destructive** — template dogfood only. Prefer accreting migrations (D9) unless you
intentionally squash.

### ef-shared-variables.ps1

Shared project paths, context name, output directory, and namespace.

## Cutover wipe (EnsureCreated → migrations)

Volumes created before task 147-007 have no `__EFMigrationsHistory`. One-time:

```bash
# Stop AppHost, then remove the Aspire Postgres data volume for this app (name is
# deterministic per AppHost; list with: docker volume ls | grep postgres)
# After wipe, next dev run applies InitialPostgresModel cleanly.
```

Or set `Postgres:UseDataVolume=false` once for an ephemeral container, then re-enable.

## Canonical CLI (also in how-to-add-your-aggregate.md §8)

```bash
dotnet tool restore

dotnet ef migrations add <Name> \
  --project source/container-apps/web/projects/web-infrastructure/web-infrastructure.csproj \
  --startup-project source/container-apps/web/projects/web-server/web-server.csproj \
  --context PostgresDbContext \
  --output-dir ../../platform/postgres/migrations \
  --namespace TimeWarp.Architecture.Persistence.Migrations
```
