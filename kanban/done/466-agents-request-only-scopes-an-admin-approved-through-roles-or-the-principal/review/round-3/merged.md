# Round 3 — merged findings
**Date:** 2026-11-02
**Sources:** general
**Scope:** Member-decision delta `5a60e94c5..d39a9aa98` (product commit `d0a9d35d9`)

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 1 | 0 |
| suggestion | 0 | 3 | 0 |
| nit | 0 | 1 | 0 |

Counts carry M1–M5 forward from round 2. Round 3 raised no new findings.

## Issues

### M1 — Severity: bug — Status: fixed
- File: .template.config/template.json
- Description: The four EF agent-scope files were missing from the `(!postgres)` exclude list.
- Source: general (round 1)
- Disposition notes: Fixed in 5a60e94c5 and verified in round 2. The delta does not touch it.

### M2 — Severity: suggestion — Status: fixed
- File: source/container-apps/web/platform/authorization/agent-scope-order-application.cs
- Description: There were three copies of `OrderKnown`.
- Source: general (round 1)
- Disposition notes: Fixed in 5a60e94c5 and verified in round 2. The delta does not touch it.

### M3 — Severity: suggestion — Status: fixed
- File: source/container-apps/web/projects/web-spa/features/admin/roles/pages/RoleDetailPage.razor
- Description: Member agent scopes reached every unassigned principal, including self-registered
  agents, and the editor gave no warning.
- Source: general (round 1)
- Disposition notes: The 5a60e94c5 warning was replaced by Steve's decision (2026-11-02, agents
  are not Members), implemented in d0a9d35d9. Self-registered agents no longer resolve to Member.
  The Member page sentence now says Member scopes reach roleless humans, including a human who
  holds an agent key. `MemberScopes_Should_NotApproveARolelessAgent` pins this: the reviewer
  re-ran it and it passed. The round-2 escalation is resolved.

### M4 — Severity: suggestion — Status: fixed
- File: source/container-apps/web/platform/authorization/agent-scope-approval-tests.cs
- Description: No test covered effective-set source marking.
- Source: general (round 1)
- Disposition notes: Fixed in 5a60e94c5 and verified in round 2.

### M5 — Severity: nit — Status: fixed
- File: source/container-apps/web/projects/web-spa/features/admin/principals/pages/PrincipalDetailPage.razor
- Description: Save agent scopes was not disabled while saving.
- Source: general (round 1)
- Disposition notes: Fixed in 5a60e94c5 and verified in round 2.

## New findings

None in the Member-decision delta. `round-3/general.md` lists two observations that are not
findings: an extra principal read per roleless resolution, and the `Human` default on
`SimulateEffectiveRoles`.

## Duplicates / conflicts

- Single reviewer. Nothing was collapsed.
