$Env:ASPNETCORE_ENVIRONMENT = "Development"

Push-Location $PSScriptRoot
try {
  Push-Location ../source/container-apps/web/web-spa
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
