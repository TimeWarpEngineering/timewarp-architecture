# Migrate Common.Contracts to source/common/foundation-contracts/

## Description

[Brief description of the task]

## Checklist

- [ ] Item 1
- [ ] Item 2

## Session

- Created: ses_2d78597cfffeIe36aerm1ibchw (2026-04-20)

## Notes

[Additional context]

## Results

Successfully migrated Common.Contracts and Common.Domain to source/common/.

### What was implemented
- Common.Contracts → source/common/foundation-contracts/ (26 source files + 8 mixin files)
- Common.Domain → source/common/foundation-domain/ (6 source files)
- All files renamed to kebab-case (except Mixin files which stay PascalCase for Roslyn)
- Directory structure: base/, behaviors/, configuration/, mixins/, services/, types/, validators/ (Contracts) and entities/base/, enumeration/ (Domain)
- Added CA1040 pragma to IAssemblyMarker interfaces
- Added source/common/Directory.Build.props with CA suppressions for pre-existing code patterns
- Updated 6 referencing project .csproj paths (Common.Application, Api.Contracts, Web.Contracts, Web.Domain, Grpc.Contracts, Grpc.Domain)
- Updated AdditionalFiles mixin paths from Mixins/ to mixins/ in Contracts csproj and consumers
- Updated both .slnx files
- Namespaces remain unchanged (TimeWarp.Architecture.Common.*) — rename to TimeWarp.Foundation.* is task 051
- Added missing PackageVersion entries to root Directory.Packages.props for CPM compliance

### Files changed
- source/common/foundation-contracts/ (new, 26+ source files)
- source/common/foundation-domain/ (new, 6 source files)
- source/common/Directory.Build.props (new, CA suppressions)
- source/common/foundation-contracts/foundation-contracts.csproj (renamed, updated Mixins paths)
- source/common/foundation-domain/foundation-domain.csproj (renamed, minimal)
- Various referencing .csproj files (path updates)
- Both .slnx files updated
- Directory.Packages.props (added CPM entries)

### Key decisions
- Directory name: foundation-contracts/foundation-domain (aligned with future TimeWarp.Foundation.* namespace)
- Mixin file names kept PascalCase (Roslyn convention), directory renamed to mixins/ (kebab-case)
- Generated/ directories skipped (gitignored, auto-regenerated)
- CA warnings suppressed in Directory.Build.props rather than individual files (pre-existing code patterns)

### Build verification
- timewarp-architecture.slnx: Build succeeded, 0 warnings, 0 errors
- Individual projects: foundation-contracts and foundation-domain build clean
