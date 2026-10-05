# Round 1 — merged findings
**Date:** 2026-10-06
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 2 | 0 | 0 |
| nit | 2 | 0 | 0 |

## Issues

### M1 — Severity: suggestion — Status: open
- File: source/foundation/foundation-contracts-generators/contracts-generator.offerable.cs:112
- Description: `[Offerable]` on a non-partial class, a record, or a global-namespace contract is silently ignored: no Offer is generated, no TWE is reported, and TWA0031 skips it. That makes it fail-open.
- Suggestion: report a fail-closed generator diagnostic (new TWE014) for an `[Offerable]` type that cannot carry generated members.
- Source: general (Issue 1)
- Disposition notes:

### M2 — Severity: nit — Status: open
- File: source/foundation/foundation-contracts-generators/contracts-generator.offerable.cs:193
- Description: TWE012 says "not a property of Command" for a duplicate entry or for the auth-filled UserId.
- Suggestion: make the message cover those cases.
- Source: general (Issue 2)
- Disposition notes:

### M3 — Severity: nit — Status: open
- File: source/container-apps/web/features/identity/offered-action-contracts.cs:97
- Description: the hand-written `Credentials.LinkMicrosoft365` uses a different naming scheme from the generated `Identity.*` names, and the skill does not say which scheme to use.
- Suggestion: rename it to `Identity.LinkMicrosoft365` and document the scheme for hand-written offers.
- Source: general (Issue 3)
- Disposition notes:

### M4 — Severity: suggestion — Status: open
- File: tests/analyzers/timewarp-architecture-sourcegenerator-tests/contracts-generator-offerable-tests.cs:1
- Description: no tests cover nested contracts, inherited Command properties, UserInput naming a route parameter, or the TWE013 location. The TWA0031 tests hand-write the generator's output.
- Suggestion: add the generator cases.
- Source: general (Issue 4)
- Disposition notes:

## Duplicates / conflicts

- None (single reviewer).
