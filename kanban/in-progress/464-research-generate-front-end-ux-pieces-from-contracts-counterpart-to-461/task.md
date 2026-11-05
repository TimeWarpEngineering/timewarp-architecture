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


Kitchen draft: `contracts-frontend-example.md` (real code quoted verbatim with paths; proposed code marked).

## Outcome of 461 (read first; Steve 2026-11-05)

461 is resolved. There is **no** entity generator: domain is modeled first by hand, and
`TWA0039–TWA0043` (task 474, `contract-agreement-analyzer.cs`) prove that an aggregate satisfies
its operation group's contracts. Glue generation in the application layer was spiked (476,
`kanban/done/476-*/proposal.md`) and Steve chose "analyzer alone" because only 3 mutation calls
were mechanical and by-name mapping had a real leak case. The ballot record is
`kanban/in-progress/461-*/rfc/rfc.md`.

What that means for this task:

- Do not assume a back-end generator exists to coordinate with. The SPA side stands alone.
- Options must be **code documents** in the kitchen (input the developer writes, what is
  generated, what stays hand-written), not prose. Steve compares code, not descriptions.
- Hooks that must be written are **extended partial methods with an access modifier** (missing
  body is CS8795). `partial void` is silent and only fits optional hooks.
- Consider an **analyzer-first** answer here too: for each candidate piece, say whether a build
  check that proves agreement (contract ↔ action set, DTO, mock, validator) is enough before a
  generator is justified. Count the mechanical pieces in Roles the way 476 did.
- Roles is now a real aggregate (473) and Profile/Roles contracts were renamed to match
  (`DisplayName`, `Permissions`); read current master, not the `f003919db` draft.
- Proposal only. Stop at the options and Steve's question; do not split implementation tasks.

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
