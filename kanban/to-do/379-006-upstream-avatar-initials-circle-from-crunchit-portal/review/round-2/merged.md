# Round 2 — merged findings
**Date:** 2026-10-22
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 1 | 0 |
| nit | 0 | 1 | 0 |

## Issues

### M1 — Severity: suggestion — Status: fixed
- File: tests/container-apps/web/web-spa-integration-tests/components/avatar-render-tests.cs:40
- Description: The default-size test passed `Size = Medium` explicitly, so the property default
  was not pinned.
- Source: general (round 1)
- Disposition notes: Fixed in b336a866e. The test renders with only `Name` and asserts that no
  `twe-avatar--` modifier is present. Verified in round 2.

### M2 — Severity: nit — Status: fixed
- File: source/container-apps/web/projects/web-spa/components/elements/avatar-initials.cs:26
- Description: The empty-word-list and empty-text-element guards could never fire.
- Source: general (round 1)
- Disposition notes: Fixed in b336a866e. Both guards are removed. `FirstTextElement` calls
  `StringInfo.GetNextTextElement`. Verified in round 2. The split and the whitespace guard use
  the same whitespace definition.

## New findings

- None.

## Duplicates / conflicts

- None. There was one reviewer.

## Misread check

- The implementer restatement still matches the task's Description and Checklist. No misread.
