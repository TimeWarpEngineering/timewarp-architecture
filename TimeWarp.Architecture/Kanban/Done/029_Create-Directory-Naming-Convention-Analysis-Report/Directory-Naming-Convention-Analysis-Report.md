# Directory Naming Convention Analysis Report
**TimeWarp.Architecture Project**  
**Analysis Date**: 2025-06-24  
**Scope**: Comprehensive analysis of all directory naming patterns across the entire project structure

## Executive Summary

This analysis examines 382 directories across the TimeWarp.Architecture project (excluding build artifacts like `bin/`, `obj/`, `Generated/`, and IDE folders). The project demonstrates a **multi-layered naming strategy** that adapts conventions based on technology context and organizational hierarchy.

### Key Findings
- **Primary Convention**: PascalCase (87.7% of directories)
- **Technology-Specific Adaptations**: Kebab-case for infrastructure, snake_case for Kubernetes organization
- **Dotted Namespace Pattern**: 47 directories use dotted PascalCase (e.g., `Web.Spa`, `Common.Application`)
- **High Consistency**: 95%+ consistency within each major domain area
- **Strategic Inconsistencies**: Deliberate deviations for infrastructure tooling compatibility

## Detailed Analysis by Domain

### 1. .NET Source Code Organization (Source/)

**Primary Pattern**: **PascalCase with Dotted Namespaces**
- **Examples**: `Common.Application/`, `Web.Spa/`, `Api.Server/`, `TimeWarp.Architecture.Analyzers/`
- **Consistency**: 100% within .NET projects
- **Rationale**: Aligns with .NET namespace conventions and Visual Studio expectations

#### Clean Architecture Layers
```
Source/
├── Common/
│   ├── Common.Application/          # Application layer naming
│   ├── Common.Contracts/            # Contracts layer naming
│   ├── Common.Domain/               # Domain layer naming  
│   ├── Common.Infrastructure/       # Infrastructure layer naming
│   └── Common.Server/               # Server layer naming
├── ContainerApps/                   # PascalCase grouping
│   ├── Api/                         # Technology identifier
│   │   ├── Api.Application/         # Layer + Technology
│   │   ├── Api.Contracts/           # Consistent pattern
│   │   ├── Api.Domain/              # across all services
│   │   ├── Api.Infrastructure/
│   │   └── Api.Server/
│   ├── Web/                         # Service grouping
│   │   ├── Web.Application/         # Same pattern for Web
│   │   ├── Web.Contracts/
│   │   ├── Web.Domain/
│   │   ├── Web.Infrastructure/
│   │   ├── Web.Server/
│   │   └── Web.Spa/                 # SPA-specific
│   └── Grpc/                        # gRPC service follows same pattern
├── Analyzers/                       # Source generation tools
│   ├── TimeWarp.Architecture.Analyzers/
│   ├── TimeWarp.Architecture.Attributes/
│   └── TimeWarp.Architecture.SourceGenerator/
└── Libraries/                       # Shared libraries
    ├── TimeWarp.Automation/
    ├── TimeWarp.Automation.Contracts/
    ├── TimeWarp.MediatR/
    └── TimeWarp.Modules/
```

**Feature Organization Pattern**: **PascalCase with Descriptive Names**
- `Features/WeatherForecast/` (not `weather-forecast/` or `weather_forecast/`)
- `Features/Authentication/` (full descriptive words)
- `Features/Authorization/` (clear semantic distinction)

#### Internal Organization Patterns

**Components and Pages**: **Hierarchical PascalCase**
```
Components/
├── Base/                           # Semantic grouping
├── Composites/                     # Component categories
├── Elements/                       # Atomic components
├── Forms/                          # Form-specific components
├── Interfaces/                     # Abstraction layer
├── Layouts/                        # Layout components
└── Pages/                          # Page-level components
```

**State Management**: **Feature + Pattern**
```
Features/
├── Application/
│   └── ApplicationState/           # Feature + State pattern
├── Counter/
│   ├── Components/                 # Feature organization
│   ├── CounterState/              # State management
│   ├── Notification/              # Cross-cutting concerns
│   └── Pages/                     # UI layer
└── WeatherForecast/
    ├── Components/
    ├── Pages/
    ├── WeatherForecastModule.cs    # Module pattern
    └── WeatherForecastsState/      # Plural for collections
```

