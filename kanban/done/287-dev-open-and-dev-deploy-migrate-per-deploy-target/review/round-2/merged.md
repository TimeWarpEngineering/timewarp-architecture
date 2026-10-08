# Round 2 — merged findings
**Date:** 2026-10-09
**Sources:** general (re-review of 42f60566b + round-1 M1–M7)

## Counts (final, carried IDs + new)

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 3 | 0 |
| suggestion | 0 | 1 | 1 |
| nit | 0 | 3 | 1 |

## Resolved prior

- M1 bug — fixed (verified: pwsh backtick escapes, test asserts form)
- M2 bug — fixed (verified: `dev deploy --help` lists deploy options + `migrate`; `--capabilities` intact; Nuru prints the empty route pattern as `deploy ` — cosmetic, upstream)
- M3 bug — fixed (verified: report from finally, no double print, Ctrl+C test)
- M4 suggestion — fixed (verified)
- M5 suggestion — wontfix (rationale holds per re-review)
- M6 nit — wontfix (rationale holds per re-review)
- M7 nit — fixed (verified)

## Issues

### M8 — Severity: nit — Status: fixed
- File: tools/dev-cli/services/deploy-operate.cs (BuildPipedCommandDisplay / BuildComposeMigrateArguments)
- Description: new helpers were inserted between `BuildComposeMigrateArguments`' `<summary>` and the method, leaving one method with two summaries and the other with none.
- Suggestion: move the summary back.
- Source: general
- Disposition notes: summary moved back onto `BuildComposeMigrateArguments`.

### M9 — Severity: nit — Status: fixed
- File: tools/dev-cli/endpoints/deploy-migrate-command.cs (Design step 4/5), tools/dev-cli/endpoints/open-command.cs (Design)
- Description: step 5 sentence missing a verb; edited Design lines exceed the region's wrap width.
- Suggestion: reword and rewrap.
- Source: general
- Disposition notes: reworded ("a failed delete is reported from the finally with the command to run by hand, so it is printed even when Ctrl+C propagates") and rewrapped in both files.

## Duplicates / conflicts

- None.
