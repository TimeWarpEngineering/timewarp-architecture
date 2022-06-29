---
uid: TimeWarp.Architecture.Template:Overview
title: TimeWarp Architecture
---

# TimeWarp Architecture

[![NuGet](https://img.shields.io/nuget/v/TimeWarp.Architecture.svg)](https://www.nuget.org/packages/TimeWarp.Architecture/)
[![NuGet](https://img.shields.io/nuget/dt/TimeWarp.Architecture.svg)](https://www.nuget.org/packages/TimeWarp.Architecture/)

TimeWarp Architecture is a dotnet net 6 template for creating a distributed or monolithic application.

## Prerequisites

* Install the **latest** [Powershell](https://docs.microsoft.com/en-us/powershell/scripting/install/installing-powershell)
  > `winget install  Microsoft.PowerShell`
* Install the latest released [.NET 6 SDK](https://dotnet.microsoft.com/download/dotnet/6.0)
  > trust the developer cert with  
  > `dotnet dev-certs https --trust`

* Install the latest **LTS** of [Node.js ](https://nodejs.org/en/)
* Install the latest [Cosmos Db Emulator](https://docs.microsoft.com/en-us/azure/cosmos-db/local-emulator?tabs=ssl-netstd21)
* Edit your powershell profile (`code $Profile`) to include the following [^1] :

```powershell
$env:PSModulePath += ";$env:ProgramFiles\Azure Cosmos DB Emulator\PSModules"
Import-Module Microsoft.Azure.CosmosDB.Emulator
```

* Install Tye dotnet tool

```console
dotnet tool install -g Microsoft.Tye --version "0.11.0-alpha.22111.1"
```
> [!CAUTION]
> Currently Project Tye will use the latest installed SDK regardless of the `global.json` settings when building the projects.
> It will not work with Visual Studio 2022 preview 3,4 or 5. installed.
## Installation

* Install TimeWarp Architecture Templates
  
```console
dotnet new --install TimeWarp.Architecture
```

## Usage

To create new solution enter the following:

```console
dotnet new timewarp-architecture -n MyTimeWarpApp
```

To run the new solution change to the newly created directory.

```console
cd .\MyTimeWarpApp\
```

Execute the `Run.ps1` powershell script

```console
./Run.ps1
```

You should see the Tye Dashboard opened in your browser.

## Technologies

* [Dotnet 6](https://dotnet.microsoft.com/en-us/download/dotnet/6.0)
* [Blazor](https://docs.microsoft.com/en-us/aspnet/core/blazor/?view=aspnetcore-6.0)
* [Tailwind](https://tailwindcss.com/)
* [Entity Framework Core](https://docs.microsoft.com/en-us/ef/core/)
* [MediatR](https://github.com/jbogard/MediatR)
* [Automapper](https://automapper.org/)
* [FluentValidation](https://fluentvalidation.net/)
* [Fixie](https://github.com/fixie/fixie/wiki)
* [FluentAssertions](https://fluentassertions.com/)
* [Playwright](https://playwright.dev/)
* [Project Tye](https://github.com/dotnet/tye)
* [YARP](https://microsoft.github.io/reverse-proxy/)
* [Cosmos DB](https://docs.microsoft.com/en-us/azure/cosmos-db/introduction)

## Content

The template creates the distributed app projects and their corresponding test projects.

### Projects

* Api.Contracts
* Api.Server
* Grpc.Contracts
* Grpc.Server
* Web.Server
* Web.Shared
* Web.Spa - The Blazor Single Page Application (SPA)
* Web.TypeScript - Project that contains any needed TypeScript for Web.Spa
* Yarp
* SourceCodeGenerators

### Test Projects

* Spa.Integration.Tests - contains integration tests for the SPA
* Server.Integration.Tests - contains integration tests for the web api
* EndToEnd.Playwright.Tests - contains TestCafe based end-to-end tests.
* TimeWarp.Testing - a shared library used for testing.
* EndToEnd.TestCafe.Tests - contains TestCafe based end-to-end tests.
* SourceCodeGenerators.Tests

### Test Library

TimeWarp.Testing - Share testing library.

#### Footnotes

[^1]: https://docs.microsoft.com/en-us/azure/cosmos-db/emulator-command-line-parameters#:%7E:text=Install%20the%20latest%20version%20of,Azure.