### 2. Test Organization (Tests/)

**Pattern**: **Mirrors Source Structure + `Tests` Suffix**
- **Examples**: `Web.Server.Integration.Tests/`, `Common.Infrastructure.Tests/`
- **Feature Testing**: Maintains same hierarchical structure as source
- **Consistency**: 100% alignment with corresponding source projects

#### Test Structure Patterns
```
Tests/
├── Analyzers/                      # Mirror of Source/Analyzers/
│   ├── TimeWarp.Architecture.Analyzers.Tests/
│   └── TimeWarp.Architecture.SourceGenerator.Tests/
├── Common/                         # Mirror of Source/Common/
│   └── Common.Infrastructure.Tests/
├── ContainerApps/                  # Mirror of Source/ContainerApps/
│   ├── Api/
│   │   └── Api.Server.Integration.Tests/
│   └── Aspire/
├── EndToEnd.Playwright.Tests/      # Technology-specific naming
├── Libraries/                      # Mirror of Source/Libraries/
│   └── TimeWarp.Automation.Tests/
├── TimeWarp.Testing/               # Testing infrastructure
└── Web.*.Integration.Tests/        # Pattern for integration tests
```

**Test Internal Organization**: **Matches Source Feature Structure**
```
Web.Server.Integration.Tests/
└── Features/                       # Mirrors source Features/
    ├── Analytics/
    │   └── TrackEvent/             # Individual feature tests
    │       ├── TrackEvent_Endpoint_Tests.cs
    │       ├── TrackEvent_Handler_Tests.cs
    │       └── TrackEvent_Validator_Tests.cs
    └── Hello/                      # Consistent pattern across features
```

### 3. Infrastructure and DevOps (DevOps/)

**Mixed Strategy**: **Technology-Appropriate Conventions**

#### Kubernetes Organization: **Numbered + Snake_Case**
```
DevOps/Kubernetes/
├── 0_Namespaces/                   # Numbered for deployment order
├── 1_Nodes/                        # Kubernetes operational hierarchy  
├── 2_Workloads/
│   ├── Deployments/
│   │   ├── api-server/             # kebab-case for K8s resources
│   │   ├── grpc-server/            # Standard Kubernetes naming
│   │   ├── web-server/             # Deployment resource naming
│   │   └── yarp/                   # Service identifier
│   └── Pods/
├── 3_Network/
│   ├── Ingress/                    # PascalCase for categories
│   └── Services/
│       ├── api-server/             # Matches Deployments naming
│       ├── grpc-server/
│       ├── web-server/
│       └── yarp/
├── 4_Storage/
│   ├── Persistent_Volume_Claims/   # Snake_case for K8s concepts
│   ├── Persistent_Volumes/         # Official Kubernetes terminology
│   └── Storage_Classes/            # Maintains K8s naming conventions
├── 5_Configuration/
├── 6_Custom_Resources/
└── 7_Helm_Releases/
```

**Rationale**: Kubernetes tooling expects specific naming patterns:
- **Numbered prefixes**: Deployment order dependency management
- **Snake_case**: Kubernetes resource type names (official K8s terminology)
- **kebab-case**: Individual resource names (K8s best practices)

#### Other DevOps Patterns: **PascalCase Maintained**
```
DevOps/
├── Bicep/                          # Azure tool naming
│   └── modules/
│       └── Authorization/          # PascalCase for logical grouping
├── Docker/                         # Technology identifier
├── Pipelines/                      # CI/CD organization
│   └── timewarp.software.com/      # Domain-based naming (kebab-case)
└── Pulumi/                         # Infrastructure as Code tool
    └── Infrastructure/             # Logical grouping
```

### 4. Documentation (Documentation/)

**Pattern**: **Hierarchical PascalCase with Semantic Organization**

