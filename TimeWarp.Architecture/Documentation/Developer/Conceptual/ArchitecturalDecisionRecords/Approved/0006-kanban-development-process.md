# Kanban-Based Development Process

## Context and Problem Statement

Development teams need a structured approach to manage tasks, track progress, and maintain visibility into work in progress. The repository currently contains two different task management approaches: a Process-based system and a Kanban-based system. These parallel systems create confusion, duplicate effort, and make it unclear which process should be followed for new work.

Which task management and development process should we standardize on for the TimeWarp Architecture repository?

## Considered Options

### Option 1: Process-Based System
The existing Process folder structure with:
* Linear process documentation
* Traditional project management approach
* Documentation-heavy workflow
* Less visual representation of work states

### Option 2: Kanban Board System
A folder-based Kanban implementation with:
* Visual workflow stages (Backlog → ToDo → InProgress → Done)
* Task cards as markdown files
* Clear work-in-progress visualization
* Flexible, iterative workflow

### Option 3: Hybrid Approach
Maintain both systems for different types of work

### Option 4: External Tool Integration
Use external project management tools (Jira, Azure DevOps, etc.)

## Decision Outcome

Chosen option: **Kanban Board System**, because:

### Workflow Advantages
* **Visual workflow**: Clear representation of task states and work progression
* **WIP limits**: Natural constraints on work in progress prevent context switching
* **Flexible prioritization**: Easy to reorder tasks within columns
* **Continuous flow**: Supports iterative development without rigid sprint boundaries

### Repository Integration Benefits
* **Version controlled**: Task history tracked in git alongside code changes
* **Branch alignment**: Task numbers integrate directly with branch naming convention (ADR-0004)
* **Self-contained**: No external dependencies or tools required
* **Collaborative**: All team members can see and update task status

### Simplicity and Maintenance
* **Single source of truth**: Eliminates confusion between multiple task systems
* **Low overhead**: Minimal process ceremony, maximum flexibility
* **Developer-friendly**: Uses familiar file system and markdown
* **Automation potential**: Can be automated with scripts and git hooks

## Implementation Details

### Kanban Folder Structure
```
Kanban/
├── Backlog/     # B###_task-name.md (not ready for work)
├── ToDo/        # ###_task-name.md (ready to start)  
├── InProgress/  # ###_task-name.md (actively being worked)
└── Done/        # ###_task-name.md (completed)
```

### Task Lifecycle
1. **Creation**: New tasks start in `Backlog/` with `B###_` prefix
2. **Ready**: Move to `ToDo/` and rename to `###_` when ready to work
3. **Active**: Move to `InProgress/` when work begins
4. **Complete**: Move to `Done/` when work is finished

### Task Numbering Convention
* **Backlog tasks**: `B001_`, `B002_`, etc.
* **Active tasks**: `001_`, `002_`, etc. (remove B prefix when ready)
* **Sequential numbering**: Ensures unique identification and chronological order

### Integration with Development Workflow
* **Branch naming**: Task numbers align with branch naming (e.g., `Author/Date/Task_019`)
* **Commit references**: Commits can reference task numbers for traceability
* **Pull requests**: Link to task files for context and requirements

### Definition of Done
Tasks in `Done/` column must meet criteria appropriate to task type:

**API Endpoints:**
- Server: Endpoint, Handler, Validator, Mapper implemented
- Contracts: Request, Response, RequestValidator created
- Integration tests for Handler and Endpoint written
- Documentation updated

**Client Features:**
- State, Actions, Components/Pages implemented
- Integration tests (ShouldClone, ShouldSerialize) written
- Action tests for positive cases written
- End-to-end tests for rendering and happy paths

## Migration from Process System

### Process Folder Review
* **Preserve valuable content**: Extract reusable documentation, templates, or guidelines
* **Migrate active items**: Convert any active Process items to Kanban tasks
* **Archive**: Move Process folder to archived state once migration is complete

### Transition Plan
1. **Audit**: Review all content in Process folder
2. **Extract**: Identify valuable templates, guidelines, or documentation
3. **Convert**: Transform active Process items into Kanban tasks
4. **Integrate**: Incorporate useful Process content into Kanban workflow
5. **Sunset**: Archive or remove Process folder once migration is complete

## Positive Consequences

* **Single workflow**: Eliminates confusion about which process to follow
* **Visual clarity**: Easy to see work distribution and bottlenecks
* **Git integration**: Task management history preserved alongside code
* **Developer experience**: Familiar file-based system using preferred tools
* **Flexibility**: Can adapt to changing team size and project needs
* **Traceability**: Clear link between tasks, branches, and delivered features

## Negative Consequences

* **Learning curve**: Team needs to understand Kanban principles
* **Discipline required**: Success depends on consistent task movement and updates
* **Limited reporting**: Fewer built-in metrics compared to dedicated project management tools
* **Manual process**: Requires human discipline to move tasks between columns

## Related Decisions

* [ADR-0004: Branch Naming Conventions](0004-branch-naming-conventions.md) - Task numbers integrate with branch naming
* [ADR-0005: Git Merge Strategy](0005-git-merge-strategy.md) - Preserves complete task development history

## Future Considerations

* **Automation opportunities**: Scripts to help with task movement and reporting
* **Metrics collection**: Tools to analyze cycle time, throughput, and bottlenecks
* **Template standardization**: Common task templates for different work types
* **Integration possibilities**: Potential connections to CI/CD or notification systems

---

*"We choose Kanban for its visual clarity, git integration, and flexibility to support our iterative development approach while maintaining simplicity and developer focus."*