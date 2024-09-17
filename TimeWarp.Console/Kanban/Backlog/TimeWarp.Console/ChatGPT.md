# Goal: Create a .NET Console App Template with Dependency Injection, Hosting, and System.CommandLine

## Overview

The aim of this project is to develop a custom .NET template for a console application that integrates the following features:

- **Dependency Injection (DI):** Utilize the built-in DI container provided by `Microsoft.Extensions.DependencyInjection`.
- **.NET Generic Host:** Leverage the .NET hosting model to manage application lifetime, configuration, and logging.
- **System.CommandLine:** Incorporate command-line parsing capabilities to handle user inputs and commands.

By creating this template, developers can quickly scaffold new console applications that follow best practices and have a consistent structure.

## Objectives

- **Custom Template Creation:** Develop a .NET template that can be installed and used via the `dotnet new` command.
- **Dependency Injection Setup:** Configure DI to manage application services and dependencies.
- **Hosting Model Integration:** Use the .NET Generic Host to control application startup, shutdown, and lifetime events.
- **Command-Line Parsing:** Implement command-line parsing using the `System.CommandLine` library to handle arguments and commands.
- **Documentation:** Provide clear instructions on how to use the template, including installation and customization.

## Benefits

- **Consistent Structure:** Ensures new console applications have a standardized setup.
- **Productivity:** Reduces the time needed to set up boilerplate code for DI, hosting, and command-line parsing.
- **Best Practices:** Encourages the use of modern .NET practices in console applications.

## Deliverables

- **Template Files:** All necessary files for the .NET template.
- **Installation Guide:** Steps to install the template locally or globally.
- **Usage Guide:** Instructions on how to create a new project using the template and how to extend it.
- **Sample Application:** An example console app generated from the template demonstrating the integrated features.

## Tools and Technologies

- **.NET SDK:** .NET 6.0 or later.
- **Microsoft.Extensions.DependencyInjection:** For setting up DI.
- **Microsoft.Extensions.Hosting:** To use the Generic Host.
- **System.CommandLine:** For command-line parsing.

## Steps to Achieve the Goal

1. **Set Up Project Structure:**
    - Create a new console application project that will serve as the template.
2. **Integrate Dependency Injection:**
    - Configure services in a `ConfigureServices` method.
    - Use constructor injection to inject dependencies.
3. **Implement the Generic Host:**
    - Set up a `HostBuilder` in the `Program.cs` file.
    - Configure logging and configuration providers if necessary.
4. **Add System.CommandLine:**
    - Install the `System.CommandLine` NuGet package.
    - Define commands and options.
    - Parse and handle command-line arguments.
5. **Create Template Configuration:**
    - Add a `.template.config` folder with a `template.json` file.
    - Define template parameters and symbols.
6. **Test the Template:**
    - Install the template locally using `dotnet new --install`.
    - Create a new project using the template and ensure all features work as expected.
7. **Document the Template:**
    - Write clear installation and usage instructions.
    - Provide examples and code snippets.

## Timeline

- **Week 1:** Set up the initial project and integrate DI and hosting.
- **Week 2:** Add command-line parsing and finalize the template structure.
- **Week 3:** Test the template thoroughly and make necessary adjustments.
- **Week 4:** Prepare documentation and sample applications.

## References

- [.NET Generic Host Documentation](https://docs.microsoft.com/en-us/dotnet/core/extensions/generic-host)
- [Dependency Injection in .NET](https://docs.microsoft.com/en-us/dotnet/core/extensions/dependency-injection)
- [System.CommandLine Repository](https://github.com/dotnet/command-line-api)
- [Creating Custom Templates for dotnet new](https://docs.microsoft.com/en-us/dotnet/core/tools/custom-templates)

---

This document outlines the goal and steps required to create a robust .NET console app template that integrates dependency injection, the hosting model, and command-line parsing using `System.CommandLine`.