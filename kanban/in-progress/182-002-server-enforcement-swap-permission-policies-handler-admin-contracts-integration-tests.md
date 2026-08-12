# Server enforcement swap: permission policies, handler, admin contracts, integration tests

**Parent:** 182 · **Order:** B (after 182-001) · **Depends on:** 182-001 green

## Description

Server evaluates **permissions** via `PermissionRequirement` + `IPermissionEvaluator`. Admin contracts and program.cs leave `RequireRole(Administrator)`. SPA still uses RolePolicyGrants until 182-003 — seed must keep observably equivalent grants.

## Requirements

- Register permission policies from single helper on server.
- Move admin contracts to `PermissionIds.*` policies; split read vs manage endpoints.
- Delete inline RequireRole admin policies in program.cs.
- Integration tests: member 403, admin 200, **read without manage cannot Create/Set**, agent still 401 on admin (scheme).
- api-server out of scope.

## Checklist

- [ ] PermissionRequirement + handler registered
- [ ] Admin contracts + program.cs
- [ ] roles-authorization + principals-authorization tests extended
- [ ] Results + How to validate

## Notes

Do not release template to consumers with only B done (SPA still role-based).
