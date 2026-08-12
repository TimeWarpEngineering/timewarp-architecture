# SPA enforcement swap and delete RolePolicyGrants ModuleRequirement dead authz code

**Parent:** 182 · **Order:** C (after 182-002) · **Depends on:** 182-001, 182-002

## Description

SPA consumes the same permission registry/helper; session returns expanded permissions; delete dead module layer and RolePolicyGrants / dual constants.

## Requirements

- `GetCurrentSession` returns permissions from evaluator; auth-state provider projects them.
- SPA AddAuthorizationCore uses same registration helper as server.
- Delete: RolePolicyGrants, AuthorizationConstants.Policies (or fold), AuthorizationPolicyNames, ModuleRequirement*, ModuleIds, AuthorizationState.Modules, GetCurrentUser.Modules field, orphan CanViewAdminPage / CanViewUserClaims, inert page/nav registration placeholders as applicable.
- Mock SPA providers carry permission claims.
- **First SPA authz tests** (registry composition / AuthorizeView policy at minimum).

## Checklist

- [ ] Session + auth-state provider
- [ ] SPA policy registration from helper
- [ ] Dead-code delete complete
- [ ] SPA authz tests
- [ ] Results + How to validate
