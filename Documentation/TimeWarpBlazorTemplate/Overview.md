---
uid: TimeWarp.Blazor.Template:Overview
title: TimeWarp Blazor Template Overview
---

# TimeWarp-Blazor Template

[![NuGet](https://img.shields.io/nuget/v/TimeWarp.AspNetCore.Blazor.Templates.svg)](https://www.nuget.org/packages/TimeWarp.AspNetCore.Blazor.Templates/)
[![NuGet](https://img.shields.io/nuget/dt/TimeWarp.AspNetCore.Blazor.Templates.svg)](https://www.nuget.org/packages/TimeWarp.AspNetCore.Blazor.Templates/)
[![Build Status](https://timewarpenterprises.visualstudio.com/timewarp-templates/_apis/build/status/TimeWarp.Blazor.Template?branchName=master)](https://timewarpenterprises.visualstudio.com/timewarp-templates/_build/latest?definitionId=20&branchName=master)

## Installation

```console
dotnet new --install TimeWarp.AspNetCore.Blazor.Templates::1.0.0-3.0.100-preview9-014004-104
```

## Usage

To create new solution enter the following:

```console
dotnet new timewarp-blazor -n MyBlazorApp
```

To run the new solution change to the directory that contains the startup project.  In our template the startup project is the server project.

```console
cd .\MyBlazorApp\Source\Server\
dotnet run
```

You should see similar console output to the following:

```console
λ  dotnet run
info: Microsoft.Hosting.Lifetime[0]
      Now listening on: http://localhost:5000
info: Microsoft.Hosting.Lifetime[0]
      Now listening on: https://localhost:5001
info: Microsoft.Hosting.Lifetime[0]
      Application started. Press Ctrl+C to shut down.
info: Microsoft.Hosting.Lifetime[0]
      Hosting environment: Production
info: Microsoft.Hosting.Lifetime[0]
      Content root path: C:\git\temp\MyBlazorApp\Source\Server
```

Open up your browser to <https://localhost:5001> and confirm you have running site.

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
