# Timewarp-Templates

## timewarp-blazor

[![NuGet](https://img.shields.io/nuget/v/TimeWarp.Architecture.svg)](https://www.nuget.org/packages/TimeWarp.Architecture/)
[![NuGet](https://img.shields.io/nuget/dt/TimeWarp.Architecture.svg)](https://www.nuget.org/packages/TimeWarp.Architecture/)

### Documentation

https://timewarpengineering.github.io/timewarp-templates/TimeWarpBlazorTemplate/Overview.html

### Installation

```console
dotnet new --install TimeWarp.AspNetCore.Blazor.Templates
```

### Usage

```console
dotnet new timewarp-blazor -n MyBlazorApp
```

## Content

The template creates 3 projects which will be deployed and 4 test projects.

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

Spa.Integration.Tests - contains integration tests for the SPA
Server.Integration.Tests - contains integration tests for the web api
EndToEnd.Playwright.Tests - contains TestCafe based end-to-end tests.
TimeWarp.Testing - a shared library used for testing.
EndToEnd.TestCafe.Tests - contains TestCafe based end-to-end tests.

## timewarp-console

[![NuGet](https://img.shields.io/nuget/v/TimeWarp.Console.Template.svg)](https://www.nuget.org/packages/TimeWarp.Console.Template)
[![NuGet](https://img.shields.io/nuget/dt/TimeWarp.Console.Template.svg)](https://www.nuget.org/packages/TimeWarp.Console.Template)
[![Build Status](https://timewarpenterprises.visualstudio.com/timewarp-console/_apis/build/status/TimeWarpEngineering.timewarp-console?branchName=master)](https://timewarpenterprises.visualstudio.com/timewarp-console/_build/latest?definitionId=19&branchName=master)

Console template for dotnet core applications utilizing MediatR 

### Documentation

https://timewarpengineering.github.io/timewarp-templates/TimeWarpConsoleTemplate/Overview.html
