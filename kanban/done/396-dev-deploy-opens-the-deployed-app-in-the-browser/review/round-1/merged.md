# Round 1 — merged findings
**Date:** 2026-10-22
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 0 | 0 |
| nit | 2 | 0 | 0 |

## Issues

### M1 — Severity: nit — Status: open
- File: tools/dev-cli/services/deploy-operate.cs:780
- Description: `NoBrowserMessage` says `(or --no-browser)`, but `--no-browser` no longer prints it.
  `ChooseBrowserOpen` returns no fallback for `--no-browser`. The line is shown only when no launcher
  is on PATH, or when every launcher on PATH failed.
- Suggestion: remove the `--no-browser` clause, e.g. `No browser opener worked; open {url}`.
- Source: general (Issue 1)
- Disposition notes:

### M2 — Severity: nit — Status: open
- File: tools/dev-cli/endpoints/deploy-command.cs:420
- Description: `followUp?.Hint ?? DeployPipeline.TemporaryForwardStopped(...)` is always
  `TemporaryForwardStopped(target)`, so the nullable `followUp` local and the coalesce are redundant.
- Suggestion: print `TemporaryForwardStopped` directly in `finally`. Use a non-null local for the
  summary in the success branch.
- Source: general (Issue 2)
- Disposition notes:

## Duplicates / conflicts

- None (single reviewer).

## Verification re-run (reviewer)

- `dotnet test -c Release` in `tests/tools/dev-cli-tests`: 315 passed, 0 failed.
- `dotnet build tools/dev-cli/dev.cs --nologo -v q`: exit 0.
- No live deploy (no cluster or subscription in this walk).
