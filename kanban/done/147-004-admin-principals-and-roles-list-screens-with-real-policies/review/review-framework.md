# Review framework — task 147-004

**Date:** 2026-08-04
**Host task:** kanban/in-progress/147-004-admin-principals-and-roles-list-screens-with-real-policies/
**Diff scope:** commit a0007945 (feat 147-004) vs parent a0b22bb4; also plan commit context
**Plan / brief:** Principal→role store, effective roles, admin policies on APIs, GetCurrentSession.RoleIds, SPA Roles list + Principals assignment, bootstrap Administrator PrincipalIds
**Effort:** 1 (general only)
**Reviewer roster:** general
**Session IDs:** implement 019fcbd6-87a3-7aa0-b912-91a7a6df1660; plan 019fcbd2-8712-73f2-bb04-fa343d3534ca

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-1/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
