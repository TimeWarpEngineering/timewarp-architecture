# Round 2 — merged findings
**Date:** 2026-11-05
**Sources:** general

## Counts

Final statuses across both rounds. Round 2 raised no new findings.

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 1 | 0 |
| suggestion | 0 | 1 | 0 |
| nit | 0 | 1 | 0 |

## Resolved prior (round 1, re-verified against 76f95a527)

### M1 — Severity: bug — Status: fixed
- File: source/container-apps/web/features/feedback/feedback-activity-entry-contracts.cs:31
- Re-verified: `[Computed]` is removed. `ContractAgreementTypes.ReadCompatible` is used only in the
  TWA0041 walk, and TWA0039/TWA0040 stay strict. The narrowing, reference-widening, and
  details-strict tests pass. web-application builds with 0 warnings.

### M2 — Severity: suggestion — Status: fixed
- File: source/analyzers/timewarp-architecture-convention-analyzers/contract-agreement-projection.cs:66
- Re-verified: `TryGetWrapper` requires one non-`[Computed]` member that is unmatched and is a
  contract class. The direct-projection test and the two-unmatched-members test pass.

### M3 — Severity: nit — Status: fixed
- File: source/analyzers/timewarp-architecture-attributes/computed-attribute.cs:10
- Re-verified: the Design and the summary list the response, the list item, child classes, and
  wrapped classes. They say a redacted value is `T?`.

## Issues

None new.

## Duplicates / conflicts

- None.
