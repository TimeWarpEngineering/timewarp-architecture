$Env:ASPNETCORE_ENVIRONMENT = "Development"

Push-Location $PSScriptRoot
try {   
  $Env:DOTNET_WATCH_RESTART_ON_RUDE_EDIT = "True"
  dotnet watch --project .\Source\Server\ run
}
finally {
  Pop-Location
}
