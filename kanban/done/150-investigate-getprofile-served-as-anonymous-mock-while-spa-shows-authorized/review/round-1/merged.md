# Round 1 — merged findings
**Date:** 2026-08-05
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 0 | 0 |
| nit | 0 | 0 | 0 |

## Issues

None raised. Gate verification (independent re-run by the reviewer): `dev build` 0/0;
co-located runfile 10/10; integration suite 112 total / 16 failed / 95 passed / 1 skipped with
all 16 failures pre-existing in agent-key/credential features (none profile-related) and the
new `GetProfileSession` class 2/2 green in isolated re-run. Repo-wide grep confirmed no other
web-family consumer of `ICurrentUserService` remains.

## Duplicates / conflicts

None — single reviewer.
