---
uid: TimeWarp.Blazor.Template:Overview
title: TimeWarp Architecture Overview
---

# TimeWarp Architecture

[![NuGet](https://img.shields.io/nuget/v/TimeWarp.AspNetCore.Blazor.Templates.svg)](https://www.nuget.org/packages/TimeWarp.AspNetCore.Blazor.Templates/)
[![NuGet](https://img.shields.io/nuget/dt/TimeWarp.AspNetCore.Blazor.Templates.svg)](https://www.nuget.org/packages/TimeWarp.AspNetCore.Blazor.Templates/)
[![Build Status](https://timewarpenterprises.visualstudio.com/timewarp-templates/_apis/build/status/TimeWarp.Blazor.Template?branchName=master)](https://timewarpenterprises.visualstudio.com/timewarp-templates/_build/latest?definitionId=20&branchName=master)

This is a dotnet net template for creating a Single Page Application(SPA) with Blazor and dotnet 5. The TimeWarp view of clean architecture, much thanks to Jimmy Bogard, Steve Ardalis Smith, Jason Taylor and more.

## Technologies
* Dotnet 5 and Blazor
* [Entity Framework Core 5](https://docs.microsoft.com/en-us/ef/core/)
* [MediatR](https://github.com/jbogard/MediatR)
* [Automapper](https://automapper.org/)
* [FluentValidation](https://fluentvalidation.net/)
* [Fixie](),[Playwright](https://playwright.dev/),[FluentAssertions](https://fluentassertions.com/)



## Installation
1. Install the latest [.NET 5 SDK](https://dotnet.microsoft.com/download/dotnet/5.0)
2. Install the latest [Node.js LTS](https://nodejs.org/en/)
3. Install the latest [Powershell](https://docs.microsoft.com/en-us/powershell/scripting/install/installing-powershell?view=powershell-7.1)
4. Install the latest [Cosmos Db Emulator](https://docs.microsoft.com/en-us/azure/cosmos-db/local-emulator?tabs=ssl-netstd21)
5. Update your powershell profile to include the following <sup><a href="#footnotes">1</a></sup>:
```powershell
$env:PSModulePath += ";$env:ProgramFiles\Azure Cosmos DB Emulator\PSModules"
Import-Module Microsoft.Azure.CosmosDB.Emulator
```
6. Install TimeWarp Templates
```console
dotnet new --install TimeWarp.AspNetCore.Blazor.Templates
```

## Usage

To create new solution enter the following:

```console
dotnet new timewarp-blazor -n MyBlazorApp
```

To run the new solution change to the newly created directory. 

```console
cd .\MyBlazorApp\
```
Execute the `Run.ps1` powershell script

```console
Run.ps1
```

You should see similar console output to the following:

```console
λ  .\Run.ps1
...
info: Microsoft.Hosting.Lifetime[0]
      Now listening on: http://localhost:5000
info: Microsoft.Hosting.Lifetime[0]
      Now listening on: https://localhost:5001
info: Microsoft.Hosting.Lifetime[0]
      Application started. Press Ctrl+C to shut down.
info: Microsoft.Hosting.Lifetime[0]
      Hosting environment: Development
info: Microsoft.Hosting.Lifetime[0]
      Content root path: C:\git\temp\MyBlazorApp\Source\Server
```

Open up your browser to <https://localhost:5001> and confirm you have a running site.

## Content

The template creates four projects which will be deployed, three test projects and one test libary.

### Projects

MyBlazorApp.Client - This is the user interface project . The "Single Page Application (SPA)"  
MyBlazorApp.Server - This is the server project that serves up the SPA and is also the web api.  
MyBlazorApp.Shared - This is a library of common code shared between the Client and Server Projects.  
TimeWarp.Blazor.TypeScript

### Test Projects

MyBlazorApp.Client.Integration.Tests - contains integration tests for the SPA  
MyBlazorApp.Server.Integration.Tests - contains integration tests for the web api  
MyBlazorApp.EndToEnd.Playwright.Tests - contains Playwright based end-to-end tests.

### Test Library
MyBlazorApp.Testing - Share testing library.


# Footnotes:
[1] https://docs.microsoft.com/en-us/azure/cosmos-db/emulator-command-line-parameters#:%7E:text=Install%20the%20latest%20version%20of,Azure.