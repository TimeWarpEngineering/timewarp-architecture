$Env:ASPNETCORE_ENVIRONMENT = "Development"

Push-Location $PSScriptRoot
try {
  #if(web)
  Push-Location ./Source/ContainerApps/Web/Web.TypeScript
  try {
    npx run tailwind-watch
  }
  finally {
    Pop-Location
  }
}
finally {
  Pop-Location
}
