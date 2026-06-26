# drop-database.ps1

Push-Location $PSScriptRoot

try {
    . .\ef-shared-variables.ps1
    dotnet ef database drop --project $projectPath --startup-project $startupProjectPath --context $dbContext
}
finally {
    Pop-Location 
}
