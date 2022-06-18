ARG node_version=18.2.0
ARG dotnet_version=6.0

# https://hub.docker.com/_/microsoft-dotnet
FROM node:$node_version-alpine AS node
FROM mcr.microsoft.com/dotnet/sdk:$dotnet_version-jammy AS build
ENV NODE_OPTIONS=--openssl-legacy-provider

# add node packages from their image
COPY --from=node /usr/lib /usr/lib
COPY --from=node /usr/local/share /usr/local/share
COPY --from=node /usr/local/lib /usr/local/lib
COPY --from=node /usr/local/include /usr/local/include
COPY --from=node /usr/local/bin /usr/local/bin

RUN node --version

# copy global files and sln
WORKDIR /git
COPY Directory.Build.props ./
COPY Directory.Packages.props ./
COPY global.json ./ 
COPY NuGet.config ./
COPY TimeWarp.Architecture.sln ./

# Copy the main source project files
COPY Source/ContainerApps/Web/Web.Server/*.csproj ./Source/Source/ContainerApps/Web/Web.Server/
COPY Source/ContainerApps/Web/Web.Spa/*.csproj ./Source/ContainerApps/Web/Web.Spa/
COPY Source/ContainerApps/Web/Web.Shared/*.csproj ./Source/ContainerApps/Web/Web.Shared/
COPY Source/ContainerApps/Web/TypeScript/*.csproj ./Source/ContainerApps/Web/TypeScript
COPY Source/SourceCodeGenerators/*.csproj ./Source/SourceCodeGenerators/

# restore nugets for alpine
RUN dotnet restore ./Source/Server -r linux-musl-x64

# do npm install
WORKDIR /git/Source/TypeScript
COPY package.json .
COPY package-lock.json .
RUN npm install

# copy everything else and build app
COPY Source/. ./Source/
WORKDIR /git/Source/Server
RUN ls
RUN dotnet publish -c release -o /app --runtime linux-musl-x64 --self-contained true --no-restore

# final stage/image
# FROM mcr.microsoft.com/dotnet/aspnet:5.0-alpine-amd64
FROM mcr.microsoft.com/dotnet/runtime-deps:$dotnet_version-jammy
WORKDIR /app
COPY --from=build /app .
RUN ls -l /app

ENV ASPNETCORE_ENVIRONMENT Development
ENTRYPOINT ["./Web.Server"]
EXPOSE 80 443
