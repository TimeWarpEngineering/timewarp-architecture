# Review framework — task 219-005

**Date:** 2026-09-15
**Host task:** kanban/to-do/219-005-dev-cli-entra-setup-create-app-registration-via-az-and-write-web-server-user-secrets/
**Diff scope:** branch `task/219-005-dev-cli-entra-setup-create-app-registration-via-az` vs `origin/master` (commits `16f2ec4e` feat + `73b19775` Results). Product: `tools/dev-cli/endpoints/entra-*.cs`, `tools/dev-cli/services/entra-*.cs`, `tests/tools/dev-cli-tests/`, `source/container-apps/web/projects/web-spa/wwwroot/auth.md`, `.template.config/template.json`, `Directory.Packages.props` (drop unused Identity.Web / MSAL pins), `timewarp-architecture.slnx`. Kitchen `task.md` is in the Results commit.
**Plan / brief:** `dev entra` group (setup / status / disable) so a logged-in `az` plus one command writes Web.Server user secrets for the named `entra` scheme. Idempotent find-or-create by display name, redirect-URI union, SP ensure, mint secret only on first run / `--new-secret`, never echo the password, `--dry-run` prints without executing. Companion to 219-002 (scheme) and 219-004 (`PublicOrigin`).
**Effort:** 1 (general only)
**Reviewer roster:** general
**Session IDs:** Review oracle: grok session 01a0a507-2450-71d3-8e75-6504cd495eaa (2026-09-15). General reviewer (round 1): grok session 01a0a509-7b03-75e2-9620-987f6e1ab270 (2026-09-15). General reviewer (round 2): grok session 01a0a512-1293-7d20-bf00-2c5cbe703118 (2026-09-15).

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
