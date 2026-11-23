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

## Checklist

- [ ] Add `## Propagation` (required | likely | template-only) to `kanban/task-template.md`
- [ ] Write the convention as a reference under the skill that owns kanban conventions (tw-kanban in timewarp-flow is cross-repo, so here it is a `skills/` reference page if one owns kanban; otherwise a `kanban/readme.md` section). documentation/ is retired and is not an option (groom 2026-11-23).
- [ ] Link the doc from the template so authors know why the field exists

## Session

- Created: f5c3f33c-f934-403b-a23a-50b137ccf5f0 (2026-11-14)

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
