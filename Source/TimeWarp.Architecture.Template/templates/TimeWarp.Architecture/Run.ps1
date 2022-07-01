$Env:ASPNETCORE_ENVIRONMENT = "Development"

Push-Location $PSScriptRoot
try {   
  Push-Location ./Source/ContainerApps/Web/Web.TypeScript
  npm install  
  Pop-Location
  
  # currently `tye run` doesn't build the esproj file, so we need to do it manually
  dotnet build
  
  # Start Cosmos DB emulator
  # see https://timewarpengineering.github.io/timewarp-architecture/#prerequisites
  # Start-CosmosDbEmulator
  # TODO add check for CosmosDB emulator to be running

  Push-Location DevOps/Tye
  # Launch new shell for tye 
  # tye run --dashboard --watch --logs console
  Start-Process pwsh -argument '-nologo -noprofile -executionpolicy bypass -command tye run --dashboard --logs console'
  Pop-Location
}
finally {
  Pop-Location
}

 