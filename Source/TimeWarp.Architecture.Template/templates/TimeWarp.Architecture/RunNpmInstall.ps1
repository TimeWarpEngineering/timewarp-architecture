$Env:ASPNETCORE_ENVIRONMENT = "Development"

Push-Location $PSScriptRoot
try {
  #if(web)
  Push-Location ./Source/ContainerApps/Web/Web.TypeScript
  rm .\node_modules\ -Force -Recurse
  npm install  
  dotnet build
  Pop-Location
}
finally {
  Pop-Location
}
