# Round 1 — general
**Date:** 2026-09-15
**Scope reviewed:** `origin/master...HEAD` product surface for 219-005 — `tools/dev-cli/endpoints/entra-*.cs`, `tools/dev-cli/services/entra-*.cs`, `tests/tools/dev-cli-tests/`, `auth.md`, template/slnx/CPM ancillary edits; compared to `db-*` / `build-command` / `EntraAuthenticationOptions` keys.

## Summary

`dev entra setup|status|disable` matches the brief: Amuru-only process execution, find-or-create by display name, redirect-URI union, SP ensure, user-secrets keys aligned with `Authentication:Entra`, dry-run that executes nothing, and secret masking on status/summary/dry-run ClientSecret lines. Live credential-reset stdout is captured and never printed; `--capabilities` and Nuru group wiring look correct; helper tests cover the cheap seams (11 via Jaribu). Overall risk is low; the one contract gap is fail-open minting when `user-secrets list` fails.

## Issues

### Issue 1 — Severity: bug
- File: tools/dev-cli/endpoints/entra-setup-command.cs:375
- Description: When `--new-secret` is not set, `MaybeMintSecretAsync` treats a failed `dotnet user-secrets list` as “no ClientSecret” and mints (`mint = true`). That breaks the mint-only-when-needed / idempotent contract whenever listing fails while `Authentication:Entra:ClientSecret` is already present. Credential reset runs with `--append` before any write attempt, so a subsequent `user-secrets set` failure (same tooling/project problems that made list fail) leaves an orphan Azure password that was never stored locally and was never shown to the operator.
- Suggestion: If `ListUserSecretsAsync` fails, call `WriteFailure`, set `Environment.ExitCode = 1`, and return `false` without calling `az ad app credential reset`. Mint only when list succeeds and `HasClientSecret` is false, or when `--new-secret` is set. Empty successful list output already covers first-run.
- Status: open
