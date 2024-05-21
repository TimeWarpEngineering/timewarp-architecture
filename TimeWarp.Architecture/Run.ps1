# Run.ps1
$Env:ASPNETCORE_ENVIRONMENT = "Development"

Push-Location $PSScriptRoot
try {
  # Run the dotnet project and capture the output
  $output = & dotnet watch --project Source/ContainerApps/Aspire/Aspire.AppHost/Aspire.AppHost.csproj

  # Extract the login URL from the output
  $url = ($output -split "`n" | Where-Object { $_ -match "https://localhost:17204/login?t=" }) -replace ".*(https://localhost:17204/login\?t=[a-f0-9]+).*", '$1'

  if ($url) {
    # Launch the browser to the dashboard URL
    Start-Process $url
  } else {
    Write-Host "Failed to extract the login URL from the output."
  }

}
finally {
  Pop-Location
}
