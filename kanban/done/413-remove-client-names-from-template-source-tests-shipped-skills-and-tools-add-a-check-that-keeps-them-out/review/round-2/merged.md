# Round 2 — merged findings
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
- File: tests/tools/dev-cli-tests/entra-tenant-tests.cs:96 (also :148)
- Description: The case-variant Entra tests used an all-digit GUID, so they were vacuous.
- Source: general (round 1)
- Disposition notes: Fixed in `70a5819ed`. Both tests use `aaaaaaaa-0000-0000-0000-000000000001`
  against its upper-case form. The union asserts the first casing. Re-verified in round 2.

### M2 — Severity: suggestion — Status: fixed
- File: tools/dev-cli/services/client-name-scan.cs:104
- Description: Scan roots were hardcoded and missed packed paths.
- Source: general (round 1)
- Disposition notes: Fixed in `70a5819ed`. `TryListPackedFiles` builds the set from the pack
  allow-list (`Content`, `None` with `Pack=true`, minus `Exclude`). `$(…)` and repo-escaping patterns
  fail the scan. `RealPack_Should_CoverAllowListPaths` and `PackedFiles_Given_` pin the set.
  Re-verified in round 2: scan exit 0, 353/353 dev-cli tests.

## Duplicates / conflicts

- None. Round 2 raised no new findings.
