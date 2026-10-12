# Kanban Board

Work is tracked as tasks on this board. Columns are kebab folders under `kanban/`: `backlog/`,
`to-do/`, `in-progress/`, `done/`, `archived/`. The folder a task lives in **is** its status;
moving a task between folders is the status change. Each column holds a `.gitkeep` so an empty
column survives git.

Task ids are allocated by the tooling (`ganda kanban` where available). Never hand-number a task
file or invent its name. Keep the task's checklist and `## Session` current, and commit board
changes with the work. `kanban/task-template.md` is the task body shape.

## Definition of Ready

A task moves from `backlog/` to `to-do/` when its spec is complete, not when it is scheduled.

- [ ] Description states the purpose and the outcome
- [ ] Requirements and acceptance criteria are written
- [ ] Dependencies are identified and available
- [ ] API work: the data the client needs is defined (`tw-web-api-contracts`)
- [ ] Client work: the pages, state and actions involved are named (`tw-blazor`)
- [ ] Placement is known: which slice the work lands in (`tw-feature-placement`,
      `tw-slice-isolation`)

## Definition of Done

- [ ] Endpoints follow `tw-web-api-contracts`: contract, generated endpoint, validator and
      serialization round-trip test
- [ ] Razor and client state follow `tw-blazor`; every user interaction dispatches a state action
- [ ] Layout and styling follow `tw-blazor-layout` and `tw-blazor-css-strategy`
- [ ] Aggregates follow `tw-aggregate-pattern`
- [ ] Files sit where `tw-feature-placement` puts them, with no cross-slice references
      (`tw-slice-isolation`)
- [ ] Tests are written and pass (Jaribu and Shouldly)
- [ ] The build has 0 warnings and 0 errors
- [ ] Deployment changes follow `tw-deploy`
- [ ] The task has `## Results` with `### How to validate` (Smoke and Expect)
