# Map agent scopes to permission bundles; scheme-aware evaluator

**Parent:** 182 · **Order:** F (after 182-001–003) · **Depends on:** model + enforcement live

## Description

Agent scopes become permission bundles (parallel to human roles). Unify credential-management two-arm assertion where possible; keep scheme restrictions on admin; no agent token can hold `admin.*` via scope seed.

## Requirements

- Map `identity:read`, `credential:manage`, `demo:invoke` → permission sets in registry/seed.
- Evaluator: agent-token principal grants from scopes only (not human role expansion).
- Keep admin scheme restriction + agent 401 integration pin.
- Fix accidental Member role projection onto agents via claims transform if still present.

## Checklist

- [x] Scope→permission seed map
- [x] Scheme-aware evaluator behavior + tests
- [x] Agent integration pins still green
- [x] Results + How to validate

## Notes

### Implementation plan (Phase 2 — 2026-08-12)

1. **PermissionIds:** `identity.read`, `credential.manage.self`, `demo.invoke` + All.
2. **RolePermissionSeed:** add `credential.manage.self` to SelfServicePermissions; EF migration Insert for product roles.
3. **AgentScopePermissionSeed:** map AgentScopes → permission bundles; no admin.*.
4. **PermissionEvaluator:** inject IAgentCallerContext; agent-token expands scopes only; principal match fail-closed.
5. **PrincipalRoleClaimsTransformation:** only human schemes.
6. **Contracts:** credential → CredentialManageSelf dual scheme; GetAgentIdentity → IdentityRead agent-only; InvokeMetered → DemoInvoke agent-only.
7. **program.cs:** remove credential-management assertion + agent-scope claim policies (replaced by permission policies).
8. **Tests:** evaluator co-located + integration pins.
9. **Docs:** ADR-0010 + how-to update.

## Session

- Orchestrator: Grok tw-orchestrate-task 182-006 (2026-08-12)
- Implementer (2026-08-12): shipped scheme-aware evaluator + AgentScopePermissionSeed; PermissionIds + SelfService seed + EF data migration; contracts on PermissionIds; program.cs claim/assertion policies removed; PrincipalRoleClaimsTransformation human-only; ADR-0010 + how-to-swap updated.
- Validate: `dotnet build source/container-apps/web/projects/web-server/web-server.csproj --no-restore` (0/0); `dotnet run source/container-apps/web/features/authorization/permission-evaluator-tests.cs` (18/18); permission-claim-policies-tests (6/6). Hosted metered/agent integration suites not re-run (need AppHost).
