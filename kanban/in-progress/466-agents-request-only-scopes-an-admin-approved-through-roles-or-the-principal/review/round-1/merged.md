# Round 1 — merged findings
**Date:** 2026-11-02
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 1 | 0 | 0 |
| suggestion | 3 | 0 | 0 |
| nit | 1 | 0 | 0 |

## Issues

### M1 — Severity: bug — Status: open
- File: .template.config/template.json:98
- Description: The four new EF files (`ef-role-agent-scope-store-infrastructure.cs`,
  `ef-principal-agent-scope-store-infrastructure.cs`, and their two entity-type configurations under
  `platform/authorization/`) are missing from the `(!postgres)` exclude list. The same block
  removes `platform/postgres/**` (`PostgresDbContext`), so a `--postgres false` app fails CS0246.
  The default-variant `dev template-smoke` that ran cannot see it.
- Suggestion: Add the four files beside the role-permission pair and prove with
  `dev template-smoke --matrix` (the postgres-off variant at minimum).
- Source: general
- Disposition notes:

### M2 — Severity: suggestion — Status: open
- File: source/container-apps/web/features/admin/roles/set-role-agent-scopes/set-role-agent-scopes-handler-application.cs:50
- Description: There are three hand-kept copies of `OrderKnown`, in SetRoleAgentScopes,
  SetPrincipalAgentScopes, and AgentScopeApproval. GetRole and GetRoles also reach into
  `SetRoleAgentScopes.Handler.OrderKnown`.
- Suggestion: Keep one helper in `platform/authorization` and call it from all five sites.
- Source: general
- Disposition notes:

### M3 — Severity: suggestion — Status: open
- File: source/container-apps/web/projects/web-spa/features/admin/roles/pages/RoleDetailPage.razor:106
- Description: Agent scopes put on the Member role go to every anonymously self-registered agent,
  because unassigned principals resolve to Member. The role editor gives no warning.
- Suggestion: On the Member role, add one sentence of warning on RoleDetailPage and record the
  rule in the SetRoleAgentScopes Design region. Refusing Member is the alternative, and that
  is Steve's call.
- Source: general
- Disposition notes:

### M4 — Severity: suggestion — Status: open
- File: source/container-apps/web/platform/authorization/agent-scope-approval-application.cs:34
- Description: No test covers the effective-set source marking (default, role with role id,
  principal, and a scope with several sources) that the Principal page shows.
- Suggestion: Add one co-located Jaribu test over in-memory stores that asserts the
  `ApprovedAgentScopes` sources and their order.
- Source: general
- Disposition notes:

### M5 — Severity: nit — Status: open
- File: source/container-apps/web/projects/web-spa/features/admin/principals/pages/PrincipalDetailPage.razor:167
- Description: The Principal page "Save agent scopes" button is not disabled while saving.
  RoleDetailPage's button is.
- Suggestion: Bind `Disabled` to the SetPrincipalAgentScopes action's active state.
- Source: general
- Disposition notes:

## Duplicates / conflicts

- Single reviewer. Nothing was collapsed.

## Verified (no finding)

- The issuance order holds. The challenge is consumed, then the unknown scope gets 400, then the
  credential lookup, proof, and active check run, and only then the 403 `unapproved_scope`.
  The refusal comes before the last-used stamp and before Issue.
- Human role permissions are not consulted. Approval reads only the role agent-scope store and
  the principal agent-scope store.
- The existing token tests (credential ceremony helper, agent-protected endpoint, agent token,
  metered capability, sunny paths) approve scopes through the shared `AgentScopeApprovals`
  helper, and their assertions are unchanged. The other token tests request only `identity:read`
  or mint tokens without going through issuance.
- Reviewer re-ran the tests in this worktree. `dotnet test -c Release -- --filter-class AgentScopeApproval_` gave 4/4.
  The full `web-server-integration-tests` run gave 434 passed, 1 skipped, 0 failed.
