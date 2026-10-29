# Round 3 — merged findings
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
- Disposition notes: Still unfixed. HEAD is `0c6ffb48`, the round-1 base. Both fix passes (`oracle-implement-fix-2-20261029t005056091z.log`, `oracle-implement-fix-3-20261029t005333177z.log`) returned `Done` with `num_turns: 1` and no tool calls. The implementer did no work, so another automatic fix pass is unlikely to help. An operator needs to check the implementer-grok harness, fix M1 by hand, or decide to mark it wontfix.

## Duplicates / conflicts

- None. The delta was empty, so there are no new findings.
