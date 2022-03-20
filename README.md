# Timewarp-Templates

## timewarp-architecture

[![NuGet](https://img.shields.io/nuget/v/TimeWarp.AspNetCore.Blazor.Templates.svg)](https://www.nuget.org/packages/TimeWarp.AspNetCore.Blazor.Templates/)
[![NuGet](https://img.shields.io/nuget/dt/TimeWarp.AspNetCore.Blazor.Templates.svg)](https://www.nuget.org/packages/TimeWarp.AspNetCore.Blazor.Templates/)
[![Build Status](https://timewarpenterprises.visualstudio.com/timewarp-templates/_apis/build/status/TimeWarp.Blazor.Template?branchName=master)](https://timewarpenterprises.visualstudio.com/timewarp-templates/_build/latest?definitionId=20&branchName=master)

### Documentation

https://timewarpengineering.github.io/timewarp-templates/TimeWarpBlazorTemplate/Overview.html

### Installation

```console
dotnet new --install TimeWarp.Architecture
```

### Usage

```console
dotnet new timewarp-blazor -n MyTimeWarpApp
```

## Content

The template creates  projects which will be deployed and 4 test projects.

### Projects
MyTimeWarpApp.Api.Contracts
MyTimeWarpApp.Api.Server
MyTimeWarpApp.Grpc.Contracts
MyTimeWarpApp.Grpc.Server
MyTimeWarpApp.Web.Server - This is the server project that serves up the SPA and is also the web api.
MyTimeWarpApp.Web.Shared - This is a library of common code shared between the Spa and Server Projects.
MyTimeWarpApp.Yarp
MyTimeWarpApp.Spa - This is the user interface project . The "Single Page Application (SPA)"
MyTimeWarpApp.SourceCodeGenerators

### Test Projects

Client.Integration.Tests - contains integration tests for the SPA
Server.Integration.Tests - contains integration tests for the web api
EndToEnd.Playwright.Tests - contains TestCafe based end-to-end tests.
MyTimeWarpApp.Testing - a shared library amongst all tests
EndToEnd.TestCafe.Tests - contains TestCafe based end-to-end tests.
MyTimeWarpApp.SourceCodeGenerators.Tests

## timewarp-console

[![NuGet](https://img.shields.io/nuget/v/TimeWarp.Console.Template.svg)](https://www.nuget.org/packages/TimeWarp.Console.Template)
[![NuGet](https://img.shields.io/nuget/dt/TimeWarp.Console.Template.svg)](https://www.nuget.org/packages/TimeWarp.Console.Template)
[![Build Status](https://timewarpenterprises.visualstudio.com/timewarp-console/_apis/build/status/TimeWarpEngineering.timewarp-console?branchName=master)](https://timewarpenterprises.visualstudio.com/timewarp-console/_build/latest?definitionId=19&branchName=master)

Console template for dotnet core applications utilizing MediatR 

### Documentation

https://timewarpengineering.github.io/timewarp-templates/TimeWarpConsoleTemplate/Overview.html
