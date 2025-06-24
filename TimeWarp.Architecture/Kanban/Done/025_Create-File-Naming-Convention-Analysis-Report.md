# Task 025: Create File Naming Convention Analysis Report

## Overview
Generate a comprehensive analysis report of existing file naming conventions across the TimeWarp.Architecture project to identify inconsistencies and establish a standardized approach.

## Problem Statement
The project currently lacks a documented file naming convention, which may lead to inconsistent naming patterns across different areas of the codebase. This analysis will provide the foundation for creating an ADR and migration path to standardize file naming.

## Scope
1. **Kanban Folder Analysis**: Document existing naming patterns in the Kanban task management system
2. **Project-wide Analysis**: Analyze file naming patterns across the entire TimeWarp.Architecture project
3. **Consistency Evaluation**: Identify inconsistencies and patterns that need standardization
4. **Report Generation**: Create a comprehensive report with findings and recommendations

## Analysis Areas

### 1. Kanban Task Files
- Current pattern: `###_Task-Name-With-Hyphens.md` (e.g., `001_Fix-FastEndpointSourceGenerator.md`)
- Backlog pattern: `B###_Task-Name-With-Hyphens.md` (e.g., `B001_Create-Strongly-Typed-Id-Mixin.md`)
- Sub-task pattern: `###_###_subtask-name.md` (e.g., `004_001_convert-weatherforecast-to-fastendpoints.md`)

### 2. Project Structure Files
- Source code files (.cs)
- Configuration files (.json, .xml, .yml)
- Documentation files (.md)
- Build scripts (.ps1, .sh)
- Test files

### 3. Key Patterns to Analyze
- Case conventions (PascalCase, camelCase, kebab-case, snake_case)
- Separator usage (hyphens, underscores, periods)
- Numbering schemes
- File extension conventions
- Directory naming patterns

## Deliverables
1. **Analysis Report**: Detailed findings of current naming patterns
2. **Inconsistency Map**: Areas where naming conventions conflict
3. **Recommendations**: Proposed standardized naming conventions
4. **Migration Impact**: Assessment of changes needed for consistency

## Success Criteria
- [ ] Complete inventory of file naming patterns in Kanban folder
- [ ] Complete inventory of file naming patterns across entire project
- [ ] Identification of all inconsistencies
- [ ] Documented recommendations for standardization
- [ ] Foundation established for creating ADR on file naming conventions

## Follow-up Tasks
- Create ADR for file naming conventions based on analysis
- Develop migration plan for inconsistent files
- Implement automated checks for naming convention compliance

## Implementation Notes

### Completed Analysis (2025-06-24)

**Branch**: `Cramer/2025-06-24/Task_025`

#### Analysis Results Summary
- **Total Files Analyzed**: 350+ files across entire project
- **Overall Compliance Rate**: 99%
- **Critical Issues Identified**: 4 files in Kanban system
- **Report Generated**: `File-Naming-Convention-Analysis-Report.md`

#### Key Findings
1. **Excellent C# Convention Compliance**: 100% PascalCase adherence across all source files
2. **Strong Project Structure**: Consistent dot notation for all `.csproj` files
3. **Kanban Inconsistencies**: 4 files need case correction (sub-tasks using lowercase)
4. **Infrastructure Standards**: Minor clarifications needed for Kubernetes YAML files

#### Critical Issues Requiring Immediate Fix
1. **Sub-task Case**: `004_001_convert-weatherforecast-to-fastendpoints.md` → `004_001_Convert-WeatherForecast-To-FastEndpoints.md`
2. **Duplicate Task Numbers**: Two tasks with `001_` prefix - need to renumber one
3. **Single Main Task**: `004_migrate-api-to-fastendpoints.md` → `004_Migrate-Api-To-FastEndpoints.md`

#### Next Steps Recommended
1. Create ADR for file naming conventions based on this analysis
2. Fix the 4 identified Kanban file naming issues
3. Implement automated naming convention validation
4. Update Task-Template.md with proper naming guidelines

#### Analysis Methodology
- Used comprehensive file system analysis across all directories
- Categorized files by type and technology stack
- Identified patterns within each category
- Documented inconsistencies with specific examples
- Provided migration path and impact assessment

**Status**: Analysis complete, comprehensive report delivered

## Notes
This analysis will inform the creation of a comprehensive file naming convention ADR that ensures consistency across the entire TimeWarp.Architecture ecosystem.