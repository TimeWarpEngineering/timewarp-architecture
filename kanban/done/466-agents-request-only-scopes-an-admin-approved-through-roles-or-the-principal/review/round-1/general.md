# Round 1 — general
**Date:** 2026-11-02
**Scope reviewed:** branch `task/466-agents-request-only-scopes-an-admin-approved-throu` vs merge-base with `master` (commits a012755c0, f9a137c39; 52 files, +2571 / −42). The migration Designer and model snapshot were spot-checked only.

## Summary

The change adds role and principal agent-scope stores (in-memory and EF), a migration, two admin
contracts with SPA editors, an `IAgentScopeApproval` union service, and a 403 `unapproved_scope`
check in `complete-agent-token-issuance`. That check runs after the unknown-scope 400, the
credential lookup, the proof, and the active check, and before the last-used stamp, so the
challenge is consumed first. Human role permissions still never reach the token. The tests
cover every case the task lists, and the existing token tests approve scopes through one shared
helper (`AgentScopeApprovals.ApproveAsync`) with their assertions unchanged. I re-ran the new
suite (4/4) and the whole web-server integration suite (434 passed, 1 skipped, 0 failed).

The main defect is a template one. The new EF stores are not in the `(!postgres)` exclude list,
so a generated app with `--postgres false` will not compile. The default-variant smoke cannot
catch that.

## Issues

### Issue 1 — Severity: bug
- File: .template.config/template.json:98
- Description: The `(!postgres)` exclude block lists `ef-role-permission-store-infrastructure.cs` and
  `role-permission-grant-entity-type-configuration-infrastructure.cs`, but not the four new EF files:
  `ef-role-agent-scope-store-infrastructure.cs`, `ef-principal-agent-scope-store-infrastructure.cs`,
  `role-agent-scope-grant-entity-type-configuration-infrastructure.cs`, and
  `principal-agent-scope-grant-entity-type-configuration-infrastructure.cs`. The same block excludes
  `source/container-apps/web/platform/postgres/**`, which removes `PostgresDbContext`. Both EF stores
  have a `PostgresDbContext Db` field (`ef-role-agent-scope-store-infrastructure.cs:19`,
  `ef-principal-agent-scope-store-infrastructure.cs:19`). A `--postgres false` app therefore fails with
  CS0246. `dev template-smoke` without `--matrix` builds only the default variant, so the gate the
  implementer ran cannot see this. CI's nightly flag-off matrix would fail.
- Suggestion: Add the four files to the `(!postgres)` exclude list beside the role-permission pair,
  then run `dev template-smoke --matrix` (at least the postgres-off variant) to prove the build.
- Status: open

### Issue 2 — Severity: suggestion
- File: source/container-apps/web/features/admin/roles/set-role-agent-scopes/set-role-agent-scopes-handler-application.cs:50
- Description: There are three copies of the "order by `AgentScopes.All`" helper: `OrderKnown` in
  `SetRoleAgentScopes.Handler`, in `SetPrincipalAgentScopes.Handler`, and in `AgentScopeApproval`.
  `GetRole` and `GetRoles` borrow `SetRoleAgentScopes.Handler.OrderKnown`, so one handler's static
  is used by two others. The copies must stay identical by hand, which breaks the repo rule
  "when two things must agree, generate one from the other or add a build check".
- Suggestion: Keep one canonical helper next to the catalog or in the authorization platform
  (for example a static `AgentScopeOrdering.OrderKnown` in `platform/authorization`), and call it
  from all five sites.
- Status: open

### Issue 3 — Severity: suggestion
- File: source/container-apps/web/projects/web-spa/features/admin/roles/pages/RoleDetailPage.razor:106
- Description: Every principal with no stored roles resolves to Member (`AgentScopeApproval` Design
  region). Self-registration is anonymous, so agent scopes put on the Member role go to every new
  agent and reopen the hole this task closes. Nothing warns the admin. The role editor shows the
  same checkboxes for Member as for any other role.
- Suggestion: On the Member role, show one plain sentence saying these scopes go to every
  self-registered agent. Alternatively refuse agent scopes on Member in `SetRoleAgentScopes`, but
  that is a product decision, so a warning is the smaller change. Record the choice in the
  `SetRoleAgentScopes` Design region.
- Status: open

### Issue 4 — Severity: suggestion
- File: source/container-apps/web/platform/authorization/agent-scope-approval-application.cs:34
- Description: No test covers the source marking the Principal page depends on. That is the
  `Effective` list with `Default` / `Role(roleId)` / `Principal` sources and the merge when
  one scope has several sources. The integration tests only check issuance success or refusal.
  `GetPrincipal` tests build the handler with empty stores and never assert `ApprovedAgentScopes`.
- Suggestion: Add one co-located Jaribu test, using in-memory stores and the real resolver, that
  gives one role scope and one principal grant on the same scope and asserts the
  `ApprovedAgentScopes` sources: identity:read is `default`, the role scope is `role` with the
  role id, the shared scope lists both sources, and the order follows `AgentScopes.All`.
- Status: open

### Issue 5 — Severity: nit
- File: source/container-apps/web/projects/web-spa/features/admin/principals/pages/PrincipalDetailPage.razor:167
- Description: The Principal page's "Save agent scopes" button stays enabled while the save runs.
  The role page's button is disabled while saving (`IsSavingAgentScopes`), so a double click posts twice.
- Suggestion: Add `Disabled` bound to `IsAnyActive(typeof(PrincipalState.SetPrincipalAgentScopesActionSet.Action))`, matching RoleDetailPage.
- Status: open
