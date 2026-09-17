# Review framework — task 235

**Date:** 2026-09-17
**Host task:** kanban/in-progress/235-dev-entra-setup-mint-a-new-client-secret-automatically-when-the-tenant-or-app-registration-changes/
**Diff scope:** branch `task/235-dev-entra-setup-mint-a-new-client-secret-automatic` vs `origin/master` (product commit `10798f9c` plus kanban results `46cfd46d`). Product paths:

- `tools/dev-cli/services/entra-setup.cs`
- `tools/dev-cli/services/entra-cli.cs`
- `tools/dev-cli/endpoints/entra-setup-command.cs`
- `tools/dev-cli/endpoints/entra-status-command.cs`
- `tests/tools/dev-cli-tests/entra-setup-tests.cs`
- `source/container-apps/web/projects/web-spa/wwwroot/auth.md`

**Plan / brief:** `dev entra setup` must re-mint the client secret when stored `ClientId` or `TenantId` differs from the app it is about to write (GUID-equal, case-insensitive). `--new-secret` stays a same-app rotation flag. Dry-run prints the decision and the (masked) `az ad app credential reset` invocation. `dev entra status` warns when stored `ClientId` does not match the app found by name. Never print the secret. Failed `user-secrets list` still aborts (does not fail-open mint).

**Effort:** 1 (general only)
**Reviewer roster:** general
**Session IDs:** Grok review oracle (2026-09-17) — task-235 worktree

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
