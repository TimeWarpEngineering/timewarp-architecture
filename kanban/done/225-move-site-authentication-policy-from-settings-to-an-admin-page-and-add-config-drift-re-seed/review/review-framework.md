# Review framework — task 225

**Date:** 2026-09-16
**Host task:** kanban/in-progress/225-move-site-authentication-policy-from-settings-to-an-admin-page-and-add-config-drift-re-seed/
**Diff scope:** branch `task/225-move-site-authentication-policy-from-settings-to-a` vs `origin/master` (commits `f439bbdb` feat + `1fd78a43` Results). Product: move site authentication policy from `/Settings` to `/Admin/Authentication`, surface config-vs-store drift (boot Warning + admin banner + add-tenant), Development-only `ReseedSiteSettings` + `dev entra reseed`.
**Plan / brief:** See `task.md` Requirements A (Admin page + permission decision) and B (config-drift detection and re-seed).
**Effort:** 1 (general only)
**Reviewer roster:** general
**Session IDs:** Review oracle: grok session 01a0a874-0185-7d43-a864-54f056fcd78c (2026-09-16). General reviewer (round 1): grok session 01a0a875-a1b8-7403-af3a-9a4b95755d9c (2026-09-16).

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
