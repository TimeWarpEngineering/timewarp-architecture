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
source/
└── libraries/
    └── timewarp-modules/
        ├── imodule.cs           ← kebab-case filename
        ├── timewarp-modules.csproj
        ├── global-usings.cs
        └── assembly-marker.cs
```

## Purpose

The IModule interface provides a plugin-style service registration pattern for Clean Architecture. Each feature implements `IModule.ConfigureServices` to self-register its DI dependencies.

## Checklist

### Phase 1: Read & Analyze Current Files
- [ ] Read IModule.cs
- [ ] Read TimeWarp.Modules.csproj
- [ ] Read GlobalUsings.cs
- [ ] Read AssemblyMarker.cs

### Phase 2: Create Target Directory
- [ ] Create directory: source/libraries/timewarp-modules/
- [ ] Create kebab-case filenames

### Phase 3: Migrate Files (Apply C# Coding Standards)
- [ ] Convert indentation to 2 spaces
- [ ] Convert to file-scoped namespaces
- [ ] Use explicit types (no var)
- [ ] Use target-typed new
- [ ] Use Allman bracket style
- [ ] No underscore prefix on private fields
- [ ] Check PascalCase for class-scope members

### Phase 4: Update References
- [ ] Update namespace from `TimeWarp.Architecture` to `TimeWarp.Modules`
- [ ] Verify no ProjectReferences to other timewarp-architecture projects

### Phase 5: Verification
- [ ] Verify build
- [ ] Run tests (if any exist)

## Notes

### C# Coding Standards (from csharp skill)
- **Indentation**: 2 spaces, LF line endings
- **Brackets**: Allman style (`{` on own line)
- **Types**: Explicit, never `var`
- **New**: Target-typed `new()` not `new TypeName()`
- **Namespaces**: File-scoped
- **Private fields**: No underscore prefix (PascalCase: `private readonly HttpClient HttpClient;`)
- **Global usings**: In global-usings.cs

### Namespace Decision
Current: `TimeWarp.Architecture`
Proposed: Keep as `TimeWarp.Modules` (matches package name)

This keeps the "Foundation" NuGet naming separate from namespace. The namespace will match the package name `TimeWarp.Modules` rather than `TimeWarp.Foundation.*`.

## References
- Task 050: Root directory structure
- Task 049: Dependency graph (Phase 1 - leaf project #5)
- C# skill: /home/steventcramer/.../timewarp-flow/master/opencode/skills/csharp