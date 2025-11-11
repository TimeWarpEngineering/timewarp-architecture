# Run.ps1
$Env:ASPNETCORE_ENVIRONMENT = "Development"

Push-Location $PSScriptRoot
try
{
  $projectPath = "Source/ContainerApps/Aspire/Aspire.AppHost/Aspire.AppHost.csproj"
  dotnet run --project $projectPath
}
finally
{
  Pop-Location
}
