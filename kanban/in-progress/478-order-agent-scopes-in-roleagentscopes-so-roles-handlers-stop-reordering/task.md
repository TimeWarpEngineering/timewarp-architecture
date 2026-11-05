# Order agent scopes in Role.AgentScopes so Roles handlers stop reordering

## Description

`Role.AgentScopes` returns grants in stored order. Three Roles handlers reorder them by hand with
`AgentScopeOrder.OrderKnown` (`platform/authorization/agent-scope-order-application.cs`):

- `get-role/get-role-handler-application.cs` (projection)
- `get-roles/get-roles-handler-application.cs` (projection)
- `set-role-agent-scopes/set-role-agent-scopes-handler-application.cs` (input before
  `role.SetAgentScopes`, and the response)

Every read of a role's agent scopes wants catalog order (`AgentScopes.All`). Task 476 recorded the
rule in `tw-aggregate-pattern`: ordering and normalization that every read wants belong to the
root, not the handler. Move the ordering into the Role aggregate so the Roles handlers project
`role.AgentScopes` as is.

## Requirements

- `Role.AgentScopes` (or `SetAgentScopes`, so the stored list is already ordered) yields grants in
  `RoleCatalog.AgentScopes` order. The domain uses its own catalog copy
  (`role-catalog-domain.cs`), not `AgentScopeOrder`, because web-domain does not reference the
  application layer.
- GetRole, GetRoles, and SetRoleAgentScopes stop calling `AgentScopeOrder.OrderKnown` on role
  scopes. Unknown scopes are still rejected or dropped as today (keep the current behavior and say
  which in the Design region).
- `AgentScopeOrder` stays for Principals and `AgentScopeApproval`, which do not go through the Role
  aggregate. Update its Design region so it no longer lists the Roles callers.
- A domain unit test proves the order: set scopes in reverse catalog order, read them back in
  catalog order. Existing Roles handler tests still pass.
- Read order must not depend on EF load order of the owned collection.

## Goal (implementer restatement)

Make the Role aggregate own the catalog order of its agent scopes, so no Roles handler sorts them.

Acceptance:

- Reading `Role.AgentScopes` gives grants in `RoleCatalog.AgentScopes` order, whatever order they
  were set in and whatever order EF loaded the owned rows in. Only the domain catalog copy is used.
- GetRole, GetRoles, and SetRoleAgentScopes no longer call `AgentScopeOrder.OrderKnown`. Unknown
  scopes are still rejected (contract validator 400; `Role.SetAgentScopes` throws as the belt), and
  the SetRoleAgentScopes Design region says so.
- `AgentScopeOrder` stays for SetPrincipalAgentScopes and `AgentScopeApproval`; its Design region
  drops the Roles callers.
- A domain test sets scopes in reverse catalog order and reads them back in catalog order. Existing
  Roles tests pass.
- `dev build` is clean, `dev test` is green, and `ganda repo audit` exits 0.

## Checklist

- [x] Ordering moved into `Role`; domain test added
- [x] Three Roles handlers no longer reorder; Design regions updated
- [x] `dev build` 0 warnings, 0 errors; `dev test` green
- [x] `ganda repo audit` exits 0

## Session

- Created: 1002142 (2026-11-05)
- 2026-11-05 implementer (ganda task work): ordering moved into `Role.AgentScopes`; handlers,
  Design regions, and domain tests updated; build, tests, and audit run (see Results).
- Implement oracle: implemented by implementer-claude (claude, vendor anthropic, model claude-opus-5-5, response model claude-opus-5-5), session b6a65fe8-eb49-4187-a33a-876151f3de12, max-turns 500, Done — 2026-11-05T08:36:54Z
- Review oracle: claude-opus-5-5 (ganda task work, headless), tw-implementation-review effort 2,
  roster general, turn cap 120. Round 1 → accepted-exceptions (0 open) — 2026-11-05
- Review oracle: review by reviewer-claude (claude, vendor anthropic, model claude-opus-5-5, response model claude-opus-5-5), session a7d08793-c7d0-4035-9473-fe4853654dec, max-turns 120, artifacts general.md, merged.md — 2026-11-05T08:39:11Z

