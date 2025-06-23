# Task: 024 = Migrate Repository to Hybrid Task Management Approach

## Description

Implement the hybrid Kanban + GitHub Issues approach documented in ADR-0006 by removing the Process folder methodology and preparing repository for GitHub Issues integration.

## Requirements

- [ ] Remove Process folder entirely (no historical preservation needed)
- [ ] Extract valuable patterns from Process folder before deletion:
  - [ ] Integrate Definition of Done into Kanban/Overview.md
  - [ ] Preserve ToastNotification.md example as task template reference
  - [ ] Extract Definition of Ready concepts
- [ ] Update template files to remove Process references
- [ ] Enable GitHub Issues with appropriate templates
- [ ] Update documentation to reflect hybrid approach

## Acceptance Criteria

- [ ] Process folder completely removed from repository
- [ ] Kanban/Overview.md enhanced with Definition of Done/Ready
- [ ] GitHub Issues enabled with issue templates
- [ ] No references to Process methodology in template files
- [ ] ADR-0006 migration section can be removed (task completion replaces it)
- [ ] Template instances will only offer Kanban approach going forward

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
- Update CLAUDE.md to reflect hybrid approach
- Ensure new template instances only include Kanban

## Related Work

- Implements ADR-0006 hybrid approach decision
- Connects to Task 023 (ADR creation)
- Prepares foundation for AI agent integration

## Status

Ready to implement hybrid approach migration.