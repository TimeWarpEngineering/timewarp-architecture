# Round 1 — merged findings
**Date:** 2026-10-12
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 2 | 0 |
| nit | 0 | 1 | 0 |

## Issues

### M1 — Severity: suggestion — Status: fixed
- File: tools/dev-cli/endpoints/open-command.cs:38,47; Design regions in program.cs, constants.cs, deploy-operate.cs, open-command.cs
- Description: Shipped help text and comments say "36371 in this repo" and cite private `ganda generate-port`. Both are wrong in generated apps.
- Suggestion: Name the committed parameter, not the number.
- Source: general
- Disposition notes: The help text and Design regions now say "the ingress-port parameter committed in the AppHost appsettings.json". The ganda reference is gone. 36371 stays only in appsettings.json, the template symbol and dev-cli-tests (not packed).

### M2 — Severity: suggestion — Status: fixed
- File: tools/dev-cli/endpoints/open-command.cs:189
- Description: Compose did not validate a port from `.env` or the parameter.
- Suggestion: ParsePort with its source.
- Source: general
- Disposition notes: OpenComposeAsync now refuses with `ParsePort(port, source)`, so the error names where the bad value came from.

### M3 — Severity: nit — Status: fixed
- File: tools/dev-cli/endpoints/template-smoke-command.cs:543
- Description: With fewer than two generations, it printed a "differ" claim it had not checked.
- Suggestion: Print a separate message.
- Source: general
- Disposition notes: There is now a separate message that says the per-app check was skipped.

## Duplicates / conflicts

- None.

## Re-verification of fixes

- `dev build`: 0 warnings, 0 errors. dev-cli-tests: 211/211. A grep finds 36371 only in appsettings.json, template.json and dev-cli-tests.
