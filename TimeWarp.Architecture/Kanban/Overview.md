# Kanban Board

This Kanban board manages and tracks epics and tasks for the project using a simple folder structure.
Each item is represented by a Markdown file, and the status of the item is indicated by the folder it is in.

## Folders

1. **Backlog**: Contains tasks that are not yet ready to be worked on. These tasks have a temporary backlog scoped unique identifier.
   a. **Scratch** - contains epics or tasks that are works in progress or ideas.  They can be stored in folders under users names if needed.
2. **ToDo**: Contains tasks that are ready to be worked on. When a task from the Backlog becomes ready, it is assigned a unique identifier and moved to this folder.
3. **InProgress**: Contains tasks that are currently being worked on.
4. **Done**: Contains tasks that have been completed.

## File Naming Convention

### Tasks
- For tasks in the Backlog folder, use a short description with a 'B' prefix followed by a three-digit identifier,
such as `B001_research-authentication-methods.md` or `B002_design-game-rules.md`.
- When a task becomes "Ready," assign it a unique identifier (without the 'B' prefix) and move it to the ToDo folder, e.g.,
`001_implement-user-registration.md` or `002_create-game-logic.md`.
- <3 digit Id>_<short-description-separated-by-hyphens>

### Depth
001_top-level.md
001_001_second-level.md
001_001_001_third-level.md
001_002_second-level.md
002_top-level.md

## Workflow

1. Create an item in the Backlog folder with a short description as the filename
2. When an item is ready to be worked on, assign it a unique identifier and move it to the ToDo folder
3. As you work on items, move them to the InProgress folder
4. When an item is complete, move it to the Done folder
5. Update Implementation notes as work is being done with pertinent information or references
