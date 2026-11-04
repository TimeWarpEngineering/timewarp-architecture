# Review framework

## Budget (by-diff)

- Lines changed: 3865
- Effort: 3
- TCB hits: none
- Roster axes: general
- Turn cap: 200 (--max-turns; cursor uncapped)

# Review framework — task 473

**Date:** 2026-11-05
**Host task:** kanban/in-progress/473-roles-aggregate-by-hand-role-with-permissions-and-agent-scopes-irolestore-owned-mappings-seed-migration/
**Diff scope:** branch `task/473-roles-aggregate-by-hand-role-with-permissions-and` (HEAD 5cc7baaca) vs merge base 762accd5e with `master`; product files only (kanban paths excluded)
**Plan / brief:** task.md Requirements and `## Goal (implementer restatement)`: hand-written Role aggregate owning permissions and agent scopes, IRoleStore (in-memory + EF), seven handlers on the aggregate, grant stores read-only over the owned tables, AddRoles migration with seed and stray parents, product-role delete guard
**Effort:** 3 (by-diff budget, 3865 lines)
**Reviewer roster:** general
**Session IDs:** review oracle, Claude Opus 5.5 (claude-opus-5-5), ganda task work, 2026-11-05

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
