# Review framework — task 208

**Date:** 2026-09-02
**Host task:** kanban/in-progress/208-ignore-routine-journals-and-untrack-committed-task-workjournaljson/
**Diff scope:** branch `task/208-ignore-routine-journals-and-untrack-committed-task` vs `origin/master` (product: `.gitignore`; index deletes of two tracked `task-work.journal.json` paths; kitchen `task.md` move to-do → in-progress)
**Plan / brief:** Task 208 — Architecture `.gitignore` lacked the org routine-journal block, so `ganda task work` left journals as porcelain and `/tw-merge` 207 / PR #318 refused worktree gc. Append the six basename lines (prefer `ganda repo audit --fix`); `git rm --cached` the two tracked journals; delete leftover empty `kanban/in-progress/207-…/` if journal-only; do not commit journal contents or remove product `task.md`.
**Effort:** 1 (general only)
**Reviewer roster:** general
**Session IDs:** Review oracle: Grok (2026-09-02) — `ganda task work` review body

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`

## Product files in scope

- `.gitignore` (appended routine-journal block at end)
- `kanban/in-progress/207-wire-timewarpstateplus-breadcrumbs-in-timewarppage/task-work.journal.json` (index delete)
- `kanban/in-progress/task-work.journal.json` (index delete)

Kitchen-only (not product, but confirm no accidental loss):

- `kanban/in-progress/208-ignore-routine-journals-and-untrack-committed-task-workjournaljson/task.md`
- `kanban/done/207-wire-timewarpstateplus-breadcrumbs-in-timewarppage/task.md` must still exist

## Task requirements to check

- Root `.gitignore` contains these exact basename lines (comments/blanks ok):
  `task-work.journal.json`, `stacked-task-set.journal.json`, `planning.journal.json`,
  `rfc.journal.json`, `debate.journal.json`, `advisor.journal.json`
- Preferred commented block matches other repos (`# Task-work resume journal beside kitchens (local; not product)`)
- Both tracked journals removed from the index (`git rm --cached`, not a content rewrite)
- Journal **contents** were not newly committed
- Leftover empty `kanban/in-progress/207-…/` directory gone
- Product 207 kitchen in `kanban/done/` was **not** removed
- `git ls-files` lists no `*.journal.json`
- `git check-ignore -v kanban/in-progress/task-work.journal.json` hits the new basename line
- `ganda repo audit` check `routine-journals-gitignore` PASSes
- Porcelain does not list journals (`??` or staged)

## Implementer claims to re-verify

- Used `ganda repo audit --fix --checks routine-journals-gitignore` then `git rm --cached`
- `--checks` on this CLI is a **fix filter**; full `ganda repo audit` still runs the suite
- Remaining full-audit FAILs (`bin-dev`, `dev-cli-capabilities`, `memsearch-scaffold`) are pre-existing and out of scope
- timewarp-flow `.gitignore` was not touched (explicitly out of scope)
- `git check-ignore -v` reports `.gitignore:469:task-work.journal.json`
- Kitchen journal for 208 is ignored locally and not tracked
