$Env:ASPNETCORE_ENVIRONMENT = "Development"

Push-Location $PSScriptRoot
try {
  Push-Location ./Source/ContainerApps/Web/Web.Spa
  try {
    npm run css:build
  }
  finally {
    Pop-Location
  }
}
finally {
  Pop-Location
}
