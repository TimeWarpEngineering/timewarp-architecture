# ef-shared-variables.ps1
# Shared paths for EF Core tooling (task 147-007). Paths are relative to this scripts folder.

# Project that owns PostgresDbContext + migrations (web-infrastructure).
$projectPath = "..\..\..\source\container-apps\web\projects\web-infrastructure\web-infrastructure.csproj"

# Startup project: design-time factory lives on web-infrastructure, so the same project is enough.
# (web-server does not reference Microsoft.EntityFrameworkCore.Design.)
$startupProjectPath = $projectPath

$dbContext = "PostgresDbContext"

# Relative to $projectPath (web-infrastructure project directory).
$migrationsOutput = "..\..\platform\postgres\migrations"

$migrationsNamespace = "TimeWarp.Architecture.Persistence.Migrations"

# Absolute-from-scripts path for scripts that touch migration files on disk.
$migrationsFolderPath = "..\..\..\source\container-apps\web\platform\postgres\migrations"
