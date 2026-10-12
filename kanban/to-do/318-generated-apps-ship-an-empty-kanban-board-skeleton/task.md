# Generated apps ship an empty kanban board skeleton

## Description

Generated apps have no `kanban/` folder. Steve, 2026-10-12: "we should create the structure at
least". The repo's working model (folder location is status; `tw-kanban`; `ganda kanban` where
available) applies to apps built from the template just as it does here. A generated app should
start with an empty, ready-to-use board.

## Requirements

- **Pack a skeleton, not this repo's board.**
  - The template ships `kanban/` with the five column folders `backlog/`, `to-do/`,
    `in-progress/`, `done/`, `archived/`. Each holds a `.gitkeep` so the empty folders survive git.
    The template csproj already has `NoDefaultExcludes`, so dotfiles pack.
  - Also ship `overview.md` and `task-template.md`.
  - **Never pack this repo's tasks, journals (`*.journal.json`) or `task-examples/`.** Add
    explicit `Content` entries for the skeleton files only, in
    `timewarp-templates/source/timewarp-architecture-template/timewarp-architecture-template.csproj`
    next to the existing `source/`, `tests/`, `skills/` entries.
- **Generic `overview.md` for generated apps.** The monorepo's `kanban/overview.md` mentions this
  repo's history ("older board template", Figma) and its own conventions. Either:
  - make the root `overview.md` generic enough to ship as is; or
  - keep a separate generic copy that packs to `kanban/overview.md`, for example a template-only
    source file mapped with `PackagePath`.

  Pick one and record it. The shipped text says:
  - columns are folders and the folder is the status;
  - ids are allocated by the tooling, never hand-numbered;
  - `task-template.md` is the body shape;
  - the Definition of Ready and Definition of Done point at the shipped skills
    (`tw-web-api-contracts`, `tw-blazor`, …).

  Same rule as skills: public-safe, no client names, no task numbers or history. ganda is private,
  so name the tool only as "`ganda kanban` where available" or leave it generic.
- **`task-template.md`:** ship it as is if it is generic. Otherwise trim it in the same way.
- **Template-smoke:** assert that a generated app has `kanban/{backlog,to-do,in-progress,done,archived}/`
  (with `.gitkeep`), `kanban/overview.md` and `kanban/task-template.md`, and **no** task folders
  or journals.
- **Interaction with 316.** Even with a board present, tests must never write into `kanban/`.
  316 removes those lookups. This task must not reintroduce any test dependency on `kanban/`.

## Checklist

- [ ] Skeleton (five columns + `.gitkeep`, overview, task template) packed; monorepo tasks/journals excluded
- [ ] Generic overview decision recorded
- [ ] Template-smoke assertions
- [ ] Gates: `dev template-smoke`, `dev build` 0/0, `ganda repo audit`

## Session

- Created: 2026-10-12 (cockpit, per Steve)
