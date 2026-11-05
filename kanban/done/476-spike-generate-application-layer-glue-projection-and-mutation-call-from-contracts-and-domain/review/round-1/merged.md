# Round 1 — merged findings
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
- Description: The named-argument example uses PascalCase names (`RoleId:`, `Name:`). The repo's
  contract constructors take camelCase parameters (`get-role-contracts.cs:67-71`), so the example
  fails with CS1739.
- Suggestion: `new Response(roleId: role.Id.Value, name: role.Name, …)`.
- Source: general
- Disposition notes: Fixed: example now reads `new Response(roleId: role.Id.Value, name: role.Name, …)`.

### M2 — Severity: suggestion — Status: fixed
- File: skills/tw-aggregate-pattern/SKILL.md:120
- Description: "Mutation calls pass the principal and the clock" is stated as universal. The Role
  mutations take neither (`role-domain.cs:53,71,79,103`).
- Suggestion: Make it conditional: when the mutation records who or when, the handler passes the
  principal and the clock, and the root does not read ambient context.
- Source: general
- Disposition notes: Fixed: bullet renamed "The handler supplies who and when"; principal and clock are passed only when the mutation records them, and the root does not read ambient context. Results line updated to match.

### M3 — Severity: nit — Status: fixed
- File: kanban/…/476-…/proposal.md:81 and task.md Results
- Description: `feedback-admin-tests.cs:420` is a mock-factory test, not a redaction assertion on
  the handler.
- Suggestion: Cite `feedback-admin-tests.cs:324` (and `feedback-triage-tests.cs:177`) instead.
- Source: general
- Disposition notes: Fixed: `proposal.md` and task.md Results cite `feedback-admin-tests.cs:263` and `:324` plus `feedback-triage-tests.cs:177`.

## Duplicates / conflicts

- None (single reviewer).
