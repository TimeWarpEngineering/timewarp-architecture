# Drop-Database.ps1

Push-Location $PSScriptRoot

try {
    . .\EfSharedVariables.ps1
    dotnet ef database drop --project $projectPath --startup-project $startupProjectPath --context $dbContext
}
finally {
    Pop-Location 
}
