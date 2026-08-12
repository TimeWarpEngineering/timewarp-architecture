# drop-database.ps1
# Drops the database resolved by the design-time factory (ConnectionStrings:postgres-db / env).

Push-Location $PSScriptRoot

try {
  . .\ef-shared-variables.ps1
  dotnet ef database drop `
    --project $projectPath `
    --startup-project $startupProjectPath `
    --context $dbContext
}
finally {
  Pop-Location
}
