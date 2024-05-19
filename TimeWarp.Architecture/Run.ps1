$Env:ASPNETCORE_ENVIRONMENT = "Development"

Push-Location $PSScriptRoot
try {
  # Start Cosmos DB emulator
  # see https://timewarpengineering.github.io/timewarp-architecture/#prerequisites
  # Start-CosmosDbEmulator
  # TODO add check for CosmosDB emulator to be running

  Push-Location DevOps/Tye
  # dotnet build -c Release
  # tye run --dashboard --logs console -v Debug --no-build
  tye run --dashboard --watch --logs console -v Debug
  # Start-Process pwsh -argument '-nologo -noprofile -executionpolicy bypass -command tye run --dashboard --logs console'
  Pop-Location
}
finally {
  Pop-Location
}
