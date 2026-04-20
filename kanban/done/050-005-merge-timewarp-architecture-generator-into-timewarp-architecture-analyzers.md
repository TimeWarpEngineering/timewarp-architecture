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

## Results

Successfully merged timewarp-architecture-generator into timewarp-architecture-analyzers.

### What was implemented
- All generator files moved to analyzers subdirectories (generators/, diagnostics/, helpers/, models/, validation/)
- Namespace unified to TimeWarp.Architecture.Analyzers (fixed singular 'Analyzer' bug in partial-class-declaration-analyzer.cs)
- AnalyzerReleases merged (TWPA0001 + TWE001-TWE004)
- ToKebabCase deduplicated — analyzer uses shared StringExtensions.ToKebabCase()
- Consumer references updated (Api.Server, GenTester, SourceGenerator.Tests now point to analyzers project)
- CA1062 suppressed (Roslyn guarantees non-null context parameters)
- CA1031 kept suppressed (source generators must be resilient)
- RS1041 kept suppressed (targeting net10.0 per Nuru pattern)
- timewarp-architecture-generator/ directory deleted entirely
- Both .slnx files updated (generator entry removed)

### Files changed
- source/analyzers/timewarp-architecture-analyzers/ (merged project)
  - generators/fast-endpoint-source-generator.cs (namespace fix)
  - diagnostics/diagnostic-descriptors.cs (namespace fix)
  - helpers/string-extensions.cs (namespace fix)
  - models/endpoint-metadata.cs (namespace fix)
  - validation/route-registry.cs (namespace fix)
  - partial-class-declaration-analyzer.cs (uses shared ToKebabCase)
  - global-usings.cs (merged)
  - AnalyzerReleases.Unshipped.md (merged rules)
  - timewarp-architecture-analyzers.csproj (merged deps, NoWarn RS1041+CA1031+CA1062)
- source/analyzers/timewarp-architecture-generator/ (deleted entirely)
- TimeWarp.Architecture/Source/ContainerApps/Api/Api.Server/Api.Server.csproj
- TimeWarp.Architecture/Source/GenTester/GenTester.csproj
- TimeWarp.Architecture/Tests/Analyzers/TimeWarp.Architecture.SourceGenerator.Tests/TimeWarp.Architecture.SourceGenerator.Tests.csproj
- TimeWarp.Architecture/TimeWarp.Architecture.slnx (removed generator entry)
- timewarp-architecture.slnx (already clean — no generator entry existed)

### Key decisions
- Followed Nuru pattern: 'analyzers' is umbrella term for analyzers + generators
- Attributes project kept separate (clean transitive dependency chain for Api.Contracts)
- All diagnostic IDs preserved (TWPA prefix for analyzer, TWE prefix for generator/endpoint)

### Build verification
- timewarp-architecture.slnx: Build succeeded, 0 warnings, 0 errors

## Session
- Created: ses_2d78597cfffeIe36aerm1ibchw (2026-04-20)
- Completed: ses_2551679a4ffe6T9dEoaPsaXjoP (2026-04-20)
