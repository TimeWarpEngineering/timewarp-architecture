# Kanban-Based Development Process

## Context and Problem Statement

Development teams need a structured approach to manage tasks, track progress, and maintain visibility into work in progress. The repository currently contains two different task management approaches: a Process-based system and a Kanban-based system. These parallel systems create confusion, duplicate effort, and make it unclear which process should be followed for new work.

Which task management and development process should we standardize on for the TimeWarp Architecture repository?

## Considered Options

### Option 1: Sprint-Based Process System
The existing Process folder structure with:
* **Sprint/iteration planning**: Formal iteration boundaries with per-developer folders
* **Heavy documentation**: Definition of Done/Ready, user stories, iteration summaries
* **Multi-developer structure**: Dedicated folders for up to 4 developers per iteration  
* **Historical tracking**: Past iterations preserved in archive structure
* **Traditional agile**: Formal sprint planning, review, and retrospective cycles

### Option 2: GitHub Issues + Projects
Native GitHub project management with:
* **Issue tracking**: Rich issue templates, labels, milestones, assignees
* **Project boards**: Kanban-style boards with automation rules
* **Integration**: Direct link to PRs, commits, and code references
* **Collaboration**: Comments, mentions, notifications, and external visibility
* **Reporting**: Built-in metrics, burndown charts, and velocity tracking

### Option 3: Folder-Based Kanban System
A file-system Kanban implementation with:
* **Simple workflow**: Backlog → ToDo → InProgress → Done folders
* **Version controlled**: Task history tracked in git alongside code changes
* **Self-contained**: No external dependencies or tool integration required
* **Developer-friendly**: Uses familiar file system and markdown tools
* **Branch integration**: Task numbers align with branch naming conventions
* **Continuous flow**: Work moves organically through states without artificial boundaries
* **Rapid task creation**: Can quickly capture ideas and convert to actionable tasks
* **Zero tool overhead**: Uses familiar file system and markdown
* **Offline capable**: Works without external dependencies
* **Search friendly**: File-based system enables powerful text search
* **Automation ready**: Can be scripted and automated with git hooks

### Option 4: Hybrid Approach
Combine multiple systems for different work types

### Option 5: External Tool Integration
Use dedicated project management tools (Jira, Azure DevOps, Linear, etc.)

## Decision Outcome

Chosen option: **Hybrid: Folder-Based Kanban + GitHub Issues**, because:

### AI-Assisted Development Context
* **Sprint obsolescence**: With AI assistance, development velocity is unpredictable and sprints become artificial constraints
* **Continuous delivery**: AI enables rapid iteration cycles that don't align with fixed sprint boundaries
* **Dynamic prioritization**: Work can be reprioritized instantly based on insights or blockers discovered during development
* **Context switching optimization**: AI helps maintain context across multiple tasks, reducing traditional WIP concerns

### Hybrid Approach: Best of Both Worlds

**AI Tool Transparency:**
* **File-based visibility**: Folder-based Kanban enables AI tools to easily search and understand current work state
* **Context awareness**: AI assistants can quickly scan task files to understand project priorities and progress
* **No API barriers**: Direct file access eliminates need for GitHub API integration for AI analysis
* **Real-time insight**: AI tools see task evolution alongside code changes in git history

**GitHub Issues for External Interface:**
* **Public visibility**: Community contributions, bug reports, feature requests
* **AI agent integration**: Create issues and mention AI agents (Claude, etc.) for automated work
* **Cross-repository coordination**: Issues that span multiple repositories
* **Stakeholder communication**: Professional interface for external collaboration
* **Automated workflows**: AI agents can create branches, implement features, and submit PRs
* **Rich linking**: Cross-references between issues, PRs, and commits
* **Community engagement**: Public roadmap and transparent development process

**Folder-Based Kanban for Internal Development:**
* **Rapid iteration**: Quick task creation and management for AI-assisted development
* **Version controlled**: Task evolution tracked alongside code changes
* **Zero tool overhead**: Uses familiar file system and markdown
* **Offline capable**: Works without external dependencies
* **Branch integration**: Task numbers align perfectly with branch naming (ADR-0004)


## Implementation Details

### Hybrid Workflow

**GitHub Issues for:**
* External collaboration and community engagement
* AI agent task assignment and automation
* Cross-repository coordination
* Public roadmap and feature requests
* Bug reports from users

**Folder-Based Kanban for:**
* Internal development tasks and rapid iteration
* AI-assisted development workflow
* Technical debt and refactoring tasks
* Architecture and design decisions
* Template development and maintenance

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

## Template Evolution

This repository will evolve to support only the hybrid approach:
* **Process folder removal**: Sprint-based methodology will be removed from template
* **Kanban enhancement**: Extract valuable patterns from Process folder into Kanban documentation  
* **GitHub Issues integration**: Enable issue templates and AI agent workflows
* **Template simplification**: New instances will offer only the hybrid approach

Implementation details are tracked in separate Kanban tasks.

### AI Agent Integration

**GitHub Issues enable powerful AI automation:**
* **Issue creation**: Create issue describing feature or bug fix needed
* **AI agent mention**: Comment mentioning @claude or other AI agents
* **Automated development**: AI agent creates branch, implements solution, submits PR
* **Code review integration**: AI agents can respond to review feedback
* **Continuous improvement**: AI agents learn from issue patterns and solutions

**Example workflow:**
1. User creates GitHub issue: "Add dark mode toggle to settings page"
2. Maintainer comments: "@claude please implement this feature"
3. AI agent creates branch, implements feature, adds tests, submits PR
4. Human reviews and merges, closing the issue automatically

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

*"We choose a hybrid approach combining folder-based Kanban for internal development with GitHub Issues for external collaboration and AI agent integration, maximizing both development velocity and community engagement."*