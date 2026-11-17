# Round 1 — general
**Date:** 2026-11-05
**Scope reviewed:** commit 5e6bb6281 (Role aggregate, RoleCatalog, three Roles handlers, AgentScopeOrder Design region, web-domain role tests), plus call sites of `Role.AgentScopes` and the EF mapping

## Summary

`Role.AgentScopes` now returns a new list sorted by `RoleCatalog.AgentScopeRank` over the private
`AgentScopeList`, so read order is catalog order regardless of set order or EF load order. The EF
navigation uses `HasField("AgentScopeList")` with `PropertyAccessMode.Field`
(`role-entity-type-configuration-infrastructure.cs:83`), so the copying getter does not affect
materialization or change tracking. GetRole, GetRoles, and SetRoleAgentScopes project the property
as is; `rg OrderKnown` now finds only SetPrincipalAgentScopes and `AgentScopeApproval`, matching the
updated `AgentScopeOrder` Design region. Unknown scopes stay rejected (contract validator 400;
`Role.SetAgentScopes` throws), and the SetRoleAgentScopes Design region states it, as the task asked.
`OrderBy` is stable, so out-of-catalog scopes keep relative order at the end where `Invariants` sees
them. Domain tests cover reverse-set order, out-of-order backing list (EF load), and child identity.
Re-ran `dotnet run --project tests/container-apps/web/web-domain-tests`: 57/57 passed. Low risk.

The implementer restatement matches the task (it picks "rejected" for unknown scopes, which the
task allowed as long as the Design region says which). No misread.

## Issues

### Issue 1 — Severity: nit
- File: source/container-apps/web/features/admin/roles/role-domain.cs:16
- Description: The edited Design region was not reflowed; line 16 is 144 characters
  ("outside the catalog sorts last, so Invariants still sees it. A child already present is kept as
  the same instance and only missing values are"), while the surrounding region wraps near 100.
- Suggestion: Reflow the Design paragraph to the region's width.
- Status: open
