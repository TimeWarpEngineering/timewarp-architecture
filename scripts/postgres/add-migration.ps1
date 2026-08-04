# add-migration.ps1
# Adds an EF Core migration for PostgresDbContext into platform/postgres/migrations/.
# Prefer: dotnet tool restore once per machine, then this script or the how-to command.

param (
  [Parameter(Mandatory = $true)]
  [string]$MigrationName
)

Push-Location $PSScriptRoot

try {
  . .\ef-shared-variables.ps1
  dotnet ef migrations add $MigrationName `
    --project $projectPath `
    --startup-project $startupProjectPath `
    --context $dbContext `
    --output-dir $migrationsOutput `
    --namespace $migrationsNamespace
}
finally {
  Pop-Location
}