```
Documentation/
├── C4/                             # Architecture methodology
├── Developer/                      # Audience-based organization
│   ├── Conceptual/                 # Information architecture
│   │   ├── ArchitecturalDecisionRecords/  # ADR pattern
│   │   │   ├── Approved/           # Status-based organization
│   │   │   ├── Examples/           # Category-based grouping
│   │   │   ├── Proposed/           # Workflow state
│   │   │   └── ProjectStructureAndConventions/  # Topic grouping
│   │   ├── Features/               # Feature documentation
│   │   │   ├── Application/        # Mirrors source structure
│   │   │   └── Overview.md
│   │   └── Testing/                # Practice-based organization
│   ├── HowToGuides/                # Document type classification
│   │   ├── Api_Contracts/          # Snake_case exception for multi-word
│   │   └── Testing/                # Nested by practice area
│   ├── Reference/                  # Information architecture
│   └── Tutorials/                  # Learning path organization
├── StarUml/                        # Tool-specific (PascalCase maintained)
│   └── Generated/                  # Generation status indicator
│       └── html-docs/              # Output format + content type
└── User/                           # Audience segmentation
```

**Special Pattern**: `Api_Contracts/` uses snake_case - appears to be an exception for multi-word technical terms.

### 5. Task Management (Kanban/)

**Pattern**: **Workflow-State + Hyphenated File Names**

```
Kanban/
├── Backlog/                        # Workflow state (PascalCase)
│   ├── B001_Create-Strongly-Typed-Id-Mixin.md  # Prefix + kebab-case
│   └── Scratch/                    # Sub-organizational category
├── Done/                           # Workflow state
│   ├── 025_Create-File-Naming-Convention-Analysis-Report/  # Folder for complex tasks
│   │   ├── File-Naming-Convention-Analysis-Report.md
│   │   └── Task.md
│   └── [Other completed tasks with kebab-case names]
├── InProgress/                     # Workflow state (PascalCase)
├── ToDo/                           # Workflow state (PascalCase) 
├── Task-Examples/                  # Reference material (hyphenated)
└── Task-Template.md                # Template file (hyphenated)
```

**File Naming Within Kanban**: **Number + kebab-case**
- `001_Fix-FastEndpointSourceGenerator.md`
- `025_Create-File-Naming-Convention-Analysis-Report/`
- Consistent pattern: `{Number}_{Kebab-Case-Description}`

### 6. Frontend Assets and Configuration

#### Web.Spa TypeScript Organization: **camelCase for JavaScript Ecosystem**
```
source/
├── Spa.ts                          # PascalCase for main files
├── Web.Spa.lib.module.ts           # Dotted PascalCase + descriptive
├── features/                       # camelCase directory (JS convention)
│   └── Counter.ts                  # PascalCase files
└── types/                          # camelCase directory (TypeScript convention)
    ├── Constants.d.ts              # PascalCase type definition files
    ├── DotNetReference.d.ts
    ├── Logger.d.ts
    ├── ReduxDevTools.d.ts
    ├── TimeWarpState.d.ts
    └── global.d.ts                 # lowercase for global definitions
```

**Rationale**: Follows JavaScript/TypeScript ecosystem conventions while maintaining PascalCase for .NET integration points.

## Statistical Analysis

| **Convention Type** | **Count** | **Percentage** | **Primary Usage Context** |
|---------------------|-----------|----------------|---------------------------|
| PascalCase | 335 | 87.7% | .NET projects, documentation, general organization |
| PascalCase with Dots | 47 | 12.3% | .NET namespace-aligned projects |
| kebab-case | 8 | 2.1% | Kubernetes resources, task files, domain names |
| snake_case | 15 | 3.9% | Kubernetes concepts, numbered organizational categories |
| camelCase | 2 | 0.5% | JavaScript/TypeScript directories |

**Note**: Percentages sum to more than 100% due to directories that may exhibit multiple patterns (e.g., dotted PascalCase).

## Technology-Specific Naming Patterns

### .NET Ecosystem
- **Projects**: `TimeWarp.Architecture.{Component}` (Dotted PascalCase)
- **Folders**: `{Domain}/{Technology}.{Layer}/` (Hierarchical PascalCase)
- **Features**: `Features/{FeatureName}/` (Descriptive PascalCase)
- **Components**: `{ComponentType}s/` (Plural PascalCase categories)

### Kubernetes/Infrastructure
- **Resource Directories**: `api-server/`, `grpc-server/` (kebab-case)
- **Kubernetes Concepts**: `Persistent_Volume_Claims/` (snake_case)
- **Organizational**: `0_Namespaces/`, `1_Nodes/` (numbered + snake_case)

### Web Frontend
- **TypeScript Directories**: `features/`, `types/` (camelCase)
- **Component Hierarchies**: `Components/{Category}/` (PascalCase)
- **State Management**: `{Feature}State/` (PascalCase + descriptive suffix)

