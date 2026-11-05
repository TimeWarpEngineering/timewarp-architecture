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

## Goal (implementer restatement)

Write a proposal that Steve can decide from. It covers which SPA pieces for the Roles slice could be
built from the contracts, using the current master code. No product code changes.

Acceptance criteria:

- A kitchen `proposal.md` built on current master, not the `f003919db` draft. It contains:
  - an inventory of the Roles SPA action sets that counts the mechanical pieces the way 476 did
  - each option as a code document: what the developer writes, what is generated, what stays
    hand-written
- Each candidate piece (action sets, fetchers and snapshots, screen states, `[Destructive]`
  confirm, mocks, forms and validation, CRUD scaffolding) has an analyzer-first answer: is a build
  check that proves agreement enough before a generator is justified?
- Every hand-written piece in a generator option has a required hook or a failing check. Required
  hooks are extended partial methods with an access modifier (CS8795). Optional hooks are
  `partial void`.
- Facts that limit the options are stated with paths.
- The proposal ends with Steve's question. No option is picked and no implementation tasks are
  split.

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
- 2026-11-05 implementer (ganda task work): read current master Roles SPA, contracts, generators,
  and the 476 proposal. Wrote `proposal.md` with options A–D as code. Marked
  `contracts-frontend-example.md` as superseded. No product code changed.

## Results

`proposal.md` in this kitchen is the deliverable. Findings:

- **Mechanical pieces in Roles:** 4 server-bound handlers. 2 of them have mechanical request builds
  (Create, Fetch). 4 success writes are mechanical (1 snapshot, 3 flags). 3 required hand-written
  pieces read or seed local drafts. That is about 12 lines of mechanical body. SPA-wide, only 6 of
  41 `DefaultApiHandler`s pass the contract request through.
- **Feasibility limit:** Roslyn generators cannot see each other's output. A generated action set
  gets no dispatch method from TimeWarp.State, no handler registration from TimeWarp.Mediator, and
  no roster entry. A generated `[Page]` partial gets no route. Only bodies inside hand-written
  partial types (Option B) work. The old draft's whole-set generator is Option C.
- **Hazard case:** if draft seeding in FetchRoles were an optional hook and nobody wrote it, Save
  would post an empty permission list and strip every non-Administrator role. No test covers that
  path. Any generator must make that hook required (CS8795).
- **Analyzer-first candidates (Option A):** A1 SPA contract coverage (it flags GetRole, UpdateRole,
  and DeleteRole), A2 `[Destructive]` ⇔ `ConfirmAsync` (0 contracts use `[Destructive]`, 2 handlers
  confirm), A3 `[CatalogAction]` ⇔ contract policy and description (CreateRole's description
  already differs), A4 mock coverage (today a missing factory falls through to the real API with
  only a Debug log).
- **Option D:** `ContractResult<T>` slots replace the 9 hand-written success flags and ids, and the
  sticky `LastCreatedRoleId` comparison in `RoleForm.razor`.
- **Forms and validation:** nothing to generate. The form already binds the contract's
  `IRoleDetails` and uses the contract's validator.
- **Steve's question** is in section 7. No option is picked and no implementation tasks are split.

### How to validate

Smoke: open `kanban/in-progress/464-*/proposal.md`. Check each section 4 fact against the cited
path:

- `git grep -n "\[Destructive" -- source` finds no uses.
- `git grep -l "DefaultApiHandler<" -- source/container-apps/web/projects/web-spa | wc -l` prints 42
  (41 handlers plus `default-api-handler.cs`).
- `git grep -n "UpdateRole\|DeleteRole" -- source/container-apps/web/projects/web-spa` finds
  nothing outside comments.
- `sed -n 55,70p source/container-apps/web/projects/web-spa/services/mocks/mock-web-api-service.cs`
  shows the Debug fallback.

Expect: every claim in sections 2 and 4 matches the code. Every option in section 5 is a code
document, with input, generated code, and hand-written hooks. Required hooks carry an access
modifier. Section 7 asks the question and does not pick.

## Notes

- Related: 461 (back-end counterpart). Draft produced read-only from origin/HEAD `f003919db`.
