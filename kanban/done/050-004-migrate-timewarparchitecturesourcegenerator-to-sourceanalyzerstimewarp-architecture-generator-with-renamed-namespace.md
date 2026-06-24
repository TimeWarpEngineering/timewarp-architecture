# Migrate TimeWarp.Architecture.SourceGenerator to source/analyzers/timewarp-architecture-generator/

## Parent
050-establish-root-directory-structure-source-tests-in-kebab-case

## Summary

Migrated TimeWarp.Architecture.SourceGenerator to source/analyzers/timewarp-architecture-generator/ with namespace renamed from SourceGenerator to Generator. Phase 1 (Level 1) project - depends on Attributes (already migrated).

## Rename Decision
- Old: `TimeWarp.Architecture.SourceGenerator`
- New: `TimeWarp.Architecture.Generator` (shorter, no loss of clarity)

## Skills
- csharp

## Checklist
- [x] Create directory: source/analyzers/timewarp-architecture-generator/
- [x] git mv files with kebab-case filenames (recursively through subdirectories)
- [x] Rename namespace from `TimeWarp.Architecture.SourceGenerator` to `TimeWarp.Architecture.Generator`
- [x] Migrate timewarp-architecture-generator.csproj (inherit net10.0, suppress RS1041 and CA1031)
- [x] Update project reference to Attributes (already points to new location)
- [x] Apply C# coding standards (explicit types, CA1040 suppression, sealed classes, StringComparison)
- [x] Keep AnalyzerReleases files in PascalCase (Roslyn convention)
- [x] Create AnalyzerReleases.Unshipped.md with TWE001-TWE004 rules
- [x] Update all project references (GenTester, Api.Server, SourceGenerator.Tests)
- [x] Update solution file paths
- [x] Verify build (0 warnings, 0 errors)

## Results

Successfully migrated. Build passes clean.

### Code Quality Fixes Applied
- **CA1040**: Suppressed on IAssemblyMarker (empty marker interface pattern)
- **CA1031**: Suppressed project-wide (source generators must catch general exceptions)
- **CA1304/CA1308/CA1310/CA1311**: Fixed all culture-sensitive string operations (ToLower → ToLowerInvariant, IndexOf/Contains → StringComparison.Ordinal)
- **CA1852**: Made RouteRegistry and EndpointMetadata sealed
- **CA1860**: Tags.Any() → Tags.Length > 0
- **Explicit types**: Replaced all `var` with explicit type declarations
- **RS1041**: Suppressed (analyzer intentionally targets net10.0)

### Files Changed
- All .cs files moved with kebab-case filenames
- Namespace renamed from SourceGenerator to Generator
- timewarp-architecture-generator.csproj created (net10.0, RoslynComponent)
- AnalyzerReleases.Unshipped.md created
- GenTester.csproj, Api.Server.csproj, SourceGenerator.Tests.csproj paths updated
- Both .slnx files updated

### Subdirectory Structure
- diagnostics/diagnostic-descriptors.cs
- helpers/string-extensions.cs
- models/endpoint-metadata.cs
- validation/route-registry.cs

## Session
- Created: ses_2d78597cfffeIe36aerm1ibchw (2026-04-20)
