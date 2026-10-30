# Round 5 — merged findings
**Date:** 2026-10-29
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 1 | 0 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: suggestion — Status: fixed
- File: source/container-apps/web/features/profile/get-profile/get-profile-contracts.cs:29
- Description: The GetProfile `[EndpointAllowAnonymous]` reason was published in OpenAPI and contained internal task ids (`task 148 D3 / 182-003`).
- Suggestion: Change only the attribute reason to spec-reader prose, and keep the history in the Design region.
- Source: general (round 1, carried through rounds 2–4)
- Disposition notes: Fixed in `75b50b45`. The reason now reads "This read is dual-mode: an anonymous caller gets the demo profile, and a signed-in caller gets their stored profile." The reviewer re-ran `OpenApiAnonymousIntent_` (6/6 passed). No other `[EndpointAllowAnonymous]` reason under `source/` contains a task id.

## Duplicates / conflicts

- None. The delta has no new findings.
