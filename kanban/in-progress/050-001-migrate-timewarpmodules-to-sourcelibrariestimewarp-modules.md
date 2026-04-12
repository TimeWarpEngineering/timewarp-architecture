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

- [ ] Create directory: source/libraries/timewarp-modules/
- [ ] Migrate i-module.cs (apply C# coding standards)
- [ ] Migrate timewarp-modules.csproj
- [ ] Migrate global-usings.cs (apply C# coding standards)
- [ ] Migrate assembly-marker.cs (apply C# coding standards)
- [ ] Verify build

## Session

- Created: ses_2d78597cfffeIe36aerm1ibchw (2026-04-12)

## Notes

### C# Coding Standards
- 2 spaces indentation, LF line endings
- Allman bracket style
- Explicit types (no var)
- Target-typed new
- File-scoped namespaces
- No underscore prefix on private fields (PascalCase for class scope)

### Namespace Decision
Current: `TimeWarp.Architecture` (IModule.cs) and `TimeWarp.Modules` (AssemblyMarker.cs)
Proposed: Keep as `TimeWarp.Modules` (matches project/package name)

### Dependency Status
Leaf project — no internal ProjectReferences to other timewarp-architecture projects.
Only references: `TimeWarp.Mediator` (NuGet), `Microsoft.AspNetCore.App` (framework)

### Referenced By
- Common.Application (Level 1)