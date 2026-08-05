# Review framework — task 154

**Date:** 2026-08-05
**Host task:** kanban/in-progress/154-deep-links-to-protected-pages-return-raw-401-instead-of-serving-the-spa-shell/
**Diff scope:** commit `0067116b` (fix(web): dual-mode cookie challenge for protected page deep links) vs parent — files:
- `source/container-apps/web/platform/identity-host/identity-session-cookie-challenge-server.cs` (new)
- `source/container-apps/web/projects/web-server/program.cs` (cookie events)
- `tests/container-apps/web/web-server-integration-tests/features/identity/protected-page-deep-link-tests.cs` (new)
**Plan / brief:** Dual-mode identity-session cookie `OnRedirectToLogin`: non-API → 302 `/Login?returnUrl=…`; `/api` → 401; forbid always 403. Not SPA-shell serve.
**Effort:** 1 (general only)
**Reviewer roster:** general
**Session IDs:** Grok orchestration 2026-08-05; implementer subagent 019fd0ae-e87b-7aa2-a65f-26672bcb1ef8

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
