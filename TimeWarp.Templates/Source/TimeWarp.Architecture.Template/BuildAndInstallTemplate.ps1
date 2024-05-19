$Version = '10.2.3'
dotnet pack .\TimeWarp.Architecture.csproj -o . /p:PackageVersion=$Version
dotnet new uninstall TimeWarp.Architecture
dotnet new install TimeWarp.Architecture.$Version.nupkg