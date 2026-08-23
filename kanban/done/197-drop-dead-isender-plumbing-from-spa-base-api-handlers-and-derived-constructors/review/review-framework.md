# Review framework — task 197

**Date:** 2026-08-23
**Host task:** kanban/in-progress/197-drop-dead-isender-plumbing-from-spa-base-api-handlers-and-derived-constructors/
**Diff scope:** branch `task/197-drop-dead-isender-plumbing-from-spa-base-api-handl` vs `origin/master` — drop dead `ISender` from SPA `DefaultApiHandler` / `FileResponseApiHandler` and ten derived handlers; Design region reconciliation; kanban plan + folderize
**Plan / brief:** Remove unread `Sender` fields and ctor plumbing so bases no longer afford direct mediator Send (defence in depth after TWA0022 / task 196 M4)
**Effort:** 1 (general only)
**Reviewer roster:** general
**Session IDs:** implementer-grok headless (task-197 worktree)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
