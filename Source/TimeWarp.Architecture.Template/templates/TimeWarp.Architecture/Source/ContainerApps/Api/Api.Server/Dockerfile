#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
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
COPY ["Source/TimeWarp.OptionsValidation/TimeWarp.OptionsValidation.csproj", "Source/TimeWarp.OptionsValidation/"]
COPY ["Source/ContainerApps/Api/Api.Infrastructure/Api.Infrastructure.csproj", "Source/ContainerApps/Api/Api.Infrastructure/"]
COPY ["Source/ContainerApps/Api/Api.Application/Api.Application.csproj", "Source/ContainerApps/Api/Api.Application/"]
COPY ["Source/ContainerApps/Api/Api.Domain/Api.Domain.csproj", "Source/ContainerApps/Api/Api.Domain/"]
RUN dotnet restore "Source/ContainerApps/Api/Api.Server/Api.Server.csproj"
COPY . .
WORKDIR "/src/Source/ContainerApps/Api/Api.Server"
RUN dotnet build "Api.Server.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Api.Server.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Api.Server.dll"]
