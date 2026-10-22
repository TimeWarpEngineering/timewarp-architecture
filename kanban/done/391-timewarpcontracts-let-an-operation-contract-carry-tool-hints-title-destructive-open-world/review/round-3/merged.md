# Round 3 — merged findings
**Date:** 2026-10-22
**Sources:** general (review oracle, claude-opus-5-5)

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 2 | 0 |
| nit | 0 | 3 | 0 |

## Issues

### M1 — Severity: suggestion — Status: fixed
- File: source/foundation/timewarp-contracts/destructive-attribute.cs:22
- Description: The destructive hint docs disagreed with MCP and with each other.
- Source: general (round 1)
- Disposition notes: Fixed in `13dd8cef6` (verified in round 2).

### M2 — Severity: suggestion — Status: fixed
- File: skills/tw-web-api-contracts/references/neutral-operations.md:72
- Description: The generator's blank-name rule appeared only in the Design regions.
- Source: general (round 1)
- Disposition notes: Fixed in `13dd8cef6` (verified in round 2).

### M3 — Severity: nit — Status: fixed
- File: tests/analyzers/timewarp-architecture-sourcegenerator-tests/contracts-generator-tests.cs:919
- Description: No test covered a whitespace name with hints, and none covered an explicit `[Destructive(true)]`.
- Source: general (round 1)
- Disposition notes: Fixed in `13dd8cef6` (verified in round 2).

### M4 — Severity: nit — Status: fixed
- File: kanban/to-do/391-…/task.md:59
- Description: The checklist claimed a release had been cut.
- Source: general (round 1)
- Disposition notes: Fixed in `13dd8cef6` (verified in round 2).

### M5 — Severity: nit — Status: fixed
- File: tests/foundation/foundation-contracts-tests/operation-catalog-tool-hint-tests.cs:24 (also tests/analyzers/timewarp-architecture-sourcegenerator-tests/contracts-generator-tests.cs:955)
- Description: Two test fixtures contradicted the destructive meaning M1 documented.
- Source: general (round 2)
- Disposition notes: Fixed in `d87c02ded`. The explicit-false fixtures are `notes.append` (additive, `HttpVerb.Post`). The explicit-true, bare, blank-name, and blank-title destructive fixtures are a `Command` on `HttpVerb.Delete`. Tests pass 47/47 and 149/149.

## Duplicates / conflicts

- None. No new findings in round 3.
