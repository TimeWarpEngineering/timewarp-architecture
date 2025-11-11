# Build.ps1
$Env:ASPNETCORE_ENVIRONMENT = "Development"

Push-Location $PSScriptRoot
try {
  Write-Host "Building TimeWarp.Architecture solution..." -ForegroundColor Green
  
  $projectPath = "Source/ContainerApps/Aspire/Aspire.AppHost/Aspire.AppHost.csproj"
  dotnet build $projectPath
  
  Write-Host "Build complete!" -ForegroundColor Green
}
finally {
  Pop-Location
}