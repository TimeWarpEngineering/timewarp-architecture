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
- [x] Rename `source/` → `Source/`
- [x] Rename `source/features/` → `Source/Features/`
- [x] Rename `source/types/` → `Source/Types/`
- [x] Rename `Source/Types/global.d.ts` → `Source/Types/Global.d.ts`

### Phase 2: Update References
- [x] Update any TypeScript import statements referencing old paths
- [x] Update build configuration files (webpack, vite, etc.) 
- [x] Update any MSBuild/project files referencing the paths
- [x] Update any documentation referencing the old structure

### Phase 3: Validation
- [x] Verify TypeScript compilation succeeds
- [x] Verify build processes work correctly
- [x] Run tests to ensure no runtime issues
- [x] Verify Web.Spa loads and functions properly in browser

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

- [x] `npm run build` succeeds without errors
- [x] `npm run lint` passes
- [x] TypeScript compilation clean (no path resolution errors)
- [x] Web.Spa launches successfully
- [x] All features function correctly in browser
- [x] No broken imports or missing modules

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

## Resolution

**Status**: ✅ **COMPLETED** (2025-01-07)

**Changes Implemented**:
1. **Directory Structure Fixed**:
   - `source/` → `Source/` (PascalCase)
   - `source/features/` → `Source/Features/` (PascalCase)  
   - `source/types/` → `Source/Types/` (PascalCase)
   - `Source/Types/global.d.ts` → `Source/Types/Global.d.ts` (PascalCase)

2. **Configuration Updates**:
   - Updated `Web.Spa.csproj` TypeScriptInputs reference
   - Updated `tsconfig.json` rootDir and include paths
   - Updated `package.json` lint script path
   - Fixed case-sensitive build issues on Linux

3. **Validation Results**:
   - ✅ TypeScript compilation successful
   - ✅ Build processes work correctly
   - ✅ All npm scripts functional
   - ✅ Web.Spa loads and functions properly
   - ✅ No broken imports or missing modules

**Key Files Modified**:
- `/Source/ContainerApps/Web/Web.Spa/Web.Spa.csproj`
- `/Source/ContainerApps/Web/Web.Spa/tsconfig.json`
- `/Source/ContainerApps/Web/Web.Spa/package.json`
- All TypeScript source files moved to PascalCase directories

**Impact**: 
This change eliminates the cognitive overhead of remembering different naming rules for different parts of the same .NET solution. The Web.Spa TypeScript structure now consistently follows PascalCase conventions, aligning with the project-wide standard established in the Directory Naming Analysis.