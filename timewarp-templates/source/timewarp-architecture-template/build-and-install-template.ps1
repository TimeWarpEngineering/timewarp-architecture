# Save the current location
Push-Location $PSScriptRoot

try {
    # Version is inherited from the repo-wide Directory.Version.props (single source of truth).
    dotnet pack .\timewarp-architecture-template.csproj -o .
    dotnet new uninstall TimeWarp.Architecture
    $package = Get-ChildItem -Path . -Filter 'TimeWarp.Architecture.*.nupkg' |
        Sort-Object Name -Descending | Select-Object -First 1
    dotnet new install $package.FullName
}
finally {
    # Always restore the original location, even if an error occurs
    Pop-Location
}
