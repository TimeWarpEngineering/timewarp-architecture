$Env:ASPNETCORE_ENVIRONMENT = "Development"
Start-CosmosDbEmulator
dotnet run --project .\Source\Server\TimeWarp.Blazor.Server.csproj
