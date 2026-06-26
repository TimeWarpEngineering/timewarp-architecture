# add-migration.ps1

param (
  [Parameter(Mandatory=$true)]
  [string]$MigrationName
)
  
  Push-Location $PSScriptRoot
  
try {
  . .\ef-shared-variables.ps1
  dotnet ef migrations add $MigrationName `
    --project $projectPath `
    --startup-project $startupProjectPath `
    --context $dbContext `
    --output-dir $migrationsOutput
}
finally {
  Pop-Location
}
