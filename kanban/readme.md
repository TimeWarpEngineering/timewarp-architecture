# Kanban

Columns, Definition of Ready, and Definition of Done are in [overview.md](overview.md).
A task body starts from [task-template.md](task-template.md).

## What an instance inherits

A repo made from this template (an instance) starts with its `source/`, `tests/`, `msbuild/`,
root build files, `skills/`, this empty board, and the dev CLI in `tools/dev-cli/` (with
`.timewarp/dev.jsonc` and `.envrc`). An upstream task that changes any of them may need porting.
The ledger below records that decision.

## Upstream sync ledger

A repo made from this template is an instance. This template records no instance status.
Each instance keeps its own ledger of upstream tasks that reached done.

### Propagation

On a task in this template, `## Propagation` classifies the work for every instance.
The first line under the heading is one of:

| Value | When to use it |
|-------|----------------|
| `required` | Every instance should port this. Security, analyzer, build, and infra fixes. |
| `likely` | Most instances will want it. Check. |
| `template-only` | Sample, demo, or template scaffolding. An instance can skip it. |

The line `required | likely | template-only` means the author has not chosen yet.
Replace it with the one word. A line that still contains `|` has no propagation value.

### Ledger file

The ledger lives in the instance repo at `kanban/upstream/<origin>.md`.
The file name is the origin, the same name `ganda kanban board <origin>` takes.
For this template the file is `kanban/upstream/timewarp-architecture.md`.
Ledger files are the `*.md` files in that folder other than `readme.md` and `overview.md`.

At the fork, record the baseline: the highest upstream task id the instance inherited,
and that upstream commit. Ids at or below the baseline were inherited with the fork.
Ids compare in full, segment by segment, and a parent sorts before its children.
Baseline `214` includes `200-003` and `214`. The child `214-001` is above that baseline.

Review upstream tasks that reached done. A done id above the baseline with no row
is still to review. The ledger has no pending rows.

```markdown
# Upstream: timewarp-architecture
Baseline: task 214 / commit 1a2b3c4d

| Upstream | Title | Disposition | Instance ref | Note |
|----------|-------|-------------|--------------|------|
| 215 | Fix analyzer severity | ported | shop#031 | |
| 216 | Sample page polish | n/a | | sample-only |
| 217 | Add telemetry | deferred | | after launch |
```

Disposition is one of:

- `ported` — name the instance task in Instance ref. That instance task carries a line `Upstream: timewarp-architecture#215`.
- `n/a` — give a one-line reason in Note.
- `deferred` — the decision is still open. Say why in Note.

`Upstream` and `Disposition` are required columns. The other columns may sit in any order.
The Upstream cell may be `215`, `#215`, `timewarp-architecture#215`, or a markdown link whose text is the id.

`ganda kanban upstream status` lists unreviewed done tasks and deferred rows.
It exits 1 when an unreviewed task is `required`, the ledger is invalid, or the upstream board cannot be read.
Unreviewed `likely`, `template-only`, and unlabelled tasks stay in the report, and the exit code stays 0.
Record a decision by adding a ledger row on a task branch in the instance repo.
