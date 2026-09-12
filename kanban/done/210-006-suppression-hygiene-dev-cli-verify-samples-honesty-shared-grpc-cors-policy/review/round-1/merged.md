# Round 1 — merged findings
**Date:** 2026-09-12
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 0 | 0 |
| nit | 0 | 0 | 0 |

## Issues

No issues raised.

## Duplicates / conflicts

- None. Single general reviewer; nothing to collapse.
- Orchestrator independently re-ran `dotnet run tools/dev-cli/dev.cs -- verify-samples` (empty-set message, exit 0) and confirmed `ExamplePolicy` still overrides only the 1-arg `Apply`. That throw path is unused and matches the fail-loud Enumeration contract — not carried as a finding.
