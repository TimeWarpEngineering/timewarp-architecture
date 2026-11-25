# Fix dev db add-migration snapshot output directory

## Description

Running `dev db add-migration <Name>` (`tools/dev-cli/endpoints/db-add-migration-command.cs` and `tools/dev-cli/services/db-ef.cs`) places the new migration and designer files in `source/container-apps/web/platform/postgres/migrations/`, but writes the updated `PostgresDbContextModelSnapshot.cs` into the wrong folder:

`source/container-apps/web/projects/web-infrastructure/TimeWarp/Architecture/Persistence/Migrations/PostgresDbContextModelSnapshot.cs`

instead of updating the tracked snapshot at:

`source/container-apps/web/platform/postgres/migrations/PostgresDbContextModelSnapshot.cs`

This surfaced during task 520 (`AddFeedbackLastActivity`), where the implementer had to manually move the snapshot over the tracked file and delete the stray `TimeWarp/...` directory tree.

## Cause

`web-infrastructure.csproj` links `source/container-apps/web/platform/postgres/migrations/*.cs` from outside the project directory (`../../platform/postgres/migrations`), while `DbEf.BuildAddMigrationArguments` passes `--output-dir ../../platform/postgres/migrations --namespace TimeWarp.Architecture.Persistence.Migrations`. When EF Core's `MigrationsScaffolder` cannot resolve the existing snapshot path inside the project directory, it falls back to placing `PostgresDbContextModelSnapshot.cs` under `<ProjectDir>/<NamespacePath>/`.

## Scope

1. **Fix `dev db add-migration` so `PostgresDbContextModelSnapshot.cs` always lands in `source/container-apps/web/platform/postgres/migrations/`:**
   - First check whether EF Core's `dotnet ef migrations add` options or `web-infrastructure` project metadata can make EF Core write the snapshot directly to `--output-dir` without creating `TimeWarp/Architecture/Persistence/Migrations/`.
   - If EF Core's `MigrationsScaffolder` always emits the snapshot under the namespace path for linked outside-project migrations, add deterministic post-scaffold reconciliation in `DbEf` / `DbAddMigrationCommand` that detects the misplaced `PostgresDbContextModelSnapshot.cs`, moves it over the tracked `source/container-apps/web/platform/postgres/migrations/PostgresDbContextModelSnapshot.cs`, removes the empty generated directory segments under `web-infrastructure/`, and fails loudly if neither snapshot was updated.
   - Preserve `dotnet-new` `sourceName` template safety (no hardcoded unrewritten namespace strings outside `DbEf.MigrationsNamespace`).
2. **Add unit tests in `tests/tools/dev-cli-tests/db-ef-tests.cs`:**
   - Keep `DbEf` free of Amuru/Terminal dependencies so `dev-cli-tests` continues to compile-include it cleanly.
   - Cover the snapshot path resolution / relocation and empty-directory cleanup (using a temporary directory fixture), plus any argument changes.
3. **Update `#region Design`** in `tools/dev-cli/services/db-ef.cs` and `tools/dev-cli/endpoints/db-add-migration-command.cs` to record the behavior.

## Checklist

- [ ] Investigate root cause with `dotnet ef migrations add` and implement the cleanest fix in `tools/dev-cli/services/db-ef.cs` and `tools/dev-cli/endpoints/db-add-migration-command.cs`
- [ ] Ensure no stray `TimeWarp/Architecture/Persistence/Migrations/` directory or duplicate `PostgresDbContextModelSnapshot.cs` remains under `source/container-apps/web/projects/web-infrastructure/`
- [ ] Add unit tests in `tests/tools/dev-cli-tests/db-ef-tests.cs`
- [ ] Verify `./bin/dev build`, `./bin/dev test --project dev-cli-tests`, and `ganda repo audit`

## Session

- Created: 37533 (2026-11-25)

## Notes

- Observed during task 520 (`AddFeedbackLastActivity`).
- Existing tests live in `tests/tools/dev-cli-tests/db-ef-tests.cs`.