## Results

- `Role.AgentScopes` (`role-domain.cs`) now returns a copy of the backing list sorted by
  `RoleCatalog.AgentScopeRank` (new, `role-catalog-domain.cs`): the scope's index in the domain
  copy of `AgentScopes.All`. The sort happens on read, so EF load order of
  `identity.role_agent_scopes` rows does not matter. EF still maps the private `AgentScopeList`
  field (field access mode), and a child that is already present stays the same instance. A scope
  outside the catalog sorts last (rank `int.MaxValue`) instead of being dropped, so the
  `Invariants` save-time check still sees it.
- GetRole, GetRoles, and SetRoleAgentScopes project `role.AgentScopes` as is. They no longer import
  `TimeWarp.Architecture.Authorization` or call `AgentScopeOrder`. SetRoleAgentScopes passes
  `command.Scopes` to `Role.SetAgentScopes` without changing it.
- Unknown scopes: **rejected, not dropped**. The contract `Validator` already answered 400 before
  the handler ran, and that does not change. Before, the handler's `OrderKnown` also dropped unknown
  scopes as a second check. Now `Role.SetAgentScopes` throws `ArgumentException` for an unknown
  scope that gets past the validator. The SetRoleAgentScopes Design region says this.
- The `AgentScopeOrder` Design region now names only SetPrincipalAgentScopes and
  `AgentScopeApproval`, and points the Roles callers to `Role.AgentScopes`.
- Domain tests (`tests/container-apps/web/web-domain-tests/role-tests.cs`, `Set_agent_scopes`):
  - `Full_replace_and_empty_clears` now expects catalog order.
  - New: `Reads_back_in_catalog_order_when_set_in_reverse`.
  - New: `Reads_back_in_catalog_order_whatever_the_load_order` fills the backing list out of order,
    the way EF can.
  - New: `Keeps_existing_children_as_the_same_instance`.
  - `Invariants.BackingList` is now `internal` so the new tests can use it.
- Gates:
  - `./bin/dev build -q`: succeeded with 0 warnings.
  - `./bin/dev test -q`: 23 of 23 projects passed. web-domain-tests ran 57 tests and all passed.
  - `ganda repo audit`: exit 0, 35 checks passed. Two warnings were already there before this task:
    - `kanban-landed`: 9 stale claims on other tasks.
    - `source-file-line-cap`: EF migration designers, the model snapshot, and a done-task research
      runfile.

  I did not run `--fix` for `kanban-landed`. It would release claims on other tasks, which is
  board state outside this task. `source-file-line-cap` is a warning by policy (469-001), and
  TWA0038 enforces the cap.

### How to validate

- Smoke: `dotnet run --project tests/container-apps/web/web-domain-tests` from the repo root. Then
  `./bin/dev test -g integration` for the Roles handler and EF store suites.
- Expect: web-domain-tests passes all tests, including the three new `Set_agent_scopes` tests.
  `rg "AgentScopeOrder" source/container-apps/web/features/admin/roles` finds no matches. A
  `PUT api/Roles/{id}/agent-scopes` with `["feedback:read","identity:read"]` answers
  `["identity:read","feedback:read"]`, and a later GetRole and GetRoles return the same order. An
  unknown scope still answers 400.

### Review disposition

- Rounds: 1. Effort 2, roster: general (review oracle, claude-opus-5-5).
- Final counts: bug 0, suggestion 0, nit 1 (wontfix). Open: 0.
- Disposition: **accepted-exceptions**. M1 (nit): the Design comment at `role-domain.cs:16` is
  144 characters and was not reflowed. It is cosmetic, no line-length rule is enforced, and the
  gates pass, so it is accepted as is.
- The reviewer re-ran web-domain-tests: 57 of 57 passed. The implementer restatement matches the
  task. No misread.
- Paths: `review/review-framework.md`, `review/round-1/general.md`, `review/round-1/merged.md`,
  `review/disposition.md`.

## Review

- Round 1: Done

## Notes

Filed from task 476 (decision A, analyzer alone). Evidence: 476 `proposal.md` sections 2 and 8.
