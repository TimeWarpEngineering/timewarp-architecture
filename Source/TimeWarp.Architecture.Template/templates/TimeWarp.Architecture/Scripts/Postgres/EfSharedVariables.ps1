# EfSharedVariables.ps1
$projectPath = "..\..\Source\ContainerApps\Web\Web.Infrastructure\Web.Infrastructure.csproj"
$startupProjectPath = "..\..\Source\ContainerApps\Web\Web.Server\Web.Server.csproj"
$dbContext = "PostgresDbContext"
$migrationsOutput = "Persistence\Migrations"; # relative to the $projectPath
$migrationsFolderPath = "..\..\Source\ContainerApps\Web\Web.Infrastructure\$migrationsOutput"
