# Round 2 — merged findings
**Date:** 2026-10-28
**Sources:** general
**Scope:** fix delta `f029de790..309942e66`

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 1 | 1 | 0 |
| suggestion | 0 | 1 | 0 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: source/container-apps/web/projects/web-spa/features/agent-links/agent-link-onboarding-prompt.cs
- Description (round 1): the human-ux step relied on a bearer the script discarded.
- Source: general (round 1)
- Disposition notes: Fixed in `309942e66`. The first block saves the `keyId` and `linkId` files and unsets the bearer. The second block mints a fresh Token.v1 bearer from the saved key and GETs `human-ux`. Re-verified in round 2.

### M2 — Severity: suggestion — Status: fixed
- File: source/container-apps/web/projects/web-spa/features/agent-links/agent-link-onboarding-prompt.cs
- Description (round 1): nothing tied the prompt's routes and field names to the contracts.
- Source: general (round 1)
- Disposition notes: Fixed in `309942e66`. Routes, verbs, JSON names, prefixes, and scope come from the contracts and `AgentKeyProof`. The new `Build_Should` test serializes the contracts and checks the prompt against them, and it passed in round 2.

### M3 — Severity: bug — Status: open
- File: source/libraries/timewarp-identity/ceremonies/agent-key/agent-key-proof.cs:91
- Description: `AgentKeyProof.GetPrefix` is now public, and the template's web-spa calls it. `<Version>` and the `TimeWarp.Identity` pin are still `2.0.0-beta.27`, which is already on NuGet with `GetPrefix` private. Generated apps run in package mode, so they fail to compile (CS0122). `dev template-smoke` packs local source as `2.0.0-smoke` and hides this. `dev check-version` fails: "Version 2.0.0-beta.27 was already released."
- Suggestion: Bump `<Version>` in `source/Directory.Build.props` to 2.0.0-beta.28 and every platform pin in `Directory.Packages.props` in the same commit. Re-run `dev check-version` until it passes. Keep `GetPrefix` public; do not copy the literals back.
- Source: general (round 2)
- Disposition notes:

## Duplicates / conflicts

- None (single reviewer).
