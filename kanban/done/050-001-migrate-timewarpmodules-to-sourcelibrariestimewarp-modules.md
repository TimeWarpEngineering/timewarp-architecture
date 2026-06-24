# Migrate TimeWarp.Modules to source/libraries/timewarp-modules/

## Parent
050-establish-root-directory-structure-source-tests-in-kebab-case

## Summary

Migrate the TimeWarp.Modules project from `TimeWarp.Architecture/Source/Libraries/TimeWarp.Modules/` to `source/libraries/timewarp-modules/` (kebab-case directory and file names). This is Phase 1 - leaf project (no internal dependencies).

## Current Location

```
TimeWarp.Architecture/Source/Libraries/TimeWarp.Modules/
├── IModule.cs
├── TimeWarp.Modules.csproj
├── GlobalUsings.cs
└── AssemblyMarker.cs
```

## Target Location

```
source/libraries/timewarp-modules/
├── i-module.cs              ← kebab-case filename
├── timewarp-modules.csproj
├── global-usings.cs
└── assembly-marker.cs
```

## Skills

- csharp

## Checklist

- [x] Create directory: source/libraries/timewarp-modules/
- [x] git mv files with kebab-case filenames
- [x] Migrate i-module.cs (namespace TimeWarp.Architecture → TimeWarp.Modules)
- [x] Migrate timewarp-modules.csproj
- [x] Migrate global-usings.cs
- [x] Migrate assembly-marker.cs (add CA1040 suppression for IAssemblyMarker)
- [x] Update namespace in i-module.cs (TimeWarp.Architecture → TimeWarp.Modules)
- [x] Update project references (Common.Application, Api.Application)
- [x] Update solution file (.slnx) path
- [x] Add using TimeWarp.Modules to GlobalUsings.cs in dependent projects
- [x] Remove unnecessary TimeWarp.Mediator package reference
- [x] Replace Microsoft.AspNetCore.App framework reference with narrow abstraction packages
- [x] Add Microsoft.Extensions.* package versions to root Directory.Packages.props
- [x] Update source/Directory.Build.props with TargetFramework and build properties
- [x] Remove unnecessary source/libraries/Directory.Build.props (MSBuild chains correctly without it)
- [x] Verify build (0 warnings, 0 errors)

## Session

- Created: ses_2d78597cfffeIe36aerm1ibchw (2026-04-12)

## Notes

### Namespace Decision
- `IModule.cs`: Changed from `TimeWarp.Architecture` → `TimeWarp.Modules`
- `AssemblyMarker.cs` and `GlobalUsings.cs`: Already used `TimeWarp.Modules`, no change needed

### Dependency Changes
- **Removed**: `TimeWarp.Mediator` NuGet package (not used by this project)
- **Removed**: `Microsoft.AspNetCore.App` framework reference (overly broad)
- **Added**: `Microsoft.Extensions.Configuration.Abstractions` 10.0.5 (only actual dependency needed)
- **Added**: `Microsoft.Extensions.DependencyInjection.Abstractions` 10.0.5 (only actual dependency needed)

### CA1040 Suppression
- `IAssemblyMarker` is an empty interface by design (marker pattern for assembly scanning)
- Microsoft guidance: custom attributes are preferred for runtime identification, but empty interfaces are acceptable for compile-time identification
- This repo uses `IAssemblyMarker` consistently across all projects for `typeof(...IAssemblyMarker).Assembly` patterns
- Added `#pragma warning disable CA1040` / `#pragma warning restore CA1040` around `IAssemblyMarker`
- A separate task should be created to evaluate replacing all `IAssemblyMarker` interfaces repo-wide

### MSBuild Directory.Build.props Chain
- `source/Directory.Build.props` — imports root, adds `TargetFramework` (net10.0), `Nullable`, `ImplicitUsings`, `LangVersion`, `ManagePackageVersionsCentrally`, package metadata
- `source/libraries/Directory.Build.props` was initially created but proved unnecessary — MSBuild walks up to `source/Directory.Build.props` naturally
- Deleted `source/libraries/Directory.Build.props` as it was a passthrough with no additions

### GlobalUsings Changes
Files that needed `global using TimeWarp.Modules;` added:
- `Common.Infrastructure/GlobalUsings.cs`
- `Common.Server/GlobalUsings.cs`
- `Api.Application/GlobalUsings.cs`
- `Web.Server/GlobalUsings.cs`
- `Web.Infrastructure/GlobalUsings.cs`

### Project Reference Paths Updated
- `Common.Application.csproj`: `../../../../source/libraries/timewarp-modules/timewarp-modules.csproj`
- `Api.Application.csproj`: `../../../../../source/libraries/timewarp-modules/timewarp-modules.csproj`

### IModule Usage Analysis
- `IModule` is NOT used as a runtime marker (no assembly scanning for IModule implementations)
- `IModule` defines `static abstract void ConfigureServices(...)` — a compile-time contract
- `IAspNetModule : IModule` extends it with 3 more static abstract methods
- Concrete modules are called directly (e.g., `CommonServerModule.ConfigureServices(...)`)
- CA1040 does NOT apply to `IModule` — it has members

### Referenced By
- Common.Application (Level 1)
- Api.Application (direct)

### Template Impact
The project references in `Common.Application.csproj` and `Api.Application.csproj` point outside the `TimeWarp.Architecture/` template root. This will need to be addressed when the template is updated (separate task).