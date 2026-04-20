# Merge timewarp-architecture-generator into timewarp-architecture-analyzers

## Parent
050-establish-root-directory-structure-source-tests-in-kebab-case

## Summary

Combine the timewarp-architecture-generator project into timewarp-architecture-analyzers. Both are Roslyn analyzer components (targeting net10.0, same dependencies, same consumption pattern). This follows the Nuru pattern where "analyzers" is the umbrella term for both analyzers and generators.

## Rationale

### Why merge:
- **Shared code**: Both have duplicate `ToKebabCase()` implementations
- **Same dependencies**: Microsoft.CodeAnalysis.CSharp, Microsoft.CodeAnalysis.Analyzers
- **Same consumption**: Both consumed via `OutputItemType="Analyzer"`
- **Simpler NuGet**: One package instead of two for Foundation NuGet work
- **Ecosystem convention**: Microsoft and community use "Analyzers" as umbrella term

### Why keep attributes separate:
- **Api.Contracts references just the attribute** — shouldn't pull in Roslyn
- **Zero dependencies for attributes** — clean transitive dependency chain

## Current State

```
source/analyzers/
├── timewarp-architecture-attributes/     ← keep separate
└── timewarp-architecture-analyzers/    ← target
└── timewarp-architecture-generator/      ← source (to be merged)
```

## Target State

```
source/analyzers/
├── timewarp-architecture-attributes/     ← just [ApiEndpoint] attribute
└── timewarp-architecture-analyzers/     ← analyzers + generators + diagnostics + validation
    ├── analyzer-releases.shipped.md     ← merge rules from both
    ├── analyzer-releases.unshipped.md   ← merge rules from both
    ├── assembly-marker.cs
    ├── global-usings.cs
    ├── partial-class-declaration-analyzer.cs
    ├── partial-class-declaration-analyzer.md
    ├── fast-endpoint-source-generator.cs
    ├── fast-endpoint-source-generator.md
    ├── diagnostics/
    │   └── diagnostic-descriptors.cs
    ├── generators/
    │   ├── fast-endpoint-source-generator.cs  ← renamed to avoid conflict
    │   └── fast-endpoint-source-generator.md
    ├── helpers/
    │   └── string-extensions.cs         ← single shared implementation
    ├── models/
    │   └── endpoint-metadata.cs
    └── validation/
        └── route-registry.cs
```

## Skills

- csharp

## Checklist

### Phase 1: Prepare
- [ ] Review both projects for namespace collisions
- [ ] Decide on unified namespace: `TimeWarp.Architecture.Analyzers` (keep existing)
- [ ] Plan file structure under generators/ and helpers/

### Phase 2: Move Files
- [ ] Move generator files to analyzers/generators/ subdirectory
- [ ] Move helper files (deduplicate ToKebabCase)
- [ ] Move model files
- [ ] Move validation files
- [ ] Update AnalyzerReleases files (merge TWE and TWPA rules)

### Phase 3: Merge Code
- [ ] Update global-usings.cs to include generator namespaces
- [ ] Deduplicate StringExtensions.ToKebabCase (keep one in helpers/)
- [ ] Update namespace in all moved files from Generator to Analyzers
- [ ] Merge diagnostic descriptors if any overlap

### Phase 4: Update Project
- [ ] Update timewarp-architecture-analyzers.csproj to include generator files
- [ ] Add OutputItemType="Analyzer" for generator assembly reference
- [ ] Remove timewarp-architecture-generator.csproj reference
- [ ] Update NoWarn to include both RS1041 and CA1031

### Phase 5: Update Consumers
- [ ] Update all csproj references (GenTester, Api.Server, Tests)
- [ ] Remove separate generator project references
- [ ] Keep just: timewarp-architecture-attributes + timewarp-architecture-analyzers

### Phase 6: Cleanup
- [ ] Delete timewarp-architecture-generator/ directory
- [ ] Update both .slnx files
- [ ] Verify build

## Notes

### Namespace Changes
- Files in `generators/` subdirectory keep namespace `TimeWarp.Architecture.Analyzers`
- No separate `TimeWarp.Architecture.Generator` namespace needed
- Both analyzers and generators share same root namespace

### File Naming
- Analyzer: `partial-class-declaration-analyzer.cs`
- Generator: `fast-endpoint-source-generator.cs` (under generators/)
- Helpers: `string-extensions.cs` (single shared implementation)

### AnalyzerReleases Merge
Current rules:
- **Analyzers**: TWPA0001 (PartialClassDeclaration)
- **Generator**: TWE001-TWE004 (Endpoint diagnostics)

All go into AnalyzerReleases.Unshipped.md

## References
- Nuru pattern: /source/timewarp-nuru-analyzers/ (generators/, diagnostics/, validation/ subdirectories)
- Microsoft convention: "Analyzers" = umbrella term for analyzers + generators
- Current merged projects: timewarp-architecture-analyzers (TWPA0001), timewarp-architecture-generator (TWE001-TWE004)

## Session
- Created: ses_2d78597cfffeIe36aerm1ibchw (2026-04-20)
