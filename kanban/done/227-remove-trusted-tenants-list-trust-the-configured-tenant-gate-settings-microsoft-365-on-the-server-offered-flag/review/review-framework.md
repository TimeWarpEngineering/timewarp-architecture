# Review framework — task 227

**Date:** 2026-09-16
**Host task:** kanban/in-progress/227-remove-trusted-tenants-list-trust-the-configured-tenant-gate-settings-microsoft-365-on-the-server-offered-flag/
**Diff scope:** branch `task/227-remove-trusted-tenants-list-trust-the-configured-t` vs `origin/master` (commits `a818d927` feat + `1c9b22c2` Results). Product: remove TrustedTenants allowlist (trust = token `tid` GUID-equals `Authentication:Entra:TenantId`); gate `/Settings` Microsoft 365 on server `GetEntraSignInOffered`; admin page read-only tenant line + heading cleanup; drop-column migration; options validator rejects leftover TrustedTenants key.
**Plan / brief:** See `task.md` Requirements A (remove TrustedTenants everywhere + foreign-tid tests), B (`/Admin/Authentication` UI), C (`/Settings` server offered gate + SPA test). Out of scope: breadcrumb trail.
**Effort:** 1 (general only)
**Reviewer roster:** general
**Session IDs:** Review oracle: grok session 01a0aa03-4bbd-7d21-aa21-400c840ccc7a (2026-09-16). General reviewer (round 1): grok session 01a0aa04-ced5-72d1-99fd-7588ae6b7516 (2026-09-16).

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
