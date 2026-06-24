# Branch Naming Conventions

## Context and Problem Statement

Development teams need consistent branch naming conventions to maintain organization, enable automation, and provide clear context about the work being performed. Without standardized naming, branches become difficult to identify, organize, and manage, especially in repositories with multiple contributors and long-term development cycles.

Which branch naming convention should we adopt for the TimeWarp Architecture repository?

## Considered Options

* **Flat naming**: Simple descriptive names without structure (e.g., `feature-auth`, `bugfix-db`)
* **Issue-based**: Branch names tied to issue numbers (e.g., `issue-123`, `gh-456`)
* **Hierarchical with author**: Include developer identification (e.g., `author/feature/description`)
* **Date-based with author**: Include creation date for temporal organization (e.g., `author/YYYY-MM-DD/description`)
* **GitFlow convention**: Standard GitFlow prefixes (e.g., `feature/`, `hotfix/`, `release/`)

## Decision Outcome

Chosen option: **Date-based with author naming convention**, because:

* **Clear ownership**: Developer name provides immediate context about who is working on the branch
* **Temporal organization**: Date component enables chronological tracking and cleanup
* **Task integration**: Aligns with our Kanban task management system
* **Conflict avoidance**: Unique author/date combination prevents naming conflicts
* **Historical context**: Easy to understand when work was started and by whom
* **Automation friendly**: Structured format enables scripting and automation

## Standard Format

## Main Branch

- **Main Branch**: `master` (not `main`)
- Direct commits to `master` are prevented via git hooks
- All feature work should be done in separate branches and merged via pull requests

## Branch Naming Pattern

### Standard Format

```
{Author}/{YYYY-MM-DD}/{Description}
```

**Components:**
- **Author**: Developer's name or identifier (e.g., `Cramer`, `Smith`)
- **Date**: Branch creation date in `YYYY-MM-DD` format
- **Description**: Task identifier or feature description

### Examples

**Task-based branches:**
```
Cramer/2025-06-18/Task_019
Cramer/2025-06-20/Task_022
```

**Feature-based branches:**
```
Cramer/2025-06-15/AddUserAuthentication
Cramer/2025-06-12/UpdateSwagger
Cramer/2025-06-10/RefactorApiEndpoints
```

**Bug fix branches:**
```
Cramer/2025-06-14/Fix_DatabaseConnection
Cramer/2025-06-16/Fix_ValidationError
```

## Branch Types and Descriptions

### Task Branches
Used when working on numbered tasks from the Kanban system:
- **Format**: `{Author}/{Date}/Task_{###}`
- **Example**: `Cramer/2025-06-18/Task_019`
- **Usage**: Corresponds to tasks in `Kanban/InProgress/` or `Kanban/ToDo/`

### Feature Branches
Used for new features or enhancements:
- **Format**: `{Author}/{Date}/{FeatureName}`
- **Example**: `Cramer/2025-06-15/AddUserDashboard`
- **Naming**: Use PascalCase for multi-word features

### Bug Fix Branches
Used for bug fixes:
- **Format**: `{Author}/{Date}/Fix_{BugDescription}`
- **Example**: `Cramer/2025-06-14/Fix_DatabaseTimeout`
- **Naming**: Prefix with `Fix_` followed by brief description

### Experimental Branches
Used for experimental work or proof-of-concepts:
- **Format**: `{Author}/{Date}/Experiment_{Description}`
- **Example**: `Cramer/2025-06-13/Experiment_NewStateManagement`
- **Naming**: Prefix with `Experiment_`

## Naming Guidelines

### Do's
- ✅ Use forward slashes (`/`) as separators
- ✅ Use consistent date format: `YYYY-MM-DD`
- ✅ Use PascalCase for multi-word descriptions
- ✅ Keep descriptions concise but descriptive
- ✅ Use task numbers when working on Kanban tasks
- ✅ Include fix context for bug fix branches

### Don'ts
- ❌ Don't use special characters except underscores and hyphens
- ❌ Don't use spaces in branch names
- ❌ Don't create branches directly from master without following the pattern
- ❌ Don't use abbreviations that aren't clear
- ❌ Don't omit the date component

## Workflow Integration

### With Kanban Tasks
When working on tasks from the Kanban system:
1. Move task from `Kanban/ToDo/` to `Kanban/InProgress/`
2. Create branch: `{Author}/{Date}/Task_{TaskNumber}`
3. Complete work and create pull request
4. After merge, move task to `Kanban/Done/`

### Pull Request Titles
Pull request titles should reflect the branch purpose:
- **Task**: `Task 019: Implement user authentication`
- **Feature**: `Add user dashboard with analytics`
- **Bug Fix**: `Fix database connection timeout issue`

## Git Commands

### Creating a New Branch
```bash
# For a task
git checkout -b "Cramer/2025-06-18/Task_019"

# For a feature
git checkout -b "Cramer/2025-06-18/AddUserDashboard"

# For a bug fix
git checkout -b "Cramer/2025-06-18/Fix_DatabaseTimeout"
```

### Pushing to Remote
```bash
git push -u origin "Cramer/2025-06-18/Task_019"
```

## Branch Lifecycle

1. **Creation**: Create branch from latest `master`
2. **Development**: Make commits following conventional commit patterns
3. **Testing**: Ensure all tests pass before creating PR
4. **Pull Request**: Create PR with descriptive title and summary
5. **Review**: Address any review feedback
6. **Merge**: Squash merge into `master`
7. **Cleanup**: Delete local and remote feature branch after merge

## Enforcement

Currently, branch naming is enforced through:
- Code review process
- Documentation standards
- Team conventions

Future enhancements may include:
- Git hooks to validate branch names
- GitHub Actions to check branch naming patterns
- Automated branch cleanup for merged branches

## Related Documentation

- [How to Prevent Local Commits to Master](HowToPreventLocalCommitsToMaster.md)
- [How to Rename Default Branch From Main to Master](HowToRenameDefaultBranchFromMainToMaster.md)
- [Task Management Workflow](../Conceptual/Overview.md#task-management-workflow)

## Examples from Repository History

Historical examples that follow this pattern:
- `Cramer/2025-06-18/Task_019`
- `Cramer/2020-02-05/UpdateTo3.2.0-preview1`
- `Cramer/2019-11-24/AddGlobal`
- `Cramer/2020-06-03/Swagger`

---

*This convention ensures consistency, enables better branch organization, and provides clear context about the work being performed on each branch.*