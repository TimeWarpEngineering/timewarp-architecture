$Env:ASPNETCORE_ENVIRONMENT = "Development"

Push-Location $PSScriptRoot
try {
  #if(web)
  Push-Location ../source/container-apps/web/web-spa
  try {
    if (Test-Path .\node_modules\) {
      Remove-Item .\node_modules\ -Force -Recurse
    }
    npm install
  }
  finally {
    Pop-Location
  }
}
finally {
  Pop-Location
}
