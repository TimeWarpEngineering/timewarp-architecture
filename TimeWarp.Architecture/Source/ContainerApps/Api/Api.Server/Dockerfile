ARG dotnet_version=6.0
#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

# https://hub.docker.com/_/microsoft-dotnet
FROM mcr.microsoft.com/dotnet/sdk:$dotnet_version-jammy AS build

WORKDIR /git
COPY ["NuGet.config", "."]
COPY ["Directory.Build.props", "."]
COPY ["Directory.Packages.props", "."]
COPY ["Source/ContainerApps/Api/Api.Contracts/Api.Contracts.csproj", "Source/ContainerApps/Api/Api.Contracts/"]
COPY ["Source/ContainerApps/Api/Api.Server/Api.Server.csproj", "Source/ContainerApps/Api/Api.Server/"]
COPY ["Source/Common/Common.Server/Common.Server.csproj", "Source/Common/Common.Server/"]
COPY ["Source/Common/Common.Infrastructure/Common.Infrastructure.csproj", "Source/Common/Common.Infrastructure/"]
COPY ["Source/Common/Common.Application/Common.Application.csproj", "Source/Common/Common.Application/"]
COPY ["Source/Common/Common.Domain/Common.Domain.csproj", "Source/Common/Common.Domain/"]
COPY ["Source/Common/Common.Contracts/Common.Contracts.csproj", "Source/Common/Common.Contracts/"]
COPY ["Source/Libraries/TimeWarp.Modules/TimeWarp.Modules.csproj", "Source/Libraries/TimeWarp.Modules/"]
COPY ["Source/ContainerApps/Api/Api.Infrastructure/Api.Infrastructure.csproj", "Source/ContainerApps/Api/Api.Infrastructure/"]
COPY ["Source/ContainerApps/Api/Api.Application/Api.Application.csproj", "Source/ContainerApps/Api/Api.Application/"]
COPY ["Source/ContainerApps/Api/Api.Domain/Api.Domain.csproj", "Source/ContainerApps/Api/Api.Domain/"]

# restore nugets
RUN dotnet restore "Source/ContainerApps/Api/Api.Server/Api.Server.csproj"

COPY ["/Source/ContainerApps/Api/.", "Source/ContainerApps/Api"]
COPY ["/Source/Common/.", "Source/Common"]
COPY ["/Source/Libraries/.", "Source/Libraries"]

WORKDIR "/git/Source/ContainerApps/Api/Api.Server"
RUN dotnet build "Api.Server.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Api.Server.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:$dotnet_version-jammy AS final
WORKDIR /app
EXPOSE 80
EXPOSE 443
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Api.Server.dll"]
