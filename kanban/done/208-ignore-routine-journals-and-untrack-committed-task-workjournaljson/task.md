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

- [x] Root `.gitignore` has the six routine-journal basenames
- [x] Tracked `task-work.journal.json` paths removed from the index
- [x] Leftover empty `kanban/in-progress/207-…` dir gone
- [x] Audit `routine-journals-gitignore` passes
- [x] `git check-ignore -v` confirms ignore; porcelain does not list journals
- [x] Implementation review disposition recorded (`review/`)

## Session

- Created: ganda session 443357 (2026-09-02)
- Cockpit: grok `01a03d38-9611-7620-aae5-848e15dafa94` (timewarp-flow)
- Trigger: `/tw-merge` 207 / PR #318 — GC refused dirty journal
- Implementer: grok (ganda task-work implement oracle, 2026-09-02)
- Review: grok (ganda task-work review oracle, 2026-09-02) — effort 1 general; round 1; disposition clean

## Notes

Do not implement on `master`. Work in this claimed tree.

Ganda `--fix` will **not** untrack. Sequence: `--fix` gitignore first, then `git rm --cached`
the two ls-files paths, then commit ignore + index removal together.

Related: timewarp-flow `.gitignore` also lacks the block (out of scope unless trivial and
asked). This task is architecture only.

`--checks` on this CLI is a **fix filter** (`ganda repo audit --help`: "audit check ids to
fix"). `ganda repo audit --checks routine-journals-gitignore` without `--fix` still runs the
full suite. Verified invocation: `ganda repo audit --fix --checks routine-journals-gitignore`.
After untrack, that check is **PASS**. Remaining full-audit FAILs (`bin-dev`,
`dev-cli-capabilities`, `memsearch-scaffold`) are pre-existing and out of this task's scope.

## Results

Root `.gitignore` now ignores the six routine-journal basenames (ganda
`routine-journals-gitignore` `--fix` block). The two journals that landed on master with
PR #318 were removed from the index (`git rm --cached`); journal **contents** were not
committed. The leftover `kanban/in-progress/207-wire-timewarpstateplus-breadcrumbs-in-timewarppage/`
directory (journal-only; task 207 lives in `kanban/done/`) was deleted from the worktree.

**Files changed**

- `.gitignore` — appended the commented routine-journal block
- `kanban/in-progress/207-wire-timewarpstateplus-breadcrumbs-in-timewarppage/task-work.journal.json` — untracked (index delete)
- `kanban/in-progress/task-work.journal.json` — untracked (index delete)

**Decisions / deviations**

- Used `ganda repo audit --fix --checks routine-journals-gitignore` so the comment matches
  other repos; then `git rm --cached` (audit `--fix` does not untrack).
- Deleted the leftover 207 in-progress directory from disk after `--cached` left the working
  copy (gitignore does not hide tracked files; after untrack the dir was journal-only).
- Did not touch timewarp-flow `.gitignore` (explicitly out of scope).
- Did not attempt to fix pre-existing `bin-dev` / memsearch audit FAILs.

**Test outcomes**

- `git ls-files '*.journal.json'` — empty
- `git check-ignore -v kanban/in-progress/task-work.journal.json` —
  `.gitignore:469:task-work.journal.json`
- `ganda repo audit --fix --checks routine-journals-gitignore` — `routine-journals-gitignore` **PASS**
- `git status --porcelain` — no `*.journal.json` lines (kitchen journal ignored)

### How to validate

**Smoke**

```bash
# from this worktree (or origin-home after merge)
git ls-files '*.journal.json'
git check-ignore -v kanban/in-progress/task-work.journal.json
git status --porcelain
ganda repo audit --fix --checks routine-journals-gitignore
tail -8 .gitignore
```

**Expect**

- `git ls-files '*.journal.json'` prints nothing
- `git check-ignore -v` reports `.gitignore:469:task-work.journal.json` (line number may
  shift; the matched pattern must be the basename `task-work.journal.json`)
- porcelain does **not** list any `*.journal.json` (`??` or staged)
- audit table: `routine-journals-gitignore` **PASS** ("Routine journal gitignore patterns are present and no routine journals are tracked")
- `.gitignore` ends with the six basenames under `# Task-work resume journal beside kitchens (local; not product)`
- `kanban/in-progress/207-wire-timewarpstateplus-breadcrumbs-in-timewarppage/` does not exist
- `kanban/done/` still has the 207 kitchen (`task.md` not removed)

**Automated gate**

```bash
ganda repo audit --fix --checks routine-journals-gitignore
# expect: routine-journals-gitignore PASS
# note: this CLI's --checks filters --fix only; a full `ganda repo audit` may still
# FAIL on unrelated pre-existing checks (bin-dev, memsearch-scaffold)
```

**Not in scope:** timewarp-flow `.gitignore`; restoring `bin/dev` for the architecture
worktree audit.

### Review disposition

**Outcome:** clean (0 open). **Effort:** 1 (general only). **Rounds:** 1.

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 0 | 0 |
| nit | 0 | 0 | 0 |

No issues raised. The six routine-journal basename lines, `git rm --cached` of both master-tracked journals, leftover empty `kanban/in-progress/207-…/` removal, preservation of `kanban/done/207-…/task.md`, `git ls-files` empty of `*.journal.json`, `git check-ignore` hitting the new basename, and `routine-journals-gitignore` PASS were confirmed.

**Paths:** `review/review-framework.md`, `review/round-1/{general,merged}.md`, `review/disposition.md`.
