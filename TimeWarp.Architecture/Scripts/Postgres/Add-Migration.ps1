# Add-Migration.ps1

param (
  [Parameter(Mandatory=$true)]
  [string]$MigrationName
)
  
  Push-Location $PSScriptRoot
  
try {
  . .\EfSharedVariables.ps1
  dotnet ef migrations add $MigrationName `
    --project $projectPath `
    --startup-project $startupProjectPath `
    --context $dbContext `
    --output-dir $migrationsOutput
}
finally {
  Pop-Location
}
