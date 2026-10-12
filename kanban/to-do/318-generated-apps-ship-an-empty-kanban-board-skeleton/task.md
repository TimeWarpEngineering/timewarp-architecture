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

- [x] Skeleton (five columns + `.gitkeep`, overview, task template) packed; monorepo tasks/journals excluded
- [x] Generic overview decision recorded
- [x] Template-smoke assertions
- [x] Gates: `dev template-smoke`, `dev build` 0/0, `ganda repo audit`

## Session

- Created: 2026-10-12 (cockpit, per Steve)
- 2026-10-12: implementer (ganda task work) — skeleton packed, smoke assert added, gates green.
- Review oracle: review by implementer-claude (claude, model claude-opus-5-5), session not reported, max-turns 120 — 2026-10-12T15:56:54Z

## Results

**Decision — generic overview:** the root `kanban/overview.md` is now generic and packs as is
(one source, nothing to keep in sync). It says columns are folders and the folder is the status,
ids come from the tooling (`ganda kanban` where available), `task-template.md` is the body shape,
and the Definition of Ready / Done point at the shipped skills (`tw-web-api-contracts`,
`tw-blazor`, `tw-blazor-layout`, `tw-blazor-css-strategy`, `tw-aggregate-pattern`,
`tw-feature-placement`, `tw-slice-isolation`, `tw-deploy`). The old Mapper/Handler checklist and
the Figma line are gone. `kanban/task-template.md` was trimmed (no `tw-agent-collaboration`
reference, which does not ship; no example task number) and also packs as is.

Changes:

- `kanban/{to-do,in-progress,done,archived}/.gitkeep` added (`backlog/` already had one).
- `timewarp-templates/source/timewarp-architecture-template/timewarp-architecture-template.csproj`:
  explicit `Content` items for the five `.gitkeep` files, `overview.md` and `task-template.md`.
  No `kanban\**` glob, so tasks, journals and `task-examples/` never pack.
- `tools/dev-cli/services/template-smoke-harness.cs`: `AssertKanbanSkeletonShipped` — requires
  the five column `.gitkeep` files, `overview.md` and `task-template.md`, and fails on any other
  file or directory under `kanban/`. Called from `template-smoke` next to `AssertSkillsShipped`.
- No test reads or writes `kanban/` (316 interaction unchanged).

Gates: `dev template-smoke` SUCCEEDED (skeleton assert passed for SmokeDefault, SmokeNoApi,
SmokeNoPostgres); `dev build` succeeded (warnings are errors); `ganda repo audit` passes.

### How to validate

**Smoke**

```bash
dotnet run tools/dev-cli/dev.cs -- self-install   # refresh bin/dev with the new assert
./bin/dev template-smoke
find artifacts/template-smoke/work/SmokeDefault/kanban | sort
```

**Expect**

- `template-smoke` prints "Generated app contains the empty kanban/ skeleton (five columns,
  overview, task template)." for each matrix app and ends with `Template smoke SUCCEEDED`.
- The `find` lists exactly `kanban/`, the five column folders each with `.gitkeep`,
  `overview.md` and `task-template.md` — no task folders, `*.md` tasks or `*.journal.json`.


### Review disposition

- Rounds: 1. Reviewer: general. Effort: 2 (by-diff budget).
- Final counts: bug 0, suggestion 0, nit 1 fixed. Open 0, wontfix 0.
- Disposition: **clean**. M1, the `./bin/dev run` smoke example in the shipped `task-template.md`, now
  uses a `dotnet run --project …/aspire-app-host` example instead.
- I re-ran the gate during review: `dev template-smoke` SUCCEEDED, and the skeleton check passed for 3/3 matrix apps.
- Artifacts: `review/review-framework.md`, `review/round-1/general.md`, `review/round-1/merged.md`,
  `review/disposition.md`.
