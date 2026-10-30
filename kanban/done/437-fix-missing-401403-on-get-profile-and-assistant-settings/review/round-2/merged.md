# Round 2 — merged findings
**Date:** 2026-10-29
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 1 | 0 | 0 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: suggestion — Status: open
- File: source/container-apps/web/features/profile/get-profile/get-profile-contracts.cs:28
- Description: The GetProfile `[EndpointAllowAnonymous]` reason is published in OpenAPI and still contains internal task ids (`task 148 D3 / 182-003`).
- Suggestion: Rewrite the reason for spec readers, for example: "Dual-mode read: an anonymous caller gets the demo profile, and a signed-in caller gets their stored profile." Keep the history in the Design region.
- Source: general (round 1, carried)
- Disposition notes: Round 2 found no product commits since round 1 (HEAD = round-1 base `0c6ffb48`). The fix pass has not run yet.

## Duplicates / conflicts

- None. No new findings on the delta, which was empty.
