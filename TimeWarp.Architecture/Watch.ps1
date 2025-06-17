# Watch.ps1
$Env:ASPNETCORE_ENVIRONMENT = "Development"

Push-Location $PSScriptRoot
try {   
  $Env:DOTNET_WATCH_RESTART_ON_RUDE_EDIT = "True"
  $projectPath = "Source/ContainerApps/Aspire/Aspire.AppHost/Aspire.AppHost.csproj"
  dotnet watch --project $projectPath
}
finally {
  Pop-Location
}
