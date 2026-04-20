# Migrate TimeWarp.Architecture.Attributes to source/analyzers/timewarp-architecture-attributes/

## Parent
050-establish-root-directory-structure-source-tests-in-kebab-case

## Summary

Migrate the TimeWarp.Architecture.Attributes project from `TimeWarp.Architecture/Source/Analyzers/TimeWarp.Architecture.Attributes/` to `source/analyzers/timewarp-architecture-attributes/` (kebab-case directory and file names). This is Phase 1 - leaf project (no internal dependencies).

## Current Location

```
TimeWarp.Architecture/Source/Analyzers/TimeWarp.Architecture.Attributes/
├── ApiEndpointAttribute.cs
├── AssemblyMarker.cs
└── TimeWarp.Architecture.Attributes.csproj
```

## Target Location

```
source/analyzers/timewarp-architecture-attributes/
├── api-endpoint-attribute.cs
├── assembly-marker.cs
├── global-usings.cs
└── timewarp-architecture-attributes.csproj
```

## Skills

- csharp

## Checklist

- [x] Create directory: source/analyzers/timewarp-architecture-attributes/
- [x] git mv files with kebab-case filenames
- [x] Migrate timewarp-architecture-attributes.csproj (preserve netstandard2.0)
- [x] Migrate api-endpoint-attribute.cs (apply C# coding standards: 2-space indent)
- [x] Migrate assembly-marker.cs (add CA1040 suppression, apply C# coding standards)
- [x] Add global-usings.cs (needed because ImplicitUsings disabled for netstandard2.0)
- [x] Update project references in Api.Contracts, Api.Server, SourceGenerator
- [x] Update solution file (.slnx) paths (both root and TimeWarp.Architecture)
- [x] Verify build (dev build succeeded)

## Notes

### Special Considerations
- **Target framework**: Preserved `netstandard2.0` override for analyzer compatibility
- **ImplicitUsings disabled**: netstandard2.0 doesn't support C# implicit usings, so added `global-usings.cs` with `global using System;`
- **Namespace**: Kept as `TimeWarp.Architecture.Attributes` — this is the public API namespace
- **IAssemblyMarker**: Added `#pragma warning disable CA1040` per established repo pattern
- **No NuGet dependencies**: Completely self-contained project, no package references needed

### Project Reference Paths
- SourceGenerator: `../../../../source/analyzers/timewarp-architecture-attributes/timewarp-architecture-attributes.csproj`
- Api.Contracts: `../../../../../source/analyzers/timewarp-architecture-attributes/timewarp-architecture-attributes.csproj`
- Api.Server: `../../../../../source/analyzers/timewarp-architecture-attributes/timewarp-architecture-attributes.csproj`

### Dependency Status
Leaf project — no internal ProjectReferences, no NuGet package references.

### Referenced By
- Api.Contracts
- Api.Server
- TimeWarp.Architecture.SourceGenerator

## Results

Successfully migrated. Build passes (0 warnings, 0 errors for this project; pre-existing NU1903 vulnerability warnings in TimeWarp.Architecture.slnx are unrelated).

## Session

- Created: ses_2d78597cfffeIe36aerm1ibchw (2026-04-20)
