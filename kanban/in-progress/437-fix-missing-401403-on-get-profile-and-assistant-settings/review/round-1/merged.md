# Round 1 — merged findings
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
- Description: `[EndpointAllowAnonymous]` reasons now appear verbatim in the public OpenAPI description. GetProfile's reason contains internal history (`Page stays PermissionIds.ProfileRead (task 148 D3 / 182-003).`), so the profile GET this task targets now shows kanban task ids in the published spec.
- Suggestion: Rewrite the reason for spec readers, for example: "Dual-mode read: an anonymous caller gets the demo profile, and a signed-in caller gets their stored profile." Keep the history in the Design region. Optionally, note on `EndpointAllowAnonymousAttribute.Reason` that the text is published in OpenAPI.
- Source: general
- Disposition notes:

## Duplicates / conflicts

- None (single reviewer).
