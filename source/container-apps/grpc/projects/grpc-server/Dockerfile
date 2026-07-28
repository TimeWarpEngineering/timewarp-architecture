ARG dotnet_version=10.0
#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

# https://hub.docker.com/_/microsoft-dotnet
FROM mcr.microsoft.com/dotnet/sdk:$dotnet_version-jammy AS build

WORKDIR /git
COPY ["Directory.Build.props", "."]
COPY ["Directory.Packages.props", "."]
COPY ["source/container-apps/grpc/projects/grpc-contracts/grpc-contracts.csproj", "source/container-apps/grpc/projects/grpc-contracts/"]
COPY ["source/container-apps/grpc/projects/grpc-domain/grpc-domain.csproj", "source/container-apps/grpc/projects/grpc-domain/"]
COPY ["source/container-apps/grpc/projects/grpc-application/grpc-application.csproj", "source/container-apps/grpc/projects/grpc-application/"]
COPY ["source/container-apps/grpc/projects/grpc-infrastructure/grpc-infrastructure.csproj", "source/container-apps/grpc/projects/grpc-infrastructure/"]
COPY ["source/container-apps/grpc/projects/grpc-server/grpc-server.csproj", "source/container-apps/grpc/projects/grpc-server/"]
COPY ["source/foundation/foundation-contracts/foundation-contracts.csproj", "source/foundation/foundation-contracts/"]
COPY ["source/foundation/foundation-application/foundation-application.csproj", "source/foundation/foundation-application/"]
COPY ["source/foundation/foundation-infrastructure/foundation-infrastructure.csproj", "source/foundation/foundation-infrastructure/"]
COPY ["source/foundation/foundation-domain/foundation-domain.csproj", "source/foundation/foundation-domain/"]
COPY ["source/foundation/foundation-server/foundation-server.csproj", "source/foundation/foundation-server/"]
COPY ["source/container-apps/aspire/projects/aspire-service-defaults/aspire-service-defaults.csproj", "source/container-apps/aspire/projects/aspire-service-defaults/"]

# restore nugets
RUN dotnet restore "source/container-apps/grpc/projects/grpc-server/grpc-server.csproj"

COPY ["source/container-apps/grpc/.", "source/container-apps/grpc"]
COPY ["source/foundation/.", "source/foundation"]
COPY ["source/container-apps/aspire/.", "source/container-apps/aspire"]

WORKDIR "/git/source/container-apps/grpc/projects/grpc-server"
RUN dotnet build "grpc-server.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "grpc-server.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:$dotnet_version-jammy AS final
WORKDIR /app
EXPOSE 80
EXPOSE 443
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "grpc-server.dll"]