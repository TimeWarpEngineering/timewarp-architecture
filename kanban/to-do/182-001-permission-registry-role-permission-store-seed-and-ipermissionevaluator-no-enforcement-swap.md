# Permission registry, role-permission store, seed, and IPermissionEvaluator (no enforcement swap)

**Parent:** 182 · **Order:** A (first) · **Enforcement:** RequireRole still live

## Description

Stand up the permission model and grant expansion with **zero user-visible enforcement change**. After this child, RequireRole / RolePolicyGrants still gate surfaces; new store and evaluator are tested in isolation.

## Requirements

- Permission registry: dotted lowercase `const string` ids (`admin.roles.read`, `admin.roles.manage`, `admin.principals.read`, `admin.principals.manage`, `admin.access`, `developer.access`, `developer.claims.read`, `profile.read`, `settings.read`, … per disposition seed table).
- Role→permission grant store: dual-mode in-memory + EF (mirror principal-role store); seed Administrator / Member / Developer / Operator (Operator may be empty reserved).
- `IPermissionEvaluator` + default impl (principal → roles → permissions); scheme-aware so agents do not inherit human expansion.
- Co-located Jaribu tests: empty, seed expand, no admin on Member, read≠manage sets differ.
- Draft ADR notes alongside (accept in 182-005).
- **Do not** change `[EndpointAuthorize]`, SPA policies, or program.cs RequireRole.

## Checklist

- [ ] Registry + single registration helper skeleton (may not yet replace both constant classes until C)
- [ ] Role-permission store + seed
- [ ] IPermissionEvaluator + tests
- [ ] ADR draft started under documentation or task folder
- [ ] `dev build` 0/0; Results + How to validate

## Notes

Disposition: `kanban/in-progress/182-…/disposition.md`. Round-2: `review/round-2/grok.md`.
