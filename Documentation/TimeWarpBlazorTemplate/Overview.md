---
uid: TimeWarp.Blazor.Template:Overview
title: TimeWarp Architecture Overview
---

# TimeWarp Architecture

[![NuGet](https://img.shields.io/nuget/v/TimeWarp.Architecture.svg)](https://www.nuget.org/packages/TimeWarp.Architecture/)
[![NuGet](https://img.shields.io/nuget/dt/TimeWarp.Architecture.svg)](https://www.nuget.org/packages/TimeWarp.Architecture/)

This is a dotnet net 6 template for creating a TimeWarp Architecture application. Based on ideas from 

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

## Prerequisites

* Install the latest [.NET 6 SDK](https://dotnet.microsoft.com/download/dotnet/6.0)
* Install the latest [Node.js LTS](https://nodejs.org/en/)
* Install the latest [Powershell](https://docs.microsoft.com/en-us/powershell/scripting/install/installing-powershell)
* Install the latest [Cosmos Db Emulator](https://docs.microsoft.com/en-us/azure/cosmos-db/local-emulator?tabs=ssl-netstd21)
* Update your powershell profile to include the following <sup><a href="#footnotes">1</a></sup>:
```powershell
$env:PSModulePath += ";$env:ProgramFiles\Azure Cosmos DB Emulator\PSModules"
Import-Module Microsoft.Azure.CosmosDB.Emulator
```
* Install Tye dotnet tool 
```console
dotnet tool install -g Microsoft.Tye --version "0.11.0-alpha.22111.1"
```
## Installation

* Install TimeWarp Templates
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
Run.ps1
```

You should see the Tye Dashboard opened in your browser.

## Content

The template creates four projects which will be deployed, three test projects and one test libary.

### Projects

MyTimeWarpApp.Client - This is the user interface project . The "Single Page Application (SPA)"  
MyTimeWarpApp.Server - This is the server project that serves up the SPA and is also the web api.  
MyTimeWarpApp.Shared - This is a library of common code shared between the Client and Server Projects.  
TimeWarp.Blazor.TypeScript

### Test Projects

MyTimeWarpApp.Client.Integration.Tests - contains integration tests for the SPA  
MyTimeWarpApp.Server.Integration.Tests - contains integration tests for the web api  
MyTimeWarpApp.EndToEnd.Playwright.Tests - contains Playwright based end-to-end tests.

### Test Library
MyTimeWarpApp.Testing - Share testing library.


# Footnotes:
[1] https://docs.microsoft.com/en-us/azure/cosmos-db/emulator-command-line-parameters#:%7E:text=Install%20the%20latest%20version%20of,Azure.