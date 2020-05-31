$Version = '1.2.3'
dotnet pack .\TimeWarp.AspNetCore.Blazor.Templates.csproj -o . /p:PackageVersion=$Version
dotnet new -u TimeWarp.AspNetCore.Blazor.Templates
dotnet new -i TimeWarp.AspNetCore.Blazor.Templates.$Version.nupkg