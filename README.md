# Timewarp-Templates

## timewarp-blazor

[![NuGet](https://img.shields.io/nuget/v/TimeWarp.AspNetCore.Blazor.Templates.svg)](https://www.nuget.org/packages/TimeWarp.AspNetCore.Blazor.Templates/)
[![NuGet](https://img.shields.io/nuget/dt/TimeWarp.AspNetCore.Blazor.Templates.svg)](https://www.nuget.org/packages/TimeWarp.AspNetCore.Blazor.Templates/)
[![Build Status](https://timewarpenterprises.visualstudio.com/timewarp-templates/_apis/build/status/TimeWarp.Blazor.Template?branchName=master)](https://timewarpenterprises.visualstudio.com/timewarp-templates/_build/latest?definitionId=20&branchName=master)

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

MyBlazorApp.Client - This is the user interface project . The "Single Page Application (SPA)"
MyBlazorApp.Server - This is the server project that serves up the SPA and is also the web api.
MyBlazorApp.Shared - This is a library of common code shared between the Client and Server Projects.

### Test Projects

Client.Integration.Tests - contains integration tests for the SPA
Server.Integration.Tests - contains integration tests for the web api
EndToEnd.Selenium.Tests - contains Selenium based end-to-end tests.
EndToEnd.TestCafe.Tests - contains TestCafe based end-to-end tests.

## timewarp-console

[![NuGet](https://img.shields.io/nuget/v/TimeWarp.Console.Template.svg)](https://www.nuget.org/packages/TimeWarp.Console.Template)
[![NuGet](https://img.shields.io/nuget/dt/TimeWarp.Console.Template.svg)](https://www.nuget.org/packages/TimeWarp.Console.Template)
[![Build Status](https://timewarpenterprises.visualstudio.com/timewarp-console/_apis/build/status/TimeWarpEngineering.timewarp-console?branchName=master)](https://timewarpenterprises.visualstudio.com/timewarp-console/_build/latest?definitionId=19&branchName=master)

Console template for dotnet core applications utilizing MediatR 

### Documentation

https://timewarpengineering.github.io/timewarp-templates/TimeWarpConsoleTemplate/Overview.html
