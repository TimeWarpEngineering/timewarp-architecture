# 026: Fix Critical Kanban Inconsistencies

## Description

Fix the 4 critical naming inconsistencies identified in the file naming convention analysis report to achieve consistent PascalCase naming across all Kanban task files.

## Parent

025_Create-File-Naming-Convention-Analysis-Report

## Requirements

- Fix all 4 identified Kanban naming inconsistencies
- Preserve git history using `git mv` for renames
- Update any references to renamed files
- Maintain task numbering uniqueness

## Checklist

### Critical Issues to Fix
- [ ] Fix sub-task case inconsistency (3 files):
  - [ ] `004_001_convert-weatherforecast-to-fastendpoints.md` → `004_001_Convert-WeatherForecast-To-FastEndpoints.md`
  - [ ] `004_002_remove-mediatr-from-api.md` → `004_002_Remove-MediatR-From-Api.md`
  - [ ] `004_003_implement-openapi-and-scalar.md` → `004_003_Implement-OpenApi-And-Scalar.md`

### Duplicate Task Numbers
- [ ] Resolve duplicate task numbers:
  - [ ] Keep `001_Fix-FastEndpointSourceGenerator.md`
  - [ ] Rename `001_NavLink-Encapsulation-Implementation.md` to next available number
  - [ ] Update any cross-references to the renumbered task

### Single Lowercase Exception
- [ ] Fix main task case inconsistency:
  - [ ] `004_migrate-api-to-fastendpoints.md` → `004_Migrate-Api-To-FastEndpoints.md`

### Verification
- [ ] Verify all Kanban task files use proper PascalCase naming
- [ ] Confirm no duplicate task numbers exist
- [ ] Test that all references still work correctly
- [ ] Update analysis report if needed

## Notes

This is Phase 1 of the Implementation Timeline from the file naming convention analysis. Should be completed in 1 day with low risk as it only affects Kanban folder organization.

**Priority**: High - These inconsistencies break the established naming conventions and should be fixed immediately.

**Risk Level**: Low - File renames in Kanban folder with no impact on source code.

## Implementation Notes

[To be updated during task execution]