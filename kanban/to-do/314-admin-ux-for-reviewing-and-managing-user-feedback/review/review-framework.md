# Review framework

## Budget (by-diff)

- Lines changed: 4588
- Effort: 3
- TCB hits: none
- Roster axes: general
- Turn cap: 200 (--max-turns; cursor uncapped)

# Review framework — task 314

**Date:** 2026-10-12
**Host task:** kanban/to-do/314-admin-ux-for-reviewing-and-managing-user-feedback/
**Diff scope:** branch `task/314-admin-ux-for-reviewing-and-managing-user-feedback` vs `master` (commits 49059061d, a94ee1deb; 74 files, +4511/−77)
**Plan / brief:** task.md decision log 1–6 + Implementation shape; Results section describes what landed
**Effort:** 3 (by-diff budget)
**Reviewer roster:** general — split by area into `general-server` (domain, store, EF/migration, contracts, handlers, mail, permissions, server tests) and `general-spa` (web-spa state/pages, SPA tests)
**Session IDs:** review oracle = headless implementer-claude (ganda task work); reviewers = Claude subagents

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
