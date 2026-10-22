# Round 2 — merged findings
**Date:** 2026-10-22
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 1 | 1 | 0 |
| nit | 0 | 1 | 0 |

## Issues

### M1 — Severity: suggestion — Status: fixed
- File: skills/tw-deploy/references/azure-container-apps-neon.md:23
- Description: The shipped section read as a past-tense spike log.
- Source: general (round 1)
- Disposition notes: Fixed in `28369a335`. Present-tense Aspire 13.6.2 facts. Task ids stay as
  parentheticals. The run narrative moved to task.md Results. The round-1 phrase `rg` is empty.

### M2 — Severity: nit — Status: fixed
- File: skills/tw-deploy/references/azure-container-apps-neon.md:39
- Description: "Container registry configuration" was named without a link.
- Source: general (round 1)
- Disposition notes: Fixed in `28369a335`. It links `https://aspire.dev/app-host/container-registry/`,
  which returns 200 and is titled "Container registry configuration". The page contains
  `WithContainerRegistry(environment)`.

### M3 — Severity: suggestion — Status: open
- File: skills/tw-deploy/references/azure-container-apps-neon.md:99
- Description: The token paragraph now says the token "lives as an AppHost user secret" and
  "`dev deploy` copies it". That states as current behavior something that does not exist. No
  dev-cli code handles a GHCR or registry token. The round-1 text used "would". M1 targeted
  past-tense narration, not that conditional.
- Suggestion: Restore the conditional or intended-design wording. Alternatively, state that
  `dev deploy` does not forward a registry token today.
- Source: general
- Disposition notes:

## Duplicates / conflicts

- None.
