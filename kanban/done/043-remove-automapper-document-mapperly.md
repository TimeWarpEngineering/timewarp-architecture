# 043: Remove AutoMapper and Document Mapperly as Preferred Mapping Library

## Description

Remove unused `AutoMapper` dependency from the solution and document `Mapperly` as the preferred object mapping library. AutoMapper is registered but never actually used (no Profile classes, no IMapper usage found).

## Context

AutoMapper 13.0.1 is referenced and registered in Web.Server but analysis shows zero actual usage throughout the codebase. Mapperly (Riok.Mapperly 4.1.1) is already referenced in Web.Application as the preferred mapping solution. This is a cleanup task to remove dead dependencies.

## Requirements

- Remove AutoMapper package from Directory.Packages.props
- Remove AutoMapper configuration and registration code
- Remove AutoMapper package references from project files
- Verify build succeeds without AutoMapper
- Document Mapperly as the standard mapping approach

## Checklist

### Package Management
- [x] Remove `AutoMapper` from Directory.Packages.props (version 13.0.1)
- [x] Remove AutoMapper package reference from Web.Server.csproj
- [x] Verify Riok.Mapperly is properly referenced in Web.Application.csproj (version 4.1.1)

### Code Changes
- [x] Remove AutoMapper global using from Web.Server/GlobalUsings.cs
  - [x] Remove `global using AutoMapper;` statement
- [x] Remove AutoMapper registration from Web.Server/Program.cs
  - [x] Remove `serviceCollection.AddAutoMapper(typeof(TimeWarp.Architecture.Web.Application.IAssemblyMarker).Assembly);` call
- [x] Verify no AutoMapper Profile classes exist (analysis confirmed none found)
- [x] Verify no IMapper usage exists (analysis confirmed none found)

### Testing
- [x] Build solution to verify no compilation errors
- [x] Run test suites to ensure no regressions
- [x] Verify Web.Server starts successfully without AutoMapper

### Documentation
- [x] Document Mapperly as preferred mapping library (if not already documented)
- [x] Add example of Mapperly usage for future reference (optional)

## Notes

**Important Considerations:**
- AutoMapper is a "zombie dependency" - registered but never used
- No actual mapping code to migrate
- Mapperly uses compile-time source generation (better performance than AutoMapper's reflection)
- Mapperly provides type-safe mappings with compile-time validation
- This removal has zero risk since no code depends on AutoMapper

**Affected Files:**
- `Directory.Packages.props` - Remove AutoMapper package entry (line 15)
- `Source/ContainerApps/Web/Web.Server/Web.Server.csproj` - Remove PackageReference (line 27)
- `Source/ContainerApps/Web/Web.Server/GlobalUsings.cs` - Remove global using (line 1)
- `Source/ContainerApps/Web/Web.Server/Program.cs` - Remove AddAutoMapper call (line 111)

**Current State:**
- AutoMapper Version: 13.0.1 (2 major versions behind latest 15.1.0)
- Mapperly Version: 4.1.1 (already referenced in Web.Application)
- Latest Mapperly: 4.3.0 (minor update available)

## Implementation Notes

### Analysis Results

**AutoMapper Usage Search:**
- ✅ Zero Profile classes found
- ✅ Zero IMapper injections found
- ✅ Zero CreateMap/ForMember calls found
- ✅ Zero mapping operations found
- ✅ Only registration code exists (never executed in practice)

**Mapperly Status:**
- Already referenced in Web.Application.csproj
- No mappers implemented yet
- Ready to be used when mapping becomes necessary

### Future Mapping Strategy

When object mapping becomes necessary:
1. Use Riok.Mapperly (already referenced)
2. Create partial mapper classes with `[Mapper]` attribute
3. Define mapping methods - Mapperly generates implementation at compile-time
4. Example:
```csharp
[Mapper]
public partial class UserMapper
{
    public partial UserDto MapToDto(User user);
}
```

## Definition of Done

- ✅ AutoMapper completely removed from solution
- ✅ All tests passing (Analyzer: 8 passed, Common: 1 passed, API Integration: 6 passed)
- ✅ Build succeeds without AutoMapper (0 warnings, 0 errors)
- ✅ Web.Server runs without AutoMapper registration
- ✅ No compilation errors or warnings related to removal

## Completion Summary

**Date Completed:** 2025-11-09

**Changes Committed:**
- Commit: `184c2e54` - Remove unused AutoMapper dependency
- Files Modified: 4 files changed, 1 insertion(+), 5 deletions(-)

**Files Changed:**
1. [Directory.Packages.props](Directory.Packages.props) - Removed AutoMapper 13.0.1 package entry
2. [Web.Server.csproj](Source/ContainerApps/Web/Web.Server/Web.Server.csproj) - Removed AutoMapper package reference
3. [Web.Server/GlobalUsings.cs](Source/ContainerApps/Web/Web.Server/GlobalUsings.cs:1) - Removed `global using AutoMapper;`
4. [Web.Server/Program.cs](Source/ContainerApps/Web/Web.Server/Program.cs:111) - Removed `AddAutoMapper()` registration

**Test Results:**
- Build: ✅ Succeeded (16.20 seconds, 0 warnings, 0 errors)
- Analyzer Tests: ✅ 8 passed
- Common Tests: ✅ 1 passed
- API Integration Tests: ✅ 6 passed, 1 skipped

**Impact:**
- Zero risk removal - no code was using AutoMapper
- Reduced startup overhead from unnecessary assembly scanning
- Clarified Mapperly as the chosen mapping library
