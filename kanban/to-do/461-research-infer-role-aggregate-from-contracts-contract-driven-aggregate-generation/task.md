# Research: infer Role aggregate from contracts (contract-driven aggregate generation)

## Description

Research task (proposal only, no implementation). Contract-driven development: the contract is the
agreement between front end and back end. Goal: decide whether, and how, an aggregate root plus its
children, a store port, and a default EF mapping can be source-generated from the existing Roles
contracts, following the `tw-aggregate-pattern` golden pattern (Profile exemplar).


Kitchen draft: `contracts-aggregate-example.md` (worked example with real code quoted verbatim and
proposed code marked).

## Proposal summary

- Add one contract-level declaration (`role-aggregate-contracts.cs`, `RoleAggregate` with
  `[AggregateRoot]`, `[AggregateFields]`, `[AggregateChildren]`, `[AggregateOperations]`). It
  references contract types only, because contracts must not reference domain.
- Infer from what the contracts already state:
  - `RoleId` typed id from the `{RoleId:guid}` route.
  - Name and Description from `IRoleDetails` and `RoleDetailsValidator`.
  - The Permissions child set from `SetRolePermissions.Command.PermissionIds`.
  - The rule that each permission must be in `PermissionIds.All`, from the RuleForEach.
  - Mutation names from the `[Operation]` names (`roles.create`, `roles.update`, `roles.set-permissions`).
- Generate:
  - `RoleId` (`[TypedId]`).
  - `Role : Entity<RoleId>, IAggregateRoot`, with fail-closed `Create`, named mutations, and a
    private `Invariants` validator.
  - The `RolePermission` child.
  - A store port.
  - `RoleEntityTypeConfiguration`, with an `identity.roles` table plus an owned
    `identity.role_permissions`.

## Decision (Steven, 2026-11-02)

- The generated port keeps the house Store naming: `IRoleStore`, not `IRoleRepository`.
- The existing `internal static class RoleStore` is retired and replaced by the generated store, provided
  it has no lingering callers outside generated code.

## Requirement: no implicit hand-written pieces

Every hand-written piece gets a generated partial hook or a stub that fails until it is filled in
(throws `NotImplementedException`, or an analyzer error), so nothing is left implicit:

- Store implementations (EF and in-memory) of `IRoleStore`.
- Handlers that call the aggregate (create/update/delete/set-permissions).
- Migration plus seed data for the existing compile-time `RoleIds`.
- Retiring `IRolePermissionStore`/`RolePermissionGrant` (and its EF configuration).
- Already marked: `partial void OnSettingPermissions` (Administrator protected-core).

## Hand-written exceptions (not inferable)

- Administrator protected-core rule (`AdminLockoutGuards`). The generated `partial void OnSettingPermissions` hook carries it.
- Data migration and seed: `RoleIds` are compile-time Guids with no roles table today, so the four
  product roles need seed rows. Existing grants already live in `identity.role_permissions`.
- Retiring `IRolePermissionStore` (in-memory and EF), `RolePermissionGrant`, and its entity type configuration.

## Side finding

- The FastEndpoint generator (`fast-endpoint-source-generator.cs`) emits an empty OpenAPI summary for
  SetRolePermissions. It reads the XML `<summary>` on the inner Command, which the contract puts on
  the outer class, and it ignores `[Operation(Description = ...)]`. That's a small fix on its own.

## Checklist

- [ ] Review the kitchen draft with Steven
- [x] Decide `IRoleStore` vs `IRoleRepository` (IRoleStore, Steven 2026-11-02)
- [ ] Confirm `RoleStore` has no lingering callers outside generated code, then retire it
- [ ] Every hand-written piece has a generated partial hook or failing stub (stores EF + in-memory, handlers, migration + RoleIds seed, IRolePermissionStore/RolePermissionGrant retirement, OnSettingPermissions)
- [ ] Decide attribute shape for the contract-level aggregate declaration
- [ ] Decide migration and seed approach for compile-time RoleIds
- [ ] Split implementation tasks (generator, Roles migration, OpenAPI summary fix)

## Session

- Created: 2933507 (2026-11-02)

## Notes

Draft produced read-only from origin/HEAD `c12d08361` on 2026-11-01.
