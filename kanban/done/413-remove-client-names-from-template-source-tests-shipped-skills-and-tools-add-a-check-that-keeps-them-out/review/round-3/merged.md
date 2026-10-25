# Round 3 — merged findings
**Date:** 2026-10-25
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 1 | 0 |
| suggestion | 0 | 1 | 0 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: tests/tools/dev-cli-tests/entra-tenant-tests.cs
- Description: The case-variant Entra tests used an all-digit GUID, so they were vacuous.
- Source: general (round 1)
- Disposition notes: Fixed in `70a5819ed`; untouched by the rework. The dev-cli suite (342/342) still passes in round 3.

### M2 — Severity: suggestion — Status: fixed
- File: tools/dev-cli/services/client-name-scan.cs
- Description: Scan roots were hardcoded and missed packed paths.
- Source: general (round 1)
- Disposition notes: Superseded by the plain-search rework (`4fa6334cb`, Steve 2026-10-25). The
  scan now covers every tracked file outside `documentation/`, the board tree, and
  `skills/*/analysis/`, a superset of the packed paths, so the original gap stays closed.

No new findings in round 3.

## Duplicates / conflicts

None.