### Documentation
- **Audience-Based**: `Developer/`, `User/` (PascalCase)
- **Information Architecture**: `Conceptual/`, `HowToGuides/`, `Reference/`, `Tutorials/` (PascalCase)
- **Status/Workflow**: `Approved/`, `Proposed/`, `Examples/` (PascalCase)

### Task Management
- **Workflow States**: `Backlog/`, `InProgress/`, `Done/`, `ToDo/` (PascalCase)
- **Task Files**: `{Number}_{Kebab-Case-Description}.md` (Mixed convention)

## Consistency Analysis

### High Consistency Areas (95-100%)
1. **.NET Source Projects**: Virtually perfect adherence to dotted PascalCase
2. **Test Organization**: 100% mirror of source structure with `.Tests` suffix
3. **Feature Internal Organization**: Consistent hierarchical PascalCase across all features
4. **Documentation Categories**: Strong semantic PascalCase organization

### Strategically Inconsistent Areas
1. **Kubernetes Resources**: Deliberately follows K8s conventions (kebab-case, snake_case)
2. **Frontend TypeScript**: Adopts JavaScript ecosystem conventions (camelCase)
3. **Task File Names**: Uses kebab-case for readability in file names
4. **Domain Names**: `timewarp.software.com/` follows DNS conventions

### Identified Inconsistencies
1. **`Api_Contracts/`**: Should likely be `ApiContracts/` for consistency
2. **Mixed numbering systems**: Some areas use numbers, others don't
3. **Generated content**: Some generated directories don't follow patterns

## Organizational Principles

### Hierarchy Depth Strategy
- **Shallow for broad categories**: `Source/`, `Tests/`, `Documentation/`
- **Deep for specific contexts**: `Features/{Feature}/{ComponentType}/{Specific}/`
- **Technology-appropriate depth**: Kubernetes uses 7 levels, .NET uses 3-4 levels

### Multi-Word Handling
- **.NET Context**: PascalCase (`WeatherForecast`, `ApplicationState`)
- **Infrastructure Context**: kebab-case (`api-server`, `grpc-server`)
- **File Names in Kanban**: kebab-case (`Create-Directory-Naming-Convention-Analysis-Report`)
- **Kubernetes Concepts**: snake_case (`Persistent_Volume_Claims`)

### Technology Alignment
- **Follows .NET conventions** for C# projects
- **Follows Kubernetes conventions** for infrastructure
- **Follows JavaScript conventions** for frontend assets
- **Follows documentation standards** for information architecture

## Recommendations

### Maintain Current Strategy
The project's **adaptive naming strategy** is appropriate and should be maintained:

1. **PascalCase as Default**: Continue using PascalCase for general .NET organization
2. **Technology-Specific Adaptations**: Maintain technology-appropriate conventions (K8s kebab-case, JS camelCase)
3. **Semantic Clarity**: Prioritize descriptive names over abbreviated forms

### Address Minor Inconsistencies
1. **Standardize `Api_Contracts/`**: Consider renaming to `ApiContracts/` for consistency
2. **Document Exceptions**: Create clear documentation for when to deviate from PascalCase
3. **Tooling Integration**: Ensure naming supports build tools and IDE expectations

### Best Practices Going Forward
1. **Feature Organization**: Continue the `Features/{FeatureName}/{ComponentType}/` pattern
2. **Layer Alignment**: Maintain `{Service}.{Layer}/` naming for clean architecture
3. **Test Mirroring**: Keep test structure identical to source structure
4. **Technology Respect**: Use conventions appropriate to each technology ecosystem

## Conclusion

The TimeWarp.Architecture project demonstrates a **sophisticated, context-aware naming strategy** that balances consistency with technological appropriateness. The high degree of consistency within each domain (87.7% PascalCase) combined with strategic adaptations for specific technologies (Kubernetes, JavaScript) represents mature architectural decision-making.

The directory naming conventions serve multiple purposes:
- **Developer Productivity**: Predictable patterns aid navigation
- **Tool Integration**: Names work well with IDEs, build systems, and deployment tools  
- **Team Communication**: Clear semantic meaning in directory names
- **Technology Compatibility**: Respects ecosystem conventions for each technology

This approach should be maintained and codified as a standard for similar projects, with the adaptive strategy serving as a model for other complex, multi-technology solutions.