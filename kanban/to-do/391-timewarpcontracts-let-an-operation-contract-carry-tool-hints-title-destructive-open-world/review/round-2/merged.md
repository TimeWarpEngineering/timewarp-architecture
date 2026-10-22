# Round 2 — merged findings
**Date:** 2026-10-22
**Sources:** general (review oracle, claude-opus-5-5)

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 2 | 0 |
| nit | 1 | 2 | 0 |

## Issues

### M1 — Severity: suggestion — Status: fixed
- File: source/foundation/timewarp-contracts/destructive-attribute.cs:22
- Description: The destructive hint docs disagreed with MCP and with each other.
- Source: general (round 1)
- Disposition notes: Fixed in `13dd8cef6`. The XML docs, the Design region, the readme, `SKILL.md`, and `neutral-operations.md` use the MCP additive-versus-destructive meaning and state the default-true rule and the not-read-only rule. The readme example is `notes.append`.

### M2 — Severity: suggestion — Status: fixed
- File: skills/tw-web-api-contracts/references/neutral-operations.md:72
- Description: The generator's blank-name rule appeared only in the Design regions.
- Source: general (round 1)
- Disposition notes: Fixed in `13dd8cef6`. The readme and `neutral-operations.md` now state the rule.

### M3 — Severity: nit — Status: fixed
- File: tests/analyzers/timewarp-architecture-sourcegenerator-tests/contracts-generator-tests.cs:919
- Description: No test covered a whitespace name with hints, and none covered an explicit `[Destructive(true)]`.
- Source: general (round 1)
- Disposition notes: Fixed in `13dd8cef6`. Each reader has a whitespace-name test, and the contracts generator has an explicit-true test. Generator tests pass 149 of 149.

### M4 — Severity: nit — Status: fixed
- File: kanban/to-do/391-…/task.md:59
- Description: The checklist claimed a release had been cut.
- Source: general (round 1)
- Disposition notes: Fixed in `13dd8cef6`. The item is unticked and reworded to `dev release` after merge.

### M5 — Severity: nit — Status: open
- File: tests/foundation/foundation-contracts-tests/operation-catalog-tool-hint-tests.cs:23 (also tests/analyzers/timewarp-architecture-sourcegenerator-tests/contracts-generator-tests.cs:953)
- Description: Two test fixtures contradict the destructive meaning M1 documented. The catalog test pairs `rename_symbol`, which overwrites, with `DestructiveAttribute(false)`. The new explicit-true generator test puts `[Destructive(true)]` on a `Query` with `HttpVerb.Get`, but the docs now say the hint does not apply to a query.
- Suggestion: Use an additive operation for the false case, or mark the rename `[Destructive]`. Make the explicit-true fixture a `Command` with a non-GET verb.
- Source: general (round 2)
- Disposition notes:

## Duplicates / conflicts

- None. M5 is what M1 left over in test fixtures, which M1's file list did not name.
