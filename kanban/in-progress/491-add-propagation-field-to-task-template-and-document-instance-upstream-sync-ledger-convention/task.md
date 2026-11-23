# Add Propagation field to task template and document instance upstream-sync ledger convention

## Description

CrunchIt (and later other repos) will be instances of timewarp-architecture. After an
instance forks, every finished architecture task must be checked: does the instance also
need it (port it as an instance task) or not (n/a)?

Each instance keeps that record in **its own** repo. The template never tracks instance
status. This task adds the template side:

1. A `## Propagation` section (heading, matching the checklist) in `kanban/task-template.md` so authors classify a task up front:
   - `required`: every instance should port this (security, analyzer, build, infra fixes)
   - `likely`: most instances will want it; check
   - `template-only`: sample/demo/template-scaffolding only; instances can skip
2. A convention doc that explains the instance-side upstream ledger (see Notes).

## Goal (implementer restatement)

Authors of tasks in this template say, up front, whether an instance repo should port the work. The template itself never records what any instance decided.

Acceptance criteria:

- `kanban/task-template.md` has a `## Propagation` heading whose first choice line is `required | likely | template-only`, so an unfilled copy has no propagation value. The three words mean: every instance should port it; most instances will want it; sample or scaffolding an instance can skip.
- The ledger convention (baseline at the fork, review only upstream tasks that reached done, no pending rows, dispositions `ported` / `n/a` / `deferred`, and `Upstream: timewarp-architecture#NNN` on a ported instance task) is written in this repo. No skill here owns kanban, so the page is `kanban/readme.md`. `documentation/` stays unused.
- The task template links that page, so an author can see why the field exists.
- A generated app receives `kanban/readme.md` with the rest of the board skeleton, and the skeleton check allows that file and no other extra kanban path.

## Checklist

- [x] Add `## Propagation` (required | likely | template-only) to `kanban/task-template.md`
- [x] Write the convention as a reference under the skill that owns kanban conventions (tw-kanban in timewarp-flow is cross-repo, so here it is a `skills/` reference page if one owns kanban; otherwise a `kanban/readme.md` section). documentation/ is retired and is not an option (groom 2026-11-23).
- [x] Link the doc from the template so authors know why the field exists

## Session

- Created: f5c3f33c-f934-403b-a23a-50b137ccf5f0 (2026-11-14)
- Implementation: 01a209ce-1627-7432-b1f9-131c8f169dae (2026-11-24)
- Implement oracle: implemented by implementer-grok (grok, vendor xai, model grok-4.7, response model grok-4.7-build), session 01a209ce-1627-7432-b1f9-131c8f169dae, max-turns 500, Done — 2026-11-23T19:25:32Z

## Notes

Instance ledger convention (lives in the instance repo, e.g. `kanban/upstream/timewarp-architecture.md`):

```markdown
# Upstream: timewarp-architecture
Baseline: task NNN / commit <sha>   <- at or below this was inherited at fork

| Upstream | Title | Disposition | Instance ref | Note |
|----------|-------|-------------|--------------|------|
| 215 | ... | ported   | crunchit#031 |                    |
| 216 | ... | n/a      |              | sample-only        |
| 217 | ... | deferred |              | after launch       |
```

Rules:
- At fork, record the baseline (highest upstream task id + commit). Nothing at or below the baseline is reviewed.
- Review only upstream tasks that reached **done**.
- Upstream done ids above the baseline with no ledger row are the ones still to review. No "pending" rows.
- Disposition is `ported` (link the instance task), `n/a` (one-line reason), or `deferred` (still open).
- A ported instance task carries `Upstream: timewarp-architecture#NNN`.

Companion tooling: timewarp-ganda task "Add ganda kanban upstream status command for instance repos".

Origin: design discussion 2026-11-14 (Steve), ahead of the CrunchIt launch.

## Results

`kanban/task-template.md` now has `## Propagation`. The first line under the heading is the unfilled choice list `required | likely | template-only`, which `ganda kanban upstream status` reads as no value. The next paragraph names the three meanings and links to `kanban/readme.md`.

No skill in this repo owns kanban (`skills/` has no kanban skill). The convention is `kanban/readme.md`: baseline at the fork (full id, parent before children, so baseline `214` includes `200-003` and `214`, and `214-001` is above it), review only upstream tasks that reached done, no pending rows, dispositions `ported` / `n/a` / `deferred`, and `Upstream: timewarp-architecture#NNN` on a ported instance task. `kanban/overview.md` points at that page.

Generated apps ship the page. The template pack lists `kanban/readme.md` next to `overview.md` and `task-template.md`, and `AssertKanbanSkeletonShipped` expects it.

### How to validate

**Smoke**

```bash
rg -n -A 4 '^## Propagation$' kanban/task-template.md
dotnet run --file tools/dev-cli/dev.cs -- template-smoke
```

**Expect**

- The template's first line under `## Propagation` is `required | likely | template-only`, and that section links to `readme.md`.
- Template smoke prints `Generated app contains the empty kanban/ skeleton (five columns, overview, readme, task template).` and ends `Template smoke SUCCEEDED`.

**Automated gate**

```bash
./bin/dev build
dotnet run --file tools/dev-cli/dev.cs -- template-smoke
ganda repo audit
```

- `./bin/dev build` — succeeded, 0 warnings, 0 errors.
- No test file was added or edited. `TemplateSmokeGeneratedContent` has no Jaribu runfile (no `#if !JARIBU_MULTI` file). `tests/tools/dev-cli-tests/template-smoke-content-assets-tests.cs` covers `TemplateSmokeContentAssets`, a different type, and was not run. CI runs that project inside `./bin/dev test`.
- `dotnet run --file tools/dev-cli/dev.cs -- template-smoke` — succeeded. SmokeDefault OK. The skeleton line names overview, readme, and the task template.
- `ganda repo audit` — exit 0. 43 passed, 2 skipped, 1 advisory warning (`vscode-window-icon`: gitignored `.vscode/settings.json` is missing on this machine). The repository passes.
