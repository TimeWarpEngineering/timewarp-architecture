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

- [x] RFC authored: `rfc/rfc.md` (2026-11-04, six decisions; draft was stale on the second child set and on generator visibility)
- [x] Ballots collected (three reviewers, one adversarial; two rounds)
- [x] Tally and Steve resolves dissent (2026-11-05: analyzer, not generator; see rfc/rfc.md Final resolution)
- [x] Fold-in: task split filed (472–476). Skill text lands with the analyzer in 474, where the rule exists.

- [x] Review the kitchen draft with Steven (RFC ballot, two rounds, 2026-11-04/05)
- [x] Decide `IRoleStore` vs `IRoleRepository` (IRoleStore, Steven 2026-11-02)
- [x] `RoleStore` callers confirmed: seven roles handlers; retirement is part of the Roles-by-hand task
- [x] Hand-written pieces: superseded. Nothing is generated; the whole aggregate is hand-written (473) and the analyzer (474) is the build check
- [x] Attribute shape: none. No declaration; the analyzer links by operation group and member name
- [x] Migration and seed: InsertData in the FK migration from RoleIds (Decision 6, 3–0)
- [x] Split: 472 OpenAPI summary fix, 473 Roles aggregate by hand, 474 contract-agreement analyzer, 475 FeedbackItem under the analyzer, 476 spike on generating application glue. Order aggregate stays with backlog 118.

## Session

- RFC ballot: 66368815-cdd5-46be-bde8-a09d94e1cdc8 (2026-11-04), protocol `tw-rfc-ballot`

- Created: 2933507 (2026-11-02)

## Notes

Draft produced read-only from origin/HEAD `c12d08361` on 2026-11-01.

RFC (2026-11-04): `rfc/rfc.md`. Steve chose the rfc-ballot protocol over back-and-forth because the
declaration shape ships in the template. Finding while authoring: `web-domain` does not reference
`web-contracts`, so the draft's generator cannot see contract syntax; that is Decision 1. The slice
now has two child sets (permissions and agent scopes, task 466).

Resolution (Steve, 2026-11-05): no entity generator. Domain hand-written, domain-first. An
agreement analyzer in web-application checks the aggregate satisfies the contracts. Glue generation
(projection + mutation call) is evaluated after the analyzer covers Roles and FeedbackItem.
Documents: rfc/rfc.md, rfc/decision-2-code-comparison.md, rfc/option-2c-full-code.md,
rfc/option-2c-minimal.md (the last three are the rejected generator shapes, kept as record).

## Results

- Outcome: **no entity generator.** Domain stays domain-first and hand-written; contracts stay
  UI-first; an agreement analyzer in web-application (the only project that sees both) proves the
  aggregate satisfies its operation group. Glue generation (projection, mutation call) is a later
  spike. Steve, 2026-11-05.
- Ballot record: `rfc/rfc.md` (six decisions, three reviewers across two vendors, two rounds,
  every falsifiable claim re-verified against the worktree). Rejected generator shapes kept as code
  in `rfc/decision-2-code-comparison.md`, `rfc/option-2c-full-code.md`, `rfc/option-2c-minimal.md`.
- Decisions that survive into 473: hand-written Invariants (4B, 3–0); owned collections on the
  existing grant tables with the authorization read ports kept (5A, 3–0); InsertData seed in the FK
  migration plus a product-role delete guard (6A, 3–0).
- Findings that changed the draft: web-domain cannot see contracts (and must not); the slice has
  two child sets since task 466; protected core cannot be a domain hook; `partial void` does not
  fail the build; both grant ports have platform readers; delete-role has no product-role guard.
- Follow-on tasks: 472, 473, 474, 475, 476 (all in to-do, published). Third analyzer shape,
  Order, belongs to backlog 118.
