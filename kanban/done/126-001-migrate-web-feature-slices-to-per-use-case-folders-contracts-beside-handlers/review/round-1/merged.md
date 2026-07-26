# Round 1 — merged findings
**Date:** 2026-07-26
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 1 | 0 |
| suggestion | 0 | 0 | 0 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: source/container-apps/web/features/chat/receive-message/receive-message-contracts.cs:8
- Description: Design region narrated the pre-migration folder structure — "the folder
  (server-to-client) encodes direction" — after U2 collapsed that direction-named folder into
  the `receive-message/` use-case folder. Stale-Design-region class (AGENTS.md: "A Design region
  describing the old approach is a bug you just introduced").
- Suggestion: Reword so direction is carried by the regions/type documentation, not a folder
  name that no longer exists.
- Source: general
- Disposition notes: Fixed by orchestrator 2026-07-26 — comment now reads "the Purpose region
  records the direction (server-to-client) because the type alone cannot" (the Purpose region
  does exactly that, line 2). Comment-only change; no behavior impact. Verified in round-2
  orchestrator verification.

## Duplicates / conflicts

None — single reviewer, single finding.
