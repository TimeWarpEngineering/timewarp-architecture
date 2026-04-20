# Migrate TimeWarp.Automation.Contracts to source/libraries/timewarp-automation-contracts/

## Parent
050-establish-root-directory-structure-source-tests-in-kebab-case

## Summary

Migrate TimeWarp.Automation.Contracts from `TimeWarp.Architecture/Source/Libraries/` to `source/libraries/timewarp-automation-contracts/` with kebab-case file and directory naming. Small leaf project with no internal dependencies.

## Rationale

- Leaf project (no ProjectReferences to other solution projects)
- Only referenced by TimeWarp.Automation and its test project
- Small scope: 5 source files total
- Fits the libraries/ bucket alongside timewarp-modules

## Current State

```
TimeWarp.Architecture/Source/Libraries/TimeWarp.Automation.Contracts/
├── AssemblyMarker.cs
├── GlobalUsings.cs
├── TimeWarp.Automation.Contracts.csproj
├── Features/
│   ├── RunApplication/RunApplication.cs
│   └── RunWindowsApplication/RunWindowsApplication.cs
└── Generated/  (gitignored)
```

## Target State

```
source/libraries/timewarp-automation-contracts/
├── assembly-marker.cs
├── global-usings.cs
├── timewarp-automation-contracts.csproj
└── features/
    ├── run-application/run-application.cs
    └── run-windows-application/run-windows-application.cs
```

## Skills

- csharp

## Checklist

### Phase 1: Create Directory Structure
- [x] Create source/libraries/timewarp-automation-contracts/features/run-application/
- [x] Create source/libraries/timewarp-automation-contracts/features/run-windows-application/

### Phase 2: Migrate Files
- [x] git mv AssemblyMarker.cs → assembly-marker.cs
- [x] git mv GlobalUsings.cs → global-usings.cs
- [x] git mv TimeWarp.Automation.Contracts.csproj → timewarp-automation-contracts.csproj
- [x] git mv Features/RunApplication/RunApplication.cs → features/run-application/run-application.cs
- [x] git mv Features/RunWindowsApplication/RunWindowsApplication.cs → features/run-windows-application/run-windows-application.cs
- [x] Skip Generated/ (gitignored)

### Phase 3: Update Project Files
- [x] Update csproj: remove redundant `<Nullable>enable</>` (inherited from source/Directory.Build.props)
- [x] Add CA1034 NoWarn (nested generic types in Command/Response pattern)
- [x] Add CA1040 pragma to assembly-marker.cs

### Phase 4: Update Referencing Projects
- [x] TimeWarp.Automation.csproj — update ProjectReference path
- [x] TimeWarp.Automation.Tests.csproj — update ProjectReference path

### Phase 5: Update Solution Files
- [x] Update TimeWarp.Architecture.slnx (redirect project path)
- [x] Add project to timewarp-architecture.slnx under source/libraries/

### Phase 6: Cleanup and Verify
- [x] Remove old directory
- [x] Build verify timewarp-automation-contracts individually
- [x] Build verify timewarp-architecture.slnx

## Notes

### Namespace
- Namespace stays `TimeWarp.Automation.Contracts` and `TimeWarp.Automation.Features` — unchanged
- The `Contracts` suffix in namespace will be addressed by task 051 if applicable

### CA1034 Suppression
- `CA1034: Nested types should not be visible` — the Command/Response pattern uses nested types by design
- Suppressed in csproj rather than per-file since this is a project-wide pattern

## Results

Successfully migrated TimeWarp.Automation.Contracts to source/libraries/timewarp-automation-contracts/.

### What was implemented
- 5 source files migrated with kebab-case naming
- csproj updated (removed redundant Nullable, kept Description, added CA1034 NoWarn)
- CA1040 pragma added to assembly-marker.cs
- Updated 2 referencing project paths (TimeWarp.Automation, TimeWarp.Automation.Tests)
- Both .slnx files updated
- Namespace unchanged (TimeWarp.Automation.Contracts)

### Build verification
- timewarp-architecture.slnx: Build succeeded, 0 warnings, 0 errors

## Session
- Created: ses_2d78597cfffeIe36aerm1ibchw (2026-04-20)
- Completed: ses_2550ccbd7ffengZayMrI70H7HX (2026-04-20)