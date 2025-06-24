# 030: Fix Web.Spa TypeScript Directory Naming

## Description

Fix inconsistent TypeScript directory naming in Web.Spa to align with project PascalCase conventions. Currently mixing lowercase, camelCase, and PascalCase within the same TypeScript project structure.

## Parent

029_Create-Directory-Naming-Convention-Analysis-Report

## Requirements

- Standardize TypeScript directory naming to PascalCase
- Update any import/reference paths affected by directory renames
- Ensure build and runtime functionality remains intact
- Update any configuration files that reference the old paths

## Current Issues

**Location**: `/Source/ContainerApps/Web/Web.Spa/source/`

**Current Inconsistency**:
```
source/                               # lowercase
├── Spa.ts                           # PascalCase file
├── Web.Spa.lib.module.ts            # Mixed case with dots
├── features/                        # camelCase directory
│   └── Counter.ts                   # PascalCase file
└── types/                           # camelCase directory
    ├── Constants.d.ts               # PascalCase file
    └── global.d.ts                  # lowercase file
```

**Problems**:
1. Parent directory `source/` is lowercase when everything else is PascalCase
2. `global.d.ts` is lowercase when `Constants.d.ts` is PascalCase
3. Mix of camelCase directories with PascalCase files is jarring
4. Deviates from project-wide PascalCase standard

## Target Structure

**Better Approach**:
```
Source/                              # Consistent with project naming
├── Spa.ts                          
├── Web.Spa.lib.module.ts            
├── Features/                        # PascalCase for consistency
│   └── Counter.ts                   
└── Types/                           # PascalCase for consistency
    ├── Constants.d.ts               
    └── Global.d.ts                  # Consistent PascalCase
```

## Implementation Plan

### Phase 1: Directory Renames
- [ ] Rename `source/` → `Source/`
- [ ] Rename `source/features/` → `Source/Features/`
- [ ] Rename `source/types/` → `Source/Types/`
- [ ] Rename `Source/Types/global.d.ts` → `Source/Types/Global.d.ts`

### Phase 2: Update References
- [ ] Update any TypeScript import statements referencing old paths
- [ ] Update build configuration files (webpack, vite, etc.) 
- [ ] Update any MSBuild/project files referencing the paths
- [ ] Update any documentation referencing the old structure

### Phase 3: Validation
- [ ] Verify TypeScript compilation succeeds
- [ ] Verify build processes work correctly
- [ ] Run tests to ensure no runtime issues
- [ ] Verify Web.Spa loads and functions properly in browser

## Files Likely Affected

**Configuration Files to Check**:
- `Web.Spa.csproj` - May reference source paths
- `tsconfig.json` - TypeScript configuration
- Any webpack/vite configuration
- MSBuild targets files
- Package.json scripts

**Code Files to Check**:
- Any TypeScript files with relative imports
- Any C# files that reference the TypeScript structure
- Build scripts or automation

## Validation Checklist

- [ ] `npm run build` succeeds without errors
- [ ] `npm run lint` passes
- [ ] TypeScript compilation clean (no path resolution errors)
- [ ] Web.Spa launches successfully
- [ ] All features function correctly in browser
- [ ] No broken imports or missing modules

## Risk Assessment

**Low Risk Changes**:
- Directory renames (if imports are relative and properly updated)
- File case changes (Global.d.ts)

**Medium Risk Areas**:
- Build configuration may hard-code paths
- MSBuild integration points

**Mitigation**:
- Test thoroughly after each rename
- Keep git history clean for easy rollback
- Update imports immediately after directory renames

## Success Criteria

1. All TypeScript directories follow PascalCase convention
2. All TypeScript files follow PascalCase convention  
3. Build process works without modification
4. No broken imports or references
5. Web.Spa functions identical to before changes
6. Aligns with project-wide directory naming standard

## Notes

This addresses the valid criticism from the Directory Naming Analysis that Web.Spa mixes conventions unnecessarily. While JavaScript ecosystem often uses camelCase, this is a .NET project template where PascalCase should take precedence for consistency.

The change eliminates the cognitive overhead of remembering different naming rules for different parts of the same .NET solution.