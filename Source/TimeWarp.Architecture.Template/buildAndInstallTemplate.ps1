$Version = '1.2.3'
dotnet pack .\TimeWarp.Architecture.csproj -o . /p:PackageVersion=$Version
dotnet new -u TimeWarp.Architecture
dotnet new -i TimeWarp.Architecture.$Version.nupkg