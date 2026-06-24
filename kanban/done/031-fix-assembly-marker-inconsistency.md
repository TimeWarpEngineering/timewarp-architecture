# 031: Fix Assembly Marker Inconsistency

## Description

Fix inconsistent assembly marker naming and purpose across projects. Some assemblies have `AssemblyMarker.cs`, others have `AssemlyAnnotations.cs` (with typo), and there's unclear consistency about when to use which pattern.

## Parent

029_Create-Directory-Naming-Convention-Analysis-Report

## Requirements

- Standardize assembly marker file naming across all projects
- Fix the typo in `AssemlyAnnotations.cs` → `AssemblyAnnotations.cs`
- Establish clear guidelines for when to use AssemblyMarker vs AssemblyAnnotations
- Ensure all projects follow the established pattern consistently

## Current Issues

**Location**: Various assembly projects

**Inconsistent Naming**:
```
Web.Contracts/
├── AssemlyAnnotations.cs            # ← TYPO + Different name
└── ...

Web.Application/  
├── AssemblyMarker.cs                # ← Correct name
└── ...
```

**Problems**:
1. **Typo**: `AssemlyAnnotations.cs` should be `AssemblyAnnotations.cs`  
2. **Inconsistent Purpose**: Why `AssemblyAnnotations` vs `AssemblyMarker`?
3. **No Clear Pattern**: When to use which approach?

## Investigation Required

### Phase 1: Audit Current State
- [ ] Find all `AssemblyMarker.cs` files across the solution
- [ ] Find all `AssemlyAnnotations.cs` files (with typo)
- [ ] Find all `AssemblyAnnotations.cs` files (correct spelling)
- [ ] Document which projects use which pattern
- [ ] Examine the contents of each to understand purpose differences

### Phase 2: Determine Correct Pattern
- [ ] Review project documentation for assembly marker guidelines
- [ ] Check if AssemblyMarker vs AssemblyAnnotations serve different purposes
- [ ] Determine if this follows a specific .NET or TimeWarp convention
- [ ] Decide on single standard or document when to use each

### Phase 3: Standardization
- [ ] Fix typo: `AssemlyAnnotations.cs` → `AssemblyAnnotations.cs`
- [ ] Standardize naming pattern across all assemblies
- [ ] Update any code that references the old names
- [ ] Update build files or reflection code that might reference these files

## Files to Investigate

**Known Issues**:
- `Source/ContainerApps/Web/Web.Contracts/AssemlyAnnotations.cs` (typo)
- `Source/ContainerApps/Web/Web.Application/AssemblyMarker.cs` (correct)

**Projects to Check**:
- All projects under `Source/Common/`
- All projects under `Source/ContainerApps/`
- All projects under `Source/Analyzers/`
- All projects under `Source/Libraries/`

## Expected Pattern

Based on CLAUDE.md guidance: "Each assembly must contain a sealed `AssemblyMarker` class for reflection operations"

**Likely Standard**:
```csharp
// AssemblyMarker.cs
using System.Reflection;

[assembly: AssemblyMetadata("IsTrimmable", "true")]

namespace TimeWarp.Architecture.{ProjectName};

/// <summary>
/// Assembly marker for reflection and assembly identification.
/// </summary>
public sealed class AssemblyMarker
{
}
```

## Implementation Plan

### Step 1: Fix Immediate Typo
- [ ] Rename `Web.Contracts/AssemlyAnnotations.cs` → `Web.Contracts/AssemblyAnnotations.cs`
- [ ] Update any references to the old filename
- [ ] Commit this fix immediately to prevent further typo propagation

### Step 2: Comprehensive Audit
- [ ] Search entire solution for assembly marker patterns
- [ ] Document current state in spreadsheet/table format
- [ ] Identify content differences between AssemblyMarker and AssemblyAnnotations

### Step 3: Standardization Decision
- [ ] Choose single pattern (likely AssemblyMarker based on CLAUDE.md)
- [ ] Document the standard in CLAUDE.md or ADR if not already documented
- [ ] Create template/example for future projects

### Step 4: Mass Rename/Standardization
- [ ] Rename all non-standard files to use chosen pattern
- [ ] Ensure file contents follow standard template
- [ ] Update any reflection code that references these classes
- [ ] Verify build succeeds after all changes

## Validation Checklist

- [ ] All assembly projects have exactly one assembly marker file
- [ ] All assembly marker files use consistent naming
- [ ] No typos in any assembly marker filenames
- [ ] All assembly marker classes follow same pattern/template
- [ ] Solution builds successfully after changes
- [ ] No broken references to old assembly marker names
- [ ] All reflection code works with new naming

## Success Criteria

1. **Consistent Naming**: All projects use same assembly marker filename pattern
2. **No Typos**: All assembly marker files have correct spelling
3. **Clear Purpose**: Single, well-documented pattern for assembly markers
4. **Complete Coverage**: Every assembly has appropriate marker file
5. **Working Code**: No broken builds or reflection issues after changes

## Risk Assessment

**Low Risk**:
- Fixing the typo (simple rename)
- Standardizing filenames

**Medium Risk**:
- Changing class names referenced in reflection code
- Build scripts that might reference specific filenames

**Mitigation**:
- Search for all references before renaming
- Test build after each change
- Use git to track exactly what was changed

## Notes

This is a good catch - having typos in foundational files like assembly markers reflects poorly on code quality and can cause confusion for developers. The inconsistency suggests this pattern may not be well-documented or enforced through templates/tooling.

This fix will improve professionalism and consistency across the template, preventing propagation of typos to projects created from this template.