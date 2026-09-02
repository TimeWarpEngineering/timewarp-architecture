# Ignore routine journals and untrack committed task-work.journal.json

## Description

`task-work.journal.json` is a **local** ganda resume file beside the kitchen. It is supposed
to be gitignored. Architecture's root `.gitignore` does **not** list it, so `ganda task work`
left the journal as porcelain (`??` or tracked), and `/tw-merge` on **207** / PR **#318**
refused `worktree gc` (`GC: Refuse: worktree is dirty`). Same class as ganda **262**
(`routine-journals-gitignore`) and mediator 004-001/004-002.

After #318 merged, **master still tracks**:

- `kanban/in-progress/207-wire-timewarpstateplus-breadcrumbs-in-timewarppage/task-work.journal.json`
- `kanban/in-progress/task-work.journal.json`

Task 207 is in `kanban/done/`. The in-progress `207-…` folder is a leftover that only exists
because the journal was committed. Gitignore does **not** hide tracked files.

Org SSOT: `ganda repo audit` check `routine-journals-gitignore` (ganda 262). `--fix` appends
the missing basename lines. Tracked journals are **Failed / not fixable** — `git rm --cached`
is required (do not leave the files in the index).

## Requirements

Root `.gitignore` must contain these exact basename lines (comments/blanks ok):

```
task-work.journal.json
stacked-task-set.journal.json
planning.journal.json
rfc.journal.json
debate.journal.json
advisor.journal.json
```

Prefer `ganda repo audit --fix` (or `--fix --checks routine-journals-gitignore`) so the
commented block matches other repos:

```gitignore
# Task-work resume journal beside kitchens (local; not product)
task-work.journal.json
stacked-task-set.journal.json
planning.journal.json
rfc.journal.json
debate.journal.json
advisor.journal.json
```

Then:

- `git rm --cached` both tracked journals (and delete the leftover
  `kanban/in-progress/207-…/` directory if it is empty after that).
- Do **not** commit journal contents. Do **not** `git rm` product task.md files.
- `git ls-files` must not list any `*.journal.json`.
- `ganda repo audit --checks routine-journals-gitignore` PASSes (use the invocation
  this repo's `ganda repo audit --help` documents; `--checks` may require `--fix` on
  some CLI versions — verify).
- `git check-ignore -v kanban/in-progress/task-work.journal.json` hits the new line.

## Checklist

- [ ] Root `.gitignore` has the six routine-journal basenames
- [ ] Tracked `task-work.journal.json` paths removed from the index
- [ ] Leftover empty `kanban/in-progress/207-…` dir gone
- [ ] Audit `routine-journals-gitignore` passes
- [ ] `git check-ignore -v` confirms ignore; porcelain does not list journals

## Session

- Created: ganda session 443357 (2026-09-02)
- Cockpit: grok `01a03d38-9611-7620-aae5-848e15dafa94` (timewarp-flow)
- Trigger: `/tw-merge` 207 / PR #318 — GC refused dirty journal

## Notes

Do not implement on `master`. Work in this claimed tree.

Ganda `--fix` will **not** untrack. Sequence: `--fix` gitignore first, then `git rm --cached`
the two ls-files paths, then commit ignore + index removal together.

Related: timewarp-flow `.gitignore` also lacks the block (out of scope unless trivial and
asked). This task is architecture only.
