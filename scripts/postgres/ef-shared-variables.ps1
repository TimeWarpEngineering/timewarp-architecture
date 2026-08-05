# ef-shared-variables.ps1
# Shared paths for EF Core tooling (task 147-007). Paths are relative to this scripts folder.

# Project that owns PostgresDbContext + migrations (web-infrastructure).
$projectPath = "..\..\..\source\container-apps\web\projects\web-infrastructure\web-infrastructure.csproj"

# Startup project: web-server — matching the AppHost's AddEFMigrations wiring and
# how-to-add-your-aggregate.md §8. web-server DOES reference Microsoft.EntityFrameworkCore.Design
# (added in 147-007 precisely because it is the EF startup project).
$startupProjectPath = "..\..\..\source\container-apps\web\projects\web-server\web-server.csproj"

$dbContext = "PostgresDbContext"

# Relative to $projectPath (web-infrastructure project directory).
$migrationsOutput = "..\..\platform\postgres\migrations"

$migrationsNamespace = "TimeWarp.Architecture.Persistence.Migrations"

# Absolute-from-scripts path for scripts that touch migration files on disk.
$migrationsFolderPath = "..\..\..\source\container-apps\web\platform\postgres\migrations"
