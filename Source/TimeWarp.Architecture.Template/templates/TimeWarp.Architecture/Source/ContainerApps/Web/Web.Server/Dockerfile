ARG node_version=18.2.0
ARG dotnet_version=6.0
#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM node:$node_version-alpine AS node

# https://hub.docker.com/_/microsoft-dotnet
FROM mcr.microsoft.com/dotnet/sdk:$dotnet_version-alpine AS build
ENV NODE_OPTIONS=--openssl-legacy-provider

# add node packages from their image
COPY --from=node /usr/lib /usr/lib
COPY --from=node /usr/local/share /usr/local/share
COPY --from=node /usr/local/lib /usr/local/lib
COPY --from=node /usr/local/include /usr/local/include
COPY --from=node /usr/local/bin /usr/local/bin

RUN node --version

# Copy project files
WORKDIR /git
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
COPY ["Source/Libraries/TimeWarp.OptionsValidation/TimeWarp.OptionsValidation.csproj", "Source/Libraries/TimeWarp.OptionsValidation/"]
COPY ["Source/ContainerApps/Web/Web.Infrastructure/Web.Infrastructure.csproj", "Source/ContainerApps/Web/Web.Infrastructure/"]
COPY ["Source/ContainerApps/Web/Web.Application/Web.Application.csproj", "Source/ContainerApps/Web/Web.Application/"]
COPY ["Source/ContainerApps/Web/Web.Domain/Web.Domain.csproj", "Source/ContainerApps/Web/Web.Domain/"]
COPY ["Source/ContainerApps/Web/Web.Shared/Web.Contracts.csproj", "Source/ContainerApps/Web/Web.Shared/"]
COPY ["Source/ContainerApps/Web/Web.Spa/Web.Spa.csproj", "Source/ContainerApps/Web/Web.Spa/"]
COPY ["Source/ContainerApps/Grpc/Grpc.Contracts/Grpc.Contracts.csproj", "Source/ContainerApps/Grpc/Grpc.Contracts/"]
COPY ["Source/ContainerApps/Api/Api.Contracts/Api.Contracts.csproj", "Source/ContainerApps/Api/Api.Contracts/"]

# restore nugets
RUN dotnet restore "Source/ContainerApps/Web/Web.Server/Web.Server.csproj"

# do npm install
COPY ["Source/ContainerApps/Web/Web.TypeScript/package.json", "Source/ContainerApps/Web/Web.TypeScript/package.json"]
COPY ["Source/ContainerApps/Web/Web.TypeScript/package-lock.json", "Source/ContainerApps/Web/Web.TypeScript/package-lock.json"]
WORKDIR /git/Source/ContainerApps/Web/Web.TypeScript/

RUN npm install

# Copy remaining source
WORKDIR /git
COPY ["Source/ContainerApps/Web/.", "Source/ContainerApps/Web"]
COPY ["Source/ContainerApps/Grpc/Grpc.Contracts/.", "Source/ContainerApps/Grpc/Grpc.Contracts"]
COPY ["Source/ContainerApps/Api/Api.Contracts/.", "Source/ContainerApps/Api/Api.Contracts"]
COPY ["Source/Common/.", "Source/Common"]
COPY ["Source/Libraries/.", "Source/Libraries"]

WORKDIR "/git/Source/ContainerApps/Web/Web.Server"
RUN dotnet build "Web.Server.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Web.Server.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:$dotnet_version-alpine AS final
WORKDIR /app
EXPOSE 80
EXPOSE 443
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Web.Server.dll"]
