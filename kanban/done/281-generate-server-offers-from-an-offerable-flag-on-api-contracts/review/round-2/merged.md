# Round 2 — merged findings
**Date:** 2026-10-06
**Sources:** general

## Counts (final)

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 2 | 0 |
| nit | 0 | 4 | 0 |

## Resolved prior (round 1)

| ID | Severity | Status | Fix |
|----|----------|--------|-----|
| M1 | suggestion | fixed | a9f2a03cd: TWE014 is reported for an `[Offerable]` type that cannot carry the generated members (registered in the SSOT, Unshipped.md, AGENTS.md and both skills) |
| M2 | nit | fixed | a9f2a03cd: TWE012 carries a reason (names no property / listed more than once / auth-filled UserId) |
| M3 | nit | fixed | a9f2a03cd: `Identity.LinkMicrosoft365`; the `<Slice>.<Operation>` scheme is documented for hand-written offers |
| M4 | suggestion | fixed | a9f2a03cd: generator tests for nested contracts, inherited and hidden properties, route-parameter UserInput, the TWE013 location, TWE014 shapes and TWE012 reasons. The analyzer tests still hand-write the generated shape; that drift is covered by the real SPA build (TWA0031/TWA0029 over the generated records) |

## Issues

### M5 — Severity: nit — Status: fixed
- File: source/foundation/foundation-contracts-generators/contracts-generator.offerable.cs:249
- Description: `CanCarryOffer` accepted generic contracts or containers. `ToTarget` drops type parameters, so the Offer would land on a separate non-generic class, which is a fail-open path.
- Suggestion: reject generic types with TWE014.
- Source: general
- Disposition notes: fixed. `CanCarryOffer` rejects `IsGenericType` on the contract and its containers. The TWE014 message, description, SSOT region, Unshipped.md, AGENTS.md, both skills and the generator Design region now say "non-generic". There is a new `generic` TWE014 test case; the sourcegenerator suite passes 119/119.

### M6 — Severity: nit — Status: fixed
- File: source/container-apps/web/projects/web-spa/features/application/command-palette/command-palette-roster.cs:16
- Description: the Design text said "rows group by state", but the Link Microsoft 365 row now groups under the "Identity" slice prefix.
- Suggestion: reword it.
- Source: general
- Disposition notes: fixed. The Design region now says rows group by the catalog-name prefix (the owning state, or the slice for offer names).

## Duplicates / conflicts

- None.
