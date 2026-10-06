# Round 2 — merged findings (re-verification of round-1 fixes)
**Date:** 2026-10-07
**Sources:** review oracle (re-verify of fix delta)

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 1 | 3 |
| nit | 0 | 2 | 2 |

## Resolved prior

- M4 fixed — guard widened + positive control; dev-cli-tests 130/130 (was 129, +1 new test).
- M5 fixed — `echo n | dotnet run tools/dev-cli/dev.cs -- deploy` still prints the no-terminal refusal (stdin redirected); the declined path is reachable only from a terminal. `dotnet build tools/dev-cli/dev.cs` clean.
- M6 fixed — stronger assertion passes.
- M1, M2, M3, M7, M8 — wontfix, rationale in round-1/merged.md.

## New findings on the fix delta

None.
