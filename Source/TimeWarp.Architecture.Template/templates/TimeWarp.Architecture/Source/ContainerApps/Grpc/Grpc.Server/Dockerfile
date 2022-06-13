ARG dotnet_version=6.0
#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

# https://hub.docker.com/_/microsoft-dotnet
FROM mcr.microsoft.com/dotnet/sdk:$dotnet_version-alpine AS build

WORKDIR /git
COPY ["NuGet.config", "."]
COPY ["Directory.Build.props", "."]
COPY ["Directory.Packages.props", "."]
COPY ["Source/ContainerApps/Grpc/Grpc.Server/Grpc.Server.csproj", "Source/ContainerApps/Grpc/Grpc.Server/"]
COPY ["Source/Common/Common.Server/Common.Server.csproj", "Source/Common/Common.Server/"]
COPY ["Source/Common/Common.Infrastructure/Common.Infrastructure.csproj", "Source/Common/Common.Infrastructure/"]
COPY ["Source/Common/Common.Application/Common.Application.csproj", "Source/Common/Common.Application/"]
COPY ["Source/Common/Common.Domain/Common.Domain.csproj", "Source/Common/Common.Domain/"]
COPY ["Source/Common/Common.Contracts/Common.Contracts.csproj", "Source/Common/Common.Contracts/"]
COPY ["Source/Libraries/TimeWarp.Modules/TimeWarp.Modules.csproj", "Source/Libraries/TimeWarp.Modules/"]
COPY ["Source/Libraries/TimeWarp.OptionsValidation/TimeWarp.OptionsValidation.csproj", "Source/Libraries/TimeWarp.OptionsValidation/"]
COPY ["Source/ContainerApps/Grpc/Grpc.Infrastructure/Grpc.Infrastructure.csproj", "Source/ContainerApps/Grpc/Grpc.Infrastructure/"]
COPY ["Source/ContainerApps/Grpc/Grpc.Application/Grpc.Application.csproj", "Source/ContainerApps/Grpc/Grpc.Application/"]
COPY ["Source/ContainerApps/Grpc/Grpc.Domain/Grpc.Domain.csproj", "Source/ContainerApps/Grpc/Grpc.Domain/"]
COPY ["Source/ContainerApps/Grpc/Grpc.Contracts/Grpc.Contracts.csproj", "Source/ContainerApps/Grpc/Grpc.Contracts/"]

# restore nugets
RUN dotnet restore "Source/ContainerApps/Grpc/Grpc.Server/Grpc.Server.csproj"

COPY ["/Source/ContainerApps/Grpc/.", "Source/ContainerApps/Grpc"]
COPY ["/Source/Common/.", "Source/Common"]
COPY ["/Source/Libraries/.", "Source/Libraries"]

WORKDIR "/git/Source/ContainerApps/Grpc/Grpc.Server"
RUN pwd
RUN ls
RUN dotnet build "Grpc.Server.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Grpc.Server.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:$dotnet_version-alpine AS final
WORKDIR /app
EXPOSE 80
EXPOSE 443
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Grpc.Server.dll"]
