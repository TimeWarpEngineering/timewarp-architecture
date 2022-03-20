ARG node_version=17.4.0
ARG dotnet_version=5.0

# https://hub.docker.com/_/microsoft-dotnet
FROM node:$node_version-alpine AS node
FROM mcr.microsoft.com/dotnet/sdk:$dotnet_version-alpine AS build
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
COPY Source/Server/*.csproj ./Source/Server/
COPY Source/Client/*.csproj ./Source/Client/
COPY Source/Shared/*.csproj ./Source/Shared/
COPY Source/SourceCodeGenerators/*.csproj ./Source/SourceCodeGenerators/
COPY Source/TypeScript/*.csproj ./Source/TypeScript/

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
FROM mcr.microsoft.com/dotnet/runtime-deps:5.0-alpine
WORKDIR /app
COPY --from=build /app .
RUN ls -l /app

ENV ASPNETCORE_ENVIRONMENT Development
ENTRYPOINT ["./TimeWarp.Architecture.Server"]
EXPOSE 80 443
