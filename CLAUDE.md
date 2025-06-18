# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Repository Structure

This is a **multi-project repository** containing .NET project templates and supporting infrastructure:

### Main Projects
- **TimeWarp.Architecture/** - Complete distributed microservices architecture template
- **TimeWarp.Console/** - Console application template  
- **TimeWarp.Templates/** - Template packaging and documentation

## Development Commands

### TimeWarp.Architecture (Main Template)
Navigate to `TimeWarp.Architecture/` directory first:

**Running Applications:**
- `./Run.ps1` - Runs the Aspire orchestrator (Development environment)
- `./RunDocker.ps1` - Run using Docker containers
- `./RunTailwind.ps1` - Build Tailwind CSS for Web.Spa

**Testing:**
- `./RunTests.ps1` - Runs all test suites using Fixie framework
- `dotnet fixie Tests/[ProjectName]` - Run specific test project
- `dotnet fixie Tests/[ProjectName] --tests [ClassName]` - Run specific test class
- `dotnet fixie Tests/[ProjectName] --tests [ClassName.MethodName]` - Run specific test method
- `dotnet test Tests/EndToEnd.Playwright.Tests` - Run Playwright E2E tests

**Frontend Development (Web.Spa):**
- `npm run css:build` - Build Tailwind CSS
- `npm run tailwind-watch` - Watch and rebuild CSS changes  
- `npm run build` - Build TypeScript
- `npm run lint` - Lint TypeScript files
- `npm run prettier` - Format code

### Template Management
- `dotnet new --install TimeWarp.Architecture` - Install the architecture template
- `dotnet new timewarp-architecture -n MyApp` - Create new project from template

## Architecture Overview

### TimeWarp.Architecture Template
A **distributed microservices architecture** demonstrating enterprise .NET patterns:

**Core Technologies:**
- **.NET 9** with C# latest features
- **Blazor WebAssembly/Server** with TimeWarp State management
- **MediatR** for CQRS patterns ("Object In, Object Out")
- **FastEndpoints** for minimal APIs (instead of controllers)
- **Fixie** testing framework (NOT MSTest/xUnit)
- **.NET Aspire** for service orchestration
- **Entity Framework Core** with multiple database providers
- **FluentUI + Tailwind CSS** for styling

**Key Patterns:**
- **Endpoint-Centric Design**: Each API endpoint has dedicated request/response DTOs
- **Mixin System**: Code reuse through `.mixin` files in `Common.Contracts/Mixins/`
- **Source Generation**: Custom Roslyn analyzers create FastEndpoint implementations
- **AssemblyMarker Pattern**: Each assembly has sealed `AssemblyMarker` class

### Container Applications Structure
```
ContainerApps/
├── Web/           # Blazor client + server
├── Api/           # Dedicated API service
├── Grpc/          # gRPC service
├── Aspire/        # Service orchestration
└── Yarp/          # API Gateway/Reverse Proxy
```

## Feature Development Guidelines

### API Contract Structure
- **Location**: `Features/[PluralizedFeatureName]/`
- **Commands**: Write operations (Create, Update, Delete)
- **Queries**: Read operations (prefixed with "Get")
- **Validation**: FluentValidation with shared mixins

### UI Development (FluentUI + Tailwind)
- **Layout**: Use `TimeWarpPage` with FluentUI `Stack`/`Grid`
- **Colors**: FluentUI palette only (automatic light/dark theme support)
- **Tailwind**: Limited to spacing (`m-*`, `p-*`), hover effects, responsiveness
- **Avoid**: Tailwind colors, typography, borders that conflict with FluentUI

## Task Management Workflow

Uses **Kanban folder-based system** in `Kanban/` directory:

### Workflow Structure
- `Backlog/` - Tasks not ready (B### prefix)
- `ToDo/` - Ready tasks (### prefix)
- `InProgress/` - Active development
- `Done/` - Completed tasks

### Definition of Done
**API Endpoints:**
- Server: Endpoint, Handler, Validator, Mapper
- Contracts: Request, Response, RequestValidator
- Integration tests for Handler and Endpoint

**Client Features:**
- State, Actions, Components/Pages
- Integration tests (ShouldClone, ShouldSerialize)
- Action tests and E2E tests

## Testing Framework

**Primary Framework**: Fixie (not MSTest/xUnit)

### Test Organization
```
Tests/
├── Analyzers/         # Source generator tests
├── Common/           # Shared library tests  
├── ContainerApps/    # Microservice tests
├── EndToEnd.Playwright.Tests/  # E2E browser tests
├── Libraries/        # Custom library tests
└── Web.*.Integration.Tests/    # Integration tests
```

### Test Commands
- End-to-end tests use `dotnet test` (Playwright)
- All other tests use `dotnet fixie` (Fixie framework)
- Filter by tags: `dotnet fixie -- --Tag Fast --Tag Smoke`

## Important Development Notes

- **Namespace**: All projects use `TimeWarp.Architecture` as root namespace
- **Nullable Reference Types**: Disabled by design choice  
- **Generated Code**: Excluded via `Directory.Build.targets`
- **Feature Flags**: Via preprocessor directives (`cosmosdb`, `api`, `grpc`, `web`, `yarp`)
- **State Management**: TimeWarp patterns with Redux DevTools integration
- **Assembly Markers**: Each assembly must contain sealed `AssemblyMarker` class

## Documentation

Comprehensive documentation available in:
- `TimeWarp.Architecture/Documentation/` - Architecture and development guides
- Online docs: https://timewarpengineering.github.io/timewarp-architecture/