# Round 1 — merged findings
**Date:** 2026-09-15
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 1 | 0 |
| suggestion | 0 | 0 | 0 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: tools/dev-cli/endpoints/entra-setup-command.cs:375
- Description: When `--new-secret` is not set, `MaybeMintSecretAsync` treats a failed `dotnet user-secrets list` as “no ClientSecret” and mints (`mint = true`). That breaks the mint-only-when-needed / idempotent contract whenever listing fails while `Authentication:Entra:ClientSecret` is already present. Credential reset runs with `--append` before any write attempt, so a subsequent `user-secrets set` failure (same tooling/project problems that made list fail) leaves an orphan Azure password that was never stored locally and was never shown to the operator.
- Suggestion: If `ListUserSecretsAsync` fails, call `WriteFailure`, set `Environment.ExitCode = 1`, and return `false` without calling `az ad app credential reset`. Mint only when list succeeds and `HasClientSecret` is false, or when `--new-secret` is set. Empty successful list output already covers first-run.
- Source: general
- Disposition notes: Fail-closed via `EntraSetup.TryDecideMintClientSecret`. List failure writes an error, sets exit 1, and returns before credential reset. `--new-secret` still mints without listing. Helper tests cover mint/skip/abort. Round 2 must re-verify.

## Duplicates / conflicts

- None (single general reviewer).
