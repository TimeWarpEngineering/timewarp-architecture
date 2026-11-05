# Round 2 — merged findings
**Date:** 2026-11-05
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 2 | 0 |
| nit | 0 | 1 | 0 |

## Issues

### M1 — Severity: suggestion — Status: fixed
- File: skills/tw-aggregate-pattern/SKILL.md:111
- Description: The named-argument example used PascalCase names.
- Source: general (round 1)
- Disposition notes: Checked in round 2. The camelCase `roleId:` and `name:` match
  `get-role-contracts.cs:67-68`.

### M2 — Severity: suggestion — Status: fixed
- File: skills/tw-aggregate-pattern/SKILL.md:120
- Description: The bullet said every mutation call passes the principal and the clock.
- Source: general (round 1)
- Disposition notes: Checked in round 2. The bullet is now conditional, and the root does not read
  ambient context.

### M3 — Severity: nit — Status: fixed
- File: kanban/…/476-…/proposal.md:81 and task.md Results
- Description: `feedback-admin-tests.cs:420` was cited as a redaction test, but it is not one.
- Source: general (round 1)
- Disposition notes: Checked in round 2. Lines `:263` and `:324` and `feedback-triage-tests.cs:177`
  each assert redaction or hiding.

## New findings

None.

## Duplicates / conflicts

- None (single reviewer).
