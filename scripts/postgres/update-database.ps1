# update-database.ps1
# Applies pending migrations against the connection resolved by the design-time factory
# (ConnectionStrings:postgres-db / env / PostgresDbOptions). Under Aspire local run, prefer
# AppHost AddEFMigrations (RunDatabaseUpdateOnStart) instead of this script.

Push-Location $PSScriptRoot

try {
  . .\ef-shared-variables.ps1
  dotnet ef database update `
    --project $projectPath `
    --startup-project $startupProjectPath `
    --context $dbContext
}
finally {
  Pop-Location
}
