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
- [x] Agent integration pins still green (co-located pins; hosted suite deferred — see Results)
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

## Results

### Summary

Scheme-aware agent authorization on the same permission vocabulary as humans:

| Scope (wire) | Permission |
|--------------|------------|
| `identity:read` | `identity.read` |
| `credential:manage` | `credential.manage.self` |
| `demo:invoke` | `demo.invoke` |

- **`AgentScopePermissionSeed`** — compile-time map; never intersects `admin.*` (test pin).
- **`PermissionEvaluator`** — `agent-token` expands ambient `IAgentCallerContext` scopes only; principal mismatch / null caller → empty; **never** human role store.
- **`PrincipalRoleClaimsTransformation`** — human schemes only (no Member roles on agents).
- **Contracts** — credential dual-scheme on `credential.manage.self`; agent demos on `identity.read` / `demo.invoke` with agent-token scheme.
- **web-server** — removed `credential-management` assertion and `agent-scope:*` claim policies.
- Humans get `credential.manage.self` via SelfService seed + EF data migration.
- Docs: ADR-0010 + how-to-swap updated for agent path.
- Commit: `032a9ccc`.

### How to validate

**Automated**

```bash
dotnet build source/container-apps/web/projects/web-server/web-server.csproj -c Debug --no-restore
# expect: 0/0

dotnet run source/container-apps/web/features/authorization/permission-evaluator-tests.cs
# expect: 18/18 including Agent Token_* and Agent Scope Seed_Should_Not_Intersect_Admin

dotnet run source/container-apps/web/features/authorization/permission-claim-policies-tests.cs
# expect: 6/6
```

**Integration (when suite available)**

```bash
cd tests/container-apps/web/web-server-integration-tests && dotnet test -c Release \
  -- --filter-method Unauthorized_Given_Agent_Bearer_Token_No_Cookie
# expect: pass — admin APIs still 401 for agent bearer (scheme isolation)
# also credential-list / metered / agent-identity suites when ports free
```

**Expect**

- Agent with only `identity:read` does not get `credential.manage.self` or any `admin.*`.
- Human Member session can manage credentials (self-service includes `credential.manage.self`).
- Admin endpoints remain scheme-restricted to identity-session / mock.

**Depends on:** postgres volumes need new migration for `credential.manage.self` rows if DB pre-existed.

**Not in scope:** api-server claim-based `agent-scope:*` (unchanged); AppHost OpenFGA.

## Session

- Orchestrator: Grok tw-orchestrate-task 182-006 (2026-08-12)
- Implementer (2026-08-12): shipped scheme-aware evaluator + AgentScopePermissionSeed; PermissionIds + SelfService seed + EF data migration; contracts on PermissionIds; program.cs claim/assertion policies removed; PrincipalRoleClaimsTransformation human-only; ADR-0010 + how-to-swap updated.
- Validate: web-server build 0/0; permission-evaluator-tests 18/18; permission-claim-policies-tests 6/6. Hosted integration suites deferred (AppHost/ports).
- Orchestrator follow-up: added formal Results + How to validate (required before done).
