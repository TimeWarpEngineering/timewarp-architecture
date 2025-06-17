Push-Location $PSScriptRoot

try {   
  dotnet dg --solutionFile ../TimeWarp.Architecture.sln -o ..\Documentation\Developer\Reference\dependencies-with-nuget.puml --includeNugetPackages
  dotnet dg --solutionFile ../TimeWarp.Architecture.sln -o ..\Documentation\Developer\Reference\dependencies.puml
}
finally {
  Pop-Location
}
