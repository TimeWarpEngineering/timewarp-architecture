$Env:ASPNETCORE_ENVIRONMENT = "Development"

Push-Location $PSScriptRoot
try {
  #if(web)
  Push-Location ./Source/ContainerApps/Web/Web.TypeScript
  try {
    if (Test-Path .\node_modules\) {
      Remove-Item .\node_modules\ -Force -Recurse
    }
    npm install  
    dotnet build
  }
  finally {
    Pop-Location
  }
}
finally {
  Pop-Location
}
