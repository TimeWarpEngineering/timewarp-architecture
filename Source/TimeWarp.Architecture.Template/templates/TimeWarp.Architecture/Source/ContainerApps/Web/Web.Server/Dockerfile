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
COPY ["Source/ContainerApps/Web/Web.Server/Web.Server.csproj", "Source/ContainerApps/Web/Web.Server/"]
COPY ["Source/Common/Common.Server/Common.Server.csproj", "Source/Common/Common.Server/"]
COPY ["Source/Common/Common.Infrastructure/Common.Infrastructure.csproj", "Source/Common/Common.Infrastructure/"]
COPY ["Source/Common/Common.Application/Common.Application.csproj", "Source/Common/Common.Application/"]
COPY ["Source/Common/Common.Domain/Common.Domain.csproj", "Source/Common/Common.Domain/"]
COPY ["Source/Common/Common.Contracts/Common.Contracts.csproj", "Source/Common/Common.Contracts/"]
COPY ["Source/Libraries/TimeWarp.Modules/TimeWarp.Modules.csproj", "Source/Libraries/TimeWarp.Modules/"]
COPY ["Source/TimeWarp.OptionsValidation/TimeWarp.OptionsValidation.csproj", "Source/TimeWarp.OptionsValidation/"]
COPY ["Source/ContainerApps/Web/Web.Infrastructure/Web.Infrastructure.csproj", "Source/ContainerApps/Web/Web.Infrastructure/"]
COPY ["Source/ContainerApps/Web/Web.Application/Web.Application.csproj", "Source/ContainerApps/Web/Web.Application/"]
COPY ["Source/ContainerApps/Web/Web.Domain/Web.Domain.csproj", "Source/ContainerApps/Web/Web.Domain/"]
COPY ["Source/ContainerApps/Web/Web.Shared/Web.Contracts.csproj", "Source/ContainerApps/Web/Web.Shared/"]
COPY ["Source/ContainerApps/Web/Web.Spa/Web.Spa.csproj", "Source/ContainerApps/Web/Web.Spa/"]
COPY ["Source/ContainerApps/Grpc/Grpc.Contracts/Grpc.Contracts.csproj", "Source/ContainerApps/Grpc/Grpc.Contracts/"]
COPY ["Source/ContainerApps/Api/Api.Contracts/Api.Contracts.csproj", "Source/ContainerApps/Api/Api.Contracts/"]
RUN dotnet restore "Source/ContainerApps/Web/Web.Server/Web.Server.csproj"
COPY . .
WORKDIR "/src/Source/ContainerApps/Web/Web.Server"
RUN dotnet build "Web.Server.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Web.Server.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Web.Server.dll"]
