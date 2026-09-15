# Round 2 — general
**Date:** 2026-09-15
**Scope reviewed:** re-verify M1 + fix delta (TryDecideMintClientSecret / MaybeMintSecretAsync / helper tests)

## Summary

M1 is fixed. `MaybeMintSecretAsync` no longer fail-open mints when `dotnet user-secrets list` fails: it routes through `EntraSetup.TryDecideMintClientSecret`, and on abort writes a failure, sets `Environment.ExitCode = 1`, and returns `false` before any `az ad app credential reset`. `--new-secret` still short-circuits listing and mints; helper tests cover mint / skip / abort (4/4 passed via Jaribu). No new defects in the fix delta.

## Issues

### M1 — Severity: bug
- File: tools/dev-cli/endpoints/entra-setup-command.cs
- Description: Verified fixed. Prior fail-open (`mint = true` on list failure) is gone. Uncommitted delta replaces that branch with `TryDecideMintClientSecret(newSecret, listSucceeded, hasExistingClientSecret, out mint)`: list failure → `mint = false`, method returns `false`, caller `WriteFailure`s with “Failed to list Web.Server user secrets; not minting a client secret.”, sets exit 1, and returns without credential reset. Mint only when list succeeds and `HasClientSecret` is false, or when `--new-secret` is set (call site skips listing when `Command.NewSecret`). Design comments in `entra-setup.cs` / `entra-setup-command.cs` match. `TryDecideMintClientSecret_Given_` covers new-secret-with-failed-list, failed-list abort, succeed-without-secret mint, succeed-with-secret skip.
- Suggestion: none (fixed as recommended in round 1).
- Status: fixed
