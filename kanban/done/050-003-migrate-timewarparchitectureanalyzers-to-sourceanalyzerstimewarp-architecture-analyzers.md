# Migrate TimeWarp.Architecture.Analyzers to source/analyzers/timewarp-architecture-analyzers/

## Parent
050-establish-root-directory-structure-source-tests-in-kebab-case

## Summary

Migrate the TimeWarp.Architecture.Analyzers project from `TimeWarp.Architecture/Source/Analyzers/TimeWarp.Architecture.Analyzers/` to `source/analyzers/timewarp-architecture-analyzers/` (kebab-case directory and file names). This is Phase 1 - leaf project (no internal dependencies).

## Current Location

```
TimeWarp.Architecture/Source/Analyzers/TimeWarp.Architecture.Analyzers/
├── AnalyzerReleases.Shipped.md
├── AnalyzerReleases.Unshipped.md
├── AssemblyMarker.cs
├── GlobalUsings.cs
├── PartialClassDeclarationAnalyzer.cs
├── PartialClassDeclarationAnalyzer.md
└── TimeWarp.Architecture.Analyzers.csproj
```

## Target Location

```
source/analyzers/timewarp-architecture-analyzers/
├── analyzer-releases.shipped.md
├── analyzer-releases.unshipped.md
├── assembly-marker.cs
├── global-usings.cs
├── partial-class-declaration-analyzer.cs
├── partial-class-declaration-analyzer.md
└── timewarp-architecture-analyzers.csproj
```

## Skills

- csharp

## Checklist

- [ ] Create directory: source/analyzers/timewarp-architecture-analyzers/
- [ ] git mv files with kebab-case filenames
- [ ] Migrate timewarp-architecture-analyzers.csproj (simplify - inherit from Directory.Build.props)
- [ ] Migrate source files (apply C# coding standards, CA1040 suppression on IAssemblyMarker)
- [ ] Update source/Directory.Packages.props with Microsoft.CodeAnalysis packages if needed
- [ ] Update solution file (.slnx) paths
- [ ] Verify build

## Notes

### Special Considerations
- **Target framework**: Currently `netstandard2.0` but Nuru targets `net10.0` for analyzers and works fine. Can inherit `net10.0` from source/Directory.Build.props.
- **ImplicitUsings**: Currently `enable` in the project - can remove and inherit
- **NuGet dependencies**: Microsoft.CodeAnalysis.Analyzers, Microsoft.CodeAnalysis.Common, Microsoft.CodeAnalysis.CSharp - need versions in Directory.Packages.props
- **Namespace**: Currently `TimeWarp.Architecture.Analyzer` for the analyzer class, `TimeWarp.Architecture.Analyzers` for AssemblyMarker
- **IAssemblyMarker**: Empty interface - needs CA1040 suppression like other projects
- **Code style violations**: Uses `var` in several places - needs explicit types per csharp skill

### Current Package References
- Microsoft.CodeAnalysis.Analyzers
- Microsoft.CodeAnalysis.Common  
- Microsoft.CodeAnalysis.CSharp

### Dependency Status
Leaf project - no internal ProjectReferences.

### Referenced By
- No direct project references found (analyzer is referenced via OutputItemType="Analyzer")
- Used by projects that need the partial class declaration analyzer (TWPA0001)

## Session

- Created: ses_2d78597cfffeIe36aerm1ibchw (2026-04-20)
