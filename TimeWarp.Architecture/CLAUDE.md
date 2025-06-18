# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Development Commands

### Running the Application
- `./Run.ps1` - Runs the main Aspire orchestrator (Development environment)
- `./RunDocker.ps1` - Run using Docker containers
- `./RunTailwind.ps1` - Build Tailwind CSS for the Web.Spa project

### Testing
- `./RunTests.ps1` - Runs all test suites using Fixie test framework
- `dotnet fixie Tests/[ProjectName]` - Run specific test project
- `dotnet test Tests/EndToEnd.Playwright.Tests` - Run Playwright end-to-end tests specifically

### Frontend Development (Web.Spa)
- `npm run css:build` - Build Tailwind CSS
- `npm run tailwind-watch` - Watch and rebuild CSS changes
- `npm run build` - Build TypeScript
- `npm run lint` - Lint TypeScript files
- `npm run prettier` - Format code

### Test Filtering (Fixie Framework)
- `dotnet fixie Tests/[ProjectName] --tests [ClassName]` - Run specific test class
- `dotnet fixie Tests/[ProjectName] --tests [ClassName.MethodName]` - Run specific test method
- `dotnet fixie -- --Tag Fast --Tag Smoke` - Run tests with specific tags

## Architecture Overview

This is a **distributed microservices architecture template** using .NET Aspire for orchestration. The solution demonstrates clean architecture patterns with multiple container applications.

### Core Applications
1. **Web.Spa** - Blazor WebAssembly client with TimeWarp State management
2. **Web.Server** - Blazor Server hosting the SPA and API endpoints
3. **Api.Server** - Dedicated API service
4. **Grpc.Server** - gRPC service implementation  
5. **Yarp** - Reverse proxy/API gateway

### Key Architectural Patterns

**Endpoint-Centric API Design**: Each API endpoint has its own dedicated request/response DTOs rather than sharing entity DTOs. This provides maximum flexibility and prevents coupling between endpoints.

**Interface-Based Validation**: Common validation logic is shared through interfaces (mixins) while maintaining endpoint specificity. See `Common.Contracts/Mixins/` for implementations.

**MediatR Pipeline**: Both client and server use MediatR with the "Object In, Object Out" command pattern. Features are organized by aggregate/state.

**TimeWarp State Management**: Custom Blazor state management system similar to Redux, with development tools integration.

**Source Generation**: Custom Roslyn analyzers and source generators create FastEndpoint implementations from attributes.

**AssemblyMarker Pattern**: Each assembly contains a sealed `AssemblyMarker` class for consistent assembly identification and reflection operations.

### Project Structure

```
Source/
├── Common/                    # Shared libraries
│   ├── Common.Contracts/      # API contracts, DTOs, validation
│   ├── Common.Application/    # Application services  
│   ├── Common.Domain/        # Domain entities and value objects
│   ├── Common.Infrastructure/ # Infrastructure implementations
│   └── Common.Server/        # Server-side base classes
├── ContainerApps/            # Microservices
│   ├── Web/                  # Blazor application (client + server)
│   ├── Api/                  # Dedicated API service
│   ├── Grpc/                 # gRPC service
│   ├── Aspire/               # Service orchestration
│   └── Yarp/                 # API Gateway
├── Analyzers/                # Roslyn analyzers and source generators
└── Libraries/                # Additional utility libraries

Tests/                        # All test projects using Fixie framework
```

### Feature Organization

Features follow a consistent structure across all projects:

```
Features/[FeatureName]/
├── [FeatureName].cs          # Request/Response DTOs
├── [FeatureName].Handler.cs  # MediatR handler
├── [FeatureName]Endpoint.cs  # FastEndpoint (server-side)
└── [FeatureName]State.cs     # TimeWarp state (client-side)
```

### Key Technologies

- **.NET 9** with nullable reference types enabled
- **Blazor WebAssembly/Server** for frontend
- **MediatR** for CQRS/command handling
- **FastEndpoints** for minimal API endpoints
- **Fixie** for testing (not MSTest/xUnit)
- **TimeWarp State** for client state management
- **Tailwind CSS** for styling
- **TypeScript** for client-side scripting
- **Aspire** for service orchestration
- **Entity Framework Core** for data access

### Mixins and Code Generation

The solution uses a mixin system for code reuse:
- `.mixin` files in `Common.Contracts/Mixins/` define reusable code patterns
- Mixins are applied to classes to add common functionality like validation
- Source generators create FastEndpoint implementations from `[ApiEndpoint]` attributes

## Task Management Workflow

This project uses a **Kanban folder-based workflow** for task management:

### Kanban Structure
- `Kanban/Backlog/` - Tasks not ready (B### prefix, e.g., `B001_task-name.md`)
- `Kanban/ToDo/` - Tasks ready to work (### prefix, e.g., `001_task-name.md`) 
- `Kanban/InProgress/` - Currently being worked on
- `Kanban/Done/` - Completed tasks

### Task Workflow
1. Create tasks in Backlog with `B###_description.md` naming
2. Move to ToDo when ready, renaming to `###_description.md`
3. Move through InProgress → Done as work progresses
4. Update Implementation Notes section during development

### Definition of Done Requirements

**API Endpoints require:**
- Server: Endpoint, Handler, Validator, Mapper
- Contracts: Request, Response, RequestValidator  
- Integration tests for Handler and Endpoint
- Documentation for Request/Response classes

**Client Features require:**
- State, Actions, Components/Pages
- Integration tests for State (ShouldClone, ShouldSerialize)
- Action tests (at least positive cases)
- End-to-end tests for page rendering and happy paths

## API Contract Development Guidelines

### BFF (Backend-for-Frontend) Contract Structure
- **Location**: `Features/[PluralizedFeatureName]/` (e.g., `Features/Users/`)
- **Commands Folder**: Write operations (Create, Update, Delete)
- **Queries Folder**: Read operations (prefixed with "Get")
- **Namespace**: `[ProjectName].Features.[PluralizedFeatureName]`

### Contract File Structure
```csharp
public static partial class GetUser  // CRUD operation prefix
{
    public sealed partial class Query : IRequest<OneOf<Response, SharedProblemDetails>> { }
    public sealed class Response : IUserDetails { }
    public sealed class Validator : AbstractValidator<Query> { }
}
```

### Entity Class Organization (ADR-001)
Use these regions in entity classes to organize associations:
```csharp
#region Composite-Associations (Filled Diamond)
// Strong "whole-part" relationships
#endregion

#region Aggregate-Associations (Unfilled Diamond)  
// Entity owned by one of several possible aggregates
#endregion

#region Normal-Associations (No Diamond)
// Standard relationships without ownership
#endregion
```

## UI Development Guidelines

### FluentUI + Tailwind CSS Strategy
- **Layout**: Use `TimeWarpPage` component with FluentUI's `Stack` and `Grid`
- **Colors**: Use FluentUI color palette only (supports automatic light/dark themes)
- **Tailwind Usage**: Limit to spacing (`m-*`, `p-*`), hover effects, and responsiveness
- **Avoid**: Tailwind color, typography, border, and shadow classes that conflict with FluentUI

### Development Notes

- All projects use `TimeWarp.Architecture` as the root namespace
- Feature constants are defined via preprocessor directives: `cosmosdb`, `api`, `grpc`, `web`, `yarp`
- Generated code is excluded via `Directory.Build.targets`
- Tests use the Fixie framework, not MSTest or xUnit
- The Aspire orchestrator starts all services for local development
- Client state uses TimeWarp patterns with Redux DevTools integration
- Each assembly must contain a sealed `AssemblyMarker` class for reflection operations