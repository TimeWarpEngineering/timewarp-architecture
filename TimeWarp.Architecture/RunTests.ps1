Push-Location $PSScriptRoot
try {
    # Analyzers Tests
    Write-Host "Running Analyzer Tests..." -ForegroundColor Cyan
    dotnet fixie Tests/Analyzers/TimeWarp.Architecture.Analyzers.Tests
    dotnet fixie Tests/Analyzers/TimeWarp.Architecture.SourceGenerator.Tests

    # Common Tests
    Write-Host "Running Common Tests..." -ForegroundColor Cyan
    dotnet fixie Tests/Common/Common.Infrastructure.Tests

    # Container Apps Tests
    Write-Host "Running Container Apps Tests..." -ForegroundColor Cyan
    dotnet fixie Tests/ContainerApps/Api/Api.Server.Integration.Tests

    # End to End Tests
    Write-Host "Running End to End Tests..." -ForegroundColor Cyan
    dotnet test Tests/EndToEnd.Playwright.Tests

    # Library Tests
    Write-Host "Running Library Tests..." -ForegroundColor Cyan
    dotnet fixie Tests/Libraries/TimeWarp.Automation.Tests

    # Web Tests
    Write-Host "Running Web Tests..." -ForegroundColor Cyan
    dotnet fixie Tests/Web.Server.Integration.Tests
    dotnet fixie Tests/Web.Spa.Integration.Tests
}
finally {
    Pop-Location
}
