# Task: 024 = Migrate Repository to Hybrid Task Management Approach

## Description

Implement the hybrid Kanban + GitHub Issues approach documented in ADR-0006 by removing the Process folder methodology and preparing repository for GitHub Issues integration.

## Requirements

- [x] Remove Process folder entirely (no historical preservation needed)
- [x] Extract valuable patterns from Process folder before deletion:
  - [x] Integrate Definition of Done into Kanban/Overview.md
  - [x] Preserve ToastNotification.md example as task template reference
  - [x] Extract Definition of Ready concepts
- [x] Update template files to remove Process references
- [x] Enable GitHub Issues with appropriate templates
- [x] Update documentation to reflect hybrid approach

## Acceptance Criteria

- [x] Process folder completely removed from repository
- [x] Kanban/Overview.md enhanced with Definition of Done/Ready
- [x] GitHub Issues enabled with issue templates
- [x] No references to Process methodology in template files
- [x] ADR-0006 migration section can be removed (task completion replaces it)
- [x] Template instances will only offer Kanban approach going forward

## Implementation Notes

### Process Folder Content to Extract

**Definition of Done** (Process/DefinitionOfDone.md):
- API endpoint completion criteria
- Client feature completion criteria
- Testing requirements

**Definition of Ready** (Process/DefinitionOfReady.md):
- Task readiness criteria
- Prerequisites for moving to ToDo

**User Story Example** (Process/Backlog/CurrentIteration/Cramer/ToastNotification.md):
- Well-structured narrative format
- Acceptance criteria examples

### GitHub Issues Setup

- Create issue templates for:
  - Bug reports
  - Feature requests  
  - AI agent tasks
- Enable GitHub Projects for public roadmap
- Configure labels for categorization

### Template Clean-up

- Remove Process folder references from documentation
- Update Claude.md to reflect hybrid approach
- Ensure new template instances only include Kanban

## Related Work

- Implements ADR-0006 hybrid approach decision
- Connects to Task 023 (ADR creation)
- Prepares foundation for AI agent integration

## Status

Ready to implement hybrid approach migration.