# Review framework — task 223

**Date:** 2026-09-16
**Host task:** kanban/in-progress/223-dev-entra-setup-show-tenant-name-and-domain-require-tenant-when-multiple-tenants-are-visible/
**Diff scope:** branch `task/223-dev-entra-setup-show-tenant-name-and-domain-requir` vs `origin/master` (commits `8900db03` feat + `be85ca5e` Results). Product: Graph organization name/domain on tenant lines, union of `az account list` + `az account tenant list` (subscription-less), `--tenant` on setup/status with refuse-when-ambiguous, `TenantDisplayName`/`TenantDomain` user secrets, domain-suffixed default app name with legacy reuse, sign-in audience summary + public-origin warning, Jaribu helper tests, auth.md.
**Plan / brief:** Follow-up to 219-005. Operators with more than one Microsoft tenant only saw a GUID, then signed into a different organisation. See `task.md` Requirements 1–8.
**Effort:** 1 (general only)
**Reviewer roster:** general
**Session IDs:** Review oracle: grok session 01a0a822-ccfd-7d42-8eae-66f8c27df85a (2026-09-16). General reviewer (round 1): grok session 01a0a825-1ff5-77b2-a0b2-dafd01a9dbbd (2026-09-16).

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`

## Round 2

**Date:** 2026-09-16
**Re-review scope:** post-fix delta for M1 (setup Ambiguous message when `--tenant` is provided) and M2 (`FormatTenantLine` domain-only). Carry M1/M2 with updated status; scan the fix delta for new defects.
**Reviewer roster:** general
**Session IDs:** grok session 01a0a82b-b948-7281-9377-aef14bec1827 (2026-09-16).
