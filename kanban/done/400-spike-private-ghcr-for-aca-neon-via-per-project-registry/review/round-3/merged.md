# Round 3 — merged findings
**Date:** 2026-10-22
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 2 | 0 |
| nit | 0 | 1 | 0 |

## Issues

### M1 — Severity: suggestion — Status: fixed
- File: skills/tw-deploy/references/azure-container-apps-neon.md:23
- Description: The shipped section read as a past-tense spike log.
- Source: general (round 1)
- Disposition notes: Fixed in `28369a335` (verified round 2).

### M2 — Severity: nit — Status: fixed
- File: skills/tw-deploy/references/azure-container-apps-neon.md:39
- Description: "Container registry configuration" was named without a link.
- Source: general (round 1)
- Disposition notes: Fixed in `28369a335` (verified round 2).

### M3 — Severity: suggestion — Status: fixed
- File: skills/tw-deploy/references/azure-container-apps-neon.md:99
- Description: The Token paragraph stated a `dev deploy` GHCR token copy as current behavior, and
  dev-cli has no such copy.
- Source: general (round 2)
- Disposition notes: Fixed in `e2ab3757a`. The paragraph says `dev deploy` does not forward a
  registry token, and the user secret and the copy are now intended design. `rg` over
  `tools/dev-cli` confirms that no token is forwarded.

## Duplicates / conflicts

- None. Round 3 raised no new findings.
