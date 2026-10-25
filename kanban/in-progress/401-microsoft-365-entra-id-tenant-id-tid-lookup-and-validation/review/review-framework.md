# Review framework

## Budget (by-diff)

- Lines changed: 3174
- Effort: 3
- TCB hits: none
- Roster axes: general
- Turn cap: 200 (--max-turns; cursor uncapped)

## Review framework — task 401

**Date:** 2026-10-25
**Host task:** kanban/in-progress/401-microsoft-365-entra-id-tenant-id-tid-lookup-and-validation/
**Diff scope:** branch `task/401-microsoft-365-entra-id-tenant-id-tid-lookup-and-va` vs merge-base
`a2577574f` with `origin/master` (commits `f7af5aa9d..2dc25d35e`; 60 files)
**Plan / brief:** task.md `## Scope` and `## Goal (implementer restatement)`. Domain-to-tid
resolver in TimeWarp.Identity, `{tenantId, domain?}` accepted-tenant entries with an EF
migration, `ResolveEntraTenant` (SettingsWrite, abuse rate limit, cache), Authentication page
list editor, version `2.0.0-beta.26`.
**Effort:** 3 (by-diff budget), roster axis general only
**Reviewer roster:** general (review oracle, Claude Opus 5.5)
**Session IDs:** review oracle session not reported by host

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
