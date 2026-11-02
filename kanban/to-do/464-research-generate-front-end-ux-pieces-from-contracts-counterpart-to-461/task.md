# Research: generate front-end (UX) pieces from contracts — counterpart to 461

## Description

Research task (proposal only, no implementation). This is the front-end counterpart to task 461
(`kanban/to-do/461-research-infer-role-aggregate-from-contracts-contract-driven-aggregate-generation`).
Using the Roles slice, decide which SPA pieces can be source-generated from the contracts:
- the typed API surface screens call (action sets over `DefaultApiHandler`)
- the DTOs consumed and the fetchers for server-bound state
- screen states (loading, error, success)
- `[Destructive]` confirm
- mocks
- form bindings and validation
- CRUD scaffolding

Assignee: Jenzi

Kitchen draft: `contracts-frontend-example.md` (real code quoted verbatim with paths; proposed code marked).

## Boundary

- Visuals and layout stay hand-written (Razor markup, CSS, copy). The generator emits no markup.
- Purely local state and actions that never touch the server (e.g. `SetPermissionSelectedActionSet`
  draft toggles) stay hand-written, and the generator must not know about them.

## Requirement: no implicit hand-written pieces (same rule as 461)

Every hand-written piece gets a generated partial hook or a stub that fails until filled in:
- `ConfirmXxxAsync` for `[Destructive]` operations is a required partial method, so the build fails until it's written.
- Generated page code-behind without `.razor` markup triggers an analyzer error.
- A missing mock factory triggers an analyzer warning.
- Optional `partial void` hooks: `OnBuildingRequest`, `OnSucceeded`, `OnXxxLoaded`.

## Findings so far

- Already generated or derived today:
  - ActionSet dispatch methods (TimeWarp.State)
  - `[StateAccess]` accessors
  - `[Page]` routes and policy
  - `ActionRoster`
  - the mock factory registry
  - the `[ReferenceableResource]` registry
  - TWA0022 and TWA0026 enforce dispatch through actions.
- Hand-written today:
  - every server-bound `XxxActionSet` (GetRequest + HandleSuccess)
  - success flags (`LastCreatedRoleId`, `LastSetRolePermissionsSucceeded`)
  - `ConfirmAsync` overrides
- Gap: the `UpdateRole`, `DeleteRole` and `GetRole` contracts have no SPA action sets or screens.

## Checklist

- [ ] Review the kitchen draft with Steven
- [ ] Decide the opt-in shape (`[ContractState(...)]` on the state vs per-contract attribute)
- [ ] Decide `ContractResult<T>` slots vs keeping hand-written success flags
- [ ] Decide conflict rule with existing hand-written action sets (generate-if-absent vs require removal)
- [ ] Decide refetch/invalidation declaration (default none)
- [ ] Confirm every hand-written piece has a generated hook or failing stub
- [ ] Split implementation tasks (coordinate with 461)

## Session

- Created: 2955984 (2026-11-02)

## Notes

- Related: 461 (back-end counterpart). Draft produced read-only from origin/HEAD `f003919db`.
